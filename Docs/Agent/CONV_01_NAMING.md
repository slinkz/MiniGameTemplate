---
system: conventions
scope: naming-structure
last_verified: 2026-05-02
related_code: Assets/_Framework/**, Assets/_Game/**
---

## 命名规范

### 文件与类
| 类型 | 规则 | 示例 |
|------|------|------|
| C# 类 | PascalCase | `PlayerHealth`, `GameEvent` |
| C# 接口 | I + PascalCase | `ISaveSystem`, `IWeChatBridge` |
| MonoBehaviour | PascalCase，名称描述唯一职责 | `ScoreDisplay`（不是 `ScoreDisplayAndSFXAndAnimation`）|
| ScriptableObject | PascalCase + 后缀 SO/Variable/Event | `FloatVariable`, `AudioClipSO`, `GameEvent` |
| SO 资产文件 | PascalCase，描述用途 | `PlayerScore.asset`, `OnGameOver.asset` |
| 枚举 | PascalCase，值也是 PascalCase | `enum GameState { Menu, Playing, GameOver }` |

### 变量
| 类型 | 规则 | 示例 |
|------|------|------|
| private 字段 | _camelCase | `_playerHealth`, `_isActive` |
| [SerializeField] | _camelCase | `[SerializeField] private IntVariable _score` |
| public 属性 | PascalCase | `public float Value { get; }` |
| 局部变量 | camelCase | `var currentScore = _score.Value` |
| 常量 | UPPER_SNAKE_CASE | `const int MAX_POOL_SIZE = 100` |
| static readonly | PascalCase 或 UPPER_SNAKE_CASE | `static readonly Vector3 DefaultSpawn = Vector3.zero` |

### 命名空间
```
MiniGameTemplate.Core       // GameLifecycle
MiniGameTemplate.Events     // EventSystem
MiniGameTemplate.Data       // DataSystem
MiniGameTemplate.UI         // UISystem
MiniGameTemplate.Audio      // AudioSystem
MiniGameTemplate.Pool       // ObjectPool
MiniGameTemplate.FSM        // FSM
MiniGameTemplate.Timing     // Timer
MiniGameTemplate.Asset      // AssetSystem
MiniGameTemplate.Platform   // WeChatBridge
MiniGameTemplate.Debug      // DebugTools
MiniGameTemplate.Utils      // Utils
MiniGameTemplate.Rendering  // Rendering（RBM / RuntimeAtlasSystem / RenderVertex）
MiniGameTemplate.VFX        // VFXSystem（SpriteSheetVFXSystem / VFXBatchRenderer）
MiniGameTemplate.Danmaku    // DanmakuSystem（弹幕系统核心）
MiniGameTemplate.EditorTools       // Editor 通用工具
MiniGameTemplate.Editor.Rendering  // Editor: Atlas 工具
MiniGameTemplate.Editor.Danmaku    // Editor: 弹幕 SO 编辑器
MiniGameTemplate.Danmaku.Editor    // Editor: DanmakuSystem 编辑器
MiniGameTemplate.Game       // _Game（游戏逻辑）
```

## 代码风格

### 文件结构
```csharp
using UnityEngine;
using MiniGameTemplate.Events;
using MiniGameTemplate.Data;

namespace MiniGameTemplate.Game
{
    /// <summary>
    /// 一句话描述这个类的唯一职责。
    /// </summary>
    public class MyComponent : MonoBehaviour
    {
        #region SO References
        [SerializeField] private IntVariable _score;
        [SerializeField] private GameEvent _onScoreChanged;
        #endregion

        #region Unity Lifecycle
        private void OnEnable() { /* Subscribe */ }
        private void OnDisable() { /* Unsubscribe */ }
        #endregion

        #region Public API
        public void AddScore(int amount) { }
        #endregion

        #region Private Methods
        private void UpdateDisplay() { }
        #endregion
    }
}
```

### 行数限制
- 每个 MonoBehaviour **不超过 150 行**
- 如果超过，拆分成多个单一职责组件
- 运行 `Tools → MiniGame Template → Validate Architecture` 检查

### [AGENT] XML 文档注释
- 所有 `public` / `protected` 方法、属性、类必须带 `<summary>` 注释
- 注释用英文撰写，一句话说明 **做什么**，不是 **怎么做**
- `[SerializeField]` 字段如果用途不直观，加 `[Tooltip("...")]`

## [AGENT] FairyGUI 面板规范（强制 — Extension + IUIPanel 模式）

### 目标
利用 FairyGUI 原生 Extension 机制和代码导出，FairyGUI 编辑器导出的 `*.cs` 自动生成字段绑定和 Binder，手写业务逻辑放在 `*.Logic.cs` 中。两者通过 `partial class` 连接。

### 架构概述
- **FairyGUI 导出代码**（`_Game/Scripts/UI/<PackageName>/XXXPanel.cs` + `XXXBinder.cs`）
  - 自动生成，包含 `GComponent` 子类 + `ConstructFromXML` 字段绑定 + `static URL` 常量
  - 命名空间 = FairyGUI 包名（如 `namespace Common`、`namespace MainMenu`）
  - **可被 FairyGUI 编辑器重新导出覆盖，禁止手动修改**
- **业务逻辑代码**（`_Game/Scripts/UI/<PackageName>/XXXPanel.Logic.cs`）
  - 手写 `partial class`，实现 `IUIPanel`（面板）或 `IUIPanel, IModalDialog`（对话框）
  - 包含生命周期实现（`OnOpen`/`OnClose`/`OnRefresh`）、业务状态、交互逻辑

### 强制规则
1. **目录结构**：按 FairyGUI 包名分目录，如 `_Game/Scripts/UI/Common/`、`_Game/Scripts/UI/MainMenu/`
2. **导出代码不可修改**：`XXXPanel.cs`、`XXXBinder.cs` 由 FairyGUI 编辑器导出，禁止手动编辑
3. **业务逻辑文件命名**：`XXXPanel.Logic.cs`（不是 `.FUI.cs`，不是无后缀 `.cs`）
4. **接口实现**：
   - 普通面板：`partial class XXXPanel : IUIPanel`
   - 对话框面板：`partial class XXXPanel : IUIPanel, IModalDialog`
5. **事件绑定只在 OnOpen 中做一次**：`OnRefresh` 必须调用 `ApplyData(data)` 而非 `OnOpen(data)`，避免事件双绑定
6. **`PanelPackageName` 属性**：使用字符串字面量（如 `"Common"`），与命名空间保持一致
7. **UIManager 使用 `type.Namespace` 推导包名**：运行时 UIManager 从类型的命名空间获取包名，因此命名空间 = FairyGUI 包名是强约束
8. **Binder 注册**：在 `GameStartupFlow.RunAsync` 中调用 `UIManager.RegisterBinder("PackageName", PackageName.XXXBinder.BindAll)`

### 推荐模板
```csharp
// ========== FairyGUI 自动导出（不要手动修改）==========
// XXXPanel.cs — 由 FairyGUI 编辑器 genCode 生成
namespace MainMenu
{
    public partial class MainMenuPanel : GComponent
    {
        public GButton btnStart;
        public GTextField txtTitle;
        public const string URL = "ui://xxxx";

