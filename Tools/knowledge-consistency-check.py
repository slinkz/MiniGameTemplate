#!/usr/bin/env python3
"""Knowledge Consistency Checker
Validates that file and document references in knowledge documents
point to files that actually exist in the project.
"""

import os
import re
import sys
import argparse
import glob as glob_module
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent

parser = argparse.ArgumentParser(description="Validate knowledge document references.")
parser.add_argument(
    "--allow-warnings",
    action="store_true",
    help="Return exit code 0 when only warnings are present. Default is strict.",
)
args = parser.parse_args()

errors = []
warnings = []
checked = 0


def resolve_code_path(path: str) -> str | None:
    """Resolve a code path reference to a real filesystem path."""
    path = path.strip()

    if not looks_path_like(path):
        return "[non-path reference skipped]"

    # Direct path under repo
    abs_path = REPO_ROOT / path
    if abs_path.exists():
        return str(abs_path)

    # Prepend UnityProj/Assets/
    abs_path = REPO_ROOT / "UnityProj" / "Assets" / path
    if abs_path.exists():
        return str(abs_path)

    # Common shorthand in ADR/knowledge docs: EntitySystem/** means _Framework/EntitySystem/**
    abs_path = REPO_ROOT / "UnityProj" / "Assets" / "_Framework" / path
    if abs_path.exists():
        return str(abs_path)

    # Glob pattern
    if "*" in path or "?" in path:
        for base in (
            REPO_ROOT,
            REPO_ROOT / "UnityProj" / "Assets",
            REPO_ROOT / "UnityProj" / "Assets" / "_Framework",
            REPO_ROOT / "Docs",
            REPO_ROOT / "Docs" / "Agent",
        ):
            glob_path = base / path
            matches = glob_module.glob(str(glob_path), recursive=True)
            if matches:
                return f"{glob_path} -> {len(matches)} files"

    # Search by filename (for shorthand paths)
    filename = os.path.basename(path)
    if "." in filename:
        for root, _, files in os.walk(REPO_ROOT / "UnityProj" / "Assets"):
            if filename in files:
                return f"[partial-match] {os.path.join(root, filename)}"

    return None


def resolve_doc_path(path: str) -> str | None:
    """Resolve a document path reference."""
    path = path.strip()

    if "*" in path or "?" in path:
        for base in (REPO_ROOT, REPO_ROOT / "Docs", REPO_ROOT / "Docs" / "Agent"):
            glob_path = base / path
            matches = glob_module.glob(str(glob_path), recursive=True)
            if matches:
                return f"{glob_path} -> {len(matches)} files"

    abs_path = REPO_ROOT / path
    if abs_path.exists():
        return str(abs_path)

    abs_path = REPO_ROOT / "Docs" / "Agent" / path
    if abs_path.exists():
        return str(abs_path)

    abs_path = REPO_ROOT / "Docs" / path
    if abs_path.exists():
        return str(abs_path)

    abs_path = REPO_ROOT / "Docs" / "Guide" / path
    if abs_path.exists():
        return str(abs_path)

    return None


def resolve_module_card(name: str) -> str | None:
    """Resolve a Module Card reference."""
    name = name.strip().split("/")[-1]
    abs_path = REPO_ROOT / "Docs" / "Agent" / "MODULE_CARDS" / name
    if abs_path.exists():
        return str(abs_path)
    return None


def resolve_context_pack(name: str) -> str | None:
    """Resolve a Context Pack reference."""
    name = name.strip().split("/")[-1]
    abs_path = REPO_ROOT / "Docs" / "Agent" / "CONTEXT_PACKS" / name
    if abs_path.exists():
        return str(abs_path)
    return None


def check_code_path(code_path: str, context: str = ""):
    """Check if a code path exists."""
    global checked
    if not code_path or code_path.strip() == "-":
        return
    if not looks_path_like(code_path):
        return
    resolved = resolve_code_path(code_path)
    if resolved:
        checked += 1
    else:
        warnings.append(f"[{context}] Code path NOT FOUND: '{code_path}'")


