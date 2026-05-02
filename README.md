# MiniGameTemplate

Unity 微信小游戏开发模板 — 基于 ScriptableObject 驱动的模块化架构，面向微信小游戏平台。

## 🎯 设计目标

为 Agent（AI 开发助手）和人类开发者提供一个**开箱即用**的小游戏开发起点：

- **ScriptableObject 驱动**：数据、事件、配置全部通过 SO 资产管理，零硬编码引用
- **模块化自包含**：每个系统一个目录，目录内含文档，改一个模块不影响其他
- **设计师友好**：非程序员可通过 Inspector 调整游戏参数
- **Agent 友好**：完整文档体系（43 个活文档 + INDEX 三路由表），Agent 一键定位所需信息

## 🏗️ 技术栈

| 项目 | 选择 |
|------|------|
| Unity 版本 | 2022 LTS（2021.3.17f1） |
| 渲染管线 | Built-in Render Pipeline |
| UI 框架 | FairyGUI |
| 配置表 | Luban v4.6.0（cs-bin 格式） |
| 目标平台 | 微信小游戏 (WebGL) |
| 资源管理 | YooAsset 2.3.18（本地源码） |
| 弹幕系统 | 自研 DanmakuSystem（BatchRenderer + RuntimeAtlas） |
| Entity 框架 | 自研 Entity-Component（纯 C# 对象，零 GC） |
| 碰撞系统 | OBB 碰撞检测（Entity-Component 集成） |
| Spine（可选） | spine-runtimes 4.2（源码子模块，按需启用） |

## 📁 项目结构

```
MiniGameTemplate/               ← Git 仓库根
├── Docs/
│   ├── Agent/                  ← AI Agent 技术文档（43 个活文档）
│   │   ├── INDEX.md            # 总索引（三路由表 GPS）
│   │   ├── EC_TDD_INDEX + 8    # Entity-Component 框架设计
│   │   ├── ADR_INDEX + 5       # 架构决策记录
│   │   ├── ATLAS_TDD_INDEX + 3 # RuntimeAtlas 系统
│   │   ├── CONV_INDEX + 4      # 编码约定
│   │   ├── OBB_TDD_INDEX + 2   # OBB 碰撞系统
│   │   ├── EDITOR_TOOLS_MANUAL_INDEX + 4  # 编辑器工具手册
│   │   ├── SO_WORKFLOWS_INDEX + 5  # 34 个 SO 配置流程
│   │   ├── ARCHITECTURE.md     # 架构总览
│   │   ├── MCP_INTEGRATION.md  # Unity MCP 集成
│   │   ├── DEBUG_PLAYBOOK.md   # 调试手册
│   │   ├── NEWGAME_GUIDE.md    # 新项目指南
│   │   └── WECHAT_INTEGRATION.md # 微信平台集成
│   └── Guide/                  ← 人类开发者文档（14 篇）
│       ├── README.md           # 文档导航首页
│       ├── GETTING_STARTED.md  # 环境搭建与首次运行
│       ├── ARCHITECTURE_OVERVIEW.md # 架构设计解读
│       ├── FRAMEWORK_MODULES.md    # 框架模块使用手册
│       ├── EXAMPLE_WALKTHROUGH.md  # 示例游戏代码解读
│       ├── BUILD_MINIGAME.md   # 微信小游戏构建指南
│       ├── FAQ.md              # 常见问题与排错
│       ├── DANMAKU_SYSTEM.md   # 弹幕系统总览
│       ├── DANMAKU_RENDERING.md    # 弹幕渲染管线
│       ├── DANMAKU_CONFIG.md   # 弹幕配置参考
│       ├── DANMAKU_COLLISION.md    # 弹幕碰撞系统
│       ├── DANMAKU_DATA.md     # 弹幕数据结构
│       ├── DANMAKU_DEMO_DECISIONS.md # 弹幕 Demo 设计决策
│       └── 微信小游戏导出到手机完整指南.md
├── UIProject/                  ← FairyGUI 编辑器工程
│   ├── assets/                 # UI 素材（Common/MainMenu/Example 三个包）
│   ├── settings/               # FairyGUI 工程设置
│   └── *.fairy                 # FairyGUI 工程文件
├── UnityProj/                  ← Unity 工程（用 Unity 2022 LTS 打开此目录）
│   ├── Assets/
│   │   ├── FairyGUI/ → Junction → ThirdParty/FairyGUI-unity/Assets/
│   │   ├── _Framework/         # 框架层（18 个模块）
│   │   │   ├── AssetSystem/    # YooAsset 资源管理封装
│   │   │   ├── AudioSystem/    # 音频管理
│   │   │   ├── DanmakuSystem/  # 弹幕系统（发射/更新/碰撞/API）
│   │   │   ├── DataSystem/     # SO 变量 + RuntimeSet + 存储 + 配置表
│   │   │   ├── DebugTools/     # 调试工具（HUD/Gizmos）
│   │   │   ├── Editor/         # 编辑器扩展（构建/校验/Inspector）
│   │   │   ├── EntitySystem/   # Entity-Component 框架
│   │   │   ├── EventSystem/    # SO 事件通道
│   │   │   ├── FSM/            # 状态机
│   │   │   ├── GameLifecycle/  # 启动流程 + 场景管理
│   │   │   ├── ObjectPool/     # 对象池
│   │   │   ├── Rendering/      # RuntimeAtlas + BatchRenderer
│   │   │   ├── Tests/          # 框架测试
│   │   │   ├── Timer/          # 计时器
│   │   │   ├── UISystem/       # FairyGUI 集成
│   │   │   ├── Utils/          # 通用工具
│   │   │   ├── VFXSystem/      # SpriteSheet VFX 系统
│   │   │   └── WeChatBridge/   # 微信 SDK 桥接
│   │   ├── _Example/           # 示例游戏（ClickGame/DanmakuDemo/VFXDemo）
│   │   ├── _Game/              # 实际游戏开发区
│   │   │   ├── Configs/        # EntityConfigSO / SkillConfigSO / BuffConfigSO 等
│   │   │   ├── FairyGUI_Export/# FairyGUI 导出目标
│   │   │   ├── Scenes/         # 游戏场景
│   │   │   └── Scripts/        # 游戏逻辑代码
│   │   ├── Packages/           # 嵌入式包（com.anklebreaker.unity-mcp 等）
│   │   └── ScriptTemplates/    # C# 脚本模板
│   ├── DataTables/             # Luban 配置表源数据
│   ├── Packages/               # Unity Package Manager 配置
│   ├── ThirdParty/             # 第三方库（FairyGUI + Spine + YooAsset）
│   └── Tools/                  # 构建 & 生成脚本
│       ├── gen_config.bat/sh   # Luban 配置表生成
│       ├── setup_fairygui.*    # FairyGUI SDK 链接脚本
│       ├── setup_spine.*       # Spine 运行时源码链接脚本（可选）
│       └── Luban/              # Luban v4.6.0 工具
├── README.md                   ← 本文件
├── CHANGELOG.md                ← 版本变更记录
├── .codebuddy/skills/          ← AI Agent Skills
│   ├── luban-config/           # Luban 配置表 SOP
│   ├── fairygui-tools/         # FairyGUI 工作流
│   ├── task-tracker/           # 跨会话任务追踪
│   └── code-review-checklist/  # 代码审查检查清单
├── .gitignore
├── .gitattributes
└── .gitmodules
```

