#!/usr/bin/env python3
"""
Deduplication module for TestKit SQL Indexer
"""

import hashlib
from abc import ABC, abstractmethod
from typing import List, Set, Dict, Tuple
from .models import TestKitComponent, TestKitMethod

class DeduplicationStrategy(ABC):
    """Абстрактный класс для стратегий дедупликации"""
    
    @abstractmethod
    def should_keep(self, component: TestKitComponent, seen: Set) -> bool:
        """Определяет, нужно ли сохранить компонент"""
        pass
    
    @abstractmethod
    def _add_to_seen(self, component: TestKitComponent, seen: Set):
        """Добавляет компонент в множество уже обработанных"""
        pass
    
    @abstractmethod
    def get_component_key(self, component: TestKitComponent) -> str:
        """Получает уникальный ключ для компонента"""
        pass

class FileHashDeduplicator(DeduplicationStrategy):
    """Дедупликация по хешу файла"""
    
    def should_keep(self, component: TestKitComponent, seen: Set) -> bool:
        file_hash = self._calculate_file_hash(component.file_path)
        return file_hash not in seen
    
    def _add_to_seen(self, component: TestKitComponent, seen: Set):
        file_hash = self._calculate_file_hash(component.file_path)
        seen.add(file_hash)
    
    def get_component_key(self, component: TestKitComponent) -> str:
        return self._calculate_file_hash(component.file_path)
    
    def _calculate_file_hash(self, file_path: str) -> str:
        """Вычисляет SHA256 хеш файла"""
        try:
            with open(file_path, 'rb') as f:
                return hashlib.sha256(f.read()).hexdigest()
        except Exception as e:
            print(f"Warning: Could not calculate hash for {file_path}: {e}")
            # Возвращаем путь как ключ в случае ошибки
            return f"path:{file_path}"

class SignatureDeduplicator(DeduplicationStrategy):
    """Дедупликация по сигнатуре класса и методов"""
    
    def should_keep(self, component: TestKitComponent, seen: Set) -> bool:
        signature_hash = self._get_component_signature_hash(component)
        return signature_hash not in seen
    
    def _add_to_seen(self, component: TestKitComponent, seen: Set):
        signature_hash = self._get_component_signature_hash(component)
        seen.add(signature_hash)
    
    def get_component_key(self, component: TestKitComponent) -> str:
        return self._get_component_signature_hash(component)
    
    def _get_component_signature_hash(self, component: TestKitComponent) -> str:
        """Создает хеш сигнатуры компонента для быстрого сравнения"""
        # Создаем компактную сигнатуру
        method_count = len(component.methods)
        method_names = sorted([method.name for method in component.methods])
        class_name = component.class_name or "Unknown"
        
        # Создаем короткую строку для хеширования
        signature_parts = [class_name, str(method_count)]
        signature_parts.extend(method_names[:10])  # Ограничиваем количество имен методов
        
        signature_str = '|'.join(signature_parts)
        return hashlib.sha256(signature_str.encode('utf-8')).hexdigest()[:16]  # Короткий хеш
    
    def _get_component_signature(self, component: TestKitComponent) -> str:
        """Создает полную сигнатуру компонента (для отладки)"""
        method_signatures = []
        for method in component.methods:
            # Создаем сигнатуру метода: имя(параметры) -> возвращаемый_тип
            params_str = ','.join(method.parameters) if method.parameters else ''
            method_sig = f"{method.name}({params_str}) -> {method.return_type}"
            method_signatures.append(method_sig)
        
        # Сортируем сигнатуры для стабильности
        method_signatures.sort()
        
        # Создаем финальную сигнатуру: класс + отсортированные методы
        return f"{component.class_name}:{';'.join(method_signatures)}"

class ContentDeduplicator(DeduplicationStrategy):
    """Дедупликация по содержимому файла (без учета комментариев и форматирования)"""
    
    def __init__(self):
        self._hash_cache = {}  # Кеш для хешей файлов
    
    def should_keep(self, component: TestKitComponent, seen: Set) -> bool:
        content_hash = self._calculate_content_hash(component.file_path)
        return content_hash not in seen
    
    def _add_to_seen(self, component: TestKitComponent, seen: Set):
        content_hash = self._calculate_content_hash(component.file_path)
        seen.add(content_hash)
    
    def get_component_key(self, component: TestKitComponent) -> str:
        return self._calculate_content_hash(component.file_path)
    
    def _calculate_content_hash(self, file_path: str) -> str:
        """Вычисляет хеш содержимого файла без комментариев и лишних пробелов"""
        # Проверяем кеш
        if file_path in self._hash_cache:
            return self._hash_cache[file_path]
        
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
                print(f"Warning: Could not read {file_path}")
                hash_result = f"content_error:{file_path}"
                self._hash_cache[file_path] = hash_result
                return hash_result
        
        # Удаляем комментарии и лишние пробелы
        cleaned_content = self._clean_content(content)
        hash_result = hashlib.sha256(cleaned_content.encode('utf-8')).hexdigest()
        
        # Сохраняем в кеш
        self._hash_cache[file_path] = hash_result
        return hash_result
    
    def _clean_content(self, content: str) -> str:
        """Очищает содержимое от комментариев и лишних пробелов"""
        lines = content.split('\n')
        cleaned_lines = []
        
        for line in lines:
            # Удаляем однострочные комментарии
            if '//' in line:
                line = line.split('//')[0]
            
            # Удаляем лишние пробелы
            line = line.strip()
            
            if line:  # Добавляем только непустые строки
                cleaned_lines.append(line)
        
        return '\n'.join(cleaned_lines)

