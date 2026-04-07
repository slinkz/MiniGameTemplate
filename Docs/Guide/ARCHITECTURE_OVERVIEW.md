# 架构设计解读

> **预计阅读**：15 分钟 &nbsp;|&nbsp; **目标**：理解 MiniGameTemplate 的设计理念和架构规则

## 核心理念：ScriptableObject 驱动一切

MiniGameTemplate 的架构有一个核心原则：**组件之间不直接引用对方，而是通过 ScriptableObject 资产进行通信**。

这意味着：
- 分数显示组件不需要知道谁在计分
- 音频系统不需要知道游戏结束事件从哪来
- 每个组件只关心自己引用的 SO 资产

### 为什么这样设计？

传统 Unity 项目中，组件之间通过 `GetComponent<>()` 或单例互相引用，改一个组件可能牵动十几个文件。这个模板用 SO 把数据和事件"物化"成项目中的 `.asset` 文件：

```
传统方式:  ScoreAdder → 直接引用 → ScoreDisplay
模板方式:  ScoreAdder → 写入 PlayerScore.asset → ScoreDisplay 监听该 asset
```

**好处是**：你可以单独测试 ScoreDisplay——只需要在 Inspector 中手动修改 PlayerScore.asset 的值。

## 三层架构

```
┌─────────────────────────────────────────────────────┐
│                    游戏逻辑层                        │
│  _Example/ 或 _Game/ 中的 MonoBehaviour 组件        │
│  （每个组件职责单一，不超过 150 行）                  │
└───────────────┬───────────────┬─────────────────────┘
                │ 引用 SO 资产   │ 监听 SO 事件
┌───────────────┴───────────────┴─────────────────────┐
│              ScriptableObject 资产层                  │
│  Variables:  PlayerScore.asset, RemainingTime.asset  │
│  Events:     OnGameStart.asset, OnGameOver.asset     │
│  Config:     GameConfig.asset, AssetConfig.asset     │
│  Audio:      ClickSound.asset, BGMLibrary.asset      │
│  FSM:        State_Menu.asset, State_Playing.asset   │
└───────────────┬─────────────────────────────────────┘
                │ 驱动
┌───────────────┴─────────────────────────────────────┐
│              框架服务层                               │
│  UIManager / AudioManager / TimerService /           │
│  PoolManager / SceneLoader / AssetService            │
│  （Singleton，仅框架内部使用，游戏代码不直接访问）     │
└─────────────────────────────────────────────────────┘
```

### 各层的职责

| 层 | 住什么 | 你会改它吗 |
|----|--------|------------|
| **游戏逻辑层** | 你的游戏组件：玩家控制器、敌人 AI、关卡逻辑 | ✅ 主要在这里写代码 |
| **SO 资产层** | `.asset` 文件：变量、事件、配置、音效定义 | ✅ 在 Inspector 中创建和配置 |
| **框架服务层** | Manager 单例：UI、音频、对象池、计时器等 | ❌ 一般不需要修改 |

## 两种核心通信机制

### 1. SO 变量（数据共享）

当多个组件需要访问同一个数据时，把数据放在 SO 变量里：

```
写入方                  SO Variable                  读取方
───────────────────     ─────────────────     ───────────────────
ClickGameManager   ─写→  PlayerScore.asset  ─监听→  ScoreDisplay
                                            ─监听→  HighScoreSaver
                                            ─监听→  AudioManager (播放音效)
```

**怎么用：**
1. 右键 Project → Create → MiniGameTemplate → Variables → Int，创建一个 IntVariable
2. 在写入方的 Inspector 中拖入这个 asset
3. 在读取方的 Inspector 中也拖入同一个 asset
4. 读取方在 `OnEnable` 中订阅 `OnValueChanged` 事件

