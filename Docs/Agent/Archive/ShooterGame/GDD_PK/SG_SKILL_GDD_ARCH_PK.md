---
title: "SG 技能系统 GDD v1.3 — Unity 架构师 PK 评审"
date: 2026-05-17
pk_type: "攻方 Unity 架构师 vs 守方游戏设计师"
target: SG_SKILL_SYSTEM_GDD.md v1.3
---

# PK 评审：Unity 架构师 vs 游戏设计师

> **PK 对象**：`SG_SKILL_SYSTEM_GDD.md` v1.3  
> **PK 状态**：✅ 已完成（3 轮收敛，GDD v1.4 已回写）  
> **攻方**：Unity 架构师（关注可实现性、SO 架构、框架契合度、性能、SRP）  
> **守方**：游戏设计师（维护设计意图和玩家体验）  
> **上限**：8 回合

---

## Round 1 — 攻方 Unity 架构师：基于现有框架的全面审查

### ARCH-001 | 🔴 高 | SkillComponent 当前只支持 1 个技能——GDD 要求 V2 = 1 技能 + V3 扩 2 个，但 EntityConfigSO.SkillConfig 是单一字段

**涉及章节**：§2.2 / §3.1 技能槽设计  
**涉及代码**：`EntityConfigSO.SkillConfig` (单个 `SkillConfigSO` 引用) + `SkillComponent` (读取 `owner.ConfigSO.SkillConfig`)  

**问题**：  
当前框架 `EntityConfigSO` 只有 **1 个** `SkillConfig` 字段，`SkillComponent` 在 `Init` 时直接读取这个单字段。GDD §3.1 说 V2 只启用 1 个技能槽，但 **V3 预留第 2 个**。

如果 V2 设计不考虑扩展路径，V3 时面临两个选项：
1. 把 `SkillConfig` 改成数组 → **破坏所有现有 SO 资产的序列化**
2. 加第二个 `SkillConfig2` 字段 → **代码臭味**

**建议方向**：GDD 应明确 V2 的技能配置是 **数组化** 还是 **单字段**，以便 TDD 做出正确的架构选择。推荐 V2 就使用 `SkillConfigSO[] SkillConfigs`（长度 1），V3 直接扩展为长度 2——零 SO 迁移成本。

---

### ARCH-002 | 🔴 高 | 被动技能系统（PassiveAbility）没有框架级承载——GDD §4.1 描述了接口但未定义与 Entity-Component 框架的集成点

**涉及章节**：§4.1 被动技能定义与机制  
**涉及代码**：`Entity` 组件系统（`IEntityComponent[16]`）、`ComponentType` 枚举  

**问题**：  
GDD 描述 `PassiveAbility` 接口会 "挂载到 Entity 上"，但：

1. **当前 `ComponentType` 枚举不包含 Passive 类型**——没有可注册的组件槽位
2. GDD 说 "不通过 SkillComponent 驱动"，而是 "IDamageModifier 链 + 标记位"——那 `PassiveAbility` 到底挂在哪里？
   - 如果是新的 `PassiveComponent` → 需要 `ComponentType` 新增枚举值
   - 如果是直接注册到 `HealthComponent._modifiers[]` → `MAX_MODIFIERS = 4`，而 V2 有 4 种被动但只能装 3 个，加上可能的 Buff modifier，**4 槽可能不够**
3. GDD §4.2 PA-04（尾翼反击）= "被敌弹命中后自动发射弹幕"——这不是 IDamageModifier，而是 **事件监听 + 弹幕发射**。当前框架没有提供 "被命中时" 的事件钩子给外部系统订阅

**建议方向**：GDD 应明确 PassiveAbility 的框架集成方案：
- 方案 A：新增 `ComponentType.Passive` + `PassiveComponent`（管理已装备的被动列表，在 Init 时注册各自的 modifier/listener）
- 方案 B：被动作为 Buff 的子集——装备时预添加永久 Buff（Duration = ∞）
- 需要在 GDD 中给出方向，否则 TDD 实现时会面临设计分歧

---

### ARCH-003 | 🔴 高 | BuffComponent 扩展字段过多——R3 风险的具体量化

