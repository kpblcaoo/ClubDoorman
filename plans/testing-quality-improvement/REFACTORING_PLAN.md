# 🎯 План рефакторинга качества тестирования ClubDoorman

## 📊 Текущее состояние (метрики)

| Метрика | Текущий уровень | Цель | Приоритет |
|---------|----------------|------|-----------|
| **Интерфейсы** | 20 интерфейсов | Анализ избыточности | 🔴 Высокий |
| **Моки в тестах** | 50+ `new Mock<T>()` | < 20 через TestFactory | 🟡 Средний |
| **Необязательные параметры** | 40+ методов | < 10 через Request объекты | 🟡 Средний |
| **Приватные async методы** | 50+ методов | < 15 через сервисы | 🔴 Высокий |
| **Статические свойства** | Config.cs проблемы | IAppConfig wrapper | 🔴 Высокий |

---

## 🎯 Эпик: "Улучшение качества тестирования"

### Phase 1: Анализ и подготовка (1-2 дня)

#### 🔴 1.1 Аудит интерфейсов
```bash
# Задачи:
- Проанализировать 20 интерфейсов на избыточность
- Определить какие интерфейсы реально нужны для DI/тестирования
- Выявить интерфейсы без альтернативных реализаций
```

**Кандидаты на удаление:**
- `IBadMessageManager` - только одна реализация
- `IChatLinkFormatter` - утилитарный класс
- `ILoggingConfigurationService` - возможно избыточен

#### 🔴 1.2 Аудит приватных async методов
```bash
# Найти топ-5 классов с наибольшим количеством приватных async методов:
- Worker.cs (8 методов)
- MessageHandler.cs (15+ методов) 
- CallbackQueryHandler.cs (9 методов)
- ModerationService.cs (7 методов)
```

### Phase 2: Критические исправления (3-5 дней)

#### 🔴 2.1 Исправить статические свойства Config.cs
```csharp
// Проблема: Config.OpenRouterApi ломает тесты
// Решение: IAppConfig wrapper

public interface IAppConfig
{
    string OpenRouterApi { get; }
    string BotToken { get; }
    // ... другие свойства
}

public class AppConfig : IAppConfig
{
    public string OpenRouterApi => Config.OpenRouterApi;
    // ...
}
```

**Файлы для изменения:**
- `ClubDoorman/Infrastructure/Config.cs`
- `ClubDoorman/Program.cs` (DI регистрация)
- Все тесты, использующие `Config.OpenRouterApi`

#### 🔴 2.2 Вынести CaptchaFlow в отдельный сервис
```csharp
// Из Worker.cs вынести:
private async Task CaptchaLoop(CancellationToken token)

// В новый сервис:
public interface ICaptchaFlowService
{
    Task StartLoopAsync(CancellationToken cancellationToken);
    Task StopLoopAsync();
}

public class CaptchaFlowService : ICaptchaFlowService
{
    // Логика капчи
}
```

**Файлы для изменения:**
- `ClubDoorman/Worker.cs`
- `ClubDoorman/Services/ICaptchaFlowService.cs` (новый)
- `ClubDoorman/Services/CaptchaFlowService.cs` (новый)
- `ClubDoorman/Program.cs` (DI)
- Тесты для нового сервиса

### Phase 3: Упрощение API (5-7 дней)

#### 🟡 3.1 Заменить сложные необязательные параметры
```csharp
// До:
public async Task SendErrorNotificationAsync(Exception ex, string context, User? user = null, Chat? chat = null, CancellationToken cancellationToken = default)

// После:
public record ErrorNotificationRequest(Exception Exception, string Context, User? User = null, Chat? Chat = null);

public async Task SendErrorNotificationAsync(ErrorNotificationRequest request, CancellationToken cancellationToken = default)
```

**Кандидаты для рефакторинга:**
- `IMessageService.SendErrorNotificationAsync`
- `ITelegramBotClientWrapper.AnswerCallbackQuery`
- `ITelegramBotClientWrapper.EditMessageText`
- `IUserFlowLogger.LogSystemError`

#### 🟡 3.2 Улучшить TestFactory паттерн
```csharp
// Заменить прямые new Mock<T>() на TestFactory методы
// Вместо:
var mock = new Mock<IUserManager>();

// Использовать:
var factory = new ModerationServiceTestFactory();
var service = factory.CreateModerationService();
```

**Файлы для улучшения:**
- `ClubDoorman.Test/ModerationServiceSimpleTests.cs`
- `ClubDoorman.Test/StepDefinitions/BasicModerationSteps.cs`
- `ClubDoorman.Test/Unit/Services/CaptchaServiceFakeTests.cs`

### Phase 4: Декомпозиция больших классов (7-10 дней)

#### 🔴 4.1 Разбить MessageHandler
```csharp
// Вынести из MessageHandler.cs:
- HandleCommandAsync → ICommandProcessor
- HandleNewMembersAsync → INewMemberProcessor  
- HandleUserMessageAsync → IUserMessageProcessor
- HandleChannelMessageAsync → IChannelMessageProcessor
```

#### 🔴 4.2 Разбить CallbackQueryHandler
```csharp
// Вынести из CallbackQueryHandler.cs:
- HandleCaptchaCallback → ICaptchaCallbackProcessor
- HandleAdminCallback → IAdminCallbackProcessor
- HandleSuspiciousUserCallback → ISuspiciousUserCallbackProcessor
```

---

## 📋 Чеклист для каждого этапа

### Phase 1: Анализ
- [ ] Составить список избыточных интерфейсов
- [ ] Подсчитать приватные async методы по классам
- [ ] Создать план миграции для каждого класса

### Phase 2: Критические исправления
- [ ] Создать `IAppConfig` и заменить статические свойства
- [ ] Вынести `CaptchaFlowService`
- [ ] Обновить все тесты для работы с новыми интерфейсами

### Phase 3: Упрощение API
- [ ] Создать Request объекты для сложных методов
- [ ] Обновить TestFactory для всех сервисов
- [ ] Убрать прямые `new Mock<T>()` из тестов

### Phase 4: Декомпозиция
- [ ] Разбить MessageHandler на 4-5 сервисов
- [ ] Разбить CallbackQueryHandler на 3-4 сервиса
- [ ] Создать тесты для каждого нового сервиса

---

## 🎯 Ожидаемые результаты

| Метрика | До | После | Улучшение |
|---------|-----|-------|-----------|
| Покрытие тестами | ~30% | >50% | +67% |
| Приватные async методы | 50+ | <15 | -70% |
| Прямые моки в тестах | 50+ | <20 | -60% |
| Необязательные параметры | 40+ | <10 | -75% |
| Время выполнения тестов | 6.3с | <4с | -37% |

---

## 📝 Worklog

### 2025-01-XX - Создание плана
- [x] Создан детальный план рефакторинга
- [x] Определены приоритеты и этапы
- [x] Подготовлена структура для отслеживания прогресса

### Следующие шаги:
- [ ] Создать ветку `refactor/testing-quality-improvement`
- [ ] Начать Phase 1: Аудит интерфейсов
- [ ] Составить детальный анализ каждого интерфейса 