```csharp
// 写入方
[SerializeField] private IntVariable _playerScore;

void AddScore() {
    _playerScore.ApplyChange(1); // 自动通知所有监听者
}

// 读取方
[SerializeField] private IntVariable _playerScore;

void OnEnable() {
    _playerScore.OnValueChanged += UpdateDisplay;
}

void OnDisable() {
    _playerScore.OnValueChanged -= UpdateDisplay;
}

void UpdateDisplay(int newScore) {
    // 更新 UI 显示
}
```

### 2. SO 事件（消息广播）

当你需要通知"某件事发生了"而不需要传递具体数据时，使用 SO 事件：

```
发送方                  SO Event                    接收方
───────────────────     ─────────────────     ───────────────────
GameOverTrigger    ─Raise→  OnGameOver.asset  ─→  GameOverPanel (显示UI)
                                              ─→  AudioManager (停BGM)
                                              ─→  ScoreSubmitter (提交分数)
```

SO 事件有两种使用方式：

**方式 A：代码订阅**（适合程序员）
```csharp
[SerializeField] private GameEvent _onGameOver;

void OnEnable()  { _onGameOver.RegisterListener(this); }
void OnDisable() { _onGameOver.UnregisterListener(this); }
```

**方式 B：Inspector 配置**（适合策划/设计师）
1. 给 GameObject 添加 `GameEventListener` 组件
2. 在 Inspector 中拖入 OnGameOver.asset
3. 在 UnityEvent 中配置要触发的方法

## 启动流程

每次游戏启动，都从 `Boot.unity` 场景开始：

```
1. Unity 加载 Boot.unity
2. GameBootstrapper.Awake() 执行
   ├── 2a. 初始化 SaveSystem（PlayerPrefs 持久化）
   ├── 2b. 初始化 AssetService（YooAsset 资源管理）
   ├── 2c. 初始化 ConfigManager（Luban 配置表）
   ├── 2d. 初始化 TimerService（计时器）
   ├── 2e. 初始化 AudioManager（音频）
   ├── 2f. 初始化 UIManager（FairyGUI）
   └── 2g. 初始化 PoolManager（对象池）
3. 加载 GameConfig 中配置的初始场景
4. 游戏开始运行
```

> ⚠️ 这些系统的初始化有严格的顺序依赖。AssetService 必须先于其他所有系统，因为 UI、音频、配置表等都需要加载资源。

## 模块依赖图

框架的 12 个模块分为 6 个层级，**只能向下依赖，不能向上**：

```
L0 ── Utils                    ← 零依赖（Singleton、GameLog、CoroutineRunner）
       ↑
L1 ── EventSystem              ← 依赖 L0
      DataSystem
      Timer
      AssetSystem
       ↑
L2 ── UISystem                 ← 依赖 L1 + L0
      AudioSystem
      ObjectPool
       ↑
L3 ── FSM                      ← 依赖 L1
      WeChatBridge
       ↑
L4 ── GameLifecycle            ← 依赖 L1 + L2 + L3（启动入口）
       ↑
L5 ── DebugTools               ← 依赖 L1（调试工具）
───────────────────────────────
Game ── _Game/ _Example/        ← 可以引用所有框架模块
```

**实际意义**：
- `AudioSystem` 可以引用 `DataSystem`（音量用 FloatVariable）
- `FSM` 可以引用 `EventSystem`（状态切换时 Raise 事件）
- `UISystem` **不能**引用 `FSM`（它们在同级或更高层）
- `_Game/` 代码可以引用任何框架模块

如果你不小心引入了违规依赖，运行 `Tools → MiniGame Template → Validate → Architecture Check` 会告诉你。

## 第三方库引入方式

| 库 | 引入方式 | 路径 |
|----|----------|------|
| FairyGUI | Git submodule + Junction 链接 | `ThirdParty/FairyGUI-unity/` -> `Assets/FairyGUI/`（Junction） |
| Spine（可选） | Git submodule + Junction 链接 + define 开关 | `ThirdParty/spine-runtimes/` -> `Assets/Spine/` + `Assets/SpineCSharp/` |
| YooAsset | 本地源码 UPM 包 | `ThirdParty/YooAsset/`（`manifest.json` 中 `file:` 引用） |

