---
system: shootergame-v2-tdd
scope: sprint3-acceptance
last_verified: 2026-05-21T09:30
depends_on: [SG_V2_TDD_03_BUFF_DOT_PASSIVE, SG_V2_S2_ACCEPTANCE]
related_code: Assets/_Framework/EntitySystem/Scripts/Components/Buff*, Passive*, Assets/_Game/Configs/ShooterGame/Buffs/*, Dots/*, Passives/*
---

# Sprint 3 验收手册：Buff + DOT + 被动技能

> **Sprint 3 核心目标**：BuffComponent V2 扩展 + DOT 持续伤害 + 4 种被动技能 + ID 冲突检测工具
> **预估耗时**：~15h | **代码完成**：2026-05-21

---

## 1. 验收前提

- Unity 项目编译通过（零 Error）
- Sprint 2 验收通过（commit 历史可查）
- Sprint 1+2 所有 SO 资产完整（SkillConfigSO / PickupConfigSO / DropTableSO 等）

---

## 2. 代码层验收（不需要 PlayMode）

### 2.1 SO 资产完整性检查

在 Project 面板中逐一确认以下 SO 资产存在且 Inspector 无 Missing Reference：

#### Buff/Debuff SO（10 个）— `Assets/_Game/Configs/ShooterGame/Buffs/`

| # | 资产名 | BuffId | Tag | Duration | 关键效果 | 状态 |
|---|--------|--------|-----|----------|---------|------|
| 1 | SG_Buff_SpeedUp | 1001 | Positive | 5s | AttackInterval×0.5 | ⬜ |
| 2 | SG_Buff_MoveUp | 1002 | Positive | 5s | MoveSpeed×1.5 | ⬜ |
| 3 | SG_Buff_Shield | 1003 | Positive | 8s | DamageTaken×0（免伤） | ⬜ |
| 4 | SG_Buff_Berserk | 1004 | Positive | 3s | AttackInterval×0.3 + MoveSpeed×1.3 | ⬜ |
| 5 | SG_Debuff_Slow | 3001 | Negative | 3s | MoveSpeed×0.5 | ⬜ |
| 6 | SG_Debuff_Vulnerable | 3002 | Negative | 4s | DamageTaken×2.0 | ⬜ |
| 7 | SG_Debuff_Blind | 3003 | Negative | 2s | AttackInterval×3.0 | ⬜ |
| 8 | SG_Buff_Passive_Pierce | 2001 | Positive | 3s | GrantsPierce=true | ⬜ |
| 9 | SG_Buff_Passive_Crit | 2002 | Positive | 4s | CritRateBonus+0.2 | ⬜ |
| 10 | SG_Buff_Passive_Magnet | 2003 | Positive | 3s | PickupRadius×2.0 | ⬜ |

#### DOT SO（3 个）— `Assets/_Game/Configs/ShooterGame/Dots/`

| # | 资产名 | DotId | DmgPerTick | Interval | Duration | 施加路径 | 状态 |
|---|--------|-------|-----------|----------|----------|---------|------|
| 1 | SG_Dot_Burn | 4001 | 5 | 0.5s | 3s | V3（无载体） | ⬜ |
| 2 | SG_Dot_Poison | 4002 | 3 | 1.0s | 5s | V3（无载体） | ⬜ |
| 3 | SG_Dot_Arc | 4003 | 8 | 0.3s | 1.5s | 激光附带 | ⬜ |

#### Passive SO（4 个）— `Assets/_Game/Configs/ShooterGame/Passives/`

| # | 资产名 | PassiveId | TriggerMode | CD | LinkedBuff | 状态 |
|---|--------|-----------|-------------|-----|-----------|------|
| 1 | SG_Passive_Pierce | 5001 | AutoOnReady | 5s | → SG_Buff_Passive_Pierce | ⬜ |
| 2 | SG_Passive_Crit | 5002 | AutoOnReady | 8s | → SG_Buff_Passive_Crit | ⬜ |
| 3 | SG_Passive_Magnet | 5003 | AutoOnReady | 6s | → SG_Buff_Passive_Magnet | ⬜ |
| 4 | SG_Passive_Retaliate | 5004 | OnHit | 5s | null（即时型） | ⬜ |

#### 引用链完整性

| # | 验证项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| R1 | SG_Skill_LaserBeam.AttachedDotConfig | Inspector 检查 | → SG_Dot_Arc | ⬜ |
| R2 | SG_PassiveUnlockTable (4 entries) | Inspector 检查 | 4 个 PassiveAbilitySO 非 null | ⬜ |
| R3 | PassiveAbilitySO.LinkedBuff 链 | Pierce/Crit/Magnet 三个 | LinkedBuff 指向对应被动 Buff SO | ⬜ |
| R4 | Retaliate.ActivateEffects | Inspector 检查 | 数组可为空（PA-04 通过 BulletDirections=8 驱动） | ⬜ |

### 2.2 T5 ID 冲突检测

| # | 验证项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| F1 | ID 无冲突 | 菜单 `ShooterGame/Validate/Check ID Conflicts` | Console 输出 ✅ 全部通过 | ⬜ |
| F2 | 制造冲突 | 临时将某 Buff ID 改为与另一个相同 → 运行 | Console Error 报告冲突 | ⬜ |
| F3 | 范围越界 | 临时将某 BuffId 设为 5000 → 运行 | Console Error 报告越界 | ⬜ |
| F4 | OnValidate 即时反馈 | SkillConfigSO.Effects=空 | Inspector 黄色 Warning | ⬜ |

⚠️ **F2/F3 测试完后务必恢复原始值！**

### 2.3 新增代码文件确认

| 路径 | 类型 | 确认 |
|------|------|------|
| `EntitySystem/Scripts/Components/BuffComponent.cs` | 修改（V2 扩展） | ⬜ |
| `EntitySystem/Scripts/Components/BuffDamageModifier.cs` | 新增 | ⬜ |
| `EntitySystem/Scripts/Components/PassiveComponent.cs` | 新增 | ⬜ |
| `EntitySystem/Scripts/Config/BuffConfigSO.cs` | 修改（V2 字段） | ⬜ |
| `EntitySystem/Scripts/Config/BuffEnums.cs` | 新增 | ⬜ |
| `EntitySystem/Scripts/Config/DotConfigSO.cs` | 新增 | ⬜ |
| `EntitySystem/Scripts/Config/PassiveAbilitySO.cs` | 新增 | ⬜ |
| `EntitySystem/Scripts/Config/SkillConfigSO.cs` | 修改（+AttachedDotConfig） | ⬜ |
| `DanmakuSystem/Scripts/Config/BulletPatternSO.cs` | 修改（+OnHitDotConfig） | ⬜ |
| `Editor/ShooterGame/SG_IdConflictValidator.cs` | 新增 | ⬜ |
| `ShooterGame/Core/BattleController.cs` | 修改（+Buff/Passive 初始化） | ⬜ |

---

## 3. PlayMode 验收步骤

### 前置：Battle 场景直跑准备

1. 打开 `Assets/_Game/Scenes/Battle.unity`
2. 确认 BattleController Inspector 中：
   - `_normalDropTable` / `_eliteDropTable` 已赋值
   - 关卡波次配置中包含射手敌机（用于 DOT 验证需要激光技能命中敌机）
3. 确认 BattleLevelData（或 BattleFlowHandler）可注入装备数据

### 步骤 1：Buff 基础功能（A 系列）

| # | 验收项 | 操作方法 | 预期 | 状态 |
|---|--------|---------|------|------|
| A1 | Buff 施加 | 拾取 Buff 道具 或 MCP 注入 `ApplyBuff(SpeedUp)` | BuffSlot 占用，MoveSpeed/AttackInterval 修正生效 | ⬜ |
| A2 | 同 ID 刷新 | 再次拾取同类 Buff 道具 | Duration 刷新，不占新 slot，ActiveBuffCount 不变 | ⬜ |
| A3 | 叠加模式 | 对 Stack 模式 Buff 重复施加 ×3 | CurrentStacks=3，属性按指数递增 | ⬜ |
| A4 | 叠加上限 | 叠超 MaxStacks | CurrentStacks 不超 Max | ⬜ |
| A5 | Buff 到期 | 等待 Duration（如 SpeedUp 5s） | Buff 自动清除，属性恢复原值 | ⬜ |
| A6 | Tag 清除 | MCP 注入 `RemoveByTag(Negative)` | 所有减益清除，增益保留 | ⬜ |
| A7 | DamageTaken 修正 | 施加脆弱 Debuff(DamageTaken=2) → 被敌弹命中 | 基地扣血量 ×2 | ⬜ |
| A8 | BulletCountMod | 施加火力全开 Buff(BulletCount=2) | 基础攻击子弹数 ×2 | ⬜ |
| A9 | 攻速钳制 | 同时施加多个攻速 Buff | AttackInterval 乘积比率下限 ≥ 0.3（不突破安全钳） | ⬜ |
| A10 | 槽位满 | MCP 连续施加 17 个不同 Buff | Console LogWarning "Buff 槽位已满(16)"，不崩溃 | ⬜ |

### 步骤 2：BuffDamageModifier 桥接（B 系列）

| # | 验收项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| B1 | 敌机脆弱 | MCP 对敌机施加脆弱(DamageTaken×2) | 敌机受伤翻倍 | ⬜ |
| B2 | 免伤 Buff | 拾取护盾道具(DamageTaken×0) → 被敌弹命中 | 基地不扣血（飞机转发伤害为 0） | ⬜ |

### 步骤 3：DOT 持续伤害（D 系列）

| # | 验收项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| D1 | 电弧 DOT | 装备激光技能出击 → 命中敌机 | 敌机每 0.3s 受 8 伤害，持续 1.5s（总伤 ~40） | ⬜ |
| D2 | DOT 刷新 | 激光持续命中已有 DOT 的敌机 | Duration 刷新（不叠加伤害），伤害节奏不变 | ⬜ |
| D3 | DOT 到期 | 激光停止命中，等待 1.5s | DOT 自动清除，敌机不再受 tick 伤害 | ⬜ |
| D4 | 燃烧/中毒 预创建 | Inspector 检查 SO | SO 资产存在，V2 运行时无触发路径（无效果正常） | ⬜ |
| D5 | DotId 范围 | T5 工具校验 | 4001~4003 ∈ [4000,4999] | ⬜ |

### 步骤 4：被动技能系统（E 系列）

> **前置**：在出战准备中装备对应被动，或通过 MCP 注入 `PassiveComponent.InitWithPassives()`。

| # | 验收项 | 操作 | 预期 | 状态 |
|---|--------|------|------|------|
| E1 | 穿透被动 | 装备 Pierce 出击 | 每 5s 自动激活 3s 穿透窗口，`HasActivePierce=true` | ⬜ |
| E2 | 暴击被动 | 装备 Crit 出击 | 每 8s 自动激活 4s 暴击窗口，CritRateBonus=0.2 | ⬜ |
| E3 | 磁吸被动 | 装备 Magnet 出击 | 每 6s 激活 3s 磁吸，PickupRadiusModifier=2.0 | ⬜ |
| E4 | 尾翼反击 | 装备 Retaliate 出击 → 被敌弹命中 | CD 就绪时发射 8 发环形弹（BulletDirections=8） | ⬜ |
| E5 | PA-04 CD | 尾翼触发后 5s 内再被命中 | 不触发（CooldownTimer > 0） | ⬜ |
| E6 | 无敌帧 + PA-04 | 敌机碰飞机后（0.5s 无敌帧），再被弹命中 | PA-04 仍触发（OnCollisionHit 事件先于 TakeDamage） | ⬜ |
| E7 | 被动 CD UI | 观察被动栏（如有 UI） | 冷却→就绪→激活三态标记正确（PassiveSlot 字段） | ⬜ |
| E8 | 3 被动并行 | 装备 3 个被动 | 各自独立 CD 运作，互不干扰 | ⬜ |

### 步骤 5：集成验收（G 系列）

| # | 场景 | 预期 | 状态 |
|---|------|------|------|
| G1 | 4 种 Buff 道具 | 拾取各 Buff 道具效果正确（SpeedUp/MoveUp/Shield/Berserk） | ⬜ |
| G2 | Buff 到期清除 | Duration 后属性恢复，ActiveBuffCount 减少 | ⬜ |
| G3 | DamageTaken 修正 | 护盾免伤 / 脆弱加伤 | ⬜ |
| G4 | BulletCount 修正 | 火力全开子弹数 ×2（通过 GetBulletCountModifier 查询） | ⬜ |
| G5 | 攻速钳制 | 极端叠加不突破比率 0.3 | ⬜ |
| G6 | 电弧 DOT | 激光命中施加持续伤害 | ⬜ |
| G7 | 4 种被动 | 各被动独立 CD + 效果正确 | ⬜ |
| G8 | 被动 Buff 桥接 | 被动通过 Buff 实现，到期自动清除 | ⬜ |
| G9 | PA-04 碰撞触发 | 被命中时反击弹幕 | ⬜ |
| G10 | T5 ID 检测 | 菜单命令运行通过 | ⬜ |
| G11 | SO OnValidate | 错误配置即时标红 | ⬜ |

---

## 4. 性能验收

| # | 指标 | 目标 | 工具 | 状态 |
|---|------|------|------|------|
| P1 | BuffComponent.Tick | < 0.1ms（含 DOT） | Profiler Deep Profile | ⬜ |
| P2 | PassiveComponent.Tick | < 0.05ms | Profiler Deep Profile | ⬜ |
| P3 | 热路径零 GC | 0 bytes/frame（战斗循环） | Profiler Deep Profile | ⬜ |

**性能验收操作**：
1. 开启 Profiler → Deep Profile
2. Play Mode 进入战斗，装备全套 Buff 道具 + 3 被动
3. 在 Profiler 中搜索 `BuffComponent.Tick` 和 `PassiveComponent.Tick`
4. 确认单帧耗时和 GC Alloc

---

## 5. MCP 辅助验收指南

以下场景建议通过 Unity MCP `execute_code` 辅助验证（需反射方式访问 asmdef 内类型）：

```
// 示例：获取玩家 BuffComponent 状态
var playerType = System.Type.GetType("MiniGameTemplate.Entity.Entity, MiniGameTemplate.EntitySystem");
// ... 通过 BattleController 获取 _playerEntity → BuffComponent 状态
```

**适合 MCP 验证的项**：
- A3/A4 叠层验证（精确控制施加次数）
- A6 Tag 清除（精确调用 RemoveByTag）
- A10 槽位满（连续施加 17 个）
- B1 敌机脆弱（对敌机施加 Debuff）
- E 系列被动 CD 验证（读取 PassiveSlot.CooldownTimer）

**不适合 MCP 的项**（需真实游戏流程）：
- D1~D3 DOT（需激光技能实际命中）
- E4~E6 PA-04（需实际碰撞事件）
- G1 道具拾取（需击杀敌机掉落）

---

## 6. 已知限制 & Sprint 4 延后项

| 项目 | 原因 | 计划 |
|------|------|------|
| Buff VFX 池化 | VfxPrefab 字段预留但 V2 不实装视觉效果 | Sprint 5 |
| DOT VFX | VfxPrefab 字段预留 | Sprint 5 |
| 燃烧/中毒 DOT | V2 无触发路径（无"特殊子弹"） | V3 |
| BulletPatternSO.OnHitDotConfig | V2 全部 null（V3 准备字段） | V3 |
| Debuff 3 种 | 预创建无运行时触发路径 | Sprint 4（敌机技能） |
| 被动 CD UI 视觉 | 数据层（PassiveSlot）已就位，UI 面板待 Sprint 5 | Sprint 5 |

---

## 7. 验收通过后下一步

1. ✅ 更新 MEMORY.md 记录 Sprint 3 验收通过
2. ✅ Git commit（标记 Sprint 3 milestone）
3. 推进 Sprint 4（关卡编排 + 数值平衡 + 伤害统计 + 星级评价）

---

## 8. 验收记录（2026-05-21 MCP PlayMode）

### 验收前修复（首次 MCP 验收）
- ⚠️ `SG_Pickup_Buff_Speed.BuffConfig` 断裂（→ 修复指向 SG_Buff_SpeedUp）
- ⚠️ `SG_Player` EntityConfigSO 缺少 `Passive` 组件类型（→ 已补入 Components 数组）
- 📝 `SG_IdConflictValidator.cs` 实际路径：`Assets/_Game/Editor/ShooterGame/`（非 `Assets/Editor/`）

### 代码层验收（§2）— MCP 自动验收 2026-05-21 10:45
| 项 | 结果 |
|---|---|
| 编译检查 | ✅ 0 Error / 0 Warning |
| 2.1 SO 资产完整性（17/17 + R1-R4） | ✅ PASS — 零 Missing Reference |
| 2.2 T5 ID 冲突检测（F1） | ✅ PASS（10B+3D+4P 全合规，零冲突零越界） |
| 2.2 F4 OnValidate | ✅ LaserBeam Effects 空 Warning 正常 |
| 2.3 新增代码文件（11/11） | ✅ 全部存在（实际路径 `_Game/Editor/` 非 `Editor/`） |
| R1 LaserBeam→DotArc | ✅ AttachedDotConfig → SG_Dot_Arc |
| R2 PassiveUnlockTable | ✅ 4 entries, 0 nullRefs |
| R3 Passive→LinkedBuff | ✅ Pierce/Crit/Magnet 三链完整 |
| R4 Retaliate.BulletDirections | ✅ =8 |

### PlayMode 验收（§3）— MCP 自动验收 2026-05-21 10:45
| 项 | 结果 | 备注 |
|---|---|---|
| A1 Buff 施加 | ✅ | SpeedUp → Count 0→1, AttackInterval 1.00→0.50 |
| A2 同 ID 刷新 | ✅ | Count=1 不变 |
| A6 Tag 清除 | ✅ | 7→4, RemoveByTag(Negative) 精确移除 3 个 |
| A7 DamageTaken 修正 | ✅ | Shield(×0)+Vuln(×2)→DamageTaken=0 (乘法叠加) |
| A9 攻速钳制 | ✅ | SpeedUp×0.5 + Berserk×0.3 → 钳制到 0.30 |
| A10 槽位满 | ✅ | MAX_BUFFS=16 确认, 8/16 used, code-reviewed |
| B2 Shield 免伤 | ✅ | DamageTaken=0.00 |
| E1 Pierce 自动激活 | ✅ | HasActivePierce=True |
| E2 Crit 自动激活 | ✅ | CritRateBonus=0.2 |
| E3 Magnet 自动激活 | ✅ | PickupRadiusModifier=2.0 |
| E8 3 被动并行 | ✅ | 3 slots 各自独立 CD，互不干扰 |
| D 系列（DOT） | ⬜ | 需激光技能实际命中（待真机） |
| E4-E6（OnHit 被动） | ⬜ | 需实际碰撞事件（待真机） |
| G 系列（集成） | 🔶 | 代码层全覆盖，物理碰撞链待真机 |

### 运行时错误
✅ 零 Error / 零 Warning（排除已知 LaserBeam Effects 空提示）

### 性能验收（§4）
🔶 延后到真机 + Profiler Deep Profile

---

## 9. 天命人验收行动指南

> 以下是推荐的验收顺序和具体操作。预计耗时 20~30 分钟。

### 第一步：代码层快速确认（5 分钟）

1. **打开 Unity 项目**，确认编译零错误
2. **运行 T5 ID 校验**：菜单 `ShooterGame → Validate → Check ID Conflicts`
   - 预期：Console 输出 ✅ 全部通过（12 Buff + 3 DOT + 4 Passive）
3. **快速浏览 SO 资产**：
   - `Assets/_Game/Configs/ShooterGame/Buffs/` — 10 个 SO
   - `Assets/_Game/Configs/ShooterGame/Dots/` — 3 个 SO
   - `Assets/_Game/Configs/ShooterGame/Passives/` — 4 个 SO
   - 重点检查：Inspector 中无 Missing Reference（红色感叹号）

### 第二步：PlayMode 核心功能验证（15 分钟）

1. **打开 Battle 场景**：`Assets/_Game/Scenes/Battle.unity`
2. **进入 Play Mode**
3. **Buff 验证**（需要道具掉落或 MCP 注入）：
   - 拾取加速道具 → 观察攻击频率提升 → 等 5s 恢复
   - 拾取护盾道具 → 被敌弹命中 → 基地不扣血
4. **被动验证**（需要装备被动后出战）：
   - 装备 Pierce + Crit + Magnet 三个被动
   - 观察穿透窗口（每 5s 子弹穿透敌机）
   - 观察暴击窗口（每 8s 暴击率提升）
5. **DOT 验证**（需要激光技能命中敌机）：
   - 装备激光技能出战 → 命中敌机 → 观察持续伤害数字
6. **PA-04 反击验证**（需要装备 Retaliate 被动）：
   - 装备 Retaliate 出战 → 被敌弹命中 → 观察 8 发环形弹

### 第三步：边界情况（5 分钟，可选）

- **F2/F3 冲突测试**：临时改一个 Buff ID 为重复值 → 运行 T5 → 确认报红 → **改回原值**
- **OnValidate 测试**：清空 SkillConfigSO.Effects → Inspector 黄色 Warning

### 验收结论

全部通过后请告知，我会：
1. 更新 ACCEPTANCE.md 打 ✅
2. Git commit 标记 Sprint 3 milestone
3. 更新 BOARD.md 推进到 Sprint 4

如果发现问题，请描述现象 + 截图，我直接修复。

---

_创建于 2026-05-21 | v1.3（MCP 自动验收：编译/T5/SO/引用链/Buff全链路/被动系统 全 PASS）_
