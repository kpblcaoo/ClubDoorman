#!/usr/bin/env python3
"""
Скрипт для добавления using ClubDoorman.Services.Telegram в файлы
"""

import os
import re

def add_telegram_using(file_path):
    """Добавляет using для нового namespace в файл"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Проверяем, есть ли уже этот using
    if 'using ClubDoorman.Services.Telegram;' in content:
        print(f"✅ {file_path} - using уже есть")
        return False
    
    # Проверяем, используется ли ITelegramBotClientWrapper в файле
    if 'ITelegramBotClientWrapper' not in content:
        print(f"⏭️  {file_path} - ITelegramBotClientWrapper не используется")
        return False
    
    # Находим последний using и добавляем наш после него
    lines = content.split('\n')
    last_using_index = -1
    
    for i, line in enumerate(lines):
        if line.strip().startswith('using '):
            last_using_index = i
    
    if last_using_index == -1:
        print(f"⚠️  {file_path} - не найдены using директивы")
        return False
    
    # Добавляем наш using после последнего
    lines.insert(last_using_index + 1, 'using ClubDoorman.Services.Telegram;')
    
    # Записываем обратно
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    
    print(f"✅ {file_path} - добавлен using")
    return True

def main():
    """Основная функция"""
    # Список файлов, которые нужно обновить
    files_to_update = [
        'ClubDoorman/Services/AiChecks.cs',
        'ClubDoorman/Services/MessageService.cs',
        'ClubDoorman/Services/StatisticsService.cs',
        'ClubDoorman/Services/CaptchaService.cs',
        'ClubDoorman/Services/UserManager.cs',
        'ClubDoorman/Services/BanSystem/UserBanService.cs',
        'ClubDoorman/Services/IntroFlowService.cs',
        'ClubDoorman/Services/LogChatService.cs',
        'ClubDoorman/Services/ViolationTracker.cs',
        'ClubDoorman/Handlers/MessageHandler.cs',
        'ClubDoorman/Handlers/CallbackQueryHandler.cs',
        'ClubDoorman/Handlers/ChatMemberHandler.cs',
        'ClubDoorman/Handlers/Commands/StartCommandHandler.cs',
        'ClubDoorman/Handlers/Commands/SuspiciousCommandHandler.cs',
        'ClubDoorman/Worker.cs',
    ]
    
    updated_count = 0
    for file_path in files_to_update:
        if os.path.exists(file_path):
            if add_telegram_using(file_path):
                updated_count += 1
        else:
            print(f"❌ {file_path} - файл не найден")
    
    print(f"\n📊 Обновлено файлов: {updated_count}")

if __name__ == "__main__":
    main() 