#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CONF_ROOT="$SCRIPT_DIR/../DataTables"
OUTPUT_CODE="$SCRIPT_DIR/../Assets/_Framework/DataSystem/Scripts/Config/Generated"
OUTPUT_DATA="$SCRIPT_DIR/../Assets/_Game/ConfigData"

echo "[Luban] Generating config tables..."

luban \
    -t all \
    -d "$CONF_ROOT/Defs/__root__.xml" \
    --input_data_dir "$CONF_ROOT/Datas" \
    --output_code_dir "$OUTPUT_CODE" \
    --output_data_dir "$OUTPUT_DATA" \
    --gen_types code_cs_unity_json,data_json \
    -s all

echo "[Luban] Generation complete."

# Copy JSON data to Resources/ConfigData for fallback loading
FALLBACK_DIR="$SCRIPT_DIR/../Assets/_Framework/DataSystem/Resources/ConfigData"
mkdir -p "$FALLBACK_DIR"
cp "$OUTPUT_DATA"/*.json "$FALLBACK_DIR/"
echo "[Luban] Fallback data synced to Resources/ConfigData."
