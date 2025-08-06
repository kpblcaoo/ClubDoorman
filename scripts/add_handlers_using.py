#!/usr/bin/env python3
"""
Скрипт для добавления using директив для Handlers namespace в тестовые файлы
"""

import os
import re
from pathlib import Path

def add_using_to_file(file_path):
    """Добавляет using директиву в файл, если её нет"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Проверяем, есть ли уже using для Handlers
        if 'using ClubDoorman.Services.Handlers;' in content:
            print(f"✅ {file_path}: using уже есть")
            return False
        
        # Ищем строки с using
        using_pattern = r'using\s+[^;]+;'
        using_matches = re.findall(using_pattern, content)
        
        # Находим последний using
        last_using_index = -1
        for match in using_matches:
            index = content.rfind(match)
            if index > last_using_index:
                last_using_index = index + len(match)
        
        if last_using_index == -1:
            print(f"⚠️ {file_path}: не найдены using директивы")
            return False
        
        # Добавляем новый using после последнего
        new_using = '\nusing ClubDoorman.Services.Handlers;'
        new_content = content[:last_using_index] + new_using + content[last_using_index:]
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        print(f"✅ {file_path}: добавлен using ClubDoorman.Services.Handlers;")
        return True
        
    except Exception as e:
        print(f"❌ {file_path}: ошибка - {e}")
        return False

def find_test_files():
    """Находит все тестовые файлы, которые могут использовать Handlers"""
    test_dir = Path("ClubDoorman.Test")
    files_to_update = []
    
    # Паттерны файлов, которые могут использовать Handlers
    patterns = [
        "**/*MessageHandler*.cs",
        "**/*CallbackQueryHandler*.cs", 
        "**/*ChatMemberHandler*.cs",
        "**/*UpdateDispatcher*.cs",
        "**/*BotPermissionsService*.cs",
        "**/*Integration*.cs",
        "**/*TestInfrastructure*.cs",
        "**/*TestKit*.cs",
        "**/*StepDefinitions*.cs",
        "**/*Unit/Handlers*.cs"
    ]
    
    for pattern in patterns:
        files = test_dir.glob(pattern)
        for file in files:
            if file.is_file():
                files_to_update.append(file)
    
    return list(set(files_to_update))  # Убираем дубликаты

def main():
    print("🔧 Добавление using директив для Handlers namespace...")
    
    files = find_test_files()
    print(f"📁 Найдено {len(files)} файлов для обновления")
    
    updated_count = 0
    for file_path in files:
        if add_using_to_file(file_path):
            updated_count += 1
    
    print(f"\n📊 Результат:")
    print(f"✅ Обновлено файлов: {updated_count}")
    print(f"📁 Всего проверено: {len(files)}")

if __name__ == "__main__":
    main() 