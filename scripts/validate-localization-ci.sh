#!/bin/bash

# CI wrapper for localization validation
# Builds Docker image first, then runs validation inside container

set -e

echo "🐳 Building Docker image for CI validation..."

# Build Docker image and run validation in build stage
echo "🔍 Running localization validation in Docker build stage..."
docker run --rm -v "$(pwd):/src" -w /src mcr.microsoft.com/dotnet/sdk:9.0 bash -c "
    # Install required tools
    apt-get update -qq && apt-get install -y -qq xmllint hexdump file
    
    # Build project first
    dotnet build ClubDoorman.sln --configuration Release --no-restore
    
    # Run validation script
    ./scripts/validate-localization.sh
" 