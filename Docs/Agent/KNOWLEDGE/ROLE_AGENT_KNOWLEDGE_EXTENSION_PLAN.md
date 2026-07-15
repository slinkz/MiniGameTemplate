---
system: knowledge-engineering
scope: role-agent-knowledge-and-asset-pipeline
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP.md, Docs/Agent/SHOOTER_GAME/SG_GAME_DESIGN.md, Docs/Agent/SHOOTER_GAME/GDD/SG_GDD_INDEX.md, Docs/Agent/SHOOTER_GAME/SG_UI_DESIGN.md, Docs/Agent/CONTEXT_PACKS/FairyGUI_UI.md, Docs/Agent/MODULE_CARDS/UISystem_FairyGUI.md, skills/fairygui-tools/SKILL.md, skills/vfx-creator/SKILL.md
---

# 职业 Agent 知识工程与资产生产扩展方案

> 定位：当前知识工程已经能很好地支持“程序员 Agent”进行代码阅读、影响面分析、实现和验证。本方案补齐游戏项目中另外两类核心协作者：策划 Agent、UI/美术 Agent，以及它们对应的资产生产、交付和验收流水线。

## 1. 结论摘要

当前体系不是缺少 GDD 或 UI 文档，而是缺少“面向职业 Agent 的上岗入口和产出闭环”。

已有能力：

| 维度 | 当前状态 | 判断 |
|------|----------|------|
| 程序员 Agent | `AGENT_BOOTSTRAP`、Context Pack、Module Card、ADR、代码映射、知识维护、Evals 已形成闭环 | 成熟 |
| 策划知识 | `SG_GAME_DESIGN`、`SG_GDD_*`、`SO_WORKFLOWS`、关卡/技能/数值描述较完整 | 有设计资料，但不是职业工作台 |
| UI 设计知识 | `SG_UI_DESIGN`、`CONTEXT_PACKS/FairyGUI_UI`、`fairygui-tools` skill 已覆盖 UI 工程链路 | 工程侧强，设计系统/资产规格侧不足 |
| VFX/美术资产 | `vfx-creator` skill、`SG_GDD_04_WORKFLOW` 中有 VFX 和资源清单 | 有局部 SOP，缺少总资产管线 |
| 音频/字体/图标/2D sprite | 只有散落需求或清单 | 明显缺口 |
| 资产验收 | FairyGUI 部分有校验思路，VFX 有验证清单 | 缺少跨资产类型的统一验收 |
| 职业 Agent 评估 | 当前 Evals 主要验证编码型任务 | 缺少策划/UI/资产生产任务评估 |

核心缺口：

1. 缺少角色化入口：策划 Agent 和 UI/美术 Agent 不知道“先读什么、产出什么、如何交付给程序员”。
2. 缺少游戏设计知识的结构化资产：玩法、关卡、技能、经济、数值、反馈、玩家动线还没有被拆成可维护的设计卡片。
3. 缺少 UI 设计系统：现有 UI 文档偏屏幕规格和 FairyGUI 实现，缺少 design tokens、组件库、状态矩阵、动效语义、截图验收标准的统一入口。
4. 缺少资产生产总管线：sprite、VFX、UI 图标、音频、字体、背景、Spine/动画等没有统一的命名、目录、导入、配置、预览、验收和变更记录规范。
5. 缺少非代码任务的评估和维护闭环：当前知识同步能拦代码/资产变更，但没有专门验证“设计变更是否更新 GDD/UI/资产规格”。

## 2. 角色模型

新增三类职业 Agent 视角，但不一定要物理拆成三个系统；核心是让同一个 Agent 能按角色切换上下文。

