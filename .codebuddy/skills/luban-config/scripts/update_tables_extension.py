#!/usr/bin/env python3
"""
update_tables_extension.py — 从 tables.xml 自动生成 TablesExtension.cs。

读取 DataTables/Defs/tables.xml 中所有 <table> 定义，
提取表名并生成 TablesExtension.cs 中的 GetTableNames() 方法。

Usage:
    python update_tables_extension.py [--project-root <path>]

    --project-root: Unity 工程根目录（包含 DataTables/ 和 Assets/），
                    默认自动检测（从脚本位置向上查找 UnityProj/）。

Example:
    python update_tables_extension.py
    python update_tables_extension.py --project-root UnityProj
"""

import argparse
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


# Template for TablesExtension.cs
TEMPLATE = '''// Hand-written partial extension for Luban-generated Tables class.
// Provides table name metadata for ConfigManager pre-loading.
// This file is NOT auto-generated and should be preserved across Luban regenerations.
//
// NOTE: The namespace "cfg" is lowercase because it must match the Luban-generated code
// (controlled by luban.conf topModule setting). This is intentional, not a style violation.
//
// AUTO-UPDATED by update_tables_extension.py — do not manually edit the GetTableNames() array.

namespace cfg
{{
    public partial class Tables
    {{
        /// <summary>
        /// Returns all table file names that the Tables constructor will request from the loader.
        /// Must match the lowercase names used in the generated Tables constructor.
        /// </summary>
        public static string[] GetTableNames()
        {{
            return new string[]
            {{
{entries}
            }};
        }}
    }}
}}
'''


def find_project_root(start_path: str) -> str:
    """Walk up from start_path to find the directory containing DataTables/."""
    current = Path(start_path).resolve()
    # Check if current path has DataTables/ directly
    if (current / "DataTables").is_dir():
        return str(current)
    # Try going up to find UnityProj or a dir containing DataTables
    for parent in current.parents:
        if (parent / "DataTables").is_dir():
            return str(parent)
        if (parent / "UnityProj" / "DataTables").is_dir():
            return str(parent / "UnityProj")
    return None


def parse_table_names(tables_xml_path: str) -> list:
    """Parse tables.xml and return list of table names."""
    tree = ET.parse(tables_xml_path)
    root = tree.getroot()
    names = []
    for table in root.iter("table"):
        name = table.get("name")
        if name:
            names.append(name)
    return sorted(names)


def generate_extension(table_names: list) -> str:
    """Generate the TablesExtension.cs content."""
    entries = ""
    for name in table_names:
        lower_name = name.lower()
        entries += f'                "{lower_name}",\n'
    return TEMPLATE.format(entries=entries.rstrip("\n"))


def main():
    parser = argparse.ArgumentParser(
        description="Auto-update TablesExtension.cs from tables.xml."
    )
    parser.add_argument(
        "--project-root",
        help="Unity project root (containing DataTables/ and Assets/). Auto-detected if omitted.",
    )
    args = parser.parse_args()

    # Resolve project root
    if args.project_root:
        project_root = Path(args.project_root).resolve()
    else:
        # Try to auto-detect from script location
        script_dir = Path(__file__).resolve().parent
        detected = find_project_root(str(script_dir))
        if detected:
            project_root = Path(detected)
        else:
            print("ERROR: Cannot auto-detect project root. Use --project-root.", file=sys.stderr)
            sys.exit(1)

    tables_xml = project_root / "DataTables" / "Defs" / "tables.xml"
    extension_cs = (
        project_root
        / "Assets"
        / "_Framework"
        / "DataSystem"
        / "Scripts"
        / "Config"
        / "TablesExtension.cs"
    )

    if not tables_xml.is_file():
        print(f"ERROR: tables.xml not found at {tables_xml}", file=sys.stderr)
        sys.exit(1)

    # Parse table names
    table_names = parse_table_names(str(tables_xml))
    if not table_names:
        print("WARNING: No <table> elements found in tables.xml!", file=sys.stderr)

    print(f"Found {len(table_names)} table(s) in {tables_xml}:")
    for name in table_names:
        print(f"  - {name} -> \"{name.lower()}\"")

    # Generate and write
    content = generate_extension(table_names)

    # Ensure output directory exists
    extension_cs.parent.mkdir(parents=True, exist_ok=True)
    extension_cs.write_text(content, encoding="utf-8")

    print(f"\nOK: Updated {extension_cs}")
    print(f"    Table names: {[n.lower() for n in table_names]}")


if __name__ == "__main__":
    main()
