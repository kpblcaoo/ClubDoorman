# Мастер-план рефакторинга архитектуры ClubDoorman

## 📊 Текущее состояние

**Дата**: 2025-01-27  
**Статус**: Система банов централизована ✅  
**Следующий этап**: Рефакторинг архитектуры

## 🎯 Цели рефакторинга

### Основные цели:
1. **Устранение нарушений SRP** в критических сервисах
2. **Улучшение тестируемости** компонентов
3. **Упрощение поддержки** и развития
4. **Повышение читаемости** кода

### Критические проблемы:
- **MessageHandler**: 1,555 строк, 58.33% покрытия тестами
- **ModerationService**: 888 строк, 82.55% покрытия тестами
- **AiChecks**: 675 строк, 82.25% покрытия тестами
- **CallbackQueryHandler**: 619 строк, 84.84% строк, 0% веток

## 🚨 Анализ рисков

### ВЫСОКИЙ РИСК (требует улучшения тестов):
1. **MessageHandler** - 58.33% покрытия строк, 50% веток
2. **CallbackQueryHandler** - 84.84% строк, 0% веток

### СРЕДНИЙ РИСК:
1. **AiChecks** - 82.25% строк, 43.75% веток

### НИЗКИЙ РИСК (безопасно для рефакторинга):
1. **ModerationService** - 82.55% строк, 50% веток

## 📋 Этапы рефакторинга

### Этап 0: Подготовка (1-2 недели) ⚠️ КРИТИЧНО
**Цель**: Улучшить покрытие тестами перед рефакторингом

#### 0.1 Улучшение тестов MessageHandler
```csharp
// Добавить методы для используемого контента в TestKit
public static Message CreateVideoMessage()
public static Message CreateDocumentMessage() 

// Добавить тесты для основных каналов
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

// Добавить тесты для callback обработки
[Test]
public async Task HandleAsync_WithCaptchaCallback_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithBanCallback_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithApproveCallback_ProcessesCorrectly()

// Добавить тесты для различных ролей
[Test]
public async Task HandleAsync_WithUserRole_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithAdminRole_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithSuperUserRole_ProcessesCorrectly()

// Добавить тесты для edge-cases
[Test]
public async Task HandleAsync_WithEmptyMessage_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithSpamMessage_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithFastJoin_ProcessesCorrectly()

[Test]
public async Task HandleAsync_WithNullText_ProcessesCorrectly()
```

#### 0.2 Улучшение тестов CallbackQueryHandler
```csharp
// Добавить тесты для всех веток
[Test]
public async Task HandleAdminCallback_AllBranches_Covered()

[Test]
public async Task HandleCaptchaCallback_AllScenarios_Covered()

[Test]
public async Task HandleBanUser_AllScenarios_Covered()
```

#### 0.3 Создание инфраструктуры
- Создать новые папки для сервисов
- Подготовить DI контейнер
- Создать базовые интерфейсы

### Этап 1: Безопасный рефакторинг (2-3 недели) ✅ БЕЗОПАСНО
**Цель**: Рефакторинг хорошо покрытых тестами сервисов

#### 1.1 Рефакторинг ModerationService (82.55% покрытия)
```csharp
// Выделить ContentAnalyzer
Services/Moderation/Content/
├── IContentAnalyzer.cs
├── ContentAnalyzer.cs
├── ITextAnalyzer.cs
├── TextAnalyzer.cs
├── IMediaAnalyzer.cs
└── MediaAnalyzer.cs

// Выделить UserBehaviorTracker
Services/Moderation/Behavior/
├── IUserBehaviorTracker.cs
├── UserBehaviorTracker.cs
├── IMimicryAnalyzer.cs
└── MimicryAnalyzer.cs
```

#### 1.2 Рефакторинг AiChecks (82.25% покрытия)
```csharp
// Выделить ProfileAnalyzer
Services/AI/Profile/
├── IProfileAnalyzer.cs
├── ProfileAnalyzer.cs
├── IEroticPhotoAnalyzer.cs
└── EroticPhotoAnalyzer.cs

// Выделить SpamAnalyzer
Services/AI/Spam/
├── ISpamAnalyzer.cs
├── SpamAnalyzer.cs
├── ISuspiciousUserAnalyzer.cs
└── SuspiciousUserAnalyzer.cs
```

### Этап 2: Рискованный рефакторинг (3-4 недели) ⚠️ ТРЕБУЕТ ВНИМАНИЯ
**Цель**: Рефакторинг после улучшения тестов

#### 2.1 Рефакторинг MessageHandler (после улучшения тестов)
```csharp
// Выделить CommandHandler
Services/Commands/
├── ICommandHandler.cs
├── CommandHandler.cs
├── IAdminCommandHandler.cs
├── AdminCommandHandler.cs
├── IUserCommandHandler.cs
└── UserCommandHandler.cs

// Выделить UserJoinHandler
Services/UserJoin/
├── IUserJoinHandler.cs
├── UserJoinHandler.cs
├── IUserFlowProcessor.cs
└── UserFlowProcessor.cs

// Выделить MessageDeletionService
Services/Messages/
├── IMessageDeletionService.cs
├── MessageDeletionService.cs
├── IMessageReportingService.cs
└── MessageReportingService.cs
```

