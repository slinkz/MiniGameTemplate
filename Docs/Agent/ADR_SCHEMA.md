---
system: knowledge-engineering
scope: executable-adr-schema
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/ADR_INDEX.md, Docs/Agent/KNOWLEDGE_ENGINEERING_ROADMAP.md
---

# 可执行 ADR Schema

> 定位：本文定义 MiniGameTemplate 的“可执行 ADR”格式。ADR 不只记录历史决策，还要让 Agent 能判断设计边界、影响范围和验证要求。

## 1. 为什么需要可执行 ADR

传统 ADR 适合记录“当时为什么这么选”，但 Agent 日常工作还需要快速回答：

- 这个决策现在还有效吗？
- 它约束哪些代码和资产？
- 我这次改动会不会违反它？
- 修改后应该怎么验证？
- 哪些旧决策已经被它替代？

可执行 ADR 的目标是把架构记忆转成可检查的工程约束。

## 2. ADR 使用优先级

当 ADR、TDD、代码和归档内容冲突时，按以下顺序判断：

1. 当前代码和 Unity 编译/运行验证。
2. 当前活跃 TDD / Module Card / Context Pack。
3. ADR 原文和本文的可执行摘要。
4. Archive / PK / 历史验收文档。

ADR 是架构约束入口，但实现状态必须通过代码和验收复核。

## 3. Schema 字段

后续新增或重写 ADR 时，建议包含以下字段。

```text
ADR-ID:
Title:
Status:
DecisionStatus:
ImplementationStatus:
AppliesTo:
Decision:
Constraints:
Supersedes:
Extends:
RelatedDocs:
RelatedCode:
Verification:
Pitfalls:
ChangeProtocol:
```

## 4. 字段说明

| 字段 | 含义 | Agent 用法 |
|------|------|------------|
| `ADR-ID` | ADR 编号 | 用于索引和引用 |
| `Title` | 决策标题 | 快速识别主题 |
| `Status` | Accepted / Superseded / Rejected / Draft | 判断是否可作为约束 |
| `DecisionStatus` | 决策是否仍有效 | 区分“决策有效”与“实现完成” |
| `ImplementationStatus` | NotStarted / Partial / Implemented / NeedsVerification | 避免把未验收内容当事实 |
| `AppliesTo` | 代码、资产、文档影响范围 | 做影响面分析 |
| `Decision` | 核心决策 | 判断设计方向 |
| `Constraints` | 必须遵守 / 禁止事项 | 编码前检查 |
| `Supersedes` | 替代的旧 ADR | 避免引用过期约束 |
| `Extends` | 扩展的 ADR | 理解决策链 |
| `RelatedDocs` | TDD、Module Card、Context Pack | 找上下文 |
| `RelatedCode` | 核心代码路径 | 找实现入口 |
| `Verification` | 编译、PlayMode、Profiler、真机等 | 修改后闭环 |
| `Pitfalls` | 已知坑 | 避免复发 |
| `ChangeProtocol` | 修改该决策前的要求 | 判断是否需要新 ADR |

## 5. 状态约定

| 状态 | 含义 |
|------|------|
| `Accepted` | 决策仍有效 |
| `Superseded` | 已被后续 ADR 替代，不再作为当前约束 |
| `Rejected` | 明确不采用 |
| `Implemented` | 已实现且文档/验收确认 |
| `NeedsVerification` | 文档显示状态可能不一致，需代码或验收确认 |
| `Partial` | 部分落地，仍有未完成项 |

## 6. Agent 编码前 ADR 检查

任何中大型改动前，Agent 应回答：

```text
1. 本次任务触碰哪些模块卡？
2. 这些模块卡引用哪些 ADR？
3. ADR 是否 Accepted，是否被 Superseded？
4. ADR 的 AppliesTo 是否包含本次代码路径？
5. 本次设计是否违反 Constraints？
6. 是否需要新增 ADR 或更新现有 ADR？
7. 修改后按 Verification 需要跑哪些检查？
```

## 7. 优先可执行 ADR 摘要

### ADR-012 阵营模型升级为通用关系模型

