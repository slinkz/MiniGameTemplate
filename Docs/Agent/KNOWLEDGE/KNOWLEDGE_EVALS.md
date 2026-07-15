---
system: knowledge-engineering
scope: knowledge-evals
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/AGENT_BOOTSTRAP.md, Docs/Agent/KNOWLEDGE/CODE_KNOWLEDGE_MAP.md, Docs/Agent/KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL.md, Docs/Agent/KNOWLEDGE/KNOWLEDGE_MAINTENANCE.md
---

# Knowledge Evals

> 定位：这是 P7 的知识工程评估体系，用来判断 Agent 是否真正获得 MiniGameTemplate 的全局视角，而不是只会读局部代码完成单点任务。

## 1. 评估目标

评估不要求 Agent 立刻编码，而是检查它在任务开始前是否能正确完成：

1. 路由到正确文档。
2. 选择合适 Context Pack 和 Module Card。
3. 识别代码路径、数据/资产影响和平台约束。
4. 命中相关 ADR，并判断约束是否满足。
5. 给出可执行验证方案。
6. 判断是否需要架构审查、知识维护或 changes 变更包。

## 2. 评分维度

每个任务 10 分，建议 8 分及以上通过。

| 维度 | 分值 | 达标表现 |
|------|------|----------|
| 路由准确率 | 2 | 读到正确 INDEX 路由、Context Pack、Module Card、TDD/Workflow |
| 上下文效率 | 1 | 没有无差别读取大量无关文档 |
| 设计一致性 | 2 | 遵守 ADR、CONV、模块边界、WebGL/微信约束 |
| 影响面完整度 | 2 | 识别代码、SO、UI、Scene、平台、验证、维护影响 |
| 踩坑规避 | 1 | 主动避开已记录坑，如自动生成代码、热路径 GC、Archive 旧方案 |
| 验证闭环 | 2 | 给出可执行验证项，并区分已验证/未验证风险 |

## 3. 评估流程

1. 选择一个标准任务。
2. 让 Agent 只做任务启动分析，不直接编码。
3. 要求输出：

```text
- 应读取的文档
- 任务等级：Level 0/1/2/3
- 影响模块和代码路径
- 相关 ADR 与约束
- 数据/资产/平台影响
- 验证计划
- 是否需要知识维护或 changes 包
```

4. 按第 2 节评分。
5. 若低于 8 分，修正对应 Context Pack、Module Card、Code Knowledge Map、ADR_SCHEMA、架构审查或维护规则。

## 4. 通过标准

| 级别 | 标准 |
|------|------|
| 单任务通过 | 单个任务 >= 8 分，且无致命漏项 |
| 批次通过 | 10 个任务平均 >= 8 分，且低于 7 分的任务不超过 1 个 |
| 知识系统通过 | 批次通过后，修正所有评估中发现的路由/映射/验证缺口 |

致命漏项包括：

- 引用 Archive 旧方案作为当前事实，尤其是 `Archive/Guide/**` 中已归档的 Danmaku / Framework Modules 长文。
- 漏掉命中的 Accepted ADR。
- 漏掉热路径或 WebGL/微信平台约束。
- 涉及 SO/UI/Scene 却没有资产验证。
- 架构敏感改动却不做架构审查。

## 5. 标准评估任务

