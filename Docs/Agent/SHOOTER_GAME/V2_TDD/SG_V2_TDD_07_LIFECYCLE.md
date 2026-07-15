---
system: architecture
scope: battle-lifecycle-cleanup
last_verified: 2026-05-26
depends_on: [ADR_06_LIFECYCLE, EC_TDD_04_SYSTEMS, SG_V2_TDD_01_ENEMY_SHOOTING]
related_code: Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs, Assets/_Framework/EntitySystem/Scripts/Core/EntitySystemBootstrap.cs, Assets/_Framework/DanmakuSystem/Scripts/DanmakuSystem.API.cs
---

# TDD-07：战斗退场生命周期统一事件通道

> **版本**：v0.3（增量 PK 回写）  
> **日期**：2026-05-26  
> **状态**：✅ 编码完成 2026-05-26 | PK Approved（增量复审通过）  
> **ADR 来源**：ADR-035（`Docs/Agent/ADR/ADR_06_LIFECYCLE.md`）  
> **预估工时**：~6h  
> **PK 记录**：`SG_V2_TDD_07_Question.md`  
> - Round 1（v0.1→v0.2）：Unity 编辑器工具开发者 vs 软件架构师，2 轮收敛，13 问 12 改 1 记录  
> - Round 2（v0.2→v0.3）：微信小游戏 WebGL/IL2CPP 约束专家 vs 软件架构师，1 轮收敛，8 问 6 改 2 记录

---

## 1. 目标

将 BattleController 中 **O(路径×系统)** 的手动 ClearAll 退场编排，替换为 **O(1)** 的 SO 事件通道 + 自注册观察者模式。

**成功标准**：
1. BattleController 退场逻辑中 **零直接 ClearAll() 调用**（全走事件通道）
2. 新增 `IBattleCleanup` 实现后，**无需修改 BattleController** 即可自动退场清理
3. PlayMode 验证：Victory / Defeat / PauseQuit / Retry 四条路径均无飘字残留、无跨场景泄漏
4. 编辑器验证脚本报告 **0 遗漏**

---

## 2. 现状分析

### 2.1 退场路径清单

| 路径 | 入口方法 | 清理行为 |
|------|----------|---------|
| Victory | `HandleVictoryConfirmAsync()` | HitReaction.ClearAll + DanmakuSystem.ClearAll |
| Defeat-Quit | `HandleDefeatQuit()` | HitReaction.ClearAll + DanmakuSystem.ClearAll → Pop |
| Pause-Quit | `HandlePauseQuit()` | HitReaction.ClearAll + DanmakuSystem.ClearAll → Pop |
| Retry | `HandleRetry()` → `ResetBattleRuntimeState()` | DespawnAll + HitReaction.ClearAll + CollisionSolver.ClearCooldowns + DanmakuSystem.ClearAll + Spawner.StopAll + CameraShaker.StopShake + SO 变量重置 |
| OnDestroy | `OnDestroy()` | DanmakuSystem.ClearAll + PickupRenderer.Dispose（⚠️ 缺 HitReactionHandler.ClearAll） |

### 2.2 需接管清理的系统

| # | 系统 | 当前清理方法 | 所在层 | DontDestroyOnLoad? | 备注 |
|---|------|-------------|--------|-------------------|------|
| 1 | DanmakuSystem | `ClearAll()` | 弹幕框架 | ✅ 是 | |
| 2 | EntityHitReactionHandler | `ClearAll()` | Entity 框架 | ❌ 否（随 Bootstrap 销毁） | |
| 3 | EntityCollisionSolver | `ClearCooldowns()` | Entity 框架 | ❌ 否 | |
| 4 | EntitySystemBootstrap | `DespawnAll()` + 内部清理 | Entity 框架 | ❌ 否 | |
| 5 | PickupRenderer | `Dispose()` | 游戏层 | ❌ 否 | BC 局部工具，不走事件通道 |
| 6 | BattleHUDController | `RecycleAllFloatingTexts()` | 游戏 UI 层 | ❌ 否 | ⚠️ 新增——当前退场路径未清理，本 TDD 同时修复此遗漏 |
| 7 | CameraShaker | `StopShake()` | 游戏层 | ❌ 否 | |
| 8 | WaveSpawnerDriver | `StopAll()` | Entity 框架 | ❌ 否 | |

