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
| L2 (服务) | UISystem, AudioSystem, ObjectPool | L1 + Utils |
| L3 (编排) | FSM, WeChatBridge | L1 |
| L4 (入口) | GameLifecycle | L1 + L2 + L3 |
| L5 (调试) | DebugTools | L1 |
| L-Danmaku | DanmakuSystem | L0 + L1 (EventSystem, ObjectPool, AudioSystem) |
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
├── 渲染：BulletRenderer / DamageNumberSystem / TrailPool
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