### EVAL-01 新增一种敌人

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/SO_Config_Workflow.md`, `CONTEXT_PACKS/EntitySystem.md`, `MODULE_CARDS/EntitySystem.md`, `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` |
| 代码/资产 | `EntityConfigSO`, ShooterGame Configs, Wave/Spawn 配置, Entity 组件 |
| ADR | ADR-033 |
| 影响面 | SO 字段、创建路径、刷怪、碰撞、击杀/越线、关卡波次 |
| 验证 | SO Validator、Missing Reference、刷怪出现、碰撞/受击/死亡、越线扣血、Profiler 热路径无新增 GC |
| 维护 | 若新增核心敌人模式，更新 SO Workflow 或 Code Knowledge Map |

### EVAL-02 新增一个技能

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/EntitySystem.md`, `CONTEXT_PACKS/SO_Config_Workflow.md`, `MODULE_CARDS/EntitySystem.md` |
| 代码/资产 | `SkillConfigSO`, `SkillComponent`, Skill Effect, AimMode, 普攻 Slot[0] |
| ADR | ADR-033 |
| 影响面 | 装备、CD、目标选择、伤害链路、Buff/DOT 交互、战斗退出清理 |
| 验证 | 装备成功、CD 正确、AimMode 正确、伤害结算、Retry/Exit 后无残留 |
| 维护 | 若新增通用技能机制，更新 Module Card、Code Knowledge Map，必要时写 ADR |

