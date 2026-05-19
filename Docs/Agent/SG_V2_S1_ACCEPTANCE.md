---
system: shootergame-v2-acceptance
scope: sprint1-enemy-shooting-collision
created: 2026-05-19
version: v1.0
---

# Sprint 1 验收手册：敌方射击 + 碰撞规则 + 伤害转发

> **前置条件**：Unity Editor 已打开 MiniGameTemplate/UnityProj 项目，编译零错误。

---

## 🎯 验收目标

Sprint 1 实现了以下核心代码能力（**不含 SO 资产配置**）：

1. `EnemyShootComponent` — 敌机射击组件（首次开火延迟 + CD 循环射击）
2. `InvincibilityModifier` — 无敌帧伤害修正器（Priority=-1）
3. `DamageRedirectModifier` — 伤害转发修正器（飞机→基地，Priority=0）
4. `BattleController.SetupDamageRedirectChain()` — V2 伤害链路注入

---

## 📌 验收前准备

### 步骤 1：确认编译状态

1. 打开 Unity Editor
2. Console 面板确认 **0 Errors, 0 Warnings**
3. 如果有错误，先解决编译问题

### 步骤 2：确认新增文件存在

在 Project 面板中检查以下文件：

| 路径 | 确认 |
|------|------|
| `Assets/_Framework/EntitySystem/Scripts/Components/EnemyShootComponent.cs` | ⬜ |
| `Assets/_Framework/EntitySystem/Scripts/Components/InvincibilityModifier.cs` | ⬜ |
| `Assets/_Framework/EntitySystem/Scripts/Components/DamageRedirectModifier.cs` | ⬜ |
| `Assets/_Framework/EntitySystem/Scripts/Core/ComponentType.cs`（含 EnemyShoot=11） | ⬜ |
| `Assets/_Framework/EntitySystem/Scripts/Core/EntityPool.cs`（含 EnemyShoot case） | ⬜ |
| `Assets/_Framework/EntitySystem/Scripts/Config/EntityConfigSO.cs`（含 FirstAttackDelay） | ⬜ |

### 步骤 3：创建测试用 SO 资产

Sprint 1 代码层已完整，但需要手动创建 SO 资产才能在 PlayMode 验证：

#### 3.1 创建 BulletTypeSO（敌方子弹视觉）

1. Project 面板右键 → Create → Danmaku → Bullet Type
2. 命名 `SG_BulletType_EnemyStraight`
3. Inspector 设置：
   - Size: 0.2
   - Color: Red/Orange（`#FF4500`）
   - Trail: 开启（短拖尾）

#### 3.2 创建 BulletPatternSO（敌方弹幕模式）

1. Project 面板右键 → Create → Danmaku → Bullet Pattern
2. 命名 `SG_EnemyBullet_Straight`
3. Inspector 设置：
   - Bullet Type: 指向上面创建的 BulletTypeSO
   - Bullet Count: 1
   - Speed: 5
   - Lifetime: 3
   - Camp/Faction: Enemy

#### 3.3 创建/修改 EntityConfigSO（射击敌机）

1. 复制已有的敌机 EntityConfigSO（如 `SG_Enemy_Basic`）
2. 重命名为 `SG_Enemy_Shooter`
3. Inspector 设置：
   - Attack Bullet Pattern: 指向 `SG_EnemyBullet_Straight`
   - Attack Interval: 1.5
   - **First Attack Delay: 1.0**（V2 新增字段！）
   - Components 勾选：EnemyShoot ✅
   - HP: 40
   - MoveSpeed: 1.5

#### 3.4 修改波次配置

在任意关卡的 `EntitySpawnWaveSO` 中，添加一个使用 `SG_Enemy_Shooter` 的 SpawnGroup。

---

## ✅ 功能验收清单

### A. 代码结构验证（Inspector 检查）

