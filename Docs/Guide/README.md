# MiniGameTemplate 开发者文档

> 你正在阅读的是面向**人类开发者**的文档。如果你是 AI Agent，请阅读 [Docs/Agent/](../Agent/) 下的文档。

## 这是什么

MiniGameTemplate 是一个 **Unity 微信小游戏开发模板**。它提供了开箱即用的模块化框架，让你能快速启动一个小游戏项目，而不需要从零搭建基础设施。

**核心特性：**

- 🧩 **ScriptableObject 驱动架构** — 数据、事件、配置全部通过 SO 资产管理，组件间零硬编码引用
- 📦 **13 个即用模块** — 事件系统、数据管理、UI(FairyGUI)、音频、对象池、状态机、计时器、资源管理(YooAsset)、微信 SDK 桥接、弹幕系统等
- 🎯 **微信小游戏优化** — 内存管理、WebGL 约束、构建配置全部预设好
- 🛠 **编辑器工具链** — 架构验证、资源审计、SO 创建向导、一键构建
- 📝 **示例游戏** — 包含完整的"点击计数器"示例，展示所有框架用法

## 技术栈

| 项目 | 版本/选择 |
|------|-----------|
| Unity | 2022 LTS |
| 渲染管线 | Built-in Render Pipeline |
| UI 框架 | FairyGUI |
| 资源管理 | YooAsset 2.3.18（本地源码，`ThirdParty/YooAsset/`） |
| Spine（可选） | spine-runtimes 4.2（源码子模块，按需启用） |
| 配置表 | Luban |
| 目标平台 | 微信小游戏 (WebGL) |

## 文档导航

### 🚀 入门

| 文档 | 说明 | 预计阅读 |
|------|------|----------|
| [环境搭建与首次运行](GETTING_STARTED.md) | 从 clone 到在 Unity 中运行起来 | 15 分钟 |
| [示例游戏代码解读](EXAMPLE_WALKTHROUGH.md) | 逐步理解"点击计数器"如何串联所有框架模块 | 20 分钟 |

### 🏗 深入理解

| 文档 | 说明 | 预计阅读 |
|------|------|----------|
| [架构设计解读](ARCHITECTURE_OVERVIEW.md) | 为什么选 SO 驱动？三层架构如何协作？模块依赖规则 | 15 分钟 |
| [框架模块使用手册](FRAMEWORK_MODULES.md) | 13 个模块的详细 API、用法示例和注意事项 | 按需查阅 |

### ❓ 参考

| 文档 | 说明 |
|------|------|
| [常见问题与排错](FAQ.md) | 常见报错、微信小游戏坑点、性能优化建议 |
| [弹幕系统文档](DANMAKU_SYSTEM.md) | 弹幕系统架构总览 + 4 篇专题子文档 |
| [Agent 调试经验手册](../Agent/DEBUG_PLAYBOOK.md) | 本次弹幕 / RuntimeAtlas 排查沉淀出的系统化 Debug 方法论与案例复盘 |
| [Agent 文档](../Agent/) | AI 开发助手阅读的技术规范（包含编码规范、新游戏创建流程等） |

## 项目结构速览

> 📐 完整的项目目录树（含详细注释）参见 [根 README.md](../../README.md#-项目结构)。

```
MiniGameTemplate/               ← Git 仓库根
├── Docs/Agent/                ← AI Agent 阅读的技术文档
├── Docs/Guide/                ← 你正在读的人类开发者文档
├── UIProject/                 ← FairyGUI 编辑器工程
├── UnityProj/                 ← Unity 工程（用 Unity 2022 LTS 打开）
│   ├── Assets/_Framework/     ← 框架层（13 个模块）
│   ├── Assets/_Example/       ← 示例游戏
│   ├── Assets/_Game/          ← 你的游戏代码放这里
│   ├── DataTables/            ← Luban 配置表源数据
│   └── ThirdParty/            ← 第三方库
├── .codebuddy/skills/         ← AI Agent Skills
├── README.md
└── CHANGELOG.md
```

## 下一步

**如果你是第一次接触这个模板** → 先读 [环境搭建与首次运行](GETTING_STARTED.md)

**如果你想快速了解架构设计** → 直接看 [架构设计解读](ARCHITECTURE_OVERVIEW.md)

**如果你想知道某个模块怎么用** → 翻 [框架模块使用手册](FRAMEWORK_MODULES.md)
