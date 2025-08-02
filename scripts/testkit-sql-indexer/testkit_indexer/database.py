#!/usr/bin/env python3
"""
Database operations for TestKit SQL Indexer
"""

import sqlite3
from datetime import datetime
from typing import List, Optional
from .models import TestKitComponent, TestKitMethod

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

        # Индексы для быстрого поиска
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_methods_name ON methods(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_methods_return_type ON methods(return_type)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_components_category ON components(category)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_tags_name ON tags(name)")

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
                    INSERT INTO methods (component_id, name, return_type, signature, description, line_number, is_static, is_generic)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    component_id,
                    method.name,
                    method.return_type,
                    method.signature,
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