**涉及章节**：§5.4 BuffConfigSO 扩展设计 / §6.2 状态 DOT  
**涉及代码**：`BuffComponent.BuffSlot[8]` / 风险表 R3  

**问题**：  
当前 `BuffSlot` 是 **struct**，每个包含 `buffId, duration, remaining, moveSpeedMod, attackIntervalMod, damageTakenMod`（6 个字段）。

GDD v1.3 要新增：
- `BuffTag` (4 bytes)
- `StackMode` (4 bytes)
- `MaxStacks` + `CurrentStacks` (8 bytes)
- `StackBonusPerLayer` (4 bytes)
- `IsDot` + `DotDamage` + `DotInterval` + `DotTimer` (16 bytes)
- `VfxPrefab` 引用 (8 bytes, 指针)

原 `BuffSlot` 约 **36 bytes**，扩展后约 **80 bytes**。8 槽 × 80B = **640B/Entity**。
以 V2 性能预算 60 Entity：60 × 640B = **~37.5 KB** 仅 Buff 数据。

这在移动端不是性能问题，但对**缓存行友好性**有影响。更关键的是：**BuffSlot 膨胀后维护成本高**。

**建议方向**：GDD 应考虑 DOT 是否拆分为独立的 `DotSlot[]`（而非嵌入 BuffSlot）。这符合 SRP——Buff 管属性修正，DOT 管持续伤害，职责分离。

---

### ARCH-004 | 🟡 中 | 道具拾取 "每帧检查距离" 与 EntityCollisionSolver 的职责重叠

**涉及章节**：§7.3 道具 Entity 设计  
**涉及代码**：`EntityCollisionSolver` (O(n²) 碰撞扫描)  

**问题**：  
GDD 说道具 "无 Collision——不参与 EntityCollisionSolver"，拾取方式是 "Game 层每帧检查距离"。

但道具已经是 Entity，EntityCollisionSolver **已经在每帧做** O(n²) 圆 vs 圆检测。如果道具参与碰撞系统：
- 零额外代码：设置 `CollisionLayer` 让玩家飞机与道具碰撞 → 拾取
- 利用现有 `ContactDamage` 机制的回调通道（ContactDamage=0 但碰撞仍然被检测）

Game 层额外做一遍距离检查 = **双重遍历浪费**。

**建议方向**：道具拾取应复用 EntityCollisionSolver，而非 Game 层另写一套距离检测。

---

### ARCH-005 | 🟡 中 | AIBehaviorSO 承载 "射手机生命周期状态机"——但当前 AIBehaviorSO 是条件-动作表，没有状态持久化

**涉及章节**：§3.2 射手机完整生命周期  
**涉及代码**：`AIBehaviorSO` / `AIComponent`  

**问题**：  
GDD v1.3 定义了射手机的完整状态机：进入→下落→悬停射击→停火下落→撞基地。

但当前 `AIBehaviorSO` 是**条件-动作表**（无状态），`AIComponent` 每帧评估条件选择动作。要实现射手机生命周期需要：
1. 记住当前阶段（下落中 / 悬停中 / 下落期2）
2. 跟踪悬停已用时间
3. 到 5s 时转换行为（停射击 + 改速度）

这是一个**状态机**，不是条件-动作表能简洁表达的。

**建议方向**：GDD 是否应指定射手机行为用 **小型状态机模式**（AIBehaviorSO 的 States[] 数组），还是用 **MonoBehaviour 覆写**（违反框架）？这影响 TDD 的 AI 子系统设计。

---

### ARCH-006 | 🟡 中 | 同屏射手机 "排队悬停" 缺少实现指导——谁负责追踪全局悬停计数？

**涉及章节**：§3.2 同屏限制（悬停射手机最多 3 架）  
**涉及代码**：无直接对应  

**问题**：  
"排队等前面的进入下落状态后再悬停" 需要一个 **全局协调器**：
1. 追踪当前悬停中的射手机数量
2. 新射手机到达 Y=4 时检查计数——≥3 则继续下落（不悬停）或原地等待

这个全局计数应该放在哪里？
- 方案 A：BattleController 持有 `int _hoverShooterCount`（简单直接但违反组件独立性）
- 方案 B：使用 **RuntimeSet SO**（`TransformRuntimeSet` 模式）追踪悬停射手机
- 方案 C：EntitySpawner 层面做节流

