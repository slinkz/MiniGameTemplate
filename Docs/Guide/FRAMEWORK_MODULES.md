# 框架模块使用手册

> 按需查阅。每个模块独立成节，你只需要读用到的部分。
> 文档已拆分为三个子文件，方便定位。

## 子文件导航

| 文件 | 包含模块 | 行数 |
|------|---------|------|
| [Part 1：核心模块](FRAMEWORK_MODULES_01_CORE.md) | EventSystem · DataSystem · GameLifecycle · UISystem | ~400 |
| [Part 2：基础设施](FRAMEWORK_MODULES_02_INFRA.md) | AudioSystem · AssetSystem · Timer · ObjectPool | ~210 |
| [Part 3：工具 & 弹幕](FRAMEWORK_MODULES_03_TOOLS_DANMAKU.md) | FSM · WeChatBridge · DebugTools · Utils · Editor · DanmakuSystem | ~300 |

## 模块速查表

| # | 模块 | 位置 | 一句话说明 | 所在子文件 |
|---|------|------|-----------|-----------|
| 1 | EventSystem | `_Framework/EventSystem/` | SO 事件通道，组件间零耦合通信 | Part 1 |
| 2 | DataSystem | `_Framework/DataSystem/` | SO 变量 + RuntimeSet + 持久化 + Luban 配置表 | Part 1 |
| 3 | GameLifecycle | `_Framework/GameLifecycle/` | 启动编排 + 场景加载 | Part 1 |
| 4 | UISystem | `_Framework/UISystem/` | FairyGUI 面板管理（Extension 模式） | Part 1 |
| 5 | AudioSystem | `_Framework/AudioSystem/` | BGM/SFX + SO 驱动音量 | Part 2 |
| 6 | AssetSystem | `_Framework/AssetSystem/` | YooAsset 封装（4 种运行模式） | Part 2 |
| 7 | Timer | `_Framework/Timer/` | 不依赖 MonoBehaviour 的计时器 | Part 2 |
| 8 | ObjectPool | `_Framework/ObjectPool/` | GameObject 池化方案 | Part 2 |
| 9 | FSM | `_Framework/FSM/` | SO 驱动的有限状态机 | Part 3 |
| 10 | WeChatBridge | `_Framework/WeChatBridge/` | 微信 SDK 统一桥接层 | Part 3 |
| 11 | DebugTools | `_Framework/DebugTools/` | FPS/SO Viewer/Console | Part 3 |
| 12 | Utils | `_Framework/Utils/` | Singleton/GameLog/CoroutineRunner | Part 3 |
| 13 | Editor | `_Framework/Editor/` | 架构验证/资源审计/SO 向导/构建 | Part 3 |
| 14 | DanmakuSystem | `_Framework/DanmakuSystem/` | 纯数据弹幕（弹丸/激光/喷雾，零 GC） | Part 3 |
| 15 | EntitySystem | `_Framework/EntitySystem/` | 纯 C# Entity-Component 框架 | *见 Agent 文档* |
| 16 | VFXSystem | `_Framework/VFXSystem/` | SpriteSheet VFX 系统 | *见 Agent 文档* |
| 17 | Rendering | `_Framework/Rendering/` | RuntimeAtlas + BatchRenderer | *见 Agent 文档* |
| 18 | Tests | `_Framework/Tests/` | 框架测试 | — |

> 📐 模块 15~17（EntitySystem/VFXSystem/Rendering）的详细文档目前在 [Docs/Agent/](../Agent/INDEX.md)。
> 未来版本计划补充 Guide 级别的人类开发者文档。
