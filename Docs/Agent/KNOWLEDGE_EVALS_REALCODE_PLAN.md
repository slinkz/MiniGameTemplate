---
system: knowledge-engineering
scope: knowledge-evals-realcode
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE_EVALS.md, Docs/Agent/KNOWLEDGE_EVALS_RUN_2026-07-14.md
requires: Unity Editor 运行中 + MCP Bridge
---

# Knowledge Evals — Real Code Run

> 定位：P1-4 真实编码评估计划。验证知识工程在端到端编码任务中是否真正帮助 Agent 正确路由→设计→编码→验证。

## 1. 前置条件

- [ ] Unity Editor 打开 MiniGameTemplate 项目
- [ ] Unity MCP Bridge 已加载（包：com.anklebreaker.unity-mcp）
- [ ] 可选：CodeGraph 索引最新
- [ ] 可选：微信开发者工具（仅 EVAL-3 需要）

## 2. 评估任务

### EVAL-R1：新增一个简单敌人（预计 20min）

**任务描述**：创建一个新的敌方 Entity——"KamikazeEnemy"（自爆敌机），它比普通敌机移动快 50%，在接近玩家底线时自爆造成范围伤害。

**评估维度**：

| 维度 | 分数 | 期望行为 |
|------|------|----------|
| 路由 | 2 | Agent 先读 `INDEX.md` → `CONTEXT_PACKS/SO_Config_Workflow.md` → `CONTEXT_PACKS/EntitySystem.md`，而非 grep 全目录 |
| 上下文效率 | 1 | 实际读取文件 ≤ 6 个（Context Pack + Module Card + SO Workflow + 相关 TDD） |
| 设计一致性 | 2 | 使用 EntityConfigSO + SO 模板创建，不硬编码；遵守 ADR-033（Entity 不绑定 GameObject、ComponentType/TickOrder 契约） |
| 影响面 | 2 | 识别影响：新增 SO 资产、Wave 配置更新、碰撞组（自爆判定）、关卡验证 |
| 踩坑规避 | 1 | 不修改自动生成代码、不绕过 SO 工作流、不在热路径分配 GC、退场清理注册 |
| 验证闭环 | 2 | 编译通过（Unity MCP）、SO Validator 通过、Play mode：敌人出现→移动→接近底线→自爆→正确扣血/回收 |

**预期产出**：
- 新增 `KamikazeEnemySO.asset`（基于 Template_EnemyConfigSO）
- 可能需要新增一个简单的 `KamikazeComponent`（或复用现有组件组合）
- 在 Wave 配置中添加自爆敌机
- 验证结果日志

### EVAL-R2：修改一个 Buff 参数（预计 10min）

**任务描述**：将"攻速提升 Buff"的效果从 +30% 改为 +50%，并验证修改在战斗中生效。

**评估维度**：

| 维度 | 分数 | 期望行为 |
|------|------|----------|
| 路由 | 2 | Agent 先读 `CONTEXT_PACKS/EntitySystem.md` → `CONTEXT_PACKS/SO_Config_Workflow.md` → `SG_V2_TDD_03_BUFF_DOT_PASSIVE.md` |
| 上下文效率 | 1 | 读取文件 ≤ 5 个 |
| 设计一致性 | 2 | 通过 SO 资产修改而非硬编码；遵守 Buff 叠加规则（ADR-033）；确认不触碰 Buff 槽位上限 MAX_BUFFS=16 |
| 影响面 | 2 | 识别：BuffConfigSO 字段、BuffComponent 应用链路、DPS 计算（如果有）、关卡平衡影响 |
| 踩坑规避 | 1 | 改完 SO 后同步验证 Inspector 显示正确；不误改其他 Buff；退场清理不受影响 |
| 验证闭环 | 2 | 编译通过、SO Validator 通过、Play mode：装备 Buff→确认 +50%→退场无残留 |

**预期产出**：
- 修改目标 BuffConfigSO 的数值字段
- 验证结果日志

### EVAL-R3：调试一个可控的渲染问题（预计 15min）

**任务描述**：Agent 被问到"为什么某个特定子弹类型在 Game View 中不显示？"（实际原因：该子弹的 `BulletTypeSO` 未设置纹理引用，导致 UV 采样为空）。Agent 需要按照 DEBUG_PLAYBOOK 流程排查。

**评估维度**：

| 维度 | 分数 | 期望行为 |
|------|------|----------|
| 路由 | 2 | Agent 先读 `CONTEXT_PACKS/Danmaku_Rendering.md` → `DEBUG_PLAYBOOK.md` → `MODULE_CARDS/DanmakuSystem.md` |
| 上下文效率 | 1 | 不无差别读取大量渲染代码 |
| 设计一致性 | 2 | 按照 DEBUG_PLAYBOOK 第 3.5 节的"现象→证据"排查顺序 |
| 影响面 | 1 | 识别：active count、bucket、RT 像素、UV、shaderKeywords 等检查点 |
| 踩坑规避 | 1 | 应用 PIT-028（顶点字段顺序）、PIT-029（材质纹理未绑定）、DEBUG_PLAYBOOK 3.1（DrawCall≠可见） |
| 验证闭环 | 2 | 使用 Unity MCP 截图对比修复前后；确认修复后不需要回退 |

**预期产出**：
- 排查日志（按 DEBUG_PLAYBOOK 格式）
- 修复（设置纹理引用或补全 BulletTypeSO）
- 修复前后 Game View 截图对比

## 3. 评估流程

每个任务执行步骤：
1. Agent 读取 `AGENT_BOOTSTRAP.md` → `INDEX.md`
2. Agent 根据任务类型选择 Context Pack 和 Module Card
3. Agent 读取相关 TDD 和 ADR
4. Agent 输出：影响面分析 + 设计思路 + 预期验证方案
5. Agent 执行代码/SO 修改（使用 Unity MCP）
6. Agent 编译验证
7. Agent 记录：实际读取文件列表、实际修改、验证结果

## 4. 评分记录模板

```markdown
| 任务 | 路由(2) | 效率(1) | 设计(2) | 影响面(2) | 踩坑(1) | 验证(2) | 总分 | 备注 |
|------|---------|---------|---------|-----------|---------|---------|------|------|
| EVAL-R1 | | | | | | | | |
| EVAL-R2 | | | | | | | | |
| EVAL-R3 | | | | | | | | |
```

## 5. 执行时机

当 Unity Editor 启动且 MCP Bridge 可用时，Agent 应按本文执行评估。
建议单独用一个会话专门跑评估，便于对比路由前后差异。