GDD 未指定，TDD 需要决策。

**建议方向**：推荐方案 B——`ShooterHoverSet : RuntimeSet<Entity>` SO 资产。射手机进入悬停时注册，退出时注销。新到 Y=4 的射手机查 `ShooterHoverSet.Items.Count >= 3` 则直接进下落状态。

---

### ARCH-007 | 🟡 中 | 伤害管线 "敌弹命中飞机→基地扣血" 的数据流路径不清

**涉及章节**：§3.3 碰撞规则 / §3.3 飞机挡弹策略  
**涉及代码**：弹幕 `CollisionSolver`（弹丸 vs 目标）、`HealthComponent`  

**问题**：  
GDD 说 "敌弹命中飞机→基地扣血 5"。但弹幕碰撞系统 (`CollisionSolver`) 命中时是对 **被命中的 Entity** 造伤（走 `DamageContext`）。

当前管线：`敌弹命中飞机 → 飞机 HealthComponent.TakeDamage()` → 飞机扣血

但 GDD 说飞机不可被摧毁（无 HP），伤害应转嫁给基地。这需要一个 **伤害转发机制**：
1. 飞机 HealthComponent 不扣自己的血，而是转发给基地 Entity
2. 或者飞机不挂 HealthComponent，在弹幕碰撞回调中直接找基地扣血

方案 1 需要新增 `IDamageModifier`（DamageRedirectModifier：Priority=0，拦截所有伤害→转发给基地）。  
方案 2 需要修改弹幕碰撞回调（跳过常规 TakeDamage 流程）。

GDD 没有指定走哪条路径。

**建议方向**：推荐方案 1——新增 `DamageRedirectModifier : IDamageModifier`，飞机 Init 时注册到自己的 HealthComponent，所有对飞机的伤害转发给基地 Entity。干净、SRP、与现有管线零冲突。

---

### ARCH-008 | 🟢 低 | SkillConfigSO.Effects 的 ISkillEffect 接口——GDD 列出 6 种技能但未定义 Effect 类型清单

**涉及章节**：§3.1 / §9.4 策划 SOP  
**涉及代码**：`SkillComponent` 执行效果阶段  

**问题**：  
GDD §9.4 提到 "在 Effects 列表中添加 ISkillEffect（如 FireBulletsEffect）"，但 GDD 正文未列出 V2 需要哪些 `ISkillEffect` 实现类。

6 种技能需要的 Effect 类型至少包括：
- `FireBulletsEffect`（散射/追踪/激光共用）
- `ApplyBuffToSelfEffect`（火力全开/护盾）
- `DealAreaDamageEffect`（冲击波 AOE）

GDD 应列出这些，TDD 才能估算实现范围。

---

### ARCH-009 | 🟢 低 | §11.4 性能预算 "同屏 Entity 60" 但 V2 新增道具+敌弹 Entity——需重新校算

**涉及章节**：§11.4  

**问题**：  
V2 新增：
- 3 类新敌机（同屏可能 10+）
- 敌弹 Entity（射手机×3悬停 + 散射机×2 = 每 1.5-2.5s 产出 5-8 发）
- 道具 Entity（击杀 30% 掉率，同屏可能 5+）

粗算：V1 同屏 ~40 + V2 新增 15-20 = **55-60 Entity** + **弹丸独立于 Entity（走弹幕池 1024）**

如果敌弹也走 Entity 系统（有 Health？有 Movement？）则 60 可能偏紧。如果敌弹走弹幕系统（NativeArray），则 60 够用。

**建议方向**：GDD 应澄清——敌弹走 Entity 系统还是弹幕系统。推荐走弹幕系统（性能更好、已有碰撞逻辑），Entity 预算只留给飞机+基地+敌机+道具。

---

### ARCH-010 | 🟢 低 | DropTableSO.GuaranteeDrop 标记为 "Boss 必掉"——但 V2 没有 Boss

**涉及章节**：§7.5  

**问题**：  
`GuaranteeDrop` 字段说是 "Boss 必掉"，但 V2 无 Boss。这个字段 V2 用不到，可以留着不影响，但文档中应注明 V2 状态。

---

