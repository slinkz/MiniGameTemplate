---
system: changes
scope: danmaku-demo-cleanup
status: completed
created: 2026-07-18
related_docs: Docs/Agent/DEMO_CLEANUP_SOP.md
---

# Danmaku Demo Cleanup Summary

## Intent

Remove the standalone DanmakuDemo and EntityDemo sample scenes while preserving Danmaku runtime/framework capability and `_Game` references that still use former sample assets.

## User Decisions

1. Delete the whole `Assets/_Example/DanmakuDemo/` folder after migrating reused assets.
2. Migrate all assets still referenced by `_Game`.
3. Delete `EntityDemo.unity` and remove it from Build Settings.
4. Remove the `弹幕Demo` main menu entry and keep ClickGame as the only sample entry.
5. Update FairyGUI source XML, generated binding, and menu logic consistently.
6. Update the hardcoded `EntityTemplateSO_Creator` pattern path.
7. Replace DanmakuDemo as the VFX validation target in active docs.
8. Update current docs while preserving history/archive records.
9. Add this cleanup case as a reusable SOP reference.
10. Attempt Unity validation; if Unity is unavailable, perform text/GUID/XML checks and report.

## Result

DanmakuDemo scene, EntityDemo scene, DanmakuDemo scripts, sample-only assets, menu entry, and Build Settings entries were removed. `_Game` assets that still depend on former Danmaku sample assets were moved under `_Game` while keeping their `.meta` GUIDs stable.
