# Enhanced TestKit MCP Tools

## 🚀 Новые возможности

### 1. **Полные сигнатуры методов**
```bash
tk_method_signature "CreateSpamMessage"
tk_method_signature "SendMessage" "MessageHandler"
```

### 2. **Поиск примеров использования**
```bash
tk_search_examples "CreateSpamMessage"
tk_search_examples "MessageHandler" context="test"
```

### 3. **Поиск похожих методов**
```bash
tk_search_similar "CreateSpamMessage"
```

### 4. **Поиск по контексту**
```bash
tk_search_context "integration"
tk_search_context "unit"
```

### 5. **Улучшенная детальная информация**
```bash
tk_method_details "CreateSpamMessage"
```

## 📊 Что изменилось

### До улучшений:
- ❌ Только базовые сигнатуры без параметров
- ❌ Нет примеров использования
- ❌ Сложно найти похожие методы
- ❌ Нет контекстного поиска

### После улучшений:
- ✅ Полные сигнатуры с параметрами
- ✅ Примеры использования из реальных тестов
- ✅ Поиск похожих методов
- ✅ Контекстный поиск (test, integration, unit)
- ✅ Улучшенная навигация

## 🔧 Как использовать

### 1. Обновление индекса с примерами
```bash
cd scripts/testkit-sql-indexer
./update_testkit_index_with_examples.sh
```

### 2. Запуск MCP сервера
```bash
python3 tk_fastmcp_server.py
```

### 3. Использование новых инструментов

#### Поиск методов с примерами:
```bash
tk_method_details "CreateSpamMessage"
```
Вывод:
```
# TestKit.CreateSpamMessage

## Full Signature
```csharp
public static Message CreateSpamMessage(string text, long chatId, int messageId)
```

## Usage Examples
### Example 1
**File:** `ClubDoorman.Test/Integration/ModerationServiceTests.cs:45`
**Context:** integration
```csharp
// Line 45
var message = TK.CreateSpamMessage("spam text", chatId, messageId);
```

### Example 2
**File:** `ClubDoorman.Test/Unit/MessageHandlerTests.cs:23`
**Context:** unit
```csharp
// Line 23
var spamMessage = TK.CreateSpamMessage("test spam", 123, 456);
```

## Tags
builder, message, spam
```

#### Поиск примеров по контексту:
```bash
tk_search_context "integration"
```

#### Поиск похожих методов:
```bash
tk_search_similar "CreateSpamMessage"
```

## 📈 Ожидаемые улучшения

### Время разработки:
- **До:** 30+ минут на написание теста
- **После:** 5-10 минут на написание теста

### Точность сигнатур:
- **До:** 60% правильных сигнатур
- **После:** 95% правильных сигнатур

### Количество ошибок компиляции:
- **До:** 5-7 попыток на тест
- **После:** 1-2 попытки на тест

## 🛠 Технические детали

### База данных
- Новая таблица `usage_examples` для хранения примеров
- Поле `full_signature` в таблице `methods`
- Индексы для быстрого поиска

### Парсер
- Извлечение полных сигнатур методов
- Анализ использования в тестах
- Определение контекста (test, integration, unit)

### MCP инструменты
- `tk_search_examples` - поиск примеров
- `tk_method_signature` - полные сигнатуры
- `tk_search_similar` - похожие методы
- `tk_search_context` - поиск по контексту

## 🎯 Следующие шаги

1. **Тестирование** новых возможностей
2. **Сбор обратной связи** от команды
3. **Дополнительные улучшения**:
   - Шаблоны тестов
   - Анализ зависимостей
   - Fuzzy search
   - Автодополнение

## 📝 Примеры использования

### Сценарий 1: Написание нового теста
```bash
# 1. Поиск подходящего метода
tk_search_by_name "CreateMessage"

# 2. Получение полной сигнатуры
tk_method_signature "CreateMessage"

# 3. Поиск примеров использования
tk_search_examples "CreateMessage"

# 4. Поиск похожих методов
tk_search_similar "CreateMessage"
```

### Сценарий 2: Анализ существующих тестов
```bash
# 1. Поиск интеграционных тестов
tk_search_context "integration"

# 2. Поиск unit тестов
tk_search_context "unit"

# 3. Поиск по тегам
tk_search_by_tags ["builder", "message"]
```

## 🔍 Отладка

### Проверка базы данных:
```bash
python3 testkit_sql_query.py --stats
```

### Проверка примеров:
```bash
python3 testkit_sql_query.py --examples "CreateSpamMessage"
```

### Пересоздание индекса:
```bash
rm testkit_index.db
./update_testkit_index_with_examples.sh
``` 