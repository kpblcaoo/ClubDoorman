#!/bin/bash

# CI wrapper for localization validation
# Builds Docker image first, then runs validation inside container

set -e

echo "🐳 Building Docker image for CI validation..."

# Build Docker image
docker build -t clubdoorman-localization-test ./ClubDoorman

if [ $? -ne 0 ]; then
    echo "❌ Docker build failed"
    exit 1
fi

echo "✅ Docker build successful"

# Run validation inside Docker container
echo "🔍 Running localization validation inside Docker container..."
docker run --rm -v "$(pwd):/workspace" -w /workspace clubdoorman-localization-test bash -c "
    # Install required tools in container
    apt-get update -qq && apt-get install -y -qq xmllint hexdump file
    
    # Run validation script
    ./scripts/validate-localization.sh
" 