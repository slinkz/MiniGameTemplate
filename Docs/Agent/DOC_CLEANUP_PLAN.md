---
system: general
scope: doc-cleanup-plan
last_verified: 2026-05-02
related_code: Docs/Agent/**
---

# 文档整理计划 — 落地执行方案

> **目标**：清理 Docs/Agent/ 目录，让 Agent 读取高效、内容可信、维护成本低；补齐操作手册使 Agent 能自主使用编辑器工具和配置 SO。
>
> **原则**：过程文档归档不删除（Git 追溯友好）、活文档≤400 行、索引即入口。
>
> **编写视角**：本项目以 Agent 开发为主，所有文档以"方便 Agent 理解和操作"为首要目标。

---

## 文档编写原则（Agent-First）

1. **结构化优先**：用表格、枚举列表、代码块表达信息，避免大段散文
2. **可执行性**：操作步骤必须包含完整的菜单路径/API 调用/文件路径，Agent 能直接按步骤操作
3. **字段级精确**：SO 配置文档精确到每个字段名（C# 属性名）、类型、默认值、合法范围
4. **上下文自包含**：每个文档独立可读，不假设 Agent 已读过其他文件（可用链接但不依赖）
5. **行数纪律**：单文件 ≤400 行，超过即拆分

---

## 当前状态快照（2026-05-02）

### 文件统计

| 指标 | 数据 |
|------|------|
| Agent/ 根目录 .md 文件总数 | 37 个 |
| 超 400 行文件 | 13 个 |
| 已在 Archive/ 的 | 6 个 |
| Phase3A TDD 已拆分版本 | INDEX + 5 子文件（保留） |

### 行数排行

| 行数 | 文件 | 分类 |
|------|------|------|
| 1992 | ENTITY_COMPONENT_TDD.md | 活文档（需拆分） |
| 1263 | ARCHITECT_DECISION_RECORD.md | 活文档（需拆分） |
| 1175 | PHASE3A_TDD.md | **冗余**（已有拆分版，删除） |
| 824 | RUNTIME_ATLAS_SYSTEM_TDD.md | 活文档（需拆分） |
| 779 | CONVENTIONS.md | 活文档（需拆分） |
| 772 | PHASE3A_PK_ROUND4.md | 过程文档（归档） |
| 730 | PHASE3A_PK_ROUND3.md | 过程文档（归档） |
| 657 | PHASE3_DESIGN.md | 过程文档（归档） |
| 511 | ENTITY_COMPONENT_PK_R4.md | 过程文档（归档） |
| 503 | PHASE3A_PK_ROUND2.md | 过程文档（归档） |
| 501 | OBB_OBSTACLE_TDD.md | 活文档（需拆分） |
| 441 | PHASE3A_TDD_03_P32_P33.md | 已拆分子文件（微超，暂保留） |
| 434 | ENTITY_COMPONENT_PK_R2.md | 过程文档（归档） |

---

## Phase D1：过程文档归档

**操作**：Git `mv` 移动文件到 Archive/ 子目录。

### 目录结构（新建）

```
Archive/
├── EntityComponent/     # EC 框架 PK 评审记录 R1~R6
├── Phase3/              # Phase 3 旧设计草案
├── Phase3A/             # Phase 3A 过程（PK/Question/验收/评审）
├── OBB/                 # OBB 障碍物评审
├── VFX/                 # VFX 调研
└── General/             # 通用归档
```

### 移动清单（17 个文件）

| # | 文件 | 行数 | → 目标 |
|---|------|------|--------|
| 1 | `ENTITY_COMPONENT_PK.md` | 143 | `Archive/EntityComponent/` |
| 2 | `ENTITY_COMPONENT_PK_R2.md` | 434 | `Archive/EntityComponent/` |
| 3 | `ENTITY_COMPONENT_PK_R3.md` | 150 | `Archive/EntityComponent/` |
| 4 | `ENTITY_COMPONENT_PK_R4.md` | 511 | `Archive/EntityComponent/` |
| 5 | `ENTITY_COMPONENT_PK_R5.md` | 278 | `Archive/EntityComponent/` |
| 6 | `ENTITY_COMPONENT_PK_R6.md` | 289 | `Archive/EntityComponent/` |
| 7 | `PHASE3A_PK_ROUND2.md` | 503 | `Archive/Phase3A/` |
| 8 | `PHASE3A_PK_ROUND3.md` | 730 | `Archive/Phase3A/` |
| 9 | `PHASE3A_PK_ROUND4.md` | 772 | `Archive/Phase3A/` |
| 10 | `PHASE3A_QUESTION.md` | 343 | `Archive/Phase3A/` |
| 11 | `PHASE3A_CODE_REVIEW_REPORT.md` | 104 | `Archive/Phase3A/` |
| 12 | `PHASE3A_ACCEPTANCE_REPORT.md` | 66 | `Archive/Phase3A/` |
| 13 | `PHASE3_DESIGN.md` | 657 | `Archive/Phase3/` |
| 14 | `Question.md` | 229 | `Archive/General/` |
| 15 | `OBB_OBSTACLE_QUESTION.md` | 187 | `Archive/OBB/` |
| 16 | `VFX_RESEARCH.md` | 184 | `Archive/VFX/` |
| 17 | `REFACTOR_PLAN.md` | 366 | `Archive/General/` |

### 删除清单（1 个冗余文件）

| 文件 | 理由 |
|------|------|
| `PHASE3A_TDD.md`（1175行） | 已被 `PHASE3A_TDD_INDEX.md` + 5 个子文件取代 |

### D1 执行后效果

- Agent/ 根目录：37 → **19 个** .md 文件
- 归档后保留的活文档列表见下方

---

## Phase D2：长文档拆分

**拆分阈值**：活文档单文件 ≤400 行。INDEX ≤100 行。

**Frontmatter 要求**：每个拆分产出的子文件（非 INDEX）头部必须添加 Frontmatter（见 D4 §4.3 标准），方便 Agent 快速判断是否需要深读。

### 拆分清单（5 个文件）

#### 2.1 ENTITY_COMPONENT_TDD.md（1992 行）

**拆分方案**：INDEX + 7 子文件

| 文件名 | 内容 | 预估行数 |
|--------|------|---------|
| `EC_TDD_INDEX.md` | 版本历史 + 目录 + 子文件链接 | ~60 |
| `EC_TDD_01_OVERVIEW.md` | 架构概述、组件枚举、核心设计原则 | ~300 |
| `EC_TDD_02_ENTITY_POOL.md` | Entity + EntityPool + EntityManager | ~300 |
| `EC_TDD_03_COMPONENTS_CORE.md` | State/Health/Movement/Collision 组件 | ~350 |
| `EC_TDD_04_COMPONENTS_COMBAT.md` | Attack/AutoAim/Skill/Buff 组件 | ~350 |
| `EC_TDD_05_SYSTEMS.md` | Bootstrap/DamageDealer/边界击杀/空间查询 | ~300 |
| `EC_TDD_06_VIEW.md` | ViewPrefab/SpriteAnim/Gizmo | ~200 |
| `EC_TDD_07_APPENDIX.md` | 未决项/Phase 路线图/验收矩阵 | ~200 |

**拆分后删除**：`ENTITY_COMPONENT_TDD.md`

#### 2.2 ARCHITECT_DECISION_RECORD.md（1263 行）

**拆分方案**：INDEX + 4 子文件（按 ADR 编号分组）

| 文件名 | 内容 | 预估行数 |
|--------|------|---------|
| `ADR_INDEX.md` | 总览表 + 状态速查 | ~80 |
| `ADR_01_FOUNDATION.md` | ADR-001~010（基础架构决策） | ~350 |
| `ADR_02_DANMAKU.md` | ADR-011~020（弹幕系统决策） | ~350 |
| `ADR_03_ENTITY.md` | ADR-021~030（Entity 系统决策） | ~350 |
| `ADR_04_RECENT.md` | ADR-031+（最新决策，持续追加） | ~200 |

**拆分后删除**：`ARCHITECT_DECISION_RECORD.md`

#### 2.3 RUNTIME_ATLAS_SYSTEM_TDD.md（824 行）

**拆分方案**：INDEX + 3 子文件

| 文件名 | 内容 | 预估行数 |
|--------|------|---------|
| `ATLAS_TDD_INDEX.md` | 概述 + 目录 | ~60 |
| `ATLAS_TDD_01_DESIGN.md` | 架构设计、API、内存管理 | ~350 |
| `ATLAS_TDD_02_IMPL.md` | 实现细节、任务步骤 | ~250 |
| `ATLAS_TDD_03_ACCEPTANCE.md` | 验收标准、测试结果 | ~200 |

**拆分后删除**：`RUNTIME_ATLAS_SYSTEM_TDD.md`

#### 2.4 CONVENTIONS.md（779 行）

**拆分方案**：INDEX + 3 子文件

| 文件名 | 内容 | 预估行数 |
|--------|------|---------|
| `CONV_INDEX.md` | 总览 + 分类索引 | ~60 |
| `CONV_01_NAMING.md` | 命名规范（文件/类/变量/SO/Prefab） | ~300 |
| `CONV_02_CODING.md` | 编码规范（GC/性能/架构层级） | ~250 |
| `CONV_03_WORKFLOW.md` | 工作流约定（Git/文档/变更包） | ~200 |

**拆分后删除**：`CONVENTIONS.md`

#### 2.5 OBB_OBSTACLE_TDD.md（501 行）

**拆分方案**：INDEX + 2 子文件

| 文件名 | 内容 | 预估行数 |
|--------|------|---------|
| `OBB_TDD_INDEX.md` | 概述 + 目录 | ~50 |
| `OBB_TDD_01_DESIGN.md` | 碰撞设计、数学模型 | ~250 |
| `OBB_TDD_02_IMPL.md` | 实现 + 测试 + 验收 | ~220 |

**拆分后删除**：`OBB_OBSTACLE_TDD.md`

### D2 执行后效果

- 删除 5 个超长单体 + 1 个冗余 = **删 6 个文件**
- 新增 INDEX×5 + 子文件×19 = **新增 24 个文件**
- 但这些子文件按前缀分组，Agent 通过 INDEX 文件发现即可
- 每个文件 ≤400 行（仅 PHASE3A_TDD_03 微超 441 行，可接受）

---

## Phase D3：过时内容审计与修正

### 审计范围（D1+D2 后的活文档）

| # | 文件 | 风险 | 审计重点 |
|---|------|------|---------|
| 1 | EC_TDD 子文件群 | 🔴 高 | Phase 2~3A 新增组件是否完整记录；ComponentType 枚举 vs 代码 |
| 2 | ADR 子文件群 | 🟡 中 | ADR 状态是否与实际一致（已接受/已废弃） |
| 3 | ARCHITECTURE.md | 🟡 中 | Entity 系统模块是否涵盖 Phase 3A 新增 |
| 4 | SO_CATALOG.md | 🟡 中 | SkillConfigSO/BuffConfigSO/新字段是否记录 |
| 5 | EDITOR_TOOLS.md | 🟡 中 | SkillConfigSOEditor 等是否记录 |
| 6 | CONV 子文件群 | 🟢 低 | 新约定（Template_ 前缀）是否收录 |
| 7 | DEBUG_PLAYBOOK.md | 🟢 低 | Entity 调试条目是否需补充 |
| 8 | NEWGAME_GUIDE.md | 🟢 低 | 与当前项目结构是否一致 |
| 9 | WECHAT_INTEGRATION.md | 🟢 低 | WeChat SDK 版本是否最新 |

### 审计方法

对每个文件输出状态标记：
- ✅ 与代码一致 — 不修改
- ⚠️ 有偏差 — 列出偏差 + 就地修正
- ❌ 严重过时 — 需大幅重写（优先级最高）

### 审计产出

`Docs/Agent/AUDIT_REPORT.md`（审计完成后产出，列出所有偏差及修正 diff 摘要）

---

## Phase D4：索引架构 + 长效机制（重要升级）

> **设计依据**：基于 Anthropic Context Engineering、Codified Context（arXiv 2602.20478）、Augment AGENTS.md 和 Cursor Agent Best Practices 四大业界实践调研。核心结论：**INDEX 不是目录列表，是 Agent 的 GPS 路由系统。**

### 4.1 三层索引架构

```
┌─────────────────────────────────────────────────────────┐
│ 第一层：INDEX.md（≤150行，每次会话必读）                  │
│   → 三张路由表，解决"我要做X→读哪个文件"               │
├─────────────────────────────────────────────────────────┤
│ 第二层：Domain INDEX（每系统一个，≤100行）                │
│   → 该系统的文档子文件清单 + 一句话摘要                  │
│   → EC_TDD_INDEX / ADR_INDEX / CONV_INDEX 等            │
├─────────────────────────────────────────────────────────┤
│ 第三层：Detail Docs（≤400行，按需加载）                   │
│   → 精确的设计/配置/API 细节                            │
│   → EC_TDD_01 / ADR_01 / SO_WORKFLOWS_02 等            │
└─────────────────────────────────────────────────────────┘
```

**核心价值**：Agent 通过 INDEX.md 的路由表一步定位到目标文件，无需 grep 全目录。

### 4.2 总索引文件设计（新建 `Docs/Agent/INDEX.md`，≤150 行）

INDEX.md 包含 **三张路由表**，解决 Agent 最常见的三类检索需求：

#### 路由表 A：任务路由（"我要做 X → 读哪个文件"）

```markdown
## 🎯 任务路由

| 我要做什么 | 读什么文件 | 备注 |
|-----------|-----------|------|
| 新建一种敌人 | SO_WORKFLOWS_02 + EC_TDD_04 | SO 创建 + 组件配置 |
| 新建一个技能 | SO_WORKFLOWS_02 §SkillConfigSO + PHASE3A_TDD_03 | 技能 SO + Effect 链路 |
| 新建一个 Buff | SO_WORKFLOWS_02 §BuffConfigSO + PHASE3A_TDD_04 | Buff SO + Duration/叠加 |
| 新增子弹花样 | SO_WORKFLOWS_03 + ATLAS_TDD_01 §API | 弹幕 SO + Atlas 纹理 |
| 修改碰撞逻辑 | EC_TDD_03 §Collision + OBB_TDD_01 | 碰撞组件 + OBB 数学 |
| 新增 ADR 决策 | ADR_INDEX + ADR_04_RECENT | 追加到最新 ADR 子文件 |
| 新增编辑器工具 | EDITOR_TOOLS_MANUAL_INDEX | 模板 + 注册流程 |
| 配置微信广告 | WECHAT_INTEGRATION §Ads | 广告 ID + 回调 |
| 调试性能问题 | DEBUG_PLAYBOOK §Performance | Profiler + DC 排查 |
| 从零开始新项目 | NEWGAME_GUIDE | 全流程 |
```

#### 路由表 B：代码→文档映射（"我改了 X.cs → 哪个文档可能过时"）

```markdown
## 🔗 代码→文档映射

| 代码路径/模式 | 对应文档 | 说明 |
|--------------|---------|------|
| `EntitySystem/*.cs` | EC_TDD_INDEX 相关子文件 | 组件/系统变更 |
| `EntitySystem/Components/Skill*` | PHASE3A_TDD_03 | 技能子系统 |
| `EntitySystem/Components/Buff*` | PHASE3A_TDD_04 | Buff 子系统 |
| `Danmaku/**/*.cs` | SO_WORKFLOWS_03 + ATLAS_TDD_INDEX | 弹幕+渲染 |
| `Editor/**/*.cs` | EDITOR_TOOLS_MANUAL_INDEX | 工具注册 |
| `*ConfigSO.cs` / `*SO.cs` | SO_CATALOG + SO_WORKFLOWS_INDEX | SO 目录+流程 |
| `EntitySystemBootstrap.cs` | EC_TDD_05 §Bootstrap | 胶水层 |
| `CONVENTIONS.md` 中引用的规则 | CONV_INDEX | 命名/编码/工作流 |
```

