# Phase 3A TDD — §3.4 BuffComponent

> **所属文档**：[PHASE3A_TDD_INDEX.md](PHASE3A_TDD_INDEX.md) · v0.4  
> **本文件范围**：技术方案 P3.4

---

## 3.4 BuffComponent（最小版）

### 3.4.1 设计哲学

Buff = 持续时间 + 属性修正。最小版只做**属性加成/减成**（乘法修正器），不做 DOT、触发器、层数叠加等复杂机制。

### 3.4.2 BuffConfigSO

> **v0.4（ATK-006）**：BuffId 唯一性约束 + 命名规范。  
> **v0.4（GD-006）**：Duration=0 永久 Buff 文档约束 + Tooltip 改善。

```csharp
[CreateAssetMenu(menuName = "Entity/BuffConfig")]
public class BuffConfigSO : ScriptableObject
{
    [Header("基础")]
    public string DisplayName;
    
    /// <summary>
    /// 唯一标识。同 ID 的 Buff 施加时刷新（不叠层）。
    /// 
    /// ⚠️ BuffId 唯一性由策划保证（ATK-006）。
    /// 推荐命名规范：{类型前缀}{三位数字}
    ///   - buff_speed_001 → BuffId = 1001
    ///   - buff_atk_002   → BuffId = 2002
    ///   - debuff_slow_001 → BuffId = 3001
    /// 可选：Editor Validation 脚本扫描项目内所有 BuffConfigSO 检查 ID 冲突。
    /// </summary>
    public int BuffId;
    
    [Header("持续时间")]
    [Tooltip("持续秒数。0=永久Buff（不会自动过期，需通过 RemoveBuff 手动移除）")]
    [Min(0f)]
    public float Duration = 5f;
    // GD-006 文档约束：
    // - Duration > 0：持续 N 秒后自动移除
    // - Duration = 0：永久 Buff，仅通过代码 RemoveBuff(id) 移除
    // - 永久 Buff 在 Entity 回池时通过 Reset 清除（正确行为——Entity 复用 = 全新生命周期）
    // - ⚠️ 对会被回池的 Entity（敌人），永久 Buff 意义有限——Entity 生命周期通常短于 Buff 意图
    
    [Header("属性修正（乘法：最终值 = 基础值 × Modifier）")]
    [Tooltip("移速倍率（1=不变，0.5=减速50%，2=加速100%）")]
    public float MoveSpeedModifier = 1f;
    
    [Tooltip("攻击间隔倍率（1=不变，0.5=攻速翻倍，2=减速50%）")]
    public float AttackIntervalModifier = 1f;
    
    [Tooltip("受伤倍率（1=不变，0.5=减伤50%，2=受伤翻倍）")]
    public float DamageTakenModifier = 1f;
}
```

### 3.4.3 BuffComponent

> **v0.4 修正（GD-004）**：RecalcModifiers 添加 Min/Max Clamp。  
> **v0.4 修正（ATK-002）**：RecalcModifiers 不含 Sync，显式调用。  
> **v0.4 修正（UA-012）**：Reset 不调 SyncMoveSpeedToMovement。  
> **v0.4 修正（SA-013）**：同 ID 刷新时完整更新所有字段。