**核心主张**：GDD v1.3 的设计意图清晰、游戏体验设计扎实。但从**技术实现角度**，有 3 个阻塞级问题必须在 TDD 开工前解决：
1. 被动技能的框架集成方案（ARCH-002）
2. 技能配置的数组化 vs 单字段（ARCH-001）
3. BuffSlot 膨胀与 DOT 职责分离（ARCH-003）

另有 4 个中级问题影响 TDD 的架构选择：
- 道具拾取复用碰撞系统（ARCH-004）
- 射手机状态机模式（ARCH-005）
- 悬停计数全局协调（ARCH-006）
- 伤害转发机制（ARCH-007）

---

## Round 1 — 守方游戏设计师：逐条回应

### RE: ARCH-001 | ✅ 接受 | 技能配置数组化

**判定**：攻方说得对。V2 就用 `SkillConfigSO[]` 数组是零成本预防 V3 迁移痛苦。

**回写承诺**：§3.1 技能槽设计增加技术指导：

> **框架集成指导**（v1.4 PK 新增）：
> - `EntityConfigSO.SkillConfig` 应改为 `SkillConfigSO[] SkillConfigs`（V2 长度=1，V3 扩展为 2）
> - `SkillComponent` 变为 `SkillComponent[]` 或内部管理多技能槽（TickOrder 保持一致）
> - 出战准备界面写入 `SkillConfigs[0]`，V3 启用 `SkillConfigs[1]`

同时在 §十二 新增已确认决策：技能配置从单字段改为数组。

---

### RE: ARCH-002 | ✅ 接受（采纳方案 A 变体） | 被动技能走 PassiveComponent

**判定**：攻方对被动技能的框架集成分析非常到位。三个子问题都是真实的：

1. **ComponentType 缺 Passive**——确实需要新增枚举
2. **IDamageModifier 4 槽可能不够**——3 被动 + Buff modifier 可能撑到 4-5 个
3. **PA-04 尾翼反击不是 modifier**——是事件响应 + 弹幕发射

**采纳方案 A 变体**：新增 `ComponentType.Passive` + `PassiveComponent`

```
PassiveComponent（新组件）
├── PassiveAbilitySO[] _equipped   // 战前装备的被动列表（最多3个）
├── Init(Entity):
│   → 遍历 _equipped
│   → 每个 PassiveAbilitySO.Install(entity)
│     → Modifier 类：注册到 HealthComponent._modifiers
│     → Listener 类：订阅 EntityEventBus 事件
├── Reset():
│   → 每个 PassiveAbilitySO.Uninstall(entity)
```

**关键设计决策**：

| 被动 | 集成方式 |
|------|---------|
| PA-01 穿透 | **标记位**——在弹幕系统 BulletTypeSO 上设 PierceCount（不走 modifier） |
| PA-02 暴击 | **IDamageModifier**——修改 DamageContext.CritRate / CritMultiplier |
| PA-03 磁吸 | **标记位**——修改道具拾取半径（如果走碰撞系统就改 CollisionRadius） |
| PA-04 尾翼 | **事件监听**——订阅 HealthComponent.OnDamageTaken 事件 → 发射弹幕（ICD 1.0s） |

**IDamageModifier 槽位**：建议从 4 扩展到 **6**——3 被动 modifier 位 + 2 Buff modifier 位 + 1 预留。

**回写承诺**：§4.1 增加"框架集成方案"段落，明确每种被动的实现机制。

---

### RE: ARCH-003 | ⚠️ 部分接受 | DOT 拆分为独立系统

**判定**：攻方对 BuffSlot 膨胀的量化分析有说服力。但完全拆分 DOT 有一个设计层面的代价：

**接受的部分**：
- DOT 与 Buff 的**关注点确实不同**：Buff 修改属性，DOT 造持续伤害
- 拆分后 BuffSlot 保持精简，DOT 独立管理

**但需要保留的关联**：
- 燃烧/中毒在设计层面**既是 DOT 又是 Debuff**——燃烧不仅每 0.5s 扣 5 血，还可能附带减速效果
- 如果完全拆分，"清除所有减益"操作需要同时查 BuffSlot[] 和 DotSlot[]

