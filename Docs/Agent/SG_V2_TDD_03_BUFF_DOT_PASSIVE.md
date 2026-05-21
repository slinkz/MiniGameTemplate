---
system: shootergame-v2-tdd
scope: sprint3-buff-dot-passive
last_verified: 2026-05-21
depends_on: [SG_V2_TDD_INDEX, SG_V2_TDD_02_SKILL_EQUIP_ITEM, SG_GDD_02_PASSIVE_BUFFS]
related_code: Assets/_Framework/EntitySystem/Components/Buff*, Passive*, Assets/_Game/Configs/ShooterGame/Buffs/*, Dots/*, Passives/*
---

# Sprint 3：Buff + DOT + 被动技能（~15h）

> **目标**：BuffComponent 扩展（DOT/Tag/叠层/VFX）、3 种 DOT、4 种被动技能、ID 冲突检测工具。
> **前置**：Sprint 2 验收通过（SkillComponent 存在 → 技能可施 Buff；道具系统存在 → 道具可施 Buff）。

---

## 1. 实施任务分解

### S3.1 BuffComponent 扩展（4h）

#### 实施方案

**现有 BuffComponent 能力**（V1）：
- ✅ BuffSlot[8] 固定数组，零 GC（PK-R1: 8→12; 天命人决策: →16）
- ✅ 同 ID 刷新
- ✅ 3 种属性修正（MoveSpeed / AttackInterval / DamageTaken）
- ✅ push/pull 与 Movement/Attack 交互

**V2 扩展内容**：

```
BuffComponent（扩展——不破坏 V1 接口）
├── [已有] BuffSlot[16] _buffs              // PK-R1 UA-002: 8→12; 天命人决策: →16
├── [新增] DotSlot[16] _dots                // 独立 DOT 数组（V2 最多 3 种同时生效，16 为统一扩容预留——与 BuffSlot[16] 策略一致）（PK-R4 DE-004: 8→16）
├── [新增] int _activeDotCount = 0          // 活跃 DOT 数量
│
├── BuffSlot 扩展字段：
│   ├── [新增] BuffTag Tag                  // Positive/Negative/Status/Aura
│   ├── [新增] StackMode StackMode          // Refresh/Stack
│   ├── [新增] int CurrentStacks            // 当前层数
│   ├── [新增] int MaxStacks                // 最大层数
│   ├── [新增] float BulletCountModifier    // 子弹数修正（默认 1.0）
│   ├── [新增] int VfxInstanceId            // VFX 池实例 ID（-1=无）
│
├── 新增方法：
│   ├── ApplyBuff(BuffConfigSO config):
│   │   → 查找同 BuffId 的 slot
│   │   → 若存在 + StackMode=Refresh → 刷新 Duration
│   │   → 若存在 + StackMode=Stack → CurrentStacks++（≤MaxStacks）
│   │   → 若不存在 → 占用空 slot，初始化
│   │   → 若无空 slot → Debug.LogWarning("BuffSlot 已满")
│   │   → 刷新属性修正缓存
│   │   → 若有 VfxPrefab → 从池中 Spawn VFX 实例
│   │
│   ├── RemoveBuff(int buffId):
│   │   → 清除 slot，回收 VFX
│   │
│   ├── RemoveByTag(BuffTag tag):
│   │   → 遍历 _buffs + _dots，按 Tag 清除所有匹配项
│   │
│   ├── ApplyDot(DotConfigSO config):
│   │   → 查找同 DotId → 若有则刷新 Remaining
│   │   → 若无 → 占用空 DotSlot
│   │   → 若无空 slot → LogWarning
│   │
│   ├── float GetBulletCountModifier():
│   │   → 遍历活跃 BuffSlot，乘法累积 BulletCountModifier
│   │   → 返回结果（供 AttackComponent 查询）
│
├── Tick(float dt) 扩展：
│   → [已有] 遍历 BuffSlot 属性修正倒计时
│   → [新增] 遍历 DotSlot 持续伤害 tick：
│       slot.timer += dt
│       if (timer >= interval)
│           DamageDealer.DealDamageToEntity(owner, dotContext)
│           timer -= interval
│       slot.remaining -= dt
│       if (remaining <= 0) → 清除 DotSlot + 回收 VFX
│   → [已有] AttackInterval 乘积比率下限钳制 ≥ 0.3（即最快为基础攻速的 30%）
│     // PK-R1 UA-007：保留现有 MIN_ATTACK_INTERVAL_RATIO=0.3f
```

**BuffDamageModifier 桥接**（S3.2，但设计在此描述）：

```
BuffDamageModifier : IDamageModifier
├── Priority = 10（在 DamageRedirectModifier 之后）
├── ProcessDamage(ref DamageContext ctx, Entity target):
│   → float mod = target.BuffComponent.DamageTakenModifier
│   → ctx.FinalDamage = Mathf.RoundToInt(ctx.FinalDamage * mod)
│   → return true  // 继续链
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| A1 | Buff 施加 | ApplyBuff(SpeedUp) | BuffSlot 占用，MoveSpeed 修正生效 | 移速变化 |
| A2 | 同 ID 刷新 | 再次 ApplyBuff(SpeedUp) | Duration 刷新，不占新 slot | 槽位数不变 |
| A3 | 叠加模式 | ApplyBuff(Stack 模式) ×3 | CurrentStacks=3，属性递增 | 正确叠层 |
| A4 | 叠加上限 | 叠超 MaxStacks | CurrentStacks 不超 Max | 钳制正确 |
| A5 | Buff 到期 | 等待 Duration | Buff 自动清除，属性恢复 | VFX 回收 |
| A6 | Tag 清除 | RemoveByTag(Negative) | 所有减益清除，增益保留 | 按 Tag 过滤 |
| A7 | DamageTaken | 施加脆弱 Buff | 受伤伤害 ×2.0 | BuffDamageModifier 生效 |
| A8 | BulletCountMod | 施加火力全开 Buff | 基础攻击子弹数 ×2 | AttackComponent 查询正确 |
| A9 | 攻速钳制 | 多攻速 Buff 叠加 | AttackInterval ≥ 0.05s | 不突破安全钳 |
| A10 | 槽位满 | 施加 17 个不同 Buff | LogWarning 提示 | 不崩溃 |

---

### S3.2 BuffDamageModifier 桥接（1h）

> 设计见 S3.1，此处仅说明注册流程。

#### 实施方案

- 在 BattleController.InitBattle 中为**所有** Entity 注册 BuffDamageModifier（包括敌机——V2 Debuff 有 DamageTaken 修正）
- IDamageModifier 链顺序：InvincibilityModifier(-1) → DamageRedirectModifier(0) → BuffDamageModifier(10)
- 敌机不需要 InvincibilityModifier/DamageRedirectModifier，只注册 BuffDamageModifier

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| B1 | 敌机脆弱 | 对敌机施加脆弱(DamageTaken×2) | 受伤 ×2 | 数值正确 |
| B2 | 免伤 Buff | 对飞机施加护盾(DamageTaken×0) | 敌弹命中不扣血 | 免伤生效 |

---

### S3.3 7 种 Buff/Debuff SO 配置（2h）

#### 实施方案

创建 BuffConfigSO 资产（GDD §5.3）：

**我方 Buff**：

| 资产名 | BuffId | Tag | Duration | 属性修正 | VFX |
|--------|--------|-----|----------|---------|-----|
| SG_Buff_SpeedUp | 1001 | Positive | 5s | AttackInterval×0.5 | 蓝色发光 |
| SG_Buff_MoveUp | 1002 | Positive | 5s | MoveSpeed×1.5 | 尾焰变长 |
| SG_Buff_Shield | 1003 | Positive | 8s | DamageTaken×0 | 半透明球体 |
| SG_Buff_Berserk | 1004 | Positive | 3s | AttackInterval×0.3 + MoveSpeed×1.3 | 红色发光 |

**敌方 Debuff**（V2 预创建但无运行时触发路径）：

| 资产名 | BuffId | Tag | Duration | 属性修正 |
|--------|--------|-----|----------|---------|
| SG_Debuff_Slow | 3001 | Negative | 3s | MoveSpeed×0.5 |
| SG_Debuff_Vulnerable | 3002 | Negative | 4s | DamageTaken×2.0 |
| SG_Debuff_Blind | 3003 | Negative | 2s | AttackInterval×3.0 |

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| C1 | SO 完整 | Inspector 逐个检查 | 7 个 BuffConfigSO 字段正确 | 无 Missing |
| C2 | ID 范围 | T5 工具校验 | Buff 1001~1004 ∈ [1000,2999]，Debuff 3001~3003 ∈ [3000,3999] | 范围合规 |
| C3 | ID 唯一 | T5 工具校验 | 无重复 ID | 通过 |

---

### S3.4 3 种 DOT SO 配置 + 测试（2h）

#### 实施方案

创建 DotConfigSO 资产（GDD §6.2）：

| 资产名 | DotId | Damage/tick | Interval | Duration | 施加路径 |
|--------|-------|------------|----------|----------|---------|
| SG_Dot_Burn | 4001 | 5 | 0.5s | 3s | V3（无载体） |
| SG_Dot_Poison | 4002 | 3 | 1.0s | 5s | V3（无载体） |
| SG_Dot_Arc | 4003 | 8 | 0.3s | 1.5s | 激光技能附带 |

**激光技能 DOT 挂载**：
- SkillConfigSO (Laser) 新增 `AttachedDotConfig` 字段
- SG_Skill_Laser.AttachedDotConfig = SG_Dot_Arc
- 激光 Tick 命中时：首次命中施加 DOT，已有则不重复施加

**BulletPatternSO 扩展**（V3 准备）：
- 新增 `OnHitDotConfig : DotConfigSO`（V2 全部 null——无"特殊子弹"）

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| D1 | 电弧 DOT | 激光命中敌机 | 敌机每 0.3s 受 8 伤害，持续 1.5s | 总伤 ~40 |
| D2 | DOT 刷新 | 激光持续命中已有 DOT 的敌机 | Duration 刷新，不叠加 | 同 ID 刷新 |
| D3 | DOT 到期 | 等待 1.5s | DOT 自动清除，VFX 消失 | 清理干净 |
| D4 | 燃烧/中毒 | V2 无触发路径 | SO 资产存在但运行时无效果 | 预创建正确 |
| D5 | DotId 范围 | T5 校验 | 4001~4003 ∈ [4000,4999] | 通过 |

---

### S3.5 被动技能系统 + 4 种被动实现（4h）

#### 实施方案

**新增 `PassiveComponent`**（GDD §4.1）：

```
ComponentType 新增：Passive

PassiveComponent : IEntityComponent, ITickable
├── TickOrder = 60（在 Buff=50 之后——被动需要查询 Buff 状态）
├── PassiveAbilitySO[] _equipped
├── PassiveSlot[3] _slots
├── struct PassiveSlot:
│   ├── int passiveIndex
│   ├── float cooldownTimer
│   ├── float totalCooldown
│   ├── bool isActive          // 效果持续中
│   ├── float activeDuration   // 效果持续时间（从 LinkedBuff.Duration 读取）
│   ├── float activeTimer      // 效果计时
├── Init(Entity entity, PassiveAbilitySO[] equipped):
│   → 填充 _slots，初始化 CD
├── Tick(float dt):
│   → for each slot:
│       if (slot.isActive)
│           slot.activeTimer -= dt
│           if (activeTimer <= 0) slot.isActive = false  // Buff 自动到期处理
│       else
│           slot.cooldownTimer -= dt
│           if (cooldownTimer <= 0)
│               Activate(slot)
├── Activate(slot):
│   → PassiveAbilitySO ability = _equipped[slot.passiveIndex]
│   → ability.Activate(entity)
│   → slot.isActive = (ability.LinkedBuff != null)  // PA-04 即时型 → false
│   → slot.cooldownTimer = slot.totalCooldown
```

**4 种被动的 Activate 实现**：

| 被动 | Activate 逻辑 | 桥接 Buff |
|------|-------------|-----------|
| PA-01 穿透 | ApplyBuff(PierceBuffSO) → 标志位 HasActivePierce → 弹幕穿透+1 | Buff_Passive_Pierce (Duration=3s) |
| PA-02 暴击 | ApplyBuff(CritBoostBuffSO) → CritRate+20%, CritMultiplier=2.5x | Buff_Passive_Crit (Duration=4s) |
| PA-03 磁吸 | ApplyBuff(MagnetBuffSO) → PickupRadius = base × 2.0 | Buff_Passive_Magnet (Duration=3s) |
| PA-04 尾翼 | 直接 FireBulletsEffect（8 发环形弹幕）——不走 Buff | 无（即时型） |

**PA-04 特殊处理**：
- 触发条件：被命中时（CollisionEvent）+ CD 就绪
- 订阅 EntityEventBus.OnCollisionEvent
- 在碰撞事件级触发（先于 IDamageModifier 链）
- 无敌帧期间仍触发

**被动专属 Buff SO 资产**：

| 资产名 | BuffId | Duration | 效果 |
|--------|--------|----------|------|
| Buff_Passive_Pierce | 2001 | 3s | HasActivePierce=true |
| Buff_Passive_Crit | 2002 | 4s | CritRate+0.2, CritMultiplier=2.5 |
| Buff_Passive_Magnet | 2003 | 3s | PickupRadius×2.0 |

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| E1 | 穿透被动 | 装备穿透出击 | 每 5s 自动激活 3s 穿透窗口 | 子弹穿透 1 额外目标 |
| E2 | 暴击被动 | 装备暴击出击 | 每 8s 自动激活 4s 暴击窗口 | 伤害数字变大 |
| E3 | 磁吸被动 | 装备磁吸出击 | 每 6s 激活 3s 磁吸 | 拾取半径可见增大 |
| E4 | 尾翼反击 | 装备尾翼出击，被敌弹命中 | CD 就绪时发射 8 发环形弹 | 即时触发 |
| E5 | PA-04 CD | 尾翼触发后 5s 内再被命中 | 不触发（CD 中） | CD 正确 |
| E6 | 无敌帧 + PA-04 | 敌机碰撞后被弹命中 | PA-04 仍触发（碰撞事件级） | 先于伤害链 |
| E7 | 被动 CD UI | 观察被动栏 | 冷却→就绪→激活三态视觉正确 | 边框充能+呼吸+发光 |
| E8 | 3 被动并行 | 装备 3 个被动 | 各自独立 CD 运作 | 互不干扰 |

---

### S3.6 ID 冲突检测 + SO 验证 OnValidate（2h）

#### 实施方案

**T5 ID 冲突检测工具**：

```
[MenuItem("ShooterGame/Validate/Check ID Conflicts")]
static void CheckIdConflicts()
├── 扫描 Assets/ 下所有 BuffConfigSO
│   → 检查 BuffId 唯一性
│   → 检查 BuffId ∈ [1000, 3999]
├── 扫描所有 DotConfigSO
│   → 检查 DotId 唯一性
│   → 检查 DotId ∈ [4000, 4999]
├── 输出结果到 Console（Error/Warning）
├── 返回 bool 用于构建卡口
```

**SO OnValidate 实现**：每类 SO 按 GDD §9.5 约束表实现（见 SG_GDD_04_WORKFLOW §编辑态 SO 验证框架）。

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| F1 | ID 无冲突 | 运行菜单命令 | 全部通过 | Console 无 Error |
| F2 | 制造冲突 | 两个 Buff 同 ID | 检测出冲突 | Error 输出 |
| F3 | 范围越界 | BuffId=5000 | 检测出越界 | Error 输出 |
| F4 | OnValidate | SkillConfigSO.Effects=空 | Inspector 红色提示 | 即时反馈 |

---

## 2. Sprint 3 验收总表

### 功能验收

| # | 场景 | 预期 | 状态 |
|---|------|------|------|
| G1 | 4 种 Buff 道具 | 拾取各 Buff 道具效果正确 | ⬜ |
| G2 | Buff 到期清除 | Duration 后属性恢复 | ⬜ |
| G3 | DamageTaken 修正 | 护盾免伤 / 脆弱加伤 | ⬜ |
| G4 | BulletCount 修正 | 火力全开子弹数 ×2 | ⬜ |
| G5 | 攻速钳制 | 极端叠加不突破 0.05s | ⬜ |
| G6 | 电弧 DOT | 激光命中施加持续伤害 | ⬜ |
| G7 | 4 种被动 | 各被动独立 CD + 效果正确 | ⬜ |
| G8 | 被动 Buff 桥接 | 被动通过 Buff 实现，到期自动清除 | ⬜ |
| G9 | PA-04 碰撞触发 | 被命中时反击弹幕 | ⬜ |
| G10 | T5 ID 检测 | 菜单命令运行通过 | ⬜ |
| G11 | SO OnValidate | 错误配置即时标红 | ⬜ |

### 性能验收

| # | 指标 | 目标 | 工具 |
|---|------|------|------|
| P1 | BuffComponent.Tick | < 0.1ms（含 DOT） | Profiler |
| P2 | PassiveComponent.Tick | < 0.05ms | Profiler |
| P3 | 热路径零 GC | 0 bytes/frame | Deep Profile |

---

_创建于 2026-05-18 | Sprint 3 TDD v1.4_

**变更历史**：
- v1.0（2026-05-18）：初始版本
- v1.1（2026-05-18）：PK-R1 Unity 架构师回写
- v1.2（2026-05-18）：PK-R2 Unity 编辑器工具开发者回写
- v1.3（2026-05-18）：天命人决策——MAX_BUFFS 12→16
- v1.4（2026-05-19）：PK-R4 技术文档工程师回写（DE-004 DotSlot 8→16 统一扩容）