### 2.3 痛点

- 4 条退场路径 × 8 个清理对象 = **32 处**潜在遗漏点
- 每新增一个系统 → 至少修改 4 处退场代码
- `HandleRetry` 的清理列表与其他 3 条路径**不一致**（Retry 需要额外重置 SO 变量等状态，非清理职责）

---

## 3. 设计方案

### 3.1 核心抽象

```csharp
// ────── 清理接口 ──────
namespace MiniGameTemplate.Battle
{
    /// <summary>
    /// 战斗退场清理接口。
    /// 实现者自行注册到 BattleLifecycleEvent SO，退场时自动回调。
    /// </summary>
    /// WX-001: 不使用 C# 8 DIM（Default Interface Methods），
    /// 避免 IL2CPP/WebGL 下的 AOT 兼容性风险。所有实现者显式声明 CleanupOrder。
    public interface IBattleCleanup
    {
        /// <summary>清理优先级（数值越小越先执行）。</summary>
        int CleanupOrder { get; }

        /// <summary>执行退场清理。</summary>
        void OnBattleCleanup();
    }
}
```

```csharp
// ────── SO 事件通道 ──────
namespace MiniGameTemplate.Battle
{
    [CreateAssetMenu(menuName = "ShooterGame/Events/Battle Lifecycle Event")]
    public class BattleLifecycleEvent : ScriptableObject
    {
        private readonly List<IBattleCleanup> _listeners = new(16);
        private bool _isBroadcasting;
        private readonly List<IBattleCleanup> _pendingRemoval = new(4);

        public int ListenerCount => _listeners.Count;

        // ── UT-004: Domain Reload 关闭时清空残留监听者 ──
#if UNITY_EDITOR
        private static readonly List<BattleLifecycleEvent> _allInstances = new();

        private void OnEnable() => _allInstances.Add(this);
        private void OnDisable() => _allInstances.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllListeners()
        {
            foreach (var inst in _allInstances)
                inst._listeners.Clear();
        }
#endif

        // WX-002: 静态委托缓存——确保 IL2CPP 下无重复分配
        private static readonly System.Comparison<IBattleCleanup> s_orderComparer =
            (a, b) => a.CleanupOrder.CompareTo(b.CleanupOrder);

        public void Register(IBattleCleanup listener)
        {
            // WX-002: 用 ReferenceEquals 绕过 Unity 重载 ==，避免 native interop 开销
            for (int i = 0; i < _listeners.Count; i++)
            {
                if (ReferenceEquals(_listeners[i], listener))
                    return;
            }
            _listeners.Add(listener);
            // UT-007: n ≤ 10，Sort 开销可忽略。如 listener 数显著增长（>50）改为 lazy sort
            _listeners.Sort(s_orderComparer);
        }

        public void Unregister(IBattleCleanup listener)
        {
            // UT-010: 广播期间延迟移除，避免遍历时修改列表
            if (_isBroadcasting)
                _pendingRemoval.Add(listener);
            else
                _listeners.Remove(listener);
        }

        /// <summary>
        /// 广播退场事件。所有注册者按 CleanupOrder 顺序执行清理。
        /// </summary>
        public void Raise()
        {
            _isBroadcasting = true;
            for (int i = 0; i < _listeners.Count; i++)
            {
                try
                {
                    _listeners[i].OnBattleCleanup();
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    // 继续执行后续清理，不因一个系统异常阻塞全链
                }
            }
            _isBroadcasting = false;

            // UT-010: 处理广播期间的延迟移除
            if (_pendingRemoval.Count > 0)
            {
                foreach (var listener in _pendingRemoval)
                    _listeners.Remove(listener);
                _pendingRemoval.Clear();
            }
        }

#if UNITY_EDITOR
        /// <summary>编辑器用：获取所有监听者名称（调试）。</summary>
        public string[] GetListenerNames()
        {
            var names = new string[_listeners.Count];
            for (int i = 0; i < _listeners.Count; i++)
                names[i] = $"[{_listeners[i].CleanupOrder}] {_listeners[i].GetType().Name}";
            return names;
        }
#endif
    }
}
```