**折中方案**：
```
BuffSlot（保持精简，~40 bytes）：
  buffId, duration, remaining, 
  moveSpeedMod, attackIntervalMod, damageTakenMod,
  tag, stackMode, maxStacks, currentStacks

DotSlot（新增独立数组，~24 bytes）：
  dotId, linkedBuffId,    // 与 Buff 的关联 ID（0=无关联）
  damage, interval, timer, remaining
  
BuffComponent 内部：
  BuffSlot[8] _buffs
  DotSlot[4] _dots        // DOT 通常不超过 3-4 种叠加
  
"清除所有减益"操作：遍历 _buffs + _dots，按 tag 清除
```

**回写承诺**：
- §5.4 BuffConfigSO 拆分为 BuffConfigSO + DotConfigSO
- §6.2 DOT 实现方案更新为独立 DotSlot
- 风险表 R3 更新为"已缓解"

---

### RE: ARCH-004 | ✅ 接受 | 道具拾取走碰撞系统

**判定**：攻方说得对，双重遍历是浪费。

**回写承诺**：
- §7.3 道具 Entity 设计更新：移除"无 Collision"描述，改为：
  - 道具 Entity 启用碰撞（`EnableEntityCollision = true`）
  - 使用独立 `CollisionLayer`（如 Layer=2: 道具层），只与玩家飞机碰撞
  - `ContactDamage = 0`（不造伤），但碰撞检测触发拾取回调
  - 拾取逻辑在 `EntityCollisionSolver.ApplyContactDamage` 扩展：ContactDamage=0 且其中一方是道具 → 触发拾取

**P5 支柱校验**：策划只需配 PickupConfigSO + CollisionRadius（= 拾取半径），无需写代码。被动 PA-03 磁吸 = 放大道具的 CollisionRadius。

---

### RE: ARCH-005 | ✅ 接受 | 射手机行为走小型状态机模式

**判定**：攻方的分析完全正确。条件-动作表无法表达有时序依赖的状态转换。

**回写承诺**：§3.2 增加技术指导：

```
AIBehaviorSO 扩展方案：
├── 当前：ConditionActionTable（无状态，每帧评估）
│   适用：普通机（直线下落）、散射机（蛇形）
│
├── 新增：StateMachineBehavior（有状态，阶段转换）
│   适用：射手机（下落→悬停→下落2）、精英机（悬停→蓄力→冲刺）
│   
│   AIBehaviorSO.BehaviorType : enum { ConditionAction, StateMachine }
│   
│   StateMachineBehavior 数据：
│   ├── AIState[] States（每个 State 有 onEnter/tick/exitCondition）
│   ├── AIComponent 持有 currentStateIndex + stateTimer
│   └── 状态转换条件：timer 超时 / 生命事件 / 位置到达
```

**射手机状态定义**：

| StateIndex | 名称 | OnEnter | Tick | ExitCondition |
|-----------|------|---------|------|---------------|
| 0 | Descending | speed=normal | 向下移动 | Y ≤ 4.0 |
| 1 | Hovering | speed=0, enableShoot | 悬停+射击 | timer ≥ 5.0s |
| 2 | FallingNoShoot | speed=0.7×normal, disableShoot | 向下移动 | Y ≤ 底线 |

---

### RE: ARCH-006 | ✅ 接受 | 悬停计数走 RuntimeSet SO

**判定**：方案 B（RuntimeSet SO）完全符合项目架构原则——SO 不引用场景对象、组件解耦、策划无需碰代码。

**回写承诺**：
- §3.2 新增"悬停协调机制"：使用 `SG_HoverShooterSet : RuntimeSet<Entity>` SO 资产
- 射手机进入 State=Hovering 时 `Set.Add(this)`，退出时 `Set.Remove(this)`
- 新射手机到 Y=4 时检查 `Set.Items.Count >= 3` → 跳过 Hovering 直接进 FallingNoShoot
- SO 资产路径：`Configs/ShooterGame/RuntimeSets/SG_HoverShooterSet`

---

### RE: ARCH-007 | ✅ 接受 | 伤害转发走 DamageRedirectModifier

**判定**：方案 1（DamageRedirectModifier）干净、SRP、零侵入现有管线。

**回写承诺**：§3.3 碰撞规则增加技术实现指导：