```csharp
public sealed class BuffComponent : IEntityComponent, ITickable
{
    // ── IEntityComponent ──
    public ComponentType Type => ComponentType.Buff;
    public bool IsActive { get; private set; }
    public void SetActive(bool active) => IsActive = active;

    // ── ITickable ──
    public int TickOrder => TickOrders.Buff; // 50

    // ── 常量 ──
    private const int MAX_BUFFS = 8;
    
    // v0.4（GD-004）：属性修正 Clamp 常量 [占位符]
    private const float MIN_MOVE_SPEED_RATIO = 0.4f;      // 最低 40% 速度
    private const float MAX_MOVE_SPEED_RATIO = 2.5f;      // 最高 250% 速度
    private const float MIN_ATTACK_INTERVAL_RATIO = 0.3f; // 最快 ~3.3 倍攻速
    private const float MAX_ATTACK_INTERVAL_RATIO = 3.0f; // 最慢 3 倍攻击间隔

    // ── 内部状态 ──
    private Entity _owner;
    private readonly BuffSlot[] _slots = new BuffSlot[MAX_BUFFS];
    private int _activeCount;

    // ── 聚合后的修正值 ──
    public float MoveSpeedModifier { get; private set; } = 1f;
    public float AttackIntervalModifier { get; private set; } = 1f;
    public float DamageTakenModifier { get; private set; } = 1f;

    // ── 生命周期 ──

    public void Init(Entity owner)
    {
        _owner = owner;
        _activeCount = 0;
        RecalcModifiers();
        // Init 不调 SyncMoveSpeedToMovement——此时 Movement 可能还未 Init
        IsActive = true;
    }

    public void Reset()
    {
        _owner = null;
        _activeCount = 0;
        for (int i = 0; i < MAX_BUFFS; i++)
            _slots[i] = default;
        RecalcModifiers();
        // UA-012 / ATK-002：Reset 不调 SyncMoveSpeedToMovement。
        // ResetAll 按枚举顺序遍历，Movement(3) 先于 Buff(10) Reset，
        // 此时 Movement._modifierCount 已归零。
        IsActive = false;
    }

    // ── Tick ──

    public void Tick(float dt)
    {
        if (_activeCount == 0) return;
        
        bool dirty = false;
        for (int i = _activeCount - 1; i >= 0; i--)
        {
            if (_slots[i].Duration <= 0f) continue; // 永久 Buff
            
            _slots[i].RemainingTime -= dt;
            if (_slots[i].RemainingTime <= 0f)
            {
                RemoveAtIndex(i);
                dirty = true;
            }
        }
        if (dirty)
        {
            RecalcModifiers();
            SyncMoveSpeedToMovement(); // ATK-002：显式调用
        }
    }

    // ── 公共 API ──

    /// <summary>施加 Buff。同 ID 刷新（完整更新属性 + 持续时间）。返回是否成功。</summary>
    public bool ApplyBuff(BuffConfigSO config)
    {
        if (config == null) return false;

        // 同 ID 检查：完整刷新（v0.4 SA-013）
        for (int i = 0; i < _activeCount; i++)
        {
            if (_slots[i].BuffId == config.BuffId)
            {
                _slots[i].Duration = config.Duration;                       // SA-013
                _slots[i].RemainingTime = config.Duration;
                _slots[i].MoveSpeedMod = config.MoveSpeedModifier;          // SA-013
                _slots[i].AttackIntervalMod = config.AttackIntervalModifier; // SA-013
                _slots[i].DamageTakenMod = config.DamageTakenModifier;      // SA-013
                RecalcModifiers();            // SA-013：属性变了需重算
                SyncMoveSpeedToMovement();    // ATK-002：显式调用
                return true;
            }
        }

        // 槽位满
        if (_activeCount >= MAX_BUFFS)
        {
            Debug.LogWarning($"[BuffComponent] Buff 槽位已满({MAX_BUFFS})，无法施加: {config.DisplayName}");
            return false;
        }

        // 新增
        _slots[_activeCount] = new BuffSlot
        {
            BuffId = config.BuffId,
            Duration = config.Duration,
            RemainingTime = config.Duration,
            MoveSpeedMod = config.MoveSpeedModifier,
            AttackIntervalMod = config.AttackIntervalModifier,
            DamageTakenMod = config.DamageTakenModifier,
        };
        _activeCount++;
        RecalcModifiers();
        SyncMoveSpeedToMovement(); // ATK-002：显式调用
        return true;
    }

    /// <summary>按 BuffId 移除指定 Buff</summary>
    public bool RemoveBuff(int buffId)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            if (_slots[i].BuffId == buffId)
            {
                RemoveAtIndex(i);
                RecalcModifiers();
                SyncMoveSpeedToMovement(); // ATK-002：显式调用
                return true;
            }
        }
        return false;
    }

    public int ActiveBuffCount => _activeCount;

    // ── 内部 ──

    private void RemoveAtIndex(int index)
    {
        _activeCount--;
        if (index != _activeCount)
            _slots[index] = _slots[_activeCount];
        _slots[_activeCount] = default;
    }

    // v0.4（GD-004）：Clamp 极端值保证手感底线
    private void RecalcModifiers()
    {
        float move = 1f, attack = 1f, damage = 1f;
        for (int i = 0; i < _activeCount; i++)
        {
            move *= _slots[i].MoveSpeedMod;
            attack *= _slots[i].AttackIntervalMod;
            damage *= _slots[i].DamageTakenMod;
        }
        MoveSpeedModifier = Mathf.Clamp(move, MIN_MOVE_SPEED_RATIO, MAX_MOVE_SPEED_RATIO);
        AttackIntervalModifier = Mathf.Clamp(attack, MIN_ATTACK_INTERVAL_RATIO, MAX_ATTACK_INTERVAL_RATIO);
        DamageTakenModifier = damage; // 不 Clamp——允许无敌(0)和脆弱(×5)
    }

    // ── Buff → Movement 同步（v0.3 UA-009 + v0.4 ATK-002/SA-003）──
    
    private void SyncMoveSpeedToMovement()
    {
        var movement = _owner.GetComponent(ComponentType.Movement) as MovementComponent;
        if (movement == null) return;
        
        if (Mathf.Approximately(MoveSpeedModifier, 1f))
            movement.RemoveSpeedModifierById(SpeedModifierIds.Buff);
        else
            movement.AddOrUpdateSpeedModifier(SpeedModifierIds.Buff, MoveSpeedModifier);
    }

    // ── 内部结构 ──

    private struct BuffSlot
    {
        public int BuffId;
        public float Duration;
        public float RemainingTime;
        public float MoveSpeedMod;
        public float AttackIntervalMod;
        public float DamageTakenMod;
    }
}
```

### 3.4.4 组件集成：Buff 修正器生效

