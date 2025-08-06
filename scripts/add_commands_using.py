#!/usr/bin/env python3
"""
Скрипт для добавления using директив для Commands модуля
"""

import os
import re
from pathlib import Path

def add_commands_using_to_file(file_path):
    """Добавляет using для Commands модуля в файл"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Проверяем, есть ли уже using для Commands
        if 'using ClubDoorman.Services.Commands;' in content:
            print(f"✅ {file_path}: using уже есть")
            return False
        
        # Ищем место для вставки using директивы
        lines = content.split('\n')
        
        # Ищем последний using
        last_using_index = -1
        for i, line in enumerate(lines):
            if line.strip().startswith('using '):
                last_using_index = i
        
        if last_using_index == -1:
            print(f"⚠️ {file_path}: не найдены using директивы")
            return False
        
        # Добавляем using после последнего using
        lines.insert(last_using_index + 1, 'using ClubDoorman.Services.Commands;')
        
        # Записываем обратно
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lines))
        
        print(f"✅ {file_path}: добавлен using")
        return True
        
    except Exception as e:
        print(f"❌ {file_path}: ошибка - {e}")
        return False

def find_cs_files_with_commands():
    """Находит .cs файлы, которые используют Commands сервисы"""
    project_root = Path('/home/kpblc/projects/ClubDoorman')
    test_files = []
    
    # Ищем в тестовых файлах
    for root, dirs, files in os.walk(project_root / 'ClubDoorman.Test'):
        for file in files:
            if file.endswith('.cs'):
                file_path = Path(root) / file
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        if ('ICommandProcessingService' in content or 'CommandProcessingService' in content or 
                            'ICommandHandler' in content or 'StartCommandHandler' in content or 
                            'SuspiciousCommandHandler' in content) and 'using ClubDoorman.Services.Commands;' not in content:
                            test_files.append(file_path)
                except Exception as e:
                    print(f"⚠️ Не удалось прочитать {file_path}: {e}")
    
    return test_files

def main():
    """Основная функция"""
    print("🔍 Поиск файлов, которые используют Commands сервисы...")
    
    files_to_update = find_cs_files_with_commands()
    
    if not files_to_update:
        print("✅ Все файлы уже имеют нужные using директивы")
        return
    
    print(f"📝 Найдено {len(files_to_update)} файлов для обновления:")
    for file_path in files_to_update:
        print(f"  - {file_path}")
    
    print("\n🔄 Обновление файлов...")
    updated_count = 0
    
    for file_path in files_to_update:
        if add_commands_using_to_file(file_path):
            updated_count += 1
    
    print(f"\n✅ Обновлено {updated_count} файлов из {len(files_to_update)}")

if __name__ == '__main__':
    main() 