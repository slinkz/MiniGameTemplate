# Entity-Component TDD v2.3 — PK 评审 Round 4

> **攻方**：🎮 游戏设计师（玩法表达力、策划体验、设计灵活性视角）
> **守方**：🏛️ 软件架构师（架构合理性、权衡分析视角）
> **被审文档**：`ENTITY_COMPONENT_TDD.md` v2.3
> **日期**：2026-04-26
> **最大轮次**：3 轮
> **收敛标准**：0 个残余 🔴 + 所有 🟡 有明确方案或已知限制记录

---

## 攻方角色定义

**游戏设计师 Agent**——从"做游戏"的角度审视这份技术设计文档：
- 这套框架能否高效表达常见小游戏玩法需求？
- 策划的日常工作流是否顺畅？配置→测试→迭代的反馈循环够不够快？
- 有没有"技术正确但玩法受限"的设计决策？
- 品类覆盖度如何？塔防、射击、ARPG、跑酷、放置——哪些能做、哪些做不了？
- 玩家体验维度：打击感、节奏控制、视觉反馈、难度曲线调节——框架是否留足了钩子？

**核心审视原则**：技术为玩法服务，不是反过来。如果技术约束限制了设计空间，那就是问题。

---

## Round 1 — 游戏设计师攻方提问

### GD-R4-001 | 严重度 🔴高 | 伤害数值管线缺失——打击感无从落地

**涉及章节**：§4.2, §3.5, §四全局

**质疑**：
整个 TDD 中，从弹丸命中到伤害结算的完整链路是断裂的：
1. `CollisionComponent.OnBulletHit(int damage, int bulletIndex)` 直接收到一个 `int damage`——这个值从哪来？弹幕配置？TypeSO？没交代。
2. **没有伤害计算公式的扩展点**。塔防需要护甲减伤、ARPG 需要暴击/抗性/属性克制、射击需要伤害衰减。当前 `int damage` 直通 `HealthComponent.TakeDamage()`，策划无法插入任何中间逻辑。
3. **没有伤害来源信息传递**。`OnBulletHit` 拿到 `bulletIndex` 但拿不到发射者的 EntityId——做"击杀奖励/经验归属/仇恨系统"全靠猜。
4. P3.3 提到"伤害管理器"但没有任何设计草案——策划现在无法评估 Phase 1 的伤害通路是否足以做出最基本的"打怪有感觉"的反馈循环。

**潜在风险**：策划配数值时发现没地方调伤害公式，所有游戏都变成"固定伤害"——这个框架号称品类通用，但连最基本的伤害管线都没有。

**建议方向**：
- Phase 1 至少定义 `IDamageProcessor` 接口（输入：baseDamage + attackerInfo + defenderInfo → 输出：finalDamage），默认实现直通 `return baseDamage`
- `OnBulletHit` 携带 `DamageContext` struct（包含攻击者信息/弹幕类型/命中部位等），而非裸 `int damage`

**状态**：⏳ 待回应 → 见守方回应

---

### GD-R4-002 | 严重度 🔴高 | Entity 状态机缺乏可视化和策划可编辑的状态转移定义

**涉及章节**：§4.1, BC-07, §五

**质疑**：
1. `StateComponent` 用 `StateMask`（uint64 位标签集合）管理状态——这是程序的做法，不是策划的做法。策划思考的是**状态转移图**：Idle→Walk→Attack→Die，每条转移有条件（HP<0→Die、输入Move→Walk）。
2. TDD 中**没有任何状态转移定义机制**。策划如何配置"在 Stun 状态下不能 Attack"？靠硬编码互斥掩码？那每新增一个状态都得改代码。
3. `AIComponent` 的 `ConditionActionTableStrategy` 是唯一的策划可配置决策方式——但条件-动作表是**平铺的优先级列表**，不是状态图。复杂行为（巡逻→警觉→追击→攻击→撤退）用平铺表很难表达层级和上下文。
4. `EntityConfigSO` 没有"AI 行为配置"字段——策划无法在 SO Inspector 中配 AI。Phase 1 的 AI 要怎么工作？

**潜在风险**：策划想做一个"被打 3 次后进入狂暴状态"的怪物，发现除了写代码没别的路。框架号称"数据驱动"，但行为层全是硬编码。

**建议方向**：
- Phase 1：EntityConfigSO 新增 `AIBehaviorSO` 引用字段（条件-动作表配置资产化）
- Phase 1：状态转移至少用 `StateMachineConfigSO`（状态列表 + 转移条件 + 互斥规则），即便只是最简版
- 明确：策划可以不写代码做出一个"巡逻→追击→攻击→死亡"的完整敌兵行为循环

**状态**：⏳ 待回应

---

### GD-R4-003 | 严重度 🟡中 | 技能系统是黑洞——Phase 3 才做但 Phase 1 玩法全依赖它

**涉及章节**：§4.8, §六 Phase 3

**质疑**：
1. `SkillComponent` 在 Phase 3 才实现，但想一下：**没有技能，Entity 怎么攻击？**
   - 射击游戏：Entity 发射子弹——谁驱动 DanmakuSystem.Fire()？
   - 塔防：塔自动攻击——攻击逻辑挂在哪？
   - ARPG：近战攻击——伤害判定谁触发？
2. Phase 1 的 Demo 场景（P1.11）要求"弹幕交互"——如果 Entity 不能主动发射弹幕，那这个"交互"只能是被动挨打？
3. 策划想在 Phase 1 配一个"每 2 秒发射一颗追踪弹的塔"——**框架支持吗？**

