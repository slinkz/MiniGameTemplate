---
system: knowledge-engineering
scope: audit-report
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE_ENGINEERING_ROADMAP.md, Docs/Agent/KNOWLEDGE_EVALS.md, Docs/Agent/KNOWLEDGE_INVENTORY.md
---

# MiniGameTemplate 知识工程审计报告

> 定位：对当前项目知识工程的全面评估，基于 2026-07-14 的文档体系状态。

## 1. 总体评价：**成熟度 8.0/10**

MiniGameTemplate 的知识工程已经超越绝大多数开源和商业项目。它不仅"有文档"，而是建立起了一套完整的分层认知基础设施——从 Agent 上岗入口、任务路由、代码映射、ADR 可执行化到评估反馈闭环。这是**行业领先水平**的知识工程实践，特别适合 AI Agent 辅助开发的场景。

核心指标速览：

| 维度 | 得分 | 简述 |
|------|------|------|
| 文档组织 | 9/10 | L0-L5 分层 + 三层路由表 + Agent/人类文档分离 |
| 路由效率 | 9/10 | Agent 可从 INDEX → Context Pack → 3-8 个文件，无需 grep 全目录 |
| 架构约束可执行性 | 9/10 | ADR_SCHEMA 含 AppliesTo/Constraints/Verification/Pitfalls/ChangeProtocol |
| 代码→知识映射 | 8/10 | CODE_KNOWLEDGE_MAP 覆盖 9 个模块，但有少量映射粒度问题 |
| 维护机制 | 8/10 | knowledge-sync-check + pre-commit + 三级维护模式 |
| 评估体系 | 7/10 | 10 个标准任务 + 评分规则，但仅做了静态路由评估 |
| 自动化程度 | 6/10 | 有 CI 检查但缺少自动化一致性验证和新鲜度检测 |

---

## 2. 亮点（What Works Well）

### 2.1 分层知识存储（L0-L5）

```text
L0 战略记忆  → MEMORY.md + USER.md（项目定位、架构哲学）
L1 架构知识  → ADR + ARCHITECTURE + TDD/GDD（系统边界、决策、生命周期）
L2 工作流知识 → Context Packs + SO_WORKFLOWS（新增敌人/技能/UI/SO/构建）
L3 代码知识  → CODE_KNOWLEDGE_MAP + CodeGraph（类/方法/调用链/符号图谱）
L4 事故知识  → DEBUG_PLAYBOOK + known-pitfalls + changes/（踩坑、回归清单）
L5 验证工具  → knowledge-sync-check + Unity MCP + Validator（闭环验证）
```

这是非常清晰的知识分层设计，每一层有明确的读者（Agent vs 人类）、目标和使用场景。

### 2.2 三层路由表（INDEX.md）

`INDEX.md` 的路由表设计**堪称教科书级别**：

- **路由表 A（任务路由）**：50+ 行，覆盖"我要做什么→读什么文件"
- **路由表 B（代码→文档映射）**：100+ 行，覆盖"代码路径→对应文档"
- **路由表 C（概念速查）**：80+ 行，覆盖"术语→定义位置→一句话解释"

这种设计让 Agent 不需要猜测该读什么文档，直接根据任务类型或代码路径定位。

### 2.3 可执行 ADR Schema

传统 ADR 只记录"为什么这么选"，而本项目的 `ADR_SCHEMA.md` 将其升级为**可执行的工程约束**：

| 字段 | 价值 |
|------|------|
| `AppliesTo` | 明确代码路径，Agent 能判断是否触碰 |
| `Constraints` | 必须遵守/禁止事项，编码前即知边界 |
| `Verification` | 编译/PlayMode/Profiler/真机，修改后闭环 |
| `Pitfalls` | 已知坑，避免复发 |
| `ChangeProtocol` | 修改该决策前的要求 |

7 条优先 ADR（012/028/031/032/033/034/035/036）均已可执行化，覆盖战斗、渲染、导航、生命周期、飘字等核心领域。

### 2.4 知识评估闭环

`KNOWLEDGE_EVALS.md` 定义了 10 个标准评估任务和 6 维度评分规则（路由/效率/一致性/影响面/踩坑/验证）。首批运行得分 8.5/10，发现的具体问题（新增关卡路由、ADR-012 可执行化、模块卡补齐等）已全部修正并复测通过。

**反向修正规则**（低分维度→优先修正目标知识资产）确保了评估不是一次性活动，而是持续改进的回路。

### 2.5 自动化门禁

`Tools/knowledge-sync-check.ps1` 是一个轻量但精准的 CI 门禁：当代码/资产敏感路径（Framework/Game/Configs/FairyGUI/UIProject/CloudFunctions/skills）发生变更时，如果没有同步修改 `Docs/Agent/**` 或 Guide 文档，则 CI 失败。这从机制上防止了"代码改了、文档忘了"的常见问题。

### 2.6 Context Pack + Module Card 双轴导航

- **Context Pack（6 个）**：面向任务，回答"做这个任务该读什么"
- **Module Card（10 个）**：面向模块，回答"这个模块的边界是什么、怎么安全修改"

