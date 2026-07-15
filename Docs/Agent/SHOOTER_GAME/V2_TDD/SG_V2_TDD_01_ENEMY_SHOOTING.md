---
system: shootergame-v2-tdd
scope: sprint1-enemy-shooting-collision
last_verified: 2026-05-19
version: v1.4
depends_on: [SG_V2_TDD_INDEX, SG_GDD_01_ACTIVE_SKILLS, SG_GDD_06_ROADMAP]
related_code: Assets/_Framework/EntitySystem/**, Assets/_Framework/DanmakuSystem/**, Assets/_Game/Scripts/ShooterGame/Core/*
---

# Sprint 1：敌方射击 + 基础碰撞（~10h）

> **目标**：最小可玩增量——敌机能射击、敌弹与飞机/基地产生碰撞与伤害转发。

---

## 1. 实施任务分解

### S1.1 敌方子弹 Entity 配置 + BulletPatternSO（2h）

#### 实施方案

**新增 SO 资产**：

| 资产 | 类型 | 路径 | 关键配置 |
|------|------|------|---------|
| SG_EnemyBullet_Straight | BulletPatternSO | `Configs/ShooterGame/Bullets/` | Speed=5, BulletCount=1, Direction=Down, Camp=Enemy |
| SG_EnemyBullet_Spread3 | BulletPatternSO | `Configs/ShooterGame/Bullets/` | Speed=4.5, BulletCount=3, SpreadAngle=30°, Camp=Enemy |
| SG_EnemyBullet_Homing | BulletPatternSO | `Configs/ShooterGame/Bullets/` | Speed=4, IsHoming=true, Camp=Enemy |

**新增 BulletTypeSO**：

| 资产 | 视觉规格 |
|------|---------|
| SG_BulletType_EnemyStraight | 红/橙色圆形, 0.2 世界单位, 长拖尾+发光 |
| SG_BulletType_EnemySpread | 红/橙色小圆, 0.18 世界单位 |
| SG_BulletType_EnemyHoming | 红色菱形, 0.22 世界单位, 追踪拖尾 |

**代码变更**：无新 C# 代码——复用 V1 已有的 BulletPatternSO + BulletTypeSO + BulletWorld 能力。关键是 Camp=Enemy 标记，弹幕系统已支持阵营碰撞矩阵。

**关键约束**：
- 敌弹子弹参数：Speed=[4, 6], Lifetime=3s（GDD §3.2 确认）
- 敌弹视觉必须与我方子弹明显区分（颜色红/橙 vs 蓝/青，大小更大）
- **FirstAttackDelay 起算时刻**（PK-R3 UID-010 | PK-R4 DE-005 措辞修正）：FirstAttackDelay 是**每只敌机各自独立**的计时器，从该敌机 Spawn 时刻开始计算。注：因为 EntitySpawner 在 `BattleStartSequence` 结束（Time.timeScale=1）后才启动（`EntitySpawner.StartSpawning()`），第一波的 Spawn 不会发生在 timeScale=0 期间，故 FirstAttackDelay 不会在 BattleStartSequence 中空跑。

#### 验收方案

| # | 验收项 | 操作步骤 | 预期结果 | PASS 标准 |
|---|--------|---------|---------|-----------|
| A1 | SO 资产完整 | Project 窗口检查 `Configs/ShooterGame/Bullets/` | 3 个 BulletPatternSO + 3 个 BulletTypeSO 存在 | 无 Missing Reference |
| A2 | 敌弹视觉区分 | Scene View 或 Play Mode 观察 | 敌弹红/橙色，明显大于我方蓝色弹 | 0.2s 内可辨识 |
| A3 | 弹幕预算 | Profiler 检查 BulletWorld count | 同屏敌弹 ≤ 30 | 不超 1024 上限 |

---

### S1.2 敌机射击配置（3h）

#### 实施方案

**新增 `EnemyShootComponent`**：

```
ComponentType 枚举新增：EnemyShoot（需确认 ComponentType 枚举还有空位）

EnemyShootComponent : IEntityComponent, ITickable
├── TickOrder = 150（在 Movement=100 之后，Attack=200 之前）
├── 字段：
│   ├── BulletPatternSO _pattern       // 从 EntityConfigSO 读取
│   ├── float _cooldown                // 射击 CD
│   ├── float _cooldownTimer           // 当前 CD 计时
│   ├── float _firstFireDelay          // 首次开火延迟（≥1.0s）
│   ├── float _firstFireTimer          // 首次开火计时
│   ├── bool _hasFirstFired            // 是否已首次开火
│   └── EnumCamp _camp = EnumCamp.Enemy
├── Init(Entity entity):
│   → _pattern = entity.Config.ShootPattern
│   → _cooldown = entity.Config.ShootCooldown
│   → _firstFireDelay = entity.Config.FirstFireDelay
│   → _firstFireTimer = 0
│   → _hasFirstFired = false
├── Tick(float dt):
│   → if (!_hasFirstFired)
│       _firstFireTimer += dt
│       if (_firstFireTimer >= _firstFireDelay)
│           _hasFirstFired = true; Fire(); _cooldownTimer = _cooldown
│       return
│   → _cooldownTimer -= dt
│   → if (_cooldownTimer <= 0)
│       Fire(); _cooldownTimer = _cooldown
├── Fire():
│   → DanmakuSystem.Instance.FireBullets(_pattern, entity.Position, -90f, entity.Id.Value)
│   // baseAngle=-90° = 向下（角度制）; Camp 由 BulletPatternSO.Faction 配置
```

**数据来源**（PK-R1 UA-005/011 修正）：
- `_pattern` = `entity.ConfigSO.AttackBulletPattern`（复用已有字段）
- `_cooldown` = `entity.ConfigSO.AttackInterval`（复用已有字段）
- `_firstFireDelay` = `entity.ConfigSO.FirstAttackDelay`（V2 新增，策划可配置）

**EntityConfigSO 扩展**：

```
EntityConfigSO 新增字段（可选，射击敌机专用）：
├── [SerializeField] BulletPatternSO ShootPattern   // null = 不射击
├── [SerializeField] float ShootCooldown = 1.5f
├── [SerializeField] float FirstFireDelay = 1.0f    // 首次开火延迟
```

**新增/修改敌机 EntityConfigSO 资产**：

| 资产 | ShootPattern | ShootCooldown | FirstFireDelay | HP | MoveSpeed |
|------|-------------|---------------|----------------|-----|-----------|
| SG_Enemy_Shooter | SG_EnemyBullet_Straight | 1.5s | 1.0s | 40 | 1.5 |
| SG_Enemy_Spreader | SG_EnemyBullet_Spread3 | 2.5s | 1.5s | 60 | 1.2 |
| SG_Enemy_Elite | SG_EnemyBullet_Homing | 3.0s | 0.8s | 120 | 1.0 |

**ComponentType 注册**：
- 确认 `ComponentType` enum 有空位加入 `EnemyShoot`
- 在 EntityPool/EntityManager 中注册此组件类型
- 在 EntitySystemBootstrap 中确保组件自动初始化

#### 验收方案

| # | 验收项 | 操作步骤 | 预期结果 | PASS 标准 |
|---|--------|---------|---------|-----------|
| B1 | 射手机射击 | Play Mode，观察射手机 | 进入屏幕 ≥1.0s 后开始每 1.5s 发射一发直射弹 | CD 误差 ±0.1s |
| B2 | 散射机射击 | Play Mode，观察散射机 | 进入屏幕 ≥1.5s 后每 2.5s 发射 3 发扇形弹 | 扇形角度 ~30° |
| B3 | 精英机射击 | Play Mode，观察精英机 | 进入屏幕 ≥0.8s 后每 3.0s 发射追踪弹 | 追踪弹朝玩家飞机方向偏转 |
| B4 | 首次开火延迟 | 计时从该敌机 Spawn 时刻到首次射击 | 各类型满足各自 FirstFireDelay | 玩家能看到敌机后再被射击（PK-R4 DE-005 措辞修正） |
| B5 | 不射击的敌机 | 观察普通机、快速机 | 无射击行为 | ShootPattern=null，组件不初始化 |
| B6 | 移动中射击 | 观察射手机是否边移动边射击 | 移动不停止，射击不影响移动 | 两个组件独立 Tick |

---

### S1.3 敌弹 vs 飞机碰撞 → 伤害转发（2h）

#### 实施方案

**新增 `InvincibilityComponent`**：

```
InvincibilityComponent : IEntityComponent, ITickable
├── TickOrder = 10（最早，在伤害判定之前更新状态）
├── 字段：
│   ├── bool IsInvincible => _timer > 0
│   ├── float _timer = 0
│   ├── float _duration = 0.5f
├── Trigger():
│   → _timer = _duration
├── Tick(float dt):
│   → if (_timer > 0) _timer -= dt
│   → if (_timer < 0) _timer = 0
```

**新增 `InvincibilityModifier : IDamageModifier`**：

```
InvincibilityModifier : IDamageModifier
├── Priority = -1（最高优先级）
├── InvincibilityComponent _invComp
├── SetInvincibilityComponent(InvincibilityComponent comp)
├── ProcessDamage(ref DamageContext context, Entity target):
│   → if (_invComp != null && _invComp.IsInvincible)
│       return false  // 中断链，伤害归零
│   → return true     // 继续
```

**新增 `DamageRedirectModifier : IDamageModifier`**：

```
DamageRedirectModifier : IDamageModifier
├── Priority = 0
├── Entity _baseEntity
├── SetBaseEntity(Entity baseEntity)
├── ProcessDamage(ref DamageContext context, Entity target):
│   → if (_baseEntity == null) return true  // 防御性检查
│   → var adjustedContext = new DamageContext {
│       FinalDamage = context.FinalDamage,  // 固定值 5~10
│       SourceId = context.SourceId
│   }
│   → _baseEntity.HealthComponent.TakeDamage(adjustedContext)
│   → return false  // 中断链——飞机自身不扣血
```

**BattleController 注入流程**：

```
BattleController.InitBattle():
  (1) Spawn 基地 Entity → baseEntity
  (2) Spawn 玩家飞机 Entity → playerEntity
  (3) 获取/添加 InvincibilityComponent → invComp
  (4) 创建 InvincibilityModifier → 设置 invComp
  (5) 创建 DamageRedirectModifier → 设置 baseEntity
  (6) 将两个 Modifier 注册到飞机的 IDamageModifier 链
       排序：InvincibilityModifier(priority=-1) → DamageRedirectModifier(priority=0)
  (7) 初始化 EntitySpawner
  (8) 第一帧 Tick 开始
```

**碰撞系统桥接**（PK-R1 UA-006 确认）：
弹幕系统阵营碰撞矩阵——Enemy 弹丸只与 Player 阵营目标碰撞（`CollisionSolver.ShouldCollide`）。
桥接路径：`CollisionSolver → PlayerCollisionTarget.OnBulletHit(damage, idx) → _onPlayerHit 回调`
→ BattleController 在回调中构造 `DamageContext{BaseDamage=damage, HitType=BulletHit}`
→ `playerEntity.EventBus.Publish(new OnCollisionHit{Context=ctx})`（PA-04 订阅此事件）
→ `playerEntity.HealthComponent.TakeDamage(ref ctx)` → IDamageModifier 链
不需要修改 ICollisionTarget 接口——利用现有 `DanmakuSystem.SetPlayer()` 的 _onPlayerHit 回调机制。

#### 验收方案

| # | 验收项 | 操作步骤 | 预期结果 | PASS 标准 |
|---|--------|---------|---------|-----------|
| C1 | 敌弹命中飞机 | Play Mode，让敌弹击中飞机 | 飞机不受伤，基地扣血（5~10） | 基地 HP 减少，飞机无变化 |
| C2 | 敌弹命中基地 | 让敌弹穿过飞机到达底线 | 基地直接扣血（8~15） | 底线检测触发 |
| C3 | 碰撞优先级 | 飞机与基地重叠时敌弹命中 | 飞机优先拦截（扣 5 < 基地扣 8） | First-Hit-Wins |
| C4 | 弹丸消失 | 敌弹命中后 | 弹丸消失，不继续飞行 | 一弹一命中 |
| C5 | Modifier 链 | 断点检查 | InvincibilityModifier → DamageRedirectModifier 顺序执行 | Priority 排序正确 |

---

### S1.4 敌弹 vs 基地碰撞（底线检测）（1h）

#### 实施方案

V1 已有 `BaseLineDetector`（纯 C#，扫描敌机越线扣基地 HP）。V2 需要扩展：

**BaseLineDetector 扩展**：
- 现有逻辑：敌机越过底线 → 基地扣血 → 敌机消失
- V2 无需修改此逻辑——敌弹是弹幕系统的 Bullet，不是 Entity
- 敌弹到达屏幕底部：由弹幕系统 Lifetime 自然销毁（3s 足够穿屏）
- 敌弹碰撞基地：走弹幕系统碰撞检测（Camp=Enemy vs Camp=Player 基地碰撞体）

**需确认**：基地 Entity 是否有碰撞体可被弹幕系统检测到。
- 若有：无需新增代码——弹幕碰撞自动走 DamageDealer
- 若无：需为基地 Entity 添加碰撞组件，注册到弹幕碰撞系统

**基地碰撞参数**：
- 碰撞体类型：矩形（全屏宽度 × 底部区域高度）
- 敌弹命中基地伤害：8~15（GDD 确认的固定值）

#### 验收方案

| # | 验收项 | 操作步骤 | 预期结果 | PASS 标准 |
|---|--------|---------|---------|-----------|
| D1 | 敌弹命中基地 | 让敌弹飞到底线区域 | 基地扣血 8~15，弹丸消失 | HP 数值正确 |
| D2 | 敌机越线 | 敌机穿过底线 | 基地扣血 15，敌机消失（V1 逻辑不变） | 与 V1 行为一致 |
| D3 | 敌机碰撞飞机 | 敌机与飞机碰撞 | 基地扣血 10~15 + 敌机死亡 + 屏幕震动 + 0.5s 无敌帧 | 全部副作用触发 |

---

### S1.5 关卡 3~5 编排加入射手机（2h）

#### 实施方案

**修改波次配置**：

| 关卡 | 波次 | 新增敌机 | 说明 |
|------|------|---------|------|
| 3 | Wave 3-5 | 射手机 ×1~2 | 首次出现射击敌机 |
| 4 | Wave 3-6 | 射手机 ×2 + 散射机 ×1 | 多类型弹幕 |
| 5 | Wave 5-8 | 射手机 ×2 + 散射机 ×1 + 精英机 ×1 | 全类型 |

**修改方式**：编辑对应的 EntitySpawnWaveSO 资产（Luban xlsx 或 SO），在指定波次的敌机列表中加入新敌机类型。

**编排原则**（来自 GDD §难度曲线）：
- 关卡 3 = "首次挑战"——射手机首次出现，玩家被弹打到的惊讶
- 关卡 4 = "多维压力"——散射弹幕覆盖面大
- 关卡 5 = "英雄时刻"——全类型混合

#### 验收方案

| # | 验收项 | 操作步骤 | 预期结果 | PASS 标准 |
|---|--------|---------|---------|-----------|
| E1 | 关卡 3 射手机 | Play Mode 打到关卡 3 | Wave 3+ 出现射手机，边下落边射击 | 存在且行为正确 |
| E2 | 关卡 4 散射机 | Play Mode 打到关卡 4 | 出现散射机，3 发扇形弹 | 弹幕形态正确 |
| E3 | 关卡 5 精英机 | Play Mode 打到关卡 5 | 出现精英机，追踪弹 | 追踪弹有效 |
| E4 | 难度递进 | 通关 1~5 观察 | 射击敌机比例随关卡递增 | 压力感递增 |
| E5 | 关卡 1-2 无射击 | Play Mode 打前两关 | 无射击敌机 | V1 行为不变 |

---

## 2. 新增代码文件清单

| 文件路径 | 类型 | 说明 | 实现状态 |
|---------|------|------|---------|
| `EntitySystem/Scripts/Components/EnemyShootComponent.cs` | 新增 | 敌机射击组件 | ✅ Sprint 1 |
| `EntitySystem/Scripts/Components/InvincibilityComponent.cs` | ~~新增~~ | ~~无敌帧组件~~ | ⏭️ 复用 HealthComponent 内置 IFrame |
| `EntitySystem/Scripts/Components/InvincibilityModifier.cs` | 新增 | 无敌帧伤害修正器（IDamageModifier） | ✅ Sprint 1 |
| `EntitySystem/Scripts/Components/DamageRedirectModifier.cs` | 新增 | 伤害转发修正器（IDamageModifier） | ✅ Sprint 1 |
| `EntitySystem/Scripts/Config/EntityConfigSO.cs` | 修改 | 新增 FirstAttackDelay 字段 | ✅ Sprint 1 |
| `EntitySystem/Scripts/Core/ComponentType.cs` | 修改 | 新增 EnemyShoot=11 枚举值 | ✅ Sprint 1 |
| `EntitySystem/Scripts/Core/EntityPool.cs` | 修改 | 工厂注册 EnemyShoot | ✅ Sprint 1 |
| `ShooterGame/Core/BattleController.cs` | 修改 | SetupDamageRedirectChain + 碰撞事件订阅 | ✅ Sprint 1 |

**实现备注**：
- `InvincibilityComponent`（独立组件）被合理省略——`HealthComponent` 内置 `_iFrameRemaining` 机制已满足需求
- `InvincibilityModifier` 直接读取 `HealthComponent.IsInvincible`，作为防御性冗余层 + 未来扩展点
- `EnemyShootComponent.TickOrder = 155`（TDD 规格 150 与 AttackComponent 冲突，技术偏离已记录）
- `DamageRedirectModifier` 清除暴击标记（`IsCritical=false, CritMultiplier=1f`），避免基地二次暴击计算

---

## 3. 新增 SO 资产清单

| 资产名 | 类型 | 数量 |
|--------|------|------|
| SG_EnemyBullet_Straight / Spread3 / Homing | BulletPatternSO | 3 |
| SG_BulletType_EnemyStraight / Spread / Homing | BulletTypeSO | 3 |
| SG_Enemy_Shooter / Spreader / Elite | EntityConfigSO | 3（修改已有或新建） |

---

## 4. Sprint 1 验收总表

### 功能验收（PlayMode）

| # | 场景 | 操作 | 预期 | 状态 |
|---|------|------|------|------|
| F1 | 射手机射击 | 关卡 3+ 观察 | 射手机 CD=1.5s 向下射击 | ⬜ |
| F2 | 散射机射击 | 关卡 4+ 观察 | 散射机 3 发扇形弹 | ⬜ |
| F3 | 精英机追踪弹 | 关卡 5+ 观察 | 精英机追踪弹追向飞机 | ⬜ |
| F4 | 首次开火延迟 | 计时 | 各类型满足 GDD 规定延迟 | ⬜ |
| F5 | 敌弹→飞机 | 站定不动 | 基地扣血 5~10，飞机无伤 | ⬜ |
| F6 | 敌弹→基地 | 飞机移开 | 基地直接扣血 8~15 | ⬜ |
| F7 | 飞机挡弹优先 | 飞机挡在基地前 | 扣 5 而非扣 8 | ⬜ |
| F8 | 敌机碰飞机 | 让敌机撞飞机 | 基地扣 10~15 + 敌机死 + 震动 + 无敌帧 | ⬜ |
| F9 | 无敌帧 | 碰撞后 0.5s 内再被弹打中 | 不扣血 | ⬜ |
| F10 | 无敌帧视觉 | 观察 | 飞机半透明闪烁 | ⬜ |
| F11 | 关卡 1-2 | 通关前两关 | 无射击敌机，V1 行为不变 | ⬜ |
| F12 | 弹幕系统稳定 | 长时间游玩 | 无内存泄漏，弹丸正常回收 | ⬜ |

### 性能验收

| # | 指标 | 目标值 | 工具 |
|---|------|--------|------|
| P1 | 热路径零 GC | 0 bytes/frame | Profiler Deep Profile |
| P2 | 同屏弹丸 | ≤200（含敌弹） | BulletWorld.Count |
| P3 | Entity 数 | ≤60 | EntityPool.ActiveCount |

### 微信真机验收

| # | 验证项 | PASS 标准 |
|---|--------|-----------|
| W1 | 敌弹渲染 | 红色弹丸正常显示（Atlas 纹理包含） |
| W2 | 碰撞检测 | 伤害数值与编辑器一致 |
| W3 | 帧率 | 保持 ≥30fps（低端机 ≥24fps） |

---

_创建于 2026-05-18 | Sprint 1 TDD v1.4_

**变更历史**：
- v1.0（2026-05-18）：初始版本
- v1.1（2026-05-18）：PK-R1 Unity 架构师回写 + PK-R2 编辑器工具回写
- v1.2（2026-05-18）：PK-R3 UI 设计师回写（UID-010 FirstAttackDelay 起算时刻）
- v1.3（2026-05-19）：PK-R4 技术文档工程师回写（DE-005 FirstAttackDelay 措辞统一）
- v1.4（2026-05-19）：Sprint 1 代码实现完成——代码文件清单更新实现状态 + 实现备注