#### 路由表 C：概念速查（"X 是什么？在哪定义的？"）

```markdown
## 📖 概念速查

| 概念/术语 | 定义位置 | 一句话 |
|-----------|---------|--------|
| PendingDespawn | EC_TDD_02 §EntityPool | Entity 标记待回收但本帧不立即销毁 |
| DamageContext | PHASE3A_TDD_03 §DamageDealer | 伤害传递结构体（替代裸 int） |
| ComponentType 枚举 | EC_TDD_01 §枚举定义 | O(1) 组件访问的位标志 |
| TypeRegistry | ADR_INDEX §ADR-030 | 弹幕类型注册（内化到框架） |
| RuntimeAtlas | ATLAS_TDD_01 §架构 | 运行时动态纹理合批 |
| CampUtility | PHASE3A_TDD_03 §阵营 | 阵营判定工具类 |
| 变更包 | CONV_03 §变更包工作流 | 每次修改的归档记录 |
| Template_ 前缀 | CONV_01 §SO命名 | 模板 SO 资产命名约定 |
| TickOrder | EC_TDD_05 §更新顺序 | 系统 Tick 执行优先级 |
| EntityEventBus | EC_TDD_02 §事件 | 零 GC 预分配事件总线 |
```

### 4.3 子文件 Frontmatter 标准

