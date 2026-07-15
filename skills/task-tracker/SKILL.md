---
name: task-tracker
description: >
  传统项目级任务追踪系统。通过工作空间下的 .tasks/ 目录管理多阶段任务的全生命周期（创建、执行、挂起、恢复、归档）。
  在 MiniGameTemplate 当前知识工程中，Docs/Agent/** 是当前事实源，.tasks/** 只作为本地历史工作台或用户明确要求时使用。
  触发关键词：任务、计划、阶段、看板、进度、恢复、挂起、BOARD。
---

# Task Tracker — 长任务执行态工作台

## 目的

解决 Agent 跨会话记忆丢失问题。将任务状态、计划文档、决策记录持久化到磁盘文件系统，
不依赖 AI 上下文窗口或 working memory 的压缩机制。

> MiniGameTemplate 当前约定：`Docs/Agent/**`、代码与验证结果是当前事实源；`.tasks/**` 是早期 WorkBuddy 本地任务系统，可能含过期状态。新会话恢复项目主线时优先读 `Docs/Agent/AGENT_BOOTSTRAP.md`、`Docs/Agent/INDEX.md`、`Docs/Agent/KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP.md`。只有用户明确要求维护 `.tasks`，或当前任务确实需要跨会话临时执行态时，才按本 Skill 操作 `.tasks`。

## 何时使用

使用 `.tasks`：

- 任务预计跨多轮会话。
- 任务有多个阶段、多个恢复点或大量临时探索结论。
- 用户明确要求维护 `.tasks`、BOARD、计划或任务文件。
- 需要追溯早期 WorkBuddy 任务历史。

不要使用 `.tasks`：

- 单轮可完成的小改动。
- 只需要更新长期事实源的知识工程任务。
- 用户只是问“接下来做什么”，此时优先读 `Docs/Agent/INDEX.md` 和路线图。
- `.tasks` 内容与 `Docs/Agent/**` 冲突时，不要用 `.tasks` 覆盖当前事实。

## 目录结构

```
.tasks/                           ← 本地工作台（不纳入 Git 当前事实源）
├── README.md                     ← 使用边界和收敛规则
├── BOARD.md                      ← 任务看板（唯一入口点）
├── active/                       ← 活跃任务的详情文档
│   └── {task-id}.md              ← 每个任务一个文件
├── plans/                        ← 方案和计划的暂存目录
│   └── {plan-name}.md            ← Agent 生成的方案/计划文档
└── archive/                      ← 已完成任务的归档
    └── {task-id}.md              ← 用户确认完成后才可移入
```

## BOARD.md 格式规范

```markdown
# 任务看板

## 进行中
- [ ] `{task-id}` — {一句话描述} → [详情](active/{task-id}.md)

## 待启动
- [ ] `{task-id}` — {一句话描述} → [详情](active/{task-id}.md)

## 已完成
- [x] `{task-id}` — {一句话描述}（{完成日期}, commit `{hash}`）
```

## 任务详情文档模板

参见 [assets/task-template.md](assets/task-template.md)。创建新任务时复制此模板并填写。

## 核心工作流

### 1. 新会话启动协议

仅当用户明确要求使用 `.tasks` 恢复/维护任务时：

1. 读取 `.tasks/BOARD.md`
2. 向用户汇报当前看板状态（进行中 / 待启动的任务摘要）
3. 询问用户接下来要继续哪个任务
4. 根据用户指令，读取对应的 `active/{task-id}.md` 获取完整上下文

### 2. 接收多阶段长任务时

当用户给出或 Agent 生成一个明确会跨会话的多阶段计划时：

1. 先确认这个计划不应直接写入 `Docs/Agent/KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP.md` 或 `changes/**`。
2. 将临时计划文档保存到 `.tasks/plans/{plan-name}.md`。
3. 为每个阶段在 `active/` 下创建独立的任务详情文档。
4. 更新 `BOARD.md` 注册所有任务。
5. 任务详情中写明“知识收敛计划”：完成后哪些内容迁入 `Docs/Agent/**`。

### 3. 任务执行中

- 完成子任务时，更新 `active/{task-id}.md` 中的 checkbox
- 产生关键决策时，追加到任务详情的"决策记录"区块
- Agent 生成的方案/分析文档保存到 `.tasks/plans/`，在任务详情中引用

### 4. 任务被打断（挂起协议）

当识别到主线任务被中断（用户要求转去做别的事）时，**切走之前必须**：

1. 在当前任务详情文档底部追加挂起记录：
   ```markdown
   ## ⏸ 挂起记录
   - {日期}：挂起原因：{原因}。当前进度：{已完成的子任务}。恢复后从 {具体步骤} 继续。
   ```
2. `BOARD.md` 中该任务保持在"进行中"不变

### 5. 任务恢复

恢复挂起的任务时：

1. 读取 `active/{task-id}.md`
2. 查看挂起记录中的恢复点
3. 向用户确认从该点继续
4. 清除挂起记录，继续执行

### 6. 任务完成（归档协议）

**只有用户明确确认任务完成后**才可执行归档：

1. 先执行任务详情中的“知识收敛计划”。
2. 将有长期价值的结论迁入 `Docs/Agent/**`、`changes/**`、ADR、Module Card、Context Pack 或验收文档。
3. 将 `active/{task-id}.md` 移动到 `archive/{task-id}.md`。
4. 更新 `BOARD.md`：从"进行中/待启动"移到"已完成"，标注完成日期和 commit。
5. 关联的 `plans/` 文档保留不删除（历史参考价值）。

## 与 Docs/Agent 的关系

| 类型 | 放在哪里 |
|------|----------|
| 临时执行进度、恢复点、未定方案 | `.tasks/**` |
| 当前架构事实、模块边界、任务路由 | `Docs/Agent/**` |
| 重要变更过程和验证记录 | `Docs/Agent/changes/**` |
| 长期项目路线 | `Docs/Agent/KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP.md` |
| 团队协作任务管理 | GitHub Issues / Projects 优先 |

## 禁止事项

- ❌ 不可自行删除任何任务文件或计划文件
- ❌ 不可在用户未确认的情况下将任务标记为已完成
- ❌ 不可将方案/计划仅存在 artifact 或 AI 内部上下文中——必须落盘到 `.tasks/plans/`
- ❌ 在用户明确要求使用 `.tasks` 时，不可跳过 `.tasks` 启动协议（读 BOARD → 汇报 → 等指令）
- ❌ 不可把 `.tasks` 中的过期记录当作当前实现事实
- ❌ 不可让任务完成后只停留在 `.tasks`，必须把长期价值沉淀到版本化知识工程
