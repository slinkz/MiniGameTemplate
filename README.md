# MiniGameTemplate

Unity 微信小游戏开发模板。项目以 ScriptableObject 驱动的模块化架构为核心，包含 Entity-Component 战斗框架、弹幕/RuntimeAtlas 渲染、FairyGUI UI、Luban 配置、微信小游戏集成，以及面向 AI Agent 协作的知识工程体系。

## 当前定位

MiniGameTemplate 既是一个小游戏开发模板，也是一个 Agent 友好的游戏项目工作台：

- **程序员 Agent 友好**：`Docs/Agent/INDEX.md`、Context Pack、Module Card、ADR、代码知识映射和验证门禁已经形成闭环。
- **策划/UI/资产 Agent 友好**：新增 `DESIGN/`、`UI_DESIGN/`、`ASSET_PIPELINE/` 和对应 skills，支持非代码职业 Agent 上岗。
- **设计师可调参**：敌人、技能、Buff、关卡、弹幕和 UI 状态优先走 SO/FairyGUI/配置资产。
- **微信小游戏优先**：架构和验证流程长期约束 WebGL/微信真机表现。

## 技术栈

| 项目 | 当前选择 |
|------|----------|
| Unity | `2021.3.45f2c1` |
| 渲染管线 | Built-in Render Pipeline |
| UI | FairyGUI |
| 配置 | Luban（本地 package） |
| 资源管理 | YooAsset（本地源码） |
| 目标平台 | 微信小游戏 / WebGL |
| 战斗框架 | 自研 Entity-Component（纯 C# Entity + SO 配置） |
| 弹幕/渲染 | DanmakuSystem + RuntimeAtlas + BatchRenderer |
| 碰撞 | 圆形碰撞 + OBB 数学工具 |
| 可选动画 | Spine runtime（按需启用） |
| Agent 工具 | Unity MCP、CodeGraph 文档、仓库内 skills |

## 项目结构

```text
MiniGameTemplate/
├── Docs/
│   ├── Agent/                  # Agent 知识工程：152 活跃文档 + 90 归档文档
│   │   ├── INDEX.md            # 总索引：任务路由、代码映射、概念速查、踩坑速查
│   │   ├── AGENT_BOOTSTRAP.md  # 新 Agent 上岗入口
│   │   ├── DESIGN/             # 策划 Agent：设计支柱、关卡、平衡、技能、敌人、Buff/道具
│   │   ├── UI_DESIGN/          # UI Agent：设计系统、组件库、屏幕卡、动效、文案
│   │   ├── ASSET_PIPELINE/     # 资产 Agent：Manifest、命名、sprite/VFX/audio/font 管线
│   │   ├── CONTEXT_PACKS/      # 高频任务最小上下文
│   │   ├── MODULE_CARDS/       # 模块职责、边界、入口、验证
│   │   ├── ADR_*.md            # 架构决策与可执行约束
│   │   └── *_TDD_*.md / SG_*   # 框架与 ShooterGame 设计/技术文档
│   └── Guide/                  # 人类开发者操作型文档
├── UIProject/                  # FairyGUI 编辑器工程
├── UnityProj/                  # Unity 工程根目录
│   ├── Assets/_Framework/      # 框架层：Entity、Danmaku、Data、UI、Rendering、VFX、WeChat 等
│   ├── Assets/_Game/           # 当前游戏开发区：Configs、Scripts、Scenes、FairyGUI_Export
│   ├── Assets/_Example/        # 示例场景和 Demo
│   ├── DataTables/             # Luban 配置表源数据
│   ├── Packages/               # Unity package manifest + 本地嵌入包
│   ├── ThirdParty/             # FairyGUI / Spine / YooAsset 等第三方源码
│   └── Tools/                  # Luban、FairyGUI、Spine 设置脚本
├── skills/                     # 仓库内 Agent skills
├── Tools/                      # 知识工程检查脚本
├── README.md
└── CHANGELOG.md
```

## 快速开始

1. Clone 仓库。
2. 用 Unity 打开 `UnityProj/`。
3. Windows 运行 `UnityProj/Tools/setup_fairygui.bat` 或 PowerShell 版本。
4. 如需 Spine，运行 `UnityProj/Tools/setup_spine.bat` 或对应脚本。
5. 详细环境步骤见 [Docs/Guide/GETTING_STARTED.md](Docs/Guide/GETTING_STARTED.md)。
6. 微信小游戏构建见 [Docs/Guide/BUILD_MINIGAME.md](Docs/Guide/BUILD_MINIGAME.md)。

