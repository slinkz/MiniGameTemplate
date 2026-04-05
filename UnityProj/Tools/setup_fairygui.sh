#!/bin/bash
# Setup FairyGUI SDK symbolic link
# Run this after cloning the repository

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
REPO_ROOT="$(dirname "$PROJECT_DIR")"

cd "$REPO_ROOT"

echo "Initializing git submodules..."
git submodule update --init --recursive

cd "$PROJECT_DIR"

echo "Creating FairyGUI symbolic link..."

if [ -L "Assets/FairyGUI" ]; then
    echo "Symlink Assets/FairyGUI already exists, skipping."
elif [ -d "Assets/FairyGUI" ]; then
    echo "WARNING: Assets/FairyGUI exists as a regular directory (not a symlink)."
    echo "It will be DELETED and replaced with a symlink to ../ThirdParty/FairyGUI-unity/Assets."
    printf "Are you sure you want to continue? (y/N): "
    read -r CONFIRM
    if [ "$CONFIRM" != "y" ] && [ "$CONFIRM" != "Y" ]; then
        echo "Aborted by user."
        exit 0
    fi
    echo "Removing existing Assets/FairyGUI directory..."
    rm -rf "Assets/FairyGUI"
    ln -s "../ThirdParty/FairyGUI-unity/Assets" "Assets/FairyGUI"
else
    ln -s "../ThirdParty/FairyGUI-unity/Assets" "Assets/FairyGUI"
fi

echo "Done! FairyGUI SDK is ready."
