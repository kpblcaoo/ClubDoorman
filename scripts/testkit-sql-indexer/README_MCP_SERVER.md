# TestKit MCP Server

FastMCP сервер для интеграции TestKit SQL Indexer с Cursor через MCP протокол.

## Возможности

### 4 основных инструмента:

1. **`tk_search_by_name`** - Поиск методов по имени
   - Нечеткий поиск по имени метода или класса
   - Поддерживает лимит результатов
   - Возвращает детальную информацию

2. **`tk_search_by_tags`** - Поиск по тегам
   - Поиск методов по тегам (builders, mocks, etc.)
   - Поддерживает "any" и "all" режимы совпадения
   - Убирает дубликаты автоматически

3. **`tk_method_details`** - Детальная информация о методе
   - Полная информация о методе
   - Сигнатура, описание, теги, расположение
   - Поддержка статических и generic методов

4. **`tk_stats`** - Статистика базы данных
   - Общая статистика компонентов и методов
   - Топ теги
   - Распределение по категориям

## Установка

### 1. Создание виртуального окружения

```bash
cd scripts/testkit-sql-indexer
python3 -m venv venv
source venv/bin/activate
pip install fastmcp
```

### 2. Сборка сервера

```bash
chmod +x build_mcp_server.sh
./build_mcp_server.sh
```

### 3. Конфигурация Cursor

Добавьте в `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "testkitdex": {
      "command": "/path/to/ClubDoorman/scripts/testkit-sql-indexer/bin/tk-mcp",
      "env": {
        "TESTKIT_ROOT": "/path/to/ClubDoorman",
        "TESTKIT_DB_PATH": "/path/to/ClubDoorman/scripts/testkit-sql-indexer/testkit_index.db"
      }
    }
  }
}
```

### 4. Обновление индекса

```bash
# Обновить индекс TestKit
./update_testkit_index.sh

# Или вручную
python testkit_sql_indexer.py
```

## Использование

### В Cursor

После настройки MCP сервера, в Cursor будут доступны 4 инструмента:

- `tk_search_by_name` - поиск по имени
- `tk_search_by_tags` - поиск по тегам  
- `tk_method_details` - детали метода
- `tk_stats` - статистика

### Примеры запросов

**Поиск методов создания:**
```
tk_search_by_name("Create")
```

**Поиск builders:**
```
tk_search_by_tags(["builder"])
```

**Детали конкретного метода:**
```
tk_method_details("CreateMessageHandler")
```

**Статистика базы данных:**
```
tk_stats()
```

## Тестирование

### Тест сервера

```bash
python tk_fastmcp_server.py --test
```

### Тест исполняемого файла

```bash
./bin/tk-mcp --db-path testkit_index.db
```

## Архитектура

```
tk_fastmcp_server.py     # FastMCP сервер
├── TestKitFastMCPServer # Основной класс
├── _register_tools()    # Регистрация инструментов
└── 4 инструмента        # tk_search_by_name, tk_search_by_tags, 
                         # tk_method_details, tk_stats

testkit_query/           # Модуль запросов
├── TestKitQueryEngine   # Движок запросов
└── QueryResult          # Структура результата

bin/tk-mcp              # Исполняемый файл
```

## Преимущества

### ✅ Простота
- Тонкая обертка над существующим кодом
- FastMCP автоматическая валидация
- Минимальные изменения

### ✅ Производительность  
- Быстрые SQL запросы
- Кэширование результатов
- Низкая латентность

### ✅ Интеграция
- Нативная поддержка в Cursor
- Автоматическое обнаружение инструментов
- Валидация аргументов

### ✅ Расширяемость
- Легко добавить новые инструменты
- Модульная архитектура
- Совместимость с планами развития

## Устранение неполадок

### Ошибка "Database not found"
```bash
# Убедитесь, что индекс создан
python testkit_sql_indexer.py
```

### Ошибка "ImportError"
```bash
# Активируйте виртуальное окружение
source venv/bin/activate
```

### Ошибка "FastMCP not found"
```bash
# Установите FastMCP
pip install fastmcp
```

### Cursor не видит инструменты
1. Проверьте конфигурацию `~/.cursor/mcp.json`
2. Перезапустите Cursor
3. Проверьте логи в Settings → Model Context Protocol

## Развитие

### Планируемые улучшения

1. **Кэширование** - для улучшения производительности
2. **Лучшее форматирование** - для удобства чтения в Cursor
3. **Дополнительные инструменты** - поиск по категориям
4. **Автоматическое обновление** - live refresh индекса

### Добавление новых инструментов

```python
@self.mcp.tool(name="new_tool", description="New tool description")
def new_tool(param: str) -> str:
    # Реализация инструмента
    return "result"
```

## Заключение

TestKit MCP Server обеспечивает:

1. **Нативную интеграцию** TestKit с Cursor
2. **Быстрый поиск** методов и компонентов  
3. **Улучшенную разработку** с помощью AI
4. **Расширенные возможности** анализа кода

Это естественное развитие модульной архитектуры TestKit SQL Indexer. 