> 💡 FairyGUI 没有 `package.json`（不是 UPM 包），所以通过目录 Junction/符号链接让 Unity 识别。首次克隆后需运行 `Tools/setup_fairygui.bat`（Windows）或 `setup_fairygui.sh`（macOS/Linux）创建链接。
>
> 💡 Spine 为可选依赖。需要 FairyGUI 显示 Spine 时，运行 `Tools/setup_spine.bat`/`setup_spine.sh`，并启用 `FAIRYGUI_SPINE`。未启用时不参与编译和加载。

## 程序集定义

项目使用 Assembly Definition 分离编译域，加快编译速度：

| 程序集 | 目录 | 引用 |
|--------|------|------|
| `MiniGameFramework.Runtime` | `Assets/_Framework/` | FairyGUI, YooAsset |
| `MiniGameFramework.Editor` | `Assets/_Framework/Editor/` | Runtime, FairyGUI, YooAsset |
| `Game.Runtime` | `Assets/_Game/` | Runtime |
| `Example` | `Assets/_Example/` | Runtime |

修改 `_Game/` 目录下的代码时，只有 `Game.Runtime` 需要重新编译，不会触发整个框架的重编译。

## 关键设计决策

### 为什么用 SO 事件而不是 C# event / UnityEvent？

| | C# event | UnityEvent | SO Event |
|---|----------|------------|----------|
| Inspector 可见 | ❌ | ✅ | ✅ |
| 跨场景持久 | ❌ | ❌ | ✅ |
| 发送方/接收方解耦 | 部分 | 部分 | **完全** |
| 非程序员可配置 | ❌ | ✅ | ✅ |
| 调试时可见 | ❌ | 不方便 | **在 Project 面板可见** |

### 为什么 Singleton 仅限框架内部？

Singleton 本质上是全局变量。框架的 Manager 类（UIManager、AudioManager 等）确实需要全局唯一入口，所以允许。但游戏逻辑如果到处用 `SomeManager.Instance.DoSomething()`，会变成一团意大利面。

**规则**：游戏代码通过 SO 引用通信，不通过 `Instance` 访问。

### 为什么目录按功能域组织？

```
✅ 按功能域（模板的做法）        ❌ 按技术类型（不推荐）
AudioSystem/                    Scripts/
├── Scripts/                    ├── AudioManager.cs
│   └── AudioManager.cs         ├── UIManager.cs
├── Presets/                    ├── PoolManager.cs
│   ├── AudioClipSO.asset       Prefabs/
│   └── AudioLibrary.asset      ├── AudioPrefab.prefab
└── MODULE_README.md             ScriptableObjects/
                                 ├── AudioClipSO.asset
```

按功能域组织的好处：要改音频系统，只需要看 `AudioSystem/` 目录。不需要在 Scripts、Prefabs、ScriptableObjects 之间跳来跳去。

## 微信小游戏适配要点

模板已经为微信小游戏做了以下预置：

| 方面 | 已处理 | 说明 |
|------|--------|------|
| 渲染管线 | ✅ | 使用 Built-in RP，不使用 URP/HDRP |
| 内存限制 | ✅ | 纹理最大 1024px（自动强制）、音效强制 Mono |
| 单线程 | ✅ | 禁止 Thread/Task.Run，所有加载走 async/await |
| 文件 I/O | ✅ | 禁止 System.IO，持久化走 PlayerPrefs |
| 资源加载 | ✅ | YooAsset 异步加载，支持微信文件系统扩展 |
| 构建配置 | ✅ | 一键构建自动设置 Gamma/压缩/Stripping 等 |
| SDK 桥接 | ✅ | IWeChatBridge 接口 + 桩实现，不锁死 SDK 版本 |

## 下一步

- 📖 **[框架模块使用手册](FRAMEWORK_MODULES.md)** — 每个模块的详细 API 和用法
- 🎮 **[示例游戏代码解读](EXAMPLE_WALKTHROUGH.md)** — 看看这些概念在实际代码中是什么样的
