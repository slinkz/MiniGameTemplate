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
Luban生成的C#代码和数据加载器。详见 `Tools/Luban/README.md`。
