# Архитектурный анализ ClubDoorman

## 📊 Текущее состояние после миграции системы банов

**Дата**: 2025-01-27  
**Статус**: Система банов централизована в `UserBanService` ✅

## 🎯 Основные нарушения SRP (Single Responsibility Principle)

### 1. **MessageHandler.cs** (1,555 строк) - КРИТИЧЕСКОЕ НАРУШЕНИЕ

**Проблемы:**
- Обработка всех типов сообщений (команды, новые пользователи, обычные сообщения)
- AI анализ профилей
- Модерация контента
- Управление капчей
- Отправка уведомлений
- Удаление сообщений
- Логирование пользовательского флоу

**Предложения по рефакторингу:**

#### 1.1 Выделить `MessageTypeRouter` (Приоритет: ВЫСОКИЙ)
```csharp
// Новый сервис для маршрутизации сообщений по типам
public interface IMessageTypeRouter
{
    Task RouteMessageAsync(Message message, CancellationToken cancellationToken);
}
```

#### 1.2 Выделить `CommandHandler` (Приоритет: ВЫСОКИЙ)
```csharp
// Отдельный обработчик команд
public interface ICommandHandler
{
    Task HandleCommandAsync(Message message, string command, CancellationToken cancellationToken);
}
```

#### 1.3 Выделить `UserJoinHandler` (Приоритет: СРЕДНИЙ)
```csharp
// Обработчик новых пользователей
public interface IUserJoinHandler
{
    Task HandleNewUserAsync(Message message, User user, CancellationToken cancellationToken);
}
```

#### 1.4 Выделить `MessageDeletionService` (Приоритет: СРЕДНИЙ)
```csharp
// Сервис для удаления сообщений
public interface IMessageDeletionService
{
    Task DeleteMessageAsync(Message message, string reason, CancellationToken cancellationToken);
    Task DeleteMessageLaterAsync(Message message, TimeSpan delay, CancellationToken cancellationToken);
}
```

### 2. **ModerationService.cs** (888 строк) - СРЕДНЕЕ НАРУШЕНИЕ

**Проблемы:**
- Проверка контента (текст, медиа)
- Управление счетчиками сообщений
- Анализ мимикрии
- Уведомления админов
- Ограничение пользователей

**Предложения по рефакторингу:**

#### 2.1 Выделить `ContentAnalyzer` (Приоритет: ВЫСОКИЙ)
```csharp
// Анализатор контента
public interface IContentAnalyzer
{
    Task<ModerationResult> AnalyzeTextAsync(string text, Message message);
    Task<ModerationResult> AnalyzeMediaAsync(Message message);
}
```

#### 2.2 Выделить `UserBehaviorTracker` (Приоритет: СРЕДНИЙ)
```csharp
// Трекер поведения пользователей
public interface IUserBehaviorTracker
{
    Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText);
    Task HandleSuspiciousUserMessageAsync(User user, Chat chat);
    Task<bool> AnalyzeMimicryAsync(User user, Chat chat, string userKey);
}
```

#### 2.3 Выделить `UserRestrictionService` (Приоритет: НИЗКИЙ)
```csharp
// Сервис ограничений пользователей
public interface IUserRestrictionService
{
    Task<bool> RestrictUserToReadOnlyAsync(User user, Chat chat, TimeSpan duration);
    Task<bool> UnrestrictUserAsync(long userId, long chatId);
}
```

### 3. **AiChecks.cs** (675 строк) - СРЕДНЕЕ НАРУШЕНИЕ

**Проблемы:**
- Анализ профилей пользователей
- Анализ спама в сообщениях
- Кэширование результатов
- Работа с AI API

**Предложения по рефакторингу:**

#### 3.1 Выделить `ProfileAnalyzer` (Приоритет: СРЕДНИЙ)
```csharp
// Анализатор профилей пользователей
public interface IProfileAnalyzer
{
    Task<SpamPhotoBio> AnalyzeProfileAsync(User user, string? messageText = null);
    Task<SpamPhotoBio> AnalyzeEroticPhotoAsync(User user, ChatFullInfo userChat);
}
```

#### 3.2 Выделить `SpamAnalyzer` (Приоритет: СРЕДНИЙ)
```csharp
// Анализатор спама в сообщениях
public interface ISpamAnalyzer
{
    Task<SpamProbability> AnalyzeMessageAsync(Message message);
    Task<SpamProbability> AnalyzeSuspiciousUserAsync(Message message, User user, List<string> firstMessages, double mimicryScore);
}
```

#### 3.3 Выделить `AiCacheService` (Приоритет: НИЗКИЙ)
```csharp
// Сервис кэширования AI результатов
public interface IAiCacheService
{
    Task<T> GetCachedResultAsync<T>(long userId, Func<Task<T>> factory);
    void CacheResult<T>(long userId, T result);
}
```

### 4. **CallbackQueryHandler.cs** (619 строк) - СРЕДНЕЕ НАРУШЕНИЕ

**Проблемы:**
- Обработка капчи
- Админские действия
- Управление пользователями

**Предложения по рефакторингу:**