### 3.2 CleanupOrder 规约

| Order | 系统 | 清理职责 | 理由 |
|-------|------|---------|------|
| 0 | DanmakuSystem | 清弹丸+激光+喷雾+飘字 | 最先清弹——阻止新命中事件产生 |
| 10 | EntityHitReactionHandler | 清 GO 飘字+闪白 | 弹幕清完后再清受击表现 |
| 20 | BattleHUDController | RecycleAllFloatingTexts | UI 飘字最后清 |
| 30 | EntityCollisionSolver | ClearCooldowns | 碰撞冷却重置 |
| 40 | PickupRenderer | Dispose | 释放渲染资源 |
| 50 | CameraShaker | StopShake | 停止震动 |
| 60 | WaveSpawnerDriver | StopAll | 停止刷怪 |
| 100 | EntitySystemBootstrap | DespawnAll + 内部清理 | 最后回收所有 Entity |

> **Order 分配规则**：100 = 保留给 EntitySystemBootstrap（必须最后执行）。新系统应在 0~90 范围内分配，间隔 10。

### 3.3 BoolVariable 哨兵（已有 SO 变量框架）

```csharp
// 复用已有的 BoolVariable SO
[SerializeField] private BoolVariable _battleActive;

// BattleController.Start() 中：
_battleActive.SetValue(true);

// BattleController 退场事件触发前：
_battleActive.SetValue(false);

// DanmakuSystem.UpdatePipeline() 检查：
if (!_battleActive.Value) return;
```

### 3.4 Retry 特殊路径

`Retry` 不是退场，是**重置**。语义不同于 Victory/Defeat/PauseQuit：

- Retry = ClearAll + 重新初始化（不离开场景）
- 其他 = ClearAll + Pop（离开场景）

**决策**：`BattleLifecycleEvent` 只管「清理」语义。Retry 路径调用 `_onBattleEnd.Raise()` 后再执行额外重置逻辑（SO 变量归零、重新 Spawn 等）。

```csharp
private void ResetBattleRuntimeState()
{
    // 统一清理（走事件通道）
    // ⚠️ WX-006 约束：OnBattleCleanup 实现中不应依赖 SO 变量状态。
    // Retry 路径中 Raise() 先于 SO 变量重置执行，此时 SO 变量仍为旧值。
    _onBattleEnd.Raise();

    // Retry 专属重置（非清理语义，不放入 IBattleCleanup）
    _currentWaveIndex.SetValue(1);
    _killCount.SetValue(0);
    _displayWaveIndex = 1;
    _damageStats?.Clear();
    _battleTimer = 0f;
    _damageStatsFrozen = false;
    _lastBattleResult = null;
}
```

---

## 4. 实施计划

### Phase A：基础设施（~1.5h）

| 步骤 | 内容 | 产出 |
|------|------|------|
| A1 | 创建 `IBattleCleanup` 接口 | `Assets/_Game/Scripts/ShooterGame/Battle/IBattleCleanup.cs` |
| A2 | 创建 `BattleLifecycleEvent` SO | `Assets/_Game/Scripts/ShooterGame/Battle/BattleLifecycleEvent.cs` |
| A3 | 创建 SO 资产 | `Assets/_Game/Configs/ShooterGame/Events/OnBattleEnd.asset` |
| A4 | 在 BattleController 添加 `[SerializeField] BattleLifecycleEvent _onBattleEnd` 字段 | — |
| A5 | 编译验证 0E/0W | — |

