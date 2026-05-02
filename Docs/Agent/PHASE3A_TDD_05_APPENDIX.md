# Phase 3A TDD — §四~§十一 附录

> **所属文档**：[PHASE3A_TDD_INDEX.md](PHASE3A_TDD_INDEX.md) · v0.4  
> **本文件范围**：槽位规划、时序图、实施步骤、验收矩阵、架构决策、调整杠杆、风险、未决项、文件变更清单

---

## 四、ComponentType 槽位规划（Phase 3A 更新）

```
0  = State       ✅ Phase 1
1  = Health      ✅ Phase 1
2  = Animation   ✅ Phase 1
3  = Movement    ✅ Phase 1
4  = Collision   ✅ Phase 2
5  = AutoAim     🔜 Phase 3A (P3.1)
6  = Skill       🔜 Phase 3A (P3.3)
7  = Control     ✅ Phase 1
8  = AI          ✅ Phase 1
9  = Attack      ✅ Phase 1
10 = Buff        🔜 Phase 3A (P3.4)
11~15 = 预留
```

## 五、TickOrder 时序图（Phase 3A 更新）

```
Buff=50 → Decision=100 → AutoAim=120 → Attack=150 → Skill=160 → Health=250 → Movement=300 → Animation=400
  ↑                                                                                               ↑
 最先生效                                                                                       最后执行
 属性修正                                                                                       视觉更新
```

---

## 六、实施步骤（P3.0 ~ P3.5）

| 步骤 | 内容 | 预估工时 | 依赖 |
|------|------|---------|------|
| **P3.0** | 玩家移动边界（ClampPlayerPositions + OnPlayerHitBounds + Gizmo） | 0.5h | 无 |
| **P3.1** | 空间查询 + AutoAimComponent + AttackComponent 集成 + CampUtility | 2~3h | P3.0 |
| **P3.2** | DamageDealer 静态工具类 | 1h | P3.1（依赖 FindEntitiesInRadius） |
| **P3.3** | SkillComponent + SkillConfigSO + SkillConfigSOEditor + 内置 Effect | 3~4h | P3.1 + P3.2 |
| **P3.4** | BuffComponent + BuffConfigSO + SpeedModifierIds + 组件集成 + ApplyBuffEffect | 2~3h | P3.3 |
| **P3.5** | 集成验收 + 真机性能验证 | 1~2h | P3.0~P3.4 全部完成 |

**总计预估**：9~13 小时（1.5~2 天）

---

## 七、验收矩阵（17 项）

| # | 测试项 | 通过条件 | 步骤 |
|---|--------|---------|------|
| 1 | 玩家移动边界 | 玩家 Entity 无法移出 PlayerBounds 矩形 | P3.0 |
| 2 | 边界 Gizmo | Scene View 可见蓝色矩形框标识活动区域 | P3.0 |
| 3 | FindEntitiesInRadius | 正确返回范围内指定阵营 Entity，范围外不返回 | P3.1 |
| 4 | FindNearestEntity | 返回最近的一个，无匹配返回 null | P3.1 |
| 5 | AutoAim 锁定 | 敌方 Entity 自动锁定最近玩家，AimDirection 指向目标 | P3.1 |
| 6 | AutoAim + Attack 联动 | 弹幕朝 AutoAim 锁定方向发射（优先级高于 Entity 朝向） | P3.1 |
| 7 | DamageDealer 单体 | `DealDamageToEntity` 正确扣血，走 IDamageModifier 链 | P3.2 |
| 8 | DamageDealer AOE | `DealAreaDamage` 范围内多个 Entity 同时扣血，返回命中数 | P3.2 |
| 9 | SkillComponent CD | CD 期间不可释放，CD 结束后可再次触发 | P3.3 |
| 10 | SkillComponent 前摇/后摇 | CastTime > 0 时先进入 Casting，时间到后执行 Effects | P3.3 |
| 11 | AreaDamageEffect | 技能触发 AOE 直伤，范围内敌方 Entity 扣血 | P3.3 |
| 12 | BuffComponent 生命周期 | Apply → 持续 → 到期自动移除 → 修正值恢复 | P3.4 |
| 13 | Buff 属性修正 | 减速 Buff 使 MovementComponent 速度降低；攻速 Buff 使攻击间隔缩短 | P3.4 |
| 14 | 真机性能 | 20 Entity + AutoAim + 弹幕 ≥ 55fps（微信小游戏真机） | P3.5 |
| 15 | Buff 速度 Clamp (v0.4 GD-014) | 多减速 Buff 叠加 → MoveSpeedModifier ≥ 0.4；多加速 → ≤ 2.5 | P3.4 |
| 16 | Buff 攻速 Clamp (v0.4 GD-014) | 多攻速 Buff 叠加 → AttackIntervalModifier ∈ [0.3, 3.0] | P3.4 |
| 17 | AOE 连锁击杀安全 (v0.4 SA-006/SA-014) | AOE 中已死亡 Entity 不被二次造伤；OnDeath 单体伤害正常生效 | P3.2 |

