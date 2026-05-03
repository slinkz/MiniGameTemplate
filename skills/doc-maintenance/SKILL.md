---
name: doc-maintenance
description: "文档维护技能。确保 Docs/Agent/ 目录下的文档遵循统一规范：行数纪律、命名约定、Frontmatter 标准、INDEX 路由表同步、归档流程。当 Agent 需要新建文档、修改文档、拆分文档、归档文档、审计文档、或更新 INDEX.md 时触发此技能。触发词包括：'新建文档'、'更新文档'、'拆分文档'、'归档文档'、'文档审计'、'更新索引'、'文档维护'。"
---

# 文档维护规范

## 概述

本技能定义了 `MiniGameTemplate/Docs/Agent/` 目录的文档维护规范。所有文档操作（增/删/改/拆/归档）必须遵循本规范，确保文档体系对 Agent 始终高效可检索。

## 触发条件

以下场景触发本技能：
- 新建任何 `.md` 文件到 `Docs/Agent/`
- 修改已有文档的结构或内容
- 代码变更后需要同步文档
- 拆分超长文档
- 归档过程文档
- Phase 完成后的文档审计
- 更新 INDEX.md 路由表

## 核心规则（铁律）

### 规则 1：行数纪律

| 文件类型 | 行数上限 | 超限动作 |
|---------|---------|---------|
| INDEX 文件（总索引/Domain INDEX） | 150 行 | 精简条目或拆分 |
| Detail Doc（子文件） | 400 行 | 拆分为 INDEX + 子文件 |
| 总索引 `INDEX.md` | 150 行 | 仅保留路由表，不放细节 |

### 规则 2：命名约定

```
<SYSTEM>_INDEX.md              # 系统级索引
<SYSTEM>_NN_<TOPIC>.md         # 子文件（NN = 两位序号）

示例：
EC_TDD_INDEX.md
EC_TDD_01_OVERVIEW.md
EC_TDD_02_ENTITY_POOL.md
ADR_INDEX.md
ADR_01_FOUNDATION.md
```

**命名原则**：
- 全大写 + 下划线分隔
- 系统前缀保持一致（EC_TDD / ADR / CONV / ATLAS_TDD / OBB_TDD / PHASE3A_TDD）
- 子文件序号从 01 开始，两位数字

### 规则 3：Frontmatter 标准

每个 Detail Doc（非 INDEX）头部必须包含 Frontmatter：

```markdown
---
system: entity-component
scope: components-combat
last_verified: 2026-05-02
depends_on: [EC_TDD_01, EC_TDD_02]
related_code: Assets/_Framework/EntitySystem/Components/Attack*, Skill*, Buff*
---
```

| 字段 | 必填 | 说明 |
|------|------|------|
| `system` | ✅ | 所属系统标识（小写-连字符） |
| `scope` | ✅ | 文档覆盖范围（1~3 个词） |
| `last_verified` | ✅ | 上次确认与代码一致的日期（YYYY-MM-DD） |
| `depends_on` | 可选 | 前置阅读文件（不含 .md 后缀） |
| `related_code` | ✅ | 关联代码路径（支持 glob 通配符） |

### 规则 4：INDEX.md 三路由表

总索引 `Docs/Agent/INDEX.md` 包含三张路由表，必须保持同步：

| 路由表 | 解决的问题 | 触发更新条件 |
|--------|-----------|-------------|
| **A. 任务路由** | "我要做 X → 读哪个文件" | 新增功能/文档时 |
| **B. 代码→文档映射** | "改了 X.cs → 哪个文档可能过时" | 新增代码模块时 |
| **C. 概念速查** | "X 是什么？在哪定义的？" | 新增概念/术语时 |

### 规则 5：归档流程

过程文档（PK 记录、验收报告、旧设计草案、Question 讨论）完成后立即归档：

```
Archive/
├── EntityComponent/   # EC 框架 PK 评审记录
├── Phase3/            # Phase 3 旧设计草案
├── Phase3A/           # Phase 3A 过程文档
├── OBB/               # OBB 障碍物评审
├── VFX/               # VFX 调研
└── General/           # 其他通用归档
```

