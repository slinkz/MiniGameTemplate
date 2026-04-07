# 变更日志

MiniGameTemplate 的所有重要变更都会记录在本文件中。

## [0.5.3] - 2026-04-07

### 新增
- **ClickCounter 独立面板骨架**
  - 新增 `ClickCounterPanel` 运行时代码：`UnityProj/Assets/_Game/Scripts/UI/ClickCounterPanel.cs`
  - 新增 FairyGUI 组件源文件：`UIProject/assets/MainMenu/ClickCounterPanel.xml`
  - `UIConstants` 增加组件常量：`COMP_CLICK_COUNTER_PANEL`

### 变更
- **MainMenuPanel 内置可玩回退模式**
  - 当 `GameStartupFlow._startGameEvent` 未配置时，主菜单点击“开始游戏”将直接进入内置 ClickCounter 玩法
  - 支持开始、点击加分、倒计时、结算、重开、返回、分享、最高分本地保存
- **示例展示脚本完善**
  - `ScoreDisplay` 与 `CountdownDisplay` 去除 TODO 占位，改为可运行日志输出并在 `OnEnable` 时主动刷新一次
- **文档更新**
  - 更新 `_Example/README.md` 与 `Docs/Guide/EXAMPLE_WALKTHROUGH.md`，补充当前可玩路径与独立面板启用说明

### 说明
- `FairyGUI_Export/` 目录由 FairyGUI 编辑器发布生成；本次提交只更新了 UIProject 源文件。启用独立面板模式时，需要在 FairyGUI 编辑器重新发布 MainMenu 包。

## [0.5.2] - 2026-04-07


### 新增
- **Spine 源码接入（可选）**
  - 新增 git 子模块：`UnityProj/ThirdParty/spine-runtimes`（分支 `4.2`）
  - 新增初始化脚本：`UnityProj/Tools/setup_spine.bat` / `setup_spine.sh`
  - 初始化脚本会创建源码链接：
    - `Assets/Spine` -> `ThirdParty/spine-runtimes/spine-unity/Assets/Spine`
    - `Assets/SpineCSharp` -> `ThirdParty/spine-runtimes/spine-csharp/src`
- **Spine 集成编辑器工具**（`Tools -> MiniGame Template -> Integrations -> Spine`）
  - 支持当前目标平台一键启用/禁用脚本宏：`FAIRYGUI_SPINE`、`ENABLE_SPINE`
  - 支持校验集成状态（源码链接、asmdef 就绪情况、宏一致性）
- **FairyGUI Spine 辅助类**：新增 `FairySpineHelper`，通过框架 API 控制 `GLoader3D` 播放

### 变更
- **初始化脚本加固**（`setup_fairygui.*`、`setup_spine.*`）
  - 在删除已存在目录前先校验 source 路径是否存在
  - 为子模块初始化/链接创建增加明确失败处理
  - 增加自动化无交互参数（`--force`，Windows 额外支持 `--no-pause`）
- **ArchitectureValidator**：当启用 `FAIRYGUI_SPINE` 时，增加 Spine 可选接入一致性校验
- 更新文档（README / GETTING_STARTED / FAQ / FRAMEWORK_MODULES / ARCHITECTURE / NEWGAME_GUIDE / UISystem MODULE_README），补充 Spine 可选接入工作流

## [0.5.1] - 2026-04-07

### 变更
- **ConfigManager：Lazy Deserialization** —— 配置表系统从 Eager Load 改为延迟反序列化模式
  - `InitializeAsync()` 现在仅异步预加载全部 `.bytes` 到 `byte[]` 缓存（仅 I/O），不再在启动阶段反序列化
  - 每张表在首次访问属性时才执行反序列化（`Tables.TbXxx` 延迟属性 getter）
  - 反序列化后自动调用 `ResolveRef()` 并释放原始 `byte[]` 缓存
  - 业务代码访问方式保持不变：`ConfigManager.Tables.TbItem.Get(id)`，零侵入
- **Luban 生成代码**：`Tables.cs` 改为延迟属性模式（构造函数仅保存 loader，不执行反序列化）

### 新增
- **ConfigManager.IsTableLoaded(fileName)**：用于查询某张表是否已完成反序列化的辅助方法

## [0.5.0] - 2026-04-06

