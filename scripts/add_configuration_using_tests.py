#!/usr/bin/env python3
"""
Скрипт для добавления using ClubDoorman.Services.Core.Configuration в тестовые файлы
"""

import os
import re

def add_configuration_using(file_path):
    """Добавляет using для нового namespace в файл"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Проверяем, есть ли уже этот using
    if 'using ClubDoorman.Services.Core.Configuration;' in content:
        print(f"✅ {file_path} - using уже есть")
        return False
    
    # Проверяем, используется ли IAppConfig в файле
    if 'IAppConfig' not in content:
        print(f"⏭️  {file_path} - IAppConfig не используется")
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
    lines.insert(last_using_index + 1, 'using ClubDoorman.Services.Core.Configuration;')
    
    # Записываем обратно
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    
    print(f"✅ {file_path} - добавлен using")
    return True

def main():
    """Основная функция"""
    # Список тестовых файлов, которые нужно обновить
    files_to_update = [
        'ClubDoorman.Test/TestInfrastructure/AiChecksTestFactory.cs',
        'ClubDoorman.Test/Integration/AiAnalysisTests.cs',
        'ClubDoorman.Test/Unit/Handlers/CallbackQueryHandlerTests.cs',
        'ClubDoorman.Test/TestInfrastructure/CaptchaServiceTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/AppConfigTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/ChatMemberHandlerTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/FakeCallbackQueryHandler.cs',
        'ClubDoorman.Test/TestInfrastructure/FakeCaptchaService.cs',
        'ClubDoorman.Test/TestKit/TestKit.Facade.cs',
        'ClubDoorman.Test/TestInfrastructure/FakeServicesFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs',
        'ClubDoorman.Test/TestKit/TestKit.MessageHandlerBuilder.cs',
        'ClubDoorman.Test/TestKit/TestKit.Mocks.cs',
        'ClubDoorman.Test/TestKit/TestKit.NotificationServiceBuilder.cs',
        'ClubDoorman.Test/TestKit/TestKit.UserJoinServiceBuilder.cs',
        'ClubDoorman.Test/Unit/Services/UserBanServiceTests.cs',
        'ClubDoorman.Test/Unit/Services/UserBanServiceTests.Modern.cs',
    ]
    
    updated_count = 0
    for file_path in files_to_update:
        if os.path.exists(file_path):
            if add_configuration_using(file_path):
                updated_count += 1
        else:
            print(f"❌ {file_path} - файл не найден")
    
    print(f"\n📊 Обновлено файлов: {updated_count}")

if __name__ == "__main__":
    main() 