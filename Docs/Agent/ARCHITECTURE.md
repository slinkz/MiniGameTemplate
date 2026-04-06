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
├── .codebuddy/skills/          ← AI Agent Skills（luban-config 等）
└── UnityProj/                  ← Unity 工程（用 Unity 打开此目录）
    ├── Assets/FairyGUI/        # Junction → ThirdParty/FairyGUI-unity/Assets/
    ├── Assets/_Framework/      # 框架代码
    ├── Assets/_Game/           # 游戏业务代码
    ├── DataTables/             # Luban 配置表
    ├── ThirdParty/             # FairyGUI submodule + YooAsset 源码
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
      ├→ ConfigManager.InitializeAsync()   // 配置表 (Luban)
      ├→ TimerService (Singleton)         // 计时器
      ├→ AudioManager (Singleton)         // 音频
      ├→ UIManager (Singleton)            // UI
      ├→ PoolManager (Singleton)          // 对象池
      └→ LoadInitialScene()
           ├→ 如果 InitialScene == 当前场景 → 跳过（避免循环加载）
           └→ 否则 → SceneLoader.LoadScene(initialScene)
```

### 4. 配置表数据流（Luban 双格式）
```
DataTables/Datas/*.xlsx       ← 源数据（策划用 Excel 编辑）
        │
    gen_config.bat/sh          ← Luban v4.6.0 生成
        │
        ├→ cs-bin 代码 → Generated/*.cs      （ByteBuf 反序列化）
        ├→ bin 数据   → _Game/ConfigData/*.bytes    （YooAsset 运行时加载）
        └→ json 数据  → Editor/ConfigPreview/*.json  （编辑器人工查看，不打包）

运行时加载链：
  ConfigManager.InitializeAsync()
    └→ YooAsset: _Game/ConfigData/{name}.bytes → new ByteBuf(bytes) → Tables 构造函数
```

### 5. UI 工作流
```
FairyGUI 编辑器（UIProject/）
  └→ 导出到 UnityProj/Assets/_Game/FairyGUI_Export/
      └→ UIPackageLoader 加载 FairyGUI 包
          └→ UIManager 管理面板生命周期
```

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
