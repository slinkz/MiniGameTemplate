---
system: shootergame-gdd
scope: passive-buff-dot
last_verified: 2026-05-19
depends_on: [SG_GDD_INDEX, SG_GDD_01_ACTIVE_SKILLS]
related_code: Assets/_Framework/EntitySystem/Components/Buff*, Passive*
---

# 四、被动技能设计

### 4.1 定义与机制

被动技能 = **有 CD 的自动增强效果**，CD 好了自动触发/生效（v1.6 更新）。

> **v1.6 设计变更**：被动技能从"无 CD 持续生效"改为"有 CD 周期性触发"。
> **理由**：
> 1. 有 CD = 有节奏感——被动效果"闪现-冷却-闪现"比"永远开着"更有戏剧性
> 2. CD 是调整杠杆——策划可以通过调 CD 平衡被动强度
> 3. 升级空间——付费/升级可以缩短 CD，构成成长曲线
> 4. UI 反馈更明确——被动触发有明确的视觉反馈时刻

**框架集成方案**（v1.6 更新）：

新增 `ComponentType.Passive` + `PassiveComponent`：

```
PassiveComponent : IEntityComponent, ITickable（v1.6 改为需要 Tick——CD 计时）
├── PassiveAbilitySO[] _equipped   // 战前装备的被动列表（最多3个）
├── PassiveSlot[3] _slots          // CD 计时器
├── Init(Entity):
│   → 遍历 _equipped
│   → 初始化每个 slot 的 CD 计时器
├── Tick(dt):
│   → 遍历 _slots
│   → cooldownTimer -= dt
│   → if (cooldownTimer <= 0):
│       → PassiveAbilitySO.Activate(entity)  // 触发效果
│       → cooldownTimer = PassiveAbilitySO.Cooldown  // 重置 CD
├── Reset():
│   → 每个 PassiveAbilitySO.Uninstall(entity)

struct PassiveSlot  // ~16 bytes
{
    int passiveIndex;      // 对应 _equipped 索引
    float cooldownTimer;   // 剩余 CD
    float totalCooldown;   // 总 CD（从 SO 读取）
    bool isActive;         // 当前是否在效果持续期内（有 duration 的被动）
}
```

**每种被动的具体机制**（v1.6 更新）：

| 被动 | 触发方式 | CD | 持续时间 | 效果 |
|------|---------|-----|---------|------|
| PA-01 穿透 | CD 好了自动激活 | 5s | 3s | 3s 内所有子弹穿透 1 个额外目标 |
| PA-02 暴击 | CD 好了自动激活 | 8s | 4s | 4s 内暴击率 +20%、暴击倍率 2.5x |
| PA-03 磁吸 | CD 好了自动激活 | 6s | 3s | 3s 内拾取半径 ×2 |

**被动效果执行路径**（v2.0 架构师 PK 新增）：

> **统一机制：被动通过 Buff 桥接实现**
>
> 所有有持续时间的被动，Activate 时**施加一个对应的 Buff**，由 BuffComponent 自动管理生命周期：
>
> | 被动 | Activate 实际操作 | 桥接 Buff | 影响属性 |
> |------|-------------------|-----------|---------|
> | PA-01 穿透 | ApplyBuff(PierceBuffSO) | 标志位 HasActivePierce → AttackComponent 发射时 pierceCount+1 | 弹幕穿透 |
> | PA-02 暴击 | ApplyBuff(CritBoostBuffSO) | CritRate/CritMultiplier 修正 | 暴击率/暴击伤害 |
> | PA-03 磁吸 | ApplyBuff(MagnetBuffSO) | entity.PickupRadius = base × 2.0 | 拾取半径 |
> | PA-04 尾翼 | 直接 FireBulletsEffect（即时型） | **不走 Buff**——无持续时间 | 弹幕发射 |
>
> **为什么走 Buff 桥接**：
> 1. 生命周期自动化——Buff 到期自动清除，PassiveComponent 不需要额外计时
> 2. 与"清除所有减益"机制一致——统一走 BuffComponent.Slot 管理
> 3. V3 可扩展——"反穿透"debuff 可直接走 Buff 叠加抵消

