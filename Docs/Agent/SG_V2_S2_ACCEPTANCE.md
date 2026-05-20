---
system: shootergame-v2-tdd
scope: sprint2-acceptance
last_verified: 2026-05-19
depends_on: [SG_V2_TDD_02_SKILL_EQUIP_ITEM]
related_code: Assets/_Game/Scripts/ShooterGame/Unlock/*, Assets/_Game/Scripts/ShooterGame/Pickup/*, Assets/_Framework/EntitySystem/Scripts/Components/SkillComponent.cs
---

# Sprint 2 验收手册

> **Sprint 2 核心目标**：技能解锁 + 战前装备 + 道具系统 + 6 槽位全自动释放
> **预估耗时**：~14h | **代码完成**：2026-05-19

---

## 1. 验收前提

- Unity 项目编译通过（零 Error）
- Sprint 1 验收通过（commit `4478d71`）

---

## 2. 代码层验收（不需要真机）

### 2.1 SkillUnlockTableSO + PassiveUnlockTableSO

| # | 验证项 | 操作 | 预期 |
|---|--------|------|------|
| A1 | SO 创建 | Inspector 右键→Create→ShooterGame→SkillUnlockTable | 创建成功 |
| A2 | OnValidate | 添加 Entry，Skill 留空 | Console 红色 Error |
| A3 | 重复检测 | 同一个 Skill 填两次 | Console 红色 Error |
| A4 | PassiveUnlockTable | 同上流程 | 行为一致 |

### 2.2 SG_ProgressManager 成就 API

| # | 验证项 | 操作 | 预期 |
|---|--------|------|------|
| B1 | RecordDeath | `progress.RecordDeath(); Debug.Log(progress.TotalDeaths);` | 计数+1 |
| B2 | IsAchievementMet(1) | 死亡 5 次后查询 | 返回 true |
| B3 | UpdateMaxKills | 传入 50 后查询 IsAchievementMet(2) | 返回 true |
| B4 | RecordHit | 累计 30 次后 IsAchievementMet(3) | 返回 true |
| B5 | FlushCounters | 调用后重新 Load | 计数器持久化 |
| B6 | 版本升级 | V2 存档 Load | 新字段默认 0/空列表 |

### 2.3 SkillComponent 6 槽位

| # | 验证项 | 操作 | 预期 |
|---|--------|------|------|
| C1 | 兜底模式 | 不注入装备，EntityConfigSO.SkillConfig 有值 | Slot[0] 正常运转 |
| C2 | 多槽位 | InitWithEquipment(skills[3]) | 3 个槽位独立 CD |
| C3 | 错开释放 | 观察开场 | 不会 6 技能同时释放 |
| C4 | 空槽位 | 6 槽只填 2 个 | 其余 4 个跳过，无报错 |
| C5 | 死亡中断 | 飞机死亡（如有） | 所有槽位回 Idle |

### 2.4 PickupSystem + ItemDropSystem

| # | 验证项 | 操作 | 预期 |
|---|--------|------|------|
| D1 | 道具掉落 | Play Mode 击杀敌机 | ~30% 概率出道具 |
| D2 | 自动拾取 | 飞机靠近道具 | 距离 < 1.0 自动消失 |
| D3 | Buff 道具 | 拾取 Buff 道具 | BuffComponent.ActiveBuffCount +1 |
| D4 | 修复道具 | 拾取修复道具 | 基地 HP +10（不超 Max） |
| D5 | 超时消失 | 道具漂浮 8s | 自动移除 |
| D6 | 底线消失 | 道具到 Y=-6 | 移除，不扣血 |
| D7 | 保底修复 | 连杀 15 次不出修复 | 第 16 次强制出修复 |
| D8 | 同屏上限 | 快速击杀 >16 个 | 满 16 后不再生成新道具 |

### 2.5 BattleController 集成

| # | 验证项 | 操作 | 预期 |
|---|--------|------|------|
| E1 | 击杀计数→掉落 | 击杀敌机 | 控制台无报错，道具生成 |
| E2 | Defeat 记录 | 基地血归零 | totalDeaths +1 |
| E3 | Victory 记录 | 通关 | maxKills 更新 |
| E4 | 被命中计数 | 飞机被弹丸命中 | hitsTaken +1 |
| E5 | Retry 重置 | 点重试 | 道具清空、技能重注入 |

---

## 3. PlayMode 快速验收步骤

### 步骤 1：直跑 Battle 场景

1. 打开 `Assets/_Game/Scenes/Battle.unity`
2. 在 BattleController Inspector 中：
   - 确认 `_normalDropTable` 和 `_eliteDropTable` 已赋值（若无则创建 SO 资产）
3. Play
4. 观察：
   - 飞机技能正常释放（兜底模式=单技能循环）
   - 击杀敌机时控制台无报错
   - 如果配了 DropTable，击杀后概率出道具

### 步骤 2：验证道具（需配 SO 资产）

1. 创建 SO 资产（按 TDD S2.4 表格）：
   - `Create → Configs/ShooterGame/PickupConfig`：SG_Pickup_Repair（Type=Repair, RepairAmount=10）
   - `Create → Configs/ShooterGame/DropTable`：SG_DropTable_Normal（BaseDropRate=0.3, 填入 Entries）
2. 将 DropTable 赋给 BattleController._normalDropTable
3. Play → 击杀敌机 → 观察道具生成和拾取

### 步骤 3：验证多槽位技能（需编写测试脚本或 MCP 注入）

```csharp
// 在 BattleController.InitBattleAsync 末尾临时加入验证代码：
var skillComp = _playerEntity.GetComponent(ComponentType.Skill) as SkillComponent;
Debug.Log($"[验证] ActiveSlotCount = {skillComp.ActiveSlotCount}");
```

---

## 4. 创建 SO 资产指南

Sprint 2 需要以下 SO 资产才能完整验收（通过 Inspector Create 创建）：

| 资产名 | 类型 | 路径 | 关键字段 |
|--------|------|------|---------|
| SG_SkillUnlockTable | SkillUnlockTableSO | Configs/ShooterGame/ | 6 entries |
| SG_PassiveUnlockTable | PassiveUnlockTableSO | Configs/ShooterGame/ | 4 entries |
| SG_Pickup_Repair | PickupConfigSO | Configs/Pickup/ | Type=Repair, Amount=10 |
| SG_Pickup_Buff_Speed | PickupConfigSO | Configs/Pickup/ | Type=Buff, BuffConfig=速度Buff |
| SG_Pickup_Coin | PickupConfigSO | Configs/Pickup/ | Type=Coin, Amount=10 |
| SG_DropTable_Normal | DropTableSO | Configs/Pickup/ | BaseDropRate=0.3, 3+ Entries |

⚠️ **SO 资产创建是验收的前提条件**，不是代码层自动完成的。

---

## 5. Sprint 2 新增文件清单

### 新增文件（7 个）

| 文件 | 职责 |
|------|------|
| `Unlock/UnlockConditionType.cs` | 解锁条件枚举 |
| `Unlock/SkillUnlockTableSO.cs` | 技能解锁配置表 |
| `Unlock/PassiveUnlockTableSO.cs` | 被动解锁配置表 |
| `Unlock/SkillUnlockManager.cs` | 解锁查询服务 |
| `Pickup/PickupType.cs` | 道具类型枚举 |
| `Pickup/PickupConfigSO.cs` | 道具配置 SO |
| `Pickup/DropTableSO.cs` | 掉落表 SO |
| `Pickup/PickupSystem.cs` | 道具拾取系统 |
| `Pickup/ItemDropSystem.cs` | 道具掉落系统 |

### 修改文件（5 个）

| 文件 | 改动 |
|------|------|
| `BuffComponent.cs` | MAX_BUFFS 8→16 |
| `SkillComponent.cs` | 单槽→6 槽位 + InitWithEquipment |
| `HealthComponent.cs` | +Heal() 方法 |
| `SharedProgressData.cs` | V3 扩展（解锁+成就+星级） |
| `SG_ProgressManager.cs` | V3 成就/计数器/星级 API |
| `BattleLevelData.cs` | +EquippedSkills/Passives 字段 |
| `BattleController.cs` | 集成道具系统 + 技能注入 + 成就计数 |
| `BattleFlowHandler.cs` | 传递完整 BattleLevelData |
| `LevelSelectScreen.Logic.cs` | +using MiniGameTemplate.Entity |

---

## 6. 已知限制 & Sprint 5 延后项

| 项目 | 原因 | 计划 |
|------|------|------|
| 出战准备 Bottom Sheet UI | FairyGUI 面板需完整 XML + 美术资源 | Sprint 5 |
| 道具视觉（Prefab + VFX + 闪烁） | 依赖美术资源 + View 系统 | Sprint 5 |
| 金币系统 | 依赖商店/升级系统设计 | Sprint 5 |
| 完整 6 技能 SkillConfigSO 资产 | 需配置弹幕 Pattern + Buff | Sprint 3/4 |

---

_创建于 2026-05-19_
