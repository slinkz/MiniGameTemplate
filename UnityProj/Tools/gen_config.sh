#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CONF_ROOT="$SCRIPT_DIR/../DataTables"
OUTPUT_CODE="$SCRIPT_DIR/../Assets/_Framework/DataSystem/Scripts/Config/Generated"
OUTPUT_DATA="$SCRIPT_DIR/../Assets/_Framework/DataSystem/Resources/ConfigData"

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
