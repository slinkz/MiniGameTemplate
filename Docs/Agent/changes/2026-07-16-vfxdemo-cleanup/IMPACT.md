---
system: knowledge-engineering
scope: vfxdemo-cleanup-impact
status: active
created: 2026-07-16
---

# Impact

## Unity Assets

- Removed `UnityProj/Assets/_Example/VFXDemo/`.
- Moved still-used VFX assets to `UnityProj/Assets/_Example/DanmakuDemo/VFX/`.
- Preserved `.meta` GUIDs for assets referenced by DanmakuDemo.

## Menu / UI

- Removed `btnVFXDemo` from `UIProject/assets/MainMenu/MainMenuPanel.xml`.
- Removed generated binding field from `UnityProj/Assets/_Game/Scripts/UI/MainMenu/MainMenuPanel.cs`.
- Removed click binding and `SceneManager.LoadScene("VFXDemo")` logic from `MainMenuPanel.Logic.cs`.

## Build Settings

- Removed `Assets/_Example/VFXDemo/Scenes/VFXDemo.unity` from `UnityProj/ProjectSettings/EditorBuildSettings.asset`.

## Docs

- Updated current guide and asset-pipeline docs to describe two examples and no VFXDemo entry.
- Kept `CHANGELOG.md` and Archive records unchanged as historical records.

## Reminder

Because the FairyGUI source XML changed, the corresponding UI package must be re-published from FairyGUI Editor.
