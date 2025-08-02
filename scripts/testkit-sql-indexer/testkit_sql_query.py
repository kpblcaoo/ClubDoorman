#!/usr/bin/env python3
"""
TestKit SQL Query Tool - Утилита для выполнения запросов к SQL базе данных TestKit
Позволяет быстро находить методы, компоненты и теги
"""

import sqlite3
import argparse
import sys
from typing import List, Dict, Any
from pathlib import Path

class TestKitSQLQuery:
    def __init__(self, db_path: str = "testkit_index.db"):
        self.db_path = db_path
        self.conn = None

    def connect(self):
        """Подключается к базе данных"""
        if not Path(self.db_path).exists():
            print(f"❌ Database not found: {self.db_path}")
            print("Run testkit_sql_indexer.py first to create the database")
            return False
        
        self.conn = sqlite3.connect(self.db_path)
        return True

    def close(self):
        """Закрывает соединение"""
        if self.conn:
            self.conn.close()

    def execute_query(self, query: str, params: tuple = ()) -> List[Dict[str, Any]]:
        """Выполняет SQL запрос и возвращает результаты"""
        if not self.conn:
            return []
        
        cursor = self.conn.cursor()
        cursor.execute(query, params)
        
        # Получаем названия колонок
        columns = [description[0] for description in cursor.description]
        
        # Преобразуем в список словарей
        results = []
        for row in cursor.fetchall():
            results.append(dict(zip(columns, row)))
        
        return results

    def search_methods_by_name(self, name_pattern: str) -> List[Dict[str, Any]]:
        """Поиск методов по имени"""
        query = """
            SELECT m.name, m.return_type, m.signature, m.description, 
                   c.file_path, c.class_name, c.category
            FROM methods m
            JOIN components c ON m.component_id = c.id
            WHERE m.name LIKE ?
            ORDER BY m.name
        """
        return self.execute_query(query, (f"%{name_pattern}%",))

    def search_methods_by_tag(self, tag: str) -> List[Dict[str, Any]]:
        """Поиск методов по тегу"""
        query = """
            SELECT m.name, m.return_type, m.signature, m.description,
                   c.file_path, c.class_name, c.category
            FROM methods m
            JOIN components c ON m.component_id = c.id
            JOIN method_tags mt ON m.id = mt.method_id
            JOIN tags t ON mt.tag_id = t.id
            WHERE t.name = ?
            ORDER BY m.name
        """
        return self.execute_query(query, (tag,))

    def search_methods_by_category(self, category: str) -> List[Dict[str, Any]]:
        """Поиск методов по категории компонента"""
        query = """
            SELECT m.name, m.return_type, m.signature, m.description,
                   c.file_path, c.class_name, c.category
            FROM methods m
            JOIN components c ON m.component_id = c.id
            WHERE c.category = ?
            ORDER BY m.name
        """
        return self.execute_query(query, (category,))

    def search_methods_by_return_type(self, return_type: str) -> List[Dict[str, Any]]:
        """Поиск методов по типу возврата"""
        query = """
            SELECT m.name, m.return_type, m.signature, m.description,
                   c.file_path, c.class_name, c.category
            FROM methods m
            JOIN components c ON m.component_id = c.id
            WHERE m.return_type LIKE ?
            ORDER BY m.name
        """
        return self.execute_query(query, (f"%{return_type}%",))

    def full_text_search(self, search_term: str) -> List[Dict[str, Any]]:
        """Полнотекстовый поиск по методам"""
        query = """
            SELECT m.name, m.return_type, m.signature, m.description,
                   c.file_path, c.class_name, c.category
            FROM methods m
            JOIN components c ON m.component_id = c.id
            WHERE m.id IN (
                SELECT rowid FROM methods_fts WHERE methods_fts MATCH ?
            )
            ORDER BY m.name
        """
        return self.execute_query(query, (search_term,))

    def get_popular_tags(self, limit: int = 10) -> List[Dict[str, Any]]:
        """Получает популярные теги"""
        query = """
            SELECT name, usage_count, description
            FROM tags
            ORDER BY usage_count DESC
            LIMIT ?
        """
        return self.execute_query(query, (limit,))

    def get_categories_summary(self) -> List[Dict[str, Any]]:
        """Получает сводку по категориям"""
        query = """
            SELECT category, COUNT(*) as components_count,
                   (SELECT COUNT(*) FROM methods m 
                    JOIN components c2 ON m.component_id = c2.id 
                    WHERE c2.category = c.category) as methods_count
            FROM components c
            GROUP BY category
            ORDER BY components_count DESC
        """
        return self.execute_query(query)

    def get_methods_by_component(self, component_name: str) -> List[Dict[str, Any]]:
        """Получает методы конкретного компонента"""
        query = """
            SELECT m.name, m.return_type, m.signature, m.description,
                   m.line_number, m.is_static, m.is_generic
            FROM methods m
            JOIN components c ON m.component_id = c.id
            WHERE c.file_name LIKE ?
            ORDER BY m.line_number
        """
        return self.execute_query(query, (f"%{component_name}%",))

    def get_methods_with_tags(self, tags: List[str]) -> List[Dict[str, Any]]:
        """Получает методы, которые имеют все указанные теги"""
        if not tags:
            return []
        
        placeholders = ','.join(['?' for _ in tags])
        query = f"""
            SELECT m.name, m.return_type, m.signature, m.description,
                   c.file_path, c.class_name, c.category,
                   GROUP_CONCAT(t.name) as tags
            FROM methods m
            JOIN components c ON m.component_id = c.id
            JOIN method_tags mt ON m.id = mt.method_id
            JOIN tags t ON mt.tag_id = t.id
            WHERE t.name IN ({placeholders})
            GROUP BY m.id
            HAVING COUNT(DISTINCT t.name) = ?
            ORDER BY m.name
        """
        return self.execute_query(query, tags + [len(tags)])

    def print_results(self, results: List[Dict[str, Any]], title: str = "Results"):
        """Выводит результаты в красивом формате"""
        if not results:
            print(f"❌ No {title.lower()} found")
            return
        
        print(f"\n📋 {title} ({len(results)} items):")
        print("=" * 80)
        
        for i, result in enumerate(results, 1):
            print(f"\n{i}. {result.get('name', 'N/A')}")
            print(f"   Return Type: {result.get('return_type', 'N/A')}")
            print(f"   File: {result.get('file_path', 'N/A')}")
            print(f"   Class: {result.get('class_name', 'N/A')}")
            print(f"   Category: {result.get('category', 'N/A')}")
            
            if result.get('description'):
                print(f"   Description: {result['description'][:100]}{'...' if len(result['description']) > 100 else ''}")
            
            if result.get('signature'):
                print(f"   Signature: {result['signature']}")
            
            if result.get('tags'):
                print(f"   Tags: {result['tags']}")

