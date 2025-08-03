# TestKit SQL Indexer - Modular Version

Модульная версия индексера TestKit с разделением на компоненты для лучшей поддерживаемости и расширяемости.

## Структура модулей

### testkit_indexer/
Основной модуль для индексации:

- **models.py** - Модели данных (TestKitMethod, TestKitComponent, TestKitIndex)
- **parser.py** - Парсер C# файлов (CSharpParser)
- **scanner.py** - Сканер файлов (TestKitScanner)
- **database.py** - Работа с базой данных (DatabaseManager)
- **reporter.py** - Генерация отчетов (ReportGenerator)
- **indexer.py** - Основной класс индексера (TestKitIndexer)
- **deduplicator.py** - Модуль дедупликации (DeduplicationEngine, DeduplicationStrategy)

### testkit_query/
Модуль для выполнения запросов:

- **query_engine.py** - Движок запросов (TestKitQueryEngine, QueryResult)

## Использование

### Индексация

```bash
# Базовое использование
python testkit_sql_indexer.py /path/to/TestKit

# С дедупликацией (по умолчанию file_hash)
python testkit_sql_indexer.py /path/to/TestKit --deduplication-strategy signature

# Без дедупликации
python testkit_sql_indexer.py /path/to/TestKit --no-deduplicate

# С кастомной БД
python testkit_sql_indexer.py /path/to/TestKit --db-path custom.db

# Без генерации отчетов
python testkit_sql_indexer.py /path/to/TestKit --no-reports
```

### Стратегии дедупликации

- **file_hash** (по умолчанию) - на основе SHA256 хеша файла
- **signature** - на основе хеша сигнатур классов и методов
- **content** - на основе хеша очищенного содержимого файла
- **none** - без дедупликации

### Запросы

```bash
# Интерактивный режим
python testkit_sql_query.py --interactive

# Поиск по имени метода
python testkit_sql_query.py --name CreateUser

# Поиск по тегу
python testkit_sql_query.py --tag builder

# Поиск по типу возвращаемого значения
python testkit_sql_query.py --type string

# Поиск по компоненту
python testkit_sql_query.py --component UserBuilder

# Показать статистику
python testkit_sql_query.py --stats

# Показать все теги
python testkit_sql_query.py --tags
```

### Интерактивные команды

В интерактивном режиме доступны следующие команды:

```
name <method_name>     - Поиск по имени метода
tag <tag_name>         - Поиск по тегу
type <return_type>     - Поиск по типу возврата
component <class_name> - Поиск по компоненту/классу
tags                   - Показать все теги
stats                  - Показать статистику БД
duplicates             - Показать дубликаты файлов
duplicate-stats        - Показать статистику дубликатов
quit                   - Выход
```

## MCP Интеграция

TestKit SQL Indexer интегрирован с MCP (Model Context Protocol) для использования в AI-ассистентах:

### Настройка MCP

1. **Конфигурация** в `.cursor/mcp.json`:
```json
{
  "mcpServers": {
    "testkitdex": {
      "command": "/path/to/testkit-sql-indexer/bin/tk-mcp",
      "env": {
        "TESTKIT_ROOT": "/path/to/project",
        "TESTKIT_DB_PATH": "/path/to/testkit-sql-indexer/testkit_index.db"
      }
    }
  }
}
```

2. **Пути к БД**:
   - **По умолчанию**: `testkit_index.db` (в текущей директории)
   - **Переменная окружения**: `TESTKIT_DB_PATH`
   - **Аргумент командной строки**: `--db-path`

### Обновление БД для MCP

```bash
# 1. Создать новую БД
python testkit_sql_indexer.py ../../ClubDoorman.Test/TestKit \
  --db-path test_mcp.db \
  --deduplication-strategy signature

# 2. Скопировать как БД по умолчанию для MCP
cp test_mcp.db testkit_index.db

# 3. Проверить, что MCP инструменты работают
python testkit_sql_query.py --db-path testkit_index.db --stats
```

### Доступные MCP инструменты

- **tk_stats** - статистика базы данных
- **tk_search_by_name** - поиск методов по имени
- **tk_search_by_tags** - поиск методов по тегам
- **tk_method_details** - детали метода
- **tk_get_guidance** - руководство по использованию
- **tk_search_similar** - поиск похожих методов
- **tk_get_context_methods** - методы для контекста
- **tk_get_tag_guidance** - руководство по тегам

### Пример использования в AI

```python
# Получение статистики
stats = mcp_testkitdex_tk_stats(random_string="check")

# Поиск методов
methods = mcp_testkitdex_tk_search_by_name(query="Create", limit=5)

# Детали метода
details = mcp_testkitdex_tk_method_details(name="CreateMessageHandlerBuilder")

# Руководство по использованию
guidance = mcp_testkitdex_tk_get_guidance(method_name="CreateMessageHandlerBuilder")
```

### Запуск MCP сервера

```bash
# Активировать виртуальное окружение
source venv/bin/activate

# Запустить MCP сервер
python bin/tk-mcp --db-path testkit_index.db

# Или с переменной окружения
TESTKIT_DB_PATH=testkit_index.db python bin/tk-mcp
```

## Программное использование

### Индексация

```python
from testkit_indexer import TestKitIndexer
from testkit_indexer.deduplicator import SignatureDeduplicator

# Создание и запуск индексера с дедупликацией
indexer = TestKitIndexer("/path/to/TestKit", "testkit_index.db")
indexer.deduplication_strategy = SignatureDeduplicator()
result = indexer.run_indexing(deduplicate=True)

print(f"Indexed {result['components']} components")
print(f"Found {result['methods']} methods")
print(f"Unique tags: {result['tags']}")
```

### Запросы

