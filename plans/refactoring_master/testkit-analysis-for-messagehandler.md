# Анализ TestKit для покрытия MessageHandler

## 📊 Общая статистика TestKit

**Всего компонентов**: 31  
**Всего методов**: 389  
**Всего тегов**: 20  

### Топ теги:
- `factory`: 189 использований
- `message`: 99 использований  
- `user`: 61 использование
- `chat`: 60 использований
- `builder`: 42 использования
- `moderation`: 42 использования
- `mock`: 40 использований
- `fake`: 40 использований
- `bogus`: 38 использований

## 🎯 Доступные инструменты для MessageHandler

### 1. Создание MessageHandler (12 методов)

#### Основные фабрики:
- **`.CreateMessageHandlerFactory`** - Создает фабрику для MessageHandler
- **`.CreateMessageHandlerWithDefaults`** - Создает MessageHandler с базовой настройкой
- **`.CreateMessageHandlerWithFake`** - Создает MessageHandler с FakeTelegramClient
- **`TestKitAutoFixture.CreateMessageHandler`** - Создает MessageHandler с автозависимостями

#### Билдеры:
- **`.CreateMessageHandlerBuilder`** - Создает билдер для MessageHandler
- **`TestKitMockBuilders.CreateMessageHandlerMock`** - Создает билдер для мока MessageHandler

### 2. Создание сообщений (13+ методов)

#### Базовые сообщения:
- **`.CreateMessage`** - Создает базовое сообщение (алиас к CreateValidMessage)
- **`.CreateTextMessage`** - Создает текстовое сообщение
- **`.CreateNullTextMessage`** - Создает сообщение с null текстом
- **`.CreateLongMessage`** - Создает длинное сообщение
- **`.CreateEmptyMessage`** - Создает пустое сообщение

#### Специализированные сообщения:
- **`.CreateSpamMessage`** - Создает спам-сообщение
- **`.CreateNewUserJoinMessage`** - Создает сообщение о присоединении нового пользователя
- **`.CreateHelpCommandMessage`** - Создает команду помощи
- **`.CreateChannelMessage`** - Создает сообщение канала
- **`.CreateAdminNotificationMessage`** - Создает уведомление для админов

#### Билдеры сообщений:
- **`TestKitBuilders.CreateMessage`** - Создает builder для сообщения Telegram
- **`MessageBuilder.WithText`** - Устанавливает текст сообщения
- **`MessageBuilder.WithEmptyText`** - Устанавливает пустой текст сообщения
- **`MessageBuilder.AsSpam`** - Устанавливает сообщение как спам

### 3. Создание контента (ограниченно)

#### Фото:
- **`AiChecksMockBuilder.ThatApprovesPhoto`** - Настраивает мок для одобрения фото
- **`AiChecksMockBuilder.ThatRejectsPhoto`** - Настраивает мок для отклонения фото
- **`ChatFullInfoBuilder.WithPhoto`** - Устанавливает фото чата

#### Видео/Документы:
- ❌ **НЕТ** готовых методов для создания видео, документов
- ⚠️ **ТРЕБУЕТСЯ** добавить методы для этих типов контента (используются ботом)

### 4. Спам и модерация (10+ методов)

#### Спам:
- **`.CreateSpamMessage`** - Создает спам-сообщение
- **`TestKitAutoFixture.CreateManySpamMessages`** - Создает много спам-сообщений
- **`TestKitBogus.IsSpamText`** - Проверяет, содержит ли текст спам-паттерны
- **`MessageBuilder.AsSpam`** - Устанавливает сообщение как спам
- **`ScenarioBuilder.AsSpamScenario`** - Создает спам-сценарий

#### Модерация:
- **`.CreateBanResult`** - Создает результат модерации: забанить
- **`.CreateBanModerationService`** - Создает мок IModerationService с предустановленным действием бана
- **`ModerationServiceMockBuilder.ThatBansUsers`** - Настраивает мок для бана пользователей
- **`ModerationResultBuilder.AsBan`** - Устанавливает результат как бан

### 5. Callback обработка (10 методов)

#### Callback queries:
- **`.CreateValidCallbackQuery`** - Создает валидный callback query
- **`.CreateInvalidCallbackQuery`** - Создает невалидный callback query
- **`.CreateAdminApproveCallback`** - Создает callback для одобрения админом
- **`.CreateAdminBanCallback`** - Создает callback для бана админом
- **`.CreateAdminSkipCallback`** - Создает callback для пропуска админом
- **`.CreateCallbackQueryUpdate`** - Создает update с callback query

#### Специализированные callback:
- **`Specialized.ApproveCallback`** - Callback для одобрения пользователя админом
- **`Specialized.BanCallback`** - Callback для бана пользователя админом
- **`Specialized.SkipCallback`** - Callback для пропуска пользователя админом

### 6. Капча (10 методов)

#### Капча сервисы:
- **`.CreateCaptchaServiceFactory`** - Создает фабрику для CaptchaService
- **`.CreateMockCaptchaService`** - Создает мок ICaptchaService
- **`.CreateSuccessfulCaptchaService`** - Создает мок ICaptchaService с предустановленным успешным ответом
- **`.CreateFailedCaptchaService`** - Создает мок ICaptchaService с предустановленным неуспешным ответом

