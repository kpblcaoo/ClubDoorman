#!/usr/bin/env python3
"""
File scanner for TestKit SQL Indexer
"""

import os
import hashlib
from pathlib import Path
from typing import List, Set
from .models import TestKitComponent
from .parser import CSharpParser

class TestKitScanner:
    """Scans TestKit directory for C# files and analyzes them"""
    
    def __init__(self, testkit_path: str):
        self.testkit_path = Path(testkit_path)
        self.parser = CSharpParser()
        self.components: List[TestKitComponent] = []
        self.all_tags: Set[str] = set()
    
    def scan_testkit(self) -> List[TestKitComponent]:
        """Сканирует TestKit директорию и анализирует все C# файлы"""
        if not self.testkit_path.exists():
            raise FileNotFoundError(f"TestKit path not found: {self.testkit_path}")
        
        # Ищем все .cs файлы в TestKit
        cs_files = list(self.testkit_path.rglob("*.cs"))
        
        print(f"Found {len(cs_files)} C# files in {self.testkit_path}")
        
        for file_path in cs_files:
            try:
                component = self.analyze_file(file_path)
                if component:
                    self.components.append(component)
                    # Собираем все теги
                    for method in component.methods:
                        self.all_tags.update(method.tags)
            except Exception as e:
                print(f"Error analyzing {file_path}: {e}")
        
        print(f"Analyzed {len(self.components)} components")
        print(f"Found {len(self.all_tags)} unique tags")
        
        return self.components
    
    def analyze_file(self, file_path: Path) -> TestKitComponent:
        """Анализирует отдельный C# файл"""
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
                return None
        
        # Извлекаем информацию о классе
        class_name = self.parser.extract_class_name(content)
        class_description = self.parser.extract_class_summary(content)
        
        # Извлекаем методы
        methods = self.parser.extract_methods(content)
        
        # Определяем категорию
        relative_path = file_path.relative_to(self.testkit_path)
        category = self.parser.determine_category(str(relative_path))
        
        # Подсчитываем строки
        lines_count = len(content.split('\n'))
        
        # Вычисляем хеш файла
        file_hash = self._calculate_file_hash(file_path)
        
        component = TestKitComponent(
            file_path=str(file_path),
            file_name=file_path.name,
            class_name=class_name,
            class_description=class_description,
            category=category,
            methods=methods,
            lines_count=lines_count,
            file_hash=file_hash
        )
        
        return component
    
    def _calculate_file_hash(self, file_path: Path) -> str:
        """Вычисляет SHA256 хеш файла"""
        try:
            with open(file_path, 'rb') as f:
                return hashlib.sha256(f.read()).hexdigest()
        except Exception as e:
            print(f"Warning: Could not calculate hash for {file_path}: {e}")
            # Возвращаем путь как ключ в случае ошибки
            return f"path:{file_path}"
    
    def get_statistics(self) -> dict:
        """Получает статистику по сканированию"""
        stats = {
            'total_components': len(self.components),
            'total_methods': sum(len(c.methods) for c in self.components),
            'total_tags': len(self.all_tags),
            'categories': {},
            'top_tags': []
        }
        
        # Статистика по категориям
        category_counts = {}
        for component in self.components:
            category = component.category
            category_counts[category] = category_counts.get(category, 0) + 1
        
        stats['categories'] = sorted(category_counts.items(), key=lambda x: x[1], reverse=True)
        
        # Топ тегов
        tag_counts = {}
        for component in self.components:
            for method in component.methods:
                for tag in method.tags:
                    tag_counts[tag] = tag_counts.get(tag, 0) + 1
        
        stats['top_tags'] = sorted(tag_counts.items(), key=lambda x: x[1], reverse=True)[:10]
        
        return stats 