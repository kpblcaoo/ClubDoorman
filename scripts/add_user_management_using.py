#!/usr/bin/env python3
"""
Скрипт для добавления using ClubDoorman.Services.UserManagement в файлы
"""

import os
import re

def add_user_management_using(file_path):
    """Добавляет using для нового namespace в файл"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Проверяем, есть ли уже этот using
    if 'using ClubDoorman.Services.UserManagement;' in content:
        print(f"✅ {file_path} - using уже есть")
        return False
    
    # Проверяем, используется ли User Management сервис в файле
    if ('IUserManager' not in content and 'UserManager' not in content and 
        'ApprovedUsersStorage' not in content and
        'IUserCleanupService' not in content and 'UserCleanupService' not in content and
    'UserJoinFacade' not in content):
        print(f"⏭️  {file_path} - User Management сервисы не используются")
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
    lines.insert(last_using_index + 1, 'using ClubDoorman.Services.UserManagement;')
    
    # Записываем обратно
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    
    print(f"✅ {file_path} - добавлен using")
    return True

def main():
    """Основная функция"""
    # Список файлов, которые нужно обновить
    files_to_update = [
        'ClubDoorman/Worker.cs',
        'ClubDoorman/Handlers/MessageHandler.cs',
        'ClubDoorman/Handlers/CallbackQueryHandler.cs',
        'ClubDoorman/Handlers/ChatMemberHandler.cs',
        'ClubDoorman/Services/ModerationService.cs',
        'ClubDoorman/Services/ServiceChatDispatcher.cs',
        'ClubDoorman/Services/ChannelModerationService.cs',
        'ClubDoorman/Services/BotPermissionsService.cs',
        'ClubDoorman/Services/LogChatService.cs',
        'ClubDoorman/Services/MessageService.cs',
        'ClubDoorman/Services/CaptchaService.cs',
        'ClubDoorman/Services/IntroFlowService.cs',
        'ClubDoorman/Services/ViolationTracker.cs',
        'ClubDoorman/Services/BanSystem/UserBanService.cs',
        'ClubDoorman/Services/Handlers/Commands/StartCommandHandler.cs',
        'ClubDoorman/Services/Handlers/Commands/SuspiciousCommandHandler.cs',
    ]
    
    updated_count = 0
    for file_path in files_to_update:
        if os.path.exists(file_path):
            if add_user_management_using(file_path):
                updated_count += 1
        else:
            print(f"❌ {file_path} - файл не найден")
    
    print(f"\n📊 Обновлено файлов: {updated_count}")

if __name__ == "__main__":
    main() 