**潜在风险**：Phase 1 验收做出来的 Demo 只有"被打"没有"打回去"——这对验证框架的实际可用性几乎无意义。

**建议方向**：
- Phase 1 至少需要一个**最小攻击机制**：`AttackComponent`（定时发射弹幕，配置：TypeSO 引用 + 攻击间隔 + 发射点偏移）
- 或者把 SkillComponent 的最小子集（单技能槽 + 定时释放）提前到 Phase 1

**状态**：⏳ 待回应

---

### GD-R4-004 | 严重度 🟡中 | 打击感反馈链路不完整——受击闪白是起点不是终点

**涉及章节**：§5.0 EntityConfigSO, §3.15 ViewBridge

**质疑**：
EntityConfigSO 有 `HitFlashDuration` 和 `HitFlashColor`——很好，但打击感远不止闪白：
1. **受击顿帧（HitStop/HitLag）**：被重击时双方冻结 2~4 帧，这是动作游戏打击感的核心。当前没有任何"暂停 Entity Tick"的机制。
2. **击退（Knockback）**：EntityConfigSO 有注释掉的 `KnockbackDistance`——为什么不在 Phase 1 做？被打不动的怪没有打击感。
3. **镜头抖动（ScreenShake）**：击杀 Boss 时的屏幕震动。框架完全没提。
4. **音效触发点**：受击/死亡音效谁触发？ViewBridge 的 SyncAll 只管位置同步。
5. **伤害数字弹出**：DamageNumberSystem 已经存在但 Entity 系统没有桥接它。

策划想做"一个被打会后退、有顿帧、弹出伤害数字、播放受击音效的敌人"——Phase 1 能做到几个？

**潜在风险**：Phase 1 验收出来的 Demo 打击感为零，不如 Phaser.js 随便写的一个小游戏。

**建议方向**：
- Phase 1 至少支持：受击闪白 ✅（已有）+ 击退 + 伤害数字桥接 + 简单音效事件
- 受击顿帧可以 Phase 2，但 Phase 1 要预留 `Entity.PauseFor(int frames)` 接口
- ViewBridge 需要更丰富的事件钩子（OnDamaged/OnDeath），不只是位置同步

**状态**：⏳ 待回应

---

### GD-R4-005 | 严重度 🟡中 | 刷怪系统缺少关卡节奏控制工具

**涉及章节**：§3.14, §十

**质疑**：
当前刷怪系统（SpawnWaveEntry + SpawnGroup）是线性波次推进——Wave0 → Wave1 → ... → 最后一波。这对简单游戏够用，但稍微复杂一点就不行了：

1. **无条件分支**：策划想做"如果玩家 HP > 50%，出精英怪；否则出普通怪"——做不到。
2. **无全局事件触发**：策划想在"第 3 波结束后插入对话/剧情/商店"——WaveTriggerMode 只有 Timer/AllCleared，没有 Callback/Event。
3. **无重复波次/循环**：无限模式（Endless Mode）怎么做？波次配置是有限数组。
4. **无难度缩放**：随着波次递增，怪物数量/属性自动缩放——EntitySpawnWaveSO 里全是硬编码数值。
5. **SpawnGroup 缺 SpawnPosition 策略**：当前是 AreaRadius 内随机散布。策划想做"从屏幕右侧排一列进场"或"围成一圈"——没有阵型选项。

**潜在风险**：刷怪系统只能做最朴素的塔防/射击关卡。跑酷、放置、Roguelike 的关卡节奏全靠写代码。

**建议方向**：
- Phase 1 必做：WaveTriggerMode 新增 `OnCallback`（波次间插入自定义逻辑的钩子）
- Phase 1 建议做：SpawnGroup 新增 `SpawnFormation` 枚举（Random/Line/Circle/Grid）
- Phase 2：难度缩放参数（CountMultiplier, HpMultiplier per wave）
- 无限模式：Spawner 支持 `Loop` 标志位或循环索引

**状态**：⏳ 待回应

---

### GD-R4-006 | 严重度 🟡中 | 品类适配盲区——跑酷和放置类完全没有讨论

**涉及章节**：§一 设计目标, §三~§六 整体

**质疑**：
TDD §一写"复用到塔防、射击、ARPG、跑酷、放置等小游戏类型"，但实际设计几乎只覆盖了**射击/塔防**（弹幕碰撞主导）。我来挑战其他品类：

1. **跑酷**：核心是跳跃/滑行/变道。Entity 的 MovementComponent 只有平面移速——没有重力、没有跳跃高度、没有地面检测。碰撞系统基于 CircleHitbox——跑酷需要的是矩形碰撞+地形碰撞。
2. **放置**：核心是离线收益和自动战斗。Entity 的 Tick 是实时驱动——放置游戏需要"快进模拟"能力（模拟 N 帧的战斗结果而不渲染）。当前 EntityManager.Tick() 和 DanmakuSystem 强耦合——能不能脱离渲染跑逻辑？
3. **Roguelike**：核心是程序生成+道具组合。Entity 配置从 SO 来——Roguelike 需要运行时动态修改属性（+50% 攻击力/获得双重射击）。当前 Entity 属性从配置来、没有 runtime modifier 系统。

**潜在风险**：框架声称品类通用，但实际是"弹幕射击专用框架"。天命人想快速出不同品类的小游戏，发现换品类就得重写。