#### 2.2 Рефакторинг CallbackQueryHandler (после улучшения тестов)
```csharp
// Выделить CaptchaCallbackHandler
Services/Callbacks/
├── ICaptchaCallbackHandler.cs
├── CaptchaCallbackHandler.cs
├── IAdminCallbackHandler.cs
├── AdminCallbackHandler.cs
├── IUserCallbackHandler.cs
└── UserCallbackHandler.cs
```

### Этап 3: Финальная оптимизация (1-2 недели)
**Цель**: Улучшение структуры и документации

#### 3.1 Оптимизация уведомлений
```csharp
// Выделить NotificationFormatter
Services/Notifications/Formatting/
├── INotificationFormatter.cs
├── NotificationFormatter.cs
├── IAdminNotificationFormatter.cs
├── AdminNotificationFormatter.cs
├── ILogNotificationFormatter.cs
└── LogNotificationFormatter.cs
```

#### 3.2 Оптимизация роутинга
```csharp
// Выделить NotificationTypeRouter
Services/Notifications/Routing/
├── INotificationTypeRouter.cs
├── NotificationTypeRouter.cs
├── IServiceChatDispatcher.cs
└── ServiceChatDispatcher.cs
```

## 🎯 Ожидаемые результаты

### После рефакторинга:
- **MessageHandler**: 1,555 → 300-400 строк
- **ModerationService**: 888 → 400-500 строк
- **AiChecks**: 675 → 300-400 строк
- **CallbackQueryHandler**: 619 → 200-300 строк

### Преимущества:
- ✅ Лучшая тестируемость
- ✅ Легче поддерживать и расширять
- ✅ Четкое разделение ответственности
- ✅ Возможность независимого развития компонентов
- ✅ Упрощение отладки

### Целевые метрики покрытия:
- **MessageHandler**: 58.33% → 80%+ (фокус на реальные сценарии)
- **CallbackQueryHandler**: 84.84% → 85%+ (покрытие всех веток)
- **Количество тестов**: +30-50 новых тестов
- **Типы контента**: покрытие только используемых типов (text, video, document)

## 🚨 Критические моменты

### Безопасность:
1. **НЕ НАЧИНАТЬ** рефакторинг MessageHandler до улучшения тестов
2. **НЕ НАЧИНАТЬ** рефакторинг CallbackQueryHandler до покрытия веток
3. **СОЗДАВАТЬ** резервные ветки перед каждым этапом
4. **ТЕСТИРОВАТЬ** после каждого изменения

### Доступные инструменты TestKit:
1. **MessageHandler фабрики**: 12 методов для создания и настройки
2. **Создание сообщений**: 13+ методов для различных типов контента
3. **Спам и модерация**: 10+ методов для тестирования модерации
4. **Callback обработка**: 10 методов для тестирования callback queries
5. **Капча**: 10 методов для тестирования капчи
6. **Пробелы**: Нужно добавить методы для видео, документов, стикеров, голосовых сообщений

### Меры предосторожности:
1. **Поэтапный рефакторинг** - не все сразу
2. **Тщательное тестирование** - после каждого этапа
3. **Сохранение поведения** - все функции должны работать как раньше
4. **Документирование** - все изменения в changelog

## 📊 Метрики успеха

### Количественные метрики:
- Уменьшение размера файлов на 60-70%
- Увеличение покрытия тестами до 90%+
- Уменьшение сложности методов
- Увеличение количества unit-тестов

### Качественные метрики:
- Упрощение понимания кода
- Легкость добавления новых функций
- Упрощение отладки
- Улучшение code review

## 📝 Рекомендации

### Немедленно:
1. **Улучшить тесты** для MessageHandler и CallbackQueryHandler
2. **Создать план тестирования** для каждого этапа
3. **Подготовить инфраструктуру** для новых сервисов

### В процессе:
1. **Начать с ModerationService** - самый безопасный
2. **Продолжить с AiChecks** - хорошо покрыт
3. **Завершить с MessageHandler** - после улучшения тестов

### В долгосрочной перспективе:
1. **Документировать** все изменения
2. **Создать руководство** по архитектуре
3. **Провести code review** после каждого этапа
4. **Обновить README** с новой структурой

## 🎯 Заключение

**Рекомендация**: Начать с **Этапа 0** (улучшение тестов) для безопасного рефакторинга.

**Приоритеты**:
1. ✅ Улучшить покрытие тестами
2. ✅ Начать с ModerationService
3. ✅ Продолжить с AiChecks
4. ✅ Завершить с MessageHandler

**Ожидаемый результат**: Чистая, тестируемая, поддерживаемая архитектура с четким разделением ответственности. 