---

## 八、架构决策摘要

| 决策 | 选型 | 理由 |
|------|------|------|
| 玩家移动边界 | Bootstrap 层 Clamp + Center/Size 两字段 | 系统规则，对策划直观 |
| 空间查询算法 | 线性扫描 O(N) | 20 Entity 下 < 0.01ms |
| AutoAim 搜索策略 | 定频 + 最近优先 + 目标死亡立即重搜 | 平衡 CPU 与手感 |
| DamageDealer | 静态工具类（模仿 Physics API） | 无状态，零分配 |
| SkillComponent vs AttackComponent | 共存不替代 | 简单用 Attack，复杂用 Skill |
| ISkillEffect | 强制无状态 + bool 返回 | 共享实例安全 + 未来扩展 |
| Buff 修正模型 | 乘法叠加 + Clamp 极端值 | 可预测 + 手感底线 |
| Buff 叠加规则 | 同 ID 完整刷新（时间+属性） | 语义清晰 |
| Buff TickOrder | 50（最早）| 属性修正需在 Decision/Attack 之前 |
| Buff→Movement | push by-ID + SpeedModifierIds 常量 | 正确依赖方向 |
| Attack→Buff | pull AttackIntervalModifier | 唯一来源不值得建 Modifier 系统 |

---

## 八b、调整杠杆速查表（v0.4 GD-015 新增）

| 杠杆 | 位置 | 默认值 | 影响 | 状态 |
|------|------|--------|------|------|
| PlayerBoundsCenter | Bootstrap Inspector | (0, 0) | 玩家活动范围中心 | `[占位符]` |
| PlayerBoundsSize | Bootstrap Inspector | (9, 14) | 玩家活动范围大小 | `[占位符]` |
| AutoAimRadius | EntityConfigSO | 0 (不启用) | AutoAim 锁定范围 | `[占位符]` |
| AutoAimSearchInterval | EntityConfigSO | 0.2s | 瞄准灵敏度 | `[占位符]` |
| CooldownTime | SkillConfigSO | 5s | 技能使用频率 | `[占位符]` |
| CastTime | SkillConfigSO | 0s | 施法前摇 | `[占位符]` |
| RecoveryTime | SkillConfigSO | 0.5s | 施法后硬直 | `[占位符]` |
| BuffDuration | BuffConfigSO | 5s | Buff 持续长度 | `[占位符]` |
| MoveSpeedModifier | BuffConfigSO | 1.0 | 移速加减成 | `[占位符]` |
| AttackIntervalModifier | BuffConfigSO | 1.0 | 攻速加减成 | `[占位符]` |
| DamageTakenModifier | BuffConfigSO | 1.0 | 受伤加减成 | `[占位符]` |
| MIN_MOVE_SPEED_RATIO | BuffComponent | 0.4 | 减速下限 | `[占位符]` |
| MAX_MOVE_SPEED_RATIO | BuffComponent | 2.5 | 加速上限 | `[占位符]` |
| MIN_ATTACK_INTERVAL_RATIO | BuffComponent | 0.3 | 攻速下限 | `[占位符]` |
| MAX_ATTACK_INTERVAL_RATIO | BuffComponent | 3.0 | 攻速上限 | `[占位符]` |
| _buffer size | DamageDealer | 64 | AOE 最大可处理目标数 | 固定 |
| MAX_BUFFS | BuffComponent | 8 | 同时最大 Buff 数 | 固定 |
| AreaDamageEffect.Radius | SkillConfigSO | 3f | AOE 伤害半径 | `[占位符]` |
| ApplyBuffEffect.SearchRadius | SkillConfigSO | 5f | Debuff 施加搜索范围 | `[占位符]` |