| 字段 | 内容 |
|------|------|
| Status | Accepted |
| DecisionStatus | Active |
| ImplementationStatus | Implemented / 需按当前碰撞用例验证细节 |
| AppliesTo | `UnityProj/Assets/_Framework/DanmakuSystem/Scripts/Data/HitboxMath.cs`, `DanmakuSystem/Scripts/Core/CollisionSolver.cs`, `EntitySystem/Scripts/Collision/**`, `EnumCamp` / Faction / Camp 判定路径 |
| Decision | 将碰撞阵营表达从二元玩家/敌人升级为通用关系模型，数据结构支持 `FactionId`、Source/Target Faction 与未来关系判断扩展；当前行为仍保持玩家/敌人最小闭环 |
| Constraints | 不允许把碰撞规则重新写死为二元阵营；新增碰撞路径必须显式处理 Camp/Faction；当前阶段不强行引入完整阵营矩阵编辑器；修改 OBB/Hitbox 数学不得改变阵营过滤语义 |
| RelatedDocs | `ADR_02_DANMAKU.md`, `OBB_TDD_INDEX.md`, `CONTEXT_PACKS/EntitySystem.md`, `CONTEXT_PACKS/Danmaku_Rendering.md`, `MODULE_CARDS/EntitySystem.md`, `MODULE_CARDS/DanmakuSystem.md` |
| RelatedCode | `HitboxMath.cs`, `CollisionSolver.cs`, `EntityCollisionSolver.cs`, `TargetRegistry`, `CampUtility`, `DanmakuEnums.cs` |
| Verification | 玩家/敌人/中立 Camp 判定；弹幕/Entity/障碍物碰撞路径；SectorVsAABB/Hitbox 边界用例；底线检测关系；热路径零 GC |
| Pitfalls | 只改数学命中但漏掉 Camp/Faction；Entity 与 Danmaku 使用不同阵营语义；新增中立/召唤物时绕过关系判断扩展点 |
| ChangeProtocol | 修改阵营模型前先读 ADR-012、Entity/Danmaku Context Pack 与 OBB TDD；若引入完整阵营矩阵、编辑器或关系表，需要新增 ADR 或扩展本 ADR |

### ADR-028 RuntimeAtlasSystem 统一管线

| 字段 | 内容 |
|------|------|
| Status | Accepted |
| DecisionStatus | Active |
| ImplementationStatus | Implemented / 需按当前代码验证细节 |
| AppliesTo | `UnityProj/Assets/_Framework/Rendering/**`, `DanmakuSystem/**`, `VFXSystem/**`, RuntimeAtlas 相关代码 |
| Decision | RuntimeAtlasSystem 是运行时统一渲染管线核心，不再只是可选优化；Bullet/VFX/Trail/Laser/飘字等路径逐步收编到统一管线 |
| Constraints | 运行时渲染路径必须考虑 Atlas/Channel/RBM；旧的“独立贴图可完全绕过 Atlas”只在 Editor 工具语境保留；DrawCall 优化不能牺牲资源描述一致性 |
| Supersedes | ADR-007、ADR-008、ADR-010 的运行时约束 |
| Extends | ADR-002、ADR-015 等渲染基础约束 |
| RelatedDocs | `ATLAS_TDD_INDEX.md`, `ARCHITECTURE.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md` |
| Verification | 验证 DrawCall、RuntimeAtlas allocation、RT 像素、UV、Game View 可见性、WebGL 兼容 |

### ADR-031 RuntimeAtlas 深化

| 字段 | 内容 |
|------|------|
| Status | Accepted |
| DecisionStatus | Active |
| ImplementationStatus | Implemented / 需按当前代码验证细节 |
| AppliesTo | RuntimeAtlas Page/Channel、Laser、Trail、Bullet/VFX 渲染路径 |
| Decision | Atlas 懒建页；Laser 条件接入 RuntimeAtlas；Trail 纹理化并进入 Atlas Channel |
| Constraints | InitChannel 不应无条件创建 Page 0；UV 滚动激光保留独立贴图 fallback；无纹理 Trail 也应通过 whiteTexture fallback 统一进 Atlas |
| Extends | ADR-028、ADR-029、ADR-030 |
| RelatedDocs | `ADR_05_RECENT.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md`, `DEBUG_PLAYBOOK.md` |
| Verification | 检查启动 RT 内存、首次 Allocate 创建页、Laser 可见性、Trail DC、WebGL 真机表现 |