### EVAL-03 修改碰撞逻辑

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/EntitySystem.md`, `MODULE_CARDS/EntitySystem.md`, `SYSTEMS/OBB_TDD/OBB_TDD_INDEX.md`, `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` |
| 代码/资产 | `EntitySystem/Scripts/Collision/**`, `HitboxMath.cs`, `CollisionSolver`, `TargetRegistry` |
| ADR | ADR-033, 可能命中 ADR-012 |
| 影响面 | Camp 判定、冷却、OBB 数学、底线检测、弹幕/Entity 碰撞桥接 |
| 验证 | 数学边界用例、碰撞命中/不命中、冷却、底线关系、Profiler 零 GC |
| 维护 | 修改通用碰撞语义时更新 TDD/Module Card/Code Knowledge Map |

### EVAL-04 修改 FairyGUI 面板

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/FairyGUI_UI.md`, `MODULE_CARDS/UISystem_FairyGUI.md`, `MODULE_CARDS/AppFlow.md` |
| 代码/资产 | `UIProject`, `_Game/FairyGUI_Export/**`, `.Logic.cs`, `UIManager`, Binder |
| ADR | ADR-034 |
| 影响面 | 面板生命周期、按钮事件、Suspend/Resume、包发布、自动生成代码 |
| 验证 | FairyGUI 发布、包加载、Open/Refresh/Close、按钮流程、事件不重复绑定、AppFlow 返回 |
| 维护 | 若改变 UI 工作流，更新 FairyGUI Context Pack 或 Skill |

### EVAL-05 修改微信云存储

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/WeChat_Build_Cloud.md`, `PLATFORM/WECHAT_INTEGRATION.md`, `SHOOTER_GAME/TDD/SG_TDD_06_CLOUD_SAVE.md` |
| 代码/资产 | CloudSaveSystem, WxAuth, CloudFunctions, CDN/域名配置 |
| ADR | 平台约束见 CONV；可能关联 ADR-034 启动/流程 |
| 影响面 | 登录、Pull、Upload、Reload、离线、冲突、隐私授权、真机限制 |
| 验证 | Editor fallback、微信开发者工具、云函数返回、离线/重试、真机关键路径 |
| 维护 | 平台流程变化时更新 WeChat 文档、Guide、changes 包 |

### EVAL-06 调整 RuntimeAtlas

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/Danmaku_Rendering.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md`, `DEBUG_PLAYBOOK.md`, `ADR/ADR_SCHEMA.md` |
| 代码/资产 | `RuntimeAtlasSystem/**`, `RenderBatchManager`, `RenderVertex`, Renderer/Material |
| ADR | ADR-028, ADR-031, ADR-032 |
| 影响面 | Page/Channel、UV、DrawCall、材质关键字、WebGL RT、Game View 可见性 |
| 验证 | Allocate、RT 像素采样、UV、shaderKeywords、DrawCall、Profiler、WebGL/真机风险 |
| 维护 | 渲染管线变化必须更新 ADR/Debug Playbook/Code Knowledge Map |

### EVAL-07 新增关卡

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/ShooterGame_Battle.md`, `CONTEXT_PACKS/SO_Config_Workflow.md`, `MODULE_CARDS/ShooterGame.md` |
| 代码/资产 | Level SO, Wave SO, Enemy/Skill 配置, `SG_ProgressManager` |
| ADR | ADR-033, ADR-034 |
| 影响面 | 解锁、保存、云同步、进入战斗、胜负、返回 Main |
| 验证 | 关卡显示、进入战斗、波次生成、胜利解锁、保存/Reload、返回流程 |
| 维护 | 新增流程或配置字段时更新 SO Workflow、Code Knowledge Map |

### EVAL-08 新增 Buff/DOT

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/EntitySystem.md`, `CONTEXT_PACKS/SO_Config_Workflow.md`, `MODULE_CARDS/EntitySystem.md` |
| 代码/资产 | `BuffComponent`, `DotConfigSO`, `PassiveComponent`, Effect 链路 |
| ADR | ADR-033 |
| 影响面 | 叠加规则、持续时间、tick 顺序、伤害来源、退场清理 |
| 验证 | Buff 叠加/刷新/结束、DOT tick、被动触发、Retry/Exit 清理、零 GC |
| 维护 | 新增通用 Buff 规则时更新 TDD/Module Card |

### EVAL-09 调试渲染不显示

| 项 | 期望 |
|----|------|
| 先读 | `CONTEXT_PACKS/Danmaku_Rendering.md`, `DEBUG_PLAYBOOK.md`, `MODULE_CARDS/DanmakuSystem.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md` |
| 代码/资产 | DanmakuSystem, Renderer, RuntimeAtlas, RenderBatchManager, Material/Shader |
| ADR | ADR-028, ADR-031, ADR-032, ADR-036 |
| 影响面 | 数据是否存在、bucket、mesh upload、RT、UV、shaderKeywords、相机/排序 |
| 验证 | active count、bucket count、Frame Debugger、RT 像素、Game View 截图、材质关键字 |
| 维护 | 新排查路径写入 Debug Playbook；高价值 bugfix 建 changes 包 |

### EVAL-10 做一次架构重构评审

| 项 | 期望 |
|----|------|
| 先读 | `KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL.md`, `templates/ARCH_REVIEW_TEMPLATE.md`, `ADR/ADR_SCHEMA.md`, 相关 Module Card/Context Pack |
| 代码/资产 | 由重构主题决定；必须用 `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` 反查 |
| ADR | 根据 AppliesTo 命中；可能需要新增 ADR |
| 影响面 | 模块依赖方向、框架/Game 边界、热路径、平台、验证器、知识维护 |
| 验证 | 架构检查、编译、核心流程、Profiler、平台专项、回归任务 |
| 维护 | Level 2/3 必须给出知识资产更新计划；架构迁移创建 changes 包 |

## 6. 评估记录模板

```markdown
## Eval Run - <YYYY-MM-DD> - <Agent/Model>

| 任务 | 分数 | 主要漏项 | 需修正文档 |
|------|------|----------|------------|
| EVAL-01 | | | |

### 结论

- 批次是否通过：
- 平均分：
- 低于 7 分任务：
- 需要优先修正的知识资产：
```

## 7. 反向修正规则

| 低分维度 | 优先修正 |
|----------|----------|
| 路由准确率低 | `INDEX.md`, Context Pack |
| 上下文效率低 | Context Pack 缩减，Module Card 增强入口 |
| 设计一致性低 | `ADR/ADR_SCHEMA.md`, `KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL.md` |
| 影响面漏项 | `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md`, Module Card |
| 踩坑复发 | `DEBUG_PLAYBOOK.md`, changes 包, Skill references |
| 验证闭环弱 | Context Pack 必验项、Code Knowledge Map 验证列 |

P7 不是终点；它是后续持续校准知识工程的回路。

## 8. 评估记录索引

| 日期 | 类型 | 报告 | 平均分 | 结论 |
|------|------|------|--------|------|
| 2026-07-14 | 静态路由评估 | `KNOWLEDGE/KNOWLEDGE_EVALS_RUN_2026-07-14.md` | 8.5 | 通过 |
| 2026-07-14 | 真实编码评估 | `KNOWLEDGE/KNOWLEDGE_EVALS_REALCODE_RUN_2026-07-14.md` | 8.0 | Editor-only 通过；PlayMode 待补 |

## 9. 职业 Agent 扩展评估

P9 新增策划/UI/资产 Agent 后，标准评估任务扩展为 D/U/A 三组。评分仍按 10 分制，但重点从“能否编码”转为“能否交付给程序员和资产链路闭环”。

| 维度 | 分值 | 达标表现 |
|------|------|----------|
| 角色路由 | 2 | 读到 `DESIGNER_BOOTSTRAP` / `UI_AGENT_BOOTSTRAP` / `ART_ASSET_AGENT_BOOTSTRAP` 和专题文档 |
| 产出完整 | 2 | 有 Design/UI/Asset Handoff，不停留在概念描述 |
| 影响面 | 2 | 识别 SO、UI、VFX、Audio、代码、平台和验收影响 |
| 一致性 | 2 | 遵守设计支柱、UI token、资产命名路径和 FairyGUI/VFX 规则 |
| 验收闭环 | 2 | 给出 Editor / PlayMode / 截图录屏 / 真机等可执行验收 |

### D 组：策划 Agent

| ID | 任务 | 必读 | 期望 |
|----|------|------|------|
| D-01 | 设计一个新敌人 | `ROLES/DESIGNER_BOOTSTRAP.md`, `DESIGN/ENEMY_DESIGN_CARDS.md` | 敌人职责、数值、资产、SO、关卡投放、验收剧本 |
| D-02 | 调整第 3 关难度 | `DESIGN/LEVEL_DESIGN_GUIDE.md`, `DESIGN/BALANCE_BASELINES.md` | 前后对比、调参顺序、波次/HP/速度影响、回归验证 |
| D-03 | 新增一个 Buff | `DESIGN/ITEM_BUFF_DESIGN_CARDS.md`, `SHOOTER_GAME/GDD/SG_GDD_02_PASSIVE_BUFFS.md` | ID、叠加、UI、VFX、掉落、清理和配置验证 |
| D-04 | 设计复活广告入口 | `DESIGN/ECONOMY_AND_PROGRESSION.md`, `PLATFORM/WECHAT_INTEGRATION.md` | 失败动线、广告回调、云存储、UI 状态、平台风险 |

### U 组：UI Agent

| ID | 任务 | 必读 | 期望 |
|----|------|------|------|
| U-01 | 新增出战准备界面状态 | `ROLES/UI_AGENT_BOOTSTRAP.md`, `UI_DESIGN/SCREEN_CARDS.md` | 状态矩阵、组件、数据绑定、FairyGUI handoff |
| U-02 | 改 BattleHUD 技能槽 | `UI_DESIGN/UI_COMPONENT_LIBRARY.md`, `UI_DESIGN/UI_DESIGN_SYSTEM.md` | 安全区、触摸穿透、CD、被动/Buff 折叠和性能 |
| U-03 | 做 UI 视觉一致性走查 | `UI_DESIGN/README.md` | token、组件、字体、按钮状态、动效和文案问题清单 |

### A 组：资产 Agent

| ID | 任务 | 必读 | 期望 |
|----|------|------|------|
| A-01 | 生产一套敌机 sprite | `ROLES/ART_ASSET_AGENT_BOOTSTRAP.md`, `ASSET_PIPELINE/SPRITE_PIPELINE.md` | 尺寸、命名、导入、SO 接入、Manifest 和预览 |
| A-02 | 生产一个 Buff VFX | `ASSET_PIPELINE/VFX_PIPELINE.md`, `skills/vfx-creator/SKILL.md` | VFXTypeSO、Registry、业务触发、视觉验收 |
| A-03 | 接入一组音效 | `ASSET_PIPELINE/AUDIO_PIPELINE.md` | 格式、响度、触发点、循环、暂停/重试和真机验收 |
