# PK 评审记录 — SG_V2_TDD（全 5 Sprint）

> **目标文档**：SG_V2_TDD_01~05（全部 5 个 Sprint TDD）
> **文档类型**：TDD
> **攻方角色**：Unity 架构师（10 年+ Unity 引擎开发经验，专精 WebGL 平台限制、弹幕系统、ECS-like 纯 C# 框架）
> **守方角色**：软件架构师（专精系统设计、API 设计、可维护性和关注点分离）
> **开始时间**：2026-05-18 16:30
> **PK 状态**：✅ 已完成
> **最大轮次**：8

---

## PK Round 1 — 攻方提问

### UA-001 | 严重度 🔴高 | BulletWorld 无 SpawnPattern 方法——TDD_01 接口假设不存在
**涉及章节**：TDD_01 §S1.2 Fire()
**质疑**：TDD_01 中 EnemyShootComponent.Fire() 调用 `BulletWorld.SpawnPattern(_pattern, entity.Position, Vector2.down, _camp)`，但代码审查发现 BulletWorld 类**没有 SpawnPattern 方法**。BulletWorld 是纯数据容器（SoA 数组+空闲栈），发射逻辑在 `DanmakuSystem.API.cs` 的 `FireBullets(BulletPatternSO, origin, baseAngle, ownerEntityId)` 中。
**潜在风险**：直接按 TDD 编码会编译失败。且正确的 API 签名不接受 Camp 参数和 Direction 参数——Camp 从 BulletPatternSO 或 BulletCore.Faction 读取，方向用 float baseAngle（弧度）而非 Vector2。
**建议方向**：修正为 `DanmakuSystem.Instance.FireBullets(_pattern, entity.Position, Mathf.PI, ownerEntityId: entity.Id)`（π = 向下）。同时说明 Camp 由 BulletPatternSO 内含 Faction 字段决定还是由 BulletSpawner 从 ownerEntityId 推导。
**状态**：🟡 待回应

---

### UA-002 | 严重度 🔴高 | BuffComponent 容量 8 vs TDD 假设 16——Buff+DOT+被动可能溢出
**涉及章节**：TDD_03 §S3.1
**质疑**：TDD_03 写"BuffSlot[16] _buffs"+"DotSlot[16] _dots"，但实际代码 `BuffComponent` 使用 `MAX_BUFFS = 8`。TDD_03 还在 BuffSlot 中新增了 Tag/StackMode/CurrentStacks/MaxStacks/BulletCountModifier/VfxInstanceId 共 6 个字段。当前 BuffSlot 只有 6 个字段（BuffId/Duration/RemainingTime + 3 个 Mod），扩展后变 12 个字段。

V2 系统中玩家可同时拥有：4 增益 Buff + 3 被动 Buff + 火力全开 Buff + 弹药 Buff = 最少 9 个 Buff。8 槽不够。

**潜在风险**：(1) 直接改为 16 会使 BuffSlot struct 膨胀（当前 6×4B=24B，扩展后 12×4B=48B，×16=768B），影响缓存行效率；(2) 如果不改容量，战斗中 Buff 会被丢弃；(3) DOT 独立数组需要明确 struct 大小和内存预算。
**建议方向**：明确 V2 的 MAX_BUFFS 目标值（建议 12 或 16），并评估内存影响。DOT 独立数组的 struct 也需要定义。考虑是否需要 struct 拆分（热数据/冷数据分离）。
**状态**：🟡 待回应

---

### UA-003 | 严重度 🔴高 | NativeArray 在项目中未使用——TDD_04 sourceTag 方案需重设计
**涉及章节**：TDD_04 §S4.3
**质疑**：TDD_04 写"BulletWorld NativeArray 新增字段 NativeArray<int> _sourceTags"，但代码审查发现**项目业务代码中完全没有使用 NativeArray**。BulletWorld 使用托管数组 `BulletCore[]`，不使用 Unity Jobs/Burst。引入 NativeArray 需要：(1) 生命周期管理（Dispose）；(2) 与托管数组混用的注意事项；(3) WebGL 平台兼容性验证。

