---
system: shootergame-v2-tdd
scope: sprint4-acceptance
last_verified: 2026-05-22
depends_on: [SG_V2_TDD_04_LEVEL_BALANCE]
related_code: Assets/_Game/Scripts/ShooterGame/Core/BattleController*, Assets/_Game/Scripts/ShooterGame/Core/BattleResult*, Assets/_Game/Editor/ShooterGame/SG_DPSCalculator*
---

# V2 Sprint 4 验收手册 — 关卡编排 + 数值平衡

> **版本**：v1.1  
> **日期**：2026-05-22  
> **验收人**：天命人  
> **前置**：Sprint 1~3 编码+逻辑验收已通过
>
> **⚠️ 全装备验收策略（v1.1 新增，2026-05-22；v1.2 更新，2026-05-23）**：
> S2.3 出战准备 UI 延后至 Sprint 5（S5.7），S4 阶段无法通过 UI 选择全装备出战。
> 涉及"全装备"的验收项在 S4 阶段通过 `BattleDebugLauncher` 组件完成**数据层/系统逻辑验证**；
> 使用方式：Battle 场景中 `BattleDebugLauncher` 组件 → Inspector 拖入技能/被动 SO → 直接 Play。
> **端到端 UI 流程验收**（通过出战准备 UI 选择全装备 → 进入战斗 → 验证结果）延后至 **S5.8 最终整合测试**。
> 参见：TDD_02 §S2.3 延后说明。

---

## 1. 自动验收结果（MCP 已执行 ✅）

以下项目已通过 MCP 自动验收，**不需要天命人手动验证**：

| # | 验收项 | 结果 | 验证方式 |
|---|--------|------|---------|
| COMPILE | 编译零错误零警告 | ✅ PASS | AssetDatabase.Refresh + CompilationPipeline |
| E1 | DPS Calculator 面板可打开 | ✅ PASS | ExecuteMenuItem("ShooterGame/Tools/DPS Calculator") |
| F1 | 5 关 LevelConfig SO 完整 | ✅ PASS | 5 个 SO 全部存在，WaveConfig 引用零 NULL |
| F4 | 敌机数值正确 | ✅ PASS | Normal=20HP, Fast=20/4.0, Scatter=60/1.2, Shooter=40/1.5, Elite=120/1.0 |
| B2 | 敌弹伤害值 | ✅ PASS | 射手=5, 散射=4, 精英=8 |
| B3 | 接触伤害 | ✅ PASS | 全部敌机 ContactDamage=10 |
| B4 | 底线突破伤害 | ✅ PASS | 全部敌机 BaseLineBreachDamage=15 |
| D1 | 三星计算 (HP≥80%) | ✅ PASS | CalcStars(80,100)=3, CalcStars(100,100)=3 |
| D2 | 二星计算 (HP 50%~79%) | ✅ PASS | CalcStars(50,100)=2, CalcStars(79,100)=2 |
| D3 | 一星计算 (HP 1%~49%) | ✅ PASS | CalcStars(1,100)=1, CalcStars(49,100)=1 |
| D4 | 失败计算 (HP=0) | ✅ PASS | CalcStars(0,100)=0 |
| ASSET | SO/Prefab 引用完整性 | ✅ PASS | BattleController 零 NULL，42 个 SpawnGroup 零 NULL |

---

## 2. 需天命人手动验收（PlayMode / 真机）

### 2.1 PlayMode 验收

#### F2: 难度递进（体感验证）

**操作步骤**：
1. Unity Editor → 打开 ShooterGame 战斗场景
2. 点击 Play，依次玩关卡 1→2→3→4→5
3. **观察**：
   - 关卡 1：只有普通机，非常轻松，"我好强！"的体感
   - 关卡 2：引入快速机，节奏加快
   - 关卡 3：引入射手机，需要开始躲弹
   - 关卡 4：引入散射机，弹幕压力明显
   - 关卡 5：引入精英机，8 波全装备才能通关（⚠️ S4 阶段通过 `BattleDebugLauncher` 启用全装备）
4. **判定**：感受到明显的压力递增 → PASS

#### F5: 伤害统计验证

**操作步骤**：
1. 打开 Unity Profiler 或在 `BattleController.FreezeBattleResult()` 处加断点
2. Play → 选择关卡 1，全装备出战（⚠️ S4 阶段通过 `BattleDebugLauncher` 启用，UI 流程验收延后至 S5.8）
3. 通关后检查控制台/断点：
   - `_damageStats` 字典中应有 key=0（基础攻击）对应的伤害值
   - 如果装备了散射技能，key=1 应有对应伤害
   - 如果有 DOT，key=4001+ 应有对应伤害
4. **判定**：各来源伤害之和 ≈ 所有被击杀敌机的 HP 总和 → PASS