**PassiveConfigSO ↔ BuffConfigSO 引用关系**（v2.1 工具 PK 新增）：

> **1:1 专属关系**：每个被动技能持有一个专属 BuffConfigSO 引用。
>
> ```
> PassiveAbilitySO : ScriptableObject
> ├── ...（其他字段）
> ├── [SerializeField] BuffConfigSO LinkedBuff  // 1:1 专属引用
> ```
>
> **设计规则**：
> 1. 每个被动专属一个 Buff 实例（`Buff_Passive_{被动名}`）
> 2. 被动 Buff 的 `Duration` 填**被动的实际持续秒数**（如 PA-01 穿透=3s，PA-02 暴击=4s），由 BuffComponent 自动管理到期清除。**不填 0**——0 在 BuffConfigSO 约束表中表示"永久 Buff"，而被动 Buff 有明确的持续时间窗口。（v2.3 文档工程师 PK 修正——消除歧义）
> 3. 独立实例避免修改一个 Buff 影响多个系统
> 4. PA-04 尾翼反击为即时型，**不走 Buff**——无 LinkedBuff 字段（该字段允许为 null）
>
> **编辑器快捷工具**（T13）：
> - PassiveConfigSO Inspector 上 `[Button] CreateLinkedBuff`
> - 自动创建 `Buff_Passive_{被动名}` SO 资产到 `Configs/ShooterGame/Buffs/` 目录
> - 自动设置 Duration=0 + 自动反向引用
| PA-04 尾翼 | 被命中时触发（仍需 CD 就绪） | 5s | 即时 | 发射 8 发环形弹幕 |

> **PA-04 触发级别**（v2.0 架构师 PK 明确）：
>
> PA-04 的"被命中"定义为**碰撞事件**（EntityEventBus.OnCollisionEvent），**先于伤害管线执行**：
>
> ```
> 碰撞检测
>   → EntityEventBus.Publish(CollisionEvent)  ← PA-04 在这里监听并触发
>   → IDamageModifier 链执行  ← InvincibilityModifier 在这里可能拦截伤害
> ```
>
> **无敌帧期间 PA-04 仍然触发**——碰撞事件在伤害管线之前广播。设计理由：
> - 玩家视觉上看到"被打中"（弹丸消失）→ 反击弹幕发射 = 因果一致
> - 无敌帧仅由敌机碰撞触发，PA-04 更多被敌弹触发——两者窗口很少重叠
> - 即使重叠，0.5s 无敌帧 + 5s CD → 最多一次 PA-04，收益可控

> **被动 CD 的 UI 表达**（v1.7 更新——与主动 CD 做视觉区隔）：
> - ❌ 不用圆形 CD 扇形遮罩（那是主动技能的语言）
> - ✅ **边框渐变充能**：图标外圈有一圈边框，冷却时从灰色→逐渐亮起品牌色（顺时针充能动画）
> - 三态区分：
>   - **冷却中**：图标变暗 50% + 边框灰色渐充
>   - **就绪（等待触发）**：图标全亮 + 边框品牌色常亮 + 轻微呼吸缩放（1.0→1.03 循环）
>   - **激活持续中**：图标全亮 + **绿色环形进度条消耗**（不放倒计时文字）+ 发光脉冲（PK-R3 UID-003 变更：环形进度消耗代替数字倒计时，更精致且减少低端机文字渲染压力）
> - 尺寸：**40×40pt 方形圆角(r=8)**（PK-R3 UID-003: 32→40pt，方形圆角与技能圆形形成差异化）
> - PA-04 尾翼（即时型）：就绪后无"激活持续"态，触发瞬间图标闪白 0.3s 后直接进入冷却