更重要的是：如果只是加一个 int 数组，直接用 `int[] _sourceTags = new int[capacity]` 即可，与 BulletCore[] 同构，无需引入 NativeArray 的复杂性。

**潜在风险**：引入 NativeArray 违反项目现有的零外部依赖原则，且在 WebGL 上可能有问题（Unity WebGL 对 NativeArray 支持有限制）。
**建议方向**：改用普通托管数组 `int[] _sourceTags`，与 BulletWorld 现有风格一致。
**状态**：🟡 待回应

---

### UA-004 | 严重度 🟡中 | ComponentType 枚举扩展余量不足——V2 需要 3 个新组件但只有 5 个空位
**涉及章节**：TDD_01 §S1.2, TDD_02 §S2.6, TDD_03 §S3.5
**质疑**：ComponentType enum 当前有 11 个有效值（0~10），MAX=16 是数组容量上限，预留 5 位（11~15）。V2 需要新增：`EnemyShoot`(11) + `Passive`(12) + 还有一个隐含的 `Invincibility`(?)——但 TDD_01 的 InvincibilityComponent 是否需要 ComponentType 注册？

如果 InvincibilityComponent 需要 ComponentType（TDD 说"ComponentType 枚举新增"），那 V2 至少需要 3 个位。留 2 个给 V3。但 TDD 没有说明 InvincibilityComponent 的 ComponentType 注册——它是 IDamageModifier，不走 ComponentType。

**潜在风险**：如果后续 V3 需要更多组件（如 Pickup、Quest 等），MAX=16 可能不够。但这是 V3 的事。
**建议方向**：(1) 明确 InvincibilityComponent 不需要 ComponentType（它是 Modifier，不是 IEntityComponent）；(2) 确认 EnemyShoot=11, Passive=12 的分配；(3) 记录 MAX=16 的扩容策略（V3 如需超过 16，改为 32 或动态数组）。
**状态**：🟡 待回应

---

### UA-005 | 严重度 🟡中 | EntityConfigSO.SkillConfig 是单个 SO——但 V2 需要 6 技能槽
**涉及章节**：TDD_02 §S2.6
**质疑**：现有 EntityConfigSO 只有一个 `SkillConfig` 字段（单个 SkillConfigSO）。V2 需要玩家飞机支持 6 技能槽——数据来源是战前装备选择（BattleLevelData.EquippedSkills[]），而不是 EntityConfigSO。

TDD_02 中 SkillComponent.Init 接收 `SkillConfigSO[] equipped`，这说明技能列表不从 EntityConfigSO 读取，而是从外部注入。但 TDD_01 的 EnemyShootComponent 从 `entity.Config.ShootPattern` 读取——这个字段不存在（EntityConfigSO 没有 ShootPattern 字段）。

**潜在风险**：TDD_01 假设 EntityConfigSO 有 ShootPattern/ShootCooldown/FirstFireDelay 字段，但实际代码没有。需要确认这些字段是 V2 新增还是复用现有字段（如 AttackBulletPattern + AttackInterval？）。
**建议方向**：明确 EnemyShootComponent 的数据源：(1) 复用 EntityConfigSO.AttackBulletPattern + AttackInterval，还是 (2) 新增 ShootPattern/ShootCooldown/FirstFireDelay 字段。我倾向复用方案——敌机射击本质就是"攻击"。
**状态**：🟡 待回应

---

### UA-006 | 严重度 🟡中 | 弹幕碰撞系统：弹丸命中 Entity 的桥接缺失
**涉及章节**：TDD_01 §S1.3
**质疑**：TDD_01 说"弹幕系统已有阵营碰撞矩阵"——代码确认是 `CollisionSolver.ShouldCollide()` 硬编码规则（非可配置矩阵，但功能等效）。关键问题是**弹丸命中 Entity 后如何桥接到 IDamageModifier 链**。

当前弹幕碰撞回调路径是：`CollisionSolver → target.OnBulletHit(damage, bulletIndex)`。这个 `ICollisionTarget.OnBulletHit` 回调在飞机/基地侧是怎么实现的？需要一个适配器将弹幕碰撞转为 `HealthComponent.TakeDamage(ref DamageContext)` 调用，才能进入 IDamageModifier 链。

