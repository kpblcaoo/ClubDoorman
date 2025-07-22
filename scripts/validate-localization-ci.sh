#!/bin/bash

# CI wrapper for localization validation
# Builds Docker image first, then runs validation inside container

set -e

echo "🐳 Building Docker image for CI validation..."

# Run validation in Docker container with .NET SDK
echo "🔍 Running localization validation in Docker container..."
docker run --rm -v "$(pwd):/src" -w /src mcr.microsoft.com/dotnet/sdk:9.0 bash -c "
    # Install required tools
    apt-get update -qq && apt-get install -y -qq libxml2-utils bsdmainutils file
    
    # Run validation script with build skipped
    SKIP_BUILD=true ./scripts/validate-localization.sh
" 