---
system: shootergame-gdd
scope: items-drops-config
last_verified: 2026-05-18-c
depends_on: [SG_GDD_INDEX, SG_GDD_02_PASSIVE_BUFFS]
related_code: Assets/_Game/Scripts/ShooterGame/Config/*, Assets/_Game/Configs/ShooterGame/**
---

# 七、道具掉落系统

### 7.1 为什么需要道具

道具系统是连接"技能/Buff/被动"与"玩家获取"的桥梁。

```
敌机被击杀 → 概率掉落道具 → 玩家靠近自动拾取 → 获得效果
```

### 7.2 道具类型

> **注意**：技能和被动技能现走战前装备制（见 [SG_GDD_01_ACTIVE_SKILLS](SG_GDD_01_ACTIVE_SKILLS.md)），不再通过道具掉落获取。
> 战斗内道具专注于**临时增益**和**资源补给**。

| 类型 | 效果 | 掉落概率 | 图标颜色 |
|------|------|---------|---------|
| Buff 道具 | 获得一个临时增益 Buff（5~8s） | 20% | 蓝色 |
| 修复道具 | 基地回复 10 点 HP | 12% | 绿色 |
| 弹药道具 | 基础攻击临时强化（子弹 ×2，5s） | 10% | 红色 |
| 金币道具 | 获得金币（V3 商店货币预留） | 25% | 金色 |

> **设计变更说明**：v1.0 中道具承载技能+被动+Buff+修复+弹药五类，v1.1 精简为 Buff+修复+弹药+金币四类。
> 技能/被动走战前装备的收集解锁系统，道具专注"战斗内短期增益"，职责更清晰。

### 7.3 道具 Entity 设计

道具是轻量级 Entity：
- Components: `[Movement]`（缓慢向下漂浮）
- 无 Health（不可被摧毁）
- 无 Collision（不参与 EntityCollisionSolver——v1.4 PK 确认维持此设计）
- **拾取方式**（v1.4 PK 修正）：独立 PickupSystem 每帧 O(n) 遍历活跃道具列表，距离 < PickupRadius 时自动拾取
- PickupRadius 默认 1.0 世界单位（被动 PA-03 可增加到 2.0）

> **PickupRadius 数据流**（v2.0 架构师 PK 新增）：
>
> ```
> PickupConfigSO.BasePickupRadius = 1.0（全局基础值）
>
> 玩家 Entity 初始化时：
>   entity.PickupRadius = PickupConfigSO.BasePickupRadius
>
> PA-03 磁吸激活（通过 Buff 桥接）：
>   MagnetBuffSO 生效 → entity.PickupRadius = base × 2.0
>   Buff 到期 → entity.PickupRadius = base × 1.0
>
> PickupSystem.Tick():
>   float radius = playerEntity.PickupRadius  ← 只读动态值
>   foreach (pickup in activePickups)
>     if (distance < radius) → 拾取
> ```
>
> **职责隔离**：PickupSystem 只读一个浮点数属性，不查询 BuffComponent。修改 PickupRadius 是 Buff 系统的职责。
> V2 用 Entity 公开属性存储（单个值不值得新建组件）。V3 若加磁吸速度/动画可升级为 PickupComponent。

> **v1.4 设计决策**（PK 碰撞层分析后退回）：
> 道具不复用 EntityCollisionSolver，原因：
> 1. 碰撞系统的阵营矩阵为"攻击碰撞"设计，强行加入"拾取碰撞"会污染职责
> 2. 道具同屏 ≤5，O(n) 遍历开销可忽略（< 修改碰撞系统引入 bug 的风险）
> 3. P5 支柱满足：策划只需配 PickupConfigSO，拾取逻辑封装在 PickupSystem 中

**道具空间参数**（v1.3 PK 新增）：

| 参数 | 值 | 设计理由 |
|------|-----|---------|
| 漂浮速度 | **0.8 单位/s** | 远慢于敌弹（4-6），给玩家充足拾取时间 |
| 存在时限 | **8s**（到时未拾取→闪烁 2s 后消失） | 防止屏幕道具堆积 |
| 到达底线 | **不触发基地扣血，直接消失** | 道具不是威胁 |
| 弹幕区道具 | **有意的 risk-reward 设计** | 道具掉入弹幕覆盖区→玩家需决策"冒险捡还是放弃" |
| 同位置多道具 | 拾取半径 1.0 → 同时捡到 → Buff 按规则叠加/刷新（同 ID 刷新，不叠层） | 不会出现"Buff 爆炸" |

### 7.4 道具配置

```
PickupConfigSO : ScriptableObject
├── DisplayName : string
├── PickupType : enum { Buff, Repair, Ammo, Coin }
├── DropWeight : int (掉落权重, 用于加权随机)
├── BuffConfig : BuffConfigSO (仅 Buff 类型)
├── RepairAmount : int (仅 Repair 类型)
├── AmmoBuffConfig : BuffConfigSO (仅 Ammo 类型, 应用攻击强化 Buff)
├── CoinAmount : int (仅 Coin 类型, V3 商店预留)
├── ViewPrefab : GameObject (道具外观)
├── PickupVfx : PoolDefinition (拾取特效)
├── PickupSfx : AudioClipSO (拾取音效)
```

### 7.5 掉落表配置

```
DropTableSO : ScriptableObject
├── Entries : DropEntry[]
│   ├── PickupConfig : PickupConfigSO
│   └── Weight : int
├── BaseDropRate : float (基础掉落率, 如 0.3 = 30%)
├── GuaranteeDrop : bool (Boss 必掉, V2 不使用——V3 Boss 战启用)
```

> **DropTable 结构约束铁律**（v2.1 工具 PK 新增）：
>
> 1. DropTable 为**扁平结构**——Entry 只能引用 PickupConfigSO，不支持引用其他 DropTableSO（无嵌套）
> 2. **不支持条件掉落**——掉落概率固定，由权重决定。不存在"HP < 30% 时多掉修复"的逻辑
> 3. **权重制**（非百分比制）——每个 Entry.Weight 是相对权重值，运行时自动归一化计算实际概率
> 4. **保底机制为运行时 Manager 职责**——`ItemDropSystem` 维护 `_wavesSinceLastRepair` 计数器，连续 5 波不出修复道具则保底出一个。此逻辑不在 SO 配置中，由 TDD 定义实现
> 5. 因为无嵌套，不存在循环引用问题，无需循环引用验证工具

---

# 八、配置表设计

### 8.1 SO 资产一览（V2 新增）

| 资产名 | SO 类型 | 数量 | 存放路径 |
|--------|---------|------|----------|
| **玩家技能** | | | |
| SG_Skill_Spread | SkillConfigSO | 1 | `Configs/ShooterGame/Skills/` |
| SG_Skill_Homing | SkillConfigSO | 1 | 同上 |
| SG_Skill_Laser | SkillConfigSO | 1 | 同上 |
| SG_Skill_Overdrive | SkillConfigSO | 1 | 同上 |
| SG_Skill_Shield | SkillConfigSO | 1 | 同上 |
| SG_Skill_Shockwave | SkillConfigSO | 1 | 同上 |
| **敌机子弹 Pattern** | | | |
| SG_EnemyBullet_Straight | BulletPatternSO | 1 | `Configs/ShooterGame/Bullets/` |
| SG_EnemyBullet_Spread3 | BulletPatternSO | 1 | 同上 |
| SG_EnemyBullet_Homing | BulletPatternSO | 1 | 同上 |
| **敌机配置** | | | |
| SG_Enemy_Shooter | EntityConfigSO | 1 | `Configs/ShooterGame/` |
| SG_Enemy_Spreader | EntityConfigSO | 1 | 同上 |
| SG_Enemy_Elite | EntityConfigSO | 1 | 同上 |
| **Buff 配置** | | | |
| SG_Buff_SpeedUp | BuffConfigSO | 1 | `Configs/ShooterGame/Buffs/` |
| SG_Buff_AttackUp | BuffConfigSO | 1 | 同上 |
| SG_Buff_Shield | BuffConfigSO | 1 | 同上 |
| SG_Buff_Berserk | BuffConfigSO | 1 | 同上 |
| SG_Debuff_Slow | BuffConfigSO | 1 | 同上 |
| SG_Debuff_Vulnerable | BuffConfigSO | 1 | 同上 |
| SG_Debuff_Blind | BuffConfigSO | 1 | 同上 |
| SG_Dot_Burn | DotConfigSO | 1 | `Configs/ShooterGame/Dots/` |
| SG_Dot_Poison | DotConfigSO | 1 | 同上 |
| SG_Dot_Arc | DotConfigSO | 1 | 同上 |
| **道具配置** | | | |
| SG_Pickup_Buff_SpeedUp | PickupConfigSO | 1 | `Configs/ShooterGame/Pickups/` |
| SG_Pickup_Repair | PickupConfigSO | 1 | 同上 |
| SG_Pickup_Ammo | PickupConfigSO | 1 | 同上 |
| SG_Pickup_Coin | PickupConfigSO | 1 | 同上 |
| ... | ... | ~8 | 同上 |
| SG_DropTable_Normal | DropTableSO | 1 | `Configs/ShooterGame/` |
| SG_DropTable_Elite | DropTableSO | 1 | 同上 |
| **技能解锁配置（V1.1 新增）** | | | |
| SG_SkillUnlockTable | SkillUnlockTableSO | 1 | `Configs/ShooterGame/` |
| SG_PassiveUnlockTable | PassiveUnlockTableSO | 1 | 同上 |
| **AI 行为** | | | |
| ~~SG_AI_Hover~~ | ~~AIBehaviorSO~~ | ~~1~~ | ~~已移除（v1.6 敌机无 AI）~~ |
| ~~SG_AI_Serpentine~~ | ~~AIBehaviorSO~~ | ~~1~~ | ~~已移除~~ |
| ~~SG_AI_Rush~~ | ~~AIBehaviorSO~~ | ~~1~~ | ~~已移除~~ |
| **被动技能配置（v1.4 新增）** | | | |
| SG_Passive_Pierce | PassiveAbilitySO | 1 | `Configs/ShooterGame/Passives/` |
| SG_Passive_Crit | PassiveAbilitySO | 1 | 同上 |
| SG_Passive_Magnet | PassiveAbilitySO | 1 | 同上 |
| SG_Passive_Tailgun | PassiveAbilitySO | 1 | 同上 |
| ~~**RuntimeSet（v1.4 新增）**~~ | | | |
| ~~SG_HoverShooterSet~~ | ~~RuntimeSet&lt;Entity&gt;~~ | ~~1~~ | ~~已移除（v1.6 敌机无悬停）~~ |

**V2 SO 新增总计**：约 35~40 个资产（v1.6 减少 4 个：3 AIBehaviorSO + 1 RuntimeSet）。

### 8.2 关键数值表

#### 我方火力 DPS 计算

| 配置 | 基础攻击 DPS | 技能 DPS | 合计 DPS |
|------|-------------|---------|---------|
| 纯基础攻击 | 10/0.25 = 40/s | 0 | **40 DPS** |
| + 散射弹幕 | 40/s | 5×10/0.3 ≈ 167/s | **207 DPS** |
| + 追踪导弹 | 40/s | 2×25/2.0 = 25/s | **65 DPS** |
| + 攻速 Buff | 40/0.5×0.25 = 80/s | — | **80+ DPS** |

> **DPS 计算公式说明**（v2.3 文档工程师 PK 新增）：
>
> ```
> DPS = (BulletCount × BaseDamage) / EffectiveCycleTime
>
> 其中：
>   BaseDamage     = BulletPatternSO 的单发子弹伤害（附录数值表中标为"10"的占位值）
>   BulletCount    = 每次发射的子弹数量（散射=5，追踪=2，基础攻击=1）
>   EffectiveCycleTime = CooldownTime + CastTime + RecoveryTime
>                      （基础攻击仅有 AttackInterval=0.25s，无前/后摇）
>                      （散射：CD=0.3s + 前摇=0 + 后摇=0.1s → 实际周期=0.4s）
> ```
>
> **注意**：上表为"理论峰值 DPS"简化估算，**未计入前摇/后摇**。
> 含后摇的精确值示例：散射实际 DPS = 5×10/0.4 = **125/s**（非 167/s）。
> T4（DPS 计算面板）的输出应使用含后摇的精确公式。上表保留简化值作为快速参考。

#### 敌机 HP 与击杀时间

| 敌机 | HP | 纯基础攻击击杀时间 | + 散射击杀时间 |
|------|-----|-------------------|--------------|
| 普通机 | 20 | 0.5s（2发） | 0.06s（1发散射） |
| 快速机 | 20 | 0.5s | 0.06s |
| 射手机 | 40 | 1.0s（4发） | 0.3s |
| 散射机 | 60 | 1.5s（6发） | 0.6s |
| 精英机 | 120 | 3.0s（12发） | 1.2s |

> **[占位符]** 所有数值待 gameplay 测试后调整。初始值基于"普通机 2 发击杀"的设计锚点推算。

#### 敌方伤害与基地容错

| 伤害源 | 单次伤害 | 基地容错次数 |
|--------|---------|------------|
| 敌机突破底线 | 15 | ~6.7 次 |
| 敌机碰撞飞机 | 10 | 10 次（v1.6 新增） |
| 敌弹命中飞机 | 5 | 20 次 |
| 敌弹命中基地 | 8 | 12.5 次 |
| 射手机 DPS | 5/1.5s ≈ 3.3/s | ~30s 才死 |

> **设计校验**：在没有 Buff/技能的情况下，基地承受能力 ≈ 30 秒。加上玩家走位闪避和击杀效率，单关时长 60~120 秒是合理的。