```
DamageRedirectModifier : IDamageModifier
├── Priority = 0（最先执行）
├── Target: Entity _baseEntity（基地的引用）
├── ProcessDamage(ref context, target):
│   → 对 _baseEntity.HealthComponent.TakeDamage(adjustedContext)
│   → 调整后 context.FinalDamage = 5（固定值）
│   → return false（中断链——飞机自身不扣血）
```

**Init 时机**：玩家飞机 Entity Init 时，BattleController 注入 `DamageRedirectModifier`（因为基地引用是场景级别的，不能放在 EntityConfigSO 里）。

---

### RE: ARCH-008 | ✅ 接受 | 补充 ISkillEffect 实现类清单

**回写承诺**：§3.1 增加 V2 ISkillEffect 清单：

| ISkillEffect 实现类 | 使用技能 | 职责 |
|--------------------|---------|------|
| `FireBulletsEffect` | SK-P01/P02/P03 | 发射指定 BulletPattern |
| `ApplyBuffToSelfEffect` | SK-P04/P05 | 给自身施加指定 BuffConfigSO |
| `DealAreaDamageEffect` | SK-P06 | 以施法者位置为圆心，走 DamageDealer.DealAreaDamage |

---

### RE: ARCH-009 | ✅ 接受 | 明确敌弹走弹幕系统

**判定**：完全同意。敌弹走弹幕系统（NativeArray）而非 Entity 系统。

**回写承诺**：§3.2 / §11.4 增加：

> **技术约定**：敌弹是弹幕系统的 Bullet（走 BulletPatternSO + BulletWorld NativeArray），**不是** Entity。
> - 弹幕系统已有 EnumCamp → 敌弹 Camp=Enemy → 只命中 Camp=Player 目标
> - 性能预算调整：Entity 60 仅含飞机+基地+敌机+道具 | 弹丸池 1024 含我方弹+敌方弹

---

### RE: ARCH-010 | 🤝 记录 | GuaranteeDrop V2 不用，保留字段

**判定**：纯文档注释，不影响实现。

**回写承诺**：§7.5 `GuaranteeDrop` 字段增加注释：`// V2 不使用，V3 Boss 必掉`

---

**守方 Round 1 小结**：

| 判定 | 数量 | 分布 |
|------|------|------|
| ✅ 接受 | 8 | ARCH-001/002/004/005/006/007/008/009 |
| ⚠️ 部分接受 | 1 | ARCH-003（DOT 独立但保留 Buff 关联） |
| 🤝 记录 | 1 | ARCH-010 |
| ❌ 拒绝 | 0 | — |

**所有 🔴 高严重度问题均已获得承诺回写。**

---

## Round 2 — 攻方 Unity 架构师：对守方回应的追问

### ARCH-011 | 🟡 中 | ARCH-002 追问：PassiveComponent 的 TickOrder——被动需要每帧 Tick 吗？

**涉及章节**：Round 1 RE: ARCH-002  

**质疑**：  
守方说 PassiveComponent 管理被动列表、Install/Uninstall。但被动是 "持续生效的规则增强"——大部分被动（穿透、暴击、磁吸）不需要每帧 Tick，只有 PA-04 尾翼反击有 ICD 倒计时需要 Tick。

如果 PassiveComponent 实现 ITickable，**每帧都执行但 3/4 的被动什么都不做**——这是不必要的性能开销（虽然很小）。

**建议方向**：
- PassiveComponent 不实现 ITickable
- PA-04 等需要计时的被动自己管理 timer——通过 EntityEventBus 订阅 "OnTick" 事件（或在 OnDamageTaken 回调内检查 Time.time 做 ICD）
- 这样 PassiveComponent 只在 Init/Reset 时做注册/注销，零 Tick 开销

---

### ARCH-012 | 🟡 中 | ARCH-004 追问：道具碰撞层——CollisionLayer 当前设计是 "同层才碰撞"，道具需要跨层碰撞

**涉及章节**：Round 1 RE: ARCH-004  

**质疑**：  
当前 `EntityCollisionSolver` 的碰撞层逻辑是：

```csharp
if (!LayerCanCollide(a.ConfigSO.CollisionLayer, b.ConfigSO.CollisionLayer))
    continue;
```

