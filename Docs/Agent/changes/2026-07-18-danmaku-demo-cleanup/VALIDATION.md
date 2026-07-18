---
system: changes
scope: danmaku-demo-cleanup-validation
status: completed
created: 2026-07-18
---

# Validation

## Required Checks

- Search active code/docs for removed menu identifiers and scene paths.
- Confirm `EditorBuildSettings.asset` no longer references `DanmakuDemo.unity` or `EntityDemo.unity`.
- Parse `UIProject/assets/MainMenu/MainMenuPanel.xml`.
- Confirm migrated GUIDs still exist under `_Game`.
- Run Unity batchmode compile/import validation if `Unity.exe` or `Tuanjie.exe` is available.

## Result

- `MainMenuPanel.xml` parsed successfully.
- Active code/docs and Build Settings no longer reference removed DanmakuDemo menu identifiers or scene entries.
- Deleted scene GUIDs are no longer referenced by active Unity assets or ProjectSettings.
- Deleted asset GUIDs that are still referenced have replacement `.meta` files under `_Game`.
- Unity batchmode was attempted with `D:\Program Files\Unity\2021.3.45f2c1\Editor\Unity.exe`, but the project was already open in another Unity instance, so compile/import validation did not run.

## Follow-Up

- Re-publish the MainMenu FairyGUI package because source XML changed.
- Rebuild YooAsset Simulate/package artifacts so generated manifests drop old DanmakuDemo paths.
