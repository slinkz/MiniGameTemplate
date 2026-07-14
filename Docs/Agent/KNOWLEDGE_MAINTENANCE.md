---
system: knowledge-engineering
scope: knowledge-maintenance
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/AGENT_BOOTSTRAP.md, Docs/Agent/ARCHITECTURE_REVIEW_PROTOCOL.md, Docs/Agent/templates/DOC_UPDATE_CHECKLIST.md, Docs/Agent/changes/README.md
---

# Knowledge Maintenance

> 定位：这是 P6 的知识维护协议。它把“代码、资产、ADR、模块卡、上下文包、索引、变更包”的同步规则固化下来，防止知识库随项目推进而漂移。

## 1. 维护原则

1. 当前代码与验证结果优先于文档记忆。
2. 活跃文档优先于 Archive；Archive 只解释历史。
3. 每次重要改动都必须判断是否更新知识资产。
4. 文档更新应贴近事实，不写无法验证的结论。
5. 新知识必须能被 `INDEX.md`、Context Pack、Module Card 或 Code Knowledge Map 找到。
6. 知识维护服务于下一次任务恢复，不记录冗长过程流水。

## 2. 什么时候必须维护

以下任一条件命中时，修改后必须使用 `templates/DOC_UPDATE_CHECKLIST.md`：

| 触发条件 | 必查文档 |
|----------|----------|
| 新增或移动核心代码路径 | `CODE_KNOWLEDGE_MAP.md`, `INDEX.md` 路由表 B |
| 改变模块职责、边界、入口或数据流 | 对应 `MODULE_CARDS/*.md` |
| 改变常见任务流程或必读文档 | 对应 `CONTEXT_PACKS/*.md`, `AGENT_BOOTSTRAP.md` |
| 改变架构决策或实现状态 | ADR 原文、`ADR_SCHEMA.md`, `ADR_INDEX.md` |
| 改变 SO 类型、字段、创建流程或校验方式 | `SO_WORKFLOWS_*`, 相关 Skill |
| 改变 FairyGUI 导出、Binder、UI 生命周期 | `CONTEXT_PACKS/FairyGUI_UI.md`, `MODULE_CARDS/UISystem_FairyGUI.md` |
| 改变渲染、RuntimeAtlas、VFX、飘字排查方式 | `DEBUG_PLAYBOOK.md`, Rendering/Danmaku 模块卡 |
| 改变微信、云存储、CDN、广告、构建流程 | `WECHAT_INTEGRATION.md`, `Docs/Guide/BUILD_MINIGAME.md` |
| 引入新坑、修复高价值 bug、完成架构迁移 | `Docs/Agent/changes/YYYY-MM-DD-topic/` |
| 修改 `skills/` | 检查运行时 Skill 目录同步策略（`.workbuddy/skills/` 或 `.codebuddy/skills/`） |

## 3. 变更后维护流程

1. 运行或记录本次可执行验证。
2. 使用 `templates/DOC_UPDATE_CHECKLIST.md` 判断受影响知识资产。
3. 更新直接受影响文档。
4. 若是重要变更，创建 `Docs/Agent/changes/YYYY-MM-DD-topic/` 变更包。
5. 更新 `INDEX.md` 路由表 A/B/C 中受影响行。
6. 如果新增核心路径，更新 `CODE_KNOWLEDGE_MAP.md`。
7. 如果改变架构约束，更新 ADR 原文与 `ADR_SCHEMA.md`。
8. 如果修改 Skill，确认 `skills/` 与实际运行时 Skill 目录的同步状态。
9. 在最终回复中说明已更新哪些知识资产、哪些验证未运行。
10. 对代码/资产变更运行 `Tools/knowledge-sync-check.ps1`，确认没有漏掉知识资产同步。

## 4. 文档类型维护规则

| 文档类型 | 维护重点 | 更新时机 |
|----------|----------|----------|
| `INDEX.md` | 任务路由、代码映射、概念速查、文件统计 | 新增入口、新增核心文档、改变高频任务路径 |
| `AGENT_BOOTSTRAP.md` | 新会话启动流程、事实源优先级、禁止事项、验证入口 | 改变 Agent 默认工作流 |
| Context Pack | 高频任务的最小上下文、必读文档、代码入口、必验项 | 任务流程变化或新增高频任务 |
| Module Card | 模块职责、不负责、入口、数据流、ADR、常见坑 | 模块边界或核心入口变化 |
| `CODE_KNOWLEDGE_MAP.md` | 代码路径到文档/ADR/验证项映射 | 新增核心路径或验证项变化 |
| ADR | 长期架构约束、实现状态、替代关系、验证要求 | 决策变化或代码事实确认 |
| SO Workflow | 资产类型、字段、路径、创建与校验流程 | SO 类型/字段/Validator 变化 |
| Debug Playbook | 可复用排查路径、已知坑、验证命令 | 修复高价值 bug 或新增排查手法 |
| Skill | 操作 SOP、工具脚本、专家经验 | Agent 需要自动触发或复用该流程 |
| changes 包 | 重要变更的摘要、影响、验证、文档更新 | 架构迁移、高风险 bugfix、跨模块改动 |

