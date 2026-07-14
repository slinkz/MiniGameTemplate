---
system: knowledge-engineering
scope: module-card-index
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Module Cards 索引

> 定位：模块卡是 Agent 的系统地图。Context Pack 解决“任务该读什么”，Module Card 解决“模块边界是什么、怎么安全修改”。

## 使用方式

1. 先读 `AGENT_BOOTSTRAP.md`。
2. 根据任务读对应 Context Pack。
3. 若任务触碰核心模块，读对应 Module Card。
4. 修改代码前确认“职责 / 不负责什么 / 关键 ADR / 修改后必验”。

## 当前模块卡

| 模块 | 文件 | 适用任务 |
|------|------|----------|
| ShooterGame | `ShooterGame.md` | SG 战斗、关卡、技能、UI、进度、退场 |
| EntitySystem | `EntitySystem.md` | Entity、组件、池、Tick、技能/Buff、碰撞、刷怪 |
| DanmakuSystem | `DanmakuSystem.md` | 弹丸、激光、喷雾、碰撞事件、弹幕 SO |
| Rendering_RuntimeAtlas | `Rendering_RuntimeAtlas.md` | RBM、RuntimeAtlas、VFX、飘字、DrawCall、渲染排查 |
| AppFlow | `AppFlow.md` | 栈式导航、场景流、面板 Suspend/Resume |
| UISystem_FairyGUI | `UISystem_FairyGUI.md` | UIManager、FairyGUI 包、面板生命周期、导出代码 |

## 后续扩展候选

- `DataSystem_SO_Luban.md`
- `WeChatBridge.md`
- `Audio_Asset_Timer_Pool.md`
- `EditorTools.md`
- `VFXSystem.md`