---

## 八c、依赖方向约束（v0.4 SA-017 新增）

```
底层（无依赖）：CampUtility, SpeedModifierIds
基础设施层：EntityManagerAccessor → EntityManager
组件层：MovementComponent, HealthComponent, AutoAimComponent, BuffComponent, AttackComponent, SkillComponent
效果层：FireBulletsEffect, AreaDamageEffect, ApplyBuffEffect
工具层：DamageDealer
配置层：SkillConfigSO, BuffConfigSO, EntityConfigSO
```

**依赖方向必须遵循**：效果层 → 工具层 → 组件层 → 基础设施层 → 底层。  
**不允许逆向依赖**。组件层内部允许横向协作：
- Buff → Movement（push SpeedModifier）
- Attack → Buff（pull AttackIntervalModifier）

Phase 4+ 新增组件/效果时，必须更新此依赖图并验证无环。

---

## 九、风险与已知限制

| 风险 | 影响 | 缓解 |
|------|------|------|
| FindEntitiesInRadius 线性扫描在 Entity 数量增长后性能下降 | >100 Entity 时可能超 0.1ms | 后续可引入空间分区（Grid/Quadtree），API 不变 |
| AutoAim 搜索间隔导致目标切换延迟 | 0.2s 内新目标可能未被发现 | 目标死亡时立即重搜（GD-002），仅新目标出现时等定频 |
| SkillComponent 手动触发复用 WantsAttack | 无法区分"要攻击"和"要放技能" | Phase 4 扩展 WantsSkill 字段 |
| BuffComponent 乘法叠加极端值 | 多 Buff 叠加可能极端 | RecalcModifiers 中 Clamp（GD-004） |
| ISkillEffect [SerializeReference] 序列化 | WebGL 下 SerializeReference 有反序列化 bug（特定版本） | 当前版本已修复，真机验证 |
| ISkillEffect 实现类重命名/移动命名空间 (v0.4 SA-012) | SkillConfigSO.Effects 反序列化为 null（数据丢失）| 变更时标注 `[MovedFrom]`；命名空间确定后不变更 |

---

## 十、未决项（Phase 3B / Phase 4 / Phase 5）

| # | 功能 | 来源 | 目标阶段 |
|---|------|------|---------|
| 1 | 击杀计分（ScoreManager + Combo） | Phase 3 设计评审 | Phase 3B |
| 2 | 道具掉落/拾取 | Phase 3 设计评审 | Phase 3B |
| 3 | 玩家命数（重生无敌 + 广告续命） | Phase 3 设计评审 | Phase 3B |
| 4 | 难度渐进扩展 | Phase 3 设计评审 | Phase 3B |
| 5 | 游戏会话管理器 | Phase 3 设计评审 | Phase 3B |
| 6 | FSM 状态机编辑器 | ENTITY_COMPONENT_TDD Phase 3 | Phase 4 |
| 7 | 技能打断机制 | BC-12 | Phase 4 |
| 8 | DecisionCommand.WantsSkill 字段 | Phase 3A 风险项 | Phase 4 |
| 9 | SkillComponent 多技能槽扩展（configs[] + activeIndex） | v0.4 GD-003 | Phase 3B |
| 10 | SkillTriggerMode.Conditional + ISkillTriggerCondition | v0.4 GD-008 | Phase 4 |
| 11 | AutoAim 瞄准策略可配置（IAimStrategy） | v0.4 GD-012 | Phase 4 |
| 12 | DealAreaDamage 距离衰减（Func<float,float> falloff） | v0.4 GD-016 | Phase 4 |
| 13 | CampUtility 多阵营支持（关系矩阵/bitmask） | v0.4 SA-008 | Phase 5 |
| 14 | 边界触碰反馈效果（视觉/振动） | v0.4 GD-001 | Phase 3B |

