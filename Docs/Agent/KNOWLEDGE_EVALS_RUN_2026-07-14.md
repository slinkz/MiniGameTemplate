---
system: knowledge-engineering
scope: knowledge-evals-run
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE_EVALS.md, Docs/Agent/INDEX.md, Docs/Agent/CODE_KNOWLEDGE_MAP.md
---

# Knowledge Evals Run - 2026-07-14

> 定位：首批 Knowledge Evals 运行记录。评估目标是检查 P0-P7 知识工程是否能支撑 Agent 在 10 个标准任务中完成正确路由、影响面判断、ADR 检查和验证闭环。

## 1. 运行方式

- 评估日期：2026-07-14
- 评估对象：P0-P7 初版知识工程文档
- 评估依据：`KNOWLEDGE_EVALS.md`
- 运行方式：静态知识路由评估；未执行 Unity 编译、PlayMode、微信真机或实际代码修改

评分采用 10 分制：

```text
路由准确率 2 + 上下文效率 1 + 设计一致性 2 + 影响面完整度 2 + 踩坑规避 1 + 验证闭环 2
```

## 2. 评分结果

| 任务 | 分数 | 结论 | 主要漏项 |
|------|------|------|----------|
| EVAL-01 新增一种敌人 | 9.0 | 通过 | `INDEX.md` 入口偏向 SO Workflow，未直指 Context Pack / Module Card |
| EVAL-02 新增一个技能 | 9.0 | 通过 | 可进一步补充技能变更何时需要 changes 包 |
| EVAL-03 修改碰撞逻辑 | 8.5 | 通过 | ADR-012 未进入 `ADR_SCHEMA.md` 可执行摘要 |
| EVAL-04 修改 FairyGUI 面板 | 8.5 | 通过 | `INDEX.md` 缺少直接“修改 FairyGUI 面板”任务入口 |
| EVAL-05 修改微信云存储 | 8.0 | 通过 | `CODE_KNOWLEDGE_MAP.md` 标注 WeChat/DataSystem 模块卡待补 |
| EVAL-06 调整 RuntimeAtlas | 8.5 | 通过 | WebGL/真机验证只能静态列出，仍需实测闭环 |
| EVAL-07 新增关卡 | 7.5 | 待修正 | `INDEX.md` 与 `CODE_KNOWLEDGE_MAP.md` 缺少直接“新增关卡”反查入口 |
| EVAL-08 新增 Buff/DOT | 8.5 | 通过 | DOT 专项入口不如 Buff 明确 |
| EVAL-09 调试渲染不显示 | 8.5 | 通过 | 路由名偏“调试渲染/性能”，可补“渲染不显示”直达 |
| EVAL-10 架构重构评审 | 9.0 | 通过 | 后续需通过真实重构任务检验模板负担 |

## 3. 批次结论

| 指标 | 结果 |
|------|------|
| 平均分 | 8.5 |
| 低于 8 分任务 | 1 个：EVAL-07 |
| 低于 7 分任务 | 0 个 |
| 致命漏项 | 无 |
| 批次是否通过 | 通过 |

本批次说明：P0-P7 初版知识体系已经能覆盖主要任务路由、模块边界、ADR 与验证闭环。最明显短板不是大面积缺文档，而是少数高频任务缺少直达入口，以及少量“待补模块卡 / 待可执行 ADR”仍会让 Agent 多绕一步。

## 4. 发现的问题

### 4.1 新增关卡路由不够直达

现状：

- `CONTEXT_PACKS/ShooterGame_Battle.md` 和 `CONTEXT_PACKS/SO_Config_Workflow.md` 能覆盖新增关卡。
- `CODE_KNOWLEDGE_MAP.md` 有 `SG_ProgressManager*` 和 ShooterGame 配置映射。
- 但 `INDEX.md` 与 `CODE_KNOWLEDGE_MAP.md` 常见任务反查没有直接“新增关卡”入口。

建议：

- 在 `INDEX.md` 增加“新增关卡”任务路由。
- 在 `CODE_KNOWLEDGE_MAP.md` 第 9 节增加“新增关卡”反查行。

### 4.2 WeChat/DataSystem 模块卡待补

现状：

- `CONTEXT_PACKS/WeChat_Build_Cloud.md` 覆盖微信构建、云存储、真机验证。
- `CODE_KNOWLEDGE_MAP.md` 中 `_Framework/WeChatBridge/**`、`_Framework/DataSystem/**/Cloud*.cs` 标注“待补 `WeChatBridge.md` / `DataSystem_SO_Luban.md`”。

建议：

- 后续新增 `MODULE_CARDS/WeChatBridge.md`。
- 后续新增 `MODULE_CARDS/DataSystem_SO_Luban.md`。

### 4.3 ADR-012 未可执行化

现状：

- 碰撞评估会触碰 OBB / Hitbox 数学。
- `CODE_KNOWLEDGE_MAP.md` 已把 `HitboxMath.cs` 关联到 ADR-012。
- `ADR_SCHEMA.md` 当前优先摘要未包含 ADR-012。