| 角色 | 核心问题 | 主要产出 | 需要的知识入口 |
|------|----------|----------|----------------|
| 策划 Agent | 玩法是否成立、数值是否合理、关卡节奏是否清楚、配置能否交付 | GDD 变更、数值表、关卡波次、技能/Buff/道具方案、验收剧本 | `DESIGNER_BOOTSTRAP`、设计卡片、平衡基准、配置工作流 |
| UI/UX Agent | 玩家动线是否顺、界面状态是否完整、组件是否一致、交互反馈是否合理 | Screen spec、组件规格、状态矩阵、动效说明、FairyGUI 白模任务 | `UI_AGENT_BOOTSTRAP`、UI Design System、Screen Cards、FairyGUI SOP |
| 美术/资产 Agent | 资源规格是否可用、命名/尺寸/导入是否正确、是否能被 SO/UI/VFX 链路引用 | 资产清单、生成 prompt、导入记录、预览截图、验收清单 | `ASSET_PIPELINE`、Asset Type Cards、VFX/UI/Sprite/Audio SOP |

## 3. 目标文档树

建议在 `Docs/Agent` 下新增两个目录和若干角色入口：

```text
Docs/Agent/
├── ROLES/DESIGNER_BOOTSTRAP.md
├── ROLES/UI_AGENT_BOOTSTRAP.md
├── ROLES/ART_ASSET_AGENT_BOOTSTRAP.md
├── KNOWLEDGE/ROLE_AGENT_KNOWLEDGE_EXTENSION_PLAN.md
├── DESIGN/
│   ├── README.md
│   ├── DESIGN_PILLARS.md
│   ├── PLAYER_JOURNEY.md
│   ├── LEVEL_DESIGN_GUIDE.md
│   ├── BALANCE_BASELINES.md
│   ├── SKILL_DESIGN_CARDS.md
│   ├── ENEMY_DESIGN_CARDS.md
│   ├── ITEM_BUFF_DESIGN_CARDS.md
│   └── ECONOMY_AND_PROGRESSION.md
├── UI_DESIGN/
│   ├── README.md
│   ├── UI_DESIGN_SYSTEM.md
│   ├── UI_COMPONENT_LIBRARY.md
│   ├── SCREEN_CARDS.md
│   ├── UI_MOTION_GUIDE.md
│   ├── UI_COPY_GUIDE.md
│   └── FAIRYGUI_HANDOFF_CHECKLIST.md
└── ASSET_PIPELINE/
    ├── README.md
    ├── ASSET_MANIFEST.md
    ├── ASSET_NAMING_AND_PATHS.md
    ├── SPRITE_PIPELINE.md
    ├── VFX_PIPELINE.md
    ├── UI_ICON_PIPELINE.md
    ├── AUDIO_PIPELINE.md
    ├── FONT_TEXT_PIPELINE.md
    ├── IMPORT_SETTINGS.md
    ├── PREVIEW_AND_ACCEPTANCE.md
    └── GENERATIVE_ASSET_PROMPTS.md
```

## 4. 文档职责边界

### 4.1 策划知识层

| 文档 | 解决的问题 | 与现有文档关系 |
|------|------------|----------------|
| `ROLES/DESIGNER_BOOTSTRAP.md` | 策划 Agent 上岗入口：当前游戏、可改范围、交付物、验证方式 | 类似 `AGENT_BOOTSTRAP`，但只面向设计工作 |
| `DESIGN/README.md` | 设计文档路由：改技能/关卡/敌人/道具/经济时读什么 | 从 `INDEX.md` 分担设计路由 |
| `DESIGN_PILLARS.md` | 设计支柱和禁止事项，例如自动战斗、走位即策略、火力渐强 | 从 `SG_GAME_DESIGN` 提炼稳定原则 |
| `PLAYER_JOURNEY.md` | 玩家从加载、选关、战斗、结算到重试的体验曲线 | 对接 UI/UX 和 AppFlow |
| `LEVEL_DESIGN_GUIDE.md` | 关卡节奏、波次结构、难度曲线、出怪模板、验收剧本 | 承接 `SG_GAME_DESIGN` §4/§5 和 `SG_TOOLS_TDD` |
| `BALANCE_BASELINES.md` | 速度、DPS、HP、掉落率、同屏上限、目标通关时间 | 补足当前性能/数值基准缺口 |
| `SKILL_DESIGN_CARDS.md` | 每个技能的目标体验、数值、视觉/音频需求、配置链路 | 从 `SG_GDD_01` 提炼成卡片 |
| `ENEMY_DESIGN_CARDS.md` | 每类敌人的职责、威胁、速度、HP、弹幕、资源需求 | 当前缺口较明显 |
| `ITEM_BUFF_DESIGN_CARDS.md` | Buff/DOT/道具的设计语义、叠加规则、UI 显示、VFX | 从 `SG_GDD_02/03` 提炼 |
| `ECONOMY_AND_PROGRESSION.md` | 解锁、奖励、复活、广告、皮肤、长期留存 | 当前 mostly future，先用 proposal 状态 |

