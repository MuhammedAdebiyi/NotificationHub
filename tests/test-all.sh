#!/bin/bash
# Run all SDK tests
# Usage: NH_API_KEY=nhub_live_xxx ./test-all.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_KEY="${NH_API_KEY:?Set NH_API_KEY env var}"

echo "Testing with API key: ${API_KEY:0:12}..."
echo ""

echo "━━━ 1. cURL ━━━"
bash "$SCRIPT_DIR/test-curl.sh"
echo ""

echo "━━━ 2. TypeScript ━━━"
if command -v npx &>/dev/null; then
  npx tsx "$SCRIPT_DIR/test-typescript.ts"
else
  echo "   ⚠ npx not found, skipping"
fi
echo ""

echo "━━━ 3. Python ━━━"
python3 "$SCRIPT_DIR/test-python.py"
echo ""

echo "━━━ 4. PHP ━━━"
if command -v php &>/dev/null; then
  php "$SCRIPT_DIR/test-php.php"
else
  echo "   ⚠ php not found, skipping"
fi
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "All SDK tests completed!"