**建议方向**：
- 至少在 TDD 中为每种声称支持的品类列一个**适配评估表**（能做/需扩展/不支持）
- 识别出哪些是真正"品类无关"的核心（Entity/组件/池化/配置），哪些是"射击特化"的（弹幕碰撞/TargetRegistry）
- 如果某些品类确实需要重大扩展才能支持——把声称的品类列表缩小，别误导未来的自己

**状态**：⏳ 待回应

---

### GD-R4-007 | 严重度 🟢低 | 策划迭代速度——SO 热修改的实际体验比文档描述乐观

**涉及章节**：§十 策划工作流 10.3

**质疑**：
§10.3 写"修改 SO 参数 → 不需要重新进 Play Mode（SO 热修改生效，但已生成的 Entity 需重生效）"。这个描述过于乐观：

1. Entity 从池取出时 `Init()` 读一次 SO 配置——运行时改 SO 的 MaxHp 不会影响已存在的 Entity。策划改了数值还得等怪重新刷出来才能看效果。
2. `EntityPool` 预创建时就根据 SO 的 PoolMax 分配了——运行时改 PoolMax 无效。
3. 状态互斥掩码矩阵"启动时预计算"——运行时改互斥规则不生效。

策划的实际体验是：改参数 → 等这波怪死光 → 等新怪刷出来 → 才看到效果。这比重新进 Play Mode 好一点，但远不是"即时反馈"。

**建议方向**：
- 诚实描述"运行时修改 SO 对已存在 Entity 不生效，需要等新 Entity 从池取出时才应用新配置"
- Phase 2 可选：EntityManager.ReloadConfig(EntityConfigSO) 热刷新 API

**状态**：⏳ 待回应 → 见守方回应

---

### GD-R4-008 | 严重度 🟢低 | 视觉反馈的策划可配置性不足

**涉及章节**：§3.15, §5.0

**质疑**：
EntityConfigSO 中的视觉参数太少——只有 DebugColor 和 ViewPrefab。策划想配的东西远不止这些：

1. **受击特效**：被打时弹出火花/血花——和 DeathEffect 同级但 Phase 1 没给
2. **生成特效**：怪物从地下钻出来/传送出现——没有 SpawnEffect 字段
3. **受击变色方案**：不是所有游戏都用"闪白"——卡通风格用弹性缩放、Q弹感。HitFlashColor 太硬编码
4. **Animation 速度倍率**：加速/减速表现（中毒减速、狂暴加速）——AnimationComponent 没暴露速度倍率

**潜在风险**：策划做出来的每种怪物视觉表现都一样——闪白+死亡特效，千篇一律。

**建议方向**：Phase 1 至少加：SpawnEffect + HitEffect（和 DeathEffect 同级，走 PoolDefinition）。其他的可以 Phase 2 扩展。

**状态**：⏳ 待回应 → 见守方回应

---

## Round 1 — 软件架构师守方回应

### GD-R4-001（🔴高 伤害数值管线缺失）→ ✅ 部分接受

**回应**：

攻方说得对——从弹幕命中到最终扣血的链路确实断了一环。但要分清两个层面：

**已有的部分**：弹幕系统 TypeSO 上已配置了 `Damage` 字段，`CollisionSolver` 在检测到命中时把 `TypeSO.Damage` 传给 `ICollisionTarget.OnBulletHit(int damage, ...)`。所以 "damage 从哪来" 是有答案的——来自弹幕配置。

**确实缺失的部分**：伤害计算的扩展点。当前链路是 `TypeSO.Damage → OnBulletHit → OnCollisionHit → HealthComponent.TakeDamage` 直通，没有中间处理环节。

**方案（分层处理）**：

1. **Phase 1 最小可行**：定义 `DamageContext` struct 替代裸 `int damage`：
   ```csharp
   public struct DamageContext
   {
       public int BaseDamage;          // 弹幕配置的原始伤害
       public EntityId AttackerId;     // 发射者 EntityId（如有）
       public CollisionEventType HitType; // Bullet/Laser/Spray
       // Phase 2 扩展：DamageType (Physical/Magical)、CritMultiplier 等
   }
   ```
   `CollisionComponent` 构造 `DamageContext` 并发布 `OnCollisionHit`（携带 Context），`HealthComponent` 订阅后直接扣 `BaseDamage`。

2. **Phase 1 不做 IDamageProcessor**——理由：
   - 框架定位是"快速出原型"，Phase 1 必须保持简单直通
   - 护甲/暴击/属性克制是**品类特定逻辑**，不应该在通用框架层
   - 游戏层可以订阅 `OnCollisionHit` 并在 `HealthComponent.TakeDamage()` 之前做拦截处理

3. **Phase 2 扩展点**：HealthComponent 增加 `IDamageModifier` 接口列表（减伤/免伤/反弹），游戏层注册自己的 Modifier

**攻击者信息传递**：弹幕系统当前 `BulletCore` 没有 `OwnerId` 字段。Phase 1 新增：
- `BulletCore` 新增 `uint OwnerEntityId` 字段（+4 bytes，56→60 bytes，仍在缓存友好范围）
- `DanmakuSystem.Fire()` API 增加可选 `EntityId owner` 参数
- `CollisionSolver` 把 `OwnerEntityId` 传到 `DamageContext.AttackerId`

**TDD 变更**：§3.5 更新 OnBulletHit 签名 + 新增 DamageContext + §3.13 更新内存预算

**降级判定**：🔴→🟢 Phase 1 有最小可行方案，扩展点 Phase 2 补齐

---

### GD-R4-002（🔴高 状态机缺乏策划可编辑性）→ ✅ 部分接受

**回应**：

