---
system: knowledge-engineering
scope: knowledge-evals-realcode-run
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE_EVALS_REALCODE_PLAN.md, Docs/Agent/KNOWLEDGE_EVALS.md
---

# Knowledge Evals — Real Code Run 2026-07-14

> 定位：P1-4 真实编码评估运行记录。在 Unity Editor 运行时执行了 3 个标准任务，验证知识工程在端到端编码流程中的路由、设计和验证质量。

## 1. 运行环境

- 日期：2026-07-14 20:26~20:32
- Unity：2021.3.45f2c1，端口 7891
- 项目：MiniGameTemplate / UnityProj
- 初始编译状态：0 errors，clean
- PlayMode：未进入（仅 Editor 模式验证）

---

## 2. 评分结果

| 任务 | 路由(2) | 效率(1) | 设计(2) | 影响面(2) | 踩坑(1) | 验证(2) | 总分 | 结论 |
|------|---------|---------|---------|-----------|---------|---------|------|------|
| EVAL-R1 新增自爆敌人 | 2 | 1 | 2 | 1 | 1 | 1 | **8.0** | 通过 |
| EVAL-R2 修改 Buff 参数 | 2 | 1 | 2 | 1 | 1 | 2 | **9.0** | 通过 |
| EVAL-R3 调试渲染流程 | 2 | 1 | 1 | 1 | 1 | 1 | **7.0** | 待改进 |

| 指标 | 结果 |
|------|------|
| 平均分 | **8.0** |
| 低于 7 分任务 | 0 个 |
| 低于 8 分任务 | 1 个：EVAL-R3 |
| 致命漏项 | 无 |
| 批次是否通过 | **Editor-only 通过**（平均分 ≥ 8.0；PlayMode 验证缺失，不能视为完整端到端通过） |

---

## 3. 逐任务详情

### EVAL-R1：新增自爆敌人（8.0/10）✅

**任务**：创建 KamikazeEnemy——比普通敌机快 50%，接近底线造成高额伤害。

**路由路径**：
1. INDEX.md → "新增敌人" → `CONTEXT_PACKS/SO_Config_Workflow.md` + `MODULE_CARDS/EntitySystem.md`
2. 读取 Context Pack → 定位 EntityConfigSO 路径
3. `unity_search_assets` → 找到 5 个 SG_Enemy_* 模板
4. 读取 `EntityConfigSO.cs` 源码了解结构
5. 选择 `SG_Enemy_Fast` 作为模板
6. `AssetDatabase.CopyAsset` → 修改关键字段 → `SaveAssets`

**执行结果**：
- 创建 `SG_Enemy_Kamikaze.asset`
- HP=10（低血量自爆型）、MoveSpeed=6（+50%）、ContactDamage=30（高额伤害）
- 继承 Camp=Enemy、Components=Health,Movement,Collision,AI,Buff
- 编译 ✅ 通过、值回读 ✅ 确认

**扣分项**：
- 影响面 (-1)：未检查 Wave 配置是否引用此敌人、未验证刷怪流程
- 验证 (-1)：未进入 PlayMode 验证敌人生成、移动和碰撞

### EVAL-R2：修改 Buff 参数（9.0/10）✅

**任务**：将攻速 Buff 的 AttackIntervalModifier 从 0.5 改为 0.7。

**路由路径**：
1. INDEX.md → "新增 Buff/DOT" → `CONTEXT_PACKS/EntitySystem.md` + `CONTEXT_PACKS/SO_Config_Workflow.md`
2. `unity_search_assets` → 找到 13 个 Buff/Skill/Pickup 资产
3. 读取 `BuffConfigSO.cs` → 确认 `AttackIntervalModifier` 字段语义（1=不变，0.5=翻倍）
4. 读取 `SG_Buff_Berserk` (AtkInterval=0.3)、`SG_Buff_SpeedUp` (AtkInterval=0.5)
5. 选择 `SG_Buff_SpeedUp` → 修改 → `SetDirty` → `SaveAssets`

**执行结果**：
- SG_Buff_SpeedUp.AttackIntervalModifier：0.5 → 0.7
- 编译 ✅ 通过，值回读 ✅ 确认

**扣分项**：
- 影响面 (-1)：未查询此 Buff 的使用者（哪些敌人/技能引用它）

