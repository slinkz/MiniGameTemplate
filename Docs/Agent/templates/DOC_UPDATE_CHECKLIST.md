---
system: knowledge-engineering
scope: doc-update-checklist
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE_MAINTENANCE.md, Docs/Agent/changes/README.md
---

# Doc Update Checklist

> 用途：重要代码、资产、架构或 Skill 变更完成后使用。目标是确认知识资产是否需要同步更新。

## 1. 变更摘要

- 日期：
- 任务：
- 变更类型：代码 / 资产 / 架构 / 文档 / Skill / 构建
- 影响等级：轻量 / 标准 / 严格
- 关联 Context Pack：
- 关联 Module Card：
- 关联 ADR：

## 2. 事实确认

| 项 | 结论 | 证据 |
|----|------|------|
| 当前代码是否已确认 | 是/否 | 路径/命令/说明 |
| Unity 编译是否已跑 | 是/否 | |
| PlayMode/手动流程是否已跑 | 是/否 | |
| 真机/微信是否已跑 | 是/否/不适用 | |
| 未验证风险是否已记录 | 是/否 | |

## 3. 知识资产更新检查

| 文档/资产 | 是否需要更新 | 已更新 | 原因 |
|-----------|--------------|--------|------|
| `Docs/Agent/INDEX.md` | 是/否 | 是/否 | |
| `AGENT_BOOTSTRAP.md` | 是/否 | 是/否 | |
| Context Pack | 是/否 | 是/否 | |
| Module Card | 是/否 | 是/否 | |
| `CODE_KNOWLEDGE_MAP.md` | 是/否 | 是/否 | |
| ADR 原文 | 是/否 | 是/否 | |
| `ADR_SCHEMA.md` | 是/否 | 是/否 | |
| `SO_WORKFLOWS_*` | 是/否 | 是/否 | |
| `DESIGN/*.md` | 是/否 | 是/否 | 玩法、关卡、敌人、技能、Buff、道具、经济变更 |
| `UI_DESIGN/*.md` | 是/否 | 是/否 | UI token、组件、界面、动效、文案变更 |
| `ASSET_PIPELINE/*.md` | 是/否 | 是/否 | 关键资产、Manifest、命名、导入、验收变更 |
| `DEBUG_PLAYBOOK.md` | 是/否 | 是/否 | |
| `WECHAT_INTEGRATION.md` / Guide | 是/否 | 是/否 | |
| `skills/` | 是/否 | 是/否 | |
| `.workbuddy/skills/` / `.codebuddy/skills/` | 是/否/不存在 | 是/否/不适用 | |
| `Docs/Agent/changes/` | 是/否 | 是/否 | |
| `Tools/knowledge-sync-check.ps1` | 是/否 | 是/否 | 若检查失败但确认无需文档更新，需记录绕过原因 |

## 4. 代码路径映射

新增或改变的核心路径：

```text

```

是否已写入 `CODE_KNOWLEDGE_MAP.md`：

- 是/否：
- 不需要的原因：

## 5. ADR 与架构约束

| ADR | 是否命中 | 状态是否仍准确 | 是否需要更新 |
|-----|----------|----------------|--------------|
| | 是/否 | 是/否 | 是/否 |

是否需要新增 ADR：

- 是/否：
- 原因：

## 6. changes 变更包

是否需要创建变更包：

- 是/否：
- 目录：

若需要，至少包含：

- [ ] `SUMMARY.md`
- [ ] `IMPACT.md`
- [ ] `VALIDATION.md`
- [ ] `DOC_UPDATES.md`

## 7. INDEX 统计

是否新增/删除/归档 Markdown 文件：

- 是/否：

如是，重新计算：

```powershell
(Get-ChildItem Docs\Agent -Recurse -Filter *.md | Where-Object { $_.FullName -notmatch '\\Archive\\' }).Count
(Get-ChildItem Docs\Agent\Archive -Recurse -Filter *.md).Count
```

更新结果：

- 活跃：
- 归档：

## 8. 结论

- 知识资产已同步：是/否
- `knowledge-sync-check` 已通过：是/否/不适用
- 剩余未验证项：
- 后续维护项：
- 最终回复需要说明：
