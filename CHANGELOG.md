# Changelog

All notable changes to MiniGameTemplate will be documented in this file.

## [0.4.0] - 2026-04-06

### Added
- **Luban v4.6.0**: compiled from source to `Tools/Luban/`, replacing legacy Luban CLI
- **Dual-format config system**: Binary for runtime + JSON for editor preview
  - Runtime loads `.bytes` via YooAsset (primary) or Resources (fallback)
  - JSON preview files in `Editor/ConfigPreview/` excluded from builds — no plaintext in shipped packages
- **TablesExtension.cs**: hand-written `partial class Tables` with `GetTableNames()` for pre-loading
- **luban.conf (v4.6.0)**: new-style config with `groups`, `schemaFiles` directory scanning, `topModule: "cfg"`
- **tables.xml**: consolidated bean + table definitions (replaces split `item.xml` / `globalconst.xml` / `__tables__.xml`)

### Changed
- **ConfigManager**: rewritten from JSON text loading to Binary ByteBuf loading
  - `InitializeAsync()` pre-loads all `.bytes` asynchronously, then constructs `Tables` synchronously
  - `Initialize()` sync fallback also uses `.bytes` from Resources
  - `IntegrityVerifier` signature changed from `Func<string, string, bool>` to `Func<string, byte[], bool>`
- **gen_config.bat/sh**: rewritten for Luban v4.6.0 syntax, 3-step process:
  1. `cs-bin` + `bin` → Generated code + `_Game/ConfigData/*.bytes`
  2. `json` → `Editor/ConfigPreview/*.json`
  3. Copy `.bytes` to `Resources/ConfigData/` (fallback)
- **Generated code**: switched from `cs-simple-json` (JSONNode) to `cs-bin` (ByteBuf)
- **Luban input syntax**: `*@filename.json` for multi-record JSON files (v4.x)

### Removed
- Legacy Luban v2/v3 CLI dependency and `--gen_types` command syntax
- JSON runtime loading in ConfigManager (replaced by Binary)
- Plaintext JSON from `_Game/ConfigData/` (now only `.bytes`)
- Legacy Luban definition files: `__root__.xml`, `__tables__.xml`, `globalconst.xml`, `item.xml` (consolidated into `tables.xml`)

## [0.3.0] - 2026-04-05

### Added
- **Luban Config System**: fully wired Luban-generated config tables with runtime loading
  - `GlobalConst` table (key/stringValue/intValue) with HelloWorld test data
  - `TbItem` / `TbGlobalConst` generated table classes under `_Framework/DataSystem/Scripts/Config/Generated/`
  - `Tables.cs` async/sync factory with null-safety checks on loader return values
  - JSON data files at `_Game/ConfigData/` (YooAsset) + `Resources/ConfigData/` (fallback)
  - Luban table definitions at `DataTables/Defs/` with data sources at `DataTables/Datas/`
- **luban_unity package**: added `com.code-philosophy.luban` (Git URL) to `manifest.json`
- **Luban.Runtime asmdef reference**: added to `MiniGameFramework.Runtime.asmdef`
- **Config verification**: `GameBootstrapper` logs GlobalConst data on startup (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`)

### Fixed
- **AssetService**: added `RequestPackageVersionAsync` + `UpdatePackageManifestAsync` after YooAsset init — fixes `ActiveManifest == null` crash on all asset loads
- **ConfigManager**: wrapped YooAsset `LoadAssetAsync` in try-catch — graceful degradation to `Resources.Load` when YooAsset throws
- **ConfigManager**: `ResetStatics` now clears `_tables` (domain reload safety)
- **ConfigManager**: `ReloadAsync`/`Reload` now nulls `_tables` before re-init

### Changed
- **ConfigManager**: activated real Luban integration — replaced TODO stubs with actual `cfg.Tables.CreateAsync` / `cfg.Tables.Create` calls
- **ConfigManager**: `YooAssetConfigPath` updated from `Assets/ConfigData/` to `Assets/_Game/ConfigData/`
- **gen_config scripts**: output data to `_Game/ConfigData/` + auto-sync copy to `Resources/ConfigData/`
- **CONVENTIONS.md**: corrected ConfigManager path reference
- **Luban README**: corrected output data path
- **.gitignore**: added `UnityProj/.vs/`

## [0.2.2] - 2026-04-05

### Fixed
- **YooAsset**: configure `AssetBundleCollectorSetting.asset` with `DefaultPackage` + `GameAssets` collector group — resolves `Not found package : DefaultPackage` error in editor simulate mode
- **GameBootstrapper**: fix duplicate instance warning by skipping scene load when already in target scene (Boot → Boot circular load)
- **GameBootstrapper**: add `_isPrimaryInstance` flag so duplicate instances' `OnDestroy` does not reset `_hasBooted`
- **ArchitectureValidator**: remove duplicate `[MenuItem]` attribute (unified via `MenuItems.cs`)
- **Analytics SDK**: remove `com.unity.analytics` and `com.unity.modules.unityanalytics` to eliminate `No cloud project ID` error

### Changed
- **manifest.json**: remove 14 unused Unity packages (Ads, Analytics, Purchasing, XR, VR, Cloth, Terrain, Vehicles, Video, Wind, Umbra) to reduce build size and compile time
- **.gitignore**: add rules for `.vsconfig`, `Bundles/`, `ResolvedPackageCache`, `FairyGUI.meta`

## [0.2.1] - 2026-04-05

### Fixed
- **AssetService**: `UnloadUnusedAssetsAsync` / `ForceUnloadAllAssetsAsync` — add initialization guard with warning log instead of silently returning null (prevents downstream NRE when awaiting result)
- **setup_fairygui.bat/.sh**: add user confirmation prompt before deleting existing non-junction/non-symlink `Assets/FairyGUI` directory (prevents accidental data loss)
- **ConfigManager.InitializeAsync**: remove unnecessary `async` keyword + `await Task.CompletedTask` — now returns `Task.CompletedTask` directly to avoid allocating an async state machine on WebGL
- **UIDialogBase**: replace `MakeFullScreen()` + hardcoded `DrawRect` size with `AddRelation(GRoot.inst, RelationType.Size)` so modal overlay properly resizes on screen orientation/resolution changes
- **TextureImportEnforcer**: remove redundant Android/iPhone platform overrides — target platform is WebGL only (WeChat Mini Game); simplify `SetPlatformCompression` accordingly
- **AssetAuditWindow**: add audit check for textures missing WebGL platform override (previously only checked for uncompressed RGBA32)
- **BuildPipeline → MiniGameBuildPipeline**: rename class to avoid ambiguity with `UnityEditor.BuildPipeline`

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
