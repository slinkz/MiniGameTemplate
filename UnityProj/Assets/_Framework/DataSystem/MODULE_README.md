# DataSystem 模块

## 用途
管理所有游戏运行时数据。包含四个子模块：

### 1. Variables（SO变量）
用 ScriptableObject 持有的运行时值，替代静态字段和单例。

| 类 | 用途 |
|---|------|
| `FloatVariable` | float值，含变更事件 |
| `IntVariable` | int值，含变更事件 |
| `StringVariable` | string值，含变更事件 |
| `BoolVariable` | bool值，含变更事件 |

**创建方式**：右键 → Create → MiniGameTemplate → Variables → [类型]

**使用方式**：
```csharp
[SerializeField] private IntVariable _playerScore;

void AddScore(int amount) {
    _playerScore.ApplyChange(amount); // 自动触发 OnValueChanged 事件
}
```

### 2. RuntimeSets（运行时集合）
跟踪场景中活动实体，无需单例或 FindObjectsOfType。

| 类 | 用途 |
|---|------|
| `RuntimeSet<T>` | 泛型基类 |
| `TransformRuntimeSet` | Transform集合（最常用） |

**使用方式**：实体 OnEnable 时注册，OnDisable 时移除。

### 3. Persistence（本地存储）
| 类 | 用途 |
|---|------|
| `ISaveSystem` | 存储接口 |
| `PlayerPrefsSaveSystem` | PlayerPrefs实现 |

### 4. Config（配置表）
Luban v4.6.0 生成的 C# 代码和数据加载器，使用 **Binary ByteBuf** 格式反序列化。

| 类 | 用途 |
|---|------|
| `ConfigManager` | 统一配置加载入口（YooAsset 专用） |
| `TablesExtension` | 手写 partial class，维护表名列表供预加载 |
| `Tables` / `TbItem` / ... | Luban 自动生成，**勿手动编辑** |

**文件分布**：
- `Assets/_Game/ConfigData/*.bytes` — 运行时二进制（YooAsset 收集）
- `Assets/_Framework/Editor/ConfigPreview/*.json` — 编辑器预览（不打包）

> ⚠️ ConfigManager 要求 YooAsset 已初始化。编辑器中使用 EditorSimulate 模式，无需构建 AB。

**新增/修改配置表**：运行 `Tools/gen_config.bat`（Windows）或 `Tools/gen_config.sh`（macOS/Linux），详见 `Tools/Luban/README.md`。