TDD_01 说"需确认弹丸碰撞回调走 DamageDealer.DealDamage(target, context)"——这个确认还没做。实际代码中 `ICollisionTarget.OnBulletHit` 的签名是 `void OnBulletHit(int damage, int bulletIndex)`，不走 DamageContext。

**潜在风险**：如果不改造 OnBulletHit 回调路径，InvincibilityModifier 和 DamageRedirectModifier 都不会生效——敌弹命中飞机会直接扣飞机血而不转发给基地。
**建议方向**：需要一个 `PlayerCollisionTarget` 适配器（或修改现有的）在 OnBulletHit 中构造 DamageContext 并调用 `HealthComponent.TakeDamage(ref ctx)`，从而走 IDamageModifier 链。明确这个适配器的实现位置。
**状态**：🟡 待回应

---

### UA-007 | 严重度 🟡中 | 攻速钳制下限矛盾：TDD_03 说 0.05s，现有代码 Clamp 范围是 0.3~3.0
**涉及章节**：TDD_03 §S3.1
**质疑**：TDD_03 说"AttackInterval 乘积下限钳制 ≥ 0.05s"，但现有 BuffComponent 代码中 `MIN_ATTACK_INTERVAL_RATIO = 0.3f`（即最快只能到正常 30% 的攻击间隔）。如果基础攻击间隔是 0.25s（4 发/s），30% = 0.075s（13.3 发/s）——已经非常快了。

TDD_03 的 0.05s 钳制是绝对值还是比率？如果是比率（AttackInterval × 0.05），那基础 0.25s × 0.05 = 0.0125s = 80 发/s，这会导致弹幕系统爆满。

**潜在风险**：攻速钳制逻辑不清晰可能导致弹幕爆满崩溃（2048 上限很快被填满）。
**建议方向**：明确"0.05s"是 AttackIntervalModifier 的最小比率值还是最终 AttackInterval 的最小绝对值。建议保留现有 0.3f 的比率钳制（即使 buff 叠满也不会低于原始攻速的 30%）。
**状态**：🟡 待回应

---

### UA-008 | 严重度 🟢低 | 通关判定未等弹幕清场——与 GDD 设计矛盾
**涉及章节**：TDD_04 §S4.3
**质疑**：TDD_04 说 `OnBattleEnd` 触发条件是"EntitySpawner.IsAllWavesCleared = true AND BulletWorld 中 Camp=Enemy 弹丸数 = 0"。但现有 BattleController.TickPlaying 中通关判定只检查"IsAllWavesCleared + 无存活 Enemy Entity"，**不检查 Camp=Enemy 弹丸清场**。

这意味着需要新增弹幕清场检测逻辑。BulletWorld 是 SoA 数组需要遍历 `BulletCore[]` 找 Camp=Enemy 的活跃弹丸——性能影响需评估（2048 弹丸遍历一次 ~2048 次 if 判断）。

**潜在风险**：如果只在通关帧检查一次，性能无问题。但如果每帧都检查"是否所有 Enemy 死了 AND 弹幕清场"，遍历成本在低端机上可能可见。
**建议方向**：只在 `hasAliveEnemy=false` 之后再检查弹丕清场（不是每帧都遍历 BulletWorld），做成两阶段判定：(1) 无存活敌机 → (2) 等弹幕清场 → 触发 OnBattleEnd。这样弹丸遍历只在"等待清场"阶段执行。
**状态**：🟡 待回应

---

### UA-009 | 严重度 🟢低 | PassiveComponent 的 PA-04 事件订阅时机未明确
**涉及章节**：TDD_03 §S3.5
**质疑**：PA-04 尾翼反击需要订阅 `EntityEventBus.OnCollisionEvent`，在"碰撞事件级触发——先于 IDamageModifier 链"。但 EntityEventBus 是 Entity 框架的事件系统，其事件是什么时候触发的？

