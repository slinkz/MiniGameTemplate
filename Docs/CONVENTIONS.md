# 编码规范

## 命名规范

### 文件与类
| 类型 | 规则 | 示例 |
|------|------|------|
| C# 类 | PascalCase | `PlayerHealth`, `GameEvent` |
| C# 接口 | I + PascalCase | `ISaveSystem`, `IWeChatBridge` |
| MonoBehaviour | PascalCase，名称描述唯一职责 | `ScoreDisplay`（不是 `ScoreDisplayAndSFXAndAnimation`）|
| ScriptableObject | PascalCase + 后缀 SO/Variable/Event | `FloatVariable`, `AudioClipSO`, `GameEvent` |
| SO 资产文件 | PascalCase，描述用途 | `PlayerScore.asset`, `OnGameOver.asset` |

### 变量
| 类型 | 规则 | 示例 |
|------|------|------|
| private 字段 | _camelCase | `_playerHealth`, `_isActive` |
| [SerializeField] | _camelCase | `[SerializeField] private IntVariable _score` |
| public 属性 | PascalCase | `public float Value { get; }` |
| 局部变量 | camelCase | `var currentScore = _score.Value` |
| 常量 | UPPER_SNAKE_CASE | `const int MAX_POOL_SIZE = 100` |

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
MiniGameTemplate.Platform   // WeChatBridge
MiniGameTemplate.Debug      // DebugTools
MiniGameTemplate.Utils      // Utils
MiniGameTemplate.EditorTools // Editor
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

## 目录规范

- 框架代码 → `UnityProj/Assets/_Framework/<模块名>/`
- 游戏代码 → `UnityProj/Assets/_Game/`
- 示例代码 → `UnityProj/Assets/_Example/`
- SO 资产 → 所属模块的 `Presets/` 或 `UnityProj/Assets/_Game/ScriptableObjects/`
- Editor 脚本 → `UnityProj/Assets/_Framework/Editor/`
- FairyGUI 工程 → `UIProject/`
- FairyGUI 导出 → `UnityProj/Assets/_Game/FairyGUI_Export/`
- 配置表源数据 → `UnityProj/DataTables/`
- 构建/生成脚本 → `UnityProj/Tools/`

## Git 规范

- 提交前运行架构验证
- SO 资产的 `.asset` 文件正常提交（Force Text 模式，Git diff 友好）
- 大文件（图片、音频、模型）通过 Git LFS 追踪（已在 `.gitattributes` 中配置）
