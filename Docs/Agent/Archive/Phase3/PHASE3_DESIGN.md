# Entity-Component Phase 3 设计方案（飞行射击弹幕完整版）

> 状态：📋 **待天命人确认** | 日期：2026-05-01  
> 🎮 **游戏设计师评审版** — 补充即时循环反馈层 + 会话结构层

---

## 零、游戏设计师评估摘要

### 原方案诊断

Phase 3 原方案聚焦"战斗能力扩展"（AutoAim / DamageDealer / Skill / Buff），但**忽略了游戏循环闭合所需的反馈层和流程层**：

| 缺失功能 | 影响 | 严重度 |
|----------|------|--------|
| 击杀计分 | 即时循环断裂——玩家没有成就反馈 | 🔴 P0 |
| 道具掉落/拾取 | 会话循环断裂——玩家没有"变强"的体感 | 🔴 P0 |
| 玩家命数/续命 | 会话结构断裂——单命制太残酷，留存杀手 | 🟡 P1 |
| 玩家移动边界 | 玩家能飞出屏幕，基础体验漏洞 | 🟡 P1 |
| 难度渐进扩展 | 仅弹幕参数变化，敌人数量/频率无感 | 🟡 P1 |

### 设计决策：将 Phase 3 拆分为两个子阶段

```
Phase 3A — 战斗能力扩展（原 P3.1~P3.4，保持不变）
Phase 3B — 游戏循环闭合（新增 6 个刚需模块）
```

**理由**：3A 和 3B 可并行或串行，但 3B 不依赖 3A 的 Skill/Buff（击杀计分、道具拾取不需要技能系统）。建议**先做 3B 再做 3A**——先让游戏"能玩"，再让战斗"好玩"。

---

## 一、Phase 3A — 战斗能力扩展（原方案，保持不变）

### P3.1 空间查询 + AutoAimComponent

**目标**：让 Entity 能"感知周围"并"自动瞄准最近目标"

#### 3.1.1 FindEntitiesInRadius 实现

```csharp
// EntityManager.cs — 补完已预留的 stub
public int FindEntitiesInRadius(
    Vector2 center, float radius, EnumCamp camp,
    Entity[] resultBuffer, int maxResults)
{
    int count = 0;
    float radiusSq = radius * radius;
    
    for (int i = 0; i < _activeEntities.Count && count < maxResults; i++)
    {
        var e = _activeEntities[i];
        if (e.IsPendingDespawn) continue;
        if (e.Camp != camp) continue;
        
        float distSq = (e.Position - center).sqrMagnitude;
        if (distSq <= radiusSq)
        {
            resultBuffer[count++] = e;
        }
    }
    return count;
}
```

**新增便捷 API**：

```csharp
/// <summary>查找指定阵营的最近 Entity（零 GC，内部复用静态 buffer）</summary>
public Entity FindNearestEntity(Vector2 center, float radius, EnumCamp camp)
{
    int count = FindEntitiesInRadius(center, radius, camp, _sharedBuffer, _sharedBuffer.Length);
    if (count == 0) return null;
    
    Entity nearest = null;
    float nearestDistSq = float.MaxValue;
    for (int i = 0; i < count; i++)
    {
        float dSq = (_sharedBuffer[i].Position - center).sqrMagnitude;
        if (dSq < nearestDistSq)
        {
            nearestDistSq = dSq;
            nearest = _sharedBuffer[i];
        }
    }
    return nearest;
}

private static readonly Entity[] _sharedBuffer = new Entity[64];
```

#### 3.1.2 AutoAimComponent

```csharp
public sealed class AutoAimComponent : IEntityComponent, ITickable
{
    public ComponentType Type => ComponentType.AutoAim;
    public int TickOrder => TickOrders.AutoAim; // 140
    
    private Entity _owner;
    private float _searchRadius;
    private float _searchInterval;
    private float _timer;
    private Entity _currentTarget;
    
    public Vector2 AimDirection { get; private set; }
    public bool HasTarget => _currentTarget != null 
                          && _currentTarget.IsAlive 
                          && !_currentTarget.IsPendingDespawn;
}
```

**EntityConfigSO 新增字段**：

```csharp
[Header("自动瞄准（P3.1）")]
public float AutoAimRadius = 0f;
[Min(0.05f)]
public float AutoAimSearchInterval = 0.2f;
```