弹丸碰撞走的是弹幕系统的 `ICollisionTarget.OnBulletHit` 回调，不走 EntityEventBus。如果要让 PA-04 在弹丸命中时触发，需要在 PlayerCollisionTarget 适配器中同时 fire EntityEventBus 事件。

**潜在风险**：事件路径不通：弹幕碰撞 → ICollisionTarget → 但 EntityEventBus 由 Entity 系统管理，两个系统的事件不自动桥接。
**建议方向**：在 UA-006 的 PlayerCollisionTarget 适配器中，在调用 TakeDamage 之前先 fire EntityEventBus.OnCollisionEvent，让 PassiveComponent 能收到碰撞通知。
**状态**：🟡 待回应

---

### UA-010 | 严重度 🟢低 | EditorBulletSimulator 的 Editor 模式限制未提及 SceneView 刷新
**涉及章节**：TDD_05 §S5.1
**质疑**：Editor 模式弹幕预览需要 SceneView 持续刷新（默认 SceneView 只在交互时刷新）。需要 `SceneView.RepaintAll()` 或 `EditorApplication.QueuePlayerLoopUpdate()` 来驱动模拟 Tick。这是 Editor 工具常见坑点。
**潜在风险**：不调用 Repaint，预览弹丸会"卡住"只有鼠标移动时才更新。
**建议方向**：在 EditorBulletSimulator 的 Update 回调中调用 `SceneView.RepaintAll()`，并在关闭预览时停止刷新（避免浪费 CPU）。
**状态**：🟡 待回应

---

> **PK Round 1 攻方总结**：
> - 🔴 高优 3 个（UA-001/002/003）——阻塞编码，不解决无法开始
> - 🟡 中优 4 个（UA-004/005/006/007）——不阻塞但可能导致返工
> - 🟢 低优 3 个（UA-008/009/010）——改善建议，可编码期间解决
>
> 文档质量整体很高，Sprint 分解清晰，验收方案完整。主要问题集中在 **TDD 伪代码与实际 API 不匹配**——这些在编码时如果不先修正会浪费大量调试时间。

---

## PK Round 1 — 守方回应

### RE: UA-001 | ✅ 接受 | Fire() 改用 DanmakuSystem.Instance.FireBullets
BulletWorld 是纯数据容器无 SpawnPattern。正确 API：`DanmakuSystem.Instance.FireBullets(_pattern, entity.Position, -90f, entity.Id.Value)`。baseAngle 用角度制（FireBulletsEffect 中 Rad2Deg 确认）。Camp 由 BulletPatternSO.Faction 配置，不需运行时传参。
**回写**：TDD_01 §S1.2 伪代码已修正。

---

### RE: UA-002 | ✅ 接受 | MAX_BUFFS 扩至 12 + DotSlot 独立数组
V2 最少 9 个同时活跃 Buff（4增益+3被动+火力+弹药），8 不够。扩至 12（9+3 余量）。BuffSlot V2 扩展后 48B×12=576B/Entity，可接受。DOT 独立：DotSlot{DotId/TickInterval/RemainingTime/Duration/DmgPerTick/SourceTagId}=24B，DotSlot[8]=192B。不做热冷拆分（576B << L1）。
**回写**：TDD_03 §S3.1 修正。

---

### RE: UA-003 | ✅ 接受 | NativeArray→int[] 托管数组
项目零 NativeArray 使用，BulletWorld 全部托管数组。改 `int[] _sourceTags = new int[capacity]` 与 BulletCore[] 同生命周期。零 GC、零 Dispose、WebGL 安全。
**回写**：TDD_04 §S4.3 修正。

---

### RE: UA-004 | ✅ 接受 | ComponentType 分配表明确
InvincibilityComponent 是 IDamageModifier（通过 HealthComponent.AddModifier 注册），不需要 ComponentType。V2 新增：EnemyShoot=11, Passive=12。余 3 位(13~15)给 V3。MAX=16 扩容策略：改常量+扩数组，O(1) 改动。
**回写**：TDD_01/02/03 补充枚举分配表。

---