| # | 验收项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| A1 | ComponentType 枚举 | 打开 ComponentType.cs | `EnemyShoot = 11` 存在 | ⬜ |
| A2 | EntityConfigSO 字段 | 选中任意 EntityConfigSO | Inspector 显示 `First Attack Delay` 字段 | ⬜ |
| A3 | EntityPool 注册 | 打开 EntityPool.cs | `case ComponentType.EnemyShoot` 存在 | ⬜ |

### B. EnemyShootComponent 功能验证（PlayMode）

> **前置**：需先完成"验收前准备 步骤 3"创建 SO 资产

| # | 验收项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| B1 | 首次开火延迟 | Play Mode，Spawn 射手敌机 | 从 Spawn 开始计时 ≥1.0s 后才首次射击 | ⬜ |
| B2 | CD 循环射击 | 首次开火后观察 | 每 1.5s（AttackInterval）射击一次 | ⬜ |
| B3 | 射击方向 | 观察弹丸飞行 | 向下飞行（270° 角度） | ⬜ |
| B4 | 无弹幕不射 | 使用 AttackBulletPattern=null 的敌机 | 无射击行为，组件 IsActive=false | ⬜ |
| B5 | 移动中射击 | 观察射手机行为 | 移动和射击互不干扰 | ⬜ |

### C. 伤害转发链路验证（PlayMode）

| # | 验收项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| C1 | 敌弹命中飞机 | 让敌弹击中玩家飞机 | 飞机不扣血，基地 HP 下降 | ⬜ |
| C2 | BaseHP SO 同步 | Inspector 观察 `BaseHP` FloatVariable | 数值实时下降（归一化 0~1） | ⬜ |
| C3 | 无敌帧生效 | 快速连续命中飞机 | 第一次命中后 0.5s 内再次命中不扣血 | ⬜ |
| C4 | Retry 后链路重建 | 触发 Defeat → Retry | 新一局伤害转发正常工作 | ⬜ |

### D. 边界条件验证

| # | 验收项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| D1 | 基地死亡后 | 基地 HP=0 触发 Defeat | 不再处理后续碰撞 | ⬜ |
| D2 | V1 兼容性 | 不配 EnemyShoot 的关卡 | V1 行为完全不变 | ⬜ |
| D3 | 敌机越线（V1） | 敌机穿过底线 | 基地扣血 + 敌机消失（BaseLineDetector 不变） | ⬜ |

---

## 🔍 代码审查要点（已通过）

| 维度 | 状态 | 说明 |
|------|------|------|
| CL-1 命名/风格 | ✅ | _camelCase 私有字段，PascalCase 公共属性 |
| CL-2 空引用防御 | ✅ | 所有入口有 null check |
| CL-3 GC 分配 | ✅ | 零 LINQ/string 拼接/foreach |
| CL-4 生命周期 | ✅ | Init/Reset 配对完整 |
| CL-6 SO 不引用场景 | ✅ | 无违规 |
| CL-8 跨文件联动 | ✅ | 三处联动（枚举+Pool+Config）无遗漏 |
| CL-9 编译 | ✅ | Unity MCP 确认 0E/0W |

---

## 📊 技术偏离记录

| 偏离项 | TDD 规格 | 实际实现 | 原因 | 风险评估 |
|--------|----------|---------|------|---------|
| TickOrder | 150 | 155 | 与 AttackComponent(150) 冲突 | 低——无功能差异 |
| InvincibilityComponent | 独立组件 | 省略（用 Modifier+HealthComp.IsInvincible） | 复用已有机制，减少代码量 | 低——功能等价 |
| 暴击修正 | 未明确 | DamageRedirectModifier 清除暴击标记 | 防止基地二次暴击计算 | 无——修复潜在 bug |

---

## 🚀 验收通过后下一步

1. 创建 SO 资产（3 BulletPattern + 3 BulletType + 3 敌机 EntityConfig）
2. 修改关卡 3~5 波次配置，加入射手敌机
3. 推进 Sprint 2（技能解锁 + 战前装备 + 道具系统）

---

_创建于 2026-05-19 | v1.0_