D2 拆分时，每个子文件头部添加 YAML-like Frontmatter，供 Agent 快速判断是否需要深读：

```markdown
---
system: entity-component
scope: components-combat
last_verified: 2026-05-02
depends_on: [EC_TDD_01, EC_TDD_02]
related_code: Assets/_Framework/EntitySystem/Components/Attack*, Skill*, Buff*
---
```

字段说明：
| 字段 | 用途 |
|------|------|
| `system` | 所属系统（Agent 可按系统过滤） |
| `scope` | 文档覆盖范围（一两个词） |
| `last_verified` | 上次审计确认与代码一致的日期 |
| `depends_on` | 前置阅读（Agent 可按需加载） |
| `related_code` | 关联代码路径（glob 格式，审计时自动匹配） |

### 4.4 文档维护规则（追加到 MEMORY.md）

```markdown
## 文档维护铁律

1. **活文档单文件 ≤400 行** — 超过即拆分为 INDEX + 子文件
2. **过程文档即时归档** — PK/验收/Question 完成后移入 Archive/
3. **Phase 完成后审计** — 相关活文档 `last_verified` 日期更新
4. **拆分命名规范** — `<SYSTEM>_INDEX.md` + `<SYSTEM>_NN_<TOPIC>.md`
5. **INDEX.md 三路由表同步** — 任何新增/删除文档必须更新 INDEX.md 对应路由表
6. **Frontmatter 强制** — 每个 Detail Doc 必须有 frontmatter（system/scope/last_verified/related_code）
7. **代码→文档联动** — 修改代码时检查路由表 B，标记相关文档为"待审计"
```

