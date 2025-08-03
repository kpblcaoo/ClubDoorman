#!/usr/bin/env python3
"""
Database operations for TestKit SQL Indexer
"""

import sqlite3
from datetime import datetime
from typing import List, Optional
from .models import TestKitComponent, TestKitMethod, UsageExample

class DatabaseManager:
    """Manages SQLite database operations for TestKit index"""
    
    def __init__(self, db_path: str = "testkit_index.db"):
        self.db_path = db_path
        self.conn: Optional[sqlite3.Connection] = None
    
    def create_database(self):
        """Создает схему базы данных"""
        self.conn = sqlite3.connect(self.db_path)
        cursor = self.conn.cursor()

        # Компоненты (файлы/классы)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS components (
                id INTEGER PRIMARY KEY,
                file_path TEXT NOT NULL,
                file_name TEXT NOT NULL,
                class_name TEXT,
                class_description TEXT,
                category TEXT,
                lines_count INTEGER,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """)

        # Методы
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS methods (
                id INTEGER PRIMARY KEY,
                component_id INTEGER,
                name TEXT NOT NULL,
                return_type TEXT,
                signature TEXT,
                full_signature TEXT,  -- Полная сигнатура с параметрами
                description TEXT,
                line_number INTEGER,
                is_static BOOLEAN,
                is_generic BOOLEAN,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (component_id) REFERENCES components(id)
            )
        """)

        # Теги
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS tags (
                id INTEGER PRIMARY KEY,
                name TEXT UNIQUE NOT NULL,
                category TEXT,
                description TEXT,
                usage_count INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """)

        # Связь методы-теги
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS method_tags (
                method_id INTEGER,
                tag_id INTEGER,
                PRIMARY KEY (method_id, tag_id),
                FOREIGN KEY (method_id) REFERENCES methods(id),
                FOREIGN KEY (tag_id) REFERENCES tags(id)
            )
        """)

        # Примеры использования
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS usage_examples (
                id INTEGER PRIMARY KEY,
                method_id INTEGER,
                component_name TEXT,
                example_code TEXT NOT NULL,
                file_path TEXT,
                line_number INTEGER,
                context TEXT,  -- Контекст использования (test, integration, etc.)
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (method_id) REFERENCES methods(id)
            )
        """)

        # Индексы для быстрого поиска
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_methods_name ON methods(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_methods_return_type ON methods(return_type)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_components_category ON components(category)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_tags_name ON tags(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_usage_examples_method ON usage_examples(method_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_usage_examples_context ON usage_examples(context)")

        self.conn.commit()
    
    def save_components(self, components: List[TestKitComponent]):
        """Сохраняет компоненты в базу данных"""
        if not self.conn:
            raise RuntimeError("Database not initialized. Call create_database() first.")
        
        cursor = self.conn.cursor()
        
        for component in components:
            # Сохраняем компонент
            cursor.execute("""
                INSERT INTO components (file_path, file_name, class_name, class_description, category, lines_count)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (
                component.file_path,
                component.file_name,
                component.class_name,
                component.class_description,
                component.category,
                component.lines_count
            ))
            
            component_id = cursor.lastrowid
            
            # Сохраняем методы компонента
            for method in component.methods:
                cursor.execute("""
                    INSERT INTO methods (component_id, name, return_type, signature, full_signature, description, line_number, is_static, is_generic)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    component_id,
                    method.name,
                    method.return_type,
                    method.signature,
                    method.full_signature or method.signature,  # Используем полную сигнатуру если есть
                    method.description,
                    method.line_number,
                    method.is_static,
                    method.is_generic
                ))
                
                method_id = cursor.lastrowid
                
                # Сохраняем теги метода
                for tag in method.tags:
                    # Получаем или создаем тег
                    cursor.execute("""
                        INSERT OR IGNORE INTO tags (name) VALUES (?)
                    """, (tag,))
                    
                    cursor.execute("SELECT id FROM tags WHERE name = ?", (tag,))
                    tag_id = cursor.fetchone()[0]
                    
                    # Связываем метод с тегом
                    cursor.execute("""
                        INSERT OR IGNORE INTO method_tags (method_id, tag_id) VALUES (?, ?)
                    """, (method_id, tag_id))
                    
                    # Обновляем счетчик использования тега
                    cursor.execute("""
                        UPDATE tags SET usage_count = usage_count + 1 WHERE id = ?
                    """, (tag_id,))
        
        self.conn.commit()

    def save_usage_examples(self, examples: List[UsageExample]):
        """Сохраняет примеры использования методов"""
        if not self.conn:
            raise RuntimeError("Database not initialized. Call create_database() first.")
        
        cursor = self.conn.cursor()
        
        for example in examples:
            # Находим method_id по имени метода
            cursor.execute("""
                SELECT m.id FROM methods m
                JOIN components c ON m.component_id = c.id
                WHERE m.name = ? AND c.class_name = ?
            """, (example.method_name, example.component_name))
            
            result = cursor.fetchone()
            if result:
                method_id = result[0]
                
                cursor.execute("""
                    INSERT INTO usage_examples (method_id, component_name, example_code, file_path, line_number, context)
                    VALUES (?, ?, ?, ?, ?, ?)
                """, (
                    method_id,
                    example.component_name,
                    example.example_code,
                    example.file_path,
                    example.line_number,
                    example.context
                ))
        
        self.conn.commit()

    def get_usage_examples(self, method_name: str, limit: int = 5) -> List[dict]:
        """Получает примеры использования метода"""
        if not self.conn:
            raise RuntimeError("Database not initialized. Call create_database() first.")
        
        cursor = self.conn.cursor()
        
        cursor.execute("""
            SELECT ue.example_code, ue.file_path, ue.line_number, ue.context, c.class_name
            FROM usage_examples ue
            JOIN methods m ON ue.method_id = m.id
            JOIN components c ON m.component_id = c.id
            WHERE m.name = ?
            ORDER BY ue.line_number
            LIMIT ?
        """, (method_name, limit))
        
        results = []
        for row in cursor.fetchall():
            results.append({
                'example_code': row[0],
                'file_path': row[1],
                'line_number': row[2],
                'context': row[3],
                'class_name': row[4]
            })
        
        return results

    def get_method_signature(self, method_name: str, component_name: str = None) -> str:
        """Получает полную сигнатуру метода"""
        if not self.conn:
            raise RuntimeError("Database not initialized. Call create_database() first.")
        
        cursor = self.conn.cursor()
        
        if component_name:
            cursor.execute("""
                SELECT m.full_signature, m.signature
                FROM methods m
                JOIN components c ON m.component_id = c.id
                WHERE m.name = ? AND c.class_name = ?
            """, (method_name, component_name))
        else:
            cursor.execute("""
                SELECT full_signature, signature
                FROM methods
                WHERE name = ?
                LIMIT 1
            """, (method_name,))
        
        result = cursor.fetchone()
        if result:
            return result[0] or result[1]  # Возвращаем полную сигнатуру или обычную
        
        return ""
    
    def get_statistics(self) -> dict:
        """Получает статистику по базе данных"""
        if not self.conn:
            raise RuntimeError("Database not initialized. Call create_database() first.")
        
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
    
    def close(self):
        """Закрывает соединение с базой данных"""
        if self.conn:
            self.conn.close()
            self.conn = None 