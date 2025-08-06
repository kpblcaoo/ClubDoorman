#!/usr/bin/env python3
"""
Скрипт для добавления using директив для Messaging Module в тестовые файлы
"""

import os
import re
from pathlib import Path

def add_using_to_file(file_path, using_statement):
    """Добавляет using директиву в файл, если её там нет"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Проверяем, есть ли уже этот using
        if using_statement in content:
            print(f"✅ {file_path}: using уже есть")
            return False
        
        # Находим последний using
        lines = content.split('\n')
        using_end_index = -1
        
        for i, line in enumerate(lines):
            if line.strip().startswith('using '):
                using_end_index = i
            elif line.strip() == '' and using_end_index != -1:
                # Пустая строка после using
                break
            elif not line.strip().startswith('using ') and using_end_index != -1:
                # Нашли не-using строку после using
                break
        
        # Вставляем новый using
        if using_end_index != -1:
            lines.insert(using_end_index + 1, using_statement)
        else:
            # Если нет using, добавляем в начало
            lines.insert(0, using_statement)
        
        # Записываем обратно
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lines))
        
        print(f"✅ {file_path}: добавлен using")
        return True
        
    except Exception as e:
        print(f"❌ {file_path}: ошибка - {e}")
        return False

def main():
    # Настройки для Messaging Module
    using_statement = "using ClubDoorman.Services.Messaging;"
    
    # Файлы, которые нужно обновить
    files_to_update = [
        "ClubDoorman.Test/Integration/NotificationServiceBuilderTests.cs",
        "ClubDoorman.Test/Integration/NotificationServiceIntegrationTests.cs",
        "ClubDoorman.Test/TestKit/TestKit.NotificationServiceBuilder.cs",
        "ClubDoorman.Test/TestInfrastructure/CaptchaServiceTestFactory.cs",
        "ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactory.cs",
        "ClubDoorman.Test/Services/ServiceChatDispatcherTests.cs",
        "ClubDoorman.Test/Integration/MessageHandlerBanExceptionTests.cs",
        "ClubDoorman.Test/TestInfrastructure/ChatMemberHandlerTestFactory.cs",
        "ClubDoorman.Test/Integration/NotificationServiceIntegrationTests.cs",
        "ClubDoorman.Test/TestInfrastructure/FakeCaptchaService.cs",
        "ClubDoorman.Test/Unit/Handlers/CallbackQueryHandlerTests.cs",
        "ClubDoorman.Test/TestKit/Builders/MockBuilders/MessageServiceMockBuilder.cs",
        "ClubDoorman.Test/Unit/Infrastructure/StatisticsServiceGetChatLinkTests.cs",
        "ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs",
        "ClubDoorman.Test/TestInfrastructure/StatisticsServiceTestFactory.cs",
        "ClubDoorman.Test/Unit/Infrastructure/WorkerGetChatLinkTests.cs",
        "ClubDoorman.Test/TestInfrastructure/FakeModerationService.cs",
        "ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactory.cs",
        "ClubDoorman.Test/TestInfrastructure/ServiceChatDispatcherTestFactory.cs",
        "ClubDoorman.Test/Unit/Handlers/MessageHandlerMutationCoverageTests.cs",
        "ClubDoorman.Test/Unit/Services/CaptchaServiceFakeTests.cs",
        "ClubDoorman.Test/TestKit/TestKit.Mocks.cs",
        "ClubDoorman.Test/Unit/Services/MessageTemplatesHtmlTests.cs",
        "ClubDoorman.Test/Unit/Services/UserBanServiceTests.cs",
        "ClubDoorman.Test/Unit/Services/ServiceChatDispatcherTests.cs",
        "ClubDoorman.Test/TestKit/TestKit.MessageHandlerBuilder.cs",
        "ClubDoorman.Test/TestKit/TestKit.UserJoinServiceBuilder.cs",
        "ClubDoorman.Test/Unit/Services/UserBanServiceTests.Modern.cs",
        "ClubDoorman.Test/TestKit/TestKit.Specialized.cs",
    ]
    
    print("🔄 Добавление using директив в тестовые файлы...")
    added_count = 0
    for file_path in files_to_update:
        if os.path.exists(file_path):
            if add_using_to_file(file_path, using_statement):
                added_count += 1
    
    print(f"\n✅ Готово! Добавлено using в {added_count} тестовых файлов")

if __name__ == "__main__":
    main() 