### 4.5 INDEX.md 维护成本分析

| 维护事件 | 频率 | 动作 |
|---------|------|------|
| 新建 Detail Doc | ~1次/Phase | 路由表 A/B/C 各加 1~2 行 |
| 重命名/删除 Doc | 极少 | 更新对应路由行 |
| 新增概念/术语 | ~3次/Phase | 路由表 C 加行 |
| 新增代码模块 | ~2次/Phase | 路由表 B 加行 |

**结论**：维护成本极低（每个 Phase 改 INDEX.md ~5-10 行），但检索效率提升是质变级的。

---

## Phase D5：编辑器工具使用手册

**产出文件**：`Docs/Agent/EDITOR_TOOLS_MANUAL.md`

### 覆盖范围

共 **17 个菜单工具** + **7 个自定义 Inspector** + **2 个自动处理器**。

### 文档结构

每个工具按以下模板编写：

```markdown
## [工具名]

**菜单路径**：`Tools/...`
**源码**：`Assets/_Framework/Editor/.../FileName.cs`
**用途**：一句话

### 前置条件
- （列出所有必须满足的条件）

### 操作步骤
1. （精确到按钮/字段/选项）
2. ...

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/...");
// 或等效 API 调用
```

### 输出/副作用
- （会修改什么文件、生成什么资产）