> **PA-04 触发时序编排**（v1.8 UI PK 新增——解决受伤/反击情感冲突）：
>
> PA-04 触发时与受伤反馈同帧发生，需定义时序以建立"被打→反击"的因果叙事：
>
> | 时间点 | 事件 | 情感信号 |
> |--------|------|---------|
> | T+0.00s | 受伤红闪触发（0.2s） | 😰 惩罚——"被打了！" |
> | T+0.15s | PA-04 图标闪白 + "反击！"通知条弹出 | 😤 翻转——"但我有反击！" |
> | T+0.20s | 8 发环形弹幕发射（受伤红闪结束同时） | 😎 奖励——"打回去了！" |
>
> **设计意图**：先惩罚(0.15s) → 再奖励(弹幕爆发) = "被打但马上有回报"的心理模型。延迟 0.15s 让玩家先感知"受伤"，再看到"反击发动"——情感翻转比同时混合更有力。

### 4.2 V2 被动技能清单（4 种）

| ID | 名称 | CD | 持续 | 效果 | 获取方式 | 玩家感受 |
|----|------|-----|------|------|---------|---------|
| PA-01 | 子弹穿透 | 5s | 3s | 激活期间子弹穿透 1 个额外目标 | 战前装备（默认解锁） | 穿透闪现，效率爆发 |
| PA-02 | 暴击强化 | 8s | 4s | 激活期间暴击率 +20%、暴击倍率 2.5x | 战前装备（第 3 关解锁） | 暴击窗口期，抓住打大数字 |
| PA-03 | 磁吸范围 | 6s | 3s | 激活期间道具拾取范围 ×2 | 战前装备（第 5 关解锁） | 磁铁时刻，道具自动飞来 |
| PA-04 | 尾翼反击 | 5s | 即时 | CD 就绪时被命中自动发射 8 发环形弹幕 | 战前装备（累计被命中 30 次解锁） | 挨打也有反击，减少挫败感 |

> **V2 被动 CD 调整参考**：CD 短+持续短 = 频繁但短暂的增强；CD 长+持续长 = 稀有但持久的增强。所有 CD 值为 `[占位符]`，待 playtest 调整。

### 4.3 被动技能叠加规则

| 规则 | 说明 |
|------|------|
| 最多 3 个被动（V2） | 战前选择最多 3 个已解锁的被动 |
| 同名不叠加 | 每种被动只能装备 1 个 |
| 有 CD 周期触发（v1.6） | CD 好了自动激活，持续一段时间后进入冷却 |
| 跨关卡保留 | 装备配置在出战前设定，通关不重置（与技能一起组成 build） |

> **设计意图**：战前构建 build（最多 6 技能 + 最多 3 被动）→ 形成"构筑乐趣"→ 鼓励解锁更多选项。

---

# 五、Buff 系统设计

### 5.1 架构（基于现有 BuffComponent）

现有 BuffComponent 已经实现了核心功能：
- ✅ 16 槽位固定数组，零 GC（v2.2 扩容：8→16）
- ✅ 同 ID 刷新（不叠层）
- ✅ 3 种属性修正（MoveSpeed / AttackInterval / DamageTaken）
- ✅ 与 Movement/Attack 的交互（push/pull）

**V2 需要扩展的能力**：

| 扩展项 | 现状 | V2 目标 |
|--------|------|---------|
| DamageTaken 接入 | BuffComponent 计算了值但 HealthComponent 未消费 | 新增 BuffDamageModifier 桥接到 IDamageModifier 链 |
| Buff 视觉反馈 | 无 | 新增 BuffVFX 字段（发光/变色/粒子） |
| Buff 标签（Tag） | 无 | 新增 BuffTag 用于分类清除（如"清除所有减益"） |
| 叠层 Buff | 同 ID 刷新 | 支持可选叠层模式（最大层数 + 属性递增） |

### 5.2 Buff 分类

| 分类 | BuffTag | 作用对象 | 举例 |
|------|---------|---------|------|
| 增益（Buff） | Positive | 玩家飞机 | 攻速+、移速+、护盾 |
| 减益（Debuff） | Negative | 敌机 | 减速、脆弱（受伤+） |
| 状态（Status） | Status | 任何 | 燃烧（见 DOT）、冰冻 |
| 光环（Aura） | Aura | 范围内敌人 | 减速光环（V3 扩展） |

