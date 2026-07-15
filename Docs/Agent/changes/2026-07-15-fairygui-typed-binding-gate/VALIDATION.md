---
system: knowledge-engineering
scope: fairygui-typed-binding-gate-validation
status: active
created: 2026-07-15
---

# Validation

Run from repository root:

```bash
python Tools/check_fairygui_typed_bindings.py
```

Expected result:

```text
[PASS] FairyGUI typed-binding check passed (0 baseline entries).
```

Unity compile check:

```text
MCP unity_editor_state(port=7890): isCompiling=false
MCP unity_get_compilation_errors(port=7890, severity=all): count=0
```

For Skill validation:

```bash
python C:/Users/traimenxu/.codex/skills/.system/skill-creator/scripts/quick_validate.py skills/fairygui-tools
```

For knowledge validation:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/knowledge-sync-check.ps1
python Tools/knowledge-consistency-check.py --allow-warnings
```

Unity MCP compile validation is not required for this change because only repository tooling and documentation were edited.