### 4.2 UI/UX 知识层

| 文档 | 解决的问题 | 与现有文档关系 |
|------|------------|----------------|
| `ROLES/UI_AGENT_BOOTSTRAP.md` | UI Agent 上岗入口：先读 UI 设计系统、屏幕卡、FairyGUI 约束 | 新增角色入口 |
| `UI_DESIGN/README.md` | UI 任务路由 | 对接 `CONTEXT_PACKS/FairyGUI_UI` |
| `UI_DESIGN_SYSTEM.md` | 颜色、字号、间距、圆角、按钮、图标、触摸热区、安全区 | 从 `SG_UI_DESIGN` §10 扩展 |
| `UI_COMPONENT_LIBRARY.md` | CommonButton、ProgressBar、LevelNode、SkillSlot、PopupButton 等组件规格 | 对接 FairyGUI 包和生成代码 |
| `SCREEN_CARDS.md` | Loading、LevelSelect、BattleHUD、Sortie、Pause、Victory、Defeat 等每屏状态 | 从 `SG_UI_DESIGN` 提炼 |
| `UI_MOTION_GUIDE.md` | 转场、弹窗、血条、飘字、解锁、危险态、暂停态动效语义 | 从 `SG_UI_DESIGN` §3/§9 扩展 |
| `UI_COPY_GUIDE.md` | 按钮文案、失败鼓励文案、Toast、错误提示、中文风格 | 当前缺口 |
| `FAIRYGUI_HANDOFF_CHECKLIST.md` | UI 设计到 FairyGUI XML/发布/代码绑定的交付清单 | 对接 `fairygui-tools` skill |

### 4.3 资产生产层

| 文档 | 解决的问题 | 与现有文档关系 |
|------|------------|----------------|
| `ROLES/ART_ASSET_AGENT_BOOTSTRAP.md` | 资产 Agent 上岗入口：任务分型、目录、命名、验收 | 新增角色入口 |
| `ASSET_PIPELINE/README.md` | 资产任务路由 | 总入口 |
| `ASSET_MANIFEST.md` | 当前资产清单、状态、来源、用途、接入点 | 补齐跨资源类型的 single source |
| `ASSET_NAMING_AND_PATHS.md` | 文件命名、目录、后缀禁忌、版本命名、Unity 路径 | 统一 `SG_GDD_04` 和项目导入规则 |
| `SPRITE_PIPELINE.md` | 飞机、敌机、子弹、道具、背景 sprite 生产和接入 | 当前散落在 GDD |
| `VFX_PIPELINE.md` | sprite sheet VFX 生成、导入、VFXTypeSO、预览、验收 | 对接 `vfx-creator` skill |
| `UI_ICON_PIPELINE.md` | UI 图标、按钮状态、技能图标、Buff 图标、导入 FairyGUI | 当前缺口 |
| `AUDIO_PIPELINE.md` | SFX/BGM 命名、格式、响度、循环、触发点、验收 | 当前明显缺口 |
| `FONT_TEXT_PIPELINE.md` | 字体、字号、缺字、WebGL/微信兼容、Unicode 禁忌 | 对接 FairyGUI skill 坑 |
| `IMPORT_SETTINGS.md` | Texture Type、PPU、Compression、Atlas、Audio import setting | 可逐步自动化 |
| `PREVIEW_AND_ACCEPTANCE.md` | 资产预览场景、截图/录屏、可读性、性能和真机验收 | 对接 Unity MCP/设备验收 |
| `GENERATIVE_ASSET_PROMPTS.md` | AI 生成 sprite/VFX/icon/audio 的 prompt 模板与反例 | 对接 imagegen/vfx-creator |

## 5. 工作流闭环

