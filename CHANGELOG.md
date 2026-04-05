# Changelog

All notable changes to MiniGameTemplate will be documented in this file.

## [0.2.0] - 2026-04-05

### Fixed
- **UIManager.CloseAllPanels**: snapshot-then-clear pattern prevents `InvalidOperationException` from iterator invalidation when `panel.Close()` modifies the dictionary
- **ClickGameManager / HighScoreSaver**: use `GameBootstrapper.SaveSystem` instead of creating duplicate `PlayerPrefsSaveSystem` instances (fixes FlushIfDirty bypass)
- **TimerService**: `_nextId` overflow wrap-around protection (wraps from `int.MaxValue` → 1, skipping invalid 0)
- **AudioImportEnforcer**: static `HashSet` guard prevents recursive reimport loop in `OnPostprocessAudio`
- **WeChatBridgeStub**: replaced per-call temporary `GameObject` + private `CoroutineRunner` with framework's `CoroutineRunner.Run()` (eliminates GC waste and class name shadowing)
- **ScoreDisplay**: replaced `UnityEngine.Debug.Log` with `GameLog.Log` (consistent with project convention, stripped in release builds)
- **UIDialogBase**: fix `GGraph.DrawRect` missing width/height/lineColor params (CS7036)
- **AssetService**: rename `UnloadUnusedAssets` → `UnloadUnusedAssetsAsync`, `ForceUnloadAllAssets` → `UnloadAllAssetsAsync` (YooAsset 2.3.18 API)
- **AssetService**: `OnDestroy` now properly overrides `Singleton<T>.OnDestroy()` (CS0114)
- **ConfigManager**: suppress CS1998 warning with `await Task.CompletedTask` placeholder
- **AssetImportEnforcer/AssetAuditWindow**: replace non-existent `AudioImporterSampleSettings.overridden` with `AudioImporter.ContainsSampleSettingsOverride()` (CS1061)
- **BuildPipeline**: add `using UnityEditor.Build` for `Il2CppCodeGeneration` enum and wrap with `#if UNITY_2022_3_OR_NEWER` (CS0103)

### Changed
- **Docs/CONVENTIONS.md**: expanded from ~148 lines to ~460 lines with comprehensive Agent coding rules covering:
  - Logging, error handling, async/await, GC optimization, WebGL constraints
  - Security (input validation, HTTPS, HMAC, PII protection, conditional compilation)
  - Framework system usage (SaveSystem, events, timers, UI, assets, WeChat bridge)
  - Module dependency graph (L0–L6), SO design patterns quick reference
  - Collection iteration safety, Agent pre-commit checklist (12 items)

## [0.1.0] - 2026-04-05

### Added
- Initial project skeleton
- Framework module structure with MODULE_README placeholders
- Core architecture: ScriptableObject-driven event system, data variables, runtime sets
- FairyGUI integration scaffold
- Audio, ObjectPool, FSM, Timer systems
- WeChat Bridge interface layer
- Debug tools
- Editor extensions (PropertyDrawers, menu items, architecture validator)
- Luban config table integration
- Example game scaffold
- Project documentation suite