这个问题需要拆成两部分看：

**关于 StateMask**：攻方说"策划思考状态转移图"——但这取决于品类。塔防/射击的怪物状态极简（Idle/Move/Attack/Die），平铺的条件-动作表就够了。只有 ARPG/Boss Fight 才需要完整状态机。**框架不应该在 Phase 1 就引入完整 FSM**——过早的抽象比没有抽象更有害。

**确实缺失的部分**：
1. AIComponent 的条件-动作表没有配置资产化——策划确实没法在 Inspector 里配 AI
2. EntityConfigSO 缺少 AI 配置字段

**方案**：

1. **Phase 1 新增 AIBehaviorSO**：
   ```csharp
   [CreateAssetMenu(fileName = "NewAIBehavior", menuName = "Entity/AIBehavior")]
   public class AIBehaviorSO : ScriptableObject
   {
       public AIBehaviorEntry[] Entries;  // 按优先级排列的条件-动作表
   }
   
   [System.Serializable]
   public struct AIBehaviorEntry
   {
       public AIConditionType Condition;   // 枚举：Always/HpBelow/TargetInRange/TargetLost/...
       public float ConditionParam;        // 条件参数
       public AIActionType Action;         // 枚举：Idle/MoveToTarget/Attack/Flee/...
       public float ActionParam;           // 动作参数
   }
   ```

2. **EntityConfigSO 新增字段**：`public AIBehaviorSO AIBehavior;`
   - AIComponent.Init() 从 `owner.ConfigSO.AIBehavior` 读取行为表
   - 策划创建 AIBehaviorSO → 拖入 EntityConfigSO → 即可配出"巡逻→追击→攻击→死亡"循环

3. **关于完整 FSM**：**不在 Phase 1 做**。理由：
   - 条件-动作表 + AIBehaviorSO 已能覆盖"被打 3 次进狂暴"（条件 = HpBelow(0.5), 动作 = EnterState(Berserk)）
   - 完整 FSM 编辑器是 Phase 3 范畴，且可能走 behaviac/行为树而非自制 FSM
   - 过早做 FSM 编辑器是 YAGNI

4. **互斥规则**：Phase 1 硬编码 < 5 条互斥规则足够（Stun 互斥 Attack/Move、Dead 互斥所有）。Phase 2 用 SO 配置化。

**TDD 变更**：新增 AIBehaviorSO 设计 + EntityConfigSO 增加 AIBehavior 字段 + §十 更新策划工作流

**降级判定**：🔴→🟢 核心问题（AI 不可配置）已解决，完整 FSM 是 Phase 3 范畴

---

### GD-R4-003（🟡中 技能系统缺失导致 Phase 1 无攻击能力）→ ✅ 接受

**回应**：

攻方一针见血——**没有攻击能力的 Demo 验证不了框架的实际价值**。

我重新审视了 Phase 1 验收场景（P1.11）："弹幕交互"如果只有被动挨打，确实不够说明问题。

**方案：Phase 1 新增最小攻击组件**

不叫 SkillComponent（那个留给 Phase 3 的完整技能系统），而是引入**最小粒度的 AttackComponent**：

```csharp
/// <summary>
/// Phase 1 最小攻击组件——定时发射弹幕。
/// Phase 3 SkillComponent 上线后，此组件作为"默认普攻"保留或被替代。
/// </summary>
public class AttackComponent : IEntityComponent, ITickable
{
    public ComponentType Type => ComponentType.Skill; // 复用 Skill 槽位
    public int TickOrder => TickOrders.Decision + 50; // Decision 之后、AutoAim 之前
    
    private float _attackInterval;    // 从 EntityConfigSO 读
    private float _timer;
    private VFXTypeSO _bulletType;    // 从 EntityConfigSO 读
    private Vector2 _fireOffset;
    
    public void Init(Entity owner)
    {
        _attackInterval = owner.ConfigSO.AttackInterval;
        _bulletType = owner.ConfigSO.AttackBulletType;
        _fireOffset = owner.ConfigSO.AttackFireOffset;
    }
    
    public void Tick(float dt)
    {
        _timer += dt;
        if (_timer >= _attackInterval)
        {
            _timer -= _attackInterval;
            // 调用 DanmakuSystem.Instance.Fire(...)
            // 方向：朝当前朝向或 AutoAim 锁定目标
        }
    }
}
```

**EntityConfigSO 新增字段**：
```csharp
[Header("攻击（Phase 1 最小集）")]
public float AttackInterval = 1f;           // 攻击间隔（秒）
public VFXTypeSO AttackBulletType;          // 发射的弹幕类型
public Vector2 AttackFireOffset;             // 发射点偏移（相对 Entity 位置）
```

**P1.11 验收更新**：Demo 场景中，敌人配 AI（追击→到达射程→定时发射弹幕）+ 玩家配 ControlComponent（手动发射）→ 双方互射→完整验证。

**TDD 变更**：§四 新增 4.9 AttackComponent + EntityConfigSO 新增攻击字段 + P1.11 AC 更新

**降级判定**：🟡→✅ 已消除

---

### GD-R4-004（🟡中 打击感反馈链路不完整）→ ⚪ 已知限制 + 部分接受

**回应**：

逐项分析权衡：

