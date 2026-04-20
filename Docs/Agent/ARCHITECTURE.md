# 架构设计文档

## 架构总览

MiniGameTemplate 采用 **ScriptableObject 驱动的组件化架构**：

```
┌─────────────────────────────────────────────────────┐
│                    Game Layer                        │
│  _Example/ 或 _Game/ 中的具体游戏逻辑               │
│  （MonoBehaviour 组件，每个 < 150 行）                │
└───────────────┬───────────────┬─────────────────────┘
                │ 引用 SO 资产   │ 监听 SO 事件
┌───────────────┴───────────────┴─────────────────────┐
│              ScriptableObject 资产层                  │
│  Variables (FloatVar, IntVar...)                     │
│  Events (GameEvent, IntGameEvent...)                 │
│  RuntimeSets (TransformRuntimeSet...)                │
│  Config (SceneDefinition, GameConfig, PoolDef...)    │
│  Audio (AudioClipSO, AudioLibrary)                   │
│  FSM (State, StateTransition)                        │
└───────────────┬─────────────────────────────────────┘
                │ 驱动
┌───────────────┴─────────────────────────────────────┐
│              Framework Services                      │
│  UIManager / AudioManager / SceneLoader /            │
│  PoolManager / TimerService / StateMachine           │
│  （Singleton，仅框架内部使用）                         │
└─────────────────────────────────────────────────────┘
```

## 项目目录分层

```
MiniGameTemplate/               ← Git 仓库根
├── Docs/                       ← 本文档所在目录（Agent/人类阅读）
├── UIProject/                  ← FairyGUI 编辑器工程
├── .codebuddy/skills/          ← AI Agent Skills（luban-config, fairygui-tools: XML 白模 + C# 代码架构）
└── UnityProj/                  ← Unity 工程（用 Unity 打开此目录）
    ├── Assets/FairyGUI/        # Junction → ThirdParty/FairyGUI-unity/Assets/
    ├── Assets/Spine/           # Optional Junction → ThirdParty/spine-runtimes/spine-unity/Assets/Spine
    ├── Assets/SpineCSharp/     # Optional Junction → ThirdParty/spine-runtimes/spine-csharp/src
    ├── Assets/_Framework/      # 框架代码
    ├── Assets/_Game/           # 游戏业务代码
    ├── Assets/_Example/        # 示例代码（ClickGame / DanmakuDemo）
    ├── DataTables/             # Luban 配置表

    ├── ThirdParty/             # FairyGUI + Spine 子模块，YooAsset 源码
    └── Tools/                  # 构建脚本
```

## 数据流

### 1. 游戏数据流（Variables）
```
[数据源]               [SO Variable]           [消费者]
ScoreAdder.cs  ─写→  PlayerScore.asset  ─事件→  ScoreDisplay.cs
LevelManager.cs ─写→  CurrentLevel.asset ─事件→  LevelUI.cs
```

### 2. 事件流（Events）
```
[发送方]                [SO Event]              [接收方]
GameOverTrigger.cs ─Raise→ OnGameOver.asset ─→ GameOverPanel.cs
                                            ─→ AudioManager (停BGM)
                                            ─→ ScoreSubmitter (提交排行榜)
```

### 3. 启动流程
```
Boot.unity 加载
  └→ GameBootstrapper.Awake()
      ├→ AssetService.InitializeAsync()   // 资源管理（YooAsset）
      ├→ ConfigManager.InitializeAsync()   // 配置表 (Luban) — 仅预加载 bytes，不反序列化
      ├→ TimerService (Singleton)         // 计时器
      ├→ AudioManager (Singleton)         // 音频
      ├→ UIManager (Singleton)            // UI
      ├→ PoolManager (Singleton)          // 对象池
      ├→ IStartupFlow.RunAsync()          // 游戏层启动编排（可选）
      │    └→ GameStartupFlow（Game 层实现）
      │         ├→ Phase 1: 打开 LoadingPanel，模拟加载进度
      │         ├→ Phase 2: 检查隐私授权（PrivacyDialog / ConfirmDialog）
      │         │    └→ 用户拒绝 → OperationCanceledException（非致命）
      │         └→ Phase 3: 淡出 Loading，打开 MainMenuPanel
      └→ LoadInitialScene()
           ├→ 如果 InitialScene == 当前场景 → 跳过（避免循环加载）
           └→ 否则 → SceneLoader.LoadScene(initialScene)
```