### 新增
- **IStartupFlow 接口**（`_Framework/GameLifecycle/`）：由 `GameBootstrapper` 在系统初始化后调用的游戏层启动编排钩子
- **GameStartupFlow**：三阶段启动流程实现——加载界面进度 → 隐私授权（PrivacyDialog/ConfirmDialog）→ 隐去加载界面并打开 MainMenuPanel
- **微信隐私 API**：`IWeChatBridge` 扩展 `CheckPrivacyAuthorize()`、`RequirePrivacyAuthorize()`、`GetPrivacySettingName()`；`WeChatBridgeStub` 跟踪 `_privacyAuthorized` 状态
- **UI 面板**（FairyGUI 白模）
  - `LoadingPanel` —— 全屏加载界面，含进度条和状态文本（SortOrder = LAYER_LOADING = 600）
  - `PrivacyDialog` —— 隐私授权弹窗（SortOrder = LAYER_LOADING + 100 = 700，显示在加载层上方）
  - `ConfirmDialog` —— 通用确认弹窗，标题/内容/按钮可配置（SortOrder = LAYER_LOADING + 100 = 700）
  - `MainMenuPanel` —— 主菜单占位面板
  - `GlobalSpinner` —— 全屏等待遮罩
- **UIBase.IsFullScreen** 虚属性（默认 `true`）：全屏面板使用 `MakeFullScreen()`；非全屏面板（对话框）使用 `Center()` 并设置 center/middle 关系
- **UIDialogBase.IsFullScreen** 重写为 `false`：对话框保持原始尺寸并居中，不再拉伸为全屏
- **UIConstants 层级常量**：LAYER_BACKGROUND(0)、LAYER_NORMAL(100)、LAYER_POPUP(200)、LAYER_DIALOG(300)、LAYER_TOAST(400)、LAYER_GUIDE(500)、LAYER_LOADING(600)
- **FairyGUI UI 包**（UIProject/ 白模原型）
  - Common：LoadingPanel、PrivacyDialog、ConfirmDialog、GlobalSpinner、CommonButton、CommonProgressBar
  - MainMenu：MainMenuPanel、MenuIconButton
- **fairygui-tools Skill**（`.codebuddy/skills/fairygui-tools/`）：用于 FairyGUI 工作流的 AI Skill，支持示意图生成、XML 生成和结构分析，包含图形白模规则、组件闭环原则与校验脚本

### 修复
- **PrivacyDialog 被 LoadingPanel 遮挡不可见**：Dialog 的 SortOrder 原为 300（LAYER_DIALOG），而 LoadingPanel 为 600（LAYER_LOADING），导致弹窗被遮挡。已将启动阶段弹窗 SortOrder 重写为 LAYER_LOADING + 100（700）
- **对话框被拉伸成全屏**：`UIBase.CreateAndShow()` 会对所有面板调用 `MakeFullScreen()`，包含 600×500 对话框。已新增 `IsFullScreen` 虚属性并在 `UIDialogBase` 中重写

### 变更
- **GameBootstrapper**：系统初始化后、`LoadInitialScene()` 前可选执行 `IStartupFlow.RunAsync()`；将 `OperationCanceledException` 视为非致命（例如用户拒绝隐私授权）
- **UIPackageLoader**：修正 `YooAssetBasePath` 为 `Assets/_Game/FairyGUI_Export/`，并将路径模式从 `{base}{pkg}/{pkg}_fui.bytes` 修正为 `{base}{pkg}_fui.bytes`

### 移除
- **ConfigManager 的 Resources 兜底路径**：移除 `Resources/ConfigData/` 拷贝步骤和同步 `Initialize()` 中的 Resources 路径。配置加载现统一通过 YooAsset

## [0.4.0] - 2026-04-06

### 新增
- **Luban v4.6.0**：从源码编译到 `Tools/Luban/`，替换旧版 Luban CLI
- **双格式配置系统**：运行时二进制 + 编辑器预览 JSON
  - 运行时通过 YooAsset（主路径）或 Resources（兜底）加载 `.bytes`
  - `Editor/ConfigPreview/` 下的 JSON 预览文件不参与构建，避免明文进入发布包
- **TablesExtension.cs**：手写 `partial class Tables`，提供 `GetTableNames()` 用于预加载
- **luban.conf（v4.6.0）**：使用新式配置，支持 `groups`、`schemaFiles` 目录扫描、`topModule: "cfg"`
- **tables.xml**：合并 bean 与 table 定义（替代拆分的 `item.xml` / `globalconst.xml` / `__tables__.xml`）
- **luban-config Skill**（`.codebuddy/skills/luban-config/`）：项目级 Skill，包含 SOP、格式参考和自动化脚本
  - `scripts/create_xlsx.py`：自动创建符合 Luban 规范的 xlsx 数据文件
  - `scripts/update_tables_extension.py`：根据 `tables.xml` 自动同步 `TablesExtension.cs`