### 5.3 V2 Buff 清单

#### 我方 Buff（道具拾取获得）

| BuffId | 名称 | Tag | 持续时间 | 效果 | 视觉 |
|--------|------|-----|---------|------|------|
| 1001 | 疾风 | Positive | 5s | AttackInterval ×0.5（攻速翻倍） | 飞机蓝色发光 |
| 1002 | 加速引擎 | Positive | 5s | MoveSpeed ×1.5 | 飞机尾焰变长 |
| 1003 | 能量护盾 | Positive | 8s | DamageTaken ×0（免伤） + 视觉护盾球 | 半透明蓝色球体 |
| 1004 | 暴走 | Positive | 3s | AttackInterval ×0.3 + MoveSpeed ×1.3 | 飞机红色发光 + 尾焰 |

#### 敌方 Debuff（由玩家技能/特殊子弹施加）

| BuffId | 名称 | Tag | 持续时间 | 效果 | 视觉 | V2 施加入口 |
|--------|------|-----|---------|------|------|------------|
| 3001 | 减速 | Negative | 3s | MoveSpeed ×0.5 | 蓝色冰霜粒子 | ⚠️ V3（无施加载体） |
| 3002 | 脆弱 | Negative | 4s | DamageTaken ×2.0 | 红色破碎边缘 | ⚠️ V3（无施加载体） |
| 3003 | 致盲 | Negative | 2s | AttackInterval ×3.0（射速骤降） | 暗色光环 | ⚠️ V3（无施加载体） |

> **⚠️ V2 Debuff 施加入口说明**（v2.3 文档工程师 PK 新增）：
>
> V2 六种玩家技能均无"对敌施加 Debuff"的设计——所有技能效果是直接伤害或自身 Buff：
> - SK-P01~P03：FireBulletsEffect（直接伤害）
> - SK-P04~P05：ApplyBuffToSelfEffect（自身 Buff）
> - SK-P06：DealAreaDamageEffect（直接伤害+击退）
>
> **Debuff 施加入口在 V3 引入**，预期载体：
> - 冰冻弹技能（V3 新增）→ 施加减速(3001)
> - 脆弱标记技能（V3 新增）→ 施加脆弱(3002)
> - 闪光弹技能（V3 新增）→ 施加致盲(3003)
>
> **V2 开发者动作**：BuffConfigSO 资产（SG_Debuff_Slow / SG_Debuff_Vulnerable / SG_Debuff_Blind）可预创建以便工具验证 ID 范围合规，但运行时无触发路径。

### 5.4 BuffConfigSO 扩展设计

```
BuffConfigSO（V2 扩展——仅管属性修正，不含 DOT）
├── [现有] BuffId / DisplayName / Duration
├── [现有] MoveSpeedModifier / AttackIntervalModifier / DamageTakenModifier
├── [新增] BulletCountModifier : float (默认=1.0, 火力全开=2.0)（v2.0 架构师 PK）
├── [新增] BuffTag : enum { Positive, Negative, Status, Aura }
├── [新增] StackMode : enum { Refresh, Stack }
├── [新增] MaxStacks : int (仅 Stack 模式, 默认 1)
├── [新增] StackBonusPerLayer : float (每层额外修正, 乘法)
├── [新增] VfxPrefab : PoolDefinition (Buff 视觉特效, 可选)
├── [新增] IconSprite : Sprite (UI 图标, 可选)
├── [新增] Description : string (策划填写, UI 悬浮提示用)
```

> **BulletCountModifier 说明**（v2.0 架构师 PK 新增）：
> - 仅对 **AttackComponent（基础攻击）** 生效——SkillComponent 不查询此字段
> - `AttackComponent.Tick()` 发射时：`int count = Mathf.RoundToInt(baseCount × entity.BuffComponent.BulletCountModifier)`
> - SK-P04 火力全开的 BuffConfigSO 填 `BulletCountModifier = 2.0` + `AttackIntervalModifier = 0.5`
> - P5 支柱校验通过：策划改 SO 即可，不需要写代码