> **v0.4（SA-003）设计决策——push + pull 混合模式**：
> - **Speed 用 push**：MovementComponent 的 SpeedModifier 系统是 Phase 1 通用设计，其他系统也可注入。Buff 是多个来源之一，通过 by-ID push 注入。Movement 不知道 Buff 存在——依赖方向正确。
> - **Attack 用 pull**：AttackComponent 只有 Buff 一个修正来源（Phase 3A），不值得建 Modifier 系统。直接 pull 查询更简单。

##### SpeedModifierIds（v0.4 SA-003/SA-010 新增）

```csharp
// SpeedModifierIds.cs — 路径：_Framework/EntitySystem/Scripts/Core/SpeedModifierIds.cs

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// MovementComponent SpeedModifier ID 注册表。(v0.4 SA-003/SA-010)
    /// ⚠️ ID 唯一性由开发者保证——如果两个系统用了相同 ID，后注册的会覆盖前一个。
    /// Phase 3A 只有 Buff 一个来源，冲突风险为零。
    /// Phase 4+ 如增加来源，考虑在 Debug 模式下添加 ID 冲突检测。
    /// </summary>
    public static class SpeedModifierIds
    {
        public const int Buff = 1;          // BuffComponent 速度修正
        public const int Terrain = 2;       // 预留：地形减速
        public const int Equipment = 3;     // 预留：装备加成
    }
}
```

##### MovementComponent by-ID 接口扩展（v0.3 UA-009）

```csharp
// MovementComponent.cs — 新增字段 + by-ID 重载

private readonly int[] _modifierIds = new int[MAX_MODIFIERS];

/// <summary>
/// 按 ID 添加或更新速度修正器。同 ID 覆盖，不同 ID 新增。
/// </summary>
public bool AddOrUpdateSpeedModifier(int id, float multiplier)
{
    for (int i = 0; i < _modifierCount; i++)
    {
        if (_modifierIds[i] == id)
        {
            _speedModifiers[i] = multiplier;
            return true;
        }
    }
    if (_modifierCount >= MAX_MODIFIERS) return false;
    _modifierIds[_modifierCount] = id;
    _speedModifiers[_modifierCount] = multiplier;
    _modifierCount++;
    return true;
}

/// <summary>按 ID 移除速度修正器。</summary>
public bool RemoveSpeedModifierById(int id)
{
    for (int i = 0; i < _modifierCount; i++)
    {
        if (_modifierIds[i] == id)
        {
            RemoveSpeedModifier(i); // 复用已有 swap-remove
            return true;
        }
    }
    return false;
}

// MovementComponent.Reset() — v0.4（ATK-011）补充确认：
// _modifierCount = 0 即可（数组元素无需逐个清零，count=0 → 所有槽位无效）。

// 向下兼容：原有 AddSpeedModifier(float) 内部给 id=-1（匿名），不与 by-ID 冲突。
```

##### AttackComponent 集成 Buff 攻速修正

```csharp
// AttackComponent.Tick — 修改攻击间隔判断
float effectiveInterval = _attackInterval;
var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
if (buff != null)
    effectiveInterval *= buff.AttackIntervalModifier;
```

### 3.4.5 ComponentType 新增

```csharp
// ComponentType.cs — 新增
Buff = 10,
// 预留 11~15
```

### 3.4.5b EntityPool.CreateComponent 工厂更新

```csharp
case ComponentType.AutoAim:  return new AutoAimComponent();
case ComponentType.Skill:    return new SkillComponent();
case ComponentType.Buff:     return new BuffComponent();
```

### 3.4.6 ApplyBuffEffect（Skill→Buff 桥接）

> **v0.4 修正（GD-013）**：SearchRadius 从硬编码 5f 提取为可配置字段。  
> **v0.4 修正（ATK-008）**：bool 返回值 + 无目标时 return false。  
> **v0.4 修正（SA-004）**：Debug.Assert。

```csharp
[System.Serializable]
public class ApplyBuffEffect : ISkillEffect
{
    [Tooltip("要施加的 Buff 配置")]
    public BuffConfigSO BuffConfig;
    
    [Tooltip("施加给自己还是目标")]
    public bool ApplyToSelf = true;
    
    [Tooltip("搜索半径（仅 ApplyToSelf=false 时生效）")]
    [Min(0.1f)]
    public float SearchRadius = 5f; // v0.4 GD-013
    
    public bool Execute(SkillContext ctx)
    {
        Entity target;
        if (ApplyToSelf)
        {
            target = ctx.Caster;
        }
        else
        {
            var mgr = EntityManagerAccessor.Instance;
            Debug.Assert(mgr != null, "[ApplyBuffEffect] EntityManager not initialized!"); // SA-004
            target = mgr?.FindNearestEntity(
                ctx.CastPosition, SearchRadius, CampUtility.GetHostileCamp(ctx.Caster.Camp));
        }
        
        if (target == null) return false;
        var buffComp = target.GetComponent(ComponentType.Buff) as BuffComponent;
        if (buffComp == null) return false;
        return buffComp.ApplyBuff(BuffConfig);
    }
}
```
