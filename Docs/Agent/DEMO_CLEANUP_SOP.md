---
system: general
scope: demo-cleanup-sop
status: active
created: 2026-07-16
related_docs: Docs/Agent/changes/2026-07-16-vfxdemo-cleanup/SUMMARY.md
---

# Demo Cleanup SOP

Use this when removing sample demos, menu entries, test scenes, or example assets that may touch Unity scenes, SO assets, FairyGUI, Build Settings, and docs.

## Decision-First Workflow

Before editing files, list the questions that need user decisions. Each question should include options, tradeoffs, and a recommendation. Execute only after the user chooses.

Recommended decision list:

1. Cleanup scope: only the standalone demo / also remove dependent presentation in other demos / remove framework capability.
2. Shared assets: migrate and preserve references / clear references / leave temporary remnants.
3. New asset home: owning demo / common example folder / production game folder.
4. Main menu behavior: delete entry / redesign section / hide only.
5. FairyGUI handling: update source XML, generated binding, and logic together / update only one side / wait for tool generation.
6. Documentation scope: update current docs and keep changelog/history / rewrite history only if explicitly requested.
7. Acceptance entry: replace removed demo names with surviving demos or business chains.
8. Validation: Unity batchmode if available; otherwise report why it could not run and do text/GUID/XML checks.

## Execution Order

1. Run `git status --short` and avoid overwriting user changes.
2. Search for entry names, scene names, button names, GUIDs, and doc references with `rg`.
3. Move still-referenced assets first, including their `.meta` files, so GUID references stay valid.
4. Remove deleted scenes from `UnityProj/ProjectSettings/EditorBuildSettings.asset`.
5. Remove menu entry consistently across FairyGUI source XML, generated C# binding, and `.Logic.cs`.
6. Delete the demo directory and assets used only by that demo.
7. Update current docs. Keep `CHANGELOG.md` and Archive docs as historical records unless the user explicitly says otherwise.
8. Search for residual names and orphaned GUIDs; parse modified XML.
9. Run Unity batchmode compile/import validation. If `Unity.exe` / `Tuanjie.exe` is unavailable, say that plainly.

## FairyGUI Rule

If any file under `UIProject/assets/**` changes, the final response must remind the user to re-publish the affected FairyGUI package from FairyGUI Editor and regenerate/update the corresponding Unity-side exported UI assets and C# binding.

## Reference Case

See `Docs/Agent/changes/2026-07-16-vfxdemo-cleanup/`.