        public static MainMenuPanel CreateInstance() { ... }
        public override void ConstructFromXML(XML xml) { ... }
    }
}

// ========== 手写业务逻辑 ==========
// XXXPanel.Logic.cs
using MiniGameTemplate.UI;

namespace MainMenu
{
    public partial class MainMenuPanel : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL;
        public bool IsFullScreen => true;
        public string PanelPackageName => "MainMenu";

        public void OnOpen(object data)
        {
            // 绑定按钮事件（仅在 OnOpen 中做一次）
            if (btnStart != null) btnStart.onClick.Add(OnStartClicked);
            ApplyData(data);
        }

        public void OnClose()
        {
            // 清理资源、取消定时器
        }

        public void OnRefresh(object data)
        {
            // 仅刷新数据，不重新绑定事件
            ApplyData(data);
        }

        private void ApplyData(object data) { /* 数据应用逻辑 */ }
        private void OnStartClicked() { /* 业务逻辑 */ }
    }
}
```


## 🚫 禁止事项


| 禁止 | 替代方案 |
|------|---------|
| `GameObject.Find()` | 使用 SO RuntimeSet 或 Inspector 直接引用 |
| `FindObjectOfType()` | 使用 SO RuntimeSet 的 `GetFirst()` |
| 游戏逻辑中的 Singleton | SO 事件/变量通信 |
| `GetComponent<>()` 跨系统引用 | SO 事件通道 |
| 魔法字符串 | `const` 或 SO 引用 |
| `Update()` 中的轮询逻辑 | SO Variable 的 `OnValueChanged` 事件 |
| `DontDestroyOnLoad`（Bootstrapper 外） | Singleton<T> 基类（仅框架内部） |
| `Resources.Load()` 直接加载 | AssetService 或 UIPackageLoader |
| `UnityEngine.Debug.Log/LogWarning` | `GameLog.Log` / `GameLog.LogWarning`（见日志规范） |
| `new PlayerPrefsSaveSystem()` | `GameBootstrapper.SaveSystem`（全局唯一实例） |
| `async void`（Unity 事件除外） | `async Task` 或 `async UniTask`（见异步规范） |
| `Thread` / `Task.Run` | WebGL 单线程，禁止多线程（见 WebGL 约束） |

## 目录规范

- 框架代码 → `UnityProj/Assets/_Framework/<模块名>/`
- 游戏代码 → `UnityProj/Assets/_Game/`
- 示例代码 → `UnityProj/Assets/_Example/`
- SO 资产 → 所属模块的 `Presets/` 或 `UnityProj/Assets/_Game/ScriptableObjects/`
- Entity SO 资产 → `UnityProj/Assets/_Game/Configs/`（按实体分子目录）
- **模板资产** → `UnityProj/Assets/_Game/Configs/_Template/`（`Template_` 前缀，WF-009）
- Editor 脚本 → `UnityProj/Assets/_Framework/Editor/`
- FairyGUI 工程 → `UIProject/`
- FairyGUI 导出 → `UnityProj/Assets/_Game/FairyGUI_Export/`
- 配置表源数据 → `UnityProj/DataTables/`
- 构建/生成脚本 → `UnityProj/Tools/`

## 路径规范

### 原则：优先使用相对路径

项目中**禁止硬编码系统绝对路径**（如 `C:\Users\...`、`/home/user/...`）。所有路径应通过以下方式获取：

| 场景 | 做法 |
|------|------|
| `.bat` 脚本 | `%~dp0` 取脚本所在目录，再用 `..` 做相对导航 |
| `.sh` 脚本 | `SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"`，再用 `../` 相对导航 |
| Unity C# 运行时 | `Application.streamingAssetsPath`、`Application.persistentDataPath` 等 API 动态获取 |
| Unity C# 编辑器 | `Application.dataPath`（指向 `Assets/`），通过 `Path.Combine` 向上导航到仓库根 |
| FairyGUI 发布路径 | Publish.json 中使用相对路径 `../UnityProj/Assets/_Game/FairyGUI_Export` |
| YooAsset 资源路径 | Unity 工程内的 `Assets/...` 形式路径（YooAsset 需要此前缀）|

