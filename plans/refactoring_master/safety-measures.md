# Защитные меры перед рефакторингом системы банов

## 🛡️ Обзор

Простые защитные меры для одного спринта рефакторинга системы банов. Основной фокус - тесты, гарантирующие неизменность поведения.

## 🎯 Цели защиты

1. **Гарантия неизменности поведения**: Система банов работает точно так же после рефакторинга
2. **Быстрый откат**: Возможность вернуться к предыдущему состоянию
3. **Тестирование безопасности**: Проверка корректности всех типов банов
4. **Документирование состояния**: Фиксация текущего поведения

## 📋 Защитные меры

### 1. Создание резервных копий

#### Git стратегия
```bash
# Создание защитной ветки
git checkout -b backup/ban-system-before-refactoring
git push origin backup/ban-system-before-refactoring
```

#### Резервное копирование конфигурации
```bash
# Копирование текущих настроек
cp ClubDoorman/appsettings.json ClubDoorman/appsettings.json.backup
cp ClubDoorman/data/approved_users.json ClubDoorman/data/approved_users.json.backup
```

### 2. Создание тестов поведения (КРИТИЧНО)

#### Golden Master тесты для всех типов банов
```csharp
[Test]
public async Task BanSystem_GoldenMaster_LongNameBan_BehaviorUnchanged()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    var userBanService = testKit.CreateUserBanService();
    var user = testKit.CreateUser();
    var chat = testKit.CreateChat();
    var message = testKit.CreateMessage(user, chat);
    
    // Act
    await userBanService.BanUserForLongNameAsync(message, user, "Test reason", null);
    
    // Assert - проверяем что поведение не изменилось
    testKit.VerifyBanChatMemberCalled(chat.Id, user.Id, null, true); // перманентный бан, revokeMessages=true
    testKit.VerifyDeleteMessageCalled(chat.Id, message.MessageId);
    testKit.VerifyCleanupUserFromAllListsCalled(user.Id, chat.Id);
    testKit.VerifyNotificationSent(LogNotificationType.BanForLongName);
}

[Test]
public async Task BanSystem_GoldenMaster_BlacklistBan_BehaviorUnchanged()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    var userBanService = testKit.CreateUserBanService();
    var user = testKit.CreateUser();
    var chat = testKit.CreateChat();
    var message = testKit.CreateMessage(user, chat);
    
    // Act
    await userBanService.BanBlacklistedUserAsync(message, user);
    
    // Assert - проверяем что поведение не изменилось
    var expectedDuration = TimeSpan.FromMinutes(240); // 4 часа
    testKit.VerifyBanChatMemberCalled(chat.Id, user.Id, expectedDuration, true);
    testKit.VerifyDeleteMessageCalled(chat.Id, message.MessageId);
    testKit.VerifyCleanupUserFromAllListsCalled(user.Id, chat.Id);
    testKit.VerifyNotificationSent(LogNotificationType.AutoBanBlacklist);
}

[Test]
public async Task BanSystem_GoldenMaster_AutoBan_BehaviorUnchanged()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    var userBanService = testKit.CreateUserBanService();
    var user = testKit.CreateUser();
    var chat = testKit.CreateChat();
    var message = testKit.CreateMessage(user, chat);
    
    // Act
    await userBanService.AutoBanAsync(message, "Spam detected");
    
    // Assert - проверяем что поведение не изменилось
    testKit.VerifyBanChatMemberCalled(chat.Id, user.Id, null, false); // перманентный бан, revokeMessages=false
    testKit.VerifyCleanupUserFromAllListsCalled(user.Id, chat.Id);
    testKit.VerifyNotificationSent(LogNotificationType.AutoBan);
}
```

