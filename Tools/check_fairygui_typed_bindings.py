#!/usr/bin/env python3
"""
Check hand-written FairyGUI UI code for string-based bindings.

The project rule is that business controllers should use FairyGUI generated
fields, for example `_view.btn_confirm`, instead of `_view.GetChild("...")`.
Existing debt is tracked in Tools/fairygui-typed-bindings-baseline.txt so new
violations fail fast without blocking the repository on legacy dynamic items.
"""

from __future__ import annotations

import argparse
import re
import sys
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_BASELINE = ROOT / "Tools" / "fairygui-typed-bindings-baseline.txt"
DEFAULT_SCAN_ROOTS = (
    ROOT / "UnityProj" / "Assets" / "_Game" / "Scripts" / "ShooterGame" / "UI",
    ROOT / "UnityProj" / "Assets" / "_Game" / "Scripts" / "UI",
)

LOOKUP_RE = re.compile(r"(?P<api>GetChild|GetController|GetTransition)\s*\(")
FGUI_CAST_RE = re.compile(
    r"\bas\s+G(?:Button|TextField|TextInput|Loader|List|Component|Graph|ProgressBar|Slider|ComboBox|Group|Label|Object)\b"
    r"|\(G(?:Button|TextField|TextInput|Loader|List|Component|Graph|ProgressBar|Slider|ComboBox|Group|Label|Object)\)\s*"
)
AS_PROPERTY_RE = re.compile(
    r"\.as(?:Button|TextField|TextInput|Loader|List|Com|Graph|Progress|Slider|ComboBox)\b"
)
ALLOW_MARKER = "fairygui-typed-binding: allow"


@dataclass(frozen=True)
class Violation:
    relative_path: str
    line_no: int
    rule: str
    source: str

    @property
    def signature(self) -> str:
        return f"{self.relative_path} | {self.rule} | {self.source.strip()}"

    def format(self) -> str:
        return f"{self.relative_path}:{self.line_no}: {self.rule}: {self.source.strip()}"


def to_relative(path: Path) -> str:
    return path.resolve().relative_to(ROOT).as_posix()


def is_generated_fairygui_file(path: Path, content: str) -> bool:
    rel = to_relative(path)
    if "/Scripts/UI/" not in rel:
        return False
    if rel.endswith(".Logic.cs"):
        return False
    return "automatically generated class by FairyGUI" in content[:300]


def iter_cs_files(scan_roots: Iterable[Path]) -> Iterable[Path]:
    seen: set[Path] = set()
    for root in scan_roots:
        if root.is_file() and root.suffix == ".cs":
            candidates = [root]
        elif root.is_dir():
            candidates = sorted(root.rglob("*.cs"))
        else:
            continue

        for path in candidates:
            resolved = path.resolve()
            if resolved in seen:
                continue
            seen.add(resolved)
            yield path


def line_has_string_lookup(line: str) -> bool:
    if not LOOKUP_RE.search(line):
        return False
    return '"' in line or "$\"" in line or "@\"" in line


def find_violations(scan_roots: Iterable[Path]) -> list[Violation]:
    violations: list[Violation] = []
    for path in iter_cs_files(scan_roots):
        try:
            content = path.read_text(encoding="utf-8-sig")
        except UnicodeDecodeError:
            content = path.read_text(encoding="utf-8")

        if is_generated_fairygui_file(path, content):
            continue

        relative_path = to_relative(path)
        for line_no, raw_line in enumerate(content.splitlines(), start=1):
            source = raw_line.strip()
            if not source or source.startswith("//") or source.startswith("*"):
                continue
            if ALLOW_MARKER in source:
                continue

            rules: list[str] = []
            if line_has_string_lookup(source):
                rules.append("string-lookup")
            if FGUI_CAST_RE.search(source):
                rules.append("fairygui-cast")
            if AS_PROPERTY_RE.search(source):
                rules.append("as-property")

            if not rules:
                continue

            violations.append(
                Violation(
                    relative_path=relative_path,
                    line_no=line_no,
                    rule="+".join(rules),
                    source=source,
                )
            )
    return violations


def read_baseline(path: Path) -> Counter[str]:
    if not path.exists():
        return Counter()
    entries: Counter[str] = Counter()
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        entries[stripped] += 1
    return entries


def write_baseline(path: Path, violations: Iterable[Violation]) -> None:
    entries = sorted(v.signature for v in violations)
    header = [
        "# FairyGUI typed-binding baseline.",
        "#",
        "# This file records existing string-based FairyGUI bindings in",
        "# hand-written UI code. The checker allows these exact signatures",
        "# and fails when new signatures appear.",
        "#",
        "# Do not add new entries for generated Controller code. Prefer",
        "# generated FairyGUI fields such as _view.btn_confirm.",
        "",
    ]
    path.write_text("\n".join(header + entries) + "\n", encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "paths",
        nargs="*",
        type=Path,
        help="Optional files or directories to scan. Defaults to project UI code.",
    )
    parser.add_argument(
        "--baseline",
        type=Path,
        default=DEFAULT_BASELINE,
        help=f"Baseline file path. Default: {DEFAULT_BASELINE.relative_to(ROOT)}",
    )
    parser.add_argument(
        "--update-baseline",
        action="store_true",
        help="Rewrite the baseline from current violations.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    scan_roots = [p if p.is_absolute() else ROOT / p for p in (args.paths or DEFAULT_SCAN_ROOTS)]
    baseline_path = args.baseline if args.baseline.is_absolute() else ROOT / args.baseline

    violations = find_violations(scan_roots)
    if args.update_baseline:
        write_baseline(baseline_path, violations)
        print(f"[UPDATE] Wrote {len(violations)} baseline entries to {to_relative(baseline_path)}")
        return 0

    baseline = read_baseline(baseline_path)
    current = Counter(v.signature for v in violations)
    example_by_signature = {v.signature: v for v in violations}

    new_signatures = sorted((current - baseline).elements())
    stale_signatures = sorted((baseline - current).elements())

    if not new_signatures and not stale_signatures:
        print(f"[PASS] FairyGUI typed-binding check passed ({len(current)} baseline entries).")
        return 0

    if new_signatures:
        print(f"[FAIL] Found {len(new_signatures)} new FairyGUI string-binding violation(s):")
        for signature in new_signatures:
            print(f"  {example_by_signature[signature].format()}")
        print()
        print("Use generated FairyGUI fields instead of GetChild/GetController/GetTransition.")
        print(f"If this is intentional legacy debt, review it and update {to_relative(baseline_path)} explicitly.")

    if stale_signatures:
        print(f"[STALE] Found {len(stale_signatures)} obsolete baseline entrie(s):")
        for signature in stale_signatures:
            print(f"  {signature}")
        print(f"Run with --update-baseline after confirming the cleanup.")

    return 1


if __name__ == "__main__":
    sys.exit(main())