| 反馈项 | Phase 1 处理 | 理由 |
|--------|------------|------|
| 受击闪白 | ✅ 已有 | — |
| 击退 | ✅ **Phase 1 加入** | 只需 MovementComponent 增加 `ApplyKnockback(Vector2 dir, float dist, float duration)` + EntityConfigSO 解注释 KnockbackDistance |
| 顿帧（HitStop） | ❌ Phase 2 | 需要暂停 Tick 管线 + 对所有组件生效，复杂度不低。Phase 1 EntityConfigSO 预留 `HitStopFrames` 字段（注释掉），实现留 Phase 2 |
| 镜头抖动 | ❌ 框架外 | 这是相机系统的事，不属于 Entity 框架。游戏层订阅 OnDeath 事件触发 ScreenShake |
| 音效 | ✅ **Phase 1 事件钩子** | ViewBridge 不处理音效——音效由游戏层订阅 EntityEventBus 的 OnCollisionHit/OnDeath 事件触发。TDD 补充说明 |
| 伤害数字 | ✅ **Phase 1 桥接** | ViewBridge.OnDamaged 回调中调用 `DamageNumberSystem.Show(position, damage)`。EntityConfigSO 新增 `bool ShowDamageNumber = true` |

**关键原则**：Entity 框架负责发事件，**不负责做表现**。闪白/击退/音效/伤害数字——这些都是事件订阅者的事。框架要做的是确保事件携带足够信息（DamageContext），表现层的策划友好性由游戏层保证。

**TDD 变更**：MovementComponent 新增 Knockback + EntityConfigSO 新增攻击反馈字段 + ViewBridge 事件钩子说明

**降级判定**：🟡→🟢 Phase 1 覆盖闪白+击退+伤害数字+音效事件。顿帧/相机 Phase 2+

---

### GD-R4-005（🟡中 刷怪系统缺少关卡节奏控制）→ ⚪ 已知限制 + 部分接受

**回应**：

攻方列的 5 个缺失我逐一评估：

| 需求 | Phase 1 处理 | 理由 |
|------|------------|------|
| 条件分支（HP 判断出精英） | ❌ Phase 2 | 需要条件表达式解析，Phase 1 不做。游戏层可通过代码在 `OnWaveCleared` 回调中切换下一波配置 |
| 全局事件触发（波次间插对话） | ✅ **新增 OnCallback** | `WaveTriggerMode.OnCallback` = 波次完成后触发事件，等待游戏层调用 `Spawner.ContinueNextWave()` 才推进。零成本加，价值极高 |
| 循环波次（无限模式） | ✅ **新增 Loop 标志** | `EntitySpawnWaveSO` 增加 `bool Loop = false` + `int LoopStartWave = 0`。到达最后一波后从 LoopStartWave 重新开始 |
| 难度缩放 | ⚪ **Phase 1 预留字段** | `SpawnWaveEntry` 增加 `float HpMultiplier = 1f` + `float CountMultiplier = 1f`（注释掉，Phase 2 实现） |
| 生成阵型 | ⚪ **Phase 1 预留枚举** | `SpawnGroup` 增加 `SpawnFormation Formation = Random`（枚举：Random/Line/Circle），Phase 1 只实现 Random |

**TDD 变更**：WaveTriggerMode 新增 OnCallback + EntitySpawnWaveSO 新增 Loop + SpawnGroup 新增 Formation 枚举

**降级判定**：🟡→🟢 核心需求（OnCallback + Loop）Phase 1 做，高级需求（分支/缩放/阵型）Phase 2

---

### GD-R4-006（🟡中 品类适配盲区）→ ⚪ 接受并记录

**回应**：

这是一个**非常好的战略问题**。攻方说得对——TDD 声称品类通用但实际设计偏向弹幕射击。我的回应是：**承认现实，缩小声称，并提供品类适配评估。**

**品类适配评估表**（新增到 TDD）：

| 品类 | 核心适配度 | Entity 框架可用部分 | 需要额外开发的部分 | 评估 |
|------|-----------|-------------------|-------------------|------|
| **弹幕射击** | ⭐⭐⭐⭐⭐ | 全部 | 无 | 完全匹配 |
| **塔防** | ⭐⭐⭐⭐ | Entity/组件/池化/碰撞/刷怪/AI | 塔放置系统、路径规划 | 高度匹配 |
| **ARPG** | ⭐⭐⭐ | Entity/组件/池化/状态 | 技能系统(P3)、装备/属性修改器、完整 FSM | 中度匹配 |
| **跑酷** | ⭐⭐ | Entity/组件/池化 | 重力/跳跃/地面检测、矩形碰撞、地形系统 | 低匹配，需重大扩展 |
| **放置** | ⭐⭐ | Entity/组件/池化/配置 | 离线模拟、快进 Tick、倍速战斗 | 低匹配，需重大扩展 |
| **Roguelike** | ⭐⭐⭐ | Entity/组件/池化/碰撞 | 运行时属性修改器(P2)、程序生成、道具系统 | 中度匹配 |

**修正设计目标措辞**：
- ~~"复用到塔防、射击、ARPG、跑酷、放置等小游戏类型"~~
- → "**核心定位：弹幕射击 + 塔防**。通用部分（Entity/组件/池化/配置驱动）可扩展到 ARPG、Roguelike 等品类；跑酷/放置需要额外的领域特定系统层。"

**关键洞察**：Entity/Component/Pool/Config 是品类无关的**基础设施层**。TargetRegistry/CollisionSolver/DanmakuSystem 是**弹幕领域层**。框架的品类适配度 = 基础设施层 + 是否有对应的领域层。

**TDD 变更**：§一 修正设计目标措辞 + 新增品类适配评估表

**降级判定**：🟡→⚪ 已知限制，诚实记录

---

