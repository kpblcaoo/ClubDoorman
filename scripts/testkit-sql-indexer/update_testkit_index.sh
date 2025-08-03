#!/bin/bash

# TestKit Index Updater
# Быстрое обновление SQL индекса TestKit

set -e

echo "🔄 Updating TestKit SQL Index..."
echo "=================================="

# Проверяем, что мы в корне проекта
if [ ! -f "ClubDoorman.sln" ]; then
    echo "❌ Error: Run this script from the project root directory"
    exit 1
fi

# Проверяем наличие Python
if ! command -v python3 &> /dev/null; then
    echo "❌ Error: Python 3 is required but not installed"
    exit 1
fi

# Проверяем наличие TestKit
if [ ! -d "ClubDoorman.Test/TestKit" ]; then
    echo "❌ Error: TestKit directory not found"
    exit 1
fi

# Создаем резервную копию старой базы
if [ -f "testkit_index.db" ]; then
    echo "📦 Creating backup of existing database..."
    cp testkit_index.db testkit_index.db.backup.$(date +%Y%m%d_%H%M%S)
fi

# Генерируем новый индекс
echo "🔍 Generating new SQL index..."
python3 scripts/testkit_sql_indexer.py

# Проверяем, что база создалась
if [ ! -f "testkit_index.db" ]; then
    echo "❌ Error: Database was not created"
    exit 1
fi

# Показываем краткую статистику
echo ""
echo "📊 New Index Statistics:"
echo "========================"
python3 scripts/testkit_sql_query.py --categories

echo ""
echo "🏷️  Popular Tags:"
echo "=================="
python3 scripts/testkit_sql_query.py --popular-tags 5

echo ""
echo "✅ TestKit SQL index updated successfully!"
echo "💡 Use 'python3 scripts/testkit_sql_query.py --help' to see available commands" 