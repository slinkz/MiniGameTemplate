---
system: architecture
scope: adr-atlas
last_verified: 2026-05-02
depends_on: [ADR_03_RENDERING]
related_code: Assets/_Framework/RuntimeAtlas/*.cs
---

## ADR-029: 彻底移除 Additive Blend — 统一 Normal，简化渲染管线

### 状态
已接受（v2 — 升级为彻底移除，Supersedes v1"降级为后门"）

### 上下文

（与 v1 相同——认知负担错配、弹幕过曝、规则不统一三大问题）

当前 `RenderLayer` 枚举（Normal / Additive）作为 `BulletTypeSO.Layer` 和 `VFXTypeSO.Layer` 的公开字段暴露在 Inspector 上，由策划手动选择。激光则硬编码为 Normal。

v1 决策是"降级为后门"——保留 Additive 代码但隐藏。**天命人复审后认为：YAGNI——不需要的代码就别留着，埋着看着费劲。**

### 决策

**彻底移除 Additive Blend 相关的全部代码、配置、Shader。统一使用 Normal（Alpha Blend）。**

#### 具体删除清单

| 类别 | 文件 | 操作 |
|------|------|------|
| 枚举 | `RenderLayer.cs` | 删除 `Additive = 1` 枚举值 |
| 常量 | `RenderSortingOrder.cs` | 删除 `BulletAdditive`、`VFXAdditive` |
| Shader | `DanmakuBulletAdditive.shader` + `.meta` | 整个删除 |
| RBM | `RenderBatchManager.cs` | `Initialize()` 删除 `additiveMaterial` 参数，简化材质选择逻辑 |
| 弹丸配置 | `BulletTypeSO.cs` | 删除 `Layer` 字段及 `[Header("渲染层")]` 区域 |
| VFX 配置 | `VFXTypeSO.cs` | 删除 `Layer` 字段 |
| 弹丸渲染 | `BulletRenderer.cs` | BucketKey 不再传 Layer；fallback 只建 1 个桶 |
| VFX 渲染 | `VFXRenderer.cs` | 同上 |
| 弹幕 Config | `DanmakuRenderConfig.cs` | 删除 `BulletAdditiveMaterial` 字段 |
| VFX Config | `VFXRenderConfig.cs` | 删除 `AdditiveMaterial` 字段 |
| 编辑器 | `VFXAssetBootstrapper.cs` | 删除 Additive 材质创建和赋值 |

#### BucketKey 简化

```csharp
// Before: BucketKey = (RenderLayer, Texture2D) — 二元组
// After:  BucketKey = Texture2D only — 降维

// RBM.Initialize() 签名变化：
// Before: Initialize(keys, normalMat, additiveMat, maxQuads, sortingOrderProvider)
// After:  Initialize(keys, material, maxQuads, sortingOrderProvider)
```

#### "看起来发光"的美术替代

需要发光效果的子弹/VFX，通过贴图美术制作实现（高亮中心 + 半透明光晕边缘），用 Normal Blend 渲染。东方 Project 就是这么做的——密集叠加时互相遮挡而非色彩过曝。

#### 如果将来真的需要 Additive？

重新加——但那是一个 **新的 ADR**，而不是"取消隐藏"。到那时项目对 Additive 的需求会更清晰，设计也会更合理。

### 后果

**收益：**
- 框架干净——没有死代码、没有隐藏后门
- BucketKey 从二元组降为单一 Texture，桶数直接减半
- RBM 签名简化，去掉一个必须但几乎不用的参数
- Shader 从 3 个减为 2 个（子弹 + 激光），少一个 variant
- 策划零认知负担，弹幕场景画面可控
- 所有已有 `.asset` 序列化的 `Layer` 字段（byte 0 = Normal）天然兼容

**代价：**
- 失去 Additive Blend 的 GPU 渲染能力（需重新实现时工作量约 2 天）
- 纯美术手段模拟发光效果，视觉精度不如真正的 Additive

**这是一个故意的不可逆删除——强制未来需求走正式 ADR 流程，而不是悄悄 unhide。**

### 关联
- Supersedes ADR-029 v1
- 影响 ADR-028（BucketKey 降维）
- 影响 TDD §3.0（RenderLayer 维度从设计中移除）

---

## ADR-030: TypeRegistry 内化 + 懒注册 + 懒建桶（Supersedes ADR-017）

### 状态
已接受（2026-04-20）

### 上下文

ADR-017 规定"RBM 桶在初始化期预热，运行时禁止隐式建桶"，其前置假设是 `DanmakuTypeRegistry` / `VFXTypeRegistrySO` 作为 public ScriptableObject 资产，由策划手动将每个 TypeSO 拖入数组。初始化时遍历 Registry 数组预建全部渲染桶。

**实际问题：**

1. **策划流程断裂**：新建 BulletTypeSO 后必须手动拖到 Registry，忘了就静默消失——运行时无报错、无兜底
2. **TypeRegistry 作为 public SO 暴露了框架实现细节**：开发者不应关心 index 分配和桶预热，这是框架内部事务
3. **ADR-017 的"禁止运行时建桶"过于刚性**：预热是性能优化手段，不应该是功能正确性的前提
4. **弹幕数据不存档、不跨会话**：RuntimeIndex 只需单次运行内稳定，不需要跨会话持久化

### 决策

#### 核心原则

**TypeRegistry 是框架内部实现细节，运行时零手工注册。** 开发者只接触两样东西：

```
1. BulletTypeSO / VFXTypeSO — 创建资产、配置参数
2. Spawner.Spawn(typeSO, ...) — 调 API 生成弹丸
```

#### 1. TypeRegistry 从 public SO 降级为 internal 运行时类

```csharp
// Before: [CreateAssetMenu] public class DanmakuTypeRegistry : ScriptableObject
//   → 策划在 Inspector 里手动拖入 BulletTypeSO[]
//   → 编辑器 OnValidate 标脏 → 重建索引 → 预热桶

// After: internal class（无资产文件，纯运行时内存结构）
internal class DanmakuTypeRegistry
{
    private readonly List<BulletTypeSO> _bulletTypes = new();
    private readonly Dictionary<BulletTypeSO, ushort> _bulletIndex = new();

    // 懒注册：首次使用时自动分配 index
    public ushort GetOrRegister(BulletTypeSO type)
    {
        if (_bulletIndex.TryGetValue(type, out ushort idx))
            return idx;
        
        idx = (ushort)_bulletTypes.Count;
        _bulletTypes.Add(type);
        _bulletIndex[type] = idx;
        return idx;
    }

    public BulletTypeSO GetType(ushort index) => _bulletTypes[index];
    public int Count => _bulletTypes.Count;
    
    // LaserType / SprayType 同理...
}
```

**变更清单：**

| 项 | Before | After |
|----|--------|-------|
| 类型 | `public class DanmakuTypeRegistry : ScriptableObject` | `internal class DanmakuTypeRegistry`（非 SO） |
| 持久化 | `.asset` 文件，Inspector 可编辑 | 不持久化，每次运行重建 |
| 可见性 | public，策划可见可编辑 | internal，框架外不可见 |
| 填充方式 | 策划手动拖入 | `GetOrRegister()` 懒注册 |
| index 分配 | 编辑器 `AssignRuntimeIndices()` 预分配 | 运行时首次使用时分配 |
| `[CreateAssetMenu]` | 有 | 删除 |

`VFXTypeRegistrySO` 同理降级为 `internal class VFXTypeRegistry`。

#### 2. RBM 支持运行时懒建桶（放松 ADR-017）

```csharp
// TryGetBucket 改为 GetOrCreateBucket
public bool GetOrCreateBucket(BucketKey key, BucketCreationInfo creationInfo, out RenderBucket bucket)
{
    // 热路径：O(1) 字典命中
    if (_bucketIndex.TryGetValue(key, out int idx))
    {
        bucket = _buckets[idx];
        return true;
    }

    // 冷路径：首次遇到新类型，动态建桶
    return TryCreateBucketDynamic(key, creationInfo, out bucket);
}
```

**约束：**
- 动态建桶每次 Spawn 新类型时只发生一次，后续帧全部命中字典 = 零额外开销
- 动态建桶有日志 + 计数（可观测性保留）
- `BucketCreationInfo` 携带 templateMaterial 和 sortingOrder，由 Renderer 层传入
- 动态桶 append 到桶数组末尾，触发一次重排序（极低频事件）

#### 3. 开发者新工作流

```
Before: 策划创建 TypeSO → 拖到 Registry → OnValidate 标脏 → 重建索引 → 预热桶 → 能用
After:  策划创建 TypeSO → Spawn(typeSO, ...) → 框架自动注册+建桶 → 能用
```

**中间环节全部由框架内部消化，运行时零手工注册。**

#### 4. 编辑器预热降级为可选优化

编辑器工作流（`DanmakuEditorRefreshCoordinator`）变更：

| 项 | Before | After |
|----|--------|-------|
| Registry 重建 | 必须——扫描 SO 资产、调 `AssignRuntimeIndices()` | 可选——作为预热提示，减少首帧 spike |
| Batch 预热 | 必须——遍历 Registry 预建全部桶 | 可选——预建已知类型的桶，未知类型运行时自建 |
| 进 PlayMode 前刷新 | 必须——缺少则运行时异常 | 可选——跳过也不影响功能正确性 |
| 资源完整性校验 | 与 Registry 绑定 | 独立为编辑器工具脚本（检查贴图引用是否丢失） |
| 发现机制 | 通过 Registry SO 枚举 | 改用 `AssetDatabase.FindAssets("t:BulletTypeSO")` 等直接扫描 TypeSO |

> **扩展点**：编辑器扫描默认全库搜索。若项目规模增长导致扫描过慢或捞出不相关资产，可通过 `AssetLabel`（如 `l:DanmakuActive`）或目录前缀收窄范围。当前阶段不实现过滤。

#### 5. 对关联 ADR 的影响

| ADR | 影响 |
|-----|------|
| **ADR-017** | **Superseded** — "运行时禁止隐式建桶"放松为"运行时可安全懒建桶" |
| **ADR-015** | 修正 — "VFX Registry 仅在初始化/变更时重建"改为"Registry 支持运行时懒注册，初始化重建变为可选预热" |
| **ADR-025** | 修正 — "Registry 重建 → Batch 预热"的固定链路降级为可选编辑器优化，不再是功能正确性前提 |
| ADR-002 | 不变 — BatchManager 仍然"共享实现，不共享实例" |
| ADR-028 | 兼容 — RuntimeAtlasSystem 的 Channel 注册同样可采用懒注册模式 |

### 后果

**收益：**
- **策划零摩擦**：创建 TypeSO + Spawn 即可见，不需要知道 Registry 的存在
- **框架内聚性提升**：实现细节不暴露为 public API，减少 API 表面积
- **运行时韧性**：任何 TypeSO 首次使用时自动建桶，不再有"忘记注册→静默消失"的问题
- **为 Lazy RT 铺路**：Atlas RT 不再需要在 init 阶段就存在（桶不依赖预知 Texture 实例）
- **减少资产文件**：删除 DanmakuTypeRegistry.asset 和 VFXTypeRegistrySO.asset

**代价：**
- 首次遇到新类型时有 1~2ms 的建桶开销（new Mesh + new Material），但仅发生一次
- 失去"启动期资源完整性校验"的隐式保证（需要独立编辑器工具显式补偿）
- TypeSO 的 RuntimeIndex 不再跨会话稳定（但弹幕系统无此需求）

**风险评估：**

| 风险 | 等级 | 缓解措施 |
|------|------|---------|
| 首帧 GC spike（密集类型场景） | 低 | 编辑器可选预热仍然可用 |
| 运行时建桶排序开销 | 极低 | 一次性事件，Array.Sort 在 <20 桶规模下 <0.1ms |
| 丢失启动期资源校验 | 中 | 独立编辑器校验工具补偿（不依赖 Registry） |

### 实施范围

| 文件 | 操作 | 工作量 |
|------|------|--------|
| `DanmakuTypeRegistry.cs` | 从 `public SO` 改为 `internal class`，增加 `GetOrRegister()`，删除 `[CreateAssetMenu]` | 0.5h |
| `VFXTypeRegistrySO.cs` | 同上 | 0.5h |
| `RenderBatchManager.cs` | `TryGetBucket` → `GetOrCreateBucket`，增加 `TryCreateBucketDynamic` | 1h |
| `BulletRenderer.cs` | Initialize 改为可选预热；Rebuild 中 Spawn 路径走 `GetOrRegister` + `GetOrCreateBucket` | 1h |
| `LaserRenderer.cs` / `LaserWarningRenderer.cs` | 同上 | 0.5h |
| `VFXBatchRenderer.cs` | 同上 | 0.5h |
| `DanmakuSystem.cs` | 删除 TypeRegistry 的 Inspector 引用，内部创建 internal registry | 0.5h |
| `DanmakuEditorRefreshCoordinator.cs` | 预热降级为可选，删除 Registry 重建的强依赖 | 0.5h |
| `BulletTypeSO.cs` / 其他 TypeSO | 删除 `OnValidate → MarkDirty(registry)` 的 Registry 联动 | 0.5h |
| 删除 `.asset` 文件 | `DanmakuTypeRegistry.asset` + `VFXTypeRegistrySO.asset` | 0.1h |
| 编辑器资源校验工具 | 新建独立脚本，扫描 TypeSO 检查贴图引用完整性 | 1h |
| `DanmakuSystem.API.cs` | `FireLaser`/`FireSpray` API 从 `byte typeIndex` 改为接收 `LaserTypeSO`/`SprayTypeSO` | 0.5h |
| `PatternScheduler.cs` + 调用方 | 更新 FireLaser/FireSpray 调用签名 | 0.5h |

**总预估：~8.5h（1~1.5 天）**

#### RuntimeIndex 迁移清单（PK UA-001 产出）

**TypeSO 字段删除**（4 处）：
- `BulletTypeSO.RuntimeIndex`（ushort）→ 删除，改由 registry 内部 Dictionary 管理
- `LaserTypeSO.RuntimeIndex`（byte）→ 同上
- `SprayTypeSO.RuntimeIndex`（byte）→ 同上
- `VFXTypeSO.RuntimeIndex`（ushort）→ 同上

**写入点重定向**（3 处）：
- `BulletSpawner.cs:64` → `core.TypeIndex = registry.GetOrRegister(type)`（不再读 `type.RuntimeIndex`）
- `DanmakuSystem.API.cs:251` → `laser.LaserTypeIndex = registry.GetOrRegister(laserType)`
- `SpriteSheetVFXSystem.cs:119/199` → `instance.TypeIndex = registry.GetOrRegister(vfxType)`

**反查机械替换**（22 处）：
- `registry.BulletTypes[core.TypeIndex]` → `registry.GetBulletType(core.TypeIndex)` — 12 处（BulletMover ×4, BulletRenderer ×2, CollisionSolver ×5, DanmakuSystem.API ×1）
- `registry.LaserTypes[laser.LaserTypeIndex]` → `registry.GetLaserType(...)` — 4 处（LaserUpdater, LaserRenderer, LaserWarningRenderer ×2）
- `registry.SprayTypes[spray.SprayTypeIndex]` → `registry.GetSprayType(...)` — 3 处（SprayUpdater, CollisionSolver ×2）
- `registry.TryGet(instance.TypeIndex, ...)` → 保持接口兼容 — 3 处（VFXBatchRenderer, SpriteSheetVFXSystem ×2）

#### 多类型生命周期安全性保证（PK UA-004 产出）

内部 `List<T>` 为 **append-only**，index 一旦分配终身有效（单次运行内）：

| 类型 | 创建时写入 | 持续更新时读取 | 销毁/重置 | 安全性 |
|------|-----------|---------------|-----------|--------|
| Bullet | `core.TypeIndex = GetOrRegister(type)` | `GetBulletType(core.TypeIndex)` 反查 | Phase=Dead → slot 回收 | ✅ |
| Laser | `laser.LaserTypeIndex = GetOrRegister(type)` | `GetLaserType(index)` 反查 | Phase=0 → slot 回收 | ✅ |
| Spray | `spray.SprayTypeIndex = GetOrRegister(type)` | `GetSprayType(index)` 反查 | Phase=0 → slot 回收 | ✅ |
| VFX | `instance.TypeIndex = GetOrRegister(type)` | `TryGet(index, out type)` 反查 | Free slot | ✅ |

**Domain Reload Off**：registry 由 `DanmakuSystem`（MonoBehaviour）持有，非 static。每次 Awake 重建 → 无残留。
**系统重置**：`ClearAllBullets` 等只清实例数据，不动 registry → 已注册类型仍有效。

#### RBM 懒建桶实施约束（PK UA-003 产出）

| 约束 | 设计方向 |
|------|----------|
| BucketKey | 纯 Texture（ADR-029 已决定，RenderLayer 废弃） |
| 数组扩容 | `List<RenderBucket>` 替代固定数组，初始容量 = 预热桶数 |
| 桶初始化 | 与现有 `Initialize()` 逻辑相同（new Mesh + Material.Instantiate + 预分配缓冲） |
| 排序重建 | 仅动态建桶时触发一次 `Sort()`，基于 sortingOrder |
| 失败回滚 | 超 MaxBuckets → 返回 false + 计入 `OverflowBucketErrorCount` |
| Dispose | 遍历 `_buckets.Count`（非固定长度），逐桶释放 |
| 统计指标 | `DynamicBucketCreatedCount`（累计）+ `DynamicBucketCreatedThisFrame`（帧内峰值） |

#### PK 评审记录

3 轮 PK（6 问题）已收敛。详见 `docs/Agent/Question.md`。

### 关联
- **Supersedes**: ADR-017
- **修正**: ADR-015, ADR-025
- **兼容**: ADR-002, ADR-028, ADR-029

---

## ADR-031：RuntimeAtlas 深化——懒建页 + Laser 接入 + Trail 纹理化