### 常见错误
| 错误信息 | 原因 | 解决 |
|---------|------|------|
```

### 行数预估

17 个工具 × ~20 行/工具 + 7 Inspector × ~10 行 + 头尾 = ~420 行

**→ 略超 400 行，拆分为**：
- `EDITOR_TOOLS_MANUAL_INDEX.md`（~60 行，速查表 + 链接）
- `EDITOR_TOOLS_MANUAL_01_BUILD.md`（构建/导出类工具，~150 行）
- `EDITOR_TOOLS_MANUAL_02_VALIDATE.md`（校验/审计类工具，~120 行）
- `EDITOR_TOOLS_MANUAL_03_ENTITY.md`（Entity 系统工具，~100 行）
- `EDITOR_TOOLS_MANUAL_04_INSPECTORS.md`（自定义 Inspector + 自动处理器，~120 行）

---

## Phase D6：SO 配置流程指南

**产出文件**：`Docs/Agent/SO_CONFIG_WORKFLOWS.md`（拆分）

### 覆盖范围

33 个 SO 类型，按系统分组：
- 核心配置（3 个）
- Entity 系统（6 个）
- 弹幕系统（9 个）
- VFX 系统（2 个）
- 渲染系统（1 个）
- 基础设施（12 个）

### 文档结构

每个 SO 按以下模板编写：

```markdown
## [SO 类型名]