### GD-R4-007（🟢低 SO 热修改体验过于乐观）→ ✅ 接受

**回应**：

攻方完全正确。§10.3 的描述确实过于乐观。

**修正方案**：
1. §10.3 措辞修正为："运行时修改 SO 参数对**已存在的 Entity 不生效**（它们在 Init 时已读取快照）。新从池中取出的 Entity 会使用新配置。如需即时验证，可重新进入 Play Mode。"
2. Phase 2 可选：`EntityManager.HotReloadConfig(EntityConfigSO config)` —— 遍历所有持有该 config 的活跃 Entity，重新调用每个组件的 `ReloadConfig()`

**TDD 变更**：§十 10.3 措辞修正

**降级判定**：🟢→✅ 已消除

---

### GD-R4-008（🟢低 视觉反馈配置不足）→ ✅ 部分接受

**回应**：

Phase 1 目标是验证框架可行性，不是做出完整的视觉表现系统。但攻方提到的 SpawnEffect 和 HitEffect 确实和 DeathEffect 同级，加上去成本极低：

**Phase 1 接受**：
```csharp
[Header("视觉特效（Phase 1 扩充）")]
public PoolDefinition SpawnEffect;      // 生成特效（可选）
public PoolDefinition HitEffect;        // 受击特效（可选）
public PoolDefinition DeathEffect;      // 死亡特效（已有）
public bool ShowDamageNumber = true;    // 是否显示伤害数字
```

**Phase 2 延迟**：
- 受击变色方案多样化（弹性缩放等）→ Phase 2 ViewBridge 扩展
- Animation 速度倍率 → Phase 2 AnimationComponent 扩展

**TDD 变更**：EntityConfigSO 新增 SpawnEffect + HitEffect + ShowDamageNumber

**降级判定**：🟢→✅ Phase 1 加入 3 个字段，其余 Phase 2

---

## Round 1 收敛评估

| 问题 | 原始严重度 | 回应后状态 | 是否需要 Round 2 追问 |
|------|-----------|-----------|---------------------|
| GD-R4-001 | 🔴高 | 🟢 Phase 1 DamageContext + OwnerEntityId | 可能（伤害扩展点细节） |
| GD-R4-002 | 🔴高 | 🟢 Phase 1 AIBehaviorSO 配置化 | 可能（AI 行为覆盖度） |
| GD-R4-003 | 🟡中 | ✅ 已消除（新增 AttackComponent） | 否 |
| GD-R4-004 | 🟡中 | 🟢 Phase 1 击退+伤害数字+音效事件 | 可能（顿帧/相机的边界） |
| GD-R4-005 | 🟡中 | 🟢 OnCallback + Loop Phase 1 做 | 否 |
| GD-R4-006 | 🟡中 | ⚪ 已知限制（品类评估表） | 可能（评估是否准确） |
| GD-R4-007 | 🟢低 | ✅ 已消除（措辞修正） | 否 |
| GD-R4-008 | 🟢低 | ✅ 已消除（新增 3 字段） | 否 |

**小结**：8 个问题中 3 个已消除，3 个降级为绿/已知限制，2 个可能需要 Round 2 追问。
0 个残余 🔴。进入 Round 2 继续深入。

---

## Round 2 — 游戏设计师攻方追问

### GD-R4-009 | 追问 GD-R4-001 | DamageContext 够了但完整战斗循环缺验证

**追问点**：
守方方案（DamageContext + OwnerEntityId）在数据传递层面解决了问题，但我想确认一个**端到端场景**：

> 策划配一个"近战哥布林"：AI 追击玩家 → 到达攻击范围 → 挥砍（发射 0 距离弹幕 / 或直接伤害）→ 玩家受伤 → 玩家反击 → 哥布林受伤 → 血量归零 → 死亡特效 → 击杀奖励

这个完整循环中，"挥砍"这一步用 AttackComponent（定时发射弹幕）实现——近战攻击也得发射弹幕吗？如果不发射弹幕，CollisionComponent 不会触发 OnBulletHit——那近战伤害走哪条路径？

**要求**：补充"近战攻击"的完整数据流（不走弹幕的情况），或明确"Phase 1 所有攻击必须通过弹幕系统"。

**状态**：⏳ 待回应

---

### GD-R4-010 | 追问 GD-R4-002 | AIBehaviorSO 的条件-动作表能否表达"追到射程再攻击"？

**追问点**：
AIBehaviorSO 用 `AIConditionType` 枚举 + `float Param` 做条件判断。我拿一个最常见的 AI 行为测试：

> "巡逻（无目标时随机移动）→ 发现玩家（范围内检测）→ 追击（移向玩家）→ 到达攻击距离 → 停下攻击 → 目标超出范围 → 回到巡逻"

用条件-动作表怎么配？

```
Priority 0: Condition=TargetInRange(3.0), Action=Attack
Priority 1: Condition=TargetInRange(8.0), Action=MoveToTarget
Priority 2: Condition=TargetLost, Action=Patrol
Priority 3: Condition=Always, Action=Idle
```

问题来了：
1. **Attack 和 MoveToTarget 同时满足**（目标在 3.0 范围内，也在 8.0 范围内）。靠优先级解决——OK，但策划要理解"优先级越小越先检查"这个隐式规则。
2. **Patrol 怎么实现？** AIActionType.Patrol 意味着"随机选一个点 → 移向该点 → 到达后等几秒 → 再选新点"。这不是单帧决策——条件-动作表是每帧重新评估的，Patrol 需要在多帧间保持上下文（目标巡逻点）。当前设计能处理**有状态的 Action** 吗？