### ADR-032 new Material shaderKeywords

| 字段 | 内容 |
|------|------|
| Status | Implemented |
| DecisionStatus | Active |
| ImplementationStatus | Implemented |
| AppliesTo | `RenderBatchManager`, Laser Renderer/Warning Renderer、所有运行时 new Material 代码 |
| Decision | 使用 `new Material(template)` 或动态创建材质时必须显式复制 `shaderKeywords` 和关键渲染状态，避免关键字丢失导致不可见 |
| Constraints | 不允许假设 `new Material()` 自动保留 shader keyword 状态；新增 Renderer 时必须检查材质克隆路径 |
| Extends | ADR-031 的 Laser Atlas 修复，影响 ADR-028/030 |
| RelatedDocs | `ADR_05_RECENT.md`, `DEBUG_PLAYBOOK.md` |
| Verification | Laser/Warning 可见性、材质关键字检查、Game View 截图、Runtime 材质状态检查 |

### ADR-033 Entity-Component 框架

| 字段 | 内容 |
|------|------|
| Status | Accepted |
| DecisionStatus | Active |
| ImplementationStatus | Implemented / 需按当前代码验证细节 |
| AppliesTo | `UnityProj/Assets/_Framework/EntitySystem/**`, `UnityProj/Assets/_Game/Scripts/ShooterGame/**` 中 Entity 使用方 |
| Decision | 引入品类无关的纯 C# Entity-Component 框架，支撑角色、技能、Buff、碰撞、刷怪等战斗能力 |
| Constraints | Entity 不直接绑定 GameObject；热路径零 GC；ComponentType/TickOrders 是契约；View 同步走 Bridge；业务规则不反向污染框架层 |
| RelatedDocs | `EC_TDD_INDEX.md`, `MODULE_CARDS/EntitySystem.md`, `CONTEXT_PACKS/EntitySystem.md` |
| RelatedCode | `EntitySystem/Core`, `Components`, `Systems`, `Spawner`, `Config`, `View`, `Skill` |
| Verification | Spawn/Despawn/Pool、组件 Reset、战斗 Retry/Exit、Profiler GC、SO Validator |

### ADR-034 AppFlow 栈式导航系统

| 字段 | 内容 |
|------|------|
| Status | Accepted |
| DecisionStatus | Active |
| ImplementationStatus | Implemented / v1.8 冷启动清栈为当前语义 |
| AppliesTo | `UnityProj/Assets/_Framework/Navigation/**`, `UIManager`, `GameStartupFlow`, 主界面/战斗场景流转 |
| Decision | 引入基于栈的 UI/场景导航系统，统一 Push/Pop/Replace/PopTo 与面板 Suspend/Resume |
| Constraints | 不绕过 AppFlow 硬切场景/关面板；Pop/Replace 必须处理 suspended panels；热启动恢复当前禁用，冷启动清栈 |
| RelatedDocs | `APPFLOW_TDD_INDEX.md`, `SG_V2_DEVICE_ACCEPTANCE.md` 第六部分, `MODULE_CARDS/AppFlow.md` |
| Verification | Push/Pop/PopTo/PopAll/Replace、Main->Battle->Return、冷启动清栈、面板事件不重复绑定 |

### ADR-035 战斗退场生命周期统一事件通道

| 字段 | 内容 |
|------|------|
| Status | Accepted |
| DecisionStatus | Active |
| ImplementationStatus | Implemented（代码级确认 2026-07-14；Unity 编译/真机验收本次未运行） |
| AppliesTo | `BattleController`, `BattleLifecycleEvent`, `IBattleCleanup`, Entity/Danmaku/Camera/FloatingText/Input/UI 清理路径 |
| Decision | 采用 BattleLifecycleEvent SO 事件通道和 `IBattleCleanup` 协议，让各系统自行注册退场清理，替代中央硬编码清理列表 |
| Constraints | 新增战斗期系统必须接入退场清理；BattleController 只负责触发统一事件；清理顺序通过 CleanupOrder 等协议表达 |
| RelatedDocs | `ADR_06_LIFECYCLE.md`, `SG_V2_TDD_07_LIFECYCLE.md`, `MODULE_CARDS/ShooterGame.md`, `MODULE_CARDS/EntitySystem.md` |
| Verification | 战斗胜利/失败/重试/返回后无 Entity、弹幕、VFX、飘字、Buff、输入残留；多次进入战斗不重复订阅；运行 `BattleCleanupValidator` 检查注册与场景引用 |

