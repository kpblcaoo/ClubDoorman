#!/usr/bin/env python3
"""
Generate usage guidance based on method tags
Генерирует рекомендации по использованию методов на основе их тегов
"""

import sys
import os
from pathlib import Path
from typing import Dict, List, Tuple

# Добавляем текущую директорию в путь
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from testkit_query import TestKitQueryEngine

class UsageGuidanceGenerator:
    """Генерирует рекомендации по использованию методов"""
    
    def __init__(self, db_path: str = "testkit_index.db"):
        self.db_path = db_path
        self.query_engine = TestKitQueryEngine(db_path)
        
        # Шаблоны использования по тегам
        self.usage_patterns = {
            'factory': {
                'description': 'Factory method for creating test objects',
                'usage': 'Use in test setup to create test data',
                'example': 'var user = TK.CreateRealisticUser();',
                'context': 'setup, arrange'
            },
            'builder': {
                'description': 'Builder pattern for complex object construction',
                'usage': 'Use to build complex test objects step by step',
                'example': 'var message = TK.CreateMessage().WithText("test").Build();',
                'context': 'setup, arrange'
            },
            'mock': {
                'description': 'Mock object for testing',
                'usage': 'Use to create mock dependencies',
                'example': 'var mockService = TK.CreateMockModerationService();',
                'context': 'setup, arrange'
            },
            'fake': {
                'description': 'Fake object with realistic behavior',
                'usage': 'Use to create objects with realistic but controlled behavior',
                'example': 'var fakeUser = TK.CreateRealisticUser();',
                'context': 'setup, arrange'
            },
            'message': {
                'description': 'Message-related test data',
                'usage': 'Use to create Telegram messages for testing',
                'example': 'var message = TK.CreateSpamMessage();',
                'context': 'setup, arrange'
            },
            'user': {
                'description': 'User-related test data',
                'usage': 'Use to create user objects for testing',
                'example': 'var user = TK.CreateRealisticUser();',
                'context': 'setup, arrange'
            },
            'chat': {
                'description': 'Chat-related test data',
                'usage': 'Use to create chat objects for testing',
                'example': 'var chat = TK.CreateRealisticGroup();',
                'context': 'setup, arrange'
            },
            'moderation': {
                'description': 'Moderation-related test data',
                'usage': 'Use to create moderation service and related objects',
                'example': 'var service = TK.CreateModerationService();',
                'context': 'setup, arrange'
            },
            'realistic': {
                'description': 'Realistic test data',
                'usage': 'Use to create realistic test scenarios',
                'example': 'var realisticUser = TK.CreateRealisticUser();',
                'context': 'integration, e2e'
            },
            'collection': {
                'description': 'Collection of test objects',
                'usage': 'Use to create multiple test objects',
                'example': 'var users = TK.CreateManyUsers(5);',
                'context': 'setup, arrange'
            }
        }
    
    def generate_method_guidance(self, method_name: str) -> Dict:
        """Генерирует рекомендации для конкретного метода"""
        # Ищем метод
        results = self.query_engine.search_methods_by_name(method_name, exact_match=True)
        if not results:
            return {"error": f"Method '{method_name}' not found"}
        
        method = results[0]
        guidance = {
            'method_name': method.method_name,
            'component_name': method.component_name,
            'return_type': method.return_type,
            'description': method.description,
            'tags': method.tags,
            'usage_patterns': [],
            'recommendations': [],
            'related_methods': []
        }
        
        # Анализируем теги и создаем рекомендации
        for tag in method.tags:
            if tag in self.usage_patterns:
                pattern = self.usage_patterns[tag]
                guidance['usage_patterns'].append({
                    'tag': tag,
                    'description': pattern['description'],
                    'usage': pattern['usage'],
                    'example': pattern['example'],
                    'context': pattern['context']
                })
        
        # Генерируем рекомендации
        if 'factory' in method.tags:
            guidance['recommendations'].append("Use in test setup to create test data")
        if 'builder' in method.tags:
            guidance['recommendations'].append("Use builder pattern for complex object construction")
        if 'mock' in method.tags:
            guidance['recommendations'].append("Use to create mock dependencies for unit testing")
        if 'realistic' in method.tags:
            guidance['recommendations'].append("Use for integration tests with realistic data")
        if 'collection' in method.tags:
            guidance['recommendations'].append("Use to create multiple test objects at once")
        
        # Ищем похожие методы
        similar_methods = self.query_engine.search_similar_methods(method_name, limit=5)
        guidance['related_methods'] = [
            {
                'name': m.method_name,
                'component': m.component_name,
                'tags': m.tags
            }
            for m in similar_methods
        ]
        
        return guidance
    
    def generate_context_guidance(self, context: str) -> Dict:
        """Генерирует рекомендации для конкретного контекста"""
        # Ищем методы, подходящие для данного контекста
        if context == 'setup':
            # Методы для setup
            results = self.query_engine.search_methods_by_tag('factory')
            results.extend(self.query_engine.search_methods_by_tag('builder'))
            results.extend(self.query_engine.search_methods_by_tag('mock'))
        elif context == 'integration':
            # Методы для интеграционных тестов
            results = self.query_engine.search_methods_by_tag('realistic')
            results.extend(self.query_engine.search_methods_by_tag('message'))
            results.extend(self.query_engine.search_methods_by_tag('user'))
        elif context == 'unit':
            # Методы для unit тестов
            results = self.query_engine.search_methods_by_tag('mock')
            results.extend(self.query_engine.search_methods_by_tag('fake'))
        else:
            results = []
        
        # Убираем дубликаты
        seen = set()
        unique_results = []
        for result in results:
            key = (result.component_name, result.method_name)
            if key not in seen:
                seen.add(key)
                unique_results.append(result)
        
        return {
            'context': context,
            'methods': [
                {
                    'name': m.method_name,
                    'component': m.component_name,
                    'tags': m.tags,
                    'description': m.description
                }
                for m in unique_results[:10]  # Ограничиваем до 10 методов
            ]
        }
    
    def generate_tag_guidance(self, tag: str) -> Dict:
        """Генерирует рекомендации для конкретного тега"""
        results = self.query_engine.search_methods_by_tag(tag)
        
        if tag in self.usage_patterns:
            pattern = self.usage_patterns[tag]
            guidance = {
                'tag': tag,
                'description': pattern['description'],
                'usage': pattern['usage'],
                'example': pattern['example'],
                'context': pattern['context'],
                'methods': [
                    {
                        'name': m.method_name,
                        'component': m.component_name,
                        'description': m.description
                    }
                    for m in results[:10]  # Ограничиваем до 10 методов
                ]
            }
        else:
            guidance = {
                'tag': tag,
                'description': f'Methods tagged with "{tag}"',
                'usage': 'Use as needed for your test scenario',
                'methods': [
                    {
                        'name': m.method_name,
                        'component': m.component_name,
                        'description': m.description
                    }
                    for m in results[:10]
                ]
            }
        
        return guidance