**Phase A 门禁验收**：
- [ ] 编译 0E/0W
- [ ] SO 资产可在 Inspector 中创建并引用

### Phase B：系统迁移（~2.5h）

逐个系统实现 `IBattleCleanup`，注册到 SO 事件：

| 步骤 | 系统 | 改动 |
|------|------|------|
| B1 | DanmakuSystem | 实现 IBattleCleanup, **Awake 中 Register**（DDOL 永久监听者，不注销）, `OnBattleCleanup() => ClearAll()`。⚠️ WX-007: 加 null 检查——`if (_onBattleEnd != null) _onBattleEnd.Register(this); else Debug.LogError(...)` |
| B2 | EntityHitReactionHandler | ⚠️ 非 MonoBehaviour，由 Bootstrap 代理注册（创建 `EntityCleanupProxy` MB 或在 Bootstrap 中实现接口） |
| B3 | EntityCollisionSolver | 同 B2 策略（Bootstrap 代理） |
| B4 | BattleHUDController | 实现 IBattleCleanup, `OnBattleCleanup() => RecycleAllFloatingTexts()` |
| B5 | PickupRenderer | ⚠️ 非 MonoBehaviour（IDisposable），由 BattleController 自身在 OnDestroy 中 Dispose（局部工具，不走事件通道） |
| B6 | CameraShaker | 实现 IBattleCleanup, `OnBattleCleanup() => StopShake()` |
| B7 | WaveSpawnerDriver | 非 MB，由 EntitySystemBootstrap 代理 |
| B8 | EntitySystemBootstrap | 实现 IBattleCleanup（作为代理包含 B2/B3/B7 + DespawnAll）。⚠️ DespawnAll 只操作 EntityManager.ActiveEntities，不影响 Bootstrap 自身。加 `Debug.Assert(isActiveAndEnabled)` 防御 |
| B9 | 编译验证 0E/0W | — |

**Phase B 门禁验收**：
- [ ] 编译 0E/0W
- [ ] 所有 IBattleCleanup 实现类的 SerializeField 引用 OnBattleEnd SO 资产
- [ ] DanmakuSystem.Awake 中 Register 调用已添加

**关键决策——非 MonoBehaviour 系统的注册策略**：

EntityHitReactionHandler / CollisionSolver / WaveSpawnerDriver 都不是 MonoBehaviour，无法自行 OnEnable/OnDisable。

**方案**：由 `EntitySystemBootstrap`（MonoBehaviour）统一实现 `IBattleCleanup`，在 `OnBattleCleanup()` 内编排框架内部清理顺序：

```csharp
public class EntitySystemBootstrap : MonoBehaviour, IBattleCleanup
{
    [SerializeField] private BattleLifecycleEvent _onBattleEnd;

    public int CleanupOrder => 100; // 最后执行

    private void OnEnable() => _onBattleEnd?.Register(this);
    private void OnDisable() => _onBattleEnd?.Unregister(this);

    public void OnBattleCleanup()
    {
        _entityManager?.DespawnAll();
        _hitHandler?.ClearAll();
        _viewBridge?.ClearAllViews();
        _collisionSolver?.ClearCooldowns();
        EntityConfigRegistry.Clear();
    }
}
```

同理，`PickupRenderer` 的 Dispose 由 BattleController 内部在 Raise 之后额外调用（因为 PickupRenderer 是 BattleController 创建的局部对象，非全局系统）。

### Phase C：替换退场路径（~1h）

| 步骤 | 内容 |
|------|------|
| C1 | `HandleVictoryConfirmAsync` → 删除手动 ClearAll，改为 `_onBattleEnd.Raise()` |
| C2 | `HandleDefeatQuit` → 同上 |
| C3 | `HandlePauseQuit` → 同上 |
| C4 | `ResetBattleRuntimeState` → `_onBattleEnd.Raise()` + Retry 专属重置 |
| C5 | `OnDestroy` → `_onBattleEnd.Raise()` + `if (DanmakuSystem.Instance != null) DanmakuSystem.Instance.ClearAll()`（DDOL 兜底，WX-003: 用 Unity 重载 `!=` 而非 `?.`） + PickupRenderer.Dispose |
| C6 | 添加 `_battleActive` BoolVariable 哨兵 |
| C7 | 编译验证 0E/0W |

