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
Luban v4.6.0 generated C# code and data loader using **Binary ByteBuf** deserialization with **lazy deserialization**.

**Loading strategy — Lazy Deserialization**:
1. `ConfigManager.InitializeAsync()` pre-loads all `.bytes` files into a `byte[]` cache via YooAsset (I/O only, fast).
2. No deserialization happens at startup. Each table is deserialized on **first property access** (e.g. `Tables.TbItem`).
3. After deserialization, `ResolveRef()` is called automatically, and the raw `byte[]` is removed from cache to free memory.
4. Business code access is unchanged: `ConfigManager.Tables.TbItem.Get(id)` — zero API changes required.

| Class | Purpose |
|---|------|
| `ConfigManager` | Unified config entry point (YooAsset only); manages bytes cache and lazy loader |
| `TablesExtension` | Hand-written partial class; maintains table name list for pre-loading |
| `Tables` / `TbItem` / ... | Luban auto-generated with lazy properties — **do not edit** |

**Helper methods**:
- `ConfigManager.IsTableLoaded(fileName)` — returns `true` if the table has been deserialized (its raw bytes freed). Useful for diagnostics and preload verification.

**File layout**:
- `Assets/_Game/ConfigData/*.bytes` — runtime binary (collected by YooAsset)
- `Assets/_Framework/Editor/ConfigPreview/*.json` — editor preview (not packaged)

> ⚠️ ConfigManager requires YooAsset to be initialized. In Editor, use EditorSimulate mode — no AB build needed.

**Add/modify config tables**: run `Tools/gen_config.bat` (Windows) or `Tools/gen_config.sh` (macOS/Linux). See `Tools/Luban/README.md`.