#### Интеграционные тесты полного цикла
```csharp
[Test]
public async Task BanSystem_Integration_CompleteBanFlow_BehaviorUnchanged()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    var messageHandler = testKit.CreateMessageHandler();
    var user = testKit.CreateUser();
    var chat = testKit.CreateChat();
    var message = testKit.CreateMessage(user, chat);
    
    // Симулируем длинное имя
    user.FirstName = "VeryLongNameThatExceedsTheLimit";
    
    // Act
    await messageHandler.HandleAsync(CreateUpdate(message));
    
    // Assert - проверяем полный цикл
    testKit.VerifyBanChatMemberCalled(chat.Id, user.Id, null, true);
    testKit.VerifyDeleteMessageCalled(chat.Id, message.MessageId);
    testKit.VerifyCleanupUserFromAllListsCalled(user.Id, chat.Id);
    testKit.VerifyNotificationSent(LogNotificationType.BanForLongName);
    testKit.VerifyUserFlowLogged(user.Id, chat.Id, "Длинное имя пользователя");
}

[Test]
public async Task BanSystem_Integration_BlacklistFlow_BehaviorUnchanged()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    testKit.SetupUserInBlacklist(userId);
    var messageHandler = testKit.CreateMessageHandler();
    var user = testKit.CreateUser();
    var chat = testKit.CreateChat();
    var message = testKit.CreateMessage(user, chat);
    
    // Act
    await messageHandler.HandleAsync(CreateUpdate(message));
    
    // Assert - проверяем полный цикл
    var expectedDuration = TimeSpan.FromMinutes(240);
    testKit.VerifyBanChatMemberCalled(chat.Id, user.Id, expectedDuration, true);
    testKit.VerifyDeleteMessageCalled(chat.Id, message.MessageId);
    testKit.VerifyCleanupUserFromAllListsCalled(user.Id, chat.Id);
    testKit.VerifyNotificationSent(LogNotificationType.AutoBanBlacklist);
}
```

#### Тесты обработки ошибок
```csharp
[Test]
public async Task BanSystem_ErrorHandling_TelegramApiError_BehaviorUnchanged()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    testKit.SetupTelegramApiError();
    var userBanService = testKit.CreateUserBanService();
    var user = testKit.CreateUser();
    var chat = testKit.CreateChat();
    var message = testKit.CreateMessage(user, chat);
    
    // Act
    await userBanService.BanUserForLongNameAsync(message, user, "Test reason", null);
    
    // Assert - проверяем что ошибка обрабатывается корректно
    testKit.VerifyErrorLogged("Не удалось забанить пользователя за длинное имя");
    testKit.VerifyNoBanChatMemberCalled(); // бан не должен быть вызван при ошибке
}

[Test]
public async Task BanSystem_ErrorHandling_PrivateChat_BehaviorUnchanged()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    var userBanService = testKit.CreateUserBanService();
    var user = testKit.CreateUser();
    var chat = testKit.CreatePrivateChat(); // приватный чат
    var message = testKit.CreateMessage(user, chat);
    
    // Act
    await userBanService.BanUserForLongNameAsync(message, user, "Test reason", null);
    
    // Assert - проверяем что в приватном чате бан не выполняется
    testKit.VerifyNoBanChatMemberCalled();
    testKit.VerifyWarningLogged("Cannot ban user in private chat");
}
```

### 3. Создание тестов производительности

#### Тесты времени выполнения
```csharp
[Test]
public async Task BanSystem_Performance_BanOperation_WithinAcceptableLimits()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    var userBanService = testKit.CreateUserBanService();
    var user = testKit.CreateUser();
    var chat = testKit.CreateChat();
    var message = testKit.CreateMessage(user, chat);
    var stopwatch = new Stopwatch();
    
    // Act
    stopwatch.Start();
    await userBanService.BanUserForLongNameAsync(message, user, "Test reason", null);
    stopwatch.Stop();
    
    // Assert
    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000)); // максимум 1 секунда
}

[Test]
public async Task BanSystem_Performance_MultipleBans_WithinAcceptableLimits()
{
    // Arrange
    var testKit = TK.CreateTestKit();
    var userBanService = testKit.CreateUserBanService();
    var chat = testKit.CreateChat();
    var users = Enumerable.Range(1, 10).Select(i => testKit.CreateUser(i)).ToList();
    var messages = users.Select(u => testKit.CreateMessage(u, chat)).ToList();
    var stopwatch = new Stopwatch();
    
    // Act
    stopwatch.Start();
    var tasks = messages.Select((m, i) => userBanService.BanUserForLongNameAsync(m, users[i], "Test reason", null));
    await Task.WhenAll(tasks);
    stopwatch.Stop();
    
    // Assert
    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(3000)); // максимум 3 секунды для 10 банов
}
```

### 4. Создание тестов конфигурации

#### Тесты загрузки конфигурации
```csharp
[Test]
public void BanSystem_Configuration_LoadFromJson_AllTypesConfigured()
{
    // Arrange
    var json = @"
    {
        ""BanConfigurations"": {
            ""Configurations"": {
                ""LongName"": {
                    ""Type"": ""LongName"",
                    ""Duration"": null,
                    ""RevokeMessages"": true,
                    ""CleanupFromLists"": true,
                    ""NotificationType"": ""BanForLongName"",
                    ""DefaultReason"": ""Длинное имя пользователя""
                }
            }
        }
    }";
    
    // Act
    var configurations = JsonSerializer.Deserialize<BanConfigurations>(json);
    
    // Assert
    Assert.That(configurations, Is.Not.Null);
    Assert.That(configurations.Configurations, Has.Count.EqualTo(1));
    Assert.That(configurations.Configurations[BanType.LongName], Is.Not.Null);
    Assert.That(configurations.Configurations[BanType.LongName].Type, Is.EqualTo(BanType.LongName));
    Assert.That(configurations.Configurations[BanType.LongName].Duration, Is.Null);
    Assert.That(configurations.Configurations[BanType.LongName].RevokeMessages, Is.True);
}
```

