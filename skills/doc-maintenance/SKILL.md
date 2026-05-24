---
name: doc-maintenance
description: "文档维护技能。确保 Docs/Agent/ 目录和 skills/ 目录下的文档遵循统一规范：行数纪律、命名约定、Frontmatter 标准、INDEX 路由表同步、归档流程。当 Agent 需要新建文档、修改文档、拆分文档、归档文档、审计文档、更新 INDEX.md、或精简 Skill 文件时触发此技能。触发词包括：'新建文档'、'更新文档'、'拆分文档'、'归档文档'、'文档审计'、'更新索引'、'文档维护'、'瘦身 skill'。"
---

# 文档维护规范

本技能定义 `Docs/Agent/` 和 `skills/` 目录的文档维护规范。所有文档操作必须遵循，确保文档体系对 Agent 高效可检索。

---

## 核心规则

### R1：行数纪律

| 文件类型 | 行数上限 | 超限动作 |
|---------|---------|---------|
| INDEX 文件 / **SKILL.md 主体** | **150 行** | 精简或拆分到 `references/` |
| Detail Doc（TDD/GDD 子文件） | 400 行 | 拆分为 INDEX + 子文件 |
| MEMORY.md | 55 行（极端 70） | 精简过程流水 |

### R2：命名约定

- `Docs/Agent/`：`<SYSTEM>_NN_<TOPIC>.md`（全大写、下划线、两位序号）
- `skills/<name>/SKILL.md`：主体规则+结论；`references/` 放详细示例/模板/踩坑

### R3：Frontmatter（Detail Doc 必填）

`system`、`scope`、`last_verified`、`related_code` — 确保 Agent 能判断文档时效性。

### R4：INDEX.md 三路由表

A=任务路由 | B=代码→文档映射 | C=概念速查。任何文档变更后同步更新。

### R5：归档流程

PK 记录/验收报告/旧草案 → `Archive/<系统>/`。归档文件不出现在活索引中。

### R6：拆分流程

超 400 行 → 创建 `<SYSTEM>_INDEX.md`（≤100 行）+ 子文件（各 ≤400 行）+ Frontmatter + 更新 INDEX.md。

### R7：代码→文档联动

代码变更后查 INDEX.md 路由表 B → 验证文档一致性 → 更新 `last_verified`。

### R8：PK 回写上游同步

TDD 回写后检查 GDD 是否引用同一数值/命名，不一致则一并修正。

### R9：工作记忆精简

每次文档操作后检查 MEMORY.md：铁律/决策→保留 | 过程流水→删除/压缩。目标 ≤55 行。

---

## Skill 文件维护规范（R1 延伸）

**SKILL.md 主体 ≤ 150 行**，只保留：
1. Frontmatter（触发条件）
2. 规则/结论（编号、可扫描）
3. 最小可用示例（每规则 1 个最短片段）
4. 归档索引表

**归档到 `references/`**：
- 完整代码模板（>20 行大段示例）
- 踩坑经验详细还原
- 历史决策推导过程
- 完整 checklist 展开

**Skill 目录结构**：
```
skills/<name>/
├── SKILL.md        ← 主体（≤150行）
├── references/     ← 详细内容（按需加载）
└── scripts/        ← 可执行脚本（如有）
```

---

## 操作 Checklist

### 新建/修改文档
- [ ] 行数 ≤ 上限 | Frontmatter 完整 | INDEX.md 路由表同步

### 拆分文档
- [ ] 按 R6 流程 | 子文件各有 Frontmatter | 原文件删除 | INDEX 同步

### 归档
- [ ] 符合归档判定标准 | `git mv` 到 `Archive/` | INDEX 移除条目

### PK 回写后（R8）
- [ ] 列出字段变更 | 搜索上游旧值 | 修正 + 标注来源

### 工作记忆精简（R9）
- [ ] MEMORY.md 超 60 行 → 逐行精简 → 目标 ≤55 行

---

## 快速参考

| 系统 | 前缀 | 有效 system 值 |
|------|------|--------------|
| Entity-Component | `EC_TDD` | `entity-component` |
| 弹幕 | `DANMAKU` | `danmaku` |
| Atlas | `ATLAS_TDD` | `runtime-atlas` |
| OBB | `OBB_TDD` | `obb-collision` |
| 技能/Buff | `PHASE3A_TDD` | `phase3a-skill-buff` |
| ADR | `ADR` | `architecture` |
| 约定 | `CONV` | `conventions` |
| 编辑器 | `EDITOR_TOOLS_MANUAL` | `editor-tools` |
| SO 配置 | `SO_WORKFLOWS` | `so-config` |

## 文档编写原则

1. 结构化优先（表格/枚举/代码块）
2. 可执行性（完整路径/API/命令）
3. 字段级精确（属性名+类型+默认值+范围）
4. 上下文自包含
5. 行数纪律严格