### RE: UA-005 | ✅ 接受 | EnemyShoot 复用 AttackBulletPattern+AttackInterval
EntityConfigSO 已有 AttackBulletPattern/AttackInterval/AttackFireOffset，直接复用。只新增 `float FirstAttackDelay` 字段（避免同帧开火）。技能列表从 BattleLevelData.EquippedSkills[] 注入 SkillComponent，不从 ConfigSO 读。
**回写**：TDD_01 §S1.2 + TDD_02 §S2.6 修正。

---

### RE: UA-006 | ✅ 接受 | 弹丸碰撞→DamageContext 桥接走 PlayerCollisionTarget._onPlayerHit
关键发现：DanmakuSystem.API.cs 第 80 行已有 `PlayerCollisionTarget` + `_onPlayerHit` 回调机制。V2 在回调中构造 DamageContext 走 HealthComponent.TakeDamage(ref ctx) → IDamageModifier 链。不需改 ICollisionTarget 接口。
```
路径：CollisionSolver → PlayerCollisionTarget.OnBulletHit → _onPlayerHit 回调(BattleController注册)
     → 构造 DamageContext → playerEntity.HealthComponent.TakeDamage(ref ctx) → Modifier 链
```
**回写**：TDD_01 §S1.3 补充完整桥接路径。

---

### RE: UA-007 | ✅ 接受 | 攻速钳制保留 Ratio=0.3 方案
TDD "0.05s" 描述有歧义。保留现有 `MIN_ATTACK_INTERVAL_RATIO=0.3f`（比率钳制），修正描述为"乘积比率下限≥0.3"。基础 0.25s×0.3=0.075s=13.3 发/s，弹幕池 2048 可承受。
**回写**：TDD_03 §S3.1 修正。

---

### RE: UA-008 | ✅ 接受 | 两阶段通关判定
新增 `BattleState.WaitingClear`：Playing→(无存活Enemy)→WaitingClear→(Enemy弹丸全清)→Victory。遍历 BulletCore[] 检查 Faction==0 只在 WaitingClear 阶段执行（<1s），2048 次 if <0.01ms。
**回写**：TDD_04 §S4.3 修正。

---

### RE: UA-009 | ✅ 接受 | PA-04 碰撞事件通过 OnCollisionHit 桥接
利用 UA-006 的 _onPlayerHit 回调，在 TakeDamage **之前**发布 `entity.EventBus.Publish(new OnCollisionHit{Context=ctx})`。PA-04 订阅此事件触发反击。无敌帧不影响事件发布（发布在 TakeDamage 检查之前）。
**回写**：TDD_03 §S3.5 补充桥接路径。

---

### RE: UA-010 | ✅ 接受 | Editor 预览补充 SceneView.RepaintAll
在 EditorBulletSimulator 的 EditorApplication.update 回调中调用 `SceneView.RepaintAll()`，关闭预览时停止刷新。
**回写**：TDD_05 §S5.1 补充。

---

> **守方 Round 1 总结**：10/10 全部接受，0 拒绝。
> 关键架构收获：UA-006 的 PlayerCollisionTarget._onPlayerHit 回调是 V2 弹丸碰撞→DamageContext 桥接的天然入口，不需改 ICollisionTarget 接口。

---

## PK Round 2 — 攻方复审

### Round 1 回应评估

| ID | 评价 | 说明 |
|----|------|------|
| UA-001 | 🟢 满意 | API 调用修正准确，角度制确认有代码依据 |
| UA-002 | 🟢 满意 | 12 槽 + 不拆分方案务实，内存分析清晰 |
| UA-003 | 🟢 满意 | int[] 与项目风格一致，干净 |
| UA-004 | 🟢 满意 | InvincibilityComponent 不走 ComponentType 逻辑自洽 |
| UA-005 | 🟢 满意 | 复用 AttackBulletPattern 避免字段膨胀 |
| UA-006 | 🟢 满意 | PlayerCollisionTarget 回调桥接优雅，零接口改动 |
| UA-007 | 🟢 满意 | 保留比率钳制，设计安全 |
| UA-008 | 🟢 满意 | 两阶段判定干净，性能可控 |
| UA-009 | 🟢 满意 | OnCollisionHit 事件时序正确，无敌帧不影响 PA-04 |
| UA-010 | 🟢 满意 | RepaintAll 是标准 Editor 工具实践 |

