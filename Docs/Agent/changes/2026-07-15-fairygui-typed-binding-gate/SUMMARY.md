---
system: knowledge-engineering
scope: fairygui-typed-binding-gate
status: active
created: 2026-07-15
---

# FairyGUI Typed Binding Gate

## Summary

Added a repository check that prevents new string-based FairyGUI bindings in hand-written UI code.

The previous victory OK bug was caused by a silent mismatch between a hand-written string binding and the generated FairyGUI field. This change turns that class of issue into an explicit local validation failure.

## Changed

- Added `Tools/check_fairygui_typed_bindings.py`.
- Added `Tools/fairygui-typed-bindings-baseline.txt` for existing legacy/dynamic bindings.
- Updated `skills/fairygui-tools` to require generated fields in Controller / `.Logic.cs` code.
- Updated FairyGUI context routing and validation notes.
- Converted generated-class-ready legacy bindings in `SkillCDPanel`, `PassiveIndicatorPanel`, `LoadingPanel.Logic`, and `DefeatPanelController`.
- Removed UI-code-only dead paths whose FairyGUI XML / generated classes did not exist: victory contribution details, pause hidden stats/build/buff code, defeat unlock hint, `UnlockPopupController`, and `BattleStartSequence`.

## Rule

New or rewritten business UI code must use generated FairyGUI fields such as `_view.btn_confirm` or `btnConfirm`.

Do not add new `GetChild("...")`, `GetController("...")`, `GetTransition("...")`, `as GButton`, or similar manual bindings in hand-written Controller code.

UI components and Controller code must appear as a pair. If there is no XML / generated class, do not keep Controller logic for it.
