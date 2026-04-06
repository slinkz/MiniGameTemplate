#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
LUBAN_DLL="$SCRIPT_DIR/Luban/Luban.dll"
CONF_ROOT="$SCRIPT_DIR/../DataTables"
OUTPUT_CODE="$SCRIPT_DIR/../Assets/_Framework/DataSystem/Scripts/Config/Generated"
OUTPUT_DATA_BIN="$SCRIPT_DIR/../Assets/_Game/ConfigData"
OUTPUT_DATA_JSON="$SCRIPT_DIR/../Assets/_Framework/Editor/ConfigPreview"
RESOURCES_BIN="$SCRIPT_DIR/../Assets/_Framework/DataSystem/Resources/ConfigData"

echo "============================================"
echo " Luban v4.6 Config Generator (Dual Format)"
echo "============================================"
echo ""

# --- Step 1: Generate C# code (cs-bin) + Binary data ---
echo "[Step 1/3] Generating C# code + Binary data..."
dotnet "$LUBAN_DLL" \
    --conf "$CONF_ROOT/luban.conf" \
    -t all \
    -c cs-bin \
    -d bin \
    -x outputCodeDir="$OUTPUT_CODE" \
    -x outputDataDir="$OUTPUT_DATA_BIN"

echo "[OK] Binary code + data generated."
echo ""

# --- Step 2: Generate JSON data (editor-only preview) ---
# JSON files go to an Editor/ folder so they are excluded from builds.
echo "[Step 2/3] Generating JSON data (editor-only preview)..."
mkdir -p "$OUTPUT_DATA_JSON"
dotnet "$LUBAN_DLL" \
    --conf "$CONF_ROOT/luban.conf" \
    -t all \
    -d json \
    -x outputDataDir="$OUTPUT_DATA_JSON"

echo "[OK] JSON data generated for editor preview."
echo ""

# --- Step 3: Copy binary data to Resources for fallback loading ---
echo "[Step 3/3] Copying binary data to Resources fallback..."
mkdir -p "$RESOURCES_BIN"
if ! cp -f "$OUTPUT_DATA_BIN"/*.bytes "$RESOURCES_BIN/" 2>/dev/null; then
    echo "[WARN] Binary copy to Resources failed! Check that .bytes files exist in $OUTPUT_DATA_BIN"
fi
echo "[OK] Binary data copied to Resources/ConfigData/."
echo ""

echo "============================================"
echo " All done!"
echo " Code:       Assets/_Framework/DataSystem/Scripts/Config/Generated/"
echo " Binary:     Assets/_Game/ConfigData/*.bytes          (YooAsset - runtime)"
echo " Bin copy:   Assets/_Framework/DataSystem/Resources/ConfigData/*.bytes (Resources fallback)"
echo " JSON:       Assets/_Framework/Editor/ConfigPreview/  (editor-only, excluded from builds)"
echo "============================================"