def main():
    """Основная функция"""
    if len(sys.argv) < 2:
        print("Usage: python3 generate_usage_guidance.py <command> [args...]")
        print("Commands:")
        print("  method <method_name> - Get guidance for specific method")
        print("  context <context> - Get guidance for context (setup, integration, unit)")
        print("  tag <tag_name> - Get guidance for tag")
        sys.exit(1)
    
    command = sys.argv[1]
    generator = UsageGuidanceGenerator()
    
    if command == 'method' and len(sys.argv) >= 3:
        method_name = sys.argv[2]
        guidance = generator.generate_method_guidance(method_name)
        
        if 'error' in guidance:
            print(f"Error: {guidance['error']}")
            return
        
        print(f"# Guidance for {guidance['method_name']}")
        print(f"**Component:** {guidance['component_name']}")
        print(f"**Return Type:** {guidance['return_type']}")
        print(f"**Tags:** {', '.join(guidance['tags'])}")
        print()
        
        if guidance['description']:
            print(f"**Description:** {guidance['description']}")
            print()
        
        if guidance['usage_patterns']:
            print("## Usage Patterns")
            for pattern in guidance['usage_patterns']:
                print(f"### {pattern['tag'].title()}")
                print(f"**Description:** {pattern['description']}")
                print(f"**Usage:** {pattern['usage']}")
                print(f"**Example:** `{pattern['example']}`")
                print(f"**Context:** {pattern['context']}")
                print()
        
        if guidance['recommendations']:
            print("## Recommendations")
            for rec in guidance['recommendations']:
                print(f"- {rec}")
            print()
        
        if guidance['related_methods']:
            print("## Related Methods")
            for method in guidance['related_methods']:
                print(f"- **{method['component']}.{method['name']}** ({', '.join(method['tags'])})")
    
    elif command == 'context' and len(sys.argv) >= 3:
        context = sys.argv[2]
        guidance = generator.generate_context_guidance(context)
        
        print(f"# Methods for {context} context")
        print(f"**Context:** {guidance['context']}")
        print()
        
        for method in guidance['methods']:
            print(f"## {method['component']}.{method['name']}")
            print(f"**Tags:** {', '.join(method['tags'])}")
            if method['description']:
                print(f"**Description:** {method['description']}")
            print()
    
    elif command == 'tag' and len(sys.argv) >= 3:
        tag = sys.argv[2]
        guidance = generator.generate_tag_guidance(tag)
        
        print(f"# Methods with tag '{tag}'")
        print(f"**Description:** {guidance['description']}")
        print(f"**Usage:** {guidance['usage']}")
        print(f"**Example:** `{guidance['example']}`")
        print(f"**Context:** {guidance['context']}")
        print()
        
        for method in guidance['methods']:
            print(f"## {method['component']}.{method['name']}")
            if method['description']:
                print(f"**Description:** {method['description']}")
            print()
    
    else:
        print("Invalid command or missing arguments")
        sys.exit(1)

if __name__ == "__main__":
    main() 