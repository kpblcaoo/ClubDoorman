#!/usr/bin/env python3
"""
Скрипт для добавления using директив для Messaging Module
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

def update_namespace_in_file(file_path, old_namespace, new_namespace):
    """Обновляет namespace в файле"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Заменяем namespace
        new_content = content.replace(old_namespace, new_namespace)
        
        if new_content != content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print(f"✅ {file_path}: обновлен namespace {old_namespace} → {new_namespace}")
            return True
        else:
            print(f"ℹ️ {file_path}: namespace не изменен")
            return False
            
    except Exception as e:
        print(f"❌ {file_path}: ошибка - {e}")
        return False

def main():
    # Настройки для Messaging Module
    using_statement = "using ClubDoorman.Services.Messaging;"
    
    # Файлы, которые нужно обновить
    files_to_update = [
        "ClubDoorman/Program.cs",
        "ClubDoorman/Handlers/MessageHandler.cs",
        "ClubDoorman/Handlers/CallbackQueryHandler.cs",
        "ClubDoorman/Handlers/ChatMemberHandler.cs",
        "ClubDoorman/Handlers/Commands/StartCommandHandler.cs",
        "ClubDoorman/Handlers/Commands/SuspiciousCommandHandler.cs",
        "ClubDoorman/Services/Worker.cs",
        "ClubDoorman/Services/ModerationService.cs",
        "ClubDoorman/Services/CaptchaService.cs",
        "ClubDoorman/Services/IntroFlowService.cs",
        "ClubDoorman/Services/UserBanService.cs",
        "ClubDoorman/Services/Statistics/StatisticsService.cs",
        "ClubDoorman/Services/AI/AiChecks.cs",
        "ClubDoorman/Services/UserManagement/UserManager.cs",
        "ClubDoorman/Services/UserManagement/UserJoinService.cs",
        "ClubDoorman/Services/UserManagement/UserCleanupService.cs",
        "ClubDoorman/Services/Commands/CommandProcessingService.cs",
        "ClubDoorman/Services/ChannelModerationService.cs",
        "ClubDoorman/Services/BotPermissionsService.cs",
        "ClubDoorman/Services/TextProcessor.cs",
        "ClubDoorman/Services/SimpleFilters.cs",
        "ClubDoorman/Services/UpdateDispatcher.cs",
        "ClubDoorman/Services/UserFlowLogger.cs",
        "ClubDoorman/Services/LoggingConfigurationService.cs",
        "ClubDoorman/Services/BanSystem/UserBanService.cs",
        "ClubDoorman/Services/BanSystem/ViolationTracker.cs",
        "ClubDoorman/Services/Statistics/GlobalStatsManager.cs",
        "ClubDoorman/Services/AI/SpamHamClassifier.cs",
        "ClubDoorman/Services/AI/MimicryClassifier.cs",
        "ClubDoorman/Services/BadMessageManager.cs",
        "ClubDoorman/Services/SuspiciousUsersStorage.cs",
        "ClubDoorman/Services/Telegram/TelegramBotClientWrapper.cs",
        "ClubDoorman/Services/Core/Configuration/AppConfig.cs",
        "ClubDoorman/Infrastructure/Utils.cs",
        "ClubDoorman/Infrastructure/Config.cs",
        "ClubDoorman/Infrastructure/Consts.cs",
        "ClubDoorman/Infrastructure/Exceptions.cs",
        "ClubDoorman/Infrastructure/OpenAiExtensions.cs",
        "ClubDoorman/Infrastructure/SemaphoreHelper.cs",
        "ClubDoorman/Infrastructure/Captcha.cs",
        "ClubDoorman/Models/NotificationData.cs",
        "ClubDoorman/Models/Notifications/AdminNotificationType.cs",
        "ClubDoorman/Models/Notifications/LogNotificationType.cs",
        "ClubDoorman/Models/Requests/CreateCaptchaRequest.cs",
        "ClubDoorman/Models/Requests/SendCaptchaMessageRequest.cs",
        "ClubDoorman/Models/Requests/SendErrorNotificationRequest.cs",
        "ClubDoorman/Models/Requests/SendNotificationRequest.cs",
        "ClubDoorman/Models/CaptchaInfo.cs",
        "ClubDoorman/Models/CaptchaResult.cs",
        "ClubDoorman/Models/ChatStats.cs",
        "ClubDoorman/Models/Logging/LoggingConfiguration.cs",
        "ClubDoorman/Models/ModerationResult.cs",
        "ClubDoorman/Models/SuspiciousUserInfo.cs",
        "ClubDoorman/Models/BanSystem/BanType.cs",
        "ClubDoorman/Models/Statistics/GlobalStats.cs",
        "ClubDoorman/Models/AI/AiAnalysisResult.cs",
        "ClubDoorman/Models/AI/AiCheckRequest.cs",
        "ClubDoorman/Models/AI/AiCheckResponse.cs",
        "ClubDoorman/Models/AI/AiCheckType.cs",
        "ClubDoorman/Models/AI/AiCheckStatus.cs",
        "ClubDoorman/Models/AI/AiCheckPriority.cs",
        "ClubDoorman/Models/AI/AiCheckResult.cs",
        "ClubDoorman/Models/AI/AiCheckError.cs",
        "ClubDoorman/Models/AI/AiCheckRequestType.cs",
        "ClubDoorman/Models/AI/AiCheckResponseType.cs",
        "ClubDoorman/Models/AI/AiCheckRequestStatus.cs",
        "ClubDoorman/Models/AI/AiCheckResponseStatus.cs",
        "ClubDoorman/Models/AI/AiCheckRequestPriority.cs",
        "ClubDoorman/Models/AI/AiCheckResponsePriority.cs",
        "ClubDoorman/Models/AI/AiCheckRequestResult.cs",
        "ClubDoorman/Models/AI/AiCheckResponseResult.cs",
        "ClubDoorman/Models/AI/AiCheckRequestError.cs",
        "ClubDoorman/Models/AI/AiCheckResponseError.cs",
        "ClubDoorman/Models/AI/AiCheckRequestType.cs",
        "ClubDoorman/Models/AI/AiCheckResponseType.cs",
        "ClubDoorman/Models/AI/AiCheckRequestStatus.cs",
        "ClubDoorman/Models/AI/AiCheckResponseStatus.cs",
        "ClubDoorman/Models/AI/AiCheckRequestPriority.cs",
        "ClubDoorman/Models/AI/AiCheckResponsePriority.cs",
        "ClubDoorman/Models/AI/AiCheckRequestResult.cs",
        "ClubDoorman/Models/AI/AiCheckResponseResult.cs",
        "ClubDoorman/Models/AI/AiCheckRequestError.cs",
        "ClubDoorman/Models/AI/AiCheckResponseError.cs",
    ]
    
    # Обновляем namespace в перемещенных файлах
    messaging_files = [
        "ClubDoorman/Services/Messaging/IMessageService.cs",
        "ClubDoorman/Services/Messaging/MessageService.cs",
        "ClubDoorman/Services/Messaging/MessageTemplates.cs",
        "ClubDoorman/Services/Messaging/IServiceChatDispatcher.cs",
        "ClubDoorman/Services/Messaging/ServiceChatDispatcher.cs",
        "ClubDoorman/Services/Messaging/ILogChatService.cs",
        "ClubDoorman/Services/Messaging/LogChatService.cs",
        "ClubDoorman/Services/Messaging/INotificationService.cs",
        "ClubDoorman/Services/Messaging/NotificationService.cs",
        "ClubDoorman/Services/Messaging/IChatLinkFormatter.cs",
        "ClubDoorman/Services/Messaging/ChatLinkFormatter.cs",
    ]
    
    print("🔄 Обновление namespace в перемещенных файлах...")
    for file_path in messaging_files:
        if os.path.exists(file_path):
            update_namespace_in_file(file_path, "ClubDoorman.Services", "ClubDoorman.Services.Messaging")
    
    print("\n🔄 Добавление using директив...")
    added_count = 0
    for file_path in files_to_update:
        if os.path.exists(file_path):
            if add_using_to_file(file_path, using_statement):
                added_count += 1
    
    print(f"\n✅ Готово! Добавлено using в {added_count} файлов")

if __name__ == "__main__":
    main() 