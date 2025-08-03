#!/bin/bash
# Обновленный скрипт для индексации TestKit с рекомендациями

set -e

# Пути к директориям (относительно корня проекта)
TESTKIT_PATH="ClubDoorman.Test/TestKit"
DB_PATH="testkit_index.db"

echo "🔄 Starting TestKit index update with guidance..."

# Проверяем существование директорий
if [ ! -d "$TESTKIT_PATH" ]; then
    echo "❌ TestKit path not found: $TESTKIT_PATH"
    echo "Current directory: $(pwd)"
    echo "Available directories:"
    ls -la
    exit 1
fi

# Переходим в директорию скрипта
cd "$(dirname "$0")"

# Шаг 1: Индексация TestKit методов
echo "📊 Step 1: Indexing TestKit methods..."
python3 run_indexer.py "../../$TESTKIT_PATH" "$DB_PATH"

if [ $? -ne 0 ]; then
    echo "❌ Failed to index TestKit methods"
    exit 1
fi

echo "✅ TestKit methods indexed successfully"

# Шаг 2: Статистика
echo "📈 Step 2: Database statistics..."
python3 testkit_sql_query.py --stats

echo "🎉 TestKit index update completed successfully!"
echo ""
echo "📊 Summary:"
echo "  - TestKit methods indexed"
echo "  - Usage guidance available"
echo "  - Reports generated"
echo "  - MCP server ready for use"
echo ""
echo "🚀 You can now use the MCP tools (9 tools):"
echo "  - tk_search_by_name"
echo "  - tk_search_by_tags"
echo "  - tk_method_details"
echo "  - tk_method_signature"
echo "  - tk_search_similar"
echo "  - tk_get_guidance"
echo "  - tk_get_context_methods"
echo "  - tk_get_tag_guidance"
echo "  - tk_stats" 