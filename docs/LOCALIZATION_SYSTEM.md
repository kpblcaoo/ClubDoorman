# Система локализации ClubDoorman

## Обзор

Система локализации ClubDoorman обеспечивает поддержку множественных языков для всех сообщений бота. Реализована с использованием .NET ресурсов (.resx файлов) и интегрирована в существующую архитектуру без нарушения обратной совместимости.

## Архитектура

### Компоненты

1. **IMessageLocalizer** - интерфейс для локализации сообщений
2. **MessageLocalizer** - реализация локализатора с поддержкой .NET ресурсов
3. **MessageTemplates** - расширен для поддержки локализованных шаблонов
4. **Ресурсы** - .resx файлы с переводами

### Структура ресурсов

```
Resources/
├── UserMessages.resx          # Пользовательские сообщения (EN)
├── UserMessages.ru.resx       # Пользовательские сообщения (RU)
├── AdminMessages.resx         # Админские сообщения (EN)
├── AdminMessages.ru.resx      # Админские сообщения (RU)
├── SystemMessages.resx        # Системные сообщения (EN)
└── SystemMessages.ru.resx     # Системные сообщения (RU)
```

## Использование

### Базовое использование

```csharp
// Получение локализованного шаблона
var template = messageTemplates.GetLocalizedUserTemplate(UserNotificationType.Welcome, chatId);

// Форматирование с данными
var message = messageTemplates.FormatNotificationTemplate(template, notificationData);
```

### Методы локализации

#### MessageTemplates
- `GetLocalizedUserTemplate(type, chatId)` - пользовательские сообщения
- `GetLocalizedAdminTemplate(type, chatId)` - админские сообщения  
- `GetLocalizedLogTemplate(type, chatId)` - лог-сообщения

#### MessageLocalizer
- `User(key, chatId, args)` - пользовательские сообщения
- `Admin(key, chatId, args)` - админские сообщения
- `System(key, args)` - системные сообщения

### Fallback механизм

Если локализатор недоступен или ключ не найден:
1. Возвращается оригинальный шаблон из `MessageTemplates`
2. Логируется предупреждение
3. Приложение продолжает работать без сбоев

## Добавление новых языков

### 1. Создание ресурсов

Создайте файлы ресурсов для нового языка:

```xml
<!-- Resources/UserMessages.es.resx -->
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="WelcomeMessage" xml:space="preserve">
    <value>¡Hola! Soy un bot anti-spam moderno para Telegram</value>
  </data>
</root>
```

### 2. Регистрация культуры

Добавьте поддержку культуры в `MessageLocalizer`:

```csharp
private CultureInfo GetCultureForChat(long chatId)
{
    // Логика определения языка чата
    return new CultureInfo("es-ES");
}
```

## Добавление новых ключей

### 1. Добавление в ресурсы

```xml
<!-- Resources/UserMessages.resx -->
<data name="NewFeature" xml:space="preserve">
  <value>New feature: {0}</value>
</data>
```

### 2. Переводы

```xml
<!-- Resources/UserMessages.ru.resx -->
<data name="NewFeature" xml:space="preserve">
  <value>Новая функция: {0}</value>
</data>
```

### 3. Использование в коде

```csharp
var template = messageTemplates.GetLocalizedUserTemplate(UserNotificationType.NewFeature, chatId);
var message = messageTemplates.FormatNotificationTemplate(template, data);
```

## Форматирование

### Индексированные плейсхолдеры

Ресурсы используют индексированные плейсхолдеры:

```xml
<value>User {0} from chat {1}: {2}</value>
```

### Форматирование в коде

```csharp
// Автоматическое форматирование через MessageTemplates
var data = new NotificationData(user, chat, reason);
var message = messageTemplates.FormatNotificationTemplate(template, data);
```

## Конфигурация

### Регистрация в DI

```csharp
services.AddSingleton<IMessageLocalizer, MessageLocalizer>();
services.AddSingleton<MessageTemplates>(provider => 
    new MessageTemplates(provider.GetRequiredService<IMessageLocalizer>()));
```

### Настройка проекта

```xml
<!-- ClubDoorman.csproj -->
<PropertyGroup>
  <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
</PropertyGroup>

<ItemGroup>
  <EmbeddedResource Include="Resources\*.resx">
    <Generator>ResXFileCodeGenerator</Generator>
    <LastGenOutput>%(Filename).Designer.cs</LastGenOutput>
  </EmbeddedResource>
</ItemGroup>
```

## Тестирование

### Тесты локализатора

```csharp
[Test]
public void User_ValidKey_ReturnsLocalizedMessage()
{
    var result = _localizer.User("CaptchaPrompt", chatId, "ABC123");
    Assert.That(result, Does.Contain("ABC123"));
}
```

### Тесты интеграции

```csharp
[Test]
public void GetLocalizedUserTemplate_WithLocalizer_ReturnsLocalizedMessage()
{
    var result = _templates.GetLocalizedUserTemplate(UserNotificationType.Welcome, chatId);
    Assert.That(result, Is.Not.EqualTo(_templates.GetUserTemplate(UserNotificationType.Welcome)));
}
```

## Лучшие практики

### 1. Ключи ресурсов
- Используйте описательные имена ключей
- Следуйте конвенции именования: `VerbNoun` или `NounAction`
- Группируйте связанные ключи по префиксам

### 2. Переводы
- Сохраняйте контекст при переводе
- Учитывайте длину текста в разных языках
- Тестируйте форматирование с реальными данными

### 3. Производительность
- Ресурсы кэшируются в памяти
- Избегайте частого создания новых экземпляров локализатора
- Используйте пул объектов для часто используемых шаблонов

### 4. Обратная совместимость
- Всегда сохраняйте fallback на оригинальные шаблоны
- Не удаляйте существующие ключи без миграции
- Тестируйте с отключенным локализатором

## Устранение неполадок

### Ключ не найден
```
Missing key: NonExistentKey in resource UserMessages
```
**Решение**: Добавьте ключ в соответствующий .resx файл

### Неправильное форматирование
```
String.Format exception: Index (zero based) must be greater than or equal to zero
```
**Решение**: Проверьте количество плейсхолдеров в ресурсе и передаваемых аргументов

### Культура не поддерживается
```
Culture 'xx-XX' is not supported
```
**Решение**: Добавьте поддержку культуры в `GetCultureForChat` или используйте fallback

## Будущие улучшения

1. **Определение языка чата** - автоматическое определение языка по настройкам чата
2. **Кэширование переводов** - улучшенное кэширование для производительности
3. **Валидация ресурсов** - проверка полноты переводов при сборке
4. **Веб-интерфейс** - управление переводами через веб-интерфейс
5. **Поддержка RTL** - поддержка языков с письмом справа налево 