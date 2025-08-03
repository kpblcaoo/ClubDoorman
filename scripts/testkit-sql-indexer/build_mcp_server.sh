#!/bin/bash
# Скрипт сборки MCP сервера

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "Building TestKit MCP Server..."

# Создаем директорию для бинарников
mkdir -p "$SCRIPT_DIR/bin"

# Активируем виртуальное окружение
if [ -d "$SCRIPT_DIR/venv" ]; then
    echo "Activating virtual environment..."
    source "$SCRIPT_DIR/venv/bin/activate"
else
    echo "Virtual environment not found. Please run: python3 -m venv venv && source venv/bin/activate && pip install fastmcp"
    exit 1
fi

# Создаем исполняемый файл с правильным shebang
cat > "$SCRIPT_DIR/bin/tk-mcp" << EOF
#!$SCRIPT_DIR/venv/bin/python3
import sys
import os

# Добавляем путь к модулям
script_dir = os.path.dirname(os.path.abspath(__file__))
project_dir = os.path.dirname(script_dir)
sys.path.insert(0, project_dir)

from tk_fastmcp_server import TestKitFastMCPServer

if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(description="TestKit MCP Server")
    parser.add_argument("--db-path", 
                       default=os.environ.get("TESTKIT_DB_PATH", "testkit_index.db"),
                       help="Database file path")
    
    args = parser.parse_args()
    
    server = TestKitFastMCPServer(args.db_path)
    server.run()
EOF

# Делаем исполняемым
chmod +x "$SCRIPT_DIR/bin/tk-mcp"

echo "MCP Server built successfully!"
echo "Binary location: $SCRIPT_DIR/bin/tk-mcp"
echo ""
echo "To register with Cursor, add to ~/.cursor/mcp.json:"
echo "{"
echo "  \"mcpServers\": {"
echo "    \"testkitdex\": {"
echo "      \"command\": \"$SCRIPT_DIR/bin/tk-mcp\","
echo "      \"env\": {"
echo "        \"TESTKIT_ROOT\": \"$PROJECT_ROOT\","
echo "        \"TESTKIT_DB_PATH\": \"$SCRIPT_DIR/testkit_index.db\""
echo "      }"
echo "    }"
echo "  }"
echo "}"
echo ""
echo "To test the server:"
echo "  $SCRIPT_DIR/bin/tk-mcp --db-path $SCRIPT_DIR/testkit_index.db" 