> **IStartupFlow 接口**：定义在 `_Framework/GameLifecycle/`，提供 `Task RunAsync(GameConfig)` 方法。Game 层通过 `GameStartupFlow` 实现自己的启动逻辑（如 Loading 界面、隐私授权）。`GameBootstrapper` 在系统初始化完成后、加载目标场景前调用。如果 `IStartupFlow` 抛出 `OperationCanceledException`，`GameBootstrapper` 将其视为非致命错误（如用户拒绝授权导致退出）。

### 4. 配置表数据流（Luban 双格式 + Lazy Deserialization）
```
DataTables/Datas/*.xlsx       ← 源数据（策划用 Excel 编辑）
        │
    gen_config.bat/sh          ← Luban v4.6.0 生成
        │
        ├→ cs-bin 代码 → Generated/*.cs      （ByteBuf 反序列化 + lazy property）
        ├→ bin 数据   → _Game/ConfigData/*.bytes    （YooAsset 运行时加载）
        └→ json 数据  → Editor/ConfigPreview/*.json  （编辑器人工查看，不打包）

运行时加载链（Lazy Deserialization）：
  ConfigManager.InitializeAsync()
    └→ Phase 1: YooAsset 异步预加载全部 .bytes → byte[] 缓存（仅 I/O，无反序列化）
    └→ Phase 2: 创建 Tables 实例，传入 lazy loader（此时不反序列化任何表）

  业务代码首次访问 ConfigManager.Tables.TbXxx
    └→ Tables lazy property getter 触发：
        ├→ 从 bytes 缓存取出 byte[] → new ByteBuf → 构造 TbXxx
        ├→ TbXxx.ResolveRef(tables)
        └→ 从缓存移除 byte[]（释放内存）
```

> **对业务代码的影响**：无。`ConfigManager.Tables.TbItem.Get(id)` 用法完全不变。
> `ConfigManager.IsTableLoaded(fileName)` 可查询某表是否已反序列化。

### 5. UI 工作流
```text
FairyGUI 编辑器（UIProject/）
  └→ 导出到 UnityProj/Assets/_Game/FairyGUI_Export/
      └→ UIPackageLoader 加载 FairyGUI 包
          └→ UIManager 管理面板生命周期

当前示例包：
  - MainMenu    → 主菜单双入口（ClickGame / DanmakuDemo）
  - ClickGame   → 点击计数器示例 UI

当前示例场景流转：
  Boot.unity
    └→ MainMenuPanel
         ├→ LoadScene("ClickGame")
         │    └→ ClickGameSceneEntry 打开 ClickCounterPanel
         └→ LoadScene("DanmakuDemo")
              └→ DanmakuDemoController 驱动弹幕示例

示例返回主菜单：
  ClickGame / DanmakuDemo
    └→ ExampleSceneNavigator.ReturnToMainMenu()
         └→ 重载 Boot.unity 并重新打开 MainMenuPanel


Spine（可选）接入：
  setup_spine.* 建立源码链接（Assets/Spine + Assets/SpineCSharp）
    └→ Unity 菜单启用 FAIRYGUI_SPINE（并保留 ENABLE_SPINE）
        └→ FairyGUI 的 GLoader3D 可直接渲染 Spine PackageItem
```

### 6. UI 层级系统（SortingOrder）

