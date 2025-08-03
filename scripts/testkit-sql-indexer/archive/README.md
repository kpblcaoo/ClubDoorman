# TestKit SQL Indexer

Инструмент для создания и использования SQLite базы данных для индексации TestKit компонентов.

## Обзор

Вместо JSON индекса, который медленно искать и анализировать, SQL индексер создает структурированную SQLite базу данных с возможностью быстрого поиска, полнотекстового поиска и сложных запросов.

## Компоненты

### 1. testkit_sql_indexer.py
Генератор SQLite базы данных из TestKit файлов.

**Использование:**
```bash
python scripts/testkit_sql_indexer.py
```

**Создает:**
- `testkit_index.db` - SQLite база данных с индексами

### 2. testkit_sql_query.py
Утилита для выполнения запросов к базе данных.

**Использование:**
```bash
python scripts/testkit_sql_query.py [опции]
```

## Схема базы данных

### Таблицы

#### components
Компоненты (файлы/классы)
- `id` - первичный ключ
- `file_path` - путь к файлу
- `file_name` - имя файла
- `class_name` - имя класса
- `class_description` - описание класса
- `category` - категория (core, mocks, builders, etc.)
- `lines_count` - количество строк
- `created_at`, `updated_at` - временные метки

#### methods
Методы
- `id` - первичный ключ
- `component_id` - внешний ключ к components
- `name` - имя метода
- `return_type` - тип возврата
- `signature` - полная сигнатура
- `description` - описание метода
- `line_number` - номер строки
- `is_static` - статический метод
- `is_generic` - generic метод
- `created_at` - временная метка

#### tags
Теги
- `id` - первичный ключ
- `name` - имя тега (уникальное)
- `category` - категория тега
- `description` - описание тега
- `usage_count` - количество использований
- `created_at` - временная метка

#### method_tags
Связь многие-ко-многим между методами и тегами
- `method_id` - внешний ключ к methods
- `tag_id` - внешний ключ к tags

#### methods_fts
FTS5 виртуальная таблица для полнотекстового поиска
- `name` - имя метода
- `description` - описание
- `signature` - сигнатура

### Индексы
- `idx_methods_name` - поиск по имени метода
- `idx_methods_return_type` - поиск по типу возврата
- `idx_tags_name` - поиск по имени тега
- `idx_tags_category` - поиск по категории тега
- `idx_components_category` - поиск по категории компонента

## Примеры использования

### Поиск методов по имени
```bash
python scripts/testkit_sql_query.py --name "Create"
```

### Поиск методов по тегу
```bash
python scripts/testkit_sql_query.py --tag "mock"
```

### Поиск методов по категории
```bash
python scripts/testkit_sql_query.py --category "core"
```

### Поиск методов по типу возврата
```bash
python scripts/testkit_sql_query.py --return-type "Message"
```

### Полнотекстовый поиск
```bash
python scripts/testkit_sql_query.py --full-text "message"
```

### Поиск методов с несколькими тегами
```bash
python scripts/testkit_sql_query.py --tags mock factory
```

### Показать популярные теги
```bash
python scripts/testkit_sql_query.py --popular-tags 10
```

### Показать сводку по категориям
```bash
python scripts/testkit_sql_query.py --categories
```

### Поиск методов в конкретном компоненте
```bash
python scripts/testkit_sql_query.py --component "Main"
```

### Выполнить произвольный SQL запрос
```bash
python scripts/testkit_sql_query.py --query "SELECT * FROM methods WHERE is_static = 1 LIMIT 5"
```

## Полезные SQL запросы

### Найти все методы с тегом "factory"
```sql
SELECT m.name, m.return_type, c.file_path, c.class_name
FROM methods m
JOIN components c ON m.component_id = c.id
JOIN method_tags mt ON m.id = mt.method_id
JOIN tags t ON mt.tag_id = t.id
WHERE t.name = 'factory'
ORDER BY m.name;
```

### Найти методы, которые возвращают Message
```sql
SELECT m.name, m.signature, c.file_path, c.class_name
FROM methods m
JOIN components c ON m.component_id = c.id
WHERE m.return_type LIKE '%Message%'
ORDER BY m.name;
```

### Статистика по категориям
```sql
SELECT 
    c.category,
    COUNT(DISTINCT c.id) as components_count,
    COUNT(m.id) as methods_count,
    AVG(c.lines_count) as avg_lines
FROM components c
LEFT JOIN methods m ON c.id = m.component_id
GROUP BY c.category
ORDER BY components_count DESC;
```

### Найти методы с наибольшим количеством тегов
```sql
SELECT 
    m.name,
    m.return_type,
    c.file_path,
    COUNT(mt.tag_id) as tag_count,
    GROUP_CONCAT(t.name) as tags
FROM methods m
JOIN components c ON m.component_id = c.id
JOIN method_tags mt ON m.id = mt.method_id
JOIN tags t ON mt.tag_id = t.id
GROUP BY m.id
ORDER BY tag_count DESC
LIMIT 10;
```

### Полнотекстовый поиск
```sql
SELECT m.name, m.description, c.file_path
FROM methods m
JOIN components c ON m.component_id = c.id
WHERE m.id IN (
    SELECT rowid FROM methods_fts WHERE methods_fts MATCH 'message'
)
ORDER BY m.name;
```

## Преимущества SQL индекса

1. **Быстрый поиск** - индексы обеспечивают быстрый поиск
2. **Полнотекстовый поиск** - FTS5 для поиска по содержимому
3. **Сложные запросы** - JOIN, GROUP BY, агрегатные функции
4. **Структурированные данные** - нормализованная схема
5. **Масштабируемость** - легко добавлять новые поля и индексы
6. **Стандартные инструменты** - можно использовать любые SQLite клиенты

## Сравнение с JSON индексом

| Аспект | JSON индекс | SQL индекс |
|--------|-------------|------------|
| Скорость поиска | Медленно | Быстро |
| Полнотекстовый поиск | Нет | Да (FTS5) |
| Сложные запросы | Ограничено | Полная поддержка |
| Размер | Большой | Компактный |
| Обновление | Пересоздание | Инкрементальное |
| Инструменты | Специальные | Стандартные SQL |

## Автоматизация

### Добавить в CI/CD
```yaml
# .github/workflows/testkit-index.yml
- name: Generate TestKit SQL Index
  run: |
    python scripts/testkit_sql_indexer.py
    python scripts/testkit_sql_query.py --categories
```

### Добавить в Makefile
```makefile
testkit-index:
	python scripts/testkit_sql_indexer.py

testkit-search:
	python scripts/testkit_sql_query.py --name $(NAME)
```

## Расширение функциональности

### Добавить новые поля
1. Изменить схему в `create_database()`
2. Обновить парсинг в `analyze_file()`
3. Пересоздать индекс

### Добавить новые типы поиска
1. Добавить метод в `TestKitSQLQuery`
2. Добавить аргумент в `argparse`
3. Добавить обработку в `main()`

### Интеграция с IDE
Можно создать плагин для VS Code/IntelliJ, который будет использовать SQL базу для автодополнения и поиска методов TestKit. 