> **Buff 乘法叠加上限**（v2.0 架构师 PK 新增）：
> - `AttackIntervalModifier` 的乘积结果不低于 **0.05s**（即攻速上限 20 发/秒）
> - 防止多个攻速 Buff 叠加导致弹幕预算溢出
> - 极端验证：火力全开(×0.5) + 疾风(×0.5) = 0.0625s → 16 发/s × 2(count) × 3s = 96 弹丸 << 1024 上限 ✅

> **V3 重构触发条件**（v2.0 架构师 PK 新增）：
> 当 BuffConfigSO 属性修正字段超过 **12 个**时，重构为 `BuffModifierEntry[]` + 自定义 Inspector。
> V2 共 4 个属性修正字段（MoveSpeed / AttackInterval / DamageTaken / BulletCount），数量可控。

**DOT 独立配置**（v1.4 PK 新增——职责分离：Buff 管属性修正，DOT 管持续伤害）：

```
DotConfigSO : ScriptableObject
├── DotId : int
├── DisplayName : string
├── Damage : int (每次 tick 伤害)
├── Interval : float (tick 间隔, 秒)
├── Duration : float (总持续时间)
├── DamageType : DamageType (Physical/Magical/Pure)
├── VfxPrefab : PoolDefinition (DOT 视觉特效)
├── Tag : BuffTag (用于"清除所有减益"操作)
```

**BuffComponent 内部存储**（v1.4 PK 修正）：

```
BuffComponent 内部：
  BuffSlot[16] _buffs    // ~40 bytes/slot，仅属性修正（v2.2 扩容：8→16，超限 LogWarning 提示扩容）
  DotSlot[16] _dots      // ~20 bytes/slot，仅持续伤害（v2.2 扩容：4→16，超限 LogWarning 提示扩容）

struct DotSlot  // ~20 bytes
{
    int dotId;
    int damage;
    float interval;
    float timer;
    float remaining;
}

"清除所有减益"操作：遍历 _buffs + _dots，按 tag 清除
```

---

# 六、DOT 系统设计

### 6.1 什么是 DOT

DOT（Damage Over Time）= **持续伤害**。两种形态：

| 形态 | 定义 | 举例 |
|------|------|------|
| **状态 DOT** | 附着在 Entity 上，每隔 N 秒扣血 | 燃烧、中毒 |
| **区域 DOT** | 存在于场地上，进入范围就扣血 | 毒雾、岩浆、电弧地面 |

### 6.2 状态 DOT（附着型）

**实现方案**（v1.4 PK 修正 | v2.2 扩容）：DOT 独立于 Buff，使用 `DotConfigSO` + `DotSlot[16]` 独立数组。

```
BuffComponent.Tick(dt)
  → 遍历 BuffSlot（属性修正倒计时）
  → 遍历 DotSlot（持续伤害 tick）：
      slot.timer += dt
      if (timer >= interval)
          → DamageDealer.DealDamageToEntity(owner, dotContext)
          → timer -= interval
      slot.remaining -= dt
      if (remaining <= 0) → 清除此 DotSlot
```

> **职责分离**（v1.4 PK 确认）：BuffSlot 只管属性修正，DotSlot 只管持续伤害。两者各自独立数组，清除减益时按 Tag 统一扫描。

#### V2 状态 DOT 清单

| BuffId | 名称 | 每 tick 伤害 | 间隔 | 持续 | 施加方式 | 视觉 |
|--------|------|-------------|------|------|---------|------|
| 4001 | 燃烧 | 5 | 0.5s | 3s | 特殊子弹命中 | 红色火焰粒子 |
| 4002 | 中毒 | 3 | 1.0s | 5s | 特殊子弹命中 | 绿色毒雾粒子 |
| 4003 | 电弧 | 8 | 0.3s | 1.5s | 激光技能附带 | 蓝白电弧粒子 |

**DOT 施加机制详解**（v2.3 文档工程师 PK 新增）：