**要求**：说明条件-动作表如何处理多帧有状态 Action，或承认需要最小状态保持机制。

**状态**：⏳ 待回应

---

### GD-R4-011 | 追问 GD-R4-004 | Entity.PauseFor 预留是否影响 Tick 管线设计？

**追问点**：
守方说顿帧 Phase 2 做，Phase 1 预留字段。但 `Entity.PauseFor(int frames)` 如果要在 Phase 2 加入，会影响 EntityManager.Tick() 的核心循环——暂停的 Entity 要跳过 Tick。

这意味着 Phase 1 的 `EntityManager.Tick()` 循环需要预留 `if (entity.IsPaused) continue;` 检查吗？还是 Phase 2 再改 Tick 循环？

如果 Phase 2 再改，改动会穿透核心循环，这不就是攻方 R3 SA-001 那种"地基级变更"吗？

**要求**：明确 Phase 1 是否在 Tick 循环中预留 pause 检查（cost = 1 个 bool 判断/帧/entity，几乎零开销），还是 Phase 2 承担改 Tick 循环的风险。

**状态**：⏳ 待回应

---

## Round 2 — 软件架构师守方回应

### GD-R4-009（近战攻击路径）→ ✅ 明确

**回应**：

好问题。两种路径都支持，策划自选：

**路径 A：近战也走弹幕（推荐，Phase 1 默认）**
- 配一个"近战弹幕 TypeSO"：射程=0.5（几乎贴身）、速度=0（瞬发）、存活时间=0.1s（1~2 帧）、Damage=配置值
- AttackComponent 发射这种弹幕 → CollisionSolver 检测命中 → 正常走 OnBulletHit → DamageContext 链路
- **好处**：整个伤害管线统一，不用分支。策划只需配弹幕参数，"近战"和"远程"的区别只是 TypeSO 参数不同
- **坏处**：近战攻击消耗 BulletWorld 槽位（1 帧即回收，实际无压力）

**路径 B：直接伤害（Phase 2 可选）**
- AttackComponent 在攻击时直接调用目标 Entity 的 `HealthComponent.TakeDamage(DamageContext)`——需要先查找射程内目标
- 需要 `EntityManager.FindEntitiesInRadius()`——这是 Phase 3 API
- **结论：Phase 1 不用这条路径**

**Phase 1 明确**：**所有攻击都通过弹幕系统**。近战 = 射程极短的瞬发弹幕。这是最简路径，框架复用度最高。

**TDD 变更**：§4.9 AttackComponent 补充"近战弹幕"配置说明

**降级判定**：✅ 已明确

---

### GD-R4-010（条件-动作表的有状态 Action）→ ✅ 接受并补充

**回应**：

攻方击中了条件-动作表的核心弱点——**它适合无状态决策，不适合有状态行为**。

Patrol 确实需要多帧上下文：当前巡逻目标点、是否正在移动中、到达后等待计时。每帧重新评估条件表不会丢失这些状态，因为条件表只决定"该做什么"，**怎么做由 Action 执行器内部维护状态**。

**方案：Action 执行器有状态化**

```csharp
/// <summary>
/// AI Action 执行器接口——支持多帧有状态执行。
/// 每帧由 AIComponent 调用 Execute()，Action 内部维护自身状态。
/// </summary>
public interface IAIAction
{
    void Enter(Entity owner);           // 进入此 Action 时调用
    DecisionCommand Execute(Entity owner, float dt); // 每帧执行
    void Exit(Entity owner);            // 退出此 Action 时调用
}

// Patrol 实现示例
public class PatrolAction : IAIAction
{
    private Vector2 _patrolTarget;
    private float _waitTimer;
    private bool _isWaiting;
    
    public void Enter(Entity owner)
    {
        _patrolTarget = PickRandomPoint(owner.Position, patrolRadius);
        _isWaiting = false;
    }
    
    public DecisionCommand Execute(Entity owner, float dt)
    {
        if (_isWaiting) { _waitTimer -= dt; if (_waitTimer <= 0) _patrolTarget = PickRandomPoint(...); _isWaiting = false; }
        if (ReachedTarget(owner.Position, _patrolTarget)) { _isWaiting = true; _waitTimer = 2f; return DecisionCommand.Idle; }
        return new DecisionCommand { MoveDir = (_patrolTarget - owner.Position).normalized };
    }
    
    public void Exit(Entity owner) { }
}
```

**AIComponent 的执行流程**：
1. 每帧评估条件-动作表 → 得到当前应执行的 ActionType
2. 如果 ActionType 与上一帧不同 → 调用旧 Action.Exit() + 新 Action.Enter()
3. 调用当前 Action.Execute() → 得到 DecisionCommand

**策划视角**：
- 条件-动作表（AIBehaviorSO）决定"什么时候做什么"——策划在 Inspector 里配
- Action 执行器决定"怎么做"——程序实现，策划不需要碰

**Phase 1 内置 Action**：Idle、MoveToTarget、Patrol、Attack、Flee（5 个覆盖基本 AI 行为）

**TDD 变更**：§4.7 补充 IAIAction 接口 + AIComponent 执行流程 + 内置 Action 列表

**降级判定**：✅ 已消除

---

### GD-R4-011（PauseFor 预留是否影响 Tick 管线）→ ✅ 接受预留

**回应**：

攻方说得对——Phase 2 改核心 Tick 循环确实有风险。而预留一个 bool 判断的成本几乎为零。

**方案：Phase 1 预留 pause 检查**