#### Капча данные:
- **`.CreateValidCaptchaInfo`** - Создает валидную капчу
- **`.CreateExpiredCaptchaInfo`** - Создает истекшую капчу
- **`.CreateBaitCaptchaInfo`** - Создает приманку-капчу
- **`.CreateCorrectCaptchaResult`** - Создает правильный результат капчи
- **`.CreateIncorrectCaptchaResult`** - Создает неправильный результат капчи

### 7. Пользователи и чаты

#### Пользователи:
- **`TestKitBogus.CreateRussianText`** - Создает случайный текст на русском языке
- **`.CreateMemberBanned`** - Создает событие бана участника

#### Чаты:
- **`ChatBuilder.WithCaptchaRequired`** - Устанавливает чат с требованием капчи

## 🚨 Реальные пробелы в покрытии

### Критические пробелы (используются ботом):
1. **Видео контент** - нет методов создания видео сообщений
2. **Документы** - нет методов создания документов

### НЕ нужны (бот не обрабатывает):
- ❌ **Стикеры** - не обрабатываются ботом
- ❌ **Голосовые сообщения** - не обрабатываются ботом  
- ❌ **Анимации** - не обрабатываются ботом
- ❌ **Локации** - не обрабатываются ботом
- ❌ **Контакты** - не обрабатываются ботом
- ❌ **Опросы** - не обрабатываются ботом

### Рекомендуемые дополнения:
1. **`.CreateVideoMessage`** - Создает видео сообщение
2. **`.CreateDocumentMessage`** - Создает документ сообщение

## 📋 План улучшения тестов MessageHandler

### Этап 1: Добавление недостающих методов (1 неделя)

#### 1.1 Создать методы для используемого контента:
```csharp
// В TestKit.Main.cs
public static Message CreateVideoMessage()
public static Message CreateDocumentMessage() 
```

#### 1.2 Создать билдеры для используемого контента:
```csharp
// В MessageBuilder.cs
public MessageBuilder WithVideo()
public MessageBuilder WithDocument()
```

### Этап 2: Улучшение существующих тестов (2-3 недели)

#### 2.1 Добавить тесты для основных каналов:
```csharp
[Test]
public async Task HandleAsync_WithTextMessage_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithCommandMessage_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithBotReply_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithVideoMessage_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithDocumentMessage_ProcessesCorrectly()
```

#### 2.2 Добавить тесты для callback обработки:
```csharp
[Test]
public async Task HandleAsync_WithCaptchaCallback_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithBanCallback_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithApproveCallback_ProcessesCorrectly()
```

#### 2.3 Добавить тесты для различных ролей:
```csharp
[Test]
public async Task HandleAsync_WithUserRole_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithAdminRole_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithSuperUserRole_ProcessesCorrectly()
```

#### 2.4 Добавить тесты для edge-cases:
```csharp
[Test]
public async Task HandleAsync_WithEmptyMessage_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithSpamMessage_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithFastJoin_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithNullText_ProcessesCorrectly()
```

### Этап 3: Создание целевых сценариев (1 неделя)

#### 3.1 Критичные сценарии:
```csharp
[Test]
public async Task MessageHandler_SpamDetection_WorksCorrectly()

[Test]
public async Task MessageHandler_CaptchaFlow_WorksCorrectly()

[Test]
public async Task MessageHandler_BanFlow_WorksCorrectly()

[Test]
public async Task MessageHandler_AdminModeration_WorksCorrectly()
```

## 🎯 Ожидаемые результаты

### После улучшения:
- **Покрытие строк**: 58.33% → 80%+
- **Покрытие веток**: 50% → 75%+
- **Количество тестов**: +30-50 новых тестов
- **Типы контента**: покрытие только используемых типов

### Преимущества:
- ✅ Покрытие основных каналов (text, command, bot_reply)
- ✅ Покрытие callback обработки (captcha, ban)
- ✅ Покрытие различных ролей (user, admin, superuser)
- ✅ Покрытие edge-cases (пустое сообщение, спам, fast-join)
- ✅ Безопасность рефакторинга
- ✅ Упрощение отладки

## 📝 Рекомендации

### Немедленно:
1. **Добавить методы** для видео и документов (используются ботом)
2. **Создать тесты** для основных каналов
3. **Покрыть edge-cases** (пустое сообщение, спам, fast-join)

### В процессе:
1. **Использовать существующие** фабрики и билдеры
2. **Фокусироваться на реальных** сценариях использования
3. **Тестировать policy coverage** (различные роли)

### НЕ делать:
- ❌ Добавлять методы для неиспользуемых типов контента
- ❌ Создавать Golden Master для всех типов
- ❌ Стремиться к 90%+ покрытию (это метрика инфраструктуры)

## 🎯 Заключение

**TestKit предоставляет отличную основу** для покрытия MessageHandler, но **требует минимальных дополнений** для покрытия реально используемых типов контента.

**Приоритеты**:
1. ✅ Добавить методы для видео и документов
2. ✅ Покрыть основные каналы (text, command, callback)
3. ✅ Покрыть различные роли и edge-cases

**Ожидаемый результат**: Целевое покрытие MessageHandler с фокусом на реальные сценарии использования бота. 