| 层级常量 | 值 | 用途 |
|---------|-----|------|
| `LAYER_BACKGROUND` | 0 | 背景面板 |
| `LAYER_NORMAL` | 100 | 普通面板（游戏 HUD 等） |
| `LAYER_POPUP` | 200 | 弹出面板 |
| `LAYER_DIALOG` | 300 | 对话框（实现 IModalDialog） |
| `LAYER_TOAST` | 400 | Toast 提示 |
| `LAYER_GUIDE` | 500 | 新手引导 |
| `LAYER_LOADING` | 600 | 加载界面 |

> **注意**：启动阶段弹出的对话框（PrivacyDialog、ConfirmDialog）需要 `PanelSortOrder` 返回 `LAYER_LOADING + 100`（700），否则会被 LoadingPanel 遮挡。
>
> 面板通过实现 `IUIPanel.IsFullScreen` 控制布局：`true` 使用 `MakeFullScreen()`；`false`（对话框）保持原始尺寸并居中显示。实现 `IModalDialog` 的面板会自动添加半透明遮罩。

## 模块依赖关系

**规则：只能向下依赖，不能向上。**

| 层级 | 模块 | 依赖 |
|------|------|------|
| L0 (零依赖) | Utils | 无 |
| L1 (基础) | EventSystem, DataSystem, Timer, AssetSystem | Utils |
| L1-R (渲染基础) | **Rendering**（RenderBatchManager / RuntimeAtlasSystem / RenderVertex） | 无（零业务依赖） |
| L2 (服务) | UISystem, AudioSystem, ObjectPool | L1 + Utils |
| L3 (编排) | FSM, WeChatBridge | L1 |
| L4 (入口) | GameLifecycle | L1 + L2 + L3 |
| L5 (调试) | DebugTools | L1 |
| L-VFX | **VFXSystem**（SpriteSheetVFXSystem / VFXBatchRenderer） | L1-R (Rendering) |
| L-Danmaku | DanmakuSystem | L0 + L1 (EventSystem, ObjectPool, AudioSystem) + L1-R (Rendering) + L-VFX (通过 IDanmakuVFXRuntime 桥接) |
| Game | _Example, _Game | 所有框架模块 |

## 关键设计决策

### 为什么用 SO 事件而不是 C# event / UnityEvent？
- SO 事件是**资产文件**，可以在 Inspector 中拖拽配置
- 发送方和接收方完全解耦——不需要引用对方，只引用同一个 SO 资产
- 调试友好：在 Project 面板中可以看到所有事件资产

### 为什么 Singleton 仅限框架内部？
- Singleton 本质上是全局状态，过度使用会导致紧耦合
- 框架的 Manager 类（UIManager, AudioManager 等）确实需要全局唯一入口
- 游戏逻辑应该通过 SO 引用通信，而非 `SomeManager.Instance.DoSomething()`

### 为什么目录按功能域而非技术分类？
- Agent 操作一个功能时，只需读一个目录的内容
- 减少 Agent 的上下文消耗（不需要在 Scripts/、Prefabs/、SO/ 之间跳转）
- 每个模块的 MODULE_README.md 就是该模块的完整说明书

### 为什么 Unity 工程和 UI 工程分离？
- FairyGUI 编辑器工程（UIProject/）是 UI 设计师的工作区
- Unity 工程（UnityProj/）是程序的工作区
- FairyGUI 导出的资源直接输出到 UnityProj，实现单向数据流
- 两个工程在同一个 Git 仓库中，保证版本一致性

## 统一渲染管线（Phase R0 ~ R4 落地后）

### 架构总览