### 5.1 策划任务闭环

```text
需求输入
  -> DESIGNER_BOOTSTRAP 路由
  -> 读取设计支柱 + 对应设计卡片
  -> 产出设计变更草案
  -> 更新配置影响面：SO / Luban / UI / VFX / Audio
  -> 写验收剧本：Editor 验证、PlayMode、设备验收
  -> 如需程序实现，交付 Implementation Brief
  -> 更新 DESIGN/* 与 INDEX 路由
```

策划交付物模板：

```text
Design Brief
- 目标体验
- 改动范围
- 玩家可感知变化
- 数值/配置变更
- UI/美术/音频需求
- 对应 SO / 表格
- 验收剧本
- 风险与回滚方式
```

### 5.2 UI 任务闭环

```text
UI 需求
  -> UI_AGENT_BOOTSTRAP 路由
  -> 读取 UI Design System + Screen Card
  -> 补全状态矩阵：normal/loading/disabled/locked/error/pause
  -> 产出白模或组件规格
  -> FairyGUI handoff：包、组件、controller、transition、导出名
  -> 运行 fairygui-tools 校验
  -> Unity 接入验证：Binder、Logic.cs、AppFlow、真机触摸
```

UI 交付物模板：

```text
UI Handoff
- Screen / Component
- 进入和退出路径
- 状态矩阵
- 交互事件
- 动效
- 文案
- FairyGUI 包/组件/导出类
- 数据绑定 SO
- 验收截图/录屏要求
```

### 5.3 资产任务闭环

```text
资产需求
  -> ART_ASSET_AGENT_BOOTSTRAP 路由
  -> 任务分型：sprite / VFX / UI icon / audio / font / background
  -> 查 Asset Manifest，避免重复生产
  -> 生成或制作资产
  -> 放入规范目录并按导入设置处理
  -> 创建或更新 SO/FairyGUI/VFXType/AudioConfig 引用
  -> 在预览场景或目标业务链路验证
  -> 更新 ASSET_MANIFEST 与变更记录
```

资产交付物模板：

```text
Asset Handoff
- Asset ID
- 类型和用途
- 文件路径
- 源文件路径
- 导出规格
- Unity 导入设置
- 接入点：SO / FairyGUI / Prefab / VFXType / Audio trigger
- 预览方式
- 验收结果
```

## 6. 与现有知识工程的接入点

需要更新的现有文件：

| 文件 | 更新内容 |
|------|----------|
| `Docs/Agent/INDEX.md` | 路由表 A 新增策划/UI/资产任务入口；文件体系总览新增三个目录 |
| `AGENT_BOOTSTRAP.md` | 说明遇到设计/UI/资产任务时切换到对应角色入口 |
| `KNOWLEDGE/KNOWLEDGE_MAINTENANCE.md` | 触发条件增加设计卡片、UI 设计系统、资产清单变更 |
| `KNOWLEDGE/KNOWLEDGE_EVALS.md` | 新增非代码评估任务 |
| `Tools/knowledge-sync-check.ps1` | 后续可增加对 `Docs/Agent/DESIGN`、`UI_DESIGN`、`ASSET_PIPELINE` 的同步提示 |
| `skills/` | 后续新增 `game-designer`、`ui-designer`、`asset-pipeline` skill，或扩展现有 skill |

## 7. 建议新增 Skills

### 7.1 `game-designer`

适用任务：

- 新增或修改关卡、敌人、技能、Buff、道具、经济、掉落。
- 做数值平衡、难度曲线、玩家动线、验收剧本。

应引用：

```text
Docs/Agent/ROLES/DESIGNER_BOOTSTRAP.md
Docs/Agent/DESIGN/*.md
Docs/Agent/SHOOTER_GAME/SG_GAME_DESIGN.md
Docs/Agent/SHOOTER_GAME/GDD/SG_GDD_INDEX.md
Docs/Agent/SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX.md
```

### 7.2 `ui-designer`

适用任务：

- 设计新界面、改 UI 状态、输出 FairyGUI 白模、设计组件库、做 UI 走查。

应引用：