### 变更
- **ConfigManager**：由 JSON 文本加载重写为二进制 ByteBuf 加载
  - `InitializeAsync()` 异步预加载全部 `.bytes`，随后同步构造 `Tables`
  - 同步 `Initialize()` 兜底路径也改为从 Resources 读取 `.bytes`
  - `IntegrityVerifier` 签名从 `Func<string, string, bool>` 调整为 `Func<string, byte[], bool>`
- **gen_config.bat/sh**：按 Luban v4.6.0 语法重写为三步流程
  1. `cs-bin` + `bin` → 生成代码 + `_Game/ConfigData/*.bytes`
  2. `json` → `Editor/ConfigPreview/*.json`
  3. 复制 `.bytes` 到 `Resources/ConfigData/`（兜底）—— **已在 v0.3.0 移除**
- **生成代码**：由 `cs-simple-json`（JSONNode）切换到 `cs-bin`（ByteBuf）
- **Luban 数据源**：由 JSON（`*@filename.json`）切换为 **xlsx**，便于策划使用 Excel 编辑

### 移除
- 旧版 Luban v2/v3 CLI 依赖及 `--gen_types` 命令语法
- ConfigManager 中的 JSON 运行时加载（由二进制替代）
- `_Game/ConfigData/` 下明文 JSON（现仅保留 `.bytes`）
- 旧版 Luban 定义文件：`__root__.xml`、`__tables__.xml`、`globalconst.xml`、`item.xml`（已合并为 `tables.xml`）

## [0.3.0] - 2026-04-05

### 新增
- **Luban 配置系统**：完整接入 Luban 生成配置表与运行时加载
  - `GlobalConst` 表（key/stringValue/intValue），含 HelloWorld 测试数据
  - 生成 `TbItem` / `TbGlobalConst` 表类，路径：`_Framework/DataSystem/Scripts/Config/Generated/`
  - `Tables.cs` 提供异步/同步工厂，并对 loader 返回值做空安全检查
  - `_Game/ConfigData/` 下 JSON 数据文件（YooAsset）
  - `DataTables/Defs/` 下 Luban 表定义，`DataTables/Datas/` 下数据源
- **luban_unity 包**：在 `manifest.json` 增加 `com.code-philosophy.luban`（Git URL）
- **Luban.Runtime asmdef 引用**：加入 `MiniGameFramework.Runtime.asmdef`
- **配置验证**：`GameBootstrapper` 启动时打印 GlobalConst 数据（`#if UNITY_EDITOR || DEVELOPMENT_BUILD`）

### 修复
- **AssetService**：在 YooAsset 初始化后增加 `RequestPackageVersionAsync` + `UpdatePackageManifestAsync`，修复所有资源加载场景下 `ActiveManifest == null` 崩溃
- **ConfigManager**：对 YooAsset `LoadAssetAsync` 增加 try-catch，YooAsset 抛错时可优雅降级到 `Resources.Load`
- **ConfigManager**：`ResetStatics` 现在会清理 `_tables`（提升 domain reload 安全性）
- **ConfigManager**：`ReloadAsync`/`Reload` 在重建前先置空 `_tables`

### 变更
- **ConfigManager**：启用真实 Luban 集成——将 TODO 桩代码替换为 `cfg.Tables.CreateAsync` / `cfg.Tables.Create`
- **ConfigManager**：`YooAssetConfigPath` 从 `Assets/ConfigData/` 调整为 `Assets/_Game/ConfigData/`
- **gen_config 脚本**：数据输出路径改为 `_Game/ConfigData/`
- **CONVENTIONS.md**：修正 ConfigManager 路径引用
- **Luban README**：修正输出数据路径
- **.gitignore**：新增 `UnityProj/.vs/`

## [0.2.2] - 2026-04-05

### 修复
- **YooAsset**：配置 `AssetBundleCollectorSetting.asset` 的 `DefaultPackage` + `GameAssets` 收集组，解决编辑器模拟模式下 `Not found package : DefaultPackage` 报错
- **GameBootstrapper**：当已在目标场景时跳过重复加载，修复重复实例警告（Boot → Boot 循环加载）
- **GameBootstrapper**：增加 `_isPrimaryInstance` 标记，避免重复实例在 `OnDestroy` 时重置 `_hasBooted`
- **ArchitectureValidator**：移除重复 `[MenuItem]` 特性（统一由 `MenuItems.cs` 注册）
- **Analytics SDK**：移除 `com.unity.analytics` 与 `com.unity.modules.unityanalytics`，消除 `No cloud project ID` 报错

### 变更
- **manifest.json**：移除 14 个未使用 Unity 包（Ads、Analytics、Purchasing、XR、VR、Cloth、Terrain、Vehicles、Video、Wind、Umbra），减少构建体积与编译时间
- **.gitignore**：增加 `.vsconfig`、`Bundles/`、`ResolvedPackageCache`、`FairyGUI.meta` 忽略规则