**命名空间**：`MiniGameTemplate.XXX`
**CreateAssetMenu**：`Create/...`
**实例目录**：`Assets/_Game/...`

### 字段清单
| 字段名 | C# 属性 | 类型 | 默认值 | 合法范围 | 说明 |
|--------|---------|------|--------|---------|------|

### 创建方式
1. 右键菜单：`Create/Entity/XXX`
2. SOCreationWizard（如适用）
3. Agent 代码（ScriptableObject.CreateInstance + AssetDatabase.CreateAsset）

### 关联资产
- 引用的其他 SO
- 被谁引用

### 验证方法
- Inspector 校验 / Validator 工具
```

### 行数预估

33 个 SO × ~25 行 + 端到端工作流 × 4 = ~900 行

**→ 需拆分为**：
- `SO_WORKFLOWS_INDEX.md`（~80 行，类型总览表 + 端到端工作流 + 链接）
- `SO_WORKFLOWS_01_CORE.md`（核心配置 3 个 SO，~100 行）
- `SO_WORKFLOWS_02_ENTITY.md`（Entity 系统 6 个 SO，~200 行）
- `SO_WORKFLOWS_03_DANMAKU.md`（弹幕系统 9 个 SO，~250 行）
- `SO_WORKFLOWS_04_VFX_RENDER.md`（VFX + 渲染 3 个 SO，~100 行）
- `SO_WORKFLOWS_05_INFRA.md`（基础设施 12 个 SO，~250 行）

---

## 实施顺序与依赖

```
D1 归档 ──→ D2 拆分 ──→ D3 审计 ──→ D4 长效机制
  │                                       │
  ├── D5 编辑器工具手册（可与 D2 并行）─────┘
  └── D6 SO 配置流程（可与 D3 并行）────────┘
