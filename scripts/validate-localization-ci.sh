#!/bin/bash

# CI wrapper for localization validation
# Builds Docker image first, then runs validation

set -e

echo "🐳 Building Docker image for CI validation..."

# Build Docker image
docker build -t clubdoorman-localization-test ./ClubDoorman

if [ $? -ne 0 ]; then
    echo "❌ Docker build failed"
    exit 1
fi

echo "✅ Docker build successful"

# Run the existing validation script
echo "🔍 Running localization validation..."
./scripts/validate-localization.sh 