### 新追问

#### UA-011 | 严重度 🟢低 | FirstAttackDelay 字段位置——EntityConfigSO 还是 EnemyShootComponent 内部常量？
**涉及章节**：TDD_01 §S1.2
**质疑**：RE:UA-005 说新增 `FirstAttackDelay` 字段，但没明确放在哪里。选项：
- (A) EntityConfigSO 新增 `[Header("敌机射击")] public float FirstAttackDelay = 0.5f;`——策划可配置
- (B) EnemyShootComponent 内部 `private const float FIRST_FIRE_DELAY = 0.5f;`——代码常量
**建议方向**：选 (A)，因为不同敌机类型可能需要不同的开火延迟（精英怪 0.2s，杂兵 1.0s）。
**状态**：🟡 待回应

---

> **攻方 Round 2 总结**：Round 1 的 10 个问题全部满意。仅追加 1 个 🟢低优细节问题。
> **收敛判断**：Round 1 全部解决 + Round 2 仅 1 个低优新问题 → **接近收敛**。

---

## PK Round 2 — 守方回应

### RE: UA-011 | ✅ 接受 | FirstAttackDelay 放入 EntityConfigSO（策划可配置）

选方案 (A)：`EntityConfigSO` 新增 `[Header("敌机射击")] [Tooltip("首次开火延迟（秒），避免同帧集火")] public float FirstAttackDelay = 0.5f;`

理由：不同敌机类型确实需要不同的开火延迟，策划可配置更灵活，且字段成本 4B 可忽略。

**回写**：TDD_01 §S1.2 明确 FirstAttackDelay 来源为 `entity.ConfigSO.FirstAttackDelay`。
**状态**：✅ 已修正

---

> **守方 Round 2 总结**：1/1 接受。

---

## PK 收敛声明

**收敛条件达成**：
- ✅ Round 2 无新 🔴/🟡 问题
- ✅ Round 2 仅 1 个 🟢低优，已当轮解决
- ✅ 双方无分歧（0 拒绝）

**PK 结束**，共 2 轮，总计 11 个问题（10 + 1），全部解决。

---

## 最终总结

| 统计项 | 数值 |
|--------|------|
| 总轮次 | 2 |
| 总问题数 | 11 |
| 🔴 高优 | 3（UA-001/002/003）|
| 🟡 中优 | 4（UA-004/005/006/007）|
| 🟢 低优 | 4（UA-008/009/010/011）|
| 接受率 | 100%（11/11）|
| 拒绝率 | 0% |

### 文档版本变更

| 文件 | 修改内容 | 版本 |
|------|----------|------|
| SG_V2_TDD_01 | Fire() API 修正 + 桥接路径 + FirstAttackDelay 来源 | →v1.1 |
| SG_V2_TDD_02 | SkillConfig[] 注入来源说明 | →v1.1 |
| SG_V2_TDD_03 | MAX_BUFFS=12 + DotSlot + 攻速钳制修正 + PA-04 桥接 | →v1.1 |
| SG_V2_TDD_04 | sourceTag→int[] + 两阶段通关判定 | →v1.1 |
| SG_V2_TDD_05 | SceneView.RepaintAll 驱动 | →v1.1 |

### 需要天命人决策的项目

| # | 决策项 | 选项 | 天命人决策 | 理由 |
|---|--------|------|----------|------|
| 1 | MAX_BUFFS 容量 | 12 / 16 | ✅ **16** | 留足余量 |
| 2 | WaitingClear 阶段 | 两阶段清场 / 直接Victory | ✅ **直接 Victory**（暂停+弹出面板） | 节奏快，无需等弹丸消散 |
| 3 | FirstAttackDelay 默认值 | 0.3s / 0.5s / 1.0s | ✅ **1.0s** | 给玩家充足反应时间 |

---

> **PK 状态**：✅ 已完成
> **结束时间**：2026-05-18 17:05
