---
system: knowledge-engineering
scope: fairygui-typed-binding-gate-impact
status: active
created: 2026-07-15
---

# Impact

## Code

- `Tools/check_fairygui_typed_bindings.py` scans hand-written UI code under:
  - `UnityProj/Assets/_Game/Scripts/ShooterGame/UI`
  - `UnityProj/Assets/_Game/Scripts/UI`
- FairyGUI generated files are excluded when they contain the generated-file marker and are not `.Logic.cs`.

## Baseline

`Tools/fairygui-typed-bindings-baseline.txt` records 0 entries after the cleanup pass. The checker fails on new string-based FairyGUI bindings.

Baseline should stay at 0. Do not add new generated Controller code to the baseline unless the exception is reviewed and intentional.

## First Cleanup Pass

- `SkillCDPanel` now creates `SG_Battle.SkillSlot` instances and uses generated `state` / `cd_bar` fields.
- `PassiveIndicatorPanel` now creates `SG_Battle.PassiveSlot` instances and uses generated `state` / `cd_progress` / `active_progress` fields.
- `LoadingPanel.Logic` now uses `ProgressTitleType.Percent` instead of reading the progress bar title child.
- `DefeatPanelController` no longer has an unused generic text fallback.
- `VictoryPanelController` now matches the current `VictoryPanel.xml` only: kills, HP, stars, confirm.
- `PausePanelController` now matches the current `PausePanel.xml` only: resume, quit, mask.
- `DefeatPanelController` no longer references missing unlock hint UI.
- `UnlockPopupController` and `BattleStartSequence` were removed because no matching UI component / call site exists.

## Docs And Skills

- `skills/fairygui-tools/SKILL.md` now includes the strong-typed binding rule.
- `skills/fairygui-tools/references/workflow-d-csharp-templates.md` now shows allowed and forbidden C# patterns.
- `Docs/Agent/CONTEXT_PACKS/FairyGUI_UI.md` now routes agents to the check.
- `Docs/Agent/INDEX.md` now mentions the check in the FairyGUI route.