> **"特殊子弹"定义**：指 BulletPatternSO 中 `OnHitDotConfig` 字段非 null 的子弹。
>
> **施加路径**：
>
> ```
> 路径 A：特殊子弹命中（燃烧/中毒）
>   BulletPatternSO
>   ├── [新增] OnHitDotConfig : DotConfigSO  // null = 普通子弹，非 null = 特殊子弹
>   └── 碰撞时：
>       → DamageDealer.DealDamage(target, context)  // 正常伤害
>       → if (bullet.OnHitDotConfig != null)
>           → target.BuffComponent.ApplyDot(OnHitDotConfig)  // 施加 DOT
>
> 路径 B：激光技能附带（电弧）
>   SkillConfigSO (SkillType=Laser)
>   ├── [新增] AttachedDotConfig : DotConfigSO  // null = 无附带 DOT
>   └── 激光 Tick 命中时：
>       → DamageDealer.DealDamage(target, laserContext)  // 正常 Tick 伤害
>       → if (AttachedDotConfig != null && !target.HasDot(dotId))
>           → target.BuffComponent.ApplyDot(AttachedDotConfig)  // 首次命中施加
> ```
>
> **V2 配置对照**：
> | DOT | 施加路径 | 配置位置 |
> |-----|---------|---------|
> | 燃烧(4001) | 路径 A | V3 特殊子弹 Pattern（V2 无载体——见下方说明） |
> | 中毒(4002) | 路径 A | V3 特殊子弹 Pattern（V2 无载体——见下方说明） |
> | 电弧(4003) | 路径 B | SG_Skill_Laser.AttachedDotConfig = SG_Dot_Arc |
>
> **⚠️ V2 实现范围说明**：
> - **电弧(4003)**：V2 实现。激光技能已存在，AttachedDotConfig 字段直接挂载。
> - **燃烧(4001)/中毒(4002)**：V2 实现 DotSlot + ApplyDot 基础能力，但**无施加入口**——V2 六种技能无"火属性/毒属性子弹"。实际施加入口在 V3 新技能（火焰喷射/毒弹等）中引入。
> - **开发者动作**：V2 Sprint 3 实现 BuffComponent.ApplyDot() + DotSlot 核心逻辑 + 电弧 DOT。燃烧/中毒的 DotConfigSO 资产可预创建但运行时无触发。

### 6.3 区域 DOT（场地型）

**实现方案**：DOT Zone 作为独立 Entity，有位置无移动，Tick 中对范围内敌人造伤。

```
DOTZone Entity
├── Components: [State]  // 最小配置
├── ConfigSO 扩展:
│   ├── DotZoneDamage : int
│   ├── DotZoneInterval : float
│   ├── DotZoneRadius : float
│   ├── DotZoneLifetime : float
│   └── DotZoneTargetCamp : EnumCamp
```

**或者更轻量**：作为新的 ISkillEffect 实现——`SpawnDotZoneEffect`，技能触发时在施法位置生成一个 DOT Zone Entity。

#### V2 区域 DOT 清单

| 名称 | 半径 | 伤害/tick | 间隔 | 持续 | 触发方式 | 视觉 |
|------|------|----------|------|------|---------|------|
| 火焰地毯 | 2.0 | 8 | 0.5s | 4s | 玩家技能 SK-P07（V3） | 地面火焰动画 |
| 毒雾区 | 2.5 | 3 | 1.0s | 6s | 特殊敌机死亡时释放 | 绿色扩散雾 |
| 电弧陷阱 | 1.5 | 10 | 0.3s | 2s | Boss 技能（V3） | 闪电柱 |

> **V2 优先级**：状态 DOT（燃烧/中毒）先做，区域 DOT 在 V3 Boss 战再做。
>
> **V2 不预留区域 DOT 接口**（v2.0 架构师 PK 确认）：
> - "毒雾区——特殊敌机死亡时释放"描述的是 V3 Boss 小弟或特殊精英敌机
> - V2 五种敌机均不需要 OnDeathEffect 字段
> - V2 不在 EntityConfigSO 上预留 `OnDeathEffect`——遵循 YAGNI 原则
> - V3 加 Boss 时再按需增加此字段