class DeduplicationEngine:
    """Движок дедупликации с поддержкой различных стратегий"""
    
    def __init__(self, strategy: DeduplicationStrategy):
        self.strategy = strategy
        self.removed_components: List[TestKitComponent] = []
        self.duplicate_groups: Dict[str, List[TestKitComponent]] = {}
    
    def deduplicate(self, components: List[TestKitComponent]) -> List[TestKitComponent]:
        """Выполняет дедупликацию компонентов"""
        if not components:
            return []
        
        # Предварительно вычисляем все ключи для группировки
        print("Computing component keys...")
        component_keys = []
        for component in components:
            key = self.strategy.get_component_key(component)
            component_keys.append((key, component))
        
        # Группируем компоненты по ключу
        print("Grouping components...")
        groups = {}
        for key, component in component_keys:
            if key not in groups:
                groups[key] = []
            groups[key].append(component)
        
        # Обрабатываем каждую группу
        unique_components = []
        removed_count = 0
        
        print("Processing duplicate groups...")
        for key, group in groups.items():
            if len(group) == 1:
                # Уникальный компонент
                unique_components.append(group[0])
            else:
                # Дубликаты - оставляем первый, остальные удаляем
                unique_components.append(group[0])
                self.removed_components.extend(group[1:])
                removed_count += len(group) - 1
                self.duplicate_groups[key] = group
        
        print(f"Deduplication: {len(components)} -> {len(unique_components)} components ({removed_count} removed)")
        
        if self.duplicate_groups:
            print(f"Found {len(self.duplicate_groups)} duplicate groups")
        
        return unique_components
    
    def get_duplicate_report(self) -> Dict:
        """Возвращает отчет о найденных дубликатах"""
        report = {
            'total_duplicate_groups': len(self.duplicate_groups),
            'total_removed_components': len(self.removed_components),
            'duplicate_groups': []
        }
        
        for key, group in self.duplicate_groups.items():
            group_info = {
                'key': key,
                'count': len(group),
                'files': [comp.file_path for comp in group],
                'class_names': [comp.class_name for comp in group]
            }
            report['duplicate_groups'].append(group_info)
        
        return report
    
    def get_removed_components(self) -> List[TestKitComponent]:
        """Возвращает список удаленных компонентов"""
        return self.removed_components.copy()

class DeduplicationAnalyzer:
    """Анализатор дубликатов для предварительного анализа"""
    
    def __init__(self):
        self.analyzers = {
            'file_hash': FileHashDeduplicator(),
            'signature': SignatureDeduplicator(),
            'content': ContentDeduplicator()
        }
    
    def analyze_duplicates(self, components: List[TestKitComponent]) -> Dict:
        """Анализирует дубликаты с использованием всех стратегий"""
        analysis = {}
        
        for strategy_name, strategy in self.analyzers.items():
            engine = DeduplicationEngine(strategy)
            unique_components = engine.deduplicate(components.copy())
            
            analysis[strategy_name] = {
                'original_count': len(components),
                'unique_count': len(unique_components),
                'removed_count': len(components) - len(unique_components),
                'duplicate_report': engine.get_duplicate_report()
            }
        
        return analysis
    
    def print_analysis(self, analysis: Dict):
        """Выводит анализ дубликатов"""
        print("Duplicate Analysis Report")
        print("=" * 50)
        
        for strategy_name, stats in analysis.items():
            print(f"\n{strategy_name.upper()} Strategy:")
            print(f"  Original components: {stats['original_count']}")
            print(f"  Unique components: {stats['unique_count']}")
            print(f"  Removed components: {stats['removed_count']}")
            
            if stats['duplicate_report']['total_duplicate_groups'] > 0:
                print(f"  Duplicate groups: {stats['duplicate_report']['total_duplicate_groups']}")
                for group in stats['duplicate_report']['duplicate_groups'][:3]:  # Показываем первые 3
                    print(f"    - {group['count']} duplicates: {', '.join(group['class_names'])}")
                if len(stats['duplicate_report']['duplicate_groups']) > 3:
                    print(f"    ... and {len(stats['duplicate_report']['duplicate_groups']) - 3} more groups") 