def main():
    """Основная функция"""
    parser = argparse.ArgumentParser(description="TestKit SQL Query Tool")
    parser.add_argument("--db", default="testkit_index.db", help="Database file path")
    parser.add_argument("--name", help="Search methods by name pattern")
    parser.add_argument("--tag", help="Search methods by tag")
    parser.add_argument("--category", help="Search methods by category")
    parser.add_argument("--return-type", help="Search methods by return type")
    parser.add_argument("--full-text", help="Full-text search")
    parser.add_argument("--component", help="Get methods from specific component")
    parser.add_argument("--tags", nargs="+", help="Search methods with all specified tags")
    parser.add_argument("--popular-tags", type=int, help="Show popular tags")
    parser.add_argument("--categories", action="store_true", help="Show categories summary")
    parser.add_argument("--query", help="Execute custom SQL query")

    args = parser.parse_args()

    query_tool = TestKitSQLQuery(args.db)
    
    if not query_tool.connect():
        sys.exit(1)

    try:

        
        if args.name:
            results = query_tool.search_methods_by_name(args.name)
            query_tool.print_results(results, f"Methods matching '{args.name}'")
        
        elif args.tag:
            results = query_tool.search_methods_by_tag(args.tag)
            query_tool.print_results(results, f"Methods with tag '{args.tag}'")
        
        elif args.category:
            results = query_tool.search_methods_by_category(args.category)
            query_tool.print_results(results, f"Methods in category '{args.category}'")
        
        elif args.return_type:
            results = query_tool.search_methods_by_return_type(args.return_type)
            query_tool.print_results(results, f"Methods returning '{args.return_type}'")
        
        elif args.full_text:
            results = query_tool.full_text_search(args.full_text)
            query_tool.print_results(results, f"Full-text search for '{args.full_text}'")
        
        elif args.component:
            results = query_tool.get_methods_by_component(args.component)
            query_tool.print_results(results, f"Methods in component '{args.component}'")
        
        elif args.tags:
            results = query_tool.get_methods_with_tags(args.tags)
            query_tool.print_results(results, f"Methods with tags {args.tags}")
        
        elif args.popular_tags is not None:
            results = query_tool.get_popular_tags(args.popular_tags)
            print(f"\n🏷️  Popular Tags (top {args.popular_tags}):")
            print("=" * 50)
            for result in results:
                print(f"{result['name']}: {result['usage_count']} uses")
        
        elif args.categories:
            results = query_tool.get_categories_summary()
            print(f"\n📁 Categories Summary:")
            print("=" * 50)
            for result in results:
                print(f"{result['category']}: {result['components_count']} components, {result['methods_count']} methods")
        
        elif args.query:
            results = query_tool.execute_query(args.query)
            query_tool.print_results(results, "Custom Query Results")
        
        else:
            print("🔍 TestKit SQL Query Tool")
            print("=" * 50)
            print("Available commands:")
            print("  --name 'Create'           # Search methods by name")
            print("  --tag 'mock'              # Search methods by tag")
            print("  --category 'core'         # Search methods by category")
            print("  --return-type 'Message'   # Search methods by return type")
            print("  --full-text 'message'     # Full-text search")
            print("  --component 'Main'        # Get methods from component")
            print("  --tags mock factory        # Search methods with all tags")
            print("  --popular-tags 10         # Show popular tags")
            print("  --categories              # Show categories summary")
            print("  --query 'SELECT * FROM methods LIMIT 5'  # Custom SQL")

    except Exception as e:
        print(f"❌ Error: {e}")
    finally:
        query_tool.close()

if __name__ == "__main__":
    main() 