#!/bin/bash

echo "╔════════════════════════════════════════════════════╗"
echo "║     LUDO GAME SERVER - SETUP VERIFICATION          ║"
echo "╚════════════════════════════════════════════════════╝"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "Checking files..."
echo ""

# Check required files
files=(
    "server.js:Server main file"
    "gameSession.js:Game logic"
    "index.html:Web client"
    "package.json:Dependencies"
    "LudoClient.cs:Unity client"
    "LudoGameUI.cs:Unity UI example"
    "test-matchmaking.js:Test suite"
    "UNITY_INTEGRATION.md:Unity guide"
    "NEW_QUICKSTART.md:Quick start"
    "CHANGES_SUMMARY.md:Changes doc"
    "CHEATSHEET.txt:Quick reference"
)

all_good=true
for item in "${files[@]}"; do
    file="${item%%:*}"
    desc="${item##*:}"
    if [ -f "$file" ]; then
        echo -e "${GREEN}✓${NC} $file ($desc)"
    else
        echo -e "${RED}✗${NC} $file ($desc) - MISSING!"
        all_good=false
    fi
done

echo ""
echo "Checking Node.js..."
if command -v node &> /dev/null; then
    version=$(node --version)
    echo -e "${GREEN}✓${NC} Node.js installed: $version"
else
    echo -e "${RED}✗${NC} Node.js not installed"
    all_good=false
fi

echo ""
echo "Checking dependencies..."
if [ -d "node_modules" ]; then
    echo -e "${GREEN}✓${NC} node_modules exists"
else
    echo -e "${YELLOW}!${NC} node_modules not found. Run: npm install"
fi

echo ""
echo "File statistics..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Server code:       $(wc -l < server.js) lines"
echo "Game logic:        $(wc -l < gameSession.js) lines"
echo "Unity client:      $(wc -l < LudoClient.cs) lines"
echo "Unity UI example:  $(wc -l < LudoGameUI.cs) lines"
echo "Web client:        $(wc -l < index.html) lines"
echo "Test suite:        $(wc -l < test-matchmaking.js) lines"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

echo ""
echo "Key features:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${GREEN}✓${NC} Automatic matchmaking"
echo -e "${GREEN}✓${NC} Flexible team sizes (2-4 players)"
echo -e "${GREEN}✓${NC} Smart queue management (10s timeout)"
echo -e "${GREEN}✓${NC} Complete Unity C# client"
echo -e "${GREEN}✓${NC} Web-based test client"
echo -e "${GREEN}✓${NC} Automated test suite"
echo -e "${GREEN}✓${NC} Full documentation"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

echo ""
if [ "$all_good" = true ]; then
    echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
    echo -e "${GREEN}   ✓ ALL CHECKS PASSED - READY TO USE!${NC}"
    echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
    echo ""
    echo "Quick start:"
    echo "  1. npm install"
    echo "  2. node server.js"
    echo "  3. Open index.html"
    echo "  4. Click 'Find Match'"
    echo ""
    echo "See NEW_QUICKSTART.md for detailed instructions"
else
    echo -e "${RED}════════════════════════════════════════════════════${NC}"
    echo -e "${RED}   ✗ SOME CHECKS FAILED${NC}"
    echo -e "${RED}════════════════════════════════════════════════════${NC}"
fi

