---
system: shootergame-v2-tdd
scope: pk-review-round2-editor-tools
last_verified: 2026-05-18
depends_on: [SG_V2_TDD_05_TOOLS_UI_POLISH, SG_V2_TDD_02_SKILL_EQUIP_ITEM]
---

# V2 TDD PK Round 2：Unity 编辑器工具开发者 vs 软件架构师

> **攻方**：Unity 编辑器工具开发者（关注 Editor 工具链、SO 工作流、Inspector 体验、OnValidate、Editor-Runtime 边界、调试/验证工具设计）
> **守方**：软件架构师（防守 TDD 设计合理性）
> **范围**：SG_V2_TDD_01~05 全部
> **轮次**：5 轮收敛（最大 8 轮）
> **日期**：2026-05-18

---

## 结果总表

| ID | 严重度 | 主题 | 结论 | 涉及 TDD |
|----|--------|------|------|----------|
| ET-001 | 🟡 | EditorBulletSimulator 驱动机制 | ✅ 接受 | TDD_05 |
| ET-002 | 🟡 | 增量验证策略 | ⚠️ 部分接受(V2仅加ValidateSelected) | TDD_05 |
| ET-003 | 🟡 | 解锁表 OnValidate + T8 验证 | ✅ 接受 | TDD_02 + TDD_05 |
| ET-004 | 🟢 | SkillPreview Undo 支持 | ✅ 接受 | TDD_05 |
| ET-005 | 🟢 | BuffConfigSO CustomEditor | ✅ 接受(新增S5.8) | TDD_05 |
| ET-006 | 🟡 | OnValidate vs T8 职责+DRY | ✅ 接受 | TDD_05 |
| ET-007 | 🟡 | asmdef 隔离 | ✅ 接受 | TDD_05 |
| ET-008 | 🟢 | BuffOverview 排序持久化 | ✅ 接受 | TDD_05 |
| ET-009 | 🟡 | 多SceneView+坐标系 | ✅ 接受 | TDD_05 |
| ET-010 | 🟡 | LevelConfigSO 验证缺失 | ✅ 接受 | TDD_05 |
| ET-011 | 🟢 | 菜单路径约定统一 | ✅ 接受 | TDD_05 |
| ET-012 | 🟡 | EditMode Test 策略 | ⚠️ 部分接受(仅SOValidationRules) | TDD_05 |
| ET-013 | 🟢 | DotConfigSO CustomEditor | ❌ 拒绝(4字段够简单) | — |
| ET-014 | 🟢 | SimBullet 性能上界 | ✅ 接受 | TDD_05 |
| ET-015 | 🟢 | T8 错误输出可点击定位 | ✅ 接受 | TDD_05 |

**统计**：15 问题 | 12 接受 | 2 部分接受 | 1 拒绝 | 5 轮收敛

---

## 回写清单

### TDD_05 修改（主体）

| 位置 | 修改内容 | 来源 |
|------|---------|------|
| 新增 §0 工具约定 | 菜单路径约定 + asmdef 依赖 + 验证职责划分 + 错误格式 | ET-007/006/011/015 |
| S5.1 | 驱动机制（EditorApplication.update）| ET-001 |
| S5.1 | Undo 策略 | ET-004 |
| S5.1 | SceneView 坐标系（World Space）| ET-009 |
| S5.1 | MAX_SIM_BULLETS=500 | ET-014 |
| S5.2 | 排序偏好 EditorPrefs 持久化 | ET-008 |
| S5.3 | ValidateSelected 菜单 | ET-002 |
| S5.3 | ValidateUnlockTables | ET-003 |
| S5.3 | ValidateAllLevelConfigs | ET-010 |
| S5.3 | 验收 C8~C11 新增 | ET-002/003/010/012 |
| 新增 S5.8 | BuffConfigEditor (1h) | ET-005 |
| §2 文件清单 | +SOValidationRules.cs +BuffConfigEditor.cs +Tests | ET-006/005/012 |
| §3 验收总表 | +G15 +G16 | ET-005/012 |
| 工时 | 18.5h → 19.5h | ET-005 |
| 版本 | v1.0 → v1.2 | — |

### TDD_02 修改

| 位置 | 修改内容 | 来源 |
|------|---------|------|
| S2.1 | SkillUnlockTableSO/PassiveUnlockTableSO OnValidate 规格 | ET-003 |
| 版本 | v1.0 → v1.2 | — |

---

## 需天命人决策的事项

> 本轮 PK 无需天命人决策的事项。所有修改均由守方直接确认或拒绝。

---

## 详细回合记录

### Round 1（ET-001 ~ ET-005）

**ET-001**：EditorBulletSimulator 缺少 Editor 循环驱动机制 → 接受，明确 EditorApplication.update 为驱动源

**ET-002**：SOConsistencyValidator 缺乏增量验证策略 → 部分接受：V2 SO<50 不需增量缓存，但补充 ValidateSelected 菜单

**ET-003**：SkillUnlockTableSO 缺少 Editor 验证 → 接受，补充 OnValidate + T8 校验

**ET-004**：SkillPreviewWindow 缺少 Undo → 接受，Undo.RecordObject

**ET-005**：BuffConfigSO CustomEditor 缺失 → 接受，新增 S5.8（+1h 工时）

### Round 2（ET-006 ~ ET-008）

**ET-006**：OnValidate vs T8 职责重叠 → 接受，抽取 SOValidationRules 共享类

**ET-007**：Editor asmdef 隔离 → 接受，明确引用链

**ET-008**：BuffOverview 排序持久化 → 接受，EditorPrefs

### Round 3（ET-009 ~ ET-011）

**ET-009**：多 SceneView 问题 → 接受，明确 World Space 坐标系

**ET-010**：LevelConfigSO 验证缺失 → 接受，ValidateAllLevelConfigs

**ET-011**：菜单路径不一致 → 接受，统一 Tools/Validate/Create 三分类

### Round 4（ET-012 ~ ET-013）

**ET-012**：Editor 测试策略 → 部分接受，仅 SOValidationRules 有 EditMode Test

**ET-013**：DotConfigSO CustomEditor → 拒绝（4 字段太简单）

### Round 5（ET-014 ~ ET-015）

**ET-014**：SimBullet 性能上界 → 接受，MAX_SIM_BULLETS=500

**ET-015**：T8 错误可点击定位 → 接受，Debug.LogError context=soAsset

---

_创建于 2026-05-18 | PK-R2 记录 v1.0_