```csharp
// EntityManager.Tick()
for (int i = 0; i < _activeEntities.Count; i++)
{
    var entity = _activeEntities[i];
    if (entity.IsPaused) { entity.DecrementPauseFrames(); continue; } // Phase 1 预留，永不触发
    entity.Tick(dt);
}
```

```csharp
// Entity 类
public bool IsPaused => _pauseFrames > 0;
private int _pauseFrames;
public void PauseFor(int frames) => _pauseFrames = frames;
internal void DecrementPauseFrames() { if (_pauseFrames > 0) _pauseFrames--; }
```

Phase 1 不调用 `PauseFor()`，所以 `IsPaused` 永远 false——分支预测器会学到这个模式，性能影响为零。Phase 2 启用顿帧时，不需要改 Tick 循环。

**TDD 变更**：§3.7 EntityManager.Tick 新增 IsPaused 检查 + Entity 新增 PauseFor/IsPaused

**降级判定**：✅ 已消除

---

## Round 2 收敛评估

| 问题 | Round 1 状态 | Round 2 状态 | 是否需要 Round 3 |
|------|-------------|-------------|-----------------|
| GD-R4-001 | 🟢 | ✅ 近战走弹幕，路径统一 | 否 |
| GD-R4-002 | 🟢 | ✅ IAIAction 有状态化 | 否 |
| GD-R4-003 | ✅ | ✅ | 否 |
| GD-R4-004 | 🟢 | ✅ Tick 预留 pause 检查 | 否 |
| GD-R4-005 | 🟢 | 🟢 （R1 已确定方案）| 否 |
| GD-R4-006 | ⚪ | ⚪ （R1 已确定方案）| 否 |
| GD-R4-007 | ✅ | ✅ | 否 |
| GD-R4-008 | ✅ | ✅ | 否 |

**收敛判定**：Round 2 追问的 3 个问题全部解决。总计 8+3=11 个问题，6 个消除 + 2 个降为🟢有方案 + 2 个已知限制 + 1 个 Round 1 直接消除。

**0 个残余 🔴，0 个无方案 🟡。无需 Round 3。**

PK R4 在 **2 轮**内收敛。

---

## 结论

**PK R4（游戏设计师 vs 软件架构师）2 轮收敛，11 个问题全部解决。**

### TDD v2.3 → v2.4 变更清单

| # | 变更 | 涉及章节 | 来源 |
|---|------|---------|------|
| 1 | 新增 DamageContext struct + BulletCore.OwnerEntityId | §3.5, §3.13 | GD-R4-001 |
| 2 | 新增 AIBehaviorSO + AIBehaviorEntry 配置资产 | §4.7, §5.0, §十 | GD-R4-002 |
| 3 | 新增 IAIAction 接口 + 5 个内置 Action（Idle/MoveToTarget/Patrol/Attack/Flee） | §4.7 | GD-R4-010 |
| 4 | 新增 AttackComponent（Phase 1 最小攻击组件） | §4.9(新增), §5.0, §六 P1.7 | GD-R4-003 |
| 5 | MovementComponent 新增 Knockback 支持 | §4.4 | GD-R4-004 |
| 6 | Entity 新增 PauseFor/IsPaused + Tick 循环预留 pause 检查 | §3.2, §3.7 | GD-R4-011 |
| 7 | EntityConfigSO 新增：AttackInterval/AttackBulletType/AttackFireOffset/AIBehavior/KnockbackDistance/SpawnEffect/HitEffect/ShowDamageNumber | §5.0 | GD-R4-003/004/008 |
| 8 | WaveTriggerMode 新增 OnCallback + EntitySpawnWaveSO 新增 Loop | §3.14 | GD-R4-005 |
| 9 | SpawnGroup 新增 Formation 枚举（Phase 1 只实现 Random） | §3.14 | GD-R4-005 |
| 10 | §一 设计目标修正措辞 + 新增品类适配评估表 | §一(新增) | GD-R4-006 |
| 11 | §十 10.3 策划工作流措辞修正（SO 热修改限制说明） | §十 | GD-R4-007 |
| 12 | ViewBridge 事件钩子说明（音效/伤害数字由游戏层订阅） | §3.15 | GD-R4-004 |
| 13 | 近战攻击路径说明（Phase 1 统一走弹幕） | §4.9 | GD-R4-009 |

### 对 Phase 1 实施计划的影响

- **P1.0**（已有）：阵营枚举迁移
- **P1.7 扩展**：ControlComponent + AIComponent + **AttackComponent**（三合一步骤）
- **P1.8 扩展**：EntityConfigSO 验证需覆盖新增字段（AIBehavior/Attack/Effect）
- **P1.10 扩展**：刷怪系统验证需覆盖 OnCallback + Loop
- **P1.11 更新**：Demo 场景需验证双向攻击（敌人发射弹幕 + 玩家反击 + 击退 + 伤害数字）

### 四轮 PK 总量

| 轮次 | 攻方 | 守方 | 问题数 | 轮数 |
|------|------|------|--------|------|
| R1 | Unity 架构师 | Unity 架构师 | 17 | 2 |
| R2 | 策划工作流 | Unity 架构师 | 12 | 2 |
| R3 | 软件架构师 | Unity 架构师 | 7 | 1 |
| R4 | 游戏设计师 | 软件架构师 | 11 | 2 |
| **总计** | | | **47** | **7** |

**下一步**：将上述 13 项变更落地到 TDD v2.4，待天命人审批后启动 Phase 1 编码。