```text
Docs/Agent/ROLES/UI_AGENT_BOOTSTRAP.md
Docs/Agent/UI_DESIGN/*.md
Docs/Agent/SHOOTER_GAME/SG_UI_DESIGN.md
skills/fairygui-tools/SKILL.md
```

### 7.3 `asset-pipeline`

适用任务：

- 新增 sprite、icon、VFX、audio、font、background。
- 生成资产 prompt、更新 manifest、接入 SO/FairyGUI/VFX。

应引用：

```text
Docs/Agent/ROLES/ART_ASSET_AGENT_BOOTSTRAP.md
Docs/Agent/ASSET_PIPELINE/*.md
skills/vfx-creator/SKILL.md
skills/fairygui-tools/SKILL.md
```

## 8. 评估体系扩展

新增职业 Agent Evals：

| ID | 任务 | 主要评估点 |
|----|------|------------|
| D-01 | 设计一个新敌机 | 是否明确职责、数值、资源、SO、关卡投放和验收 |
| D-02 | 调整第 3 关难度 | 是否基于基准、只改必要配置、给出前后对比和验收剧本 |
| D-03 | 新增一个 Buff | 是否处理 ID、叠加、UI、VFX、掉落、配置验证 |
| D-04 | 设计复活广告入口 | 是否考虑玩家动线、失败挫败、云存档、微信广告约束 |
| U-01 | 新增出战准备界面状态 | 是否覆盖状态矩阵、组件、数据绑定、FairyGUI handoff |
| U-02 | 改 BattleHUD 技能槽 | 是否考虑安全区、触摸穿透、CD、被动/Buff 折叠和性能 |
| U-03 | 做 UI 视觉一致性走查 | 是否能发现 token、组件、字体、按钮状态不一致 |
| A-01 | 生产一套敌机 sprite | 是否遵守尺寸、命名、导入设置、SO 接入和预览 |
| A-02 | 生产一个 Buff VFX | 是否走 vfx-creator、VFXTypeSO、Registry、业务触发和视觉验收 |
| A-03 | 接入一组音效 | 是否有格式、响度、触发点、循环、微信兼容和验收 |

评分维度：

| 维度 | 权重 | 说明 |
|------|------|------|
| 路由准确 | 20% | 是否读对角色入口和专题文档 |
| 产出完整 | 25% | 是否包含配置、资源、UI、验证、风险 |
| 工程可交接 | 20% | 程序员能否直接实现或接入 |
| 体验一致 | 15% | 是否遵守设计支柱、UI token、资产风格 |
| 验收闭环 | 20% | 是否给出可执行的编辑器/PlayMode/真机验收 |

## 9. 分阶段实施计划

### P9.0 角色入口与路由，0.5-1 天

产物：

- `ROLES/DESIGNER_BOOTSTRAP.md`
- `ROLES/UI_AGENT_BOOTSTRAP.md`
- `ROLES/ART_ASSET_AGENT_BOOTSTRAP.md`
- 更新 `INDEX.md`

验收：

- 任意设计/UI/资产任务都能在 2 步内路由到正确入口。
- 三个入口都说明“读什么、产出什么、如何验收、如何交付给程序员”。

### P9.1 策划知识结构化，1-2 天

产物：

- `DESIGN/README.md`
- `DESIGN_PILLARS.md`
- `LEVEL_DESIGN_GUIDE.md`
- `BALANCE_BASELINES.md`
- `SKILL_DESIGN_CARDS.md`
- `ENEMY_DESIGN_CARDS.md`
- `ITEM_BUFF_DESIGN_CARDS.md`

验收：

- 新增敌人、技能、Buff、关卡调整四类任务不再只依赖长 GDD。
- 每类设计卡片都能反查 SO、UI、VFX、音频需求。

### P9.2 UI 设计系统与交接，1-2 天

产物：

- `UI_DESIGN/README.md`
- `UI_DESIGN_SYSTEM.md`
- `UI_COMPONENT_LIBRARY.md`
- `SCREEN_CARDS.md`
- `UI_MOTION_GUIDE.md`
- `FAIRYGUI_HANDOFF_CHECKLIST.md`

验收：

