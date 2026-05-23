---
system: shootergame-v2-tdd
scope: sprint4-level-balance
last_verified: 2026-05-22
depends_on: [SG_V2_TDD_INDEX, SG_V2_TDD_03_BUFF_DOT_PASSIVE, SG_GDD_05_SUPPLEMENT]
related_code: Assets/_Game/Scripts/ShooterGame/Core/BattleController*, Assets/_Game/Configs/ShooterGame/Levels/*, Assets/_Framework/EntitySystem/**
---

# Sprint 4：关卡编排 + 数值平衡（~8h）

> **目标**：5 关完整可玩——波次配置 + 数值平衡 + 伤害统计 + 星级评价 + DPS 计算面板（T4）。
> **前置**：Sprint 3 验收通过（全系统就位 → Playtest 调参阶段）。

---

## 1. 实施任务分解

### S4.1 5 关波次配置（3h）

#### 实施方案

**数据来源**：GDD §11.6 难度曲线 + §空间编排指导。

**配置方式**：Luban xlsx（关卡波次/敌机出场属于大批量数据，符合 §11.8 数据权威原则）。

**关卡总表**：

| 关卡 | 波次数 | 预计时间 | 主要敌机 | 射击敌机 | 情绪主题 |
|------|-------|---------|---------|---------|---------|
| 1 | 3 | 45-60s | 普通机 only | 无 | "我好强！" |
| 2 | 4 | 60-75s | 普通 + 快速 | 无 | "好快！" |
| 3 | 5 | 75-90s | 普通 + 射手 | 直射 | "竟然被打了！" |
| 4 | 6 | 90-120s | 全类型 | 直射 + 散射 | "弹幕好多！" |
| 5 | 8 | 120-150s | 全类型 + 精英 | 全类型 | "满配碾压！" |

**各关卡波次编排**（关卡 | 波次 | 敌机组合 | 数量 | 阵型 | HP 倍率）：

**关卡 1（"我好强！"——3 波，无射击敌机）**：W1: 普通×3 横排 ×1.0 / W2: 普通×5 横排 ×1.0 / **W3★**: 普通×10 双排交错 ×0.8（量多血少一扫而空）

**关卡 2（"好快！"——4 波，无射击敌机）**：

| 波次 | 编排 | HP 倍率 |
|------|------|---------|
| W1 | 普通×4 横排 | ×1.0 |
| W2 | 快速×3 错列 | ×1.0 |
| W3 | 普通×3 + 快速×2 混合 | ×1.0 |
| **W4★** | 快速×6 rush | ×1.0 |

**关卡 3（"竟然被打了！"——5 波，首次射手）**：

| 波次 | 编排 | HP 倍率 |
|------|------|---------|
| W1 | 普通×5 横排 | ×1.2 |
| W2 | 普通×3 + 快速×2 混合 | ×1.2 |
| W3 | 普通×3 + **射手×1** 前后排 | ×1.2 |
| W4 | 射手×2 + 普通×4 前后排 | ×1.2 |
| **W5★** | 射手×2 + 快速×3 + 普通×3 rush | ×1.0 |

**关卡 4（"弹幕好多！"——6 波，首次散射）**：

| 波次 | 编排 | HP 倍率 |
|------|------|---------|
| W1 | 普通×4 + 快速×2 混合 | ×1.5 |
| W2 | 射手×2 + 普通×3 前后排 | ×1.5 |
| W3 | **散射×1** + 普通×4 V字夹击 | ×1.5 |
| W4 | 散射×1 + 射手×2 三线封锁 | ×1.5 |
| W5 | 射手×3 + 快速×3 混合 | ×1.3 |
| **W6★** | 散射×2 + 射手×2 + 普通×4 全阵型 | ×1.2 |

**关卡 5（"满配碾压！"——8 波，首次精英）**：

| 波次 | 编排 | HP 倍率 |
|------|------|---------|
| W1 | 普通×5 + 快速×3 混合 | ×2.0 |
| W2 | 射手×3 + 普通×3 前后排 | ×2.0 |
| W3 | 散射×2 + 普通×4 V字夹击 | ×1.8 |
| W4 | **精英×1** + 射手×2 精英+护卫 | ×2.0 |
| W5 | 快速×6 纯速度 rush | ×1.5 |
| W6 | 散射×2 + 射手×2 + 普通×2 全阵型 | ×1.8 |
| W7 | 精英×1 + 散射×1 + 射手×3 精英+封锁 | ×2.0 |
| **W8★** | 精英×2 + 射手×3 + 散射×2 + 快速×3 (10) 全类型 | ×1.5 |

**波间间歇配置**：

| 关卡 | 波间间歇 | 高潮前间歇 |
|------|---------|-----------|
| 1 | 3.5s | 4.5s |
| 2 | 3.0s | 4.0s |
| 3 | 2.8s | 3.8s |
| 4 | 2.3s | 3.5s |
| 5 | 2.0s | 3.5s |

**DropTable 绑定**：

| 关卡 | 普通敌机 DropTable | 精英/射手 DropTable | 说明 |
|------|-------------------|-------------------|------|
| 1 | SG_DropTable_Normal (30%) | — | Buff+修复 |
| 2-4 | SG_DropTable_Normal (30%) | SG_DropTable_Elite (50%) | 标准掉率 |
| 5 | SG_DropTable_Normal (40%) | SG_DropTable_Elite (60%) | 高掉率（英雄时刻） |

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| A1 | 关卡 1 节奏 | Play Mode 通关 | 45-60s，无射击敌机，轻松通关 | 情绪=爽 |
| A2 | 关卡 2 快速机 | Play Mode 通关 | 快速机首次出现，追踪导弹"追得上" | 解锁验证 |
| A3 | 关卡 3 射手机 | Play Mode 通关 | 射手机开火，"竟然被打了！" | 首次弹幕压力 |
| A4 | 关卡 4 弹幕覆盖 | Play Mode 通关 | 散射+射手弹幕交叉 | 走位需求明显 |
| A5 | 关卡 5 全类型 | Play Mode 通关 | 精英+全弹幕，满配才能碾压 | 英雄时刻 |
| A6 | 波间间歇 | 计时 | 符合配置表（±0.3s） | 节奏正确 |
| A7 | 道具掉落 | 观察 | 掉率与 DropTable 一致 | 概率正确 |
| A8 | 关卡 1-2 无射击 | 通关前两关 | 零射击敌机 | V1 体验不变 |

---

### S4.2 敌机数值平衡表（1h）

#### 实施方案

**基础数值**（来自 GDD §3.2——[占位符]待 playtest）：

| 敌机类型 | 基础 HP | 移动速度 | 射击 CD | 首次开火延迟 | 弹丸 Speed | 弹丸伤害（命中飞机） | 弹丸伤害（命中基地） |
|---------|--------|---------|--------|------------|-----------|-------------------|-------------------|
| 普通机 | 20 | 2.0 | — | — | — | — | — |
| 快速机 | 20 | 4.0 | — | — | — | — | — |
| 射手机 | 40 | 1.5 | 1.5s | 1.0s | 5 | 5 | 8 |
| 散射机 | 60 | 1.2 | 2.5s | 1.5s | 4.5 | 4 | 7 |
| 精英机 | 120 | 1.0 | 3.0s | 0.8s | 4 | 8 | 12 |

**HP 倍率公式**：`实际 HP = 基础 HP × 关卡 HP 倍率`

| 关卡 | HP 倍率范围 | 说明 |
|------|-----------|------|
| 1 | 0.8~1.0 | 低压 |
| 2 | 1.0 | 标准 |
| 3 | 1.0~1.2 | 微增 |
| 4 | 1.2~1.5 | 明显增长 |
| 5 | 1.5~2.0 | 翻倍 |

**碰撞伤害数值**：

| 碰撞对 | 伤害值 | 来源 |
|--------|--------|------|
| 敌弹→飞机→基地 | 射手=5, 散射=4, 精英=8 | DamageRedirectModifier |
| 敌弹→基地（直接） | 射手=8, 散射=7, 精英=12 | 弹幕碰撞 |
| 敌机→飞机→基地 | 10 | EntityCollisionSolver |
| 敌机→基地（底线） | 15 | BaseLineDetector |

**DPS 参考**（供 T4 面板校验）：

| 技能 | 裸 DPS（单目标） | 含被动期望 DPS | 说明 |
|------|----------------|---------------|------|
| 基础攻击 | 40/s | ~46/s（暴击被动） | 10 dmg × 4/s |
| 散射弹幕(SK-P01) | ~100/s | ~115/s | 5×10 dmg / 0.4s (含后摇) |
| 追踪导弹(SK-P02) | ~14/s | ~16/s | 2×20 dmg / 2.8s |
| 激光射线(SK-P03) | ~53/s | ~61/s | 15 dmg / 0.1s tick × 1.5s / 3.8s cycle |
| 火力全开(SK-P04) | +40/s 期间 | — | 基础攻击 DPS ×4 持续 3s / 11s cycle |
| 冲击波(SK-P06) | ~18/s（AOE） | ~21/s | 100 dmg / 5.5s |

> 以上数值为 **[占位符]**，需通过 T4 DPS 计算面板精确校验后调整。

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| B1 | HP 倍率 | 关卡 5 精英机 Inspector | HP = 120 × 2.0 = 240 | 倍率生效 |
| B2 | 伤害数值 | 敌弹命中飞机 | 扣血值与表一致 | 射手=5, 散射=4 |
| B3 | 碰撞伤害 | 敌机碰飞机 | 基地扣 10 | 固定值正确 |
| B4 | 底线伤害 | 敌机越线 | 基地扣 15 | 固定值正确 |
| B5 | 通关可行性 | 每关全装备通关（⚠️ S4 用 `BattleDebugLauncher`，UI 端到端→S5.8） | 全部可通关 | 无死局 |
| B6 | 难度递进 | 1→5 逐关 | 明显感受到压力递增 | 体感验证 |

---

### S4.3 伤害统计系统（2h）

#### 实施方案

**damageSourceId 传递链**（来自 GDD §技能贡献统计口径 v2.0 确认）：

```
BattleController 创建：
  Dictionary<int, int> _damageStats

sourceTag 分配：
  0 = 基础攻击（Bullet 默认值）
  1 = SK-P01 散射弹幕
  2 = SK-P02 追踪导弹
  3 = SK-P03 激光射线
  4 = SK-P04 火力全开（基础攻击期间 sourceTag 不变，仍=0）
  5 = SK-P05 护盾（无伤害输出）
  6 = SK-P06 冲击波
  7 = PA-04 反击弹幕
  100+ = DOT（取 DotConfigSO.DotId，如 4001=Burn）
```

**BulletWorld 扩展**：

```
BulletWorld 新增字段（PK-R1 UA-003：使用托管数组，不用 NativeArray）：
├── int[] _sourceTags   // 每弹丸来源标记，new int[capacity] 与 BulletCore[] 同生命周期
├── int 存储开销：2048 × 4 bytes = 8KB（可忽略）
```

**数据采集点**：

```
(1) FireBulletsEffect.Execute():
    → BulletWorld.Spawn(..., sourceTag: skill.SourceTagId)

(2) DealAreaDamageEffect.Execute():
    → DamageContext.SourceId = skill.SourceTagId

(3) BuffComponent.Tick() DOT tick:
    → DamageContext.SourceId = dot.DotId

(4) PA-04 尾翼反击:
    → BulletWorld.Spawn(..., sourceTag: 7)

(5) DamageDealer.DealDamage():
    → _damageStats[context.SourceId] += context.FinalDamage
```

**数据生命周期**（GDD §damageStats 生命周期）：

```
OnBattleStart() → 创建 _damageStats, 初始化键值=0
战斗中       → DamageDealer 累加
OnBattleEnd()  → 冻结，生成快照副本
               → 传递给结算面板
               → 战斗 Scene 可安全卸载
V2 不持久化    → 纯局内临时数据
```

**BattleController.OnBattleEnd 触发条件**（天命人决策：取消 WaitingClear，直接 Victory）：

```
判定逻辑（TickPlaying）：
  EntitySpawner.IsAllWavesCleared = true AND 无存活 Enemy Entity
  → EnterState(BattleState.Victory)
  → Time.timeScale = 0（暂停单局）
  → OnBattleEnd()
  → 冻结 damageStats
  → 读取 base.HealthComponent.CurrentHp
  → 计算星级
  → 构造 BattleResultData 传递给结算面板

设计理由：
├── 纵版射击节奏快，最后敌机死亡=关卡结束，体感自然
├── 残留敌弹由 timeScale=0 冻结，不影响判定
├── 省去 WaitingClear 状态和弹丸遍历逻辑
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| C1 | sourceTag 写入 | Profiler/断点 | 弹丸 sourceTag 与技能对应 | 值正确 |
| C2 | 伤害累计 | 通关后检查 _damageStats | 各来源伤害总和 = 敌机总 HP 损失 | 守恒 |
| C3 | 散射贡献 | 只带散射通关 | 散射占比 ~90%+（余下为基础攻击） | 比例合理 |
| C4 | 多技能分布 | 全装备通关（⚠️ S4 用 `BattleDebugLauncher`，UI 端到端→S5.8） | 各技能都有贡献记录 | 无遗漏 |
| C5 | DOT 记录 | 激光附带电弧 DOT | DOT 伤害独立记录 | sourceId=4003 |
| C6 | 数据冻结 | 战斗结束后 | _damageStats 不再变化 | 快照隔离 |
| C7 | int[] 回收 | 战斗结束 | _sourceTags 随 BulletWorld 清理 | 无内存泄漏 |

---

### S4.4 星级评价系统（1h）

#### 实施方案

**星级标准**（GDD §星级评价标准——[占位符]待 playtest）：

| 星级 | 条件 | 玩家感受 |
|------|------|---------|
| ⭐ | 通关（基地 HP > 0） | "过了但险" |
| ⭐⭐ | 通关 + 基地 HP ≥ 50% | "打得不错" |
| ⭐⭐⭐ | 通关 + 基地 HP ≥ 80% | "碾压！" |

**实现代码**：

```
BattleResultCalculator（纯静态工具类）
├── static int CalcStars(int currentHp, int maxHp):
│   → float ratio = (float)currentHp / maxHp
│   → if (ratio >= 0.8f) return 3
│   → if (ratio >= 0.5f) return 2
│   → if (currentHp > 0)  return 1
│   → return 0  // 失败
│   // 阈值为 const，后续 playtest 可调
```

**BattleResultData 数据结构**：

```
BattleResultData（传递给结算面板的值对象）
├── bool IsVictory
├── int Stars           // 0~3
├── int LevelIndex
├── int TotalKills
├── float BattleTime    // 秒
├── int CoinsEarned     // V3 预留，V2 固定值
├── Dictionary<int, int> DamageStats  // 快照副本
├── int BaseHpRemaining
├── int BaseHpMax
```

**星级持久化**：

```
SG_ProgressManager.SaveLevelResult(BattleResultData result):
  → int oldStars = _progress.LevelStars.GetValueOrDefault(result.LevelIndex, 0)
  → if (result.Stars > oldStars)
      _progress.LevelStars[result.LevelIndex] = result.Stars
      SaveToCloud()
  // 只更新最高星级，不覆盖
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| D1 | 三星 | 通关 HP ≥ 80% | 星级=3 | 正确 |
| D2 | 二星 | 通关 HP 50%~79% | 星级=2 | 正确 |
| D3 | 一星 | 通关 HP 1%~49% | 星级=1 | 正确 |
| D4 | 失败 | 基地 HP=0 | 星级=0, IsVictory=false | 正确 |
| D5 | 最高星级 | 同关第二次获得更低星级 | 存档中星级不被覆盖 | 只升不降 |
| D6 | 判定时机 | 最后敌机死亡 | 星级基于最终 HP，暂停+弹出 Victory | 即时判定 |

---

### S4.5 T4 DPS 计算面板——编辑器工具（1h）

#### 实施方案

**菜单路径**：`ShooterGame/Tools/DPS Calculator`

**EditorWindow 规格**：`DPSCalculatorWindow : EditorWindow`

- **输入**：EntityConfigSO 拖入槽 + Toggle[] 被动模拟(穿透/暴击/磁吸/尾翼) + [计算]按钮
- **输出**：裸 DPS 表格(各技能+基础攻击) → 含被动期望 DPS → DPS vs 关卡 HP 预算(理论清场时间对比)

**DPS 公式**：

```
单技能 DPS = EffectDamage / (CooldownTime + CastTime + RecoveryTime)
基础攻击 DPS = BulletDamage × (1 / AttackInterval)
暴击期望 = DPS × (1 + Uptime × CritRate × (CritMult-1))  // Uptime≈0.33, 加成~10%
穿透期望 = DPS × (1 + PierceUptime × 0.5)                 // Uptime≈0.375, 加成~19%
```

#### 验收方案

| # | 验收项 | 预期 | PASS |
|---|--------|------|------|
| E1 | 打开面板 | 正常打开无报错 | ✔ |
| E2 | 裸 DPS | 各技能 DPS 与手动计算一致 | ✔ |
| E3 | 被动模拟 | 勾选暴击→期望 DPS +~10% | ✔ |
| E4 | HP 预算 | 理论清场时间与游玩时间差异<30% | ✔ |
| E5 | 空配置 | 无技能→只显示基础攻击 | ✔ |

---

## 2. 新增代码文件清单

| 文件路径 | 类型 | 说明 |
|---------|------|------|
| `ShooterGame/Core/BattleResultCalculator.cs` | 新增 | 星级计算+结果数据结构 |
| `ShooterGame/Core/BattleResultData.cs` | 新增 | 战斗结果值对象 |
| `DanmakuSystem/BulletWorld.cs` | 修改 | 新增 _sourceTags int[] 托管数组 |
| `ShooterGame/Core/BattleController.cs` | 修改 | _damageStats 采集+OnBattleEnd 冻结 |
| `ShooterGame/Core/DamageDealer.cs` | 修改 | DealDamage 时累加统计 |
| `Editor/ShooterGame/DPSCalculatorWindow.cs` | 新增 | T4 DPS 计算面板 |

---

## 3. 新增/修改配置清单

| 配置 | 类型 | 数量 |
|------|------|------|
| 关卡 1~5 波次配置 | Luban xlsx | 5 关 × N 波 |
| 敌机 HP 倍率表 | Luban xlsx | 5 行 |
| 波间间歇配置 | Luban xlsx | 5 行 |
| 各关 DropTable 绑定 | Luban xlsx | 5 行 |

---

## 4. Sprint 4 验收总表

### 功能验收

| # | 场景 | 预期 | 状态 |
|---|------|------|------|
| F1 | 5 关全部可玩 | 全装备可通关，节奏符合设计（⚠️ S4 用 `BattleDebugLauncher`，UI 端到端→S5.8） | ⬜ |
| F2 | 难度递进 | 1→5 压力感递增 | ⬜ |
| F3 | 波间间歇 | 教学宽松→高压紧凑 | ⬜ |
| F4 | 敌机数值 | HP 倍率+伤害值正确 | ⬜ |
| F5 | 伤害统计 | 通关后 damageStats 准确 | ⬜ |
| F6 | 星级评价 | 三档阈值正确，只升不降 | ⬜ |
| F7 | 直接胜利判定 | 最后敌机死亡后 | 暂停+弹出 Victory 面板 | ⬜ |
| F8 | T4 DPS 面板 | 计算准确，与手动验证一致 | ⬜ |
| F9 | 存档兼容 | 新增 LevelStars 字段向后兼容 | ⬜ |
| F10 | 通关解锁 | 通关第 2/3/4/5 关后解锁正确技能/被动 | ⬜ |

### 性能验收

| # | 指标 | 目标 | 工具 |
|---|------|------|------|
| P1 | damageStats 累加 | < 0.001ms/次 | Dictionary 索引 O(1) |
| P2 | BulletWorld sourceTag | 8KB 额外内存 | int[] 托管数组 |
| P3 | 热路径零 GC | 0 bytes/frame | Deep Profile |

---

_创建于 2026-05-18 | Sprint 4 TDD v1.4_

**变更历史**：
- v1.0（2026-05-18）：初始版本
- v1.1（2026-05-18）：PK-R1 Unity 架构师回写（UA-008 两阶段判定）
- v1.2（2026-05-18）：PK-R2 Unity 编辑器工具开发者回写
- v1.3（2026-05-18）：天命人决策——取消 WaitingClear，最后敌机死亡直接暂停+Victory
- v1.4（2026-05-22）：标注 B5/C4/F1 全装备验收项使用 `_debugEquipAll` 临时方案，UI 端到端验收延后至 S5.8
- v1.5（2026-05-23）：`_debugEquipAll` 正式实现为 `BattleDebugLauncher` 组件，更新所有引用