```
┌───────────────────────────────────────────────────────────────┐
│                     统一渲染管线（当前架构）                      │
│                                                               │
│   BulletRenderer  LaserRenderer  VFXBatchRenderer  DmgNumber  │
│        │              │              │                │       │
│        └──────┬───────┴──────┬───────┘                │       │
│               ▼              ▼                        ▼       │
│   RuntimeAtlasSystem（动态图集）    独立贴图（Laser）    Atlas   │
│        │                              │                │      │
│        └──────────────┬───────────────┴────────────────┘      │
│                       ▼                                       │
│            RenderBatchManager（统一分桶 + 提交）                │
│            BucketRegistration（多模板材质 + 注册时排序）         │
│                       │                                       │
│                       ▼                                       │
│            Graphics.DrawMesh（material.renderQueue 控制层序）   │
│                                                               │
│   TrailPool ────── 独立 Mesh（方案 A）                         │
│        │                                                      │
│        └─→ Graphics.DrawMesh（renderQueue = 3090）             │
│                                                               │
│   RenderBatchManagerRuntimeStats（统一 DC 统计，含 Trail）      │
└───────────────────────────────────────────────────────────────┘
```

### 渲染提交顺序（renderQueue）

| 子系统 | renderQueue | 说明 |
|--------|-------------|------|
| Trail | 3090 | 在弹丸后方 |
| Bullet | 3100 | 主体 |
| Laser | 3120 | — |
| VFX | 3200 | 特效在弹丸前方 |
| DamageNumber | 3300 | 最前方（飘字不被遮挡） |

### 每帧管线调度

```
DanmakuSystem.RunUpdatePipeline()
  1. SpawnerDriver.Tick
  2. PatternScheduler.Tick
  3. BulletMover.UpdateAll（MotionRegistry 策略委托）
  4. LaserUpdater.UpdateAll
  5. SprayUpdater.UpdateAll
  6. IDanmakuVFXRuntime.TickVFX(dt)       ← R4.0 新增
  7. CollisionSolver.SolveAll
  8. PlayerHit + 飘字
  9. EffectsBridge.OnCollisionEventsReady
 10. CollisionEventBuffer.Reset

DanmakuSystem.RunLateUpdatePipeline()
  RenderBatchManagerRuntimeStats.BeginFrame()
  ├── TrailPool.Render()                  ← 独立 Mesh + Graphics.DrawMesh（renderQueue=3090）
  ├── BulletRenderer.Rebuild + UploadAndDrawAll    → 独立 RBM（RuntimeAtlas 纹理）
  ├── LaserRenderer.Rebuild + UploadAndDrawAll     → 独立 RBM（独立贴图）
  ├── LaserWarningRenderer.Rebuild + UploadAndDrawAll → 独立 RBM（独立贴图）
  ├── IDanmakuVFXRuntime.RenderVFX()      ← R4.0 收编（VFXBatchRenderer → 独立 RBM）
  └── DamageNumberSystem.Rebuild(dt) + UploadAndDrawAll → 独立 RBM（RuntimeAtlas DamageText）
  RenderBatchManagerRuntimeStats.EndFrame()

  注意：每个 Renderer 内部持有独立的 RBM 实例，各自在 Rebuild 末尾调用
  UploadAndDrawAll()。渲染层序由 material.renderQueue 值控制（GPU 级排序），
  不依赖代码调用顺序。
```

### 关键设计决策摘要

| 决策 | 选型 | 理由 |
|------|------|------|
| 图集算法 | Shelf Packing (Best-Fit) | 零 GC、O(N) 搜索、支持混合尺寸 |
| 纹理 Blit | CommandBuffer + SetRenderTarget | WebGL 2.0 兼容，不依赖 Graphics.CopyTexture |
| 激光入 Atlas | ❌ 不入 | UV.y 是 world-space 累积长度，Atlas 子区域会破坏 wrap 采样 |
| TrailPool 迁移 | 方案 A（独立 Mesh + 接入统计） | TriangleStrip 拓扑与 RBM 的 Quad 拓扑不匹配 |
| DC 排序 | material.renderQueue | Graphics.DrawMesh 跨 RBM 实例的层级控制必须靠 renderQueue，不能靠调用顺序 |
| VFX 编排 | DanmakuSystem 管线统一驱动 | SpriteSheetVFXSystem 退化为纯 API，TickVFX/RenderVFX 由管线调用 |

