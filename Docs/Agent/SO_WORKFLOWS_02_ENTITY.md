---
system: entity-component
scope: so-entity-configs
last_verified: 2026-05-21
related_code: Assets/_Framework/EntitySystem/Scripts/Config/*.cs, Assets/_Framework/EntitySystem/Scripts/View/SpriteAnimDataSO.cs, Assets/_Framework/EntitySystem/Scripts/Components/PassiveComponent.cs
---

# SO 配置流程 — 02 Entity 系统

## EntityConfigSO

**菜单路径**：`Create → Entity/EntityConfig`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/Config/EntityConfigSO.cs`
**实例目录**：`Assets/_Game/Configs/Entity/`
**自定义 Inspector**：`EntityConfigSOEditor`（按组件列表动态展示相关字段）

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ConfigId` | `int` | 0 | 全局唯一 ID（Phase 2 Luban 迁移必填） |
| `DisplayName` | `string` | `""` | 调试/UI 显示名 |
| `Camp` | `EnumCamp` | — | 阵营：Player/Enemy/Neutral |
| `Components` | `ComponentType[]` | `[]` | 挂载组件列表（决定 EntityPool 预创建） |
| `PoolMax` | `int` | 128 | 对象池容量 [Min(1)] |
| **属性** | | | |
| `MaxHp` | `int` | 100 | 最大血量 |
| `MoveSpeed` | `float` | 3 | 基础移速 |
| `TurnSpeed` | `float` | 360 | 转向速度 |
| `CollisionRadius` | `float` | 0.5 | 碰撞半径 |
| `KnockbackDistance` | `float` | 0.5 | 击退距离 |
| `KnockbackDuration` | `float` | 0.2 | 击退时长（秒） |
| `KnockbackCurve` | `AnimationCurve` | null | 击退速度曲线（空=线性） |
| **受击** | | | |
| `IFrameCount` | `int` | 0 | 无敌帧数（0=不启用） |
| `HitStopFrames` | `int` | 0 | 顿帧数（0=不启用） |
| **Entity 碰撞** | | | |
| `EnableEntityCollision` | `bool` | true | 是否参与 Entity vs Entity 碰撞 |
| `CollisionLayer` | `int` | 0 | 碰撞层（0=全层） |
| `ContactDamage` | `int` | 0 | 接触伤害（0=不造成） |
| `ContactDamageInterval` | `float` | 0.5 | 接触伤害间隔（秒） |
| **战斗** | | | |
| `AttackPower` | `int` | 0 | 攻击力（0=用弹幕固定伤害） |
| `CritRate` | `float` | 0 | 暴击率 [0,1] |
| `CritDamageMultiplier` | `float` | 2.0 | 暴击倍率 [Min(1)] |
| `AutoAimRadius` | `float` | 0 | 自动瞄准搜索半径（0=不启用） |
| `AutoAimSearchInterval` | `float` | 0.2 | 搜索间隔 [Min(0.05)] |
| `AttackInterval` | `float` | 1.0 | 攻击间隔（0=不攻击） |
| `AttackBulletPattern` | `BulletPatternSO` | null | 攻击弹幕 |
| `AttackFireOffset` | `Vector2` | (0,0) | 发射偏移 |
| `SkillConfig` | `SkillConfigSO` | null | 技能配置（null=不启用） |
| `AIBehavior` | `AIBehaviorSO` | null | AI 行为（null=不启用） |
| **视觉** | | | |
| `ViewPrefab` | `GameObject` | null | 渲染预制体 |
| `ViewPoolDef` | `PoolDefinition` | null | View 对象池 |
| `SpriteAnimData` | `SpriteAnimDataSO` | null | 序列帧动画 |
| `DebugColor` | `Color` | white | Debug Prefab 色调 |
| **受击反馈** | | | |
| `HitFlashDuration` | `float` | 0.1 | 闪白时长 |
| `HitFlashColor` | `Color` | white | 闪白颜色 |
| `ShowDamageNumber` | `bool` | true | 是否显示伤害数字 |
| `SpawnEffect` | `PoolDefinition` | null | 生成特效 |
| `HitEffect` | `PoolDefinition` | null | 受击特效 |
| `DeathEffect` | `PoolDefinition` | null | 死亡特效 |
| `DeathDelay` | `float` | 0.3 | 死亡延迟回收（秒） |

### 创建新敌人完整流程

1. `Create → Entity/EntityConfig`，命名 `Enemy_SlimeBoss`
2. 设置 `Camp = Enemy`，`Components = [State, Health, Movement, Collision, Attack, AutoAim, Skill, Buff]`
3. 填写属性（HP/移速/碰撞半径等）
4. 关联 `AttackBulletPattern`（已有或新建 BulletPatternSO）
5. 关联 `AIBehavior`（已有或新建 AIBehaviorSO）
6. 关联 `SkillConfig`（可选）
7. 关联 `ViewPrefab` + `ViewPoolDef`
8. 通过 `EntityConfigValidator` 校验：`Tools → MiniGame → Validate → Entity Configs`

---

## SkillConfigSO

**菜单路径**：`Create → Entity/SkillConfig`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/Config/SkillConfigSO.cs`
**实例目录**：`Assets/_Game/Configs/_Template/Skill/`
**自定义 Inspector**：`SkillConfigSOEditor`（TypeCache 自动发现 ISkillEffect 实现）

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `DisplayName` | `string` | `""` | 技能名 |
| `TriggerMode` | `SkillTriggerMode` | `Auto` | Manual=手动 / Auto=CD 自动 |
| `CooldownTime` | `float` | 5.0 | 冷却时间 [Min(0)] |
| `CastTime` | `float` | 0 | 前摇时间（0=瞬发） [Min(0)] |
| `RecoveryTime` | `float` | 0.5 | 后摇时间 [Min(0)] |
| `Effects` | `ISkillEffect[]` | `[]` | 效果链（[SerializeReference]） |

### 内置 ISkillEffect 实现

| 类 | 用途 | 关键参数 |
|----|------|---------|
| `FireBulletsEffect` | 发射弹幕 | 无（使用 Entity 的 AttackComponent） |
| `AreaDamageEffect` | AOE 范围伤害 | `Radius`、`Damage` |
| `ApplyBuffEffect` | 给目标施加 Buff | `BuffConfig(BuffConfigSO)`、`SearchRadius` |

### 状态机（SkillComponent）

```
Idle → [触发] → Casting → [CastTime] → Execute Effects → Recovery → [RecoveryTime] → Cooldown → [CD] → Idle
```

---

## BuffConfigSO

**菜单路径**：`Create → Entity/BuffConfig`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/Config/BuffConfigSO.cs`
**实例目录**：`Assets/_Game/Configs/_Template/Buff/`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `DisplayName` | `string` | `""` | 显示名 |
| `BuffId` | `int` | 0 | 唯一标识（同 ID 刷新不叠层） |
| `Duration` | `float` | 5.0 | 持续秒数（0=永久） [Min(0)] |
| `MoveSpeedModifier` | `float` | 1.0 | 移速倍率（0.5=减速50%） |
| `AttackIntervalModifier` | `float` | 1.0 | 攻速倍率（0.5=攻速翻倍） |
| `DamageTakenModifier` | `float` | 1.0 | 受伤倍率（0=无敌） |

### BuffId 命名规范

| 前缀 | 范围 | 类型 |
|------|------|------|
| 1xxx | 1001~1999 | 增益 Buff（加速/加攻） |
| 2xxx | 2001~2999 | 攻击类 Buff（攻速提升） |
| 3xxx | 3001~3999 | 减益 Debuff（减速/增伤） |

### Buff 叠加规则

- **同 ID**：刷新全部字段（Duration+属性修正），不叠层
- **不同 ID**：独立计时，属性修正**乘法叠加** + Clamp（Speed: 0.4~2.5, Attack: 0.3~3.0）

---

## AIBehaviorSO

**菜单路径**：`Create → Entity/AIBehavior`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/Config/AIBehaviorSO.cs`
**实例目录**：`Assets/_Game/Configs/AI/`
**自定义 Inspector**：`AIBehaviorSOEditor`（可视化条件-动作表）

### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `Entries` | `AIBehaviorEntry[]` | 按优先级排列的条件-动作表 |

### AIBehaviorEntry 结构

| 字段 | 类型 | 说明 |
|------|------|------|
| `Condition` | `AIConditionType` | 条件类型 |
| `ConditionParam` | `float` | 条件参数 |
| `Action` | `AIActionType` | 执行动作 |
| `ActionParam` | `float` | 动作参数 |

### 条件枚举

| 值 | 说明 | ConditionParam |
|----|------|---------------|
| `Always` | 无条件（兜底） | — |
| `HpBelow` | HP% < 阈值 | 0.0~1.0 |
| `TargetInRange` | 目标距离 < 阈值 | 世界单位 |
| `TargetLost` | 无目标 | — |

### 动作枚举

| 值 | 说明 | ActionParam |
|----|------|------------|
| `Idle` | 原地待命 | — |
| `MoveToTarget` | 向目标移动 | — |
| `Attack` | 攻击 | — |
| `Flee` | 逃跑 | 逃跑距离 |
| `Patrol` | 巡逻 | 巡逻半径 |

---

## EntitySpawnWaveSO

**菜单路径**：`Create → Entity/SpawnWaveConfig`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/Config/EntitySpawnWaveSO.cs`
**实例目录**：`Assets/_Game/Configs/SpawnWave/`
**自定义 Inspector**：`EntitySpawnWaveSOEditor`（可视化波次编排）

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Waves` | `SpawnWaveEntry[]` | `[]` | 波次数组 |
| `Loop` | `bool` | false | 是否循环（无限模式） |
| `LoopStartWave` | `int` | 0 | 循环起始索引 |

### SpawnWaveEntry

| 字段 | 类型 | 说明 |
|------|------|------|
| `Groups` | `SpawnGroup[]` | 本波怪物组 |
| `TriggerMode` | `WaveTriggerMode` | Timer/AllCleared/OnCallback |
| `TriggerDelay` | `float` | Timer 模式延迟（秒） |

### SpawnGroup

| 字段 | 类型 | 说明 |
|------|------|------|
| `EntityConfig` | `EntityConfigSO` | 怪种配置 |
| `Camp` | `EnumCamp` | 阵营 |
| `Count` | `int` | 数量 |
| `SpawnInterval` | `float` | 组内逐个间隔（秒） |
| `Formation` | `SpawnFormation` | Random/Line/Circle/Grid |

---

## SpriteAnimDataSO

**菜单路径**：`Create → Entity/SpriteAnimData`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/View/SpriteAnimDataSO.cs`
**实例目录**：`Assets/_Game/Configs/Entity/`

### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `Clips` | `SpriteAnimClip[]` | 动画片段列表（按 AnimId 索引） |

### SpriteAnimClip

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Name` | `string` | `""` | 动画名（调试） |
| `Frames` | `Sprite[]` | `[]` | 帧序列 |
| `FramesPerSecond` | `float` | 10 | 播放速度 [Min(1)] |
| `Loop` | `bool` | true | 是否循环 |

### 使用说明

- `Clips[0]` = Idle（兜底动画）
- AnimId 超出范围自动回退到 `Clips[0]`
- 空数组 `GetClip()` 返回 null

---

## DotConfigSO（Sprint 3 新增）

**菜单路径**：`Create → Entity/DotConfig`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/Config/DotConfigSO.cs`
**实例目录**：`Assets/_Game/Configs/ShooterGame/Dots/`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `DotId` | `int` | 0 | 唯一 ID [4000,4999]（OnValidate 校验） |
| `DisplayName` | `string` | `""` | 调试/UI 显示名 |
| `DamagePerTick` | `int` | 1 | 每次 Tick 伤害 [Min(1)] |
| `TickInterval` | `float` | 1f | Tick 间隔秒 [Min(0.1)] |
| `Duration` | `float` | 3f | 持续时间秒 [Min(0.1)] |
| `Tag` | `BuffTag` | Negative | DOT 标签 |
| `VfxPrefab` | `GameObject` | null | VFX 预制件（V2 预留） |

### 使用说明

- 同 ID DOT 重复施加 → 刷新 Duration（不叠加伤害）
- `BuffComponent.ApplyDot()` + `Tick()` 内 `while(timer>=interval)` 驱动
- 激光技能通过 `SkillConfigSO.AttachedDotConfig` 引用

---

## PassiveAbilitySO（Sprint 3 新增）

**菜单路径**：`Create → Entity/PassiveAbility`
**命名空间**：`MiniGameTemplate.Entity`
**源码**：`Assets/_Framework/EntitySystem/Scripts/Config/PassiveAbilitySO.cs`
**实例目录**：`Assets/_Game/Configs/ShooterGame/Passives/`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `PassiveId` | `int` | 0 | 唯一 ID [5000,5999]（OnValidate 校验） |
| `DisplayName` | `string` | `""` | 调试/UI 显示名 |
| `Description` | `string` | `""` | 描述文本 |
| `Icon` | `Sprite` | null | 被动图标 |
| `Cooldown` | `float` | 5f | CD 秒 [Min(0.1)] |
| `TriggerMode` | `PassiveTriggerMode` | AutoOnReady | 触发模式：AutoOnReady / OnHit |
| `LinkedBuff` | `BuffConfigSO` | null | 关联 Buff（桥接模式） |
| `ActivateEffects` | `ISkillEffect[]` | `[]` | 即时型效果数组 |
| `BulletDirections` | `int` | 0 | 环形弹方向数（PA-04 Retaliate 用） |

### 使用说明

- AutoOnReady：CD 归零自动激活 → ApplyBuff(LinkedBuff) 或执行 ActivateEffects
- OnHit：订阅 `OnCollisionHit` 事件 → CD 就绪时激活
- 最多 3 个被动并行（`PassiveComponent.MAX_PASSIVES = 3`）
- `PassiveComponent.InitWithPassives()` 由 BattleController 调用