当前规则："CollisionLayer=0 与所有层碰撞，同非零层才碰撞"。

守方方案：道具用 Layer=2，玩家飞机用... 什么 Layer？如果玩家飞机是 Layer=0（默认），确实能碰道具 Layer=2（因为 0 与所有碰撞）。但这也意味着**玩家飞机与所有 Layer 的 Entity 都碰撞**。

这在 V1 没问题（飞机与敌机确实要碰撞）。但需要确认：道具不应与敌机碰撞（敌机不拾取道具）。

**验证**：
- 道具 Layer=2, Camp=Neutral
- 敌机 Layer=0, Camp=Enemy
- `ShouldCollide(Neutral, Enemy)` → 阵营过滤已经排除了这个碰撞对（需确认 EnumCamp 规则）

如果 EnumCamp 过滤逻辑是 "不同阵营才碰撞"：
- Neutral vs Enemy → 不同阵营 → 会碰撞？❌ 不期望
- Neutral vs Player → 不同阵营 → 会碰撞？可能

**建议方向**：道具的阵营应设为什么？需要确认阵营碰撞矩阵能正确过滤出 "道具只与玩家碰撞"。

---

### ARCH-013 | 🟢 低 | ARCH-003 追问：DotSlot.linkedBuffId 的实际使用场景

**涉及章节**：Round 1 RE: ARCH-003  

**质疑**：  
守方提出 `DotSlot.linkedBuffId` 用于 "清除所有减益时同时清除关联 DOT"。但在 V2 设计中：
- 燃烧/中毒/电弧都是敌方技能施加给敌机的——玩家不会被 DOT
- "清除所有减益" 如果存在，也是清除敌机身上的 Debuff（减速/脆弱/致盲）——这些不是 DOT

V2 中 `linkedBuffId` 实际没有使用场景。

**建议方向**：V2 DotSlot 移除 linkedBuffId（YAGNI），V3 如果需要再加。保持 DotSlot 最小化。

---

## Round 2 — 守方游戏设计师：回应

### RE: ARCH-011 | ✅ 接受 | PassiveComponent 不实现 ITickable

**判定**：攻方说得对。大部分被动不需要 Tick，PA-04 的 ICD 可以在事件回调内用时间戳做冷却。

**回写承诺**：
```
PassiveComponent : IEntityComponent（不实现 ITickable）
├── Init: Install 所有被动
├── Reset: Uninstall 所有被动
├── 无 Tick：零帧开销
│
PA-04 尾翼反击 ICD 实现：
├── float _lastTriggerTime = -999f;
├── OnDamageTaken 回调中：
│   if (Time.time - _lastTriggerTime >= 1.0f)
│     → 发射弹幕
│     → _lastTriggerTime = Time.time
```

---

### RE: ARCH-012 | ✅ 接受 | 道具碰撞需要明确阵营规则

**判定**：攻方的质疑有效。需要确认阵营碰撞矩阵。

**澄清与承诺**：

当前 `ShouldCollide` 规则是：**不同阵营才碰撞**。道具的阵营设计：

| 方案 | 道具 Camp | 与 Player 碰撞？ | 与 Enemy 碰撞？ | 分析 |
|------|----------|-----------------|----------------|------|
| A | Neutral | ✅ 不同阵营 | ✅ 不同阵营 | ❌ 敌机会"拾取"道具 |
| B | Player | ❌ 同阵营 | ✅ 不同阵营 | ❌ 玩家不能碰道具 |
| C | **新增 Camp=Pickup** | 需改碰撞矩阵 | 需改碰撞矩阵 | ⚠️ 需要代码改动 |

**最佳方案：沿用 Camp=Player**（道具和玩家同阵营），但**碰撞层做特殊处理**：

```
碰撞层规则扩展：
- Layer=0 (默认): 与所有非零层碰撞
- Layer=1 (道具层): 只与 Layer=0 且 Camp=Player 碰撞

或者更简单：不改阵营碰撞矩阵——道具 Camp=Player + Layer=特殊值，
在 EntityCollisionSolver 碰撞响应阶段判断：
  if (一方是道具 && 另一方是玩家) → 触发拾取
  其他碰撞对中涉及道具 → 跳过

这与当前"阵营不同才碰撞"规则冲突——同 Camp 不碰撞。
```