```

**推荐执行顺序**：D1 → D2 → D5（并行 D3）→ D6 → D4（收尾）

| Phase | 预估工时 | 产出文件数 | 关键产出 |
|-------|---------|-----------|---------|
| D1 | 15 min | 0 新建（移动 17 + 删除 1） | 根目录降至 19 文件 |
| D2 | 1.5 hr | 新建 24（INDEX×5 + 子文件×19）、删除 5 | 全部 ≤400 行 + Frontmatter |
| D3 | 1 hr | 新建 1（AUDIT_REPORT.md） | 偏差修正 |
| D4 | 45 min | 新建 1（INDEX.md 三路由表）+ 更新 MEMORY.md | Agent GPS 系统 |
| D5 | 1 hr | 新建 5（INDEX + 4 子文件） | Agent 可自主操作 |
| D6 | 1.5 hr | 新建 6（INDEX + 5 子文件） | Agent 可自主配置 SO |
| **总计** | **~6 hr** | | |

---

## 验收标准

| # | 标准 | 度量方法 |
|---|------|---------|
| 1 | Agent/ 根目录活文档 ≤20 个（不含 INDEX 子文件） | 计数 |
| 2 | 单个活文档 ≤400 行 | `wc -l` |
| 3 | 零冗余文件（拆分前后不并存） | 手动确认 |
| 4 | INDEX.md 包含三张路由表（任务/代码映射/概念速查） | 人工审查 |
| 5 | INDEX.md ≤150 行 | `wc -l` |
| 6 | 每个 Detail Doc 有 Frontmatter（system/scope/last_verified/related_code） | 批量 grep |
| 7 | 审计标记的过时内容全部修正 | AUDIT_REPORT 对照 |
| 8 | 文档维护规则（含索引同步规则）写入 MEMORY.md | 检查 MEMORY.md |
| 9 | Agent 能通过 EDITOR_TOOLS_MANUAL 自主执行所有菜单操作 | MCP 调用测试 |
| 10 | Agent 能通过 SO_CONFIG_WORKFLOWS 自主创建任意类型 SO | MCP 调用测试 |
| 11 | 所有文档遵循 Agent-First 编写原则（结构化/可执行/字段精确） | 人工审查 |
| 12 | 路由表 B 覆盖所有核心代码目录 | 代码目录 vs 路由表 B 交叉核对 |

---

## D2 后 Agent/ 根目录文件总览（预期）

| # | 文件 | 类型 | 行数 |
|---|------|------|------|
| 1 | `INDEX.md` | 总索引 | ~80 |
| 2 | `ARCHITECTURE.md` | 架构总览 | 378 |
| 3 | `DEBUG_PLAYBOOK.md` | 调试手册 | 284 |
| 4 | `NEWGAME_GUIDE.md` | 新游戏指南 | 179 |
| 5 | `WECHAT_INTEGRATION.md` | 微信集成 | 250 |
| 6 | `SO_CATALOG.md` | SO 目录 | 175 |
| 7 | `EDITOR_TOOLS.md` | 编辑器工具（D5 后被 MANUAL 取代） | 297 |
| 8 | `RUNTIME_ATLAS_ACCEPTANCE_REPORT.md` | Atlas 验收报告 | 199 |
| 9 | `DOC_CLEANUP_PLAN.md` | 本文件（执行完后归档） | ~此文件 |
| — | **INDEX 文件群** | | |
| 10 | `EC_TDD_INDEX.md` + 7 子文件 | Entity TDD | ≤400/个 |
| 11 | `ADR_INDEX.md` + 4 子文件 | ADR 决策 | ≤400/个 |
| 12 | `ATLAS_TDD_INDEX.md` + 3 子文件 | Atlas TDD | ≤400/个 |
| 13 | `CONV_INDEX.md` + 3 子文件 | 约定规范 | ≤400/个 |
| 14 | `OBB_TDD_INDEX.md` + 2 子文件 | OBB TDD | ≤400/个 |
| 15 | `PHASE3A_TDD_INDEX.md` + 5 子文件 | Phase3A TDD | ≤400/个 |
| — | **D5/D6 新增** | | |
| 16 | `EDITOR_TOOLS_MANUAL_INDEX.md` + 4 子文件 | 工具手册 | ≤400/个 |
| 17 | `SO_WORKFLOWS_INDEX.md` + 5 子文件 | SO 配置流程 | ≤400/个 |

> **注**：D3 审计后 `EDITOR_TOOLS.md`（297 行）可能被 D5 的 MANUAL 系列取代后删除，届时再决定。

---

**天命人请审阅此落地计划。确认后我逐 Phase 执行，每完成一个 Phase 提交一次 Git。**
