---
system: general
scope: audit-report-d3
last_verified: 2026-05-02
related_code: Docs/Agent/**
---

# D3 过时内容审计报告

> **审计日期**：2026-05-02  
> **审计范围**：D1+D2 完成后的所有活文档（34 个 .md）  
> **审计方法**：代码→文档对照，逐文件检查关键数据点

---

## 审计结果总览

| # | 文件/文件群 | 风险 | 状态 | 发现偏差数 |
|---|------------|------|------|-----------|
| 1 | EC_TDD 子文件群 | 🔴 高 | ⚠️ 已修正 | 5 |
| 2 | ADR 子文件群 | 🟡 中 | ✅ 一致 | 0 |
| 3 | ARCHITECTURE.md | 🟡 中 | ⚠️ 已修正 | 1 |
| 4 | SO_CATALOG.md | 🟡 中 | ⚠️ 已修正 | 1 |
| 5 | EDITOR_TOOLS.md | 🟡 中 | ⚠️ 已修正 | 1 |
| 6 | CONV 子文件群 | 🟢 低 | ⚠️ 已修正 | 2 |
| 7 | DEBUG_PLAYBOOK.md | 🟢 低 | ✅ 无偏差 | 0 |

**总计**：10 处偏差，全部已修正。

---

## 偏差详情

### 1. EC_TDD_02_CORE_ARCH.md — ComponentType 枚举

| 项目 | 偏差 |
|------|------|
| **文档** | `Attack = 9` 后注释"预留 10~15" |
| **代码** | `Buff = 10`（Phase 3A P3.4 新增） |
| **修正** | 补充 `Buff = 10` + 注释 |

### 2. EC_TDD_02_CORE_ARCH.md — TickOrders

| 项目 | 偏差 |
|------|------|
| **文档** | 缺少 Buff=50, Skill=160, Health=250；AutoAim 错写为 200 |
| **代码** | Buff=50, Decision=100, AutoAim=120, Attack=150, Skill=160, Health=250, Movement=300, Animation=400 |
| **修正** | 完整替换为代码实际值 |

### 3. EC_TDD_05_COMPONENTS.md — §4.8 SkillComponent

| 项目 | 偏差 |
|------|------|
| **文档** | 仅一行"技能槽改用固定长度数组"，无详细设计 |
| **代码** | 完整的 CD 状态机（188行），含 SkillState/TriggerMode/Effect 链/死亡中断 |
| **修正** | 补充完整设计：状态转换矩阵、触发模式、效果执行、字段表 |

### 4. EC_TDD_05_COMPONENTS.md — §4.9 AttackComponent

| 项目 | 偏差 |
|------|------|
| **文档** | `ComponentType.Skill`（复用）、`TickOrder = Decision+50`、`BulletTypeSO` |
| **代码** | `ComponentType.Attack`（独立）、`TickOrder = 150`（TickOrders.Attack）、`BulletPatternSO` + Buff 攻速修正 |
| **修正** | 重写为表格+代码片段格式，反映当前实现 |

### 5. EC_TDD_05_COMPONENTS.md — §4.10 BuffComponent

| 项目 | 偏差 |
|------|------|
| **文档** | 完全缺失 |
| **代码** | 207 行完整实现（8 槽位、乘法叠加、Clamp、by-ID 同步） |
| **修正** | 新增完整 §4.10 设计文档 |

### 6. ARCHITECTURE.md — Entity-Component 战斗层

| 项目 | 偏差 |
|------|------|
| **文档** | 只描述 SO 驱动的基础设施层，完全没有 Entity 战斗系统 |
| **修正** | 在架构总览后补充 Entity-Component 战斗层架构图（含 Phase 3A 组件） |

### 7. SO_CATALOG.md — Entity 系统 SO

| 项目 | 偏差 |
|------|------|
| **文档** | 缺少 EntityConfigSO、SkillConfigSO、BuffConfigSO、AIBehaviorSO、EntitySpawnWaveSO、SpriteAnimDataSO（共 6 个） |
| **修正** | 新增 "Entity" 分类 + EntityConfigSO/BuffConfigSO 核心字段表 |

### 8. EDITOR_TOOLS.md — Entity Inspector

| 项目 | 偏差 |
|------|------|
| **文档** | 缺少 EntityConfigSOEditor、SkillConfigSOEditor、AIBehaviorSOEditor、EntitySpawnWaveSOEditor |
| **修正** | 新增 "Entity 自定义 Inspector" 章节 + 快速参考表补充 |

### 9. CONV_01_NAMING.md — Template_ 前缀约定

| 项目 | 偏差 |
|------|------|
| **文档** | 缺少模板资产目录和 `Template_` 前缀命名约定 |
| **修正** | 在目录规范中补充 Entity SO 资产路径 + Template_ 前缀 |

### 10. CONV_02_CODING.md — SO 引用约束

| 项目 | 偏差 |
|------|------|
| **文档** | 缺少"SO 不能引用场景对象"铁律 |
| **修正** | 新增 `[AGENT] ScriptableObject 引用约束` 小节 + 代码示例 |

---

## 无偏差确认

| 文件 | 审计结论 |
|------|---------|
| ADR_01~05 所有 ADR 状态 | 全部"已接受"或"已接受+Supersede 注明"，与代码一致 |
| DEBUG_PLAYBOOK.md | 渲染/Atlas 调试流程正确，Entity 调试留待后续补充（低优先级） |
| NEWGAME_GUIDE.md | 🟢 低风险，本次未深审 |
| WECHAT_INTEGRATION.md | 🟢 低风险，本次未深审 |

---

## 后续建议

1. **Entity 调试条目**：当真机验证发现 Entity 相关 bug 时，补充到 DEBUG_PLAYBOOK.md
2. **SkillConfigSO 字段文档**：SO_CATALOG 可进一步补充 SkillConfigSO 的 Effects/TriggerMode/CastTime 等字段表
3. **CONV_03_PLATFORM.md**：可补充 Entity 系统在 WebGL 下的已知限制（如目前无特殊限制则无需）
