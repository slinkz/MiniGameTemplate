#!/bin/bash
# Setup FairyGUI SDK symbolic links
# Run this after cloning the repository and initializing submodules

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
REPO_ROOT="$(dirname "$PROJECT_DIR")"

cd "$REPO_ROOT"

echo "Initializing git submodules..."
git submodule update --init --recursive

cd "$PROJECT_DIR"

echo "Creating FairyGUI symbolic links..."

mkdir -p "Assets/FairyGUI"

if [ -L "Assets/FairyGUI/Scripts" ] || [ -d "Assets/FairyGUI/Scripts" ]; then
    echo "Link Assets/FairyGUI/Scripts already exists, skipping."
else
    ln -s "../../ThirdParty/FairyGUI-unity/Assets/Scripts" "Assets/FairyGUI/Scripts"
fi

if [ -L "Assets/FairyGUI/Editor" ] || [ -d "Assets/FairyGUI/Editor" ]; then
    echo "Link Assets/FairyGUI/Editor already exists, skipping."
else
    ln -s "../../ThirdParty/FairyGUI-unity/Assets/Editor" "Assets/FairyGUI/Editor"
fi

if [ -L "Assets/FairyGUI/Resources" ] || [ -d "Assets/FairyGUI/Resources" ]; then
    echo "Link Assets/FairyGUI/Resources already exists, skipping."
else
    ln -s "../../ThirdParty/FairyGUI-unity/Assets/Resources" "Assets/FairyGUI/Resources"
fi

echo "Done! FairyGUI SDK is ready."