**Phase C 门禁验收**：
- [ ] 编译 0E/0W
- [ ] BattleController 中零直接 ClearAll() 调用（仅 OnDestroy 保留 DanmakuSystem 兜底）
- [ ] `_onBattleEnd.Raise()` 出现在所有退场路径中

### Phase D：编辑器验证（~0.5h）

| 步骤 | 内容 |
|------|------|
| D1 | 创建 `BattleCleanupValidator` 编辑器脚本 |
| D2 | 扫描所有实现 `IBattleCleanup` 的类型 |
| D3 | 验证它们都有 `[SerializeField] BattleLifecycleEvent` 字段引用 |
| D4 | 输出报告到 Console |

**Phase D 门禁验收**：
- [ ] 编译 0E/0W
- [ ] Validator 报告 0 遗漏（直接注册者）
- [ ] ⚠️ **Validator 局限性**：仅验证直接 IBattleCleanup 实现类。代理注册（Bootstrap 代理 HitReactionHandler/CollisionSolver/WaveSpawnerDriver）和例外处理（PickupRenderer.Dispose）需 Code Review 覆盖。TODO：V2 考虑 attribute 标注方案
- [ ] ⚠️ **WX-007 实例级检查**：Validator 扫描场景中所有含 `[SerializeField] BattleLifecycleEvent` 的 MB 实例，检查引用是否为 null → 输出到 Console

### Phase E：验收（~0.5h）

#### 门禁验收（阻塞项——不通过不可合入）

| 验收项 | 检查点 |
|--------|--------|
| E5 | OnDestroy（编辑器停止 PlayMode）→ 无报错、无泄漏 |
| E6 | 新增 DummyCleanup 实现 → 不修改 BC → 验证自动触发 |

#### 全局集成验收（延后到统一真机/视觉验收阶段）

| 验收项 | 路径 | 检查点 |
|--------|------|--------|
| E1 | Victory → 确认 | 无飘字/弹丸残留，场景干净切换 |
| E2 | Defeat → Quit | 同上 |
| E3 | Pause → Quit | 同上 |
| E4 | Defeat → Retry | 清理后正常重开，SO 变量归零 |

---

## 5. 文件变更清单

| 操作 | 路径 | 说明 |
|------|------|------|
| 新建 | `Assets/_Game/Scripts/ShooterGame/Battle/IBattleCleanup.cs` | 清理接口 |
| 新建 | `Assets/_Game/Scripts/ShooterGame/Battle/BattleLifecycleEvent.cs` | SO 事件通道 |
| 新建 | `Assets/_Game/Configs/ShooterGame/Events/OnBattleEnd.asset` | SO 资产 |
| 新建 | `Assets/_Game/Scripts/ShooterGame/Editor/BattleCleanupValidator.cs` | 编辑器验证脚本 |
| 修改 | `Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs` | 退场路径重构 |
| 修改 | `Assets/_Framework/EntitySystem/Scripts/Core/EntitySystemBootstrap.cs` | 实现 IBattleCleanup |
| 修改 | `Assets/_Framework/DanmakuSystem/Scripts/DanmakuSystem.cs` | 实现 IBattleCleanup |
| 修改 | `Assets/_Game/Scripts/ShooterGame/UI/BattleHUDController.cs` | 实现 IBattleCleanup |
| 修改 | `Assets/_Game/Scripts/ShooterGame/Battle/CameraShaker.cs` | 实现 IBattleCleanup |
| 可选 | 新建 `BoolVariable` SO 资产 `BattleActive.asset` | 哨兵变量 |

