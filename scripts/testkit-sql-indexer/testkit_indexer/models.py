#!/usr/bin/env python3
"""
Data models for TestKit SQL Indexer
"""

from dataclasses import dataclass
from typing import List, Set, Optional

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

@dataclass
class TestKitComponent:
    file_path: str
    file_name: str
    class_name: str
    class_description: str
    category: str
    methods: List[TestKitMethod]
    lines_count: int

@dataclass
class TestKitIndex:
    components: List[TestKitComponent]
    all_tags: Set[str]
    generated_at: str 