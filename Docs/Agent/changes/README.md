---
system: knowledge-engineering
scope: changes-index
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE/KNOWLEDGE_MAINTENANCE.md, Docs/Agent/templates/DOC_UPDATE_CHECKLIST.md
---

# changes 变更包规范

> 定位：`Docs/Agent/changes/` 用于记录重要变更的可追溯结果，帮助后续 Agent 快速理解“改了什么、影响哪里、怎么验证、文档是否同步”。

## 1. 什么时候创建变更包

必须创建：

- 跨模块或架构敏感改动。
- ADR 新增、替代、实现状态确认。
- 高风险 bugfix，尤其是渲染、生命周期、对象池、平台兼容问题。
- 大范围迁移、命名替换、数据结构变更。
- 改变构建、微信、云存储、CDN、FairyGUI 导出流程。

可不创建：

- 单行文案修正。
- 局部注释或格式修正。
- 单个低风险配置值调整，且已有清晰提交信息。

## 2. 新变更包结构

```text
Docs/Agent/changes/YYYY-MM-DD-topic/
├── SUMMARY.md
├── IMPACT.md
├── VALIDATION.md
└── DOC_UPDATES.md
```

命名规则：

- 日期使用实际变更日期。
- topic 使用小写英文、数字、短横线。
- 一个变更包只描述一个可理解的变更主题。

## 3. 文件职责

| 文件 | 内容 |
|------|------|
| `SUMMARY.md` | 动机、变更摘要、关键决策、关联 ADR/TDD/Issue |
| `IMPACT.md` | 代码路径、模块、SO/Scene/UI/平台、兼容性影响 |
| `VALIDATION.md` | 已跑验证、未跑验证、失败/修复记录、剩余风险 |
| `DOC_UPDATES.md` | 已更新知识资产、无需更新原因、后续维护项 |

## 4. 旧变更包处理

当前目录中已有若干历史变更包，结构不完全一致：

- `2026-04-21-laser-atlas-bugfix`
- `2026-04-21-phase4-deep-integration`
- `2026-04-23-obb-obstacle`
- `2026-04-29-fairygui-click-fix`
- `2026-04-30-p1.0-enumcamp-migration`
- `P2.4_P2.5_ACCEPTANCE_CHECKLIST.md`

这些文件保留原状作为历史记录；后续新变更包按本规范创建。

## 5. 最小模板

### SUMMARY.md

```markdown
# <变更主题>

## 动机

## 变更摘要

## 关键决策

## 关联
```

### IMPACT.md

```markdown
# Impact

## 代码路径

## 模块影响

## 资产 / SO / UI / Scene

## 平台与兼容性
```

### VALIDATION.md

```markdown
# Validation

## 已执行

## 未执行

## 剩余风险
```

### DOC_UPDATES.md

```markdown
# Doc Updates

## 已更新

## 无需更新

## 后续维护项
```