## 文档入口

### 人类开发者

| 入口 | 用途 |
|------|------|
| [Docs/Guide/README.md](Docs/Guide/README.md) | Guide 总导航 |
| [Docs/Guide/GETTING_STARTED.md](Docs/Guide/GETTING_STARTED.md) | 环境搭建与首次运行 |
| [Docs/Guide/ARCHITECTURE_OVERVIEW.md](Docs/Guide/ARCHITECTURE_OVERVIEW.md) | 人类版架构概览 |
| [Docs/Guide/EXAMPLE_WALKTHROUGH.md](Docs/Guide/EXAMPLE_WALKTHROUGH.md) | 示例游戏解读 |
| [Docs/Guide/BUILD_MINIGAME.md](Docs/Guide/BUILD_MINIGAME.md) | 微信小游戏构建 |
| [Docs/Guide/FAQ.md](Docs/Guide/FAQ.md) | 常见问题 |

### AI Agent

| 入口 | 用途 |
|------|------|
| [Docs/Agent/AGENT_BOOTSTRAP.md](Docs/Agent/AGENT_BOOTSTRAP.md) | 新会话上岗入口 |
| [Docs/Agent/INDEX.md](Docs/Agent/INDEX.md) | 总路由表 |
| [Docs/Agent/CODE_KNOWLEDGE_MAP.md](Docs/Agent/CODE_KNOWLEDGE_MAP.md) | 代码路径到文档/ADR/验证的映射 |
| [Docs/Agent/KNOWLEDGE_MAINTENANCE.md](Docs/Agent/KNOWLEDGE_MAINTENANCE.md) | 知识维护规则 |
| [Docs/Agent/KNOWLEDGE_EVALS.md](Docs/Agent/KNOWLEDGE_EVALS.md) | Agent 评估任务 |

### 职业 Agent

| 角色 | 入口 |
|------|------|
| 策划 Agent | [Docs/Agent/DESIGNER_BOOTSTRAP.md](Docs/Agent/DESIGNER_BOOTSTRAP.md), [Docs/Agent/DESIGN/README.md](Docs/Agent/DESIGN/README.md), `skills/game-designer` |
| UI Agent | [Docs/Agent/UI_AGENT_BOOTSTRAP.md](Docs/Agent/UI_AGENT_BOOTSTRAP.md), [Docs/Agent/UI_DESIGN/README.md](Docs/Agent/UI_DESIGN/README.md), `skills/ui-designer` |
| 资产 Agent | [Docs/Agent/ART_ASSET_AGENT_BOOTSTRAP.md](Docs/Agent/ART_ASSET_AGENT_BOOTSTRAP.md), [Docs/Agent/ASSET_PIPELINE/README.md](Docs/Agent/ASSET_PIPELINE/README.md), `skills/asset-pipeline` |

## 常用检查

```powershell
python Tools/knowledge-consistency-check.py --allow-warnings
powershell -ExecutionPolicy Bypass -File Tools/knowledge-sync-check.ps1
```

三个职业 skill 可用官方 skill 校验脚本检查：

```powershell
$env:PYTHONUTF8='1'
python C:\Users\traimenxu\.codex\skills\.system\skill-creator\scripts\quick_validate.py skills\game-designer
python C:\Users\traimenxu\.codex\skills\.system\skill-creator\scripts\quick_validate.py skills\ui-designer
python C:\Users\traimenxu\.codex\skills\.system\skill-creator\scripts\quick_validate.py skills\asset-pipeline
```

## 架构红线

- 不在业务逻辑里使用 `GameObject.Find()` / `FindObjectOfType()`。
- 不用魔法字符串表达场景、标签和关键配置。
- 不跨系统直接硬引用场景对象。
- 不手改 FairyGUI 自动生成代码，业务写 `.Logic.cs`。
- 不把 Archive 文档当成当前事实源。
- 重要代码、UI、配置或资产变更后按知识维护清单同步文档。

## 版本历史

详见 [CHANGELOG.md](CHANGELOG.md)。

## 许可证

私有项目，未公开发布。