## 5. changes 变更包规则

新变更包使用目录：

```text
Docs/Agent/changes/YYYY-MM-DD-topic/
├── SUMMARY.md
├── IMPACT.md
├── VALIDATION.md
└── DOC_UPDATES.md
```

最低要求：

- `SUMMARY.md`：动机、变更摘要、关键决策、关联 ADR/TDD。
- `IMPACT.md`：代码、资产、模块、平台、兼容性影响。
- `VALIDATION.md`：已跑验证、未跑验证、剩余风险。
- `DOC_UPDATES.md`：已更新文档、无需更新原因、后续维护项。

已有旧变更包不强制迁移；后续新包按本结构创建。

## 6. Skill 双路径同步

当前仓库约定：

- `skills/` 是纳入版本管理的 Skill 源目录。
- 运行时 Skill 目录取决于 Agent 工具：WorkBuddy 历史工作区使用 `.workbuddy/skills/`，部分工具或旧文档可能使用 `.codebuddy/skills/`。
- 运行时目录可能在某些工作区不存在；不存在时不视为错误，但需要在交付中说明。

修改 Skill 后必须检查：

1. 是否修改了 `skills/<name>/SKILL.md` 或 references/scripts。
2. 若 `.workbuddy/skills/` 或 `.codebuddy/skills/` 存在，是否同步同名 Skill。
3. 若运行时 Skill 目录不存在，是否在最终回复或变更包中记录“未同步运行时目录”。
4. 是否需要更新 `KNOWLEDGE_INVENTORY.md` 的 Skill 盘点。
5. 是否需要更新 `CODE_KNOWLEDGE_MAP.md` 的 Skill 映射。

## 7. INDEX 文件统计规则

`Docs/Agent/INDEX.md` 文件头中的数量按 Markdown 文件统计：

```powershell
(Get-ChildItem Docs\Agent -Recurse -Filter *.md | Where-Object { $_.FullName -notmatch '\\Archive\\' }).Count
(Get-ChildItem Docs\Agent\Archive -Recurse -Filter *.md).Count
```

新增、删除、归档文档后应更新统计。若只是临时草稿且未纳入知识体系，可不计入，但不建议长期保留未索引草稿。

## 8. 轻量与严格模式

| 模式 | 使用场景 | 要求 |
|------|----------|------|
| 轻量维护 | 小 bugfix、单文档修正、无核心路径变化 | 口头检查清单，必要时更新 1-2 个文档 |
| 标准维护 | 普通功能、SO/UI/模块入口变化 | 填 `DOC_UPDATE_CHECKLIST.md`，更新受影响文档 |
| 严格维护 | 架构迁移、ADR 变化、跨模块改动、高风险 bugfix | 填清单，创建 changes 包，更新索引/映射/ADR |

## 9. 自动化检查

仓库提供轻量知识同步检查：

```powershell
Tools/knowledge-sync-check.ps1
```

它会检查以下敏感路径是否发生变化：

- `UnityProj/Assets/_Framework/`
- `UnityProj/Assets/_Game/Scripts/`
- `UnityProj/Assets/_Game/Configs/`
- `UnityProj/Assets/_Game/FairyGUI_Export/`
- `UnityProj/Assets/_Example/`
- `UnityProj/DataTables/`
- `UIProject/`
- `CloudFunctions/`
- `skills/`
- `.workbuddy/skills/`
- `.codebuddy/skills/`

若这些路径有变更，但没有同时修改 `Docs/Agent/**`、操作型 Guide、根 `README.md` 或 `CHANGELOG.md`，检查会失败，并要求回到 `templates/DOC_UPDATE_CHECKLIST.md` 判断是否需要同步知识资产。

本地 pre-commit hook 已放在 `.githooks/pre-commit`。每个 clone 可执行一次：

```powershell
git config core.hooksPath .githooks
```

CI 入口为 `.github/workflows/knowledge-sync.yml`，在 push / pull_request 中运行同一脚本。

若确认某次敏感变更确实无需文档更新，可以本地使用：

```powershell
Tools/knowledge-sync-check.ps1 -AllowNoDocUpdate
```

或设置 `KNOWLEDGE_SYNC_ALLOW_NO_DOC=1`。这种绕过只能在已完成 checklist 判断并记录原因后使用。

## 10. 完成标准

一次重要变更完成时，应能回答：

```text
1. 代码事实是否已通过验证或明确标注未验证？
2. 任务入口是否能从 INDEX 找到？
3. 代码路径是否能从 CODE_KNOWLEDGE_MAP 反查文档和验证？
4. 模块职责是否仍与 Module Card 一致？
5. ADR 状态和约束是否仍准确？
6. Context Pack 是否仍能指导下一次同类任务？
7. 是否需要 changes 包记录本次变更？
8. skills 与运行时 Skill 目录是否需要同步？
9. knowledge-sync 检查是否通过，或是否记录了无需文档更新的原因？
```

如果任何答案不清楚，先补知识资产，再结束任务。
