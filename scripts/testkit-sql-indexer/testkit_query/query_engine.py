#!/usr/bin/env python3
"""
Query engine for TestKit SQL Indexer
"""

import sqlite3
from typing import List, Dict, Optional, Tuple
from dataclasses import dataclass

@dataclass
class QueryResult:
    """Result of a database query"""
    method_name: str
    return_type: str
    component_name: str
    file_path: str
    description: str
    tags: List[str]
    line_number: int
    is_static: bool
    is_generic: bool

class TestKitQueryEngine:
    """Engine for querying TestKit database"""
    
    def __init__(self, db_path: str = "testkit_index.db"):
        self.db_path = db_path
        self.conn: Optional[sqlite3.Connection] = None
    
    def connect(self):
        """Подключается к базе данных"""
        self.conn = sqlite3.connect(self.db_path)
        self.conn.row_factory = sqlite3.Row
    
    def close(self):
        """Закрывает соединение с базой данных"""
        if self.conn:
            self.conn.close()
            self.conn = None
    
    def search_methods_by_name(self, method_name: str, exact_match: bool = False) -> List[QueryResult]:
        """Поиск методов по имени"""
        if not self.conn:
            self.connect()
        
        if exact_match:
            query = """
                SELECT m.name, m.return_type, c.class_name, c.file_path, 
                       m.description, m.line_number, m.is_static, m.is_generic, m.component_id
                FROM methods m
                JOIN components c ON m.component_id = c.id
                WHERE m.name = ?
                ORDER BY c.class_name, m.name
            """
            params = (method_name,)
        else:
            query = """
                SELECT m.name, m.return_type, c.class_name, c.file_path, 
                       m.description, m.line_number, m.is_static, m.is_generic, m.component_id
                FROM methods m
                JOIN components c ON m.component_id = c.id
                WHERE m.name LIKE ?
                ORDER BY c.class_name, m.name
            """
            params = (f"%{method_name}%",)
        
        cursor = self.conn.cursor()
        cursor.execute(query, params)
        
        results = []
        for row in cursor.fetchall():
            # Получаем теги для метода
            tags = self._get_method_tags(row['name'], row['component_id'])
            
            result = QueryResult(
                method_name=row['name'],
                return_type=row['return_type'],
                component_name=row['class_name'],
                file_path=row['file_path'],
                description=row['description'] or "",
                tags=tags,
                line_number=row['line_number'],
                is_static=bool(row['is_static']),
                is_generic=bool(row['is_generic'])
            )
            results.append(result)
        
        return results
    
    def search_methods_by_tag(self, tag: str) -> List[QueryResult]:
        """Поиск методов по тегу"""
        if not self.conn:
            self.connect()
        
        query = """
            SELECT m.name, m.return_type, c.class_name, c.file_path, 
                   m.description, m.line_number, m.is_static, m.is_generic, m.component_id
            FROM methods m
            JOIN components c ON m.component_id = c.id
            JOIN method_tags mt ON m.id = mt.method_id
            JOIN tags t ON mt.tag_id = t.id
            WHERE t.name = ?
            ORDER BY c.class_name, m.name
        """
        
        cursor = self.conn.cursor()
        cursor.execute(query, (tag,))
        
        results = []
        for row in cursor.fetchall():
            tags = self._get_method_tags(row['name'], row['component_id'])
            
            result = QueryResult(
                method_name=row['name'],
                return_type=row['return_type'],
                component_name=row['class_name'],
                file_path=row['file_path'],
                description=row['description'] or "",
                tags=tags,
                line_number=row['line_number'],
                is_static=bool(row['is_static']),
                is_generic=bool(row['is_generic'])
            )
            results.append(result)
        
        return results
    
    def search_methods_by_return_type(self, return_type: str) -> List[QueryResult]:
        """Поиск методов по типу возвращаемого значения"""
        if not self.conn:
            self.connect()
        
        query = """
            SELECT m.name, m.return_type, c.class_name, c.file_path, 
                   m.description, m.line_number, m.is_static, m.is_generic, m.component_id
            FROM methods m
            JOIN components c ON m.component_id = c.id
            WHERE m.return_type LIKE ?
            ORDER BY c.class_name, m.name
        """
        
        cursor = self.conn.cursor()
        cursor.execute(query, (f"%{return_type}%",))
        
        results = []
        for row in cursor.fetchall():
            tags = self._get_method_tags(row['name'], row['component_id'])
            
            result = QueryResult(
                method_name=row['name'],
                return_type=row['return_type'],
                component_name=row['class_name'],
                file_path=row['file_path'],
                description=row['description'] or "",
                tags=tags,
                line_number=row['line_number'],
                is_static=bool(row['is_static']),
                is_generic=bool(row['is_generic'])
            )
            results.append(result)
        
        return results
    
    def search_methods_by_component(self, component_name: str) -> List[QueryResult]:
        """Поиск методов по компоненту"""
        if not self.conn:
            self.connect()
        
        query = """
            SELECT m.name, m.return_type, c.class_name, c.file_path, 
                   m.description, m.line_number, m.is_static, m.is_generic, m.component_id
            FROM methods m
            JOIN components c ON m.component_id = c.id
            WHERE c.class_name LIKE ?
            ORDER BY m.name
        """
        
        cursor = self.conn.cursor()
        cursor.execute(query, (f"%{component_name}%",))
        
        results = []
        for row in cursor.fetchall():
            tags = self._get_method_tags(row['name'], row['component_id'])
            
            result = QueryResult(
                method_name=row['name'],
                return_type=row['return_type'],
                component_name=row['class_name'],
                file_path=row['file_path'],
                description=row['description'] or "",
                tags=tags,
                line_number=row['line_number'],
                is_static=bool(row['is_static']),
                is_generic=bool(row['is_generic'])
            )
            results.append(result)
        
        return results
    
    def get_all_tags(self) -> List[Tuple[str, int]]:
        """Получает все теги с количеством использований"""
        if not self.conn:
            self.connect()
        
        query = """
            SELECT name, usage_count 
            FROM tags 
            ORDER BY usage_count DESC, name
        """
        
        cursor = self.conn.cursor()
        cursor.execute(query)
        
        return cursor.fetchall()
    
    def get_statistics(self) -> Dict:
        """Получает статистику базы данных"""
        if not self.conn:
            self.connect()
        
        cursor = self.conn.cursor()
        
        stats = {}
        
        # Общее количество компонентов
        cursor.execute("SELECT COUNT(*) FROM components")
        stats['total_components'] = cursor.fetchone()[0]
        
        # Общее количество методов
        cursor.execute("SELECT COUNT(*) FROM methods")
        stats['total_methods'] = cursor.fetchone()[0]
        
        # Общее количество тегов
        cursor.execute("SELECT COUNT(*) FROM tags")
        stats['total_tags'] = cursor.fetchone()[0]
        
        # Топ тегов по использованию
        cursor.execute("""
            SELECT name, usage_count FROM tags 
            ORDER BY usage_count DESC 
            LIMIT 10
        """)
        stats['top_tags'] = cursor.fetchall()
        
        # Распределение по категориям
        cursor.execute("""
            SELECT category, COUNT(*) FROM components 
            GROUP BY category 
            ORDER BY COUNT(*) DESC
        """)
        stats['categories'] = cursor.fetchall()
        
        return stats
    
    def _get_method_tags(self, method_name: str, component_id: int) -> List[str]:
        """Получает теги для конкретного метода"""
        if not self.conn:
            return []
        
        query = """
            SELECT t.name
            FROM tags t
            JOIN method_tags mt ON t.id = mt.tag_id
            JOIN methods m ON mt.method_id = m.id
            WHERE m.name = ? AND m.component_id = ?
            ORDER BY t.name
        """
        
        cursor = self.conn.cursor()
        cursor.execute(query, (method_name, component_id))
        
        return [row[0] for row in cursor.fetchall()] 