**归档判定标准**：
- PK 评审记录 → 评审结束后归档
- 验收报告 → 验收通过后归档（除非是正在使用的活报告）
- Question 讨论 → 结论已回写到 TDD 后归档
- 旧设计文档 → 新版 TDD 完成后归档

### 规则 6：拆分流程

当活文档超过 400 行时：

1. 创建 `<SYSTEM>_INDEX.md`（≤100 行）
   - 版本号 + 最后更新日期
   - 子文件列表（文件名 + 一句话摘要）
   - 相关代码路径
2. 按逻辑主题拆分为子文件，每个 ≤400 行
3. 每个子文件添加 Frontmatter
4. 删除原始单体文件
5. 更新 `INDEX.md` 总索引三路由表

### 规则 7：代码→文档联动

修改代码后，检查 INDEX.md 路由表 B：
1. 找到代码路径对应的文档
2. 检查文档内容是否仍然准确
3. 如有偏差：修正文档 + 更新 `last_verified` 日期
4. 如无偏差：仅更新 `last_verified` 日期

## 操作清单（Checklist）

### 新建文档

- [ ] 文件名遵循命名约定（规则 2）
- [ ] 行数 ≤400（规则 1）
- [ ] 添加 Frontmatter（规则 3）
- [ ] 更新 INDEX.md 路由表 A/B/C 相关行（规则 4）
- [ ] 如属于已有系统，更新该系统 Domain INDEX

### 修改文档

- [ ] 修改后行数仍 ≤400（规则 1）
- [ ] 更新 Frontmatter 的 `last_verified`（规则 3）
- [ ] 如涉及新概念/术语，更新路由表 C（规则 4）

### 拆分文档

- [ ] 按规则 6 完整流程执行
- [ ] 所有子文件 ≤400 行
- [ ] 所有子文件有 Frontmatter
- [ ] 原单体文件已删除
- [ ] INDEX.md 三路由表已同步

### 归档文档

- [ ] 确认符合归档判定标准（规则 5）
- [ ] 使用 `git mv` 移动到正确的 Archive 子目录
- [ ] 从 INDEX.md 移除对应条目（归档文件不出现在活索引中）

### 代码变更后

- [ ] 查看 INDEX.md 路由表 B，定位相关文档
- [ ] 验证文档内容与代码一致
- [ ] 更新 `last_verified` 或修正内容
- [ ] 如新增模块/概念，更新路由表 B/C

### Phase 完成后审计

- [ ] 列出本 Phase 涉及的所有 Detail Doc
- [ ] 逐个对比代码实现 vs 文档描述
- [ ] 标记状态：✅ 一致 / ⚠️ 偏差（修正）/ ❌ 严重过时（重写）
- [ ] 更新所有相关文件的 `last_verified`
- [ ] 产出审计报告（如有偏差）

## 文档编写原则（Agent-First）

1. **结构化优先**：用表格、枚举列表、代码块表达，避免大段散文
2. **可执行性**：操作步骤包含完整路径/API/命令，Agent 能直接按步骤操作
3. **字段级精确**：配置文档精确到每个字段名（C# 属性名）、类型、默认值、合法范围
4. **上下文自包含**：每个文档独立可读，不假设 Agent 已读过其他文件
5. **行数纪律**：严格遵守上限，宁可多一个子文件也不超限

## 快速参考

### 系统前缀对照表

| 系统 | 前缀 | 目录 |
|------|------|------|
| Entity-Component 框架 | `EC_TDD` | `EntitySystem/` |
| 弹幕系统 | `DANMAKU` | `Danmaku/` |
| Runtime Atlas | `ATLAS_TDD` | `RuntimeAtlas/` |
| OBB 碰撞 | `OBB_TDD` | `OBB/` |
| Phase 3A 技能/Buff | `PHASE3A_TDD` | `EntitySystem/Components/Skill*, Buff*` |
| ADR 决策记录 | `ADR` | 全局 |
| 编码约定 | `CONV` | 全局 |
| 编辑器工具 | `EDITOR_TOOLS_MANUAL` | `Editor/` |
| SO 配置流程 | `SO_WORKFLOWS` | `Configs/`, `*SO.cs` |

### 有效的 system 值

```
entity-component, danmaku, runtime-atlas, obb-collision,
phase3a-skill-buff, architecture, conventions, editor-tools,
so-config, wechat, general
```