**交付清单**：
- [ ] `EntityManager.FindEntitiesInRadius` 实现
- [ ] `EntityManager.FindNearestEntity` 便捷 API
- [ ] `AutoAimComponent.cs` 新文件
- [ ] `TickOrders.AutoAim = 140` 常量
- [ ] `EntityConfigSO` 新增 AutoAimRadius / AutoAimSearchInterval
- [ ] `EntityConfigSOEditor` 补齐新字段绘制
- [ ] `AttackComponent.GetFireAngle` 增加 AutoAim 优先级
- [ ] `EntityPool` 组件工厂补充 AutoAim case
- [ ] Play Mode 验证：敌人自瞄玩家

---

### P3.2 直接伤害路径（DamageDealer）

```csharp
public static class DamageDealer
{
    public static void DealDamageToEntity(Entity target, DamageContext context) { ... }
    
    public static int DealAreaDamage(
        Vector2 center, float radius, EnumCamp targetCamp,
        DamageContext baseContext, int maxTargets = 16) { ... }
    
    private static readonly Entity[] _buffer = new Entity[64];
}
```

**交付清单**：
- [ ] `DamageDealer.cs` 新文件
- [ ] Play Mode 验证：手动调用 DealAreaDamage 对范围内敌人造伤

---

### P3.3 SkillComponent（最小可用版）

设计哲学不变：一个 Skill = 一个 SO 配置 + N 个 SkillEffect（策略接口）。

```
SkillConfigSO（SO 资产）
├── DisplayName / CooldownTime / CastTime / RecoveryTime
├── TriggerMode: Manual / Auto / Passive
└── Effects: ISkillEffect[]（[SerializeReference]）
      ├── FireBulletsEffect
      ├── AreaDamageEffect
      └── ApplyBuffEffect
```

**交付清单**：
- [ ] `SkillConfigSO.cs` / `ISkillEffect.cs` + `SkillContext` struct
- [ ] `FireBulletsEffect.cs` / `AreaDamageEffect.cs`
- [ ] `SkillComponent.cs`（ComponentType.Skill = 6，TickOrder = 160）
- [ ] `EntityConfigSO.SkillConfig` 字段
- [ ] Play Mode 验证：配置 AOE 技能，敌人自动释放

---

### P3.4 Buff/Debuff 系统（最小版）

```csharp
public sealed class BuffComponent : IEntityComponent, ITickable
{
    public ComponentType Type => ComponentType.Buff; // = 10
    public int TickOrder => TickOrders.Buff; // 50
    private const int MAX_BUFFS = 8;
}
```

**交付清单**：
- [ ] `BuffConfigSO.cs` / `BuffComponent.cs`
- [ ] `ComponentType.Buff = 10` / `TickOrders.Buff = 50`
- [ ] `ApplyBuffEffect.cs` 补完
- [ ] Play Mode 验证：施加减伤 Buff → 确认伤害降低

---

## 二、Phase 3B — 游戏循环闭合（新增）

> 🎮 **设计师注**：以下 6 个模块是飞行射击弹幕小游戏从"技术 Demo"变为"可玩游戏"的最小闭合集。每个模块回答一个核心问题。

---

### P3B.1 击杀计分系统

**回答的问题**：玩家为什么要打敌人？

#### 设计

```
玩家击杀敌人 → 发布 OnEntityKilled 事件 → ScoreManager 监听 → 加分 → 触发 OnScoreChanged → UI 更新
```

**关键设计**：
- 分数来源可配置——每种 Entity 的击杀分值不同（EntityConfigSO 新增 `KillScore`）
- 连击加成（Combo）：1 秒内连续击杀 → 分数乘数递增（1x→2x→3x...），断连归 1
- 分数最终通过 `IWeChatBridge.SubmitScore()` 提交排行榜

#### ScoreManager（纯 C# 服务，非 MonoBehaviour）