#### 代码确认（2026-07-14）

- `UnityProj/Assets/_Framework/BattleLifecycle/BattleLifecycleEvent.cs`：已实现 SO 事件通道，按 `CleanupOrder` 排序广播，单个 listener 异常不阻塞后续清理。
- `UnityProj/Assets/_Framework/BattleLifecycle/IBattleCleanup.cs`：已实现清理接口。
- `UnityProj/Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs`：Victory、Defeat、PauseQuit、Retry、OnDestroy 兜底路径均调用 `_onBattleEnd.Raise()`，并用 `_battleCleanupRaised` 防止重复清理。
- `UnityProj/Assets/_Framework/DanmakuSystem/Scripts/DanmakuSystem.cs`：实现 `IBattleCleanup`，`CleanupOrder => 0`，`OnBattleCleanup() => ClearAll()`。
- `UnityProj/Assets/_Game/Scripts/ShooterGame/Core/CameraShaker.cs`：实现 `IBattleCleanup`，`CleanupOrder => 50`，退场 `StopShake()`。
- `UnityProj/Assets/_Framework/EntitySystem/Scripts/Core/EntitySystemBootstrap.cs`：实现 `IBattleCleanup`，`CleanupOrder => 100`，清理 Entity、HitReaction、ViewBridge、Collision cooldown、Spawner、ConfigRegistry。
- `UnityProj/Assets/_Game/Editor/ShooterGame/BattleCleanupValidator.cs`：已提供类型级与场景实例级检查。
- `UnityProj/Assets/_Game/Scenes/Battle.unity` 与 `SG_OnBattleEnd.asset`：场景引用已绑定到同一 BattleLifecycleEvent 资产。
- TDD 中早期提到 `BattleHUDController` 清 UI 飘字；后续 ADR-036/FLOATING_TEXT_TDD 已统一到 `FloatingTextSystem`，因此 `BattleHUDController` 不实现 `IBattleCleanup` 不是缺口。

### ADR-036 飘字系统统一到 RBM 渲染管线

| 字段 | 内容 |
|------|------|
| Status | Accepted / Implemented |
| DecisionStatus | Active |
| ImplementationStatus | Implemented（文档标注 2026-06-03 已实施） |
| AppliesTo | `_Framework/Rendering/FloatingText*.cs`, Entity 命中反馈、Danmaku 碰撞反馈 |
| Decision | 消除弹幕层和 Entity 层双飘字路径，统一到 RBM 渲染管线的 FloatingTextSystem |
| Constraints | 不再新增 TextMesh/ObjectPool 型战斗飘字路径；命中反馈应走统一 FloatingTextSystem；退场 ClearAll 必须清理飘字 |
| RelatedDocs | `FLOATING_TEXT_TDD.md`, `ADR_06_LIFECYCLE.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md` |
| Verification | 同一命中只出现一套飘字；数值一致；战斗退出后飘字清空；RBM/Atlas 路径可见且零 GC |

## 8. 新增 ADR 模板

```markdown
## ADR-XXX: <Title>

### Status

Accepted / Superseded / Rejected / Draft

### DecisionStatus

Active / Superseded / Historical

### ImplementationStatus

NotStarted / Partial / Implemented / NeedsVerification

### AppliesTo

- `path/or/module/**`

### Context

为什么需要这个决策。

### Decision

做出的选择。

### Constraints

- 必须遵守的约束。
- 禁止事项。

### Consequences

收益、代价、风险。

### Supersedes / Extends

- Supersedes: ADR-XXX
- Extends: ADR-YYY

### Verification

- 编译/测试/PlayMode/Profiler/真机/Validator。

### ChangeProtocol

修改该决策前必须读哪些文档、跑哪些验证、是否需要新 ADR。
```

## 9. P3 后续动作

1. 用本文 Schema 逐步回填关键 ADR 原文。
2. 在 `ADR_INDEX.md` 中维护可执行摘要入口。
3. P4 建立 `CODE_KNOWLEDGE_MAP.md` 时引用 `AppliesTo` 和 `Verification` 字段。
