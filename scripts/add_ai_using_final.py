#!/usr/bin/env python3
"""
Скрипт для добавления using ClubDoorman.Services.AI в оставшиеся тестовые файлы
"""

import os
import re

def add_ai_using(file_path):
    """Добавляет using для нового namespace в файл"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Проверяем, есть ли уже этот using
    if 'using ClubDoorman.Services.AI;' in content:
        print(f"✅ {file_path} - using уже есть")
        return False
    
    # Проверяем, используется ли AI сервис в файле
    if ('IAiChecks' not in content and 'AiChecks' not in content and 
        'ISpamHamClassifier' not in content and 'SpamHamClassifier' not in content and
        'IMimicryClassifier' not in content and 'MimicryClassifier' not in content and
        'SpamPhotoBio' not in content and 'SpamProbability' not in content):
        print(f"⏭️  {file_path} - AI сервисы не используются")
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
    lines.insert(last_using_index + 1, 'using ClubDoorman.Services.AI;')
    
    # Записываем обратно
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    
    print(f"✅ {file_path} - добавлен using")
    return True

def main():
    """Основная функция"""
    # Список файлов, которые нужно обновить
    files_to_update = [
        'ClubDoorman.Test/TestInfrastructure/AiChecksTestFactoryTests.cs',
        'ClubDoorman.Test/TestInfrastructure/TelegramBotClientWrapperTestFactory.cs',
        'ClubDoorman.Test/TestInfrastructure/SpamHamClassifierTestFactoryTests.cs',
        'ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs',
        'ClubDoorman.Test/TestInfrastructure/MimicryClassifierTestFactoryTests.cs',
        'ClubDoorman.Test/Integration/InfrastructureE2ETests.cs',
        'ClubDoorman.Test/TestKit/Infra/TestKitAutoFixture.cs',
        'ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs',
        'ClubDoorman.Test/Unit/Services/AiChecksExtendedTests.cs',
    ]
    
    updated_count = 0
    for file_path in files_to_update:
        if os.path.exists(file_path):
            if add_ai_using(file_path):
                updated_count += 1
        else:
            print(f"❌ {file_path} - файл не найден")
    
    print(f"\n📊 Обновлено файлов: {updated_count}")

if __name__ == "__main__":
    main() 