def check_module_cards(card_str: str, context: str = ""):
    """Check Module Card references."""
    global checked
    if not card_str or card_str.strip() == "-":
        return
    for card in extract_refs(card_str):
        card = card.strip().split("/")[-1]
        if not card or card == "-":
            continue
        if "MODULE_CARDS/" not in card_str:
            continue
        resolved = resolve_module_card(card)
        if resolved:
            checked += 1
        else:
            warnings.append(f"[{context}] Module Card NOT FOUND: '{card}'")


def check_context_packs(pack_str: str, context: str = ""):
    """Check Context Pack references."""
    global checked
    if not pack_str or pack_str.strip() == "-":
        return
    for pack in extract_refs(pack_str):
        pack = pack.strip()
        if not pack or pack == "-":
            continue
        if "CONTEXT_PACKS/" in pack:
            resolved = resolve_context_pack(pack)
            if resolved:
                checked += 1
            else:
                warnings.append(f"[{context}] Context Pack NOT FOUND: '{pack}'")
        elif looks_doc_like(pack):
            check_doc_paths(pack, context)
        else:
            continue


def check_doc_paths(doc_str: str, context: str = ""):
    """Check document references."""
    global checked
    if not doc_str or doc_str.strip() == "-":
        return
    for doc in extract_refs(doc_str):
        doc = doc.strip()
        if not doc or doc == "-":
            continue
        if not looks_doc_like(doc):
            continue
        resolved = resolve_doc_path(doc)
        if resolved:
            checked += 1
        else:
            warnings.append(f"[{context}] Doc ref NOT FOUND: '{doc}'")


def extract_refs(cell: str) -> list[str]:
    """Extract backtick-wrapped refs from a Markdown table cell.

    If no backtick refs exist, fall back to comma-separated plain refs. This keeps
    older rows such as "全局" harmless while still checking explicit paths.
    """
    refs = re.findall(r"`([^`]+)`", cell)
    if refs:
        return [r.strip() for r in refs if r.strip()]
    return [r.strip().strip("` ") for r in cell.split(",") if r.strip()]


def split_markdown_row(line: str) -> list[str]:
    """Split a simple Markdown table row into trimmed cells."""
    return [cell.strip() for cell in line.strip().strip("|").split("|")]


def looks_path_like(value: str) -> bool:
    value = value.strip()
    if not value or value == "-":
        return False
    if value.startswith(("ADR-", "PIT-")):
        return False
    return (
        "/" in value
        or "\\" in value
        or "*" in value
        or "?" in value
        or value.endswith((".cs", ".asset", ".unity", ".md", ".json", ".xml", ".bytes", ".prefab", ".mat"))
    )


def looks_doc_like(value: str) -> bool:
    value = value.strip()
    return (
        value.endswith(".md")
        or value.startswith(("Docs/", "Docs\\", "skills/", "skills\\"))
        or (("*" in value or "?" in value) and "/" in value)
    )


# =====================================================
# Check KNOWLEDGE/CODE_KNOWLEDGE_MAP.md
# =====================================================
print("=== Checking KNOWLEDGE/CODE_KNOWLEDGE_MAP.md ===")

map_file = REPO_ROOT / "Docs" / "Agent" / "KNOWLEDGE/CODE_KNOWLEDGE_MAP.md"
if not map_file.exists():
    errors.append("KNOWLEDGE/CODE_KNOWLEDGE_MAP.md not found")
else:
    content = map_file.read_text(encoding="utf-8")
    for line_no, line in enumerate(content.splitlines(), start=1):
        if not line.startswith("| `"):
            continue
        cells = split_markdown_row(line)
        if len(cells) < 4:
            continue

        code_cell, module_cell, context_cell, tdd_cell = cells[:4]
        for code_path in extract_refs(code_cell):
            check_code_path(code_path, f"CODE_MAP:{line_no}")
        check_module_cards(module_cell, f"CODE_MAP:{line_no}")
        check_context_packs(context_cell, f"CODE_MAP:{line_no}")
        check_doc_paths(tdd_cell, f"CODE_MAP.TDD:{line_no}")

