# Changelog

All notable changes to MiniGameTemplate will be documented in this file.

## [0.5.2] - 2026-04-07

### Added
- **Spine source integration (optional)**
  - Added git submodule: `UnityProj/ThirdParty/spine-runtimes` (branch `4.2`)
  - Added setup scripts: `UnityProj/Tools/setup_spine.bat` / `setup_spine.sh`
  - Setup creates source links:
    - `Assets/Spine` -> `ThirdParty/spine-runtimes/spine-unity/Assets/Spine`
    - `Assets/SpineCSharp` -> `ThirdParty/spine-runtimes/spine-csharp/src`
- **Spine integration editor tools** (`Tools -> MiniGame Template -> Integrations -> Spine`)
  - Enable/Disable defines for current target: `FAIRYGUI_SPINE`, `ENABLE_SPINE`
  - Validate integration status (source links, asmdef readiness, define consistency)
- **FairyGUI Spine helper**: `FairySpineHelper` for controlling `GLoader3D` playback via framework API

### Changed
- **Setup scripts hardening** (`setup_fairygui.*`, `setup_spine.*`)
  - Added source existence checks before deleting existing directories
  - Added explicit failure handling for submodule init / link creation
  - Added non-interactive flags for automation (`--force`, Windows also supports `--no-pause`)
- **ArchitectureValidator**: added optional Spine consistency check when `FAIRYGUI_SPINE` is enabled
- Updated docs (README / GETTING_STARTED / FAQ / FRAMEWORK_MODULES / ARCHITECTURE / NEWGAME_GUIDE / UISystem MODULE_README) for optional Spine workflow


## [0.5.1] - 2026-04-07

### Changed
- **ConfigManager: Lazy Deserialization** — 配置表系统从 Eager Load 改为延迟反序列化模式
  - `InitializeAsync()` 现在仅异步预加载全部 `.bytes` 到 `byte[]` 缓存（I/O only），不再在启动时反序列化
  - 每个表在首次访问属性时才执行反序列化（`Tables.TbXxx` lazy property getter）
  - 反序列化后自动调用 `ResolveRef()` 并释放原始 `byte[]` 缓存
  - 业务代码访问方式完全不变：`ConfigManager.Tables.TbItem.Get(id)` 零侵入
- **Luban Generated Code**: `Tables.cs` 改为 lazy property 模式（构造函数仅存储 loader，不执行反序列化）

### Added
- **ConfigManager.IsTableLoaded(fileName)**: 查询某表是否已完成反序列化的辅助方法

## [0.5.0] - 2026-04-06

### Added
- **IStartupFlow** interface (`_Framework/GameLifecycle/`): game-layer startup orchestration hook called by `GameBootstrapper` after system init
- **GameStartupFlow**: 3-phase startup implementation — loading screen with progress → privacy authorization (PrivacyDialog/ConfirmDialog) → fade out loading, open MainMenuPanel
- **WeChat Privacy API**: `IWeChatBridge` extended with `CheckPrivacyAuthorize()`, `RequirePrivacyAuthorize()`, `GetPrivacySettingName()`; `WeChatBridgeStub` tracks `_privacyAuthorized` state
- **UI panels** (FairyGUI white-box):
  - `LoadingPanel` — fullscreen loading screen with progress bar and status text (SortOrder = LAYER_LOADING = 600)
  - `PrivacyDialog` — privacy authorization dialog (SortOrder = LAYER_LOADING + 100 = 700, appears above loading)
  - `ConfirmDialog` — generic confirm dialog with configurable title/content/buttons (SortOrder = LAYER_LOADING + 100 = 700)
  - `MainMenuPanel` — main menu placeholder
  - `GlobalSpinner` — fullscreen spinner overlay