> 详细设计文档：`Docs/Agent/RUNTIME_ATLAS_SYSTEM_TDD.md`（v2.10.1）

## DanmakuSystem 架构详解

### 子系统组成

```
DanmakuSystem (MonoBehaviour, DontDestroyOnLoad)
├── 数据容器
│   ├── BulletWorld (SoA 2048)
│   ├── LaserPool (16, 含 Segments[] 折射段)
│   ├── SprayPool (8)
│   ├── ObstaclePool (64)
│   └── AttachSourceRegistry (24 挂载源, 引用计数)
├── 更新逻辑
│   ├── BulletMover / BulletSpawner
│   ├── LaserUpdater (Charging→Firing→Fading→回收)
│   ├── SprayUpdater (Active→回收)
│   ├── LaserSegmentSolver (折射段解算)
│   └── CollisionSolver (7 阶段碰撞)
├── 调度：PatternScheduler (64 槽)
├── 渲染：BulletRenderer / LaserRenderer / LaserWarningRenderer / DamageNumberSystem / TrailPool / VFXBatchRenderer（via IDanmakuVFXRuntime）
└── 配置：12 种 SO
```

### 7 阶段碰撞

| Phase | 内容 | 碰撞响应 |
|-------|------|----------|
| 1 | 弹丸 vs 目标 | Die/ReduceHP/Pierce/BounceBack/Reflect |
| 2 | 弹丸 vs 障碍物 | Die/RecycleOnDistance |
| 3 | 弹丸 vs 屏幕边缘 | 回收 |
| 4 | 激光 vs 玩家 | DamagePerTick |
| 5 | 喷雾 vs 玩家 | DamagePerTick |
| 6 | 激光 vs 障碍物 | Block/Pierce/BlockAndDamage/PierceAndDamage + 屏幕边缘 Clip/Reflect |
| 7 | 喷雾 vs 屏幕边缘 | 回收 |

### Attached 模式数据流

```
FireLaser/FireSpray(Attached 重载)
  └→ AttachSourceRegistry.Register(Transform, offset, angle) → attachId
       └→ 每帧 LaserUpdater/SprayUpdater:
            ├→ GetWorldPosition(attachId) → 更新 origin
            └→ GetWorldAngle(attachId) → 更新 angle/direction
       └→ FreeLaser/FreeSpray 时 Release(attachId) → refCount-- → 归还空闲栈
```

## [AGENT] Unity MCP 集成（AI Agent ↔ Unity Editor）

### 概述