两个维度互补，且都遵循紧凑模板（Context Pack: 适用任务/必读文档/代码入口/SO/ADR/必验；Module Card: 职责/不负责/入口/数据流/生命周期/依赖/ADR/必验）。

---

## 3. 可优化项（Areas for Improvement）

### 3.1 高优先级（P1）

#### 3.1.1 缺少自动化一致性验证器

**现状**：`knowledge-sync-check.ps1` 只能检查"敏感代码变更→是否有文档变更"，但无法验证：
- Module Card 中引用的代码路径是否仍然存在
- ADR_SCHEMA 中的 AppliesTo 路径是否过时
- CODE_KNOWLEDGE_MAP 中的映射是否与实际代码一致

**建议**：基于 CodeGraph 和文件系统扫描，构建一个 `knowledge-consistency-check` 工具，验证：
1. Module Card / CODE_KNOWLEDGE_MAP 中引用的代码路径是否有效
2. ADR_SCHEMA 中 AppliesTo 的路径是否存在
3. Context Pack 中引用的文档是否存在
4. INDEX.md 路由表中的目标文件是否存在

#### 3.1.2 知识评估仅做静态路由验证

**现状**：首批 Evals 只验证了"Agent 是否读对文档"，没有验证"读了正确文档后，是否能写出正确的代码和验证方案"。

**建议**：第二轮评估应包含 2-3 个真实编码任务（如新增一个简单敌人、修改一个 Buff 参数），验证 Agent 从路由→阅读→设计→编码→验证的完整闭环质量。

#### 3.1.3 踩坑知识（Pitfalls）缺少统一索引

**现状**：已知坑分散在 4 处：
- Skills: `code-review-checklist/references/known-pitfalls.md`（活跃 PIT，当前最高 PIT-057）+ `known-pitfalls-archive.md`（归档 PIT）
- `DEBUG_PLAYBOOK.md`（渲染排查）
- Module Card 的"常见错误"字段
- ADR_SCHEMA 的 Pitfalls 字段

**建议**：在 INDEX.md 增加一个"踩坑速查"路由项，指向已知坑的统一入口。或者将 `known-pitfalls.md` 作为 PIT 编号的统一来源，DEBUG_PLAYBOOK / Module Card / ADR_SCHEMA 引用 PIT 编号即可。

### 3.2 中优先级（P2）

#### 3.2.1 Context Pack 覆盖不完整

当前 6 个 Context Pack 覆盖了主要任务类型，但以下任务缺少直达的 Context Pack：

| 缺失 Context Pack | 当前变通方案 | 影响 |
|-------------------|-------------|------|
| Editor Tools | 走 SO_Config_Workflow 或 WeChat_Build_Cloud | 编辑器工具开发任务缺乏任务导向导航 |
| VFX System | 走 Danmaku_Rendering | VFX 创建/调试工作流无独立入口 |
| Audio / Asset / Timer / Pool | 无 | 框架基础服务无 Context Pack |

**建议**：至少补充 EditorTools Context Pack；VFX 评估是否值得独立 Pack；Audio/Pool 等低修改频率模块可暂缓。

#### 3.2.2 缺少性能基准数据

**现状**：文档反复强调"热路径零 GC""DrawCall 控制"，但没有记录可验证的性能基准：

- 战斗场景目标 DrawCall 上限
- 弹幕系统目标峰值子弹数 + 帧时间
- 微信小游戏包体上限
- RuntimeAtlas 预期 Page 数/内存占用

**建议**：在 `MODULE_CARDS/Rendering_RuntimeAtlas.md` 或独立 `PERFORMANCE_BASELINES.md` 中记录关键性能指标，并关联验证方法（Profiler 截图、Unity MCP 数值检查）。

#### 3.2.3 缺少测试策略文档

**现状**：Module Card 和 CODE_KNOWLEDGE_MAP 中均有"修改后必验"项，但：
- 没有统一的测试策略说明（单元测试 vs 集成测试 vs PlayMode vs 人工验收的边界）
- 没有记录哪些模块已有自动化测试、哪些只有手动验收
- 没有测试覆盖率数据

**建议**：创建 `TESTING_STRATEGY.md`，记录各模块的测试现状、目标覆盖率和验证工具。

#### 3.2.4 缺少 API 契约文档

**现状**：理解模块接口需要阅读 TDD 文档 + 源代码。对于框架层对外暴露的接口（如 `IBattleCleanup`、`ISkillEffect`、`IUIPanel`、`IPanelSuspendable`），没有独立的接口契约文档。

**建议**：在 Module Card 中增加"公共接口"章节，或创建 `API_CONTRACTS.md`，列出框架层接口的签名、契约和使用示例。这对新游戏接入框架尤其重要。

### 3.3 低优先级（P3）

#### 3.3.1 元文档膨胀风险

当前知识工程文档本身（KNOWLEDGE_ENGINEERING_ROADMAP、KNOWLEDGE_INVENTORY、KNOWLEDGE_EVALS、KNOWLEDGE_MAINTENANCE、KNOWLEDGE_EVALS_RUN、本审计报告）总计已超 1500 行。这些文档是"关于知识的元知识"，需要警惕变成自身的维护负担。