### Unity 工程内的 `Assets/` 路径（非系统绝对路径）

以下路径是 **Unity AssetDatabase 约定的相对路径**，以 `Assets/` 开头。它们不是系统绝对路径，但属于硬编码字符串——如果 Unity 工程内的目录结构变化，需要同步修改：

| 文件 | 路径 | 说明 |
|------|------|------|
| `UIPackageLoader.cs` | `Assets/FairyGUI_Export/` | FairyGUI 包的 YooAsset 加载基路径 |
| `ConfigManager.cs` | `Assets/_Game/ConfigData/` | Luban 配置二进制数据的 YooAsset 加载基路径（`.bytes` 文件） |
| `SOCreationWizard.cs` | `Assets/_Game/ScriptableObjects` | SO 创建向导的默认保存路径（可在 Inspector 修改）|
| `ArchitectureValidator.cs` | `Assets`（`Directory.GetFiles` 起始目录） | 架构验证扫描范围 |

这些路径通过 `public static` 字段暴露，可在运行时覆盖，无需修改源码。

### 跨目录引用

仓库采用三区分离结构（`Docs/`、`UIProject/`、`UnityProj/`）。从 Unity 工程内引用仓库根目录的文件时，需要向上导航：

```csharp
// 从 Unity 编辑器代码引用仓库根的 Docs/ 目录
var docsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs"));
```

## Git 规范

- 提交前运行架构验证
- SO 资产的 `.asset` 文件正常提交（Force Text 模式，Git diff 友好）
- 大文件（图片、音频、模型）通过 Git LFS 追踪（已在 `.gitattributes` 中配置）

---