---

## 十一、文件变更清单

### 新增文件

| 文件 | 目录 | 步骤 |
|------|------|------|
| `AutoAimComponent.cs` | `_Framework/EntitySystem/Scripts/Components/` | P3.1 |
| `CampUtility.cs` | `_Framework/EntitySystem/Scripts/Core/` | P3.1 |
| `SpeedModifierIds.cs` | `_Framework/EntitySystem/Scripts/Core/` | P3.4 (v0.4 SA-003) |
| `DamageDealer.cs` | `_Framework/EntitySystem/Scripts/Core/` | P3.2 |
| `SkillConfigSO.cs` | `_Framework/EntitySystem/Scripts/Config/` | P3.3 |
| `ISkillEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/` | P3.3 |
| `SkillContext.cs` | `_Framework/EntitySystem/Scripts/Skill/` | P3.3 |
| `FireBulletsEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/Effects/` | P3.3 |
| `AreaDamageEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/Effects/` | P3.3 |
| `ApplyBuffEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/Effects/` | P3.4 |
| `SkillComponent.cs` | `_Framework/EntitySystem/Scripts/Components/` | P3.3 |
| `SkillConfigSOEditor.cs` | `_Framework/EntitySystem/Editor/` | P3.3 (v0.4 ATK-001) |
| `BuffConfigSO.cs` | `_Framework/EntitySystem/Scripts/Config/` | P3.4 |
| `BuffComponent.cs` | `_Framework/EntitySystem/Scripts/Components/` | P3.4 |

### 修改文件

| 文件 | 变更内容 | 步骤 |
|------|---------|------|
| `EntitySystemBootstrap.cs` | `ClampPlayerPositions()` + Center/Size 字段 + OnPlayerHitBounds + Gizmo | P3.0 |
| `EntityManager.cs` | `FindEntitiesInRadius` 补完 + `FindNearestEntity` 新增 | P3.1 |
| `ITickable.cs` (TickOrders) | `AutoAim = 120`，新增 `Buff = 50` / `Skill = 160` | P3.1 |
| `ComponentType.cs` | 新增 `Buff = 10` | P3.4 |
| `EntityConfigSO.cs` | 新增 AutoAimRadius / AutoAimSearchInterval / SkillConfig | P3.1+P3.3 |
| `EntityConfigSOEditor.cs` | 补齐 AutoAim / Skill / Buff 分段绘制 | P3.1~P3.4 |
| `EntityPool.cs` | 组件工厂补充 AutoAim / Skill / Buff case | P3.1~P3.4 |
| `MovementComponent.cs` | `_modifierIds[]` + `AddOrUpdateSpeedModifier(id,mult)` / `RemoveSpeedModifierById(id)` | P3.4 |
| `AttackComponent.cs` | `GetFireAngle` 增加 AutoAim 优先级 + `TickOrder` 用常量 + Buff 攻速修正 | P3.1+P3.4 |
| `ISkillEffect.cs` | `Execute` 签名 `void` → `bool` (v0.4 ATK-008) | P3.3 |

---

**文档结束。天命人请审阅，确认后逐步推进实施。** 🎯
