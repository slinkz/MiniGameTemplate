---
system: wechat
scope: plugin-update-validation
status: complete
created: 2026-07-15
related_docs: Docs/Agent/TOOLS/MCP_INTEGRATION.md
---

# Validation

## Executed

- Queried official plugin info endpoint.
- Downloaded and reconstructed official UnityPackage.
- Reopened Unity after syncing SDK files.
- Verified Unity MCP connection to `UnityProj` on port `7890`.
- Ran `unity_editor_state`: `isCompiling=false`.
- Ran `unity_get_compilation_errors` with `severity=all`: `count=0`, `entries=[]`.

## Failure Recovered

An intermediate attempt copied the official Runtime into `Assets/WX-WASM-SDK-V2/Runtime`, causing duplicate WeChat assemblies. The duplicate Runtime was removed and Unity reimported successfully.

## Not Executed

- Full WeChat developer tool export.
- Device-side WeChat smoke test.
