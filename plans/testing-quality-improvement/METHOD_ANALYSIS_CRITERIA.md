# 🔍 Критерии анализа методов для выноса

## ✅ Когда ВЫНОСИТЬ метод в отдельный сервис

### 1. Сложность логики
- **Метод > 20 строк** и обрабатывает бизнес-логику
- **Множественные условия** (if/else, switch с >3 веток)
- **Сложные вычисления** или алгоритмы

### 2. Зависимости от внешних сервисов
- **Вызовы AI сервисов** (IAiChecks, ISpamHamClassifier)
- **Работа с базой данных** или файловой системой
- **HTTP запросы** или внешние API
- **Асинхронные операции** (async/await)

### 3. Переиспользуемость
- **Логика нужна в нескольких местах**
- **Планируется альтернативная реализация**
- **Метод может быть вызван из других компонентов**

### 4. Тестируемость
- **Сложно тестировать** из-за множественных зависимостей
- **Нужны сложные моки** для покрытия всех сценариев
- **Логика скрыта** в приватных методах

---

## ❌ Когда НЕ выносить метод

### 1. Простота логики
- **Метод < 10 строк** и простые проверки
- **Простое пересылание** вызовов другим сервисам
- **Проверка флагов** без сложной логики

### 2. Специфичность контекста
- **Логика привязана** к конкретному обработчику
- **Нет планов** на переиспользование
- **Метод только маршрутизирует** запросы

### 3. Избыточность
- **Вынос создаст больше проблем** чем решит
- **Усложнит DI** без существенных преимуществ
- **Увеличит количество файлов** без пользы

---

## 🟡 Промежуточные варианты

### 1. Вынос чистой бизнес-логики
```csharp
// Вместо полного сервиса - вынести только логику:
public class CommandEvaluator
{
    public CommandDecision Evaluate(string commandText, User user, Chat chat)
    {
        // Чистая логика без Telegram зависимостей
        return new CommandDecision { Reply = "...", Action = CommandAction.Allow };
    }
}

// В обработчике:
private async Task HandleCommandAsync(Message message)
{
    var decision = _commandEvaluator.Evaluate(message.Text, message.From, message.Chat);
    await _telegramClient.SendText(message.Chat.Id, decision.Reply);
}
```

### 2. Разделение ответственности
```csharp
// Разделить на логику + взаимодействие:
private async Task HandleCommandAsync(Message message)
{
    // Логика принятия решения
    var shouldAllow = _commandValidator.Validate(message.Text, message.From);
    
    // Взаимодействие с Telegram
    if (shouldAllow)
        await _telegramClient.SendText(message.Chat.Id, "Команда разрешена");
    else
        await _telegramClient.SendText(message.Chat.Id, "Команда запрещена");
}
```

### 3. Упрощение существующих методов
```csharp
// Вместо выноса - упростить:
private async Task HandleSimpleCommandAsync(Message message)
{
    var decision = _commandEvaluator.Evaluate(message.Text);
    await _telegramClient.SendText(message.Chat.Id, decision.Reply);
}
```

---

## 📊 Матрица принятия решений

| Критерий | Вес | Баллы |
|----------|-----|-------|
| Метод > 20 строк | 3 | 0-3 |
| Вызовы внешних сервисов | 3 | 0-3 |
| Сложная логика | 2 | 0-2 |
| Переиспользуемость | 2 | 0-2 |
| Проблемы с тестированием | 2 | 0-2 |
| **ИТОГО** | **12** | **0-12** |

### Решение:
- **0-4 балла**: ❌ НЕ выносить
- **5-8 баллов**: 🟡 Промежуточный вариант
- **9-12 баллов**: ✅ Выносить в сервис

---

## 🎯 Примеры анализа

### Пример 1: HandleCommandAsync
```csharp
private async Task HandleCommandAsync(Message message)
{
    // 15 строк логики проверки команд
    // Вызовы IAiChecks
    // Сложные условия
    // Асинхронные операции
}
```
**Анализ:** 9-10 баллов → ✅ Выносить в `ICommandProcessor`

### Пример 2: HandleSimpleCallback
```csharp
private async Task HandleSimpleCallback(CallbackQuery callback)
{
    // 5 строк простой логики
    // Простое пересылание
    // Нет сложных условий
}
```
**Анализ:** 2-3 балла → ❌ НЕ выносить

### Пример 3: HandleUserMessage
```csharp
private async Task HandleUserMessage(Message message)
{
    // 25 строк логики
    // Вызовы модерации
    // Но специфично для MessageHandler
}
```
**Анализ:** 7-8 баллов → 🟡 Промежуточный вариант (вынести логику) 