### 5. Создание простого плана отката

#### Скрипт быстрого отката
```bash
#!/bin/bash
# rollback-ban-system.sh

echo "🔄 Rolling back ban system to previous state..."

# Восстановление резервных копий
echo "Restoring backup files..."
cp ClubDoorman/appsettings.json.backup ClubDoorman/appsettings.json
cp ClubDoorman/data/approved_users.json.backup ClubDoorman/data/approved_users.json

# Переключение на резервную ветку
echo "Switching to backup branch..."
git checkout backup/ban-system-before-refactoring

# Перезапуск приложения
echo "Restarting application..."
sudo systemctl restart clubdoorman

echo "✅ Rollback completed successfully!"
```

### 6. Создание документации текущего поведения

#### Документирование текущего поведения
```markdown
# Текущее поведение системы банов на [ДАТА]

## LongNameBan
- **Условие**: Имя пользователя длиннее лимита
- **Действие**: Перманентный бан (Duration = null)
- **RevokeMessages**: true
- **Очистка**: Да (CleanupFromLists = true)
- **Уведомление**: LogNotificationType.BanForLongName

## BlacklistBan
- **Условие**: Пользователь в блэклисте
- **Действие**: Временный бан на 4 часа (Duration = 240 минут)
- **RevokeMessages**: true
- **Очистка**: Да (CleanupFromLists = true)
- **Уведомление**: LogNotificationType.AutoBanBlacklist

## AutoBan
- **Условие**: Автоматическое нарушение
- **Действие**: Перманентный бан (Duration = null)
- **RevokeMessages**: false
- **Очистка**: Да (CleanupFromLists = true)
- **Уведомление**: LogNotificationType.AutoBan

## ManualBan
- **Условие**: Ручной бан через админ-панель
- **Действие**: Перманентный бан (Duration = null)
- **RevokeMessages**: true
- **Очистка**: Да (CleanupFromLists = true)
- **Уведомление**: LogNotificationType.ManualBan

## ProfileBan
- **Условие**: Бан по анализу профиля
- **Действие**: Перманентный бан (Duration = null)
- **RevokeMessages**: true
- **Очистка**: Да (CleanupFromLists = true)
- **Уведомление**: LogNotificationType.ProfileBan

## ChannelBan
- **Условие**: Автоматический бан канала
- **Действие**: Перманентный бан (Duration = null)
- **RevokeMessages**: false
- **Очистка**: Да (CleanupFromLists = true)
- **Уведомление**: LogNotificationType.ChannelBan

## CaptchaBan
- **Условие**: Неудачная капча
- **Действие**: Временный бан на 10 минут (Duration = 10 минут)
- **RevokeMessages**: true
- **Очистка**: Нет (CleanupFromLists = false)
- **Уведомление**: LogNotificationType.CaptchaBan
```

## 📋 Чек-лист защитных мер

### Перед началом рефакторинга:
- [ ] Создать резервные копии файлов
- [ ] Создать защитную ветку в Git
- [ ] Создать Golden Master тесты для всех типов банов
- [ ] Создать интеграционные тесты полного цикла
- [ ] Создать тесты обработки ошибок
- [ ] Создать тесты производительности
- [ ] Создать тесты конфигурации
- [ ] Подготовить план отката
- [ ] Документировать текущее поведение

### Во время рефакторинга:
- [ ] Запускать Golden Master тесты после каждого изменения
- [ ] Проверять что все тесты проходят
- [ ] Мониторить производительность
- [ ] Документировать изменения

### После рефакторинга:
- [ ] Полное тестирование всех сценариев
- [ ] Проверка производительности
- [ ] Обновление документации
- [ ] Удаление резервных копий (после стабилизации)

## 🎯 Ожидаемые результаты

1. **Гарантия поведения**: Все типы банов работают точно так же
2. **Быстрое тестирование**: Golden Master тесты показывают изменения
3. **Простой откат**: Возможность быстро вернуться к предыдущему состоянию
4. **Документация**: Полная документация текущего поведения 