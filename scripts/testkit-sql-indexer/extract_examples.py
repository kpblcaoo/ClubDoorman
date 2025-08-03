#!/usr/bin/env python3
"""
Extract usage examples from test files
Извлекает примеры использования TestKit методов из тестов
"""

import os
import sys
from pathlib import Path
from typing import List, Dict
from testkit_indexer.parser import CSharpParser
from testkit_indexer.models import UsageExample
from testkit_indexer.database import DatabaseManager

class ExampleExtractor:
    """Извлекает примеры использования из тестов"""
    
    def __init__(self, testkit_path: str, tests_path: str, db_path: str = "testkit_index.db"):
        self.testkit_path = Path(testkit_path)
        self.tests_path = Path(tests_path)
        self.db_path = db_path
        self.parser = CSharpParser()
        self.db_manager = DatabaseManager(db_path)
    
    def extract_all_examples(self) -> List[UsageExample]:
        """Извлекает все примеры использования из тестов"""
        examples = []
        
        # Получаем список всех методов TestKit
        testkit_methods = self._get_testkit_methods()
        
        # Сканируем тестовые файлы
        test_files = list(self.tests_path.rglob("*.cs"))
        print(f"Scanning {len(test_files)} test files for usage examples...")
        
        for test_file in test_files:
            try:
                file_examples = self._extract_examples_from_file(test_file, testkit_methods)
                examples.extend(file_examples)
            except Exception as e:
                print(f"Error processing {test_file}: {e}")
        
        print(f"Extracted {len(examples)} usage examples")
        return examples
    
    def _get_testkit_methods(self) -> List[str]:
        """Получает список всех методов TestKit"""
        methods = []
        
        # Сканируем TestKit файлы
        testkit_files = list(self.testkit_path.rglob("*.cs"))
        
        for file_path in testkit_files:
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Извлекаем методы
                file_methods = self.parser.extract_methods(content)
                for method in file_methods:
                    methods.append(method.name)
                    
            except Exception as e:
                print(f"Error reading {file_path}: {e}")
        
        return list(set(methods))  # Убираем дубликаты
    
    def _extract_examples_from_file(self, file_path: Path, target_methods: List[str]) -> List[UsageExample]:
        """Извлекает примеры использования из одного файла"""
        examples = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except UnicodeDecodeError:
            # Пробуем другие кодировки
            for encoding in ['latin-1', 'cp1252']:
                try:
                    with open(file_path, 'r', encoding=encoding) as f:
                        content = f.read()
                    break
                except UnicodeDecodeError:
                    continue
            else:
                print(f"Could not read {file_path} with any encoding")
                return examples
        
        # Определяем контекст файла
        context = self._determine_context(file_path)
        
        # Извлекаем примеры использования
        usage_examples = self.parser.extract_usage_examples(content, target_methods)
        
        for method_name, method_examples in usage_examples.items():
            for example_code in method_examples:
                # Извлекаем номер строки из примера
                lines = example_code.split('\n')
                line_number = 0
                if lines and lines[0].startswith('// Line '):
                    try:
                        line_number = int(lines[0].replace('// Line ', ''))
                    except ValueError:
                        pass
                
                example = UsageExample(
                    method_name=method_name,
                    component_name="",  # Будет заполнено позже
                    example_code=example_code,
                    file_path=str(file_path),
                    line_number=line_number,
                    context=context
                )
                examples.append(example)
        
        return examples
    
    def _determine_context(self, file_path: Path) -> str:
        """Определяет контекст файла (test, integration, etc.)"""
        relative_path = file_path.relative_to(self.tests_path)
        path_str = str(relative_path).lower()
        
        if 'integration' in path_str:
            return 'integration'
        elif 'unit' in path_str:
            return 'unit'
        elif 'demo' in path_str:
            return 'demo'
        elif 'test' in path_str:
            return 'test'
        else:
            return 'other'
    
    def save_examples(self, examples: List[UsageExample]):
        """Сохраняет примеры в базу данных"""
        print(f"Saving {len(examples)} examples to database...")
        self.db_manager.save_usage_examples(examples)
        print("Examples saved successfully!")

def main():
    """Основная функция"""
    if len(sys.argv) < 3:
        print("Usage: python extract_examples.py <testkit_path> <tests_path> [db_path]")
        sys.exit(1)
    
    testkit_path = sys.argv[1]
    tests_path = sys.argv[2]
    db_path = sys.argv[3] if len(sys.argv) > 3 else "testkit_index.db"
    
    if not os.path.exists(testkit_path):
        print(f"TestKit path not found: {testkit_path}")
        sys.exit(1)
    
    if not os.path.exists(tests_path):
        print(f"Tests path not found: {tests_path}")
        sys.exit(1)
    
    extractor = ExampleExtractor(testkit_path, tests_path, db_path)
    examples = extractor.extract_all_examples()
    
    if examples:
        extractor.save_examples(examples)
        print(f"Successfully extracted and saved {len(examples)} usage examples")
    else:
        print("No usage examples found")

if __name__ == "__main__":
    main() 