# Phase 3A TDD — §一 设计目标 & §二 行为契约扩展

> **所属文档**：[PHASE3A_TDD_INDEX.md](PHASE3A_TDD_INDEX.md) · v0.4  
> **本文件范围**：设计目标、设计支柱、行为契约（BC-09 ~ BC-13）

---

## 一、设计目标

Phase 3A 在 Phase 2 已验收的 Entity-Component 框架基础上，扩展**战斗能力层**：

1. **玩家移动边界**（P3.0）— 基础体验保障，玩家不能飞出屏幕
2. **空间查询 + 自动瞄准**（P3.1）— Entity 能"感知周围"并锁定目标
3. **直接伤害路径**（P3.2）— 不走弹幕的 AOE/光环/陷阱伤害
4. **技能系统**（P3.3）— 最小版：配置驱动的效果槽，不替代 AttackComponent
5. **Buff/Debuff 系统**（P3.4）— 最小版：属性修正 + 持续时间

**设计支柱**（Design Pillars）：

| # | 支柱 | 约束 |
|---|------|------|
| 1 | 零 GC | 所有新增组件/服务禁止运行时分配 |
| 2 | 配置驱动 | 新增行为通过 SO 配置，不改代码 |
| 3 | 最小可用 | 每个子系统只做刚需，不过度设计 |
| 4 | 向下兼容 | 不破坏 Phase 1/2 已有组件的行为契约 |
| 5 | 真机 55fps | 20 Entity + 弹幕 ≥ 55fps（微信小游戏真机） |

---

## 二、行为契约扩展（BC-09 ~ BC-13）

> 以下契约扩展 `ENTITY_COMPONENT_TDD.md` 的行为契约层。

### BC-09 空间查询契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-09.1 | `EntityManager.FindEntitiesInRadius(center, radius, camp, buffer, max)` 返回范围内指定阵营的 Entity 列表，使用调用方传入的预分配 buffer，零 GC | 待实现 |
| BC-09.2 | `EntityManager.FindNearestEntity(center, radius, camp)` 返回最近单个 Entity（内部复用静态 buffer），零 GC | 待实现 |
| BC-09.3 | 空间查询不修改任何 Entity 状态，纯只读操作 | 待实现 |

### BC-10 自动瞄准契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-10.1 | AutoAimComponent 实现 `ITargetProvider` 接口，向 AI/Attack 系统暴露当前锁定目标 | 待实现 |
| BC-10.2 | AutoAimComponent 按可配置间隔（`SearchInterval`）定频搜索，非每帧搜索 | 待实现 |
| BC-10.3 | AutoAimComponent 仅搜索**敌对阵营**（阵营判断规则：Player ↔ Enemy 互为敌对） | 待实现 |
| BC-10.4 | AttackComponent 发射方向优先级：AutoAim 锁定方向 > DecisionCommand.AimDirection > Entity.Rotation | 待实现 |
| BC-10.5 | AutoAim 目标死亡/失效时**立即重搜**（不等下次定频搜索周期）(v0.4 GD-002) | 待实现 |

### BC-11 直接伤害契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-11.1 | `DamageDealer.DealDamageToEntity(target, context)` 直接对单个 Entity 造成伤害，走完整 TakeDamage 管线（IDamageModifier 链） | 待实现 |
| BC-11.2 | `DamageDealer.DealAreaDamage(center, radius, camp, context, max)` 对范围内多个 Entity 造成伤害，返回实际命中数 | 待实现 |
| BC-11.3 | DamageDealer 是无状态静态工具类，不占 ComponentType 槽位 | 待实现 |
| BC-11.4 | DealAreaDamage 循环中每次迭代检查 `PendingDespawn/IsAlive`（v0.4 SA-006 bug fix） | 待实现 |
| BC-11.5 | DamageDealer 所有路径最终调用 `HealthComponent.TakeDamage`，Phase 2 受击反馈管线自动生效（v0.4 GD-007） | 待实现 |

### BC-12 技能组件契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-12.1 | SkillComponent 持有**单个** SkillConfigSO 引用，管理 CD/前摇/后摇状态机 | 待实现 |
| BC-12.2 | 技能效果通过 `ISkillEffect` 策略接口实现，`[SerializeReference]` 序列化 | 待实现 |
| BC-12.3 | SkillComponent 与 AttackComponent **共存不替代**——简单 Entity 用 Attack，复杂用 Skill | 待实现 |
| BC-12.4 | SkillComponent 使用 `ComponentType.Skill = 6` 槽位（与 Attack=9 独立） | 待实现 |
| BC-12.5 | ISkillEffect 实现必须**无状态**（SA-002）：共享实例不允许持有随 Execute 调用变化的字段 | 待实现 |
| BC-12.6 | ISkillEffect.Execute 返回 `bool`（施放成功语义）(v0.4 ATK-008) | 待实现 |
| BC-12.7 | SkillComponent.Tick 入口检查 Entity 存活状态，死亡时中断技能回 Idle（v0.4 ATK-014） | 待实现 |
| BC-12.8 | Casting 期间不限制 Entity 其他行为（移动/攻击正常）(v0.4 GD-011) | 待实现 |

### BC-13 Buff 组件契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-13.1 | BuffComponent 持有固定 8 槽 Buff 数组，预分配，零 GC | 待实现 |
| BC-13.2 | Buff 生命周期：挂载（Apply）→ 持续 Tick → 到期移除 → 效果恢复 | 待实现 |
| BC-13.3 | Buff 效果通过属性修正实现（加减乘），不直接修改 EntityConfigSO | 待实现 |
| BC-13.4 | 同 ID Buff 叠加规则：刷新持续时间 + 完整更新属性修正值（v0.4 SA-013），可配置 | 待实现 |
| BC-13.5 | RecalcModifiers 结果 Clamp：MoveSpeed ∈ [0.4, 2.5]，AttackInterval ∈ [0.3, 3.0]（v0.4 GD-004） | 待实现 |
