---
name: wechat-minigame-plugin-update
description: Safely update the WeChat Mini Game Unity SDK in MiniGameTemplate. Use when asked to update, verify, diagnose, or document the WeChat Unity minigame plugin/package, especially com.qq.weixin.minigame, WX-WASM-SDK-V2, plugin update popups, UnityPackage imports, locked plugin DLLs, duplicated wx assemblies, or MCP compile validation after SDK changes.
---

# WeChat Mini Game Plugin Update

## Core Workflow

1. Read `references/update-playbook.md` before changing files.
2. Query the official plugin info endpoint for the real latest version:
   `https://game.weixin.qq.com/cgi-bin/gamewxagwasmsplitwap/getunityplugininfo`.
3. Compare it with:
   - `UnityProj/Packages/com.qq.weixin.minigame/Editor/WXPluginVersion.cs`
   - `UnityProj/Packages/com.qq.weixin.minigame/package.json`
4. Download the UnityPackage from `data.info.url`, reconstruct it in a temp directory, and inspect paths before copying.
5. For this repo, update the embedded package at `UnityProj/Packages/com.qq.weixin.minigame`.
6. Do not copy a full duplicate SDK Runtime into `UnityProj/Assets/WX-WASM-SDK-V2/Runtime`; this project uses the embedded package runtime. Duplicating it causes duplicate asmdef/DLL compile errors.
7. Preserve project config assets under `UnityProj/Assets/WX-WASM-SDK-V2/Editor`, especially `MiniGameConfig.asset`.
8. Close Unity if Windows locks plugin DLLs, then complete the file sync.
9. Reopen Unity and verify compilation through Unity MCP. If Unity enters Safe Mode and MCP is unavailable, read `Editor.log`, fix the import issue, reopen Unity, then verify through MCP.

## Version Rules

- Treat `data.info.version` as the plugin timestamp version.
- Treat the URL suffix after `#` as the human package version, for example `#0.1.34`.
- Do not blindly trust the UnityPackage's internal `package.json`; it may contain an older placeholder version.
- Update local `package.json` to the human package version only after confirming the official endpoint response.

## Validation Gate

After any SDK file change:

1. Start or focus Unity.
2. Use MCP, not the internal HTTP bridge.
3. Check `unity_editor_state` until `isCompiling=false`.
4. Run `unity_get_compilation_errors` with `severity=all`.
5. Report the exact result. Success requires `count: 0`.

If the error mentions duplicate assemblies such as `wx-perf.dll`, `wx-runtime.dll`, or assembly name `Wx`, remove the accidental duplicate Runtime under `Assets/WX-WASM-SDK-V2/Runtime` and retry import.

## Knowledge Updates

For a completed SDK update, update knowledge assets:

- Add or update a change package under `Docs/Agent/changes/YYYY-MM-DD-wechat-plugin-update/`.
- Update `Docs/Agent/KNOWLEDGE/KNOWLEDGE_INVENTORY.md` if the skill list changes.
- Update `Docs/Agent/INDEX.md` or `Docs/Agent/PLATFORM/WECHAT_INTEGRATION.md` when the task route or platform workflow changes.
- Run the repository knowledge checks when practical.