**最终决策——退回到"Game 层距离检测"但优化为 O(n) 而非 O(n²)**：

考虑到碰撞系统改动成本和潜在 bug 风险，退回简化方案：

```
道具拾取最终方案（v1.4 PK 修正）：
- 道具 Entity 不参与 EntityCollisionSolver（维持 EnableEntityCollision=false）
- Game 层每帧用 O(n) 遍历活跃道具列表（非 O(n²)）：
  for (道具 in 道具池.活跃列表)
    if (距离(玩家, 道具) < 拾取半径) → 拾取
- 道具数量通常 ≤5，O(n) 开销可忽略
- 被动 PA-03 磁吸 = 放大拾取半径
```

**为什么退回**：
1. 碰撞系统的阵营矩阵是为"攻击碰撞"设计的，强行塞入"拾取碰撞"会污染职责
2. 道具同屏 ≤5，O(n) 遍历一遍的开销 < 修改碰撞系统引入的 bug 风险
3. P5 支柱仍然满足：策划只需配 PickupConfigSO，代码已封装在 PickupSystem 中

---

### RE: ARCH-013 | ✅ 接受 | V2 DotSlot 移除 linkedBuffId

**判定**：YAGNI 原则适用。V2 不需要就不加。

**回写承诺**：DotSlot 最终定义：
```
struct DotSlot  // ~20 bytes
{
    int dotId;
    int damage;
    float interval;
    float timer;
    float remaining;
}
```

---

**守方 Round 2 小结**：全部接受，0 分歧。

---

## Round 3 — 攻方 Unity 架构师：最终确认

审查所有承诺回写项：

| ID | 原问题 | 守方承诺 | 攻方确认 |
|----|--------|---------|---------| 
| ARCH-001 | 技能配置单字段 | 改为 SkillConfigSO[] 数组（V2 长度 1） | ✅ 充分 |
| ARCH-002 | 被动无框架承载 | 新增 PassiveComponent + 明确4种被动集成方式 | ✅ 充分 |
| ARCH-003 | BuffSlot 膨胀 | DOT 拆为独立 DotSlot[4] + BuffSlot 保持精简 | ✅ 充分 |
| ARCH-004 | 道具拾取双重遍历 | 退回简化 O(n) 方案（碰撞系统改动风险 > 收益） | ✅ 合理 |
| ARCH-005 | 射手机状态机 | AIBehaviorSO 扩展 StateMachine 模式 | ✅ 充分 |
| ARCH-006 | 悬停计数协调 | RuntimeSet SO 方案 | ✅ 充分 |
| ARCH-007 | 伤害转发路径 | DamageRedirectModifier（Priority=0） | ✅ 充分 |
| ARCH-008 | ISkillEffect 清单 | 3 种 Effect 实现类 | ✅ 充分 |
| ARCH-009 | 敌弹走什么系统 | 走弹幕系统（非 Entity） | ✅ 充分 |
| ARCH-010 | GuaranteeDrop | V3 占位注释 | ✅ 记录 |
| ARCH-011 | PassiveComponent Tick | 不实现 ITickable | ✅ 充分 |
| ARCH-012 | 道具碰撞层 | 退回独立 O(n) 方案 | ✅ 合理 |
| ARCH-013 | DotSlot.linkedBuffId | V2 移除（YAGNI） | ✅ 充分 |

**收敛判定**：
- 连续 2 轮无新增 🔴 高严重度问题
- 所有已识别问题均已获得承诺回写或记录
- 攻守双方无分歧

**🟢 PK 收敛——建议结束评审，进入回写阶段。**

---

## PK 结果总览

| 统计项 | 数量 |
|--------|------|
| 总轮次 | 3（含最终确认） |
| 总问题数 | 13 |
| 🔴 高严重度 | 3 |
| 🟡 中严重度 | 7 |
| 🟢 低严重度 | 3 |
| ✅ 接受 | 11 |
| ⚠️ 部分接受 | 1（ARCH-003，DOT 折中） |
| 🤝 记录 | 1（ARCH-010） |
| ❌ 拒绝 | 0 |

---
