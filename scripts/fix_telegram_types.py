#!/usr/bin/env python3
"""
Скрипт для исправления Telegram.Bot.Types на global::Telegram.Bot.Types
"""

import os
import re

def fix_telegram_types(file_path):
    """Исправляет Telegram.Bot.Types на global::Telegram.Bot.Types в файле"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Заменяем Telegram.Bot.Types на global::Telegram.Bot.Types
    # Но только если это не в using директиве
    lines = content.split('\n')
    modified = False
    
    for i, line in enumerate(lines):
        # Пропускаем using директивы
        if line.strip().startswith('using '):
            continue
        
        # Заменяем Telegram.Bot.Types на global::Telegram.Bot.Types
        if 'Telegram.Bot.Types' in line and 'global::' not in line:
            lines[i] = line.replace('Telegram.Bot.Types', 'global::Telegram.Bot.Types')
            modified = True
    
    if modified:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lines))
        print(f"✅ {file_path} - исправлено")
        return True
    else:
        print(f"⏭️  {file_path} - не требует изменений")
        return False

def main():
    """Основная функция"""
    # Список файлов, которые нужно исправить
    files_to_fix = [
        'ClubDoorman/Services/ServiceChatDispatcher.cs',
        'ClubDoorman/Services/ModerationService.cs',
    ]
    
    updated_count = 0
    for file_path in files_to_fix:
        if os.path.exists(file_path):
            if fix_telegram_types(file_path):
                updated_count += 1
        else:
            print(f"❌ {file_path} - файл не найден")
    
    print(f"\n📊 Исправлено файлов: {updated_count}")

if __name__ == "__main__":
    main() 