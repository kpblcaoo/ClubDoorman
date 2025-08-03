#!/usr/bin/env python3
"""
TestKit SQL Index Generator - Генерация SQLite базы данных для индексации TestKit
Анализирует все файлы, методы, комментарии и создает структурированную БД для быстрого поиска
"""

import os
import re
import sqlite3
from datetime import datetime
from pathlib import Path
from collections import defaultdict
from dataclasses import dataclass, asdict
from typing import List, Dict, Set, Optional

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

class TestKitSQLIndexer:
    def __init__(self, testkit_path: str, db_path: str = "testkit_index.db"):
        self.testkit_path = Path(testkit_path)
        self.db_path = db_path
        self.components: List[TestKitComponent] = []
        self.all_tags: Set[str] = set()
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

        # FTS5 для полнотекстового поиска
        cursor.execute("""
            CREATE VIRTUAL TABLE IF NOT EXISTS methods_fts USING fts5(
                name, description, signature, 
                content='methods', content_rowid='id'
            )
        """)

        # Индексы для быстрого поиска
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_methods_name ON methods(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_methods_return_type ON methods(return_type)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_tags_name ON tags(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_tags_category ON tags(category)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_components_category ON components(category)")

        self.conn.commit()

    def scan_testkit(self):
        """Сканирует все C# файлы в TestKit"""
        if not self.testkit_path.exists():
            print(f"❌ TestKit path not found: {self.testkit_path}")
            return

        cs_files = list(self.testkit_path.rglob("*.cs"))
        cs_files = [f for f in cs_files if not f.name.endswith(".Generated.cs")]
        
        print(f"📁 Found {len(cs_files)} C# files to analyze")

        for file_path in cs_files:
            self.analyze_file(file_path)

    def analyze_file(self, file_path: Path):
        """Анализирует один C# файл"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')

            relative_path = file_path.relative_to(self.testkit_path)
            
            component = TestKitComponent(
                file_path=str(relative_path),
                file_name=file_path.name,
                class_name=self.extract_class_name(content),
                class_description=self.extract_class_summary(content),
                category=self.determine_category(str(relative_path)),
                methods=[],
                lines_count=len(lines)
            )

            # Анализируем методы
            methods = self.extract_methods(content)
            for method in methods:
                self.all_tags.update(method.tags)
                component.methods.append(method)

            if component.methods or component.class_description:
                self.components.append(component)
                
        except Exception as e:
            print(f"⚠️  Error analyzing {file_path}: {e}")

    def extract_class_name(self, content: str) -> str:
        """Извлекает имя класса"""
        match = re.search(r'public\s+(static\s+)?class\s+(\w+)', content)
        return match.group(2) if match else ""

    def extract_class_summary(self, content: str) -> str:
        """Извлекает summary комментарий класса"""
        # Ищем /// <summary> перед объявлением класса
        pattern = r'/// <summary>\s*\n(.*?)\n.*?/// </summary>\s*\n.*?public\s+(static\s+)?class'
        match = re.search(pattern, content, re.DOTALL)
        if match:
            return re.sub(r'///\s*', '', match.group(1)).strip().replace('\n', ' ')
        return ""

    def extract_methods(self, content: str) -> List[TestKitMethod]:
        """Извлекает все методы из файла"""
        methods = []
        lines = content.split('\n')
        
        # Паттерн для поиска методов
        method_pattern = r'public\s+(static\s+)?(\w+(?:<[^>]+>)?)\s+(\w+)\s*\([^)]*\)'
        
        for i, line in enumerate(lines):
            match = re.search(method_pattern, line)
            if match:
                return_type = match.group(2)
                method_name = match.group(3)
                
                # Извлекаем параметры
                params_match = re.search(r'\(([^)]*)\)', line)
                parameters = []
                if params_match:
                    params_str = params_match.group(1)
                    if params_str.strip():
                        parameters = [p.strip() for p in params_str.split(',')]

                # Определяем статичность и generic
                is_static = 'static' in line
                is_generic = '<' in return_type or '<' in method_name
                
                # Извлекаем описание
                description = self.extract_method_summary(lines, i)
                
                # Определяем теги
                tags = self.determine_tags(method_name, return_type, description)
                
                # Создаем сигнатуру
                signature = line.strip()
                
                method = TestKitMethod(
                    name=method_name,
                    return_type=return_type,
                    parameters=parameters,
                    description=description,
                    tags=tags,
                    is_static=is_static,
                    is_generic=is_generic,
                    signature=signature,
                    line_number=i + 1
                )
                methods.append(method)
        
        return methods

    def extract_method_summary(self, lines: List[str], method_line: int) -> str:
        """Извлекает описание метода из комментариев"""
        description = ""
        
        # Ищем комментарии перед методом
        for i in range(method_line - 1, max(0, method_line - 10), -1):
            line = lines[i].strip()
            if line.startswith("///"):
                if "<summary>" in line:
                    # Начало summary
                    summary_lines = []
                    for j in range(i, len(lines)):
                        summary_line = lines[j].strip()
                        if "</summary>" in summary_line:
                            break
                        if summary_line.startswith("///"):
                            summary_lines.append(summary_line.replace("///", "").strip())
                    description = " ".join(summary_lines)
                    break
            elif line.startswith("//") or line.startswith("/*"):
                # Обычный комментарий
                comment = line.replace("//", "").replace("/*", "").replace("*/", "").strip()
                if comment:
                    description = comment
                    break
            elif line.strip() == "":
                continue
            else:
                break
        
        return description

    def determine_tags(self, method_name: str, return_type: str, description: str = "") -> List[str]:
        """Определяет теги на основе имени метода, типа возврата и описания"""
        tags = []
        
        # Анализируем имя метода
        name_lower = method_name.lower()
        
        # Базовые теги по имени
        if 'create' in name_lower:
            tags.append('factory')
        if 'build' in name_lower:
            tags.append('builder')
        if 'mock' in name_lower:
            tags.append('mock')
        if 'test' in name_lower:
            tags.append('test-infrastructure')
        
        # Теги по типу возврата
        return_lower = return_type.lower()
        if 'message' in return_lower:
            tags.append('message')
        if 'user' in return_lower:
            tags.append('user')
        if 'chat' in return_lower:
            tags.append('chat')
        if 'ban' in return_lower:
            tags.append('ban')
        if 'captcha' in return_lower:
            tags.append('captcha')
        if 'moderation' in return_lower:
            tags.append('moderation')
        if 'ai' in return_lower:
            tags.append('ai')
        if 'spam' in return_lower:
            tags.append('spam')
        if 'telegram' in return_lower:
            tags.append('telegram')
        if 'collection' in return_lower or 'list' in return_lower:
            tags.append('collection')
        
        # Анализируем описание
        desc_lower = description.lower()
        if 'valid' in desc_lower:
            tags.append('valid')
        if 'invalid' in desc_lower:
            tags.append('invalid')
        if 'realistic' in desc_lower:
            tags.append('realistic')
        if 'fake' in desc_lower:
            tags.append('fake')
        if 'bogus' in desc_lower:
            tags.append('bogus')
        if 'autofixture' in desc_lower:
            tags.append('autofixture')
        
        return list(set(tags))  # Убираем дубликаты

    def determine_category(self, relative_path: str) -> str:
        """Определяет категорию файла по пути"""
        path_lower = relative_path.lower()
        
        if 'golden' in path_lower:
            return 'golden-master'
        elif 'mock' in path_lower:
            return 'mocks'
        elif 'specialized' in path_lower:
            return 'specialized'
        elif 'autofixture' in path_lower:
            return 'autofixture'
        elif 'bogus' in path_lower:
            return 'bogus'
        elif 'telegram' in path_lower:
            return 'telegram'
        elif 'builder' in path_lower:
            return 'builders'
        elif 'infra' in path_lower:
            return 'infrastructure'
        else:
            return 'core'

    def save_to_database(self):
        """Сохраняет данные в SQLite базу"""
        if not self.conn:
            print("❌ Database connection not established")
            return

        cursor = self.conn.cursor()
        
        # Сохраняем компоненты
        for component in self.components:
            cursor.execute("""
                INSERT INTO components (file_path, file_name, class_name, class_description, category, lines_count)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (component.file_path, component.file_name, component.class_name, 
                  component.class_description, component.category, component.lines_count))
            
            component_id = cursor.lastrowid
            
            # Сохраняем методы
            for method in component.methods:
                cursor.execute("""
                    INSERT INTO methods (component_id, name, return_type, signature, description, 
                                       line_number, is_static, is_generic)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """, (component_id, method.name, method.return_type, method.signature,
                      method.description, method.line_number, method.is_static, method.is_generic))
                
                method_id = cursor.lastrowid
                
                # Сохраняем теги
                for tag_name in method.tags:
                    # Вставляем тег если его нет
                    cursor.execute("""
                        INSERT OR IGNORE INTO tags (name) VALUES (?)
                    """, (tag_name,))
                    
                    # Получаем ID тега
                    cursor.execute("SELECT id FROM tags WHERE name = ?", (tag_name,))
                    tag_id = cursor.fetchone()[0]
                    
                    # Связываем метод с тегом
                    cursor.execute("""
                        INSERT OR IGNORE INTO method_tags (method_id, tag_id) VALUES (?, ?)
                    """, (method_id, tag_id))
                    
                    # Обновляем счетчик использования
                    cursor.execute("""
                        UPDATE tags SET usage_count = usage_count + 1 WHERE id = ?
                    """, (tag_id,))

        # Обновляем FTS индекс
        cursor.execute("INSERT INTO methods_fts(methods_fts) VALUES('rebuild')")
        
        self.conn.commit()
        print(f"✅ Saved {len(self.components)} components to database")

    def generate_summary(self):
        """Генерирует сводку по базе данных"""
        if not self.conn:
            return

        cursor = self.conn.cursor()
        
        # Общая статистика
        cursor.execute("SELECT COUNT(*) FROM components")
        components_count = cursor.fetchone()[0]
        
        cursor.execute("SELECT COUNT(*) FROM methods")
        methods_count = cursor.fetchone()[0]
        
        cursor.execute("SELECT COUNT(*) FROM tags")
        tags_count = cursor.fetchone()[0]
        
        # Статистика по категориям
        cursor.execute("""
            SELECT category, COUNT(*) as count 
            FROM components 
            GROUP BY category 
            ORDER BY count DESC
        """)
        categories = cursor.fetchall()
        
        # Популярные теги
        cursor.execute("""
            SELECT name, usage_count 
            FROM tags 
            ORDER BY usage_count DESC 
            LIMIT 10
        """)
        popular_tags = cursor.fetchall()
        
        print(f"\n📊 Database Summary:")
        print(f"   Components: {components_count}")
        print(f"   Methods: {methods_count}")
        print(f"   Tags: {tags_count}")
        
        print(f"\n📁 Categories:")
        for category, count in categories:
            print(f"   {category}: {count}")
        
        print(f"\n🏷️  Popular Tags:")
        for tag, count in popular_tags:
            print(f"   {tag}: {count}")

    def close(self):
        """Закрывает соединение с базой данных"""
        if self.conn:
            self.conn.close()

def main():
    """Основная функция"""
    testkit_path = "ClubDoorman.Test/TestKit"
    db_path = "testkit_index.db"
    
    print("🔍 TestKit SQL Indexer")
    print("=" * 50)
    
    indexer = TestKitSQLIndexer(testkit_path, db_path)
    
    try:
        # Создаем базу данных
        print("🗄️  Creating database schema...")
        indexer.create_database()
        
        # Сканируем TestKit
        print("📂 Scanning TestKit files...")
        indexer.scan_testkit()
        
        # Сохраняем в базу
        print("💾 Saving to database...")
        indexer.save_to_database()
        
        # Генерируем сводку
        indexer.generate_summary()
        
        print(f"\n✅ Index created successfully: {db_path}")
        print("\n🔍 Example queries:")
        print("   SELECT * FROM methods WHERE name LIKE '%Create%'")
        print("   SELECT * FROM methods WHERE tags LIKE '%mock%'")
        print("   SELECT * FROM methods_fts WHERE methods_fts MATCH 'message'")
        
    except Exception as e:
        print(f"❌ Error: {e}")
    finally:
        indexer.close()

if __name__ == "__main__":
    main() 