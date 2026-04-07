#!/bin/bash
# Setup FairyGUI SDK symbolic link
# Run this after cloning the repository

set -euo pipefail

FORCE=false
for arg in "$@"; do
  if [ "$arg" = "--force" ]; then
    FORCE=true
  fi
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
REPO_ROOT="$(dirname "$PROJECT_DIR")"

cd "$REPO_ROOT"

echo "Initializing git submodules..."
git submodule update --init --recursive UnityProj/ThirdParty/FairyGUI-unity

cd "$PROJECT_DIR"

echo "Creating FairyGUI symbolic link..."

SOURCE="../ThirdParty/FairyGUI-unity/Assets"
TARGET="Assets/FairyGUI"
SOURCE_CHECK="$(dirname "$TARGET")/$SOURCE"

if [ ! -d "$SOURCE_CHECK" ]; then
  echo "ERROR: source path does not exist: ${SOURCE} (resolved: ${SOURCE_CHECK})" >&2
  exit 1
fi

if [ -L "$TARGET" ]; then
  CURRENT="$(readlink "$TARGET")"
  if [ "$CURRENT" = "$SOURCE" ]; then
    echo "Symlink ${TARGET} already exists, skipping."
    echo "Done! FairyGUI SDK is ready."
    exit 0
  fi

  if [ "$FORCE" != "true" ]; then
    echo "WARNING: ${TARGET} symlink points to '${CURRENT}', expected '${SOURCE}'."
    printf "Replace it? (y/N): "
    read -r CONFIRM
    if [ "$CONFIRM" != "y" ] && [ "$CONFIRM" != "Y" ]; then
      echo "Aborted by user."
      exit 0
    fi
  fi

  rm "$TARGET"
elif [ -d "$TARGET" ]; then
  echo "WARNING: ${TARGET} exists as a regular directory (not a symlink)."
  echo "It will be DELETED and replaced with a symlink to ${SOURCE}."
  if [ "$FORCE" != "true" ]; then
    printf "Are you sure you want to continue? (y/N): "
    read -r CONFIRM
    if [ "$CONFIRM" != "y" ] && [ "$CONFIRM" != "Y" ]; then
      echo "Aborted by user."
      exit 0
    fi
  fi
  echo "Removing existing ${TARGET} directory..."
  rm -rf "$TARGET"
fi

ln -s "$SOURCE" "$TARGET"

echo "Done! FairyGUI SDK is ready."
