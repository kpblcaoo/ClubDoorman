#!/usr/bin/env python3
"""
Расширенный скрипт для добавления using директив для Messaging Module в тестовые файлы
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
    
    # Дополнительные файлы, которые нужно обновить
    files_to_update = [
        "ClubDoorman.Test/TestInfrastructure/FakeServicesFactory.cs",
        "ClubDoorman.Test/TestInfrastructure/TelegramBotClientWrapperTestFactory.cs",
        "ClubDoorman.Test/Integration/AiAnalysisTests.cs",
        "ClubDoorman.Test/StepDefinitions/Common/CaptchaSteps.cs",
        "ClubDoorman.Test/TestKit/Infra/TestKitAutoFixture.cs",
        "ClubDoorman.Test/Integration/InfrastructureE2ETests.cs",
        "ClubDoorman.Test/Unit/Handlers/MessageHandlerStatsCommandTests.cs",
        "ClubDoorman.Test/Unit/Handlers/MessageHandlerSendSuspiciousMessageTests.cs",
        "ClubDoorman.Test/Unit/Handlers/MessageHandlerDeleteAndReportMessageTests.cs",
        "ClubDoorman.Test/Integration/MessageHandlerBanBasicTests.cs",
        "ClubDoorman.Test/Unit/Handlers/MessageHandlerExtendedTests.cs",
        "ClubDoorman.Test/Unit/Handlers/MessageHandlerFakeTests.cs",
    ]
    
    print("🔄 Добавление using директив в дополнительные тестовые файлы...")
    added_count = 0
    for file_path in files_to_update:
        if os.path.exists(file_path):
            if add_using_to_file(file_path, using_statement):
                added_count += 1
    
    print(f"\n✅ Готово! Добавлено using в {added_count} дополнительных тестовых файлов")

if __name__ == "__main__":
    main() 