---

## 6. 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| DanmakuSystem 是 DontDestroyOnLoad，OnDisable 不会在场景切换时触发 | 注销时机错误 → 事件通道残留监听 | **UT-001**：DanmakuSystem 为永久监听者——Awake 中 Register，设计上不通过 OnDisable 注销。ClearAll 幂等，多次 Raise 安全 |
| Retry 路径清理后需要重新 Register | Raise 后 EntitySystemBootstrap 被 DespawnAll 但未销毁 | **UT-002**：DespawnAll 只操作 EntityManager.ActiveEntities，Bootstrap 不在管理列表中——这是架构不变量。加 `Debug.Assert(isActiveAndEnabled)` 防御 |
| 排序相同 Order 的系统执行顺序不确定 | 可能影响依赖关系 | 每个系统分配唯一 Order 值，间隔 10 |
| Phase B 中间状态：部分系统已迁移、部分未迁移 | 退场不完整 | 迁移采用"添加新路径+保留旧代码"策略，Phase C 统一删除旧代码。**UT-013**：`ClearAll`/`StopShake`/`ClearCooldowns` 等方法均为幂等操作，双重调用安全 |
| OnDestroy 中调用 Raise 时，其他 MB 可能已被销毁 | 非 DDOL listener 可能无法执行清理 | **UT-005**：OnDestroy 非运行时关键路径（正常退场走事件通道 + Pop，不走 OnDestroy）。兜底策略：`_onBattleEnd?.Raise()` + `DanmakuSystem.Instance?.ClearAll()`（DDOL 一定存活）+ `PickupRenderer?.Dispose()` |
| Enter Play Mode Settings 关闭 Domain Reload 时，SO 的 _listeners 残留 | 僵尸监听者 → MissingReferenceException | **UT-004**：`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 静态方法清空所有实例的 `_listeners` |
| Unity Object 的 `?.` 与 `!= null` 语义差异 | `?.` 使用 C# 原生 null check（ReferenceEquals），无法检测已销毁的 Unity Object → 可能在已销毁对象上调用方法 | **WX-003**：所有 Unity Object 引用的 null 检查必须使用 `!= null`（Unity 重载 `==`），禁止使用 `?.` |

---

## 7. 不做的事

| 项目 | 理由 |
|------|------|
| 合并三套飘字系统 | 各层职责不同，无痛点驱动（已决策 2026-05-26） |
| 引入 async cleanup | 当前所有清理都是同步操作，不需要 await |
| 对 Retry 路径也完全走事件通道 | Retry = 清理 + 重置，重置部分（SO 变量归零等）是 BattleController 专属逻辑，不应扩散到子系统 |
| 跨场景持久化事件通道状态 | SO 事件是内存态，应用重启自然清零，无需序列化 |
| 将 PickupRenderer 等 BC 局部对象纳入事件通道 | PickupRenderer 由 BC `new` 创建，生命周期完全由 BC 管理，是私有工具而非全局系统。**判定标准**：`new` 创建 + 仅 BC 引用 = 局部工具 → BC 自管（OnDestroy 中 Dispose）；`[SerializeField]` 注入或全局单例 = 全局系统 → 走 SO 事件通道 |

---

## 8. 关联文档

| 文档 | 关系 |
|------|------|
| `ADR/ADR_06_LIFECYCLE.md` | 本 TDD 的设计来源（ADR-035） |
| `SYSTEMS/EC_TDD/EC_TDD_04_SYSTEMS.md` | EntitySystemBootstrap 架构 |
| `SHOOTER_GAME/V2_TDD/SG_V2_TDD_01_ENEMY_SHOOTING.md` | DanmakuSystem 碰撞 → 飘字触发链路 |
| `SYSTEMS/CONV/CONV_03_PLATFORM.md` | WebGL/微信限制（DontDestroyOnLoad 约束） |

---

_创建于 2026-05-26 | ADR-035 实施 TDD_