#### 4.1 Выделить `CaptchaCallbackHandler` (Приоритет: СРЕДНИЙ)
```csharp
// Обработчик капча-колбэков
public interface ICaptchaCallbackHandler
{
    Task HandleCaptchaCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken);
}
```

#### 4.2 Выделить `AdminCallbackHandler` (Приоритет: СРЕДНИЙ)
```csharp
// Обработчик админских колбэков
public interface IAdminCallbackHandler
{
    Task HandleAdminCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken);
}
```

### 5. **MessageService.cs** (466 строк) - НЕЗНАЧИТЕЛЬНОЕ НАРУШЕНИЕ

**Проблемы:**
- Отправка разных типов уведомлений
- Форматирование сообщений

**Предложения по рефакторингу:**

#### 5.1 Выделить `NotificationFormatter` (Приоритет: НИЗКИЙ)
```csharp
// Форматтер уведомлений
public interface INotificationFormatter
{
    string FormatAdminNotification(NotificationData data);
    string FormatLogNotification(NotificationData data);
    string FormatUserNotification(UserNotificationType type, object data);
}
```

### 6. **ServiceChatDispatcher.cs** (464 строки) - НЕЗНАЧИТЕЛЬНОЕ НАРУШЕНИЕ

**Проблемы:**
- Форматирование разных типов уведомлений
- Отправка в разные чаты

**Предложения по рефакторингу:**

#### 6.1 Выделить `NotificationTypeRouter` (Приоритет: НИЗКИЙ)
```csharp
// Роутер типов уведомлений
public interface INotificationTypeRouter
{
    bool ShouldSendToAdminChat(NotificationData notification);
    string FormatForChat(NotificationData notification, ChatType chatType);
}
```

## 🎯 Приоритеты рефакторинга

### ВЫСОКИЙ ПРИОРИТЕТ (Критические нарушения SRP)

1. **MessageHandler.cs** - разделить на 4-5 специализированных сервисов
2. **ModerationService.cs** - выделить ContentAnalyzer
3. **Создать MessageTypeRouter** для маршрутизации сообщений

### СРЕДНИЙ ПРИОРИТЕТ (Значительные нарушения SRP)

1. **AiChecks.cs** - разделить на ProfileAnalyzer и SpamAnalyzer
2. **CallbackQueryHandler.cs** - разделить на CaptchaCallbackHandler и AdminCallbackHandler
3. **ModerationService.cs** - выделить UserBehaviorTracker

### НИЗКИЙ ПРИОРИТЕТ (Незначительные нарушения SRP)

1. **MessageService.cs** - выделить NotificationFormatter
2. **ServiceChatDispatcher.cs** - выделить NotificationTypeRouter
3. **ModerationService.cs** - выделить UserRestrictionService
4. **AiChecks.cs** - выделить AiCacheService

## 📋 План рефакторинга

### Этап 1: Критические нарушения (2-3 недели)
1. Создать `MessageTypeRouter`
2. Выделить `CommandHandler` из MessageHandler
3. Выделить `ContentAnalyzer` из ModerationService
4. Обновить все зависимости и тесты

### Этап 2: Значительные нарушения (2-3 недели)
1. Выделить `UserJoinHandler` из MessageHandler
2. Выделить `ProfileAnalyzer` из AiChecks
3. Выделить `CaptchaCallbackHandler` из CallbackQueryHandler
4. Выделить `UserBehaviorTracker` из ModerationService

### Этап 3: Незначительные нарушения (1-2 недели)
1. Выделить `MessageDeletionService` из MessageHandler
2. Выделить `NotificationFormatter` из MessageService
3. Выделить `NotificationTypeRouter` из ServiceChatDispatcher
4. Выделить `AiCacheService` из AiChecks

## 🎯 Ожидаемые результаты

### После рефакторинга:
- **MessageHandler**: ~300-400 строк (вместо 1,555)
- **ModerationService**: ~400-500 строк (вместо 888)
- **AiChecks**: ~300-400 строк (вместо 675)
- **CallbackQueryHandler**: ~200-300 строк (вместо 619)

### Преимущества:
- ✅ Лучшая тестируемость
- ✅ Легче поддерживать и расширять
- ✅ Четкое разделение ответственности
- ✅ Возможность независимого развития компонентов
- ✅ Упрощение отладки

## 🚨 Риски и меры предосторожности

### Риски:
1. **Временное увеличение сложности** - много новых интерфейсов
2. **Потенциальные ошибки** - при разделении логики
3. **Время на тестирование** - нужно переписать много тестов

### Меры предосторожности:
1. **Поэтапный рефакторинг** - не все сразу
2. **Тщательное тестирование** - после каждого этапа
3. **Сохранение поведения** - все функции должны работать как раньше
4. **Документирование** - все изменения в changelog

## 📝 Рекомендации

1. **Начать с MessageHandler** - самый критичный файл
2. **Создать план тестирования** для каждого этапа
3. **Использовать feature branches** для каждого этапа
4. **Документировать все изменения** в архитектуре
5. **Проводить code review** после каждого этапа 