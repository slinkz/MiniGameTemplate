---
system: knowledge-engineering
scope: vfxdemo-cleanup-validation
status: active
created: 2026-07-16
---

# Validation

## Executed

- Searched current assets, ProjectSettings, UI source, Guide docs, and Asset Pipeline docs for `VFXDemo`, `特效Demo`, `btnVFXDemo`, and `LoadScene("VFXDemo")`.
- Checked DanmakuDemo VFX GUIDs still resolve to the moved assets.
- Parsed `UIProject/assets/MainMenu/MainMenuPanel.xml` as XML.
- Confirmed no edits touched `UnityProj/Assets/_Framework/VFXSystem`.

## Not Executed

- Unity batchmode compile/import validation was not run because no `Unity.exe` or `Tuanjie.exe` was found in PATH, Unity Hub common path, Program Files, or the user directory.

## Residual Risk

- Until FairyGUI Editor publishes the updated MainMenu package, Unity-side exported UI assets may still reflect the old button layout.
