---
system: knowledge-engineering
scope: fairygui-typed-binding-gate-docs
status: active
created: 2026-07-15
---

# Doc Updates

## Updated

- `skills/fairygui-tools/SKILL.md`
- `skills/fairygui-tools/references/workflow-d-csharp-templates.md`
- `Docs/Agent/CONTEXT_PACKS/FairyGUI_UI.md`
- `Docs/Agent/INDEX.md`
- `Docs/Agent/KNOWLEDGE/KNOWLEDGE_INVENTORY.md`

## Added

- `Tools/check_fairygui_typed_bindings.py`
- `Tools/fairygui-typed-bindings-baseline.txt`
- `Docs/Agent/changes/2026-07-15-fairygui-typed-binding-gate/`

## Maintenance Note

When future work removes legacy string bindings, update the baseline with:

```bash
python Tools/check_fairygui_typed_bindings.py --update-baseline
```

Review the diff after updating the baseline. A smaller baseline is good; new entries need explicit justification.
