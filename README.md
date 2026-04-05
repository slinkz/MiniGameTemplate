# MiniGameTemplate

Unity 小游戏开发模板 — 基于 ScriptableObject 驱动的模块化架构，面向微信小游戏平台。

## 🎯 设计目标

为 Agent（AI 开发助手）和人类开发者提供一个**开箱即用**的小游戏开发起点：
- **ScriptableObject 驱动**：数据、事件、配置全部通过 SO 资产管理，零硬编码引用
- **模块化自包含**：每个系统一个目录，目录内含文档，改一个模块不影响其他
- **设计师友好**：非程序员可通过 Inspector 调整游戏参数
- **Agent 友好**：每个模块有 MODULE_README.md，Agent 读一个文件就能上手

## 🏗️ 技术栈

| 项目 | 选择 |
|------|------|
| Unity 版本 | 2022 LTS |
| 渲染管线 | Built-in Render Pipeline |
| UI 框架 | FairyGUI |
| 配置表 | Luban |
| 目标平台 | 微信小游戏 |
| 资源管理 | YooAsset 2.3.x（OpenUPM） |

## 📁 项目结构

```
MiniGameTemplate/               ← Git 仓库根
├── Docs/                       ← Agent / 人类阅读的项目文档
│   ├── ARCHITECTURE.md         # 架构设计、数据流、模块依赖
│   ├── CONVENTIONS.md          # 命名规范、代码风格、禁止事项
│   ├── NEWGAME_GUIDE.md        # 新建游戏操作手册
│   ├── SO_CATALOG.md           # ScriptableObject 类型清单
│   └── WECHAT_INTEGRATION.md   # 微信 SDK 接入说明
├── UIProject/                  ← FairyGUI 编辑器工程
│   ├── assets/                 # UI 素材（图片、字体等）
│   ├── settings/               # FairyGUI 工程设置
│   └── *.fairy                 # FairyGUI 工程文件
├── UnityProj/                  ← Unity 工程（用 Unity 2022 LTS 打开此目录）
│   ├── Assets/
│   │   ├── _Framework/         # 框架层（一般不改）
│   │   │   ├── GameLifecycle/  # 启动流程 + 场景管理
│   │   │   ├── AssetSystem/    # YooAsset 资源管理封装
│   │   │   ├── EventSystem/    # SO 事件通道
│   │   │   ├── DataSystem/     # SO 变量 + RuntimeSet + 存储 + 配置表
│   │   │   ├── UISystem/       # FairyGUI 集成
│   │   │   ├── AudioSystem/    # 音频管理
│   │   │   ├── ObjectPool/     # 对象池
│   │   │   ├── FSM/            # 状态机
│   │   │   ├── Timer/          # 计时器
│   │   │   ├── WeChatBridge/   # 微信 SDK 桥接
│   │   │   ├── DebugTools/     # 调试工具
│   │   │   ├── Utils/          # 通用工具
│   │   │   └── Editor/         # 编辑器扩展
│   │   ├── _Example/           # 示例游戏
│   │   ├── _Game/              # 实际游戏开发区
│   │   │   └── FairyGUI_Export/# ← FairyGUI 导出目标目录
│   │   └── ScriptTemplates/    # C# 脚本模板
│   ├── DataTables/             # Luban 配置表源数据
│   ├── Packages/               # Unity Package Manager 配置
│   ├── ThirdParty/             # 第三方库（Git submodule）
│   └── Tools/                  # 构建 & 生成脚本
│       ├── gen_config.bat/sh   # Luban 配置表生成
│       ├── setup_fairygui.*    # FairyGUI SDK 链接脚本
│       └── Luban/              # Luban 工具说明
├── README.md                   ← 本文件
├── CHANGELOG.md                ← 版本变更记录
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
4. 阅读 `Docs/NEWGAME_GUIDE.md` 了解详细步骤
5. 在 `UnityProj/Assets/_Game/` 中开始开发你的游戏

## 📖 文档

| 文档 | 说明 |
|------|------|
| [ARCHITECTURE.md](Docs/ARCHITECTURE.md) | 架构设计、数据流、模块依赖 |
| [CONVENTIONS.md](Docs/CONVENTIONS.md) | 命名规范、代码风格、禁止事项 |
| [NEWGAME_GUIDE.md](Docs/NEWGAME_GUIDE.md) | 新建游戏操作手册 |
| [SO_CATALOG.md](Docs/SO_CATALOG.md) | ScriptableObject 类型清单 |
| [WECHAT_INTEGRATION.md](Docs/WECHAT_INTEGRATION.md) | 微信 SDK 接入说明 |

每个框架模块目录下还有 `MODULE_README.md`，阅读它即可上手使用该模块。

## ⚠️ 架构红线

- ❌ 禁止 `GameObject.Find()` / `FindObjectOfType()`
- ❌ 禁止在游戏逻辑中使用单例（框架内部除外）
- ❌ 禁止魔法字符串（场景名、标签等）
- ❌ 禁止跨系统直接 `GetComponent<>()` 引用
- ✅ 一切跨系统通信走 SO 事件通道
- ✅ 一切共享数据存 SO 变量
- ✅ 每个 MonoBehaviour < 150 行

运行 `Tools → MiniGame Template → Validate Architecture` 自动检测违规。