```csharp
/// <summary>
/// 击杀计分管理器——监听 Entity 死亡事件，维护分数和连击。
/// 由 EntitySystemBootstrap 持有，无 MonoBehaviour 依赖。
/// </summary>
public class ScoreManager
{
    // ── 公开状态 ──
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public int ComboCount { get; private set; }
    public float ComboMultiplier => 1f + (ComboCount - 1) * ComboStep;
    
    // ── 配置 ──
    private const float COMBO_WINDOW = 1.2f;   // 连击窗口（秒）
    private const float ComboStep = 0.5f;      // 每次连击加成步长
    private const int MAX_COMBO_MULTI = 5;     // 最大连击倍率
    
    private float _comboTimer;
    
    // ── 事件（供 UI 监听）──
    public event System.Action<int> OnScoreChanged;      // 参数：当前分数
    public event System.Action<int> OnComboChanged;      // 参数：当前连击数
    public event System.Action OnComboBreak;             // 连击断裂
    
    /// <summary>每帧 Tick（在 Bootstrap.Update 中调用）</summary>
    public void Tick(float dt)
    {
        if (ComboCount > 0)
        {
            _comboTimer -= dt;
            if (_comboTimer <= 0)
            {
                ComboCount = 0;
                OnComboBreak?.Invoke();
                OnComboChanged?.Invoke(0);
            }
        }
    }
    
    /// <summary>
    /// 记录一次击杀得分。由死亡事件回调调用。
    /// </summary>
    public void RecordKill(int baseScore)
    {
        ComboCount++;
        _comboTimer = COMBO_WINDOW;
        
        float multi = Mathf.Min(ComboMultiplier, MAX_COMBO_MULTI);
        int finalScore = Mathf.RoundToInt(baseScore * multi);
        
        CurrentScore += finalScore;
        
        OnScoreChanged?.Invoke(CurrentScore);
        OnComboChanged?.Invoke(ComboCount);
        
        if (CurrentScore > HighScore)
            HighScore = CurrentScore;
    }
    
    public void Reset()
    {
        CurrentScore = 0;
        ComboCount = 0;
        _comboTimer = 0;
    }
}
```

**EntityConfigSO 新增字段**：

```csharp
[Header("击杀奖励（P3B.1）")]
[Tooltip("击杀此 Entity 获得的基础分数（0=不计分）")]
public int KillScore = 10;
```

**EntitySystemBootstrap 集成**：
- 在 `EntityManager.OnEntityDeath` 回调中检查死亡 Entity 的 Camp，如果是敌方则调用 `ScoreManager.RecordKill(config.KillScore)`

**交付清单**：
- [ ] `ScoreManager.cs` 新文件
- [ ] `EntityConfigSO.KillScore` 字段
- [ ] `EntityConfigSOEditor` 绘制
- [ ] `EntitySystemBootstrap` 集成（死亡→计分）
- [ ] Play Mode 验证：击杀 Slime 得分 + 连击加成

---

### P3B.2 道具掉落与拾取系统

**回答的问题**：打了敌人除了得分还有什么好处？

#### 设计哲学

道具 = Entity（走现有的 Entity 系统），但行为特殊：
- 无 AI、无碰撞攻击
- 自身有 MovementComponent（下落/漂浮动画）
- 新增 PickupComponent：当玩家 Entity 进入拾取范围时触发效果

```
敌人死亡 → 掉落判定（DropTableSO） → 生成道具 Entity → 道具下落/磁吸
→ 玩家接近 → PickupComponent 触发 → 执行效果（加分/加血/火力提升/Bomb）
```

#### DropTableSO（掉落表配置）

```csharp
[CreateAssetMenu(menuName = "Entity/DropTable")]
public class DropTableSO : ScriptableObject
{
    [System.Serializable]
    public struct DropEntry
    {
        public EntityConfigSO ItemConfig;  // 道具的 EntityConfig
        [Range(0f, 1f)]
        public float DropChance;           // 掉落概率
    }
    
    public DropEntry[] Entries;
    
    /// <summary>Roll 一次掉落（可能返回 null = 不掉）</summary>
    public EntityConfigSO RollDrop()
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Random.value <= Entries[i].DropChance)
                return Entries[i].ItemConfig;
        }
        return null;
    }
}
```

#### PickupComponent（拾取组件）

