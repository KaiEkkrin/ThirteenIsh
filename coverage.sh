#!/bin/bash
echo "🧪 Running tests with coverage..."
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults

echo "📊 Finding latest coverage file..."
LATEST_COVERAGE=$(find ./TestResults -name "coverage.cobertura.xml" -type f -printf '%T@ %p\n' | sort -n | tail -1 | cut -d' ' -f2-)

if [ -n "$LATEST_COVERAGE" ]; then
    echo "✅ Coverage file: $LATEST_COVERAGE"
    echo "🔄 Refresh Coverage Gutters in VS Code to see updated coverage!"
else
    echo "❌ No coverage file found!"
fi