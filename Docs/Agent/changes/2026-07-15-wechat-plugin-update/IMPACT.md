---
system: wechat
scope: plugin-update-impact
status: complete
created: 2026-07-15
related_docs: Docs/Agent/PLATFORM/WECHAT_INTEGRATION.md
---

# Impact

## Code Paths

- `UnityProj/Packages/com.qq.weixin.minigame/**`
- `UnityProj/Assets/WebGLTemplates/**`
- `UnityProj/Packages/com.qq.weixin.minigame/Editor/WXPluginVersion.cs`
- `UnityProj/Packages/com.qq.weixin.minigame/package.json`

## Project Assets

- `UnityProj/Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset` was preserved as the project configuration source.
- `UnityProj/Assets/WX-WASM-SDK-V2/Runtime` remains minimal in this repo and must not contain a duplicate full SDK runtime.

## Compatibility Notes

- Updating while Unity is open can fail because Windows locks plugin DLLs.
- A mistaken duplicate Runtime creates Unity Safe Mode errors for duplicate `wx-perf.dll` and assembly name `Wx`.