- UI Agent 能从需求直接产出 FairyGUI 可执行 handoff。
- UI 走查能覆盖按钮状态、组件一致性、安全区、触摸、动效和文案。

### P9.3 资产生产管线，1-2 天

产物：

- `ASSET_PIPELINE/README.md`
- `ASSET_MANIFEST.md`
- `ASSET_NAMING_AND_PATHS.md`
- `SPRITE_PIPELINE.md`
- `VFX_PIPELINE.md`
- `UI_ICON_PIPELINE.md`
- `AUDIO_PIPELINE.md`
- `IMPORT_SETTINGS.md`
- `PREVIEW_AND_ACCEPTANCE.md`

验收：

- 新资产从生成/制作到 Unity 接入有唯一 SOP。
- 每个资产都能追踪来源、路径、导入设置、接入点和验收状态。

### P9.4 Skills 与自动化，1-2 天

产物：

- `skills/game-designer/SKILL.md`
- `skills/ui-designer/SKILL.md`
- `skills/asset-pipeline/SKILL.md`
- 可选脚本：资产 manifest 检查、命名检查、导入设置检查。

验收：

- 角色任务能自动触发对应 skill。
- 资产命名/路径/manifest 的低级错误可被脚本发现。

### P9.5 职业 Agent Evals，1 天

产物：

- 更新 `KNOWLEDGE/KNOWLEDGE_EVALS.md`
- 增加 `KNOWLEDGE_EVALS_ROLE_AGENT_PLAN.md` 或合并到主 Evals。

验收：

- 至少跑通 D-01、U-01、A-02 三个端到端评估。
- 评估结果能反向修正文档或 skill。

## 10. 优先级建议

如果只做一轮，推荐先做：

1. `ROLES/DESIGNER_BOOTSTRAP.md`
2. `ROLES/UI_AGENT_BOOTSTRAP.md`
3. `ROLES/ART_ASSET_AGENT_BOOTSTRAP.md`
4. `ASSET_PIPELINE/ASSET_MANIFEST.md`
5. `ASSET_PIPELINE/ASSET_NAMING_AND_PATHS.md`
6. `UI_DESIGN/UI_DESIGN_SYSTEM.md`
7. `DESIGN/BALANCE_BASELINES.md`

原因：

- 角色入口决定 Agent 能否正确切换视角。
- Asset Manifest 和命名路径规范会立刻降低资产混乱风险。
- UI Design System 和 Balance Baselines 是 UI/策划继续扩展的根。

## 11. Definition of Done

本扩展完成后，应能回答：

1. 策划 Agent 接到“新增敌人/技能/关卡”时，是否知道读哪些设计卡片？
2. UI Agent 接到“新增界面/改组件”时，是否知道 UI token、状态矩阵和 FairyGUI handoff？
3. 资产 Agent 接到“生成图标/VFX/音效”时，是否知道目录、命名、导入、接入和验收？
4. 程序员 Agent 能否从策划/UI/资产交付物直接找到 SO、代码、FairyGUI、VFX 和验证入口？
5. 设计、UI、资产变更是否会触发知识维护检查？
6. 非代码任务是否也有 Evals 能衡量知识工程质量？

## 12. 风险与对策

| 风险 | 表现 | 对策 |
|------|------|------|
| 文档再次膨胀 | 角色文档和原 GDD/UI/TDD 重复 | 新文档只做路由、卡片、SOP，不复制长文 |
| 设计卡片过早固化 | 游戏仍在探索，卡片变成束缚 | 用 `status: draft/proposed/active` 标记成熟度 |
| UI 设计系统与 FairyGUI 资产不同步 | token 写了但包内不一致 | 后续增加 FairyGUI package XML 校验和截图走查 |
| 资产 Manifest 维护成本高 | 每个小图都要手写记录 | 先只记录 P0/P1 资产，后续脚本扫描补全 |
| Audio 被忽略 | 音频没有 owner，后期补很痛 | P9.3 必须至少建立 `AUDIO_PIPELINE.md` 占位和清单 |
| 角色边界模糊 | 策划/UI/资产互相推诿 | 交付模板中明确“谁产出、谁消费、如何验收” |
