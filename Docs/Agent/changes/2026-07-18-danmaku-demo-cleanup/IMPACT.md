---
system: changes
scope: danmaku-demo-cleanup-impact
status: completed
created: 2026-07-18
---

# Impact

## Removed

- `UnityProj/Assets/_Example/DanmakuDemo/`
- `UnityProj/Assets/_Game/Scripts/Demo/EntityDemoSetup.cs`
- `UnityProj/Assets/_Game/Scripts/Demo/EntityDemoInputBridge.cs`
- `DanmakuDemo` and `EntityDemo` entries from `EditorBuildSettings.asset`
- Main menu `弹幕Demo` UI entry and click handler

## Preserved Capability

- `UnityProj/Assets/_Framework/DanmakuSystem/`
- `UnityProj/Assets/_Framework/VFXSystem/`
- `UnityProj/Assets/_Framework/Rendering/`
- `_Game` battle and shooter configs that still reference migrated assets

## Migrated Assets

Assets still referenced by `_Game` were moved to:

- `UnityProj/Assets/_Game/Configs/Danmaku/`
- `UnityProj/Assets/_Game/Textures/Danmaku/`

The move preserved `.meta` GUIDs so existing scene/config references continue to resolve.

## Generated Artifacts

The current YooAsset Simulate package still contains generated references to old DanmakuDemo paths until the package is rebuilt. Do not hand-edit its JSON/bytes/hash files independently.