```csharp
/// <summary>
/// 拾取物组件——Entity 被玩家接近时触发效果。
/// ComponentType 新增 Pickup = 11。
/// 不走 Tick，由碰撞/距离检测外部驱动。
/// 
/// 设计：
///   - PickupRadius：触发距离
///   - 磁吸模式：当玩家在 MagnetRadius 内时，道具朝玩家加速移动
///   - 效果通过 PickupEffectType 枚举 + EffectValue 配置
/// </summary>
public sealed class PickupComponent : IEntityComponent, ITickable
{
    public ComponentType Type => ComponentType.Pickup; // = 11
    public int TickOrder => TickOrders.Pickup; // 300（最后检测）
    
    private Entity _owner;
    private float _pickupRadius;
    private float _magnetRadius;
    private PickupEffectType _effectType;
    private float _effectValue;
    private bool _isMagneted;
    
    public void Tick(float dt)
    {
        // 1. 查找最近玩家
        // 2. 距离 < magnetRadius → 开启磁吸移动
        // 3. 距离 < pickupRadius → 触发拾取效果 → Despawn 自己
    }
}

public enum PickupEffectType : byte
{
    Score = 0,          // 加分（EffectValue = 分数）
    Health = 1,         // 回血（EffectValue = 回复量）
    FirePowerUp = 2,    // 火力提升（EffectValue = 持续秒数，通过 Buff 实现）
    Shield = 3,         // 护盾（EffectValue = 持续秒数）
    Bomb = 4,           // 全屏清弹/伤害（EffectValue = 伤害值）
    ExtraLife = 5,      // +1 命
}
```

**EntityConfigSO 新增字段**：

```csharp
[Header("掉落表（P3B.2）")]
[Tooltip("击杀此 Entity 时的道具掉落表（null=不掉落）")]
public DropTableSO DropTable;

[Header("拾取物（P3B.2）—— 仅道具 Entity 填写")]
[Tooltip("拾取判定半径")]
public float PickupRadius = 0.5f;
[Tooltip("磁吸半径（0=不磁吸）")]
public float MagnetRadius = 2f;
[Tooltip("拾取效果类型")]
public PickupEffectType PickupEffect;
[Tooltip("效果数值")]
public float PickupEffectValue = 1f;
```

**交付清单**：
- [ ] `DropTableSO.cs` 新文件
- [ ] `PickupComponent.cs` 新文件（ComponentType.Pickup = 11）
- [ ] `PickupEffectType` 枚举
- [ ] `TickOrders.Pickup = 300` 常量
- [ ] `EntityConfigSO` 新增 DropTable / Pickup 字段
- [ ] `EntityConfigSOEditor` 补齐绘制
- [ ] `EntityPool` 组件工厂补充 Pickup case
- [ ] 道具生成逻辑集成到死亡回调
- [ ] 创建模板道具配置：`Template_ScorePickup` / `Template_HealthPickup`
- [ ] Play Mode 验证：击杀敌人 → 掉落 → 拾取 → 效果生效

---

### P3B.3 玩家命数系统

**回答的问题**：死了怎么办？

#### 设计

```
玩家死亡 → 命数-1 → 还有命？→ 重生（无敌 2s + 闪烁） → 继续
                             → 没命了？→ GameOver
```

**LivesManager（纯 C# 服务）**：

```csharp
/// <summary>
/// 玩家命数管理——管理续命/重生/GameOver 判定。
/// </summary>
public class LivesManager
{
    public int CurrentLives { get; private set; }
    public int MaxLives { get; private set; }
    
    // 重生参数
    public float RespawnInvincibleDuration { get; set; } = 2f;
    
    // 事件
    public event System.Action<int> OnLivesChanged;     // 参数：剩余命数
    public event System.Action OnRespawn;               // 重生触发
    public event System.Action OnGameOver;              // 命用完
    
    public void Init(int lives)
    {
        MaxLives = lives;
        CurrentLives = lives;
    }
    
    /// <summary>
    /// 玩家死亡时调用。返回 true=还有命可重生，false=GameOver。
    /// </summary>
    public bool OnPlayerDeath()
    {
        CurrentLives--;
        OnLivesChanged?.Invoke(CurrentLives);
        
        if (CurrentLives > 0)
        {
            OnRespawn?.Invoke();
            return true;
        }
        else
        {
            OnGameOver?.Invoke();
            return false;
        }
    }
    
    public void AddLife(int count = 1)
    {
        CurrentLives = Mathf.Min(CurrentLives + count, MaxLives);
        OnLivesChanged?.Invoke(CurrentLives);
    }
}
```

