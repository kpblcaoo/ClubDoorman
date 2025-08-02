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

### testkit_query/
Модуль для выполнения запросов:

- **query_engine.py** - Движок запросов (TestKitQueryEngine, QueryResult)

## Использование

### Индексация

```bash
# Использование модульной версии индексера
python testkit_sql_indexer_modular.py /path/to/TestKit

# С дополнительными опциями
python testkit_sql_indexer_modular.py /path/to/TestKit --db-path custom.db --no-reports
```

### Запросы

```bash
# Интерактивный режим
python testkit_sql_query_modular.py --interactive

# Поиск по имени метода
python testkit_sql_query_modular.py --name CreateUser

# Поиск по тегу
python testkit_sql_query_modular.py --tag builder

# Поиск по типу возвращаемого значения
python testkit_sql_query_modular.py --type string

# Поиск по компоненту
python testkit_sql_query_modular.py --component UserBuilder

# Показать статистику
python testkit_sql_query_modular.py --stats

# Показать все теги
python testkit_sql_query_modular.py --tags
```

## Программное использование

### Индексация

```python
from testkit_indexer import TestKitIndexer

# Создание и запуск индексера
indexer = TestKitIndexer("/path/to/TestKit", "testkit_index.db")
result = indexer.run_indexing()

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
```

## Преимущества модульной версии

1. **Разделение ответственности** - каждый модуль отвечает за свою область
2. **Легкость тестирования** - можно тестировать каждый модуль отдельно
3. **Переиспользование** - модули можно использовать независимо
4. **Расширяемость** - легко добавлять новые функции
5. **Читаемость** - код организован логически

## Совместимость

Модульная версия полностью совместима с оригинальной версией:
- Использует ту же схему базы данных
- Генерирует те же отчеты
- Поддерживает те же функции поиска

## Миграция

Для перехода с оригинальной версии на модульную:

1. Замените вызовы `testkit_sql_indexer.py` на `testkit_sql_indexer_modular.py`
2. Замените вызовы `testkit_sql_query.py` на `testkit_sql_query_modular.py`
3. Обновите скрипты, использующие программный API

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