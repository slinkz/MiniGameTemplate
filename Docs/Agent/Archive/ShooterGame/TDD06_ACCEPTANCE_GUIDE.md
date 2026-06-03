# TDD-06 验收指南：普攻升格为技能系统

> **日期**：2026-05-25  
> **状态**：✅ **验收通过**（2026-05-25 22:19）  
> **编码状态**：P1~P10 + P11 全部完成  
> **验收方式**：MCP 自动配置 + PlayTest 天命人确认

---

## 一、自动验收（MCP / 编译）

| # | 检查项 | 方法 | 预期 |
|---|--------|------|------|
| A1 | 零编译错误 | Unity Console 检查 | 0 Error |
| A2 | Obsolete 警告 | Unity Console 搜 "Obsolete" | 仅 AttackComponent 相关（预期） |

---

## 二、需天命人操作的验收步骤

### Step 1：创建 SK_NormalAttack.asset

1. 右键 `Assets/_Game/Configs/ShooterGame/Skills/` → Create → Entity → SkillConfig
2. 命名为 `SK_NormalAttack`
3. 配置如下：

| 字段 | 值 |
|------|---|
| DisplayName | 基础射击 |
| TriggerMode | Auto |
| AimMode | FixedForward |
| IsNormalAttack | ✅ (true) |
| CooldownTime | 0.25（运行时被覆盖，填默认即可） |
| CastTime | 0 |
| RecoveryTime | 0 |
| Effects[0] | FireBulletsEffect |
| ↳ Pattern | 赋值当前玩家使用的 BulletPatternSO |
| ↳ FireOffset | 从 EntityConfigSO.AttackFireOffset 抄写 |
| ↳ UseForwardDirection | false（AimMode 已控制方向） |

4. **复制一份到** `Assets/_Game/Resources/ShooterGame/SK_NormalAttack.asset`（Resources 兜底用）

### Step 2：配置 EntityConfigSO（玩家）

1. 选中玩家的 EntityConfigSO（如 `PlayerConfig.asset`）
2. 在 Inspector 中找到 **V2 TDD-06: 普攻技能** 区域
3. 将 Step 1 创建的 `SK_NormalAttack` 拖入 **NormalAttackSkill** 字段

### Step 3：执行迁移工具

1. 菜单 `Tools/ShooterGame/Migration/Remove Attack from All EntityConfigs`
2. 确认弹窗提示修改数量
3. 菜单 `Tools/ShooterGame/Migration/Verify No Attack Components`
4. 确认弹窗显示 "Verification Passed"

### Step 4：配置 BattleDebugLauncher

1. 选中 Battle 场景中的 BattleDebugLauncher 组件
2. 在 **普攻配置（TDD-06）** 区域拖入 `SK_NormalAttack`
3. （可选）不拖也能工作——三层兜底会从 EntityConfigSO 读取

### Step 5：PlayTest 验证

1. **正常流程启动**：MainMenu → LevelSelect → Sortie → Battle
   - ✅ 玩家自动射击（向上）
   - ✅ 射速与改造前一致（由 EntityConfigSO.AttackInterval 决定）
   - ✅ 技能 Slot[1~5] 正常释放
   - ✅ 火力全开 Buff 影响射速（CD 消耗加速）

2. **直跑 Battle 场景**：
   - ✅ 三层兜底：从 EntityConfigSO 或 Resources 加载普攻配置
   - ✅ Console 无 LogError

3. **Buff 弹丸数验证**：
   - 拾取"多发弹幕"道具
   - ✅ 弹丸数量增加（BulletCountModifier > 1）

### Step 6：Inspector 验证

1. 新建/打开任意 SkillConfigSO
   - ✅ AimMode、IsNormalAttack 字段可见
   - ✅ IsNormalAttack=true 时 CooldownTime 置灰 + HelpBox 提示
   - ✅ AttachedDotConfig、SourceTagId 字段可见

---

## 三、验收通过标准

| 目标 | 验收标准 | 方法 |
|------|---------|------|
| G1 | 零 AttackComponent 实例化 | Step 3 Verify 通过 |
| G2 | 普攻 = Slot[0] | Step 5-1 射击正常 |
| G3 | AimMode 数据驱动 | SK_NormalAttack.AimMode=FixedForward，射击方向向上 |
| G4 | Buff 攻速统一影响 Slot[0] | Step 5-3 多发弹幕生效 |
| G5 | 零行为回归 | Step 5 全部通过 |

---

## 四、已知限制

- SkillCDPanel 的 HUD 集成尚未完成（Sprint 5 遗留），UI 面板不会显示 — 不影响本次验收
- SkillSlot.CooldownProgress 属性在有 OverrideSlotCooldown 时计算不准确 — 后续 UI 集成时修复