**关键设计决策**：
- 初始命数：3（微信小游戏标准，通过激励广告可续命）
- 重生无敌：2 秒闪烁（复用 HealthComponent.IsInvincible + 视觉闪烁）
- 重生位置：屏幕底部中央（固定安全区）
- 与 `IWeChatBridge.ShowRewardedAd` 集成：GameOver 时提供"看广告续命"

**交付清单**：
- [ ] `LivesManager.cs` 新文件
- [ ] 重生逻辑：玩家 Entity 死亡 → 重置 HP → 移动到安全位 → 触发无敌
- [ ] `EntitySystemBootstrap` 集成
- [ ] 广告续命预留接口
- [ ] Play Mode 验证：玩家死亡 → 减命 → 重生 → 无敌闪烁

---

### P3B.4 玩家移动边界

**回答的问题**：玩家能在哪里活动？

#### 设计

```csharp
/// <summary>
/// 玩家移动边界约束。在 MovementComponent.Tick 后对玩家 Entity 做 Clamp。
/// 不是独立组件——直接在 ControlComponent 或 Bootstrap 层处理。
/// 
/// 设计决策：用 Rect 定义活动区域，比屏幕边缘稍内缩（留安全边距）。
/// </summary>
```

**实现方式**：`EntitySystemBootstrap` 每帧在所有 Tick 完成后，对 Camp=Player 的 Entity 做 Position Clamp。

```csharp
// EntitySystemBootstrap.cs — 新增
[Header("玩家移动边界（P3B.4）")]
[Tooltip("玩家可活动区域（世界坐标）")]
public Rect PlayerMoveBounds = new Rect(-4.5f, -7f, 9f, 14f);

private void ClampPlayerPositions()
{
    var mgr = EntityManagerAccessor.Instance;
    if (mgr == null) return;
    
    // 遍历所有玩家阵营 Entity，Clamp 位置
    for (int i = 0; i < mgr.ActiveCount; i++)
    {
        var entity = mgr.GetEntityAt(i);
        if (entity.Camp != EnumCamp.Player) continue;
        
        var pos = entity.Position;
        pos.x = Mathf.Clamp(pos.x, PlayerMoveBounds.xMin, PlayerMoveBounds.xMax);
        pos.y = Mathf.Clamp(pos.y, PlayerMoveBounds.yMin, PlayerMoveBounds.yMax);
        entity.Position = pos;
    }
}
```

**交付清单**：
- [ ] `EntitySystemBootstrap.ClampPlayerPositions()` 实现
- [ ] 配置暴露在 Inspector（Rect）
- [ ] 默认值与 DanmakuSystem WorldBounds 对齐
- [ ] Play Mode 验证：玩家到屏幕边缘被阻挡

---

### P3B.5 难度渐进系统（扩展）

**回答的问题**：游戏越往后为什么越刺激？

#### 设计

当前 DifficultyProfileSO 仅影响弹丸参数。飞行射击的难度感知主要来自**敌人数量和频率**。

```csharp
/// <summary>
/// 游戏难度曲线——随时间/波次推进，动态调整生成参数。
/// 纯 C# 服务，由 Bootstrap 每帧驱动。
/// 
/// 设计哲学：
///   难度 = f(存活时间)，不是 f(波次数)。
///   这样即使循环波次也有难度递增感。
/// </summary>
public class DifficultyScaler
{
    // ── 输出参数（供 Spawner/弹幕系统读取）──
    public float SpawnRateMultiplier { get; private set; } = 1f;  // 生成频率倍率
    public float EnemyHpMultiplier { get; private set; } = 1f;    // 敌人血量倍率
    public float EnemySpeedMultiplier { get; private set; } = 1f; // 敌人移速倍率
    public int DifficultyLevel { get; private set; } = 1;         // 当前难度等级
    
    // ── 配置 ──
    private float _elapsedTime;
    private const float LEVEL_INTERVAL = 30f;  // 每 30 秒升 1 级
    private const float SPAWN_RATE_STEP = 0.15f;
    private const float HP_STEP = 0.1f;
    private const float SPEED_STEP = 0.05f;
    private const int MAX_LEVEL = 10;
    
    public void Tick(float dt)
    {
        _elapsedTime += dt;
        int newLevel = Mathf.Min(1 + (int)(_elapsedTime / LEVEL_INTERVAL), MAX_LEVEL);
        
        if (newLevel != DifficultyLevel)
        {
            DifficultyLevel = newLevel;
            SpawnRateMultiplier = 1f + (DifficultyLevel - 1) * SPAWN_RATE_STEP;
            EnemyHpMultiplier = 1f + (DifficultyLevel - 1) * HP_STEP;
            EnemySpeedMultiplier = 1f + (DifficultyLevel - 1) * SPEED_STEP;
        }
    }
    
    public void Reset()
    {
        _elapsedTime = 0;
        DifficultyLevel = 1;
        SpawnRateMultiplier = 1f;
        EnemyHpMultiplier = 1f;
        EnemySpeedMultiplier = 1f;
    }
}
```

