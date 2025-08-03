#!/usr/bin/env python3
"""
TestKit FastMCP Server
Быстрая версия MCP сервера с использованием FastMCP
Предоставляет доступ к TestKit SQL Indexer через MCP протокол
"""

import sys
import os
from pathlib import Path
from typing import List, Optional
from fastmcp import FastMCP

# Добавляем путь к модулям testkit_query
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

try:
    from testkit_query import TestKitQueryEngine, QueryResult
except ImportError as e:
    print(f"Error importing testkit_query: {e}")
    print("Make sure you're running from the testkit-sql-indexer directory")
    sys.exit(1)

class TestKitFastMCPServer:
    """FastMCP Server для TestKit SQL Indexer"""
    
    def __init__(self, db_path: str = "testkit_index.db"):
        self.db_path = db_path
        self.query_engine = TestKitQueryEngine(db_path)
        self.mcp = FastMCP("testkitdex", "0.1.0")
        self._register_tools()
    
    def _register_tools(self):
        """Регистрирует инструменты"""
        
        @self.mcp.tool(name="tk_search_by_name", description="Fuzzy / exact lookup of methods / classes")
        def search_by_name(query: str, limit: int = 20) -> str:
            """Поиск методов по имени"""
            try:
                results = self.query_engine.search_methods_by_name(query)
                results = results[:limit]
                
                if not results:
                    return f"No methods found for query: '{query}'"
                
                output = [f"Found {len(results)} methods for query: '{query}'"]
                output.append("")
                
                for i, result in enumerate(results, 1):
                    output.append(f"{i}. **{result.component_name}.{result.method_name}**")
                    output.append(f"   - Return type: `{result.return_type}`")
                    output.append(f"   - File: `{result.file_path}:{result.line_number}`")
                    if result.description:
                        output.append(f"   - Description: {result.description}")
                    if result.tags:
                        output.append(f"   - Tags: {', '.join(result.tags)}")
                    output.append("")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error searching by name: {e}"
        
        @self.mcp.tool(name="tk_search_by_tags", description="Quickly find builders, mocks, etc.")
        def search_by_tags(tags: List[str], match: str = "any", limit: int = 20) -> str:
            """Поиск методов по тегам"""
            try:
                if match == "all":
                    # Ищем методы, которые содержат ВСЕ указанные теги
                    results = []
                    for tag in tags:
                        tag_results = self.query_engine.search_methods_by_tag(tag)
                        if not results:
                            results = tag_results
                        else:
                            # Пересечение результатов
                            result_set = {(r.component_name, r.method_name) for r in results}
                            results = [r for r in tag_results if (r.component_name, r.method_name) in result_set]
                else:
                    # Ищем методы, которые содержат ЛЮБОЙ из указанных тегов
                    results = []
                    for tag in tags:
                        tag_results = self.query_engine.search_methods_by_tag(tag)
                        results.extend(tag_results)
                    
                    # Убираем дубликаты
                    seen = set()
                    unique_results = []
                    for result in results:
                        key = (result.component_name, result.method_name)
                        if key not in seen:
                            seen.add(key)
                            unique_results.append(result)
                    results = unique_results
                
                results = results[:limit]
                
                if not results:
                    return f"No methods found for tags: {tags} (match: {match})"
                
                output = [f"Found {len(results)} methods for tags: {tags} (match: {match})"]
                output.append("")
                
                for i, result in enumerate(results, 1):
                    output.append(f"{i}. **{result.component_name}.{result.method_name}**")
                    output.append(f"   - Return type: `{result.return_type}`")
                    output.append(f"   - File: `{result.file_path}:{result.line_number}`")
                    if result.description:
                        output.append(f"   - Description: {result.description}")
                    if result.tags:
                        output.append(f"   - Tags: {', '.join(result.tags)}")
                    output.append("")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error searching by tags: {e}"

        @self.mcp.tool(name="tk_method_details", description="Get detailed information about a method")
        def method_details(name: str) -> str:
            """Детальная информация о методе"""
            try:
                # Ищем точное совпадение
                results = self.query_engine.search_methods_by_name(name, exact_match=True)
                
                if not results:
                    # Пробуем нечеткий поиск
                    results = self.query_engine.search_methods_by_name(name)
                    if not results:
                        return f"Method '{name}' not found"
                    elif len(results) > 1:
                        output = [f"Multiple methods found for '{name}':"]
                        output.append("")
                        for i, result in enumerate(results, 1):
                            output.append(f"{i}. {result.component_name}.{result.method_name}")
                        output.append("")
                        output.append("Please specify the exact method name.")
                        return "\n".join(output)
                
                result = results[0]
                
                output = []
                output.append(f"# {result.component_name}.{result.method_name}")
                output.append("")
                
                # Полная сигнатура
                full_signature = self.query_engine.get_method_signature(result.method_name, result.component_name)
                if full_signature:
                    output.append("## Full Signature")
                    output.append(f"```csharp")
                    output.append(full_signature)
                    output.append(f"```")
                    output.append("")
                
                # Базовая сигнатура
                output.append("## Basic Signature")
                static_text = "static " if result.is_static else ""
                generic_text = "<T>" if result.is_generic else ""
                output.append(f"```csharp")
                output.append(f"{static_text}{result.return_type} {result.method_name}{generic_text}()")
                output.append(f"```")
                output.append("")
                
                # Описание
                if result.description:
                    output.append("## Description")
                    output.append(result.description)
                    output.append("")
                
                # Теги
                if result.tags:
                    output.append("## Tags")
                    output.append(", ".join(result.tags))
                    output.append("")
                
                # Файл и строка
                output.append("## Location")
                output.append(f"File: `{result.file_path}`")
                output.append(f"Line: {result.line_number}")
                output.append("")
                
                # Компонент
                output.append("## Component")
                output.append(f"Class: `{result.component_name}`")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error getting method details: {e}"

        @self.mcp.tool(name="tk_method_signature", description="Get full method signature with parameters")
        def method_signature(name: str, component: str = "") -> str:
            """Полная сигнатура метода"""
            try:
                signature = self.query_engine.get_method_signature(name, component)
                if signature:
                    output = [f"# Method Signature: {name}"]
                    output.append("")
                    output.append("```csharp")
                    output.append(signature)
                    output.append("```")
                    return "\n".join(output)
                else:
                    return f"Method '{name}' not found"
            except Exception as e:
                return f"Error getting method signature: {e}"

        @self.mcp.tool(name="tk_search_similar", description="Find similar methods")
        def search_similar(method_name: str, limit: int = 10) -> str:
            """Поиск похожих методов"""
            try:
                results = self.query_engine.search_similar_methods(method_name, limit)
                
                if not results:
                    return f"No similar methods found for '{method_name}'"
                
                output = [f"Found {len(results)} similar methods for '{method_name}':"]
                output.append("")
                
                for i, result in enumerate(results, 1):
                    output.append(f"{i}. **{result.component_name}.{result.method_name}**")
                    output.append(f"   - Return type: `{result.return_type}`")
                    output.append(f"   - File: `{result.file_path}:{result.line_number}`")
                    if result.description:
                        output.append(f"   - Description: {result.description}")
                    if result.tags:
                        output.append(f"   - Tags: {', '.join(result.tags)}")
                    output.append("")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error searching similar methods: {e}"

        @self.mcp.tool(name="tk_get_guidance", description="Get usage guidance for a method")
        def get_guidance(method_name: str) -> str:
            """Получение рекомендаций по использованию метода"""
            try:
                # Импортируем генератор рекомендаций
                sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
                from generate_usage_guidance import UsageGuidanceGenerator
                
                generator = UsageGuidanceGenerator(self.db_path)
                guidance = generator.generate_method_guidance(method_name)
                
                if 'error' in guidance:
                    return guidance['error']
                
                output = []
                output.append(f"# Usage Guidance for {guidance['method_name']}")
                output.append(f"**Component:** {guidance['component_name']}")
                output.append(f"**Return Type:** {guidance['return_type']}")
                output.append(f"**Tags:** {', '.join(guidance['tags'])}")
                output.append("")
                
                if guidance['description']:
                    output.append(f"**Description:** {guidance['description']}")
                    output.append("")
                
                if guidance['usage_patterns']:
                    output.append("## Usage Patterns")
                    for pattern in guidance['usage_patterns']:
                        output.append(f"### {pattern['tag'].title()}")
                        output.append(f"**Description:** {pattern['description']}")
                        output.append(f"**Usage:** {pattern['usage']}")
                        output.append(f"**Example:** `{pattern['example']}`")
                        output.append(f"**Context:** {pattern['context']}")
                        output.append("")
                
                if guidance['recommendations']:
                    output.append("## Recommendations")
                    for rec in guidance['recommendations']:
                        output.append(f"- {rec}")
                    output.append("")
                
                if guidance['related_methods']:
                    output.append("## Related Methods")
                    for method in guidance['related_methods']:
                        output.append(f"- **{method['component']}.{method['name']}** ({', '.join(method['tags'])})")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error getting guidance: {e}"

        @self.mcp.tool(name="tk_get_context_methods", description="Get methods suitable for specific context")
        def get_context_methods(context: str, limit: int = 10) -> str:
            """Получение методов для конкретного контекста"""
            try:
                # Импортируем генератор рекомендаций
                sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
                from generate_usage_guidance import UsageGuidanceGenerator
                
                generator = UsageGuidanceGenerator(self.db_path)
                guidance = generator.generate_context_guidance(context)
                
                output = []
                output.append(f"# Methods for {context} context")
                output.append(f"**Context:** {guidance['context']}")
                output.append("")
                
                for method in guidance['methods']:
                    output.append(f"## {method['component']}.{method['name']}")
                    output.append(f"**Tags:** {', '.join(method['tags'])}")
                    if method['description']:
                        output.append(f"**Description:** {method['description']}")
                    output.append("")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error getting context methods: {e}"

        @self.mcp.tool(name="tk_get_tag_guidance", description="Get guidance for methods with specific tag")
        def get_tag_guidance(tag: str, limit: int = 10) -> str:
            """Получение рекомендаций для методов с конкретным тегом"""
            try:
                # Импортируем генератор рекомендаций
                sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
                from generate_usage_guidance import UsageGuidanceGenerator
                
                generator = UsageGuidanceGenerator(self.db_path)
                guidance = generator.generate_tag_guidance(tag)
                
                output = []
                output.append(f"# Methods with tag '{tag}'")
                output.append(f"**Description:** {guidance['description']}")
                output.append(f"**Usage:** {guidance['usage']}")
                output.append(f"**Example:** `{guidance['example']}`")
                output.append(f"**Context:** {guidance['context']}")
                output.append("")
                
                for method in guidance['methods']:
                    output.append(f"## {method['component']}.{method['name']}")
                    if method['description']:
                        output.append(f"**Description:** {method['description']}")
                    output.append("")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error getting tag guidance: {e}"
        
        @self.mcp.tool(name="tk_stats", description="High-level insight for the Agent")
        def get_stats() -> str:
            """Статистика базы данных"""
            try:
                stats = self.query_engine.get_statistics()
                
                output = []
                output.append("# TestKit Database Statistics")
                output.append("")
                
                # Основная статистика
                output.append("## Overview")
                output.append(f"- **Total Components**: {stats['total_components']}")
                output.append(f"- **Total Methods**: {stats['total_methods']}")
                output.append(f"- **Total Tags**: {stats['total_tags']}")
                output.append("")
                
                # Топ теги
                output.append("## Top Tags")
                for tag, count in stats['top_tags'][:10]:
                    output.append(f"- `{tag}`: {count} usages")
                output.append("")
                
                # Категории
                if 'categories' in stats:
                    output.append("## Categories")
                    for category, count in stats['categories']:
                        output.append(f"- `{category}`: {count} components")
                
                return "\n".join(output)
            except Exception as e:
                return f"Error getting statistics: {e}"
    
    def run(self):
        """Запускает MCP сервер"""
        try:
            self.mcp.run()
        except Exception as e:
            print(f"Error running MCP server: {e}")
            sys.exit(1)
        finally:
            self.query_engine.close()

