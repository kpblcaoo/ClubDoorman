#!/bin/bash

# Setup script for ClubDoorman Git hooks
# Installs pre-commit hook for automatic testing and validation

set -e

echo "🔧 Setting up Git hooks for ClubDoorman..."

# Check if we're in the right directory
if [ ! -f "ClubDoorman.sln" ]; then
    echo "❌ Error: This script must be run from the project root directory"
    exit 1
fi

# Check if .git directory exists
if [ ! -d ".git" ]; then
    echo "❌ Error: This is not a Git repository"
    exit 1
fi

# Create hooks directory if it doesn't exist
mkdir -p .git/hooks

# Copy pre-commit hook
if [ -f ".git/hooks/pre-commit" ]; then
    echo "⚠️  Pre-commit hook already exists. Backing up..."
    cp .git/hooks/pre-commit .git/hooks/pre-commit.backup
fi

# Create pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/bash

# Pre-commit hook for ClubDoorman
# Runs tests and localization validation before commit

set -e

echo "🔍 Running pre-commit checks..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Check if we're in the right directory
if [ ! -f "ClubDoorman.sln" ]; then
    print_status $RED "❌ Error: This hook must be run from the project root directory"
    exit 1
fi

# Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    print_status $RED "❌ Error: .NET SDK is not installed or not in PATH"
    exit 1
fi

print_status $GREEN "✅ .NET SDK found"

# Check if any resource files were changed
RESOURCE_FILES_CHANGED=false
if git diff --cached --name-only | grep -q "ClubDoorman/Resources/"; then
    RESOURCE_FILES_CHANGED=true
    print_status $YELLOW "📁 Resource files changed, will validate localization"
fi

# Run localization validation if resources changed
if [ "$RESOURCE_FILES_CHANGED" = true ]; then
    print_status $YELLOW "🔍 Validating localization resources..."
    ./scripts/validate-localization.sh
    if [ $? -ne 0 ]; then
        print_status $RED "❌ Localization validation failed"
        exit 1
    fi
    print_status $GREEN "✅ Localization validation passed"
fi

# Run tests
print_status $YELLOW "🧪 Running tests..."
dotnet test --no-restore --verbosity minimal

if [ $? -ne 0 ]; then
    print_status $RED "❌ Tests failed"
    exit 1
fi

print_status $GREEN "✅ All pre-commit checks passed!"
print_status $GREEN "🚀 Proceeding with commit..."

exit 0
EOF

# Make hook executable
chmod +x .git/hooks/pre-commit

echo "✅ Pre-commit hook installed successfully!"
echo ""
echo "📋 What this hook does:"
echo "• Runs tests before every commit"
echo "• Validates localization resources if they were changed"
echo "• Prevents commit if any checks fail"
echo ""
echo "💡 To skip hooks temporarily, use: git commit --no-verify"
echo "💡 To disable hooks permanently, remove: .git/hooks/pre-commit" 