**与现有系统集成**：
- `EntitySpawner`：波次间隔时间 ÷ SpawnRateMultiplier
- `EntityManager.Spawn`：生成时 MaxHp × EnemyHpMultiplier
- `DanmakuSystem.Difficulty`：将 DifficultyLevel 映射到 Easy/Normal/Hard Profile

**交付清单**：
- [ ] `DifficultyScaler.cs` 新文件
- [ ] EntitySpawner 集成：间隔时间受 SpawnRateMultiplier 影响
- [ ] Entity 生成时 HP 缩放
- [ ] Bootstrap 每帧驱动
- [ ] Play Mode 验证：存活 60s 后明显感知敌人变多变快

---

### P3B.6 游戏会话管理器

**回答的问题**：怎么开始/暂停/结束一局游戏？

#### 设计

将 `GameStateController`（已有框架）与 Entity 系统串联：

```csharp
/// <summary>
/// 射击游戏会话管理器——串联 Score + Lives + Difficulty + Spawner 的顶层协调者。
/// MonoBehaviour，挂在游戏场景中。
/// 
/// 职责：
///   - 游戏开始：初始化 Score/Lives/Difficulty，启动 Spawner
///   - 游戏暂停：Time.timeScale = 0（弹幕系统已支持 TimeScale）
///   - 玩家死亡：命数判定 → 重生或 GameOver
///   - GameOver：停止 Spawner，弹出结算 UI，提交分数
///   - 重新开始：Reset 所有子系统
/// </summary>
public class ShooterSessionManager : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private int _initialLives = 3;
    [SerializeField] private GameEvent _onGameOver;
    [SerializeField] private GameEvent _onGameStart;
    
    // 子系统引用
    public ScoreManager Score { get; private set; }
    public LivesManager Lives { get; private set; }
    public DifficultyScaler Difficulty { get; private set; }
    
    // 游戏状态
    public bool IsPlaying { get; private set; }
    public float ElapsedTime { get; private set; }
}
```

**交付清单**：
- [ ] `ShooterSessionManager.cs` 新文件
- [ ] 与 `GameStateController` 事件对接
- [ ] 广告续命流程（Show Rewarded Ad → Success → AddLife）
- [ ] 结算流程：GameOver → 最终分数 → SubmitScore → 排行榜
- [ ] Play Mode 验证：完整流程跑通（开始→玩→死→重生→再死→GameOver）

---

## 三、Phase 3 完整验收矩阵

### Phase 3A 验收（12 项，原方案不变）

| # | 测试项 | 通过条件 |
|---|--------|---------|
| 1 | FindEntitiesInRadius | 正确返回范围内指定阵营 Entity |
| 2 | FindNearestEntity | 返回最近的一个 |
| 3 | AutoAimComponent 瞄准 | 自动朝最近目标旋转并射击 |
| 4 | AutoAim + Attack 联动 | 弹幕朝瞄准方向发射 |
| 5 | DamageDealer.DealDamageToEntity | 单体直伤正确扣血 |
| 6 | DamageDealer.DealAreaDamage | 范围内多个 Entity 同时扣血 |
| 7 | SkillComponent CD 管理 | CD 期间不可释放 |
| 8 | SkillComponent 前摇/后摇 | 前摇期间可被打断 |
| 9 | FireBulletsEffect | 通过技能发射弹幕 |
| 10 | AreaDamageEffect | 技能触发 AOE 直伤 |
| 11 | BuffComponent 生命周期 | 挂载→持续→到期移除→效果恢复 |
| 12 | 真机性能 | 20 Entity + 弹幕 ≥ 55fps |