def main():
    """Main function for FastMCP server"""
    import argparse
    
    parser = argparse.ArgumentParser(description="TestKit FastMCP Server")
    parser.add_argument("--db-path", default="testkit_index.db", 
                       help="Database file path")
    parser.add_argument("--test", action="store_true", 
                       help="Test the server with sample queries")
    
    args = parser.parse_args()
    
    # Проверяем существование базы данных
    db_path = Path(args.db_path)
    if not db_path.exists():
        print(f"Error: Database not found: {db_path}")
        print("Please run the indexer first to create the database.")
        sys.exit(1)
    
    # Создаем сервер
    server = TestKitFastMCPServer(args.db_path)
    
    if args.test:
        # Тестируем сервер
        print("Testing TestKit MCP Server...")
        print("=" * 50)
        
        # Тест поиска по тегам
        print("\n1. Testing tk_search_by_tags:")
        try:
            results = server.query_engine.search_methods_by_tag("builder")
            if results:
                result = f"Found {len(results)} methods with 'builder' tag"
                for i, r in enumerate(results[:3], 1):
                    result += f"\n{i}. {r.component_name}.{r.method_name}"
            else:
                result = "No methods found with 'builder' tag"
            print(result)
        except Exception as e:
            print(f"Error: {e}")
        
        # Тест статистики
        print("\n2. Testing tk_stats:")
        try:
            stats = server.query_engine.get_statistics()
            result = f"Database Statistics:\n- Components: {stats['total_components']}\n- Methods: {stats['total_methods']}\n- Tags: {stats['total_tags']}"
            print(result)
        except Exception as e:
            print(f"Error: {e}")
        
        print("\nTest completed successfully!")
    else:
        # Запускаем сервер
        print("Starting TestKit FastMCP Server...")
        print(f"Database: {args.db_path}")
        print("Available tools:")
        for tool_name in server.mcp._tools.keys():
            print(f"  - {tool_name}")
        print("\nPress Ctrl+C to stop")
        
        server.run()

if __name__ == "__main__":
    main() 