# =====================================================
# Check ADR/ADR_SCHEMA.md AppliesTo
# =====================================================
print("=== Checking ADR/ADR_SCHEMA.md AppliesTo ===")

adr_file = REPO_ROOT / "Docs" / "Agent" / "ADR/ADR_SCHEMA.md"
if not adr_file.exists():
    errors.append("ADR/ADR_SCHEMA.md not found")
else:
    content = adr_file.read_text(encoding="utf-8")
    # Extract AppliesTo from ADR blocks
    adr_blocks = re.finditer(r'### (ADR-\d+).*?\| AppliesTo \| (.+?) \|', content, re.DOTALL)
    for m in adr_blocks:
        adr_id = m.group(1)
        applies_to = m.group(2).strip()
        if applies_to:
            # Only validate explicit backtick refs. Plain-language phrases in
            # AppliesTo are semantic scope notes, not necessarily paths.
            for p in re.findall(r"`([^`]+)`", applies_to):
                check_code_path(p, f"ADR_SCHEMA.{adr_id}")

# =====================================================
# Check INDEX.md document refs
# =====================================================
print("=== Checking INDEX.md document refs ===")

index_file = REPO_ROOT / "Docs" / "Agent" / "INDEX.md"
if not index_file.exists():
    errors.append("INDEX.md not found")
else:
    content = index_file.read_text(encoding="utf-8")
    doc_refs = re.finditer(r'`([A-Z_]+[A-Za-z_/]*\.md)`', content)
    seen = set()
    for m in doc_refs:
        doc_path = m.group(1)
        if doc_path not in seen and doc_path.endswith(".md"):
            seen.add(doc_path)
            check_doc_paths(doc_path, "INDEX")

# =====================================================
# Check Module Cards code paths
# =====================================================
print("=== Checking Module Card code paths ===")

module_card_dir = REPO_ROOT / "Docs" / "Agent" / "MODULE_CARDS"
if not module_card_dir.exists():
    errors.append("MODULE_CARDS directory not found")
else:
    for card_file in module_card_dir.glob("*.md"):
        if card_file.name == "README.md":
            continue
        content = card_file.read_text(encoding="utf-8")
        # Find code-like paths (strings with slashes ending in .cs)
        code_refs = re.finditer(r'`([A-Za-z_][A-Za-z_/\.]+\.cs)`', content)
        for m in code_refs:
            code_path = m.group(1)
            if "/" in code_path or "\\" in code_path:
                check_code_path(code_path, f"MODULE.{card_file.stem}")

# =====================================================
# Check Context Packs document refs
# =====================================================
print("=== Checking Context Pack document refs ===")

context_pack_dir = REPO_ROOT / "Docs" / "Agent" / "CONTEXT_PACKS"
if not context_pack_dir.exists():
    errors.append("CONTEXT_PACKS directory not found")
else:
    for pack_file in context_pack_dir.glob("*.md"):
        content = pack_file.read_text(encoding="utf-8")
        code_refs = re.finditer(r'`([A-Za-z_][A-Za-z_/\.]+\.cs)`', content)
        for m in code_refs:
            code_path = m.group(1)
            if "/" in code_path or "\\" in code_path:
                check_code_path(code_path, f"PACK.{pack_file.stem}")

# =====================================================
# Report
# =====================================================
print(f"\n{'='*50}")
print("CONSISTENCY CHECK SUMMARY")
print(f"{'='*50}")
print(f"Total checks passed: {checked}")
print(f"Warnings: {len(warnings)}")
print(f"Errors: {len(errors)}")

if errors:
    print("\nERRORS:")
    for e in errors:
        print(f"  FAIL: {e}")

if warnings:
    print("\nWARNINGS (potential stale references):")
    for w in warnings:
        print(f"  WARN: {w}")

if not errors and not warnings:
    print("\n[PASS] All knowledge references are consistent with filesystem.")
    sys.exit(0)
elif warnings and not errors:
    print(f"\n[WARN] {len(warnings)} reference(s) may be stale - review above.")
    sys.exit(0 if args.allow_warnings else 1)
else:
    print(f"\n[FAIL] Knowledge consistency check found {len(errors)} error(s).")
    sys.exit(1)