### Phase 3B 验收（10 项，新增）

| # | 测试项 | 通过条件 |
|---|--------|---------|
| 1 | 击杀计分 | 杀敌 → 分数增加 → 事件触发 |
| 2 | 连击系统 | 1.2s 内连杀 → 倍率递增 → 超时断连归 1 |
| 3 | 道具掉落 | 敌人死亡 → 按概率生成道具 Entity |
| 4 | 道具磁吸 | 玩家接近 → 道具加速飞向玩家 |
| 5 | 道具拾取效果 | Score/Health/FirePowerUp 效果正确生效 |
| 6 | 玩家命数 | 初始 3 命，死亡 -1，到 0 → GameOver |
| 7 | 重生无敌 | 重生后 2s 无敌 + 视觉闪烁 |
| 8 | 玩家移动边界 | 玩家不能移出屏幕 |
| 9 | 难度递增 | 存活 30s 后敌人明显增多加速 |
| 10 | 完整会话流程 | 开始→玩→死→重生→再死→GameOver→结算 |

---

## 四、架构决策摘要（含新增）

| 决策 | 选型 | 理由 |
|------|------|------|
| SkillComponent vs AttackComponent | 共存不替代 | 简单 Entity 用 Attack，复杂用 Skill |
| 空间查询算法 | 线性扫描 | 20 Entity 下 O(N) < 0.01ms |
| DamageDealer 设计 | 静态工具类 | 无状态，不占 ComponentType 槽位 |
| Buff 最大数量 | 8 个 | 预分配数组，射击品类够用 |
| FSM 编辑器 | Phase 4 延后 | 当前 AI Action 足够 |
| **道具实现** | **道具 = Entity** | 复用现有 EntityPool/Spawner，零额外架构成本 |
| **ScoreManager 设计** | **纯 C# 服务** | 不绑 MonoBehaviour，易测试，Bootstrap 持有 |
| **LivesManager 设计** | **纯 C# 服务** | 同上 |
| **玩家边界** | **Bootstrap 层 Clamp** | 非组件行为，系统规则，最简实现 |
| **难度渐进** | **时间驱动** | f(存活时间) 比 f(波次) 更平滑一致 |
| **ComponentType 新增** | Pickup=11 | 总共 12 个槽位（0~11），16 上限充裕 |

---

## 五、执行依赖与推荐顺序

```
推荐执行顺序（3B 优先，让游戏先"能玩"）：

Phase 3B（游戏循环闭合）— 预计 2~3 天
  P3B.4 玩家移动边界          ← 0.5h，最简单先做
  P3B.1 击杀计分              ← 依赖死亡事件（已有）
  P3B.2 道具掉落/拾取         ← 依赖 P3B.1（击杀触发掉落）
  P3B.3 玩家命数              ← 依赖死亡事件
  P3B.5 难度渐进              ← 独立，随时可做
  P3B.6 游戏会话管理器        ← 最后串联所有子系统

Phase 3A（战斗能力扩展）— 预计 3~4 天
  P3.1 → P3.2 → P3.3 → P3.4 → 性能验证
```

**关键依赖**：
- P3B.2（道具拾取）中的 FirePowerUp/Shield 效果依赖 P3.4（BuffComponent），可先用简单实现占位，P3.4 完成后补全
- P3B.6 是串联层，所有子系统就绪后再做

---

## 六、ComponentType 槽位规划（更新）

```
0  = Movement    ✅ Phase 1
1  = Animation   ✅ Phase 1
2  = Health      ✅ Phase 1
3  = State       ✅ Phase 1
4  = Attack      ✅ Phase 1
5  = AutoAim     🔜 Phase 3A
6  = Skill       🔜 Phase 3A
7  = Control     ✅ Phase 1
8  = AI          ✅ Phase 1
9  = Collision   ✅ Phase 2
10 = Buff        🔜 Phase 3A
11 = Pickup      🔜 Phase 3B
12~15 = 预留
```

---

**天命人，这就是游戏设计师给的完整处方：Phase 3 不只是"让战斗更丰富"，更要"让游戏能玩成一局完整的体验"。先 3B 后 3A，还是并行，你说了算。** 🎯