```python
from testkit_query import TestKitQueryEngine

# Создание движка запросов
query_engine = TestKitQueryEngine("testkit_index.db")

# Поиск методов по имени
results = query_engine.search_methods_by_name("CreateUser")

# Поиск методов по тегу
results = query_engine.search_methods_by_tag("builder")

# Получение статистики
stats = query_engine.get_statistics()

# Получение всех тегов
tags = query_engine.get_all_tags()

# Поиск дубликатов
duplicates = query_engine.find_duplicates()

# Статистика дубликатов
dup_stats = query_engine.get_duplicate_statistics()
```

### Дедупликация

```python
from testkit_indexer.deduplicator import (
    DeduplicationEngine, 
    FileHashDeduplicator,
    SignatureDeduplicator,
    ContentDeduplicator
)

# Создание движка дедупликации
engine = DeduplicationEngine(SignatureDeduplicator())

# Анализ дубликатов
analyzer = DeduplicationAnalyzer()
duplicate_groups = analyzer.analyze_duplicates(components)

# Применение дедупликации
deduplicated = engine.deduplicate(components)
```

## Преимущества модульной версии

1. **Разделение ответственности** - каждый модуль отвечает за свою область
2. **Легкость тестирования** - можно тестировать каждый модуль отдельно
3. **Переиспользование** - модули можно использовать независимо
4. **Расширяемость** - легко добавлять новые функции
5. **Читаемость** - код организован логически
6. **Дедупликация** - автоматическое удаление дубликатов
7. **MCP интеграция** - поддержка AI-ассистентов

## Совместимость

Модульная версия полностью совместима с оригинальной версией:
- Использует ту же схему базы данных
- Генерирует те же отчеты
- Поддерживает те же функции поиска
- Добавляет новые возможности дедупликации и MCP

## Разработка

### Добавление нового парсера

```python
# Создайте новый класс парсера
class CustomParser(CSharpParser):
    def extract_custom_info(self, content: str):
        # Ваша логика
        pass

# Используйте в сканере
scanner = TestKitScanner("/path/to/TestKit")
scanner.parser = CustomParser()
```

### Добавление новой стратегии дедупликации

```python
from testkit_indexer.deduplicator import DeduplicationStrategy

class CustomDeduplicator(DeduplicationStrategy):
    def get_component_key(self, component: TestKitComponent) -> str:
        # Ваша логика генерации ключа
        return custom_key
    
    def get_name(self) -> str:
        return "custom"
```

### Добавление нового типа запроса

```python
# В TestKitQueryEngine добавьте новый метод
def search_methods_by_custom_criteria(self, criteria: str):
    # Ваша логика запроса
    pass
```

### Добавление нового отчета

```python
# В ReportGenerator добавьте новый метод
def generate_custom_report(self, components: List[TestKitComponent]) -> str:
    # Ваша логика генерации отчета
    pass
```

## Производительность

### Оптимизации

- **Быстрая дедупликация** - алгоритм оптимизирован для работы с тысячами компонентов
- **Кэширование хешей** - избежание повторных вычислений
- **Индексы БД** - быстрые запросы по хешам и тегам
- **Модульная архитектура** - независимая работа компонентов

### Статистика производительности

- **Индексация**: ~13 секунд для 31 файла (31 компонент, 389 методов)
- **Дедупликация**: ~0.0002 секунды для 31 компонента
- **Поиск**: <0.1 секунды для большинства запросов
- **MCP запросы**: <0.05 секунды

## Примеры использования

### Полный цикл индексации и поиска

```bash
# 1. Индексация с дедупликацией
python testkit_sql_indexer.py ../../ClubDoorman.Test/TestKit \
  --db-path testkit.db \
  --deduplication-strategy signature

# 2. Поиск методов
python testkit_sql_query.py --db-path testkit.db --name Create

# 3. Статистика
python testkit_sql_query.py --db-path testkit.db --stats

# 4. Интерактивный режим
python testkit_sql_query.py --db-path testkit.db --interactive
```

### Настройка MCP для AI-ассистента

```bash
# 1. Создать актуальную БД
python testkit_sql_indexer.py ../../ClubDoorman.Test/TestKit \
  --db-path test_mcp.db \
  --deduplication-strategy signature

# 2. Скопировать как БД по умолчанию для MCP
cp test_mcp.db testkit_index.db

# 3. Проверить MCP инструменты
python testkit_sql_query.py --db-path testkit_index.db --stats
```

### Программное использование

```python
from testkit_indexer import TestKitIndexer
from testkit_query import TestKitQueryEngine

# Индексация
indexer = TestKitIndexer("../../ClubDoorman.Test/TestKit", "testkit.db")
result = indexer.run_indexing(deduplicate=True)

# Запросы
query_engine = TestKitQueryEngine("testkit.db")
methods = query_engine.search_methods_by_tag("factory")
stats = query_engine.get_statistics()

print(f"Found {len(methods)} factory methods")
print(f"Database contains {stats['total_methods']} methods")
```

## Устранение неполадок

### MCP инструменты не работают

1. **Проверьте БД**: убедитесь, что `testkit_index.db` существует и актуальна
2. **Обновите БД**: создайте новую БД и скопируйте как `testkit_index.db`
3. **Проверьте конфигурацию**: убедитесь, что путь в `.cursor/mcp.json` правильный
4. **Перезапустите MCP сервер**: остановите и запустите сервер заново

### Ошибки импорта

1. **Активируйте виртуальное окружение**: `source venv/bin/activate`
2. **Проверьте зависимости**: `pip install -r requirements.txt`
3. **Проверьте пути**: убедитесь, что модули доступны в `sys.path`

### Медленная работа

1. **Используйте дедупликацию**: уменьшает размер БД
2. **Оптимизируйте запросы**: используйте индексы
3. **Кэшируйте результаты**: избегайте повторных вычислений 