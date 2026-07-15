---
system: wechat
scope: plugin-update
status: complete
created: 2026-07-15
related_docs: Docs/Agent/PLATFORM/WECHAT_INTEGRATION.md, skills/wechat-minigame-plugin-update/SKILL.md
---

# WeChat Plugin Update

## Motivation

The WeChat Mini Game Unity plugin showed an update popup on every game run. The project was using plugin timestamp `202603160259`, while the official endpoint reported `202606220647`.

## Change Summary

- Updated the embedded SDK package under `UnityProj/Packages/com.qq.weixin.minigame`.
- Set `WXPluginVersion.cs` to `202606220647`.
- Set package display version to `0.1.34` based on the official URL suffix.
- Synced existing WebGL templates from the official UnityPackage.
- Added `skills/wechat-minigame-plugin-update` to preserve the update workflow.

## Key Decisions

- Use the official endpoint as the source of truth.
- Keep this repo's embedded package structure instead of importing a second full Runtime under `Assets/WX-WASM-SDK-V2/Runtime`.
- Preserve project config assets such as `MiniGameConfig.asset`.
- Treat MCP compile validation as the required final gate.