本项目集成了 [AnkleBreaker Unity MCP](https://github.com/AnkleBreaker-Studio/unity-mcp-plugin)，允许 AI Agent 通过 **Model Context Protocol (MCP)** 直接操作运行中的 Unity Editor。这意味着 Agent 可以在不离开对话的情况下：

- 检查编译错误和警告
- 读取 Console 日志
- 查看/创建/删除 GameObject 和场景
- 读取和修改组件属性
- 创建和更新 C# 脚本
- 触发构建
- 截取 Scene View / Game View 截图做视觉验证
- 执行任意 C# 代码片段
- 以及 288 个工具覆盖 30+ 类别

### 架构

```
AI Agent (WorkBuddy)
  │  MCP 协议（stdio）
  ▼
anklebreaker-unity-mcp (Node.js MCP Server)
  │  HTTP (localhost:7891)
  ▼
com.anklebreaker.unity-mcp (Unity Editor Plugin)
  │  编辑器 API 调用
  ▼
Unity Editor (2021.3.17f1)
```

### 配置

**Unity 侧**（已就绪，无需操作）：
- 插件包：`Packages/com.anklebreaker.unity-mcp/`（本地冻结版本，含 2021.3 兼容补丁）
- 端口：**建议使用自动端口模式**（插件默认在 `7890-7899` 范围内自动选可用端口）；仅在明确需要固定端口时才手动锁定，例如 `7891`
- 自动启动：Unity Editor 打开即运行

**WorkBuddy 侧**（已配置在 `~/.workbuddy/mcp.json`）：
```json
{
  "mcpServers": {
    "unity": {
      "command": "C:\\Program Files\\nodejs\\npx.cmd",
      "args": ["-y", "anklebreaker-unity-mcp@latest"],
      "env": {
        "UNITY_BRIDGE_PORT": "7891"
      }
    }
  }
}
```

> 说明：`UNITY_BRIDGE_PORT` 只是 Node MCP Server 的默认探测入口，不代表 Unity 实际运行端口被固定为 7891。Unity 侧启用自动端口后，Agent 必须先调用 `unity_list_instances` 扫描 `7890-7899`，拿到真实端口后再 `unity_select_instance` 并在后续工具里显式透传该端口。

**前置依赖**：
- Node.js 18+（当前：v22.22.2，路径：`C:\Program Files\nodejs\`）
- Unity Editor 需要处于打开状态且 MCP Bridge 正在运行

### [AGENT] 使用指南

#### 编译验证（最常用）

代码修改后，**优先使用 MCP 工具验证编译**，不要让用户手动检查：

```
工具名: unity_get_compilation_errors
参数:   { "severity": "all", "port": <当前实例端口> }
```

- 返回 `count: 0` 表示编译通过
- 返回错误时包含文件路径、行号、错误消息，可直接定位修复
- 此工具基于 `CompilationPipeline`，独立于 Console 日志缓冲区

#### 连接健康检查

```
工具名: unity_editor_ping
参数:   { "port": <当前实例端口> }
```

返回 Unity 版本、项目名、项目路径、平台信息。

#### 常用工具速查

| 场景 | MCP 工具 | 说明 |
|------|---------|------|
| 编译检查 | `unity_get_compilation_errors` | 获取编译错误/警告 |
| Console 日志 | `unity_console_log` | 获取运行时日志 |
| 场景信息 | `unity_scene_info` | 当前场景名、路径、脏状态 |
| 场景层级 | `unity_scene_hierarchy` | 完整 GameObject 树 |
| 读 C# 脚本 | `unity_script_read` | 从 Unity 项目读取脚本内容 |
| 写 C# 脚本 | `unity_script_create` / `unity_script_update` | 创建或更新脚本 |
| 执行 C# 代码 | `unity_execute_code` | 在编辑器上下文执行任意代码 |
| 截图验证 | `unity_graphics_scene_capture` / `unity_graphics_game_capture` | 截取场景/游戏视图 |
| 项目信息 | `unity_project_info` | 包列表、渲染管线、构建设置 |
| Play Mode | `unity_play_mode` | 进入/暂停/停止播放 |

> **⚠️ 重要**：所有 MCP 工具调用时请携带当前 Unity 实例的实际端口参数；若启用自动端口模式，先用 `unity_list_instances` 发现端口，再把该端口透传给后续工具。只有在 Unity 侧明确锁定手动端口时，才应把 `7891` 当作固定值使用。

#### 备用方案

如果 MCP Server 不可用（如 Node.js 未安装、进程未启动），可回退到 HTTP 直连：

```powershell
curl.exe -s --max-time 10 http://127.0.0.1:7891/api/compilation/errors
curl.exe -s --max-time 5  http://127.0.0.1:7891/api/ping
```

### 注意事项

1. **本地冻结**：Unity 插件是本地冻结版本（非 git URL 引用），含 Unity 2021.3 兼容补丁。详见 `Packages/com.anklebreaker.unity-mcp/README_MINIGAME_PATCH.md`
2. **仅本地访问**：Bridge 绑定 `127.0.0.1`，不暴露到网络
3. **Undo 支持**：所有编辑操作支持 Unity Undo 系统
4. **多 Agent 安全**：多个 Agent 同时连接时，请求会排队执行