## [0.2.1] - 2026-04-05

### 修复
- **AssetService**：`UnloadUnusedAssetsAsync` / `ForceUnloadAllAssetsAsync` 增加初始化守卫，未初始化时输出 warning 而非静默返回 null（避免下游 await 结果时 NRE）
- **setup_fairygui.bat/.sh**：删除已存在且非 junction/symlink 的 `Assets/FairyGUI` 目录前增加用户确认，避免误删数据
- **ConfigManager.InitializeAsync**：移除多余 `async` + `await Task.CompletedTask`，改为直接返回 `Task.CompletedTask`，避免 WebGL 分配异步状态机
- **UIDialogBase**：将 `MakeFullScreen()` + 写死 `DrawRect` 尺寸改为 `AddRelation(GRoot.inst, RelationType.Size)`，确保横竖屏/分辨率变化时遮罩正确自适应
- **TextureImportEnforcer**：移除 Android/iPhone 平台冗余覆盖配置——目标平台仅为 WebGL（微信小游戏），简化 `SetPlatformCompression`
- **AssetAuditWindow**：新增“纹理缺少 WebGL 平台覆盖”审计项（此前仅检查未压缩 RGBA32）
- **BuildPipeline → MiniGameBuildPipeline**：重命名类，避免与 `UnityEditor.BuildPipeline` 歧义

## [0.2.0] - 2026-04-05

### 修复
- **UIManager.CloseAllPanels**：采用先快照后清空策略，避免 `panel.Close()` 修改字典时触发迭代器失效 `InvalidOperationException`
- **ClickGameManager / HighScoreSaver**：改用 `GameBootstrapper.SaveSystem`，不再重复创建 `PlayerPrefsSaveSystem` 实例（修复 `FlushIfDirty` 被绕过）
- **TimerService**：`_nextId` 溢出回绕保护（从 `int.MaxValue` 回绕到 1，跳过无效 0）
- **AudioImportEnforcer**：静态 `HashSet` 守卫防止 `OnPostprocessAudio` 递归重导入死循环
- **WeChatBridgeStub**：将每次调用创建临时 `GameObject` + 私有 `CoroutineRunner` 改为统一使用框架 `CoroutineRunner.Run()`（减少 GC 浪费并避免类名遮蔽）
- **ScoreDisplay**：`UnityEngine.Debug.Log` 改为 `GameLog.Log`（符合项目规范，发布版可剥离）
- **UIDialogBase**：修复 `GGraph.DrawRect` 缺少 width/height/lineColor 参数（CS7036）
- **AssetService**：`UnloadUnusedAssets` → `UnloadUnusedAssetsAsync`，`ForceUnloadAllAssets` → `UnloadAllAssetsAsync`（适配 YooAsset 2.3.18 API）
- **AssetService**：`OnDestroy` 正确重写 `Singleton<T>.OnDestroy()`（CS0114）
- **ConfigManager**：用 `await Task.CompletedTask` 占位以消除 CS1998 警告
- **AssetImportEnforcer/AssetAuditWindow**：将不存在的 `AudioImporterSampleSettings.overridden` 替换为 `AudioImporter.ContainsSampleSettingsOverride()`（CS1061）
- **BuildPipeline**：增加 `using UnityEditor.Build` 以使用 `Il2CppCodeGeneration` 枚举，并用 `#if UNITY_2022_3_OR_NEWER` 包裹（CS0103）

### 变更
- **Docs/CONVENTIONS.md**：从约 148 行扩展到约 460 行，补充完整 Agent 编码规范，包括：
  - 日志、错误处理、async/await、GC 优化、WebGL 约束
  - 安全（输入校验、HTTPS、HMAC、PII 保护、条件编译）
  - 框架系统使用（SaveSystem、事件、计时器、UI、资源、微信桥接）
  - 模块依赖图（L0–L6）、SO 设计模式速查
  - 集合迭代安全、Agent 提交前检查清单（12 项）

## [0.1.0] - 2026-04-05

### 新增
- 初始项目骨架
- 含 MODULE_README 占位的框架模块结构
- 核心架构：ScriptableObject 驱动的事件系统、数据变量、运行时集合
- FairyGUI 集成脚手架
- Audio、ObjectPool、FSM、Timer 系统
- 微信 Bridge 接口层
- 调试工具
- 编辑器扩展（PropertyDrawers、菜单项、架构校验器）
- Luban 配置表集成
- 示例游戏脚手架
- 项目文档套件