> **简易验证法**：在 `BattleController.FreezeBattleResult()` 方法末尾已有 Debug.Log，直接看控制台输出。

#### F6: 星级判定时机

**操作步骤**：
1. Play → 关卡 1，全装备（⚠️ S4 阶段通过 `BattleDebugLauncher` 启用）
2. 通关时注意观察：
   - 最后一只敌机死亡的**瞬间**，游戏暂停（timeScale=0）
   - 不需要等剩余敌弹消失
3. **判定**：即时判定 + 暂停 → PASS

#### F7: 胜利/失败判定

**操作步骤**：
1. Play → 关卡 5，故意不操作让敌机越线
2. 基地 HP 降到 0 → 应触发 Defeat 状态
3. Play → 关卡 1，正常通关 → 应触发 Victory + 暂停
4. **判定**：两种状态都正确触发 → PASS

#### D5: 星级只升不降

**操作步骤**：
1. Play → 关卡 1，全力打到三星（基地 HP ≥ 80%）
2. 返回关卡选择 → 关卡 1 显示三星
3. 再次进入关卡 1，故意放一些敌机越线，确保基地 HP < 50%（一星通关）
4. 返回关卡选择 → 关卡 1 仍显示三星
5. **判定**：存档中的星级没有被低星覆盖 → PASS

### 2.2 DPS 面板验收

#### E2~E5: DPS 计算面板功能

**操作步骤**：
1. 菜单 → ShooterGame/Tools/DPS Calculator
2. **E2**：拖入 SG_Player EntityConfig → 点击[计算] → 检查裸 DPS 表格
   - 基础攻击 DPS 应 ≈ 40/s（10 dmg × 4/s）
3. **E3**：勾选"暴击被动" → 重新计算 → 期望 DPS 应比裸 DPS 高 ~10%
4. **E4**：查看 HP 预算表 → 理论清场时间与你的实际游玩时间差异 < 30%
5. **E5**：新建一个无技能的 EntityConfig → 只显示基础攻击 DPS
6. **判定**：数值合理且无报错 → PASS

### 2.3 真机验收（微信开发者工具）

#### 真机通用步骤

1. File → Build Settings → WebGL → Build
2. 用微信开发者工具打开导出项目
3. 玩关卡 1~3，确认：
   - 波次正常生成
   - 伤害数值与 Editor 一致
   - 通关后星级正确显示
   - 无 JS 报错
4. **判定**：WebGL 下无兼容性问题 → PASS

---

## 3. 验收总表

| # | 项目 | 验收方 | 状态 |
|---|------|-------|------|
| COMPILE | 编译 0E/0W | MCP | ✅ |
| ASSET | SO 引用完整 | MCP | ✅ |
| E1 | DPS 面板可打开 | MCP | ✅ |
| B2~B4 | 碰撞/敌弹伤害值 | MCP | ✅ |
| D1~D4 | 星级计算逻辑 | MCP | ✅ |
| F1 | 5 关 LevelConfig | MCP | ✅ |
| F4 | 敌机数值 | MCP | ✅ |
| F2 | 难度递进体感（`BattleDebugLauncher`） | 天命人 | ⬜ |
| F5 | 伤害统计准确性（`BattleDebugLauncher`） | 天命人 | ⬜ |
| F6 | 即时胜利判定（`BattleDebugLauncher`） | 天命人 | ⬜ |
| F7 | 胜利/失败触发 | 天命人 | ⬜ |
| D5 | 星级只升不降 | 天命人 | ⬜ |
| E2~E5 | DPS 面板功能 | 天命人 | ⬜ |
| 真机 | WebGL 兼容 | 天命人 | ⬜ |
| **F2/F5/F6 UI 端到端** | **全装备通过出战准备 UI 操作验收** | **→ 延后至 S5.8** | **⏳** |

---

## 4. 已知偏差（不阻塞验收）

| 偏差 | TDD 规格 | 实际 | 说明 |
|------|---------|------|------|
| HP 倍率机制 | 敌机 HP × 关卡倍率 | BaseHpRatio 用于基地 HP | Playtest 迭代项，非功能阻塞 |
| DropTable 差异化 | 关卡 5 掉率 40%/60% | 全局统一 DropTable | Playtest 迭代项 |
| 配置方式 | Luban xlsx | SO 资产 | S1~S3 已全部使用 SO，保持一致 |

---

_创建于 2026-05-22 | V2 Sprint 4 验收手册 v1.2_
_v1.1（2026-05-22）：标注全装备验收项的临时方案 + UI 端到端验收延后至 S5.8_
_v1.2（2026-05-23）：`_debugEquipAll` 正式实现为 `BattleDebugLauncher` 组件，更新所有引用_
