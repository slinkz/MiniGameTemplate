# ObjectPool 模块

## 用途
通用对象池系统。通过ScriptableObject配置池大小和预制件，避免运行时频繁Instantiate/Destroy。

## 核心类
| 类 | 用途 |
|---|------|
| `PoolDefinition` | [CreateAssetMenu] 池配置SO（预制件 + 初始大小 + 最大大小） |
| `ObjectPool` | 单个池的实现 |
| `PoolManager` | 池管理器（按PoolDefinition创建和管理池） |
| `PooledObject` | 挂在池对象上的组件，支持自动延时回收 |

## 使用方式
```csharp
[SerializeField] private PoolDefinition _bulletPoolDef;

void Shoot() {
    var bullet = PoolManager.Instance.Get(_bulletPoolDef);
    // 使用 bullet...
}

// 回收（手动）
PoolManager.Instance.Return(_bulletPoolDef, bullet);

// 或挂上 PooledObject 组件后自动延时回收
```
