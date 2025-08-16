#!/usr/bin/env python3
"""
Скрипт для добавления using ClubDoorman.Services.UserManagement в тестовые файлы
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
        'ClubDoorman.Test/TestInfrastructure/UserManagerTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/UserJoinServiceTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/ChatMemberHandlerTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/CaptchaServiceTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/ServiceChatDispatcherTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/FakeTelegramClient.cs',
        'ClubDoorman.Test/TestInfrastructure/FakeCallbackQueryHandler.cs',
        'ClubDoorman.Test/TestInfrastructure/FakeCaptchaService.cs',
        'ClubDoorman.Test/TestInfrastructure/FakeModerationService.cs',
        'ClubDoorman.Test/Integration/MessageHandlerBanAdvancedTests.cs',
        'ClubDoorman.Test/Integration/MessageHandlerBanBasicTests.cs',
        'ClubDoorman.Test/Integration/MessageHandlerBanExceptionTests.cs',
        'ClubDoorman.Test/Integration/NotificationServiceIntegrationTests.cs',
        'ClubDoorman.Test/Unit/Handlers/MessageHandlerMutationCoverageTests.cs',
        'ClubDoorman.Test/Unit/Services/UserBanServiceTests.cs',
        'ClubDoorman.Test/Unit/Services/UserBanServiceTests.Modern.cs',
        'ClubDoorman.Test/Unit/Services/BotPermissionsServiceTests.cs',
        'ClubDoorman.Test/Unit/Handlers/CallbackQueryHandlerTests.cs',
        'ClubDoorman.Test/Unit/Handlers/MessageHandlerExtendedTests.cs',
        'ClubDoorman.Test/Unit/Services/CaptchaServiceFakeTests.cs',
        'ClubDoorman.Test/Services/ServiceChatDispatcherTests.cs',
        'ClubDoorman.Test/TestKit/TestKit.NotificationServiceBuilder.cs',
    # Legacy builder removed: TestKit.UserJoinServiceBuilder.cs
        'ClubDoorman.Test/TestKit/TestKit.MessageHandlerBuilder.cs',
        'ClubDoorman.Test/TestKit/TestKit.Mocks.cs',
        'ClubDoorman.Test/TestKit/Builders/MockBuilders/TelegramBotMockBuilder.cs',
        'ClubDoorman.Test/TestKit/Builders/MockBuilders/MessageServiceMockBuilder.cs',
        'ClubDoorman.Test/TestKit/Builders/MockBuilders/AiChecksMockBuilder.cs',
        'ClubDoorman.Test/TestKit/Builders/MockBuilders/MessageHandlerMockBuilder.cs',
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