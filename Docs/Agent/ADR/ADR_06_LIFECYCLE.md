---
system: architecture
scope: battle-lifecycle-cleanup
last_verified: 2026-05-23
depends_on: [EC_TDD_02_CORE_ARCH, EC_TDD_04_SYSTEMS]
related_code: Assets/_Framework/DanmakuSystem/**, Assets/_Framework/EntitySystem/Scripts/View/EntityHitReactionHandler.cs, Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs
---

# ADR-035：战斗退场生命周期统一事件通道

- **日期**：2026-05-23
- **状态**：✅ Accepted（已实施，代码级确认 2026-07-14；实施 TDD 编码完成 2026-05-26）
- **触发**：飘字残留 Bug 排查耗时过长，暴露退场清理架构系统性弱点

---

## 1. 问题背景

### 1.1 症状

飘字（DamageNumber）在战斗结束后残留在屏幕上。排查发现问题根因不在飘字系统本身，而在于**退场生命周期管理缺乏统一协议**。

### 1.2 现状分析

当前退场清理采用**中央编排模式**：BattleController 在 Victory/Defeat/PauseQuit 三条路径中逐个手动调用各子系统的 ClearAll()。

```
退场路径 × 需清理系统 = O(路径数 × 系统数) 的维护负担

当前：
- 退场路径：3（Victory / Defeat / PauseQuit）
- 需清理系统：5+（DanmakuSystem / HitReactionHandler / PoolManager / EventBus / BattleHUD / ...）
- 每新增一个系统 → 3 处代码需修改
- 遗漏任何一处 → 残留/泄漏 Bug
```

### 1.3 暴露的四个架构问题

| # | 问题 | 表现 | 影响 |
|---|------|------|------|
| 1 | 缺乏统一退场事件 | 每条路径手动清理列表 | 新增系统必漏 |
| 2 | DontDestroyOnLoad 系统无场景感知 | 跨场景僵尸引用 | Entity 残留、飘字残留 |
| 3 | 时间源隐式选择 | timeScale=0 冻住表现层 | 暂停后飘字堆积 |
| 4 | 清理编排硬编码 | 无法自动验证覆盖完整性 | 回归风险高 |

---

## 2. 决策

### ADR-035：引入 BattleLifecycleEvent SO 事件通道

采用**观察者模式**替代**中央编排模式**：

- 创建 `BattleLifecycleEvent : ScriptableObject` 事件通道资产
- 所有需要退场清理的系统**自行注册**监听
- BattleController 退场时只需 `_onBattleEnd.Raise()` 一次调用
- 新增系统时 **O(1) 操作**：只需在新系统中注册监听，无需修改退场路径代码

### 设计草案

```csharp
// --- 事件通道 SO ---
[CreateAssetMenu(menuName = "Events/Battle Lifecycle Event")]
public class BattleLifecycleEvent : ScriptableObject
{
    private readonly List<IBattleCleanup> _listeners = new();

    public void Raise()
    {
        // 倒序遍历，后注册的先清理（子系统先于基础设施）
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnBattleCleanup();
    }

    public void Register(IBattleCleanup listener)
    {
        if (!_listeners.Contains(listener)) _listeners.Add(listener);
    }

    public void Unregister(IBattleCleanup listener)
    {
        _listeners.Remove(listener);
    }
}

// --- 清理接口 ---
public interface IBattleCleanup
{
    /// <summary>执行优先级（数值越小越先执行）。默认 0。</summary>
    int CleanupOrder => 0;
    void OnBattleCleanup();
}

// --- 使用示例（DamageNumberSystem）---
public partial class DamageNumberSystem : IBattleCleanup
{
    [SerializeField] private BattleLifecycleEvent _onBattleEnd;

    public int CleanupOrder => 10; // 子弹清理(0)之后、池回收(100)之前

    private void OnEnable() => _onBattleEnd.Register(this);
    private void OnDisable() => _onBattleEnd.Unregister(this);

    public void OnBattleCleanup() => ClearAll();
}

// --- BattleController 退场（统一入口）---
private void ExitBattle()
{
    _onBattleEnd.Raise();  // 一行搞定所有清理
    // ... 场景转换 ...
}
```

### 清理顺序规约

| CleanupOrder | 系统 | 职责 |
|-------------|------|------|
| 0 | DanmakuSystem | 清子弹（阻止新命中） |
| 10 | DamageNumberSystem | 清 GPU 飘字 |
| 20 | EntityHitReactionHandler | 清 GO 飘字 + 闪白 |
| 30 | BattleHUDController | 清 UI 飘字 |
| 50 | EntitySystemBootstrap | DespawnAll（注销碰撞） |
| 100 | PoolManager | ClearAll（回收所有借出 GO） |

---

## 3. 配套改进

### 3.1 DontDestroyOnLoad Session 哨兵

```csharp
// DontDestroyOnLoad 系统通过 SO Flag 感知战斗是否活跃
[CreateAssetMenu(menuName = "Variables/Bool")]
public class BoolVariable : ScriptableObject
{
    public bool Value;
}

// DanmakuSystem 在 UpdatePipeline 前检查
if (!_battleActive.Value) return; // 战斗已结束，跳过所有逻辑
```

### 3.2 TimeMode 显式声明（已部分落地）

铁律已建立：纯视觉效果用 `unscaledDeltaTime`。后续新增视觉系统时照办。

### 3.3 编辑器自动验证（长期）

编辑器脚本扫描所有 `IBattleCleanup` 实现者，验证它们都引用了 `BattleLifecycleEvent` 资产。

---

## 4. 收益

| 指标 | 改进前 | 改进后 |
|------|--------|--------|
| 新增系统的退场接入成本 | 修改 3 条退场路径 | 自身 Register 一行 |
| 遗漏清理的概率 | 随系统数线性增长 | 接近零（自注册） |
| 排查退场 Bug 的定位时间 | 逐路径逐系统排查 | 检查 CleanupOrder + 监听列表 |
| 代码耦合度 | BattleController 知道所有子系统 | BattleController 只知道一个 SO 事件 |

---

## 5. 实施计划

| 阶段 | 内容 | 前置 |
|------|------|------|
| Phase A | 创建 BattleLifecycleEvent SO + IBattleCleanup 接口 | — |
| Phase B | 迁移现有 5 个系统的 ClearAll 为监听模式 | Phase A |
| Phase C | 删除 BattleController 中的手动 ClearAll 调用列表 | Phase B 验证通过 |
| Phase D | 添加 BoolVariable _battleActive 哨兵 | Phase B |
| Phase E | 编辑器验证脚本 | Phase B |

**执行状态**：已落地到代码。2026-07-14 复核确认：`BattleLifecycleEvent`、`IBattleCleanup`、`BattleController` Raise 路径、`DanmakuSystem`、`EntitySystemBootstrap`、`CameraShaker`、`BattleCleanupValidator` 与 `SG_OnBattleEnd.asset` 场景绑定均存在。

---

## 6. 关联

- MEMORY.md 铁律：`DontDestroyOnLoad 池对象退场必清`
- MEMORY.md 铁律：`DontDestroyOnLoad 系统退场必注销`
- MEMORY.md 铁律：`退场路径 DanmakuSystem.ClearAll()`
- MEMORY.md 铁律：`纯视觉效果用 unscaledDeltaTime`
- ADR-033：Entity-Component 框架
- EC_TDD_04：Systems 设计

---

# ADR-036：飘字系统统一到 RBM 渲染管线

- **日期**：2026-06-03
- **状态**：✅ Accepted（已实施 2026-06-03）
- **触发**：双飘字 Bug（弹幕层 + Entity 层同时渲染同一命中的飘字）
- **详细 TDD**：[SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD.md](SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD.md)

## 上下文

项目存在两套并行飘字系统：弹幕层 `DamageNumberSystem`（RBM 零 GC）和 Entity 层 `EntityHitReactionHandler.SpawnDamageNumber`（TextMesh 对象池）。同一次命中可能同时触发两套飘字，产生重叠的不同数值。排查飘字问题需追踪两条管线，维护成本高。

## 决策

合并两套飘字为一套 `FloatingTextSystem`（`MiniGameTemplate.Rendering` 命名空间），基于 RBM + 环形缓冲区。`Spawn()` 签名新增 `Color32 color` 参数，颜色语义由调用方决定。DanmakuSystem 持有并初始化，通过 `DanmakuSystem.FloatingText` 属性公开给 Entity 层。

## 后果

- ✅ 飘字排查只需一个入口
- ✅ 删除 ~120 行 Entity 层飘字代码 + DamageNumber Prefab + 对象池
- ✅ 零 GC（消除 TextMesh ToString / GetComponent 开销）
- ⚠️ Entity 层新增对 DanmakuSystem 的弱运行时引用
- ⚠️ 暴击 `!` 后缀消失（改为缩放+颜色区分）

## 关联

- ADR-009：DamageNumber 共用数字图集（本 ADR 延续该决策的渲染基础）
- ADR-035：战斗退场生命周期（ClearAll 清理路径）
- SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD.md：完整实施 TDD