## 🚀 快速开始

1. Clone 此项目（含 submodule）：
   ```bash
   git clone --recursive <repo-url>
   ```
2. 用 Unity 2022 LTS 打开 `UnityProj/` 目录
3. 运行 `UnityProj/Tools/setup_fairygui.bat`（Windows）或 `.sh`（macOS/Linux）
4. （可选）需要 FairyGUI 显示 Spine 时，运行 `UnityProj/Tools/setup_spine.bat` 或 `.sh`，并在 Unity 菜单启用 `FAIRYGUI_SPINE`
5. 阅读 [环境搭建与首次运行](Docs/Guide/GETTING_STARTED.md) 了解详细步骤
6. 在 `UnityProj/Assets/_Game/` 中开始开发你的游戏

## 📖 文档

### 👨‍💻 人类开发者文档（[Docs/Guide/](Docs/Guide/README.md)）

| 文档 | 说明 |
|------|------|
| [文档导航首页](Docs/Guide/README.md) | 文档总览、技术栈、阅读路线 |
| [环境搭建与首次运行](Docs/Guide/GETTING_STARTED.md) | 从 clone 到运行起来（15 分钟） |
| [架构设计解读](Docs/Guide/ARCHITECTURE_OVERVIEW.md) | SO 驱动架构、三层设计、模块依赖 |
| [框架模块使用手册](Docs/Guide/FRAMEWORK_MODULES.md) | 18 个模块的 API、用法和注意事项 |
| [示例游戏代码解读](Docs/Guide/EXAMPLE_WALKTHROUGH.md) | 逐步理解示例游戏如何串联框架 |
| [微信小游戏构建指南](Docs/Guide/BUILD_MINIGAME.md) | Bundle 构建 → 微信转换 → 验证 |
| [弹幕系统文档](Docs/Guide/DANMAKU_SYSTEM.md) | 弹幕系统架构 + 4 篇专题子文档 |
| [常见问题与排错](Docs/Guide/FAQ.md) | 常见报错、微信小游戏坑点、性能优化 |

### 🤖 AI Agent 文档（[Docs/Agent/INDEX.md](Docs/Agent/INDEX.md)）

Agent 文档采用三层索引架构：**总索引 → Domain INDEX → Detail Doc**

| 系统 | 文件数 | 说明 |
|------|--------|------|
| Entity-Component | INDEX + 8 | 纯 C# Entity 框架（零 GC）、10+ 组件 |
| ADR 决策记录 | INDEX + 5 | 架构决策历史（ADR-001~033） |
| RuntimeAtlas | INDEX + 3 | 运行时图集打包、BatchRenderer |
| 编码约定 | INDEX + 4 | 命名/编码/平台/工作流规范 |
| OBB 碰撞 | INDEX + 2 | OBB 障碍物碰撞检测 |
| 编辑器工具 | INDEX + 4 | 构建/校验/Entity/Inspector |
| SO 配置流程 | INDEX + 5 | 34 个 ScriptableObject 配置手册 |

每个框架模块目录下还有 `MODULE_README.md`，Agent 读一个文件就能上手使用该模块。

## ⚠️ 架构红线

- ❌ 禁止 `GameObject.Find()` / `FindObjectOfType()`
- ❌ 禁止在游戏逻辑中使用单例（框架内部除外）
- ❌ 禁止魔法字符串（场景名、标签等）
- ❌ 禁止跨系统直接 `GetComponent<>()` 引用
- ❌ 禁止 Unity Object 使用 `?.` 空传播运算符
- ✅ 一切跨系统通信走 SO 事件通道
- ✅ 一切共享数据存 SO 变量
- ✅ 每个 MonoBehaviour < 150 行
- ✅ Entity 系统纯 C# 对象，不绑 GameObject

运行 `Tools → MiniGame Template → Validate Architecture` 自动检测违规。

## 📋 版本历史

详见 [CHANGELOG.md](CHANGELOG.md)

## 📄 许可证

私有项目，未公开发布。
