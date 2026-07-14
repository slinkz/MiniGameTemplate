---
system: knowledge-engineering
scope: code-knowledge-map
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/AGENT_BOOTSTRAP.md, Docs/Agent/MODULE_CARDS/README.md, Docs/Agent/ADR_SCHEMA.md
---

# Code Knowledge Map

> 定位：这是 P4 的代码知识映射主索引，用来把“代码路径/符号”映射到 Module Card、Context Pack、TDD/ADR 和验证项。Agent 改代码前应先用本文做影响面分析。

## 1. 使用方式

1. 根据任务在本文找到相关代码路径或符号。
2. 读取对应 Module Card，确认模块职责和边界。
3. 读取对应 Context Pack，确认任务上下文和必读文档。
4. 读取 TDD/ADR，确认设计约束。
5. 按“修改后必验”执行验证。
6. 中大型任务使用 `templates/IMPACT_ANALYSIS_TEMPLATE.md` 输出影响面。

## 2. 映射格式

```text
代码路径/符号
  -> Module Card
  -> Context Pack
  -> TDD / Workflow
  -> ADR
  -> 修改后必验
```

## 3. ShooterGame 映射

| 代码路径/符号 | Module Card | Context Pack | TDD / Workflow | ADR | 修改后必验 |
|---------------|-------------|--------------|----------------|-----|------------|
| `_Game/Scripts/ShooterGame/Core/BattleController.cs` | `MODULE_CARDS/ShooterGame.md` | `CONTEXT_PACKS/ShooterGame_Battle.md` | `SG_TDD_02_BATTLE_SYSTEM.md`, `SG_V2_TDD_07_LIFECYCLE.md` | ADR-034, ADR-035, ADR-036 | Victory/Defeat/Retry/PauseQuit/Return；退场无残留；AppFlow 返回正确 |
| `_Game/Scripts/ShooterGame/Core/BaseLineDetector*` | `MODULE_CARDS/ShooterGame.md` | `CONTEXT_PACKS/ShooterGame_Battle.md` | `SG_TDD_02_BATTLE_SYSTEM.md` | ADR-033 | 敌机越线扣基地 HP；敌机回收；失败判定正确 |
| `_Game/Scripts/ShooterGame/Core/CameraShaker.cs` | `MODULE_CARDS/ShooterGame.md` | `CONTEXT_PACKS/ShooterGame_Battle.md` | `SG_TDD_02_BATTLE_SYSTEM.md`, `SG_V2_TDD_07_LIFECYCLE.md` | ADR-035 | Shake/StopShake；退场停止震动；场景引用非空 |
| `_Game/Scripts/ShooterGame/Core/SG_ProgressManager*` | `MODULE_CARDS/ShooterGame.md`, `MODULE_CARDS/DataSystem_SO_Luban.md` | `CONTEXT_PACKS/ShooterGame_Battle.md`, `CONTEXT_PACKS/WeChat_Build_Cloud.md` | `SG_TDD_03_LEVEL_PROGRESS.md`, `SG_TDD_06_CLOUD_SAVE.md` | ADR-034 | 关卡解锁、保存、Reload、云同步、离线/重试路径 |
| `_Game/Scripts/ShooterGame/Core/IUIControllers.cs` | `MODULE_CARDS/ShooterGame.md`, `MODULE_CARDS/UISystem_FairyGUI.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `SG_TDD_04_UI_CONTROLLERS.md` | ADR-034 | Controller 绑定、UI 数据刷新、事件不重复绑定 |
| `_Game/Scripts/ShooterGame/UI/**` | `MODULE_CARDS/UISystem_FairyGUI.md`, `MODULE_CARDS/ShooterGame.md` | `CONTEXT_PACKS/FairyGUI_UI.md`, `CONTEXT_PACKS/ShooterGame_Battle.md` | `SG_TDD_04_UI_CONTROLLERS.md`, `SG_UI_DESIGN.md` | ADR-034 | 面板 Open/Refresh/Close；Suspend/Resume；按钮流程；层级 |
| `_Game/Configs/ShooterGame/**/*.asset` | `MODULE_CARDS/ShooterGame.md`, `MODULE_CARDS/DataSystem_SO_Luban.md` | `CONTEXT_PACKS/SO_Config_Workflow.md`, `CONTEXT_PACKS/ShooterGame_Battle.md` | `SO_WORKFLOWS_02_ENTITY.md`, `SG_GAME_DESIGN.md` | ADR-033, ADR-034 | Missing Reference；SO 命名路径；关卡/技能/波次运行验证；进度保存 |
| `_Game/Scenes/Battle.unity` | `MODULE_CARDS/ShooterGame.md`, `MODULE_CARDS/AppFlow.md` | `CONTEXT_PACKS/ShooterGame_Battle.md` | `SG_TDD_01_ARCHITECTURE.md`, `SG_V2_TDD_07_LIFECYCLE.md` | ADR-034, ADR-035 | 场景引用完整；Boot->Battle；BattleLifecycleEvent 绑定；返回流程 |

## 4. EntitySystem 映射

| 代码路径/符号 | Module Card | Context Pack | TDD / Workflow | ADR | 修改后必验 |
|---------------|-------------|--------------|----------------|-----|------------|
| `_Framework/EntitySystem/Scripts/Core/Entity.cs` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md` | `EC_TDD_02_CORE_ARCH.md` | ADR-033 | 组件 O(1) 访问；Reset；无 GC |
| `_Framework/EntitySystem/Scripts/Core/EntityManager.cs` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md` | `EC_TDD_03_ENTITY_POOL.md`, `EC_TDD_04_SYSTEMS.md` | ADR-033 | Spawn/Despawn/PendingDespawn/Tick 顺序 |
| `_Framework/EntitySystem/Scripts/Core/EntityPool.cs` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md` | `EC_TDD_03_ENTITY_POOL.md` | ADR-033 | 预分配、回收、容量溢出行为、无 GC |
| `_Framework/EntitySystem/Scripts/Core/EntityEventBus.cs` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md` | `EC_TDD_02_CORE_ARCH.md` | ADR-033 | 订阅/取消、事件触发、零 GC |
| `_Framework/EntitySystem/Scripts/Core/EntitySystemBootstrap.cs` | `MODULE_CARDS/EntitySystem.md`, `MODULE_CARDS/ShooterGame.md` | `CONTEXT_PACKS/EntitySystem.md` | `EC_TDD_04_SYSTEMS.md`, `SG_V2_TDD_07_LIFECYCLE.md` | ADR-033, ADR-035 | 初始化、Tick、退场 `OnBattleCleanup`、场景引用 |
| `_Framework/EntitySystem/Scripts/Components/*.cs` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md` | `EC_TDD_05_COMPONENTS.md` | ADR-033 | 组件初始化/Reset/TickOrder；热路径无 GC |
| `_Framework/EntitySystem/Scripts/Components/Skill*` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md`, `CONTEXT_PACKS/SO_Config_Workflow.md` | `EC_TDD_05_COMPONENTS.md`, `SG_V2_TDD_06_ATTACK_SKILL.md` | ADR-033 | 普攻 Slot[0]；技能装备；CD；AimMode；SO 配置 |
| `_Framework/EntitySystem/Scripts/Components/Buff*`, `Dot*`, `Passive*` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md` | `SG_V2_TDD_03_BUFF_DOT_PASSIVE.md` | ADR-033 | Buff 叠加、持续、DOT tick、被动触发、清理 |
| `_Framework/EntitySystem/Scripts/Collision/**` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/EntitySystem.md` | `EC_TDD_04_SYSTEMS.md`, `OBB_TDD_INDEX.md` | ADR-033 | Camp 判定、碰撞冷却、TargetRegistry、底线检测关系 |
| `_Framework/EntitySystem/Scripts/Config/*SO.cs` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/SO_Config_Workflow.md` | `SO_WORKFLOWS_02_ENTITY.md`, `EC_TDD_06_CONFIG.md` | ADR-033 | CreateAssetMenu、Inspector、Validator、模板资产 |
| `_Framework/EntitySystem/Editor/**` | `MODULE_CARDS/EntitySystem.md` | `CONTEXT_PACKS/SO_Config_Workflow.md` | `EC_TDD_07_EDITOR.md`, `EDITOR_TOOLS_MANUAL_04_INSPECTORS.md` | ADR-033 | CustomEditor 条件显示、SO 校验、无破坏序列化 |

## 5. Danmaku / Rendering 映射

| 代码路径/符号 | Module Card | Context Pack | TDD / Workflow | ADR | 修改后必验 |
|---------------|-------------|--------------|----------------|-----|------------|
| `_Framework/DanmakuSystem/Scripts/DanmakuSystem.cs` | `MODULE_CARDS/DanmakuSystem.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `ARCHITECTURE.md`, `SO_WORKFLOWS_03_DANMAKU.md` | ADR-006, ADR-028, ADR-035 | Init、Update/LateUpdate、ClearAll、BattleLifecycle 注册 |
| `_Framework/DanmakuSystem/Scripts/*UpdatePipeline*` | `MODULE_CARDS/DanmakuSystem.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `ARCHITECTURE.md` Danmaku 管线 | ADR-020, ADR-028, ADR-036 | 管线顺序、碰撞事件、VFX/FloatingText 调用 |
| `_Framework/DanmakuSystem/Scripts/Data/HitboxMath.cs` | `MODULE_CARDS/DanmakuSystem.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `OBB_TDD_INDEX.md` | ADR-012 | 命中数学用例、边界条件、无分配 |
| `_Framework/DanmakuSystem/Scripts/Rendering/**` | `MODULE_CARDS/DanmakuSystem.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `ATLAS_TDD_INDEX.md` | ADR-028, ADR-031, ADR-032 | DrawCall、UV、材质关键字、Game View 可见 |
| `_Framework/Rendering/RenderBatchManager*.cs` | `MODULE_CARDS/Rendering_RuntimeAtlas.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `ATLAS_TDD_INDEX.md`, `DEBUG_PLAYBOOK.md` | ADR-002, ADR-028, ADR-032 | 顶点布局、bucket、mesh upload、shaderKeywords |
| `_Framework/Rendering/RenderVertex.cs` | `MODULE_CARDS/Rendering_RuntimeAtlas.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `DEBUG_PLAYBOOK.md` | ADR-028 | VertexAttributeDescriptor 顺序、Marshal offset、可见性 |
| `_Framework/Rendering/RuntimeAtlasSystem/**` | `MODULE_CARDS/Rendering_RuntimeAtlas.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `ATLAS_TDD_INDEX.md` | ADR-028, ADR-031 | Allocate、Page 懒建、Blit、RT 像素采样、WebGL |
| `_Framework/Rendering/FloatingText*.cs` | `MODULE_CARDS/Rendering_RuntimeAtlas.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `FLOATING_TEXT_TDD.md` | ADR-036, ADR-035 | 单飘字路径、退场清理、RBM 可见性、零 GC |
| `_Framework/VFXSystem/**` | `MODULE_CARDS/Rendering_RuntimeAtlas.md` | `CONTEXT_PACKS/Danmaku_Rendering.md` | `SO_WORKFLOWS_04_VFX_RENDER.md` | ADR-016, ADR-028 | VFX Tick/Render、Atlas、排序、清理 |
| `_Game/Configs/Danmaku/**`, `_Game/Configs/VFX/**` | `MODULE_CARDS/DanmakuSystem.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md` | `CONTEXT_PACKS/SO_Config_Workflow.md` | `SO_WORKFLOWS_03_DANMAKU.md`, `SO_WORKFLOWS_04_VFX_RENDER.md` | ADR-018, ADR-028 | SO 引用、纹理/UV、Atlas 分配、运行可见 |

## 6. AppFlow / UISystem 映射

| 代码路径/符号 | Module Card | Context Pack | TDD / Workflow | ADR | 修改后必验 |
|---------------|-------------|--------------|----------------|-----|------------|
| `_Framework/Navigation/**/*.cs` | `MODULE_CARDS/AppFlow.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `APPFLOW_TDD_INDEX.md`, `APPFLOW_TDD_01_CORE_DESIGN.md` | ADR-034 | Push/Pop/Replace/PopTo；栈状态；冷启动清栈 |
| `_Framework/UISystem/Scripts/UIManager.cs` | `MODULE_CARDS/UISystem_FairyGUI.md`, `MODULE_CARDS/AppFlow.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `APPFLOW_TDD_01_CORE_DESIGN.md`, `FRAMEWORK_MODULES_01_CORE.md` | ADR-034 | Open/Close/Refresh/Suspend/Resume；层级；遮罩 |
| `_Framework/UISystem/Scripts/IUIPanel.cs` | `MODULE_CARDS/UISystem_FairyGUI.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `APPFLOW_TDD_01_CORE_DESIGN.md` | ADR-034 | IUIPanel 实现完整；IPanelSuspendable 行为 |
| `_Game/Scripts/GameStartupFlow.cs` | `MODULE_CARDS/AppFlow.md`, `MODULE_CARDS/UISystem_FairyGUI.md` | `CONTEXT_PACKS/FairyGUI_UI.md`, `CONTEXT_PACKS/ShooterGame_Battle.md` | `APPFLOW_TDD_03_INTEGRATION.md` | ADR-034 | Binder 注册、冷启动清栈、Main 场景入口 |
| `_Game/Scripts/UI/**/*.cs` | `MODULE_CARDS/UISystem_FairyGUI.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `SG_TDD_04_UI_CONTROLLERS.md`, `SG_UI_DESIGN.md` | ADR-034 | 自动生成代码不手改；Logic 生命周期；按钮事件 |
| `UIProject/assets/SG_*/**` | `MODULE_CARDS/UISystem_FairyGUI.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `skills/fairygui-tools/**` | ADR-034 | FairyGUI 发布、包加载、组件 URL、导出代码同步 |
| `_Game/FairyGUI_Export/**` | `MODULE_CARDS/UISystem_FairyGUI.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `skills/fairygui-tools/**` | ADR-034 | Unity 资源更新、包名、Binder、Prefab/Atlas 引用 |

## 7. WeChat / Build / Cloud 映射

| 代码路径/符号 | Module Card | Context Pack | TDD / Workflow | ADR | 修改后必验 |
|---------------|-------------|--------------|----------------|-----|------------|
| `_Framework/WeChatBridge/**` | `MODULE_CARDS/WeChatBridge.md` | `CONTEXT_PACKS/WeChat_Build_Cloud.md` | `WECHAT_INTEGRATION.md` | 平台约束见 CONV | 微信开发者工具、真机 API、隐私授权 |
| `_Framework/DataSystem/**/Cloud*.cs` | `MODULE_CARDS/DataSystem_SO_Luban.md`, `MODULE_CARDS/WeChatBridge.md` | `CONTEXT_PACKS/WeChat_Build_Cloud.md` | `SG_TDD_06_CLOUD_SAVE.md` | 平台约束见 CONV | 登录、Pull、Upload、Reload、离线/冲突 |
| `CloudFunctions/**` | `MODULE_CARDS/WeChatBridge.md`, `MODULE_CARDS/DataSystem_SO_Luban.md` | `CONTEXT_PACKS/WeChat_Build_Cloud.md` | `SG_TDD_06_CLOUD_SAVE.md`, `WECHAT_INTEGRATION.md` | 平台约束见 CONV | 云函数部署、权限、返回格式、异常路径 |
| `_Framework/Editor/LocalHttpServerWindow.cs` | 待补 `EditorTools.md` | `CONTEXT_PACKS/WeChat_Build_Cloud.md` | `WECHAT_INTEGRATION.md`, `EDITOR_TOOLS_MANUAL_INDEX.md` | 平台约束见 CONV | Dev Server 根目录、CDN 环境切换、域名 |
| `UnityProj/Tools/**` | 待补 `EditorTools.md` | `CONTEXT_PACKS/WeChat_Build_Cloud.md` | `BUILD_MINIGAME.md`, `NEWGAME_GUIDE.md` | 平台约束见 CONV | Bundle/WebGL/微信转换步骤、时间戳 |
| `UnityProj/Assets/link.xml` | `MODULE_CARDS/WeChatBridge.md` | `CONTEXT_PACKS/WeChat_Build_Cloud.md` | `BUILD_MINIGAME.md` | 平台约束见 CONV | IL2CPP stripping、MissingMethodException |

## 8. Skills / Knowledge Infrastructure 映射

| 代码路径/符号 | Module Card | Context Pack | TDD / Workflow | ADR | 修改后必验 |
|---------------|-------------|--------------|----------------|-----|------------|
| `skills/fairygui-tools/**` | `MODULE_CARDS/UISystem_FairyGUI.md` | `CONTEXT_PACKS/FairyGUI_UI.md` | `skills/fairygui-tools/SKILL.md` | 无 | `skills/` 与 `.codebuddy/skills/` 同步；XML 校验 |
| `skills/luban-config/**` | `MODULE_CARDS/DataSystem_SO_Luban.md` | `CONTEXT_PACKS/SO_Config_Workflow.md` | `skills/luban-config/SKILL.md` | 无 | xlsx 生成、TablesExtension、gen_config |
| `skills/coding-standards/**` | 全局 | `AGENT_BOOTSTRAP.md` | `CONV_INDEX.md` | 多 ADR | 规则不与 CONV 冲突；同步到 `.codebuddy/skills/` |
| `skills/code-review-checklist/**` | 全局 | `AGENT_BOOTSTRAP.md` | P5/P6 后续 | 多 ADR | known-pitfalls 更新、审查清单可执行 |
| `skills/doc-maintenance/**` | 全局 | `KNOWLEDGE_ENGINEERING_ROADMAP.md` | P6 后续 | 无 | frontmatter/索引模板与 Docs 规范一致 |
| `Docs/Agent/KNOWLEDGE_*` | 全局 | `AGENT_BOOTSTRAP.md` | 知识工程路线图 | 无 | 路线图状态、索引入口、跨会话恢复 |
| `Docs/Agent/CONTEXT_PACKS/**` | 全局 | `AGENT_BOOTSTRAP.md` | P1 | 无 | 每个任务包有必读文档、代码入口、必验项 |
| `Docs/Agent/MODULE_CARDS/**` | 全局 | `AGENT_BOOTSTRAP.md` | P2 | 相关 ADR | 每张卡有职责/不负责/入口/必验 |
| `Docs/Agent/ADR_SCHEMA.md` | 全局 | `AGENT_BOOTSTRAP.md` | P3 | ADR 全局 | AppliesTo/Constraints/Verification 准确 |

## 9. 常见任务反查

| 任务 | 先读 | 代码入口 | 必验摘要 |
|------|------|----------|----------|
| 新增敌人 | `CONTEXT_PACKS/SO_Config_Workflow.md`, `MODULE_CARDS/EntitySystem.md` | `EntityConfigSO`, `Configs/ShooterGame`, Wave SO | SO Validator、刷怪、碰撞、击杀/越线 |
| 新增技能 | `CONTEXT_PACKS/EntitySystem.md` | `SkillConfigSO`, `SkillComponent`, Skill Effects | 装备、CD、AimMode、伤害、退场清理 |
| 新增 Buff/DOT | `CONTEXT_PACKS/EntitySystem.md`, `CONTEXT_PACKS/SO_Config_Workflow.md` | `BuffComponent`, `BuffConfigSO`, `DotConfigSO`, `PassiveComponent` | 叠加、持续、DOT tick、结束清理 |
| 新增关卡 | `CONTEXT_PACKS/ShooterGame_Battle.md`, `CONTEXT_PACKS/SO_Config_Workflow.md`, `MODULE_CARDS/ShooterGame.md`, `MODULE_CARDS/DataSystem_SO_Luban.md` | Level/Wave SO、`SG_ProgressManager*`、`Configs/ShooterGame` | 关卡显示、进入战斗、波次生成、胜利解锁、保存/Reload、返回流程 |
| 修改战斗退出 | `MODULE_CARDS/ShooterGame.md`, `ADR_SCHEMA.md` ADR-035 | `BattleController`, `BattleLifecycleEvent`, `IBattleCleanup` | Victory/Defeat/Retry/PauseQuit/OnDestroy，无残留 |
| 调试弹幕不可见 | `CONTEXT_PACKS/Danmaku_Rendering.md`, `DEBUG_PLAYBOOK.md` | Renderer、RBM、RuntimeAtlas | active count、bucket、RT 像素、Game View |
| 修改 UI 面板 | `CONTEXT_PACKS/FairyGUI_UI.md` | `UIProject`, `Scripts/UI`, `UIManager` | 发布、Binder、Open/Refresh/Close、AppFlow |
| 修改微信云存储 | `CONTEXT_PACKS/WeChat_Build_Cloud.md` | CloudSaveSystem、WxAuth、CloudFunctions | 登录、Pull、Upload、Reload、真机 |
| 修改 AppFlow | `MODULE_CARDS/AppFlow.md`, `ADR_SCHEMA.md` ADR-034 | `_Framework/Navigation`, `UIManager` | Push/Pop/Replace、Suspend/Resume、冷启动 |

## 10. 维护规则

- 新增核心代码路径时，应在本文补映射。
- 新增 ADR 时，应在相关路径行补 ADR 引用。
- 新增 Module Card 或 Context Pack 时，应反向更新本文。
- 如果某路径映射到“待补模块卡”，P2 后续扩展应优先补齐。
- 修改验证流程时，应同步更新“修改后必验”。