建议：

- 后续将 ADR-012 纳入 `ADR_SCHEMA.md` 的可执行摘要，至少补 AppliesTo、Constraints、Verification。

### 4.4 部分任务缺少直达措辞

建议补充的直达入口：

- `INDEX.md`：修改 FairyGUI 面板。
- `INDEX.md`：调试渲染不显示。
- `INDEX.md`：新增关卡。
- `CODE_KNOWLEDGE_MAP.md`：新增关卡。
- `CODE_KNOWLEDGE_MAP.md`：新增 DOT 可单独列出，或在 Buff/DOT 行中强调。

## 5. 反向修正优先级

| 优先级 | 修正项 | 目标文档 |
|--------|--------|----------|
| P1 | 补“新增关卡”直达路由与反查 | `INDEX.md`, `CODE_KNOWLEDGE_MAP.md` |
| P1 | 补“修改 FairyGUI 面板”“调试渲染不显示”直达路由 | `INDEX.md` |
| P2 | 可执行化 ADR-012 | `ADR_SCHEMA.md` |
| P2 | 补 WeChatBridge / DataSystem_SO_Luban 模块卡 | `MODULE_CARDS/` |
| P3 | 运行一次真实任务评估，检查模板是否过重 | `KNOWLEDGE_EVALS.md`, `ARCHITECTURE_REVIEW_PROTOCOL.md` |

## 6. 本次未执行

- 未运行 Unity 编译。
- 未运行 PlayMode。
- 未运行微信开发者工具或真机。
- 未进行真实代码改动。
- 未创建 changes 变更包，因为本次是评估记录，不是代码/架构迁移。

## 7. 后续建议

下一步建议先做轻量修正：

1. 更新 `INDEX.md` 的三个直达任务入口。
2. 更新 `CODE_KNOWLEDGE_MAP.md` 的“新增关卡”反查。
3. 将本次评估报告作为后续持续校准的基线。

## 8. 修正记录

2026-07-14 已完成本报告提出的 P1/P2 修正：

| 修正项 | 状态 | 文档 |
|--------|------|------|
| 补“新增关卡”直达路由与反查 | 已完成 | `INDEX.md`, `CODE_KNOWLEDGE_MAP.md` |
| 补“修改 FairyGUI 面板”直达路由 | 已完成 | `INDEX.md` |
| 补“调试渲染不显示”直达路由 | 已完成 | `INDEX.md` |
| 可执行化 ADR-012 | 已完成 | `ADR_SCHEMA.md` |
| 补 WeChatBridge 模块卡 | 已完成 | `MODULE_CARDS/WeChatBridge.md` |
| 补 DataSystem_SO_Luban 模块卡 | 已完成 | `MODULE_CARDS/DataSystem_SO_Luban.md` |

后续建议重新运行 EVAL-03、EVAL-05、EVAL-07、EVAL-09，确认分数是否提升并检查新模块卡是否足够可用。

## 9. 针对性复测记录

### 9.1 复测范围

- 日期：2026-07-14
- 范围：EVAL-03、EVAL-05、EVAL-07、EVAL-09
- 方式：静态知识路由复测；未执行 Unity 编译、PlayMode、微信开发者工具或真机
- 前置修正：P1/P2 修正已完成，包括直达路由、ADR-012 可执行化、WeChatBridge/DataSystem 模块卡

### 9.2 复测结果

| 任务 | 原分数 | 复测分数 | 结论 | 改善点 |
|------|--------|----------|------|--------|
| EVAL-03 修改碰撞逻辑 | 8.5 | 9.0 | 通过 | `ADR_SCHEMA.md` 已补 ADR-012，可直接检查阵营模型约束与验证项 |
| EVAL-05 修改微信云存储 | 8.0 | 9.0 | 通过 | `WeChatBridge.md` 与 `DataSystem_SO_Luban.md` 已补齐，Code Knowledge Map 不再停在“待补模块卡” |
| EVAL-07 新增关卡 | 7.5 | 9.0 | 通过 | `INDEX.md` 与 `CODE_KNOWLEDGE_MAP.md` 已补新增关卡直达入口和反查 |
| EVAL-09 调试渲染不显示 | 8.5 | 9.0 | 通过 | `INDEX.md` 已补“调试渲染不显示”直达路由，减少从“性能”入口绕行 |

### 9.3 复测结论

本次针对性复测通过。P1/P2 修正有效，首批评估中低于 8 分的 `EVAL-07` 已提升到 9.0；WeChat/DataSystem 与 ADR-012 两个结构性缺口已补齐。

剩余风险：

- 本次仍是静态知识路由复测，没有验证 Unity/微信真机链路。
- `WeChatBridge.md` 与 `DataSystem_SO_Luban.md` 还需要在后续真实云存储或配置任务中检验是否足够细。
- ADR-012 的实现状态仍建议在后续碰撞代码改动时结合当前代码和测试再确认。
