#!/usr/bin/env python3
"""
Data models for TestKit SQL Indexer
"""

from dataclasses import dataclass
from typing import List, Set, Optional, Dict

@dataclass
class TestKitMethod:
    name: str
    return_type: str
    parameters: List[str]
    description: str
    tags: List[str]
    is_static: bool
    is_generic: bool
    signature: str
    line_number: int
    full_signature: str = ""  # Полная сигнатура с параметрами
    usage_examples: List[str] = None  # Примеры использования из тестов

@dataclass
class TestKitComponent:
    file_path: str
    file_name: str
    class_name: str
    class_description: str
    category: str
    methods: List[TestKitMethod]
    lines_count: int
    file_hash: str = ""  # Хеш файла для дедупликации

@dataclass
class TestKitIndex:
    components: List[TestKitComponent]
    all_tags: Set[str]
    generated_at: str

@dataclass
class UsageExample:
    """Пример использования метода"""
    method_name: str
    component_name: str
    example_code: str
    file_path: str
    line_number: int
    context: str  # Контекст использования (тест, интеграция, etc.) 