- **UIBase.IsFullScreen** virtual property (default `true`): fullscreen panels use `MakeFullScreen()`; non-fullscreen panels (dialogs) use `Center()` with center/middle relations
- **UIDialogBase.IsFullScreen** override → `false`: dialogs keep original size and center instead of stretching fullscreen
- **UIConstants layer constants**: LAYER_BACKGROUND(0), LAYER_NORMAL(100), LAYER_POPUP(200), LAYER_DIALOG(300), LAYER_TOAST(400), LAYER_GUIDE(500), LAYER_LOADING(600)
- **FairyGUI UI Packages** (white-box prototypes in UIProject/):
  - Common: LoadingPanel, PrivacyDialog, ConfirmDialog, GlobalSpinner, CommonButton, CommonProgressBar
  - MainMenu: MainMenuPanel, MenuIconButton
- **fairygui-tools Skill** (`.codebuddy/skills/fairygui-tools/`): AI Skill for FairyGUI workflow — mockup generation, XML creation, and structure analysis. Includes graph-based white-box rules, component closure principle, and validation script

### Fixed
- **PrivacyDialog invisible behind LoadingPanel**: Dialog SortOrder was 300 (LAYER_DIALOG) while LoadingPanel was 600 (LAYER_LOADING), making dialog hidden. Fixed by overriding SortOrder to LAYER_LOADING + 100 (700) for startup-phase dialogs
- **Dialogs stretched to fullscreen**: `UIBase.CreateAndShow()` called `MakeFullScreen()` on all panels including 600×500 dialogs. Fixed by adding `IsFullScreen` virtual property with `UIDialogBase` override

### Changed
- **GameBootstrapper**: now optionally runs `IStartupFlow.RunAsync()` after system init, before `LoadInitialScene()`. Catches `OperationCanceledException` as non-fatal (e.g., user rejects privacy authorization)
- **UIPackageLoader**: corrected `YooAssetBasePath` to `Assets/_Game/FairyGUI_Export/` and fixed path pattern from `{base}{pkg}/{pkg}_fui.bytes` to `{base}{pkg}_fui.bytes`

### Removed
- **ConfigManager Resources fallback**: removed `Resources/ConfigData/` copy step and sync `Initialize()` Resources path. All config loading now exclusively via YooAsset

## [0.4.0] - 2026-04-06

### Added
- **Luban v4.6.0**: compiled from source to `Tools/Luban/`, replacing legacy Luban CLI
- **Dual-format config system**: Binary for runtime + JSON for editor preview
  - Runtime loads `.bytes` via YooAsset (primary) or Resources (fallback)
  - JSON preview files in `Editor/ConfigPreview/` excluded from builds — no plaintext in shipped packages
- **TablesExtension.cs**: hand-written `partial class Tables` with `GetTableNames()` for pre-loading
- **luban.conf (v4.6.0)**: new-style config with `groups`, `schemaFiles` directory scanning, `topModule: "cfg"`
- **tables.xml**: consolidated bean + table definitions (replaces split `item.xml` / `globalconst.xml` / `__tables__.xml`)
- **luban-config Skill** (`.codebuddy/skills/luban-config/`): project skill with SOP, format references, and automation scripts
  - `scripts/create_xlsx.py`: auto-create Luban-compliant xlsx data files
  - `scripts/update_tables_extension.py`: auto-sync `TablesExtension.cs` from `tables.xml`

### Changed
- **ConfigManager**: rewritten from JSON text loading to Binary ByteBuf loading
  - `InitializeAsync()` pre-loads all `.bytes` asynchronously, then constructs `Tables` synchronously
  - `Initialize()` sync fallback also uses `.bytes` from Resources
  - `IntegrityVerifier` signature changed from `Func<string, string, bool>` to `Func<string, byte[], bool>`
- **gen_config.bat/sh**: rewritten for Luban v4.6.0 syntax, 3-step process:
  1. `cs-bin` + `bin` → Generated code + `_Game/ConfigData/*.bytes`
  2. `json` → `Editor/ConfigPreview/*.json`
  3. Copy `.bytes` to `Resources/ConfigData/` (fallback) — **removed in v0.3.0**
- **Generated code**: switched from `cs-simple-json` (JSONNode) to `cs-bin` (ByteBuf)
- **Luban data source**: switched from JSON (`*@filename.json`) to **xlsx** for designer-friendly Excel editing

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
  - JSON data files at `_Game/ConfigData/` (YooAsset)
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
- **gen_config scripts**: output data to `_Game/ConfigData/`
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