**建议**：定期审视，合并或精简。例如 KNOWLEDGE_INVENTORY 和 INDEX.md 有部分功能重叠（都做文件清单），可考虑合并。

#### 3.3.2 缺少版本兼容性矩阵

**现状**：没有统一文档记录 Unity 版本、FairyGUI 版本、Luban 版本、微信 SDK 版本、YooAsset 版本的兼容性要求。

**建议**：在 `Docs/Agent/` 或 `README.md` 中增加版本兼容性表。

#### 3.3.3 知识工程缺少从"人类文档→Agent 文档"的生成工具

**现状**：P8.2 将人类可读的 Guide 深文档（Danmaku/框架模块）归档，但仍需要手动维护 Agent 知识文档。当有新模块或新决策时，人类可能只写了 Guide 风格的长文，而没有更新 Context Pack/Module Card。

**建议**：长期考虑构建一个工具，能从代码 + CodeGraph 自动生成 Module Card 的初稿（代码路径、入口类、公共接口），再由人工补充职责、边界、ADR 等语义信息。

#### 3.3.4 缺少多语言支持

**现状**：Agent 文档全部中文，Guide 文档中英文混合。对于可能国际化协作的场景，英文版本的知识工程文档会降低门槛。

**建议**：不是当前优先项，但在项目主线完成后可以补充英文版 AGENT_BOOTSTRAP 和 INDEX。

---

## 4. 对比行业最佳实践

| 实践 | Andrej Karpathy LLM Wiki | MiniGameTemplate | 评价 |
|------|--------------------------|------------------|------|
| 原始资料→编译 Wiki | raw/ → wiki/ 三步（读→写摘要→建关联） | Context Pack + Module Card + CODE_KNOWLEDGE_MAP | 超越——多维度交叉索引 |
| 增量更新 | 追加新内容 + 变更记录 | changes/ 变更包 + DOC_UPDATE_CHECKLIST | 超越——结构化变更包 |
| 矛盾标注 | ⚠️ 标记来源冲突 | ADR_SCHEMA Supersedes + ARCHITECTURE_REVIEW_PROTOCOL | 持平 |
| 知识新鲜度检查 | 手动巡检 | knowledge-sync-check.ps1 CI 门禁 | 超越——自动化 |
| 评估体系 | 无 | KNOWLEDGE_EVALS + 10 任务 + 评分规则 | MiniGameTemplate 独有 |
| 跨会话恢复 | 无标准机制 | KNOWLEDGE_ENGINEERING_ROADMAP + 恢复协议 | MiniGameTemplate 独有 |

---

## 5. 优化优先级路线图

### 近期（P1，1-2 周）

1. **构建知识一致性自动检查器**：验证 Module Card / CODE_KNOWLEDGE_MAP / ADR_SCHEMA 中引用的路径是否有效。
2. **补充 EditorTools Context Pack**：编辑器工具开发任务需要独立的任务导向导航。
3. **统一踩坑知识入口**：在 INDEX.md 增加"踩坑速查"路由，将 PIT 编号作为唯一来源。
4. **运行真实编码 Evals**：选 2-3 个任务（新增敌人、修改 Buff、调试渲染）做端到端验证。

### 中期（P2，2-4 周）

5. **创建 PERFORMANCE_BASELINES.md**：记录关键性能指标。
6. **创建 TESTING_STRATEGY.md**：统一测试策略和覆盖率追踪。
7. **在 Module Card 中增加"公共接口"章节**：记录框架对外 API 契约。
8. **补充 VFX Context Pack**（如果 VFX 修改频率高的话）。

### 远期（P3，1-2 个月）

9. **精简元文档**：合并 KNOWLEDGE_INVENTORY 和 INDEX.md 重复内容；合并 Evals Run 到 Evals 主文档。
10. **构建 Module Card 自动生成原型**：从 CodeGraph 提取代码路径和入口类。
11. **补充版本兼容性矩阵**。
12. **考虑英文版核心知识文档**。

---

## 6. 结论

MiniGameTemplate 的知识工程已经处于**行业领先水平**。它在 Andrej Karpathy LLM Wiki 模式的基础上，独立发展出了一套适配"AI Agent + 复杂游戏项目"的知识管理体系：

- **三层路由是独创性的**——我没有在任何其他开源项目中看到过如此精细的"任务→文档→代码→验证"映射
- **可执行 ADR Schema 做到了传统架构决策记录想做但做不到的事**——从历史记录升级为可检查的工程约束
- **知识评估反馈闭环**让知识工程本身可度量、可改进，这在整个行业都极为罕见
- **CI 门禁自动化**从机制上防止了知识漂移

当前最紧迫的优化不是"补更多文档"，而是三个方向：

1. **自动化验证**：让一致性检查不再依赖人工
2. **真实编码闭环**：用实际编码任务验证知识工程是否真的降低了 Agent 的错误率
3. **踩坑知识收敛**：把分散的已知坑统一索引，减少复发

以当前水平为基准继续迭代 1-2 轮，这套知识工程完全可以成为**独立游戏开发的参考级实践**。