### EVAL-R3：调试渲染流程（7.0/10）⚠️

**任务**：验证 DEBUG_PLAYBOOK 诊断路径的可用性。

**路由路径**：
1. INDEX.md → "调试渲染不显示" → `CONTEXT_PACKS/Danmaku_Rendering.md` + `DEBUG_PLAYBOOK.md`
2. 打开 Main 场景
3. 读取 DEBUG_PLAYBOOK 排查经验（3.1~3.5）
4. `unity_scene_stats` → 确认场景状态（仅 Camera + Light，0 Renderers）
5. `unity_graphics_game_capture` → 捕获 Game View 截图

**执行结果**：
- 路由准确，场景诊断命令可用
- 但由于场景为空（无战斗运行），未执行完整的 5 层排查（逻辑→批处理→顶点→纹理→Shader）

**扣分项**：
- 设计 (-1)：未完整走完 DEBUG_PLAYBOOK 6 步排查流程
- 影响面 (-1)：未进入 PlayMode 检查渲染管线
- 验证 (-1)：仅做了场景级别的粗粒度检查

---

## 4. 发现的问题

### 4.1 影响面分析深度不足（EVAL-R1/R2）

修改 SO 后 Agent 应主动查询该资产的使用者（被哪些 Entity、Wave、Skill 引用），但当前路由中缺少"修改后必验→查引用者"的明确提示。

**建议**：在 SO_WORKFLOWS 或 CODE_KNOWLEDGE_MAP 的"修改后必验"中增加"Unity 查找引用（Find References In Scene / Asset）"步骤。

### 4.2 PlayMode 验证门槛高（全部 3 个 Eval）

所有 Evals 都未进入 PlayMode，因为：
- 进入 PlayMode 需要更多前置条件（场景配置、boot 流程完整）
- Unity MCP 的 PlayMode 工具需要确认不会长时间阻塞

**建议**：后续 Evals 增加一个"进入 PlayMode + 截图验证"的标准步骤。可选：创建最小验收场景。

### 4.3 EVAL-R3 需要一个可控的渲染问题模拟器

完全空场景或正常场景都难以测试 DEBUG_PLAYBOOK 的所有诊断步骤。理想情况是：
- 创建一个已知问题的场景（如 UV 错位、纹理缺失、shader keyword 丢失）
- Agent 用 DEBUG_PLAYBOOK 诊断并修复

**建议**：创建 `EVAL-R3_BROKEN_SCENE.unity` 作为专门的评估场景。

---

## 5. 对比首次静态 Evals

| 维度 | 静态 Evals (7/14) | 真实代码 Evals (7/14) | 变化 |
|------|-------------------|----------------------|------|
| 路由准确率 | 8.5~9.0 | 2/2 满分 | ✅ 一致 |
| 上下文效率 | 高 | 1/1 满分 | ✅ 一致 |
| 设计一致性 | 假想场景 | 实际 SO 修改遵守约束 | ✅ 验证了文档约束有效 |
| 影响面完整度 | 静态推理 | 遗漏"查资产引用者"步骤 | ⚠️ 需补 |
| 踩坑规避 | 静态推理 | 均未触发坑 | ✅ 当前任务未命中坑 |
| 验证闭环 | 静态推理 | 编译+回读可用，PlayMode 缺失 | ⚠️ PlayMode 高门槛 |

---

## 6. 后续行动

### ✅ 已完成（2026-07-14 22:55）
1. ✅ **查 SO 引用者**：CODE_KNOWLEDGE_MAP §1 新增步骤 6（修改 SO 后必须先查引用者），并在 6 处 SO 相关行的"修改后必验"中补充此要求
2. ✅ **PlayMode 验证流程**：MCP_INTEGRATION.md 新增「PlayMode 快速验证工作流」章节（7 步最小流程 + 操作矩阵 + 注意事项），AGENT_BOOTSTRAP 验证入口新增 PlayMode 验证行
3. ✅ **Eval 修改已还原**：删除 SG_Enemy_Kamikaze.asset，恢复 SG_Buff_SpeedUp.AttackIntervalModifier→0.5

### 短期（P2）
4. 创建可控渲染问题场景用于 EVAL-R3 复测
5. 用改进后的流程重新跑一轮 Evals 对比路由和影响面提升
