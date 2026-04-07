#!/bin/bash
# Setup Spine runtime source links for Unity project
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
git submodule update --init --recursive UnityProj/ThirdParty/spine-runtimes

cd "$PROJECT_DIR"

ensure_link() {
  local target="$1"
  local source="$2"
  local source_check
  source_check="$(dirname "$target")/$source"

  echo
  echo "Preparing ${target} -> ${source}"

  if [ ! -d "$source_check" ]; then
    echo "ERROR: source path does not exist: ${source} (resolved: ${source_check})" >&2
    return 1
  fi

  if [ -L "$target" ]; then
    local current_target
    current_target="$(readlink "$target")"
    if [ "$current_target" = "$source" ]; then
      echo "Symlink ${target} already exists, skipping."
      return 0
    fi

    if [ "$FORCE" != "true" ]; then
      echo "WARNING: ${target} symlink points to '${current_target}', expected '${source}'."
      printf "Replace it? (y/N): "
      read -r confirm
      if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
        echo "Aborted by user."
        return 1
      fi
    fi

    rm "$target"
  elif [ -d "$target" ]; then
    echo "WARNING: ${target} exists as a regular directory (not a symlink)."
    echo "It will be DELETED and replaced with a symlink."
    if [ "$FORCE" != "true" ]; then
      printf "Continue? (y/N): "
      read -r confirm
      if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
        echo "Aborted by user."
        return 1
      fi
    fi
    rm -rf "$target"
  fi

  ln -s "$source" "$target"
}

ensure_link "Assets/Spine" "../ThirdParty/spine-runtimes/spine-unity/Assets/Spine"
ensure_link "Assets/SpineCSharp" "../ThirdParty/spine-runtimes/spine-csharp/src"

echo "Done! Spine source links are ready."
echo "Next step: enable FAIRYGUI_SPINE define from Unity menu if needed."
