#!/bin/bash
set -e

COVERAGE_TMP_DIR="/tmp/coverage-$(date +%s)"
mkdir -p "$COVERAGE_TMP_DIR"

# Собираем coverage в формате cobertura
dotnet test --collect:"XPlat Code Coverage" --results-directory "$COVERAGE_TMP_DIR"

# Ищем файл cobertura
COBERTURA_FILE=$(find "$COVERAGE_TMP_DIR" -name "coverage.cobertura.xml" | head -n 1)
if [ -z "$COBERTURA_FILE" ]; then
  echo "Файл coverage.cobertura.xml не найден в $COVERAGE_TMP_DIR"
  exit 1
fi

# Генерируем html-отчет
DOTNET_ROOT=$HOME/.dotnet reportgenerator -reports:"$COBERTURA_FILE" -targetdir:"$COVERAGE_TMP_DIR/coveragereport" -reporttypes:Html

echo "Coverage report сгенерирован в $COVERAGE_TMP_DIR/coveragereport"