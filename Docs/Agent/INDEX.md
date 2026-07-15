# Docs/Agent 索引

> **定位**：Agent 每次会话的 GPS。通过路由表一步定位目标文件，无需 grep 全目录。
>
> 最后更新：2026-07-15 | 文件总数：160（活跃） + 90（归档）

---

## 🎯 路由表 A：任务路由

| 我要做什么 | 读什么文件 | 备注 |
|-----------|-----------|------|
| 新建一种敌人 | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY §EntityConfigSO | 字段 + 完整创建流程 |
| 设计一种新敌人 | ROLES/DESIGNER_BOOTSTRAP + DESIGN/ENEMY_DESIGN_CARDS | 敌人职责、数值、资产、SO、关卡投放与验收剧本 |
| 新建一个技能 | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY §SkillConfigSO | 技能 SO + Effect 链路 |
| 设计一个新技能 | ROLES/DESIGNER_BOOTSTRAP + DESIGN/SKILL_DESIGN_CARDS | 目标体验、CD/伤害/表现、UI/VFX/SFX、配置入口与验收 |
| 新建一个 Buff | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY §BuffConfigSO | Buff SO + Duration/叠加 |
| 设计 Buff/DOT/道具 | ROLES/DESIGNER_BOOTSTRAP + DESIGN/ITEM_BUFF_DESIGN_CARDS | ID 范围、叠加/刷新、UI、VFX、掉落与验收 |
| 新增关卡 | CONTEXT_PACKS/ShooterGame_Battle + CONTEXT_PACKS/SO_Config_Workflow + KNOWLEDGE/CODE_KNOWLEDGE_MAP §常见任务反查 | Level/Wave/Progress 配置 + 解锁/保存/返回验证 |
| 设计/调优关卡 | ROLES/DESIGNER_BOOTSTRAP + DESIGN/LEVEL_DESIGN_GUIDE + DESIGN/BALANCE_BASELINES | 关卡节奏、波次、难度锚点、调参顺序与验收 |
| 新增子弹花样 | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_03_DANMAKU §BulletType/Pattern | 弹幕 SO + Atlas 纹理 |
| 修改碰撞逻辑 | EC_TDD_04_SYSTEMS §Collision + SYSTEMS/OBB_TDD/OBB_TDD_INDEX | 碰撞组件 + OBB 数学 |
| 修改 FairyGUI 面板 | CONTEXT_PACKS/FairyGUI_UI + MODULE_CARDS/UISystem_FairyGUI | UIProject 发布 + 导出代码 + `.Logic.cs` + 强类型绑定检查 + AppFlow 验证 |
| 设计 UI 界面/组件 | ROLES/UI_AGENT_BOOTSTRAP + UI_DESIGN/README + skills/ui-designer/SKILL.md | UI token、状态矩阵、组件库、动效、文案与 FairyGUI handoff |
| 生产/接入游戏资产 | ROLES/ART_ASSET_AGENT_BOOTSTRAP + ASSET_PIPELINE/README + skills/asset-pipeline/SKILL.md | Manifest、命名路径、导入设置、SO/FairyGUI/VFX/Audio 接入与验收 |
| 新增 ADR 决策 | ADR/ADR_INDEX → ADR/ADR_05_RECENT / ADR/ADR_06_LIFECYCLE | 追加到对应 ADR 子文件 |
| 查可执行 ADR 约束 | ADR/ADR_SCHEMA + ADR/ADR_INDEX | 编码前确认 ADR 的 AppliesTo、Constraints、Verification 与 Supersedes |
| 实施退场生命周期改造 | SHOOTER_GAME/V2_TDD/SG_V2_TDD_07_LIFECYCLE | ADR-035 实施——SO 事件通道 + IBattleCleanup |
| 配置微信广告/SDK/云开发/CDN | PLATFORM/WECHAT_INTEGRATION | 广告 ID + 云开发 + CDN 单一数据源 + Dev Server 环境切换 + 域名白名单 |
| 更新微信小游戏 Unity 插件 | skills/wechat-minigame-plugin-update/SKILL.md + PLATFORM/WECHAT_INTEGRATION | 官方版本接口 + embedded package 更新 + DLL 锁处理 + MCP 编译验证 |
| 理解/修改云存储系统 | SHOOTER_GAME/TDD/SG_TDD_06_CLOUD_SAVE | V4 云端权威+纯内存（登录+云同步+CloudSaveSystem+启动阻塞重试） |
| 调试渲染不显示 | CONTEXT_PACKS/Danmaku_Rendering + DEBUG_PLAYBOOK + MODULE_CARDS/Rendering_RuntimeAtlas | active count、bucket、RT 像素、Game View、shaderKeywords |
| 调试渲染/性能 | DEBUG_PLAYBOOK | Profiler + DC + Atlas 排查 |
| 统一飘字系统重构 | SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD + ADR_06 §ADR-036 | RBM 通用飘字系统（消除双飘字 Bug） |
| 从零开始新项目 | NEWGAME_GUIDE | 全流程 |
| 了解全局架构 | ARCHITECTURE | 分层 + Entity 战斗层图 |
| 了解导航系统 | SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX | 栈式 FlowNode + AppFlowNavigator |
| 验收 AppFlow 导航 | SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE 第六部分 + SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX | AppFlow 验收已并入统一设备验收，旧独立验收计划已归档 |
| 查命名/编码规范 | SYSTEMS/CONV/CONV_INDEX → CONV_01~04 | 命名/编码/平台/工作流 |
| 查踩坑记录/已知坑 | → **[路由表 D：踩坑速查](#-路由表-d踩坑速查)** | 45 条活跃 PIT（当前最高 PIT-057）+ 12 条归档 PIT + 模块卡常见错误 + DEBUG_PLAYBOOK + ADR Pitfalls |
| 使用编辑器工具 | MODULE_CARDS/EditorTools + SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_INDEX → 01~04 | 菜单工具 + Inspector + 自动处理器 |
| 操作 Unity Editor (MCP) | TOOLS/MCP_INTEGRATION | 编译验证/截图/执行代码/Play Mode |
| AI 代码检索（知识图谱） | TOOLS/CODEGRAPH_INTEGRATION | CodeGraph MCP 安装/配置/工具优先级 |
| 推进项目知识工程 | KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP | 跨会话主任务：Agent 上岗入口、Context Pack、模块卡、ADR 可执行化、代码映射、架构审查、维护与评估 |
| 扩展策划/UI/资产职业 Agent 知识工程 | KNOWLEDGE/ROLE_AGENT_KNOWLEDGE_EXTENSION_PLAN | 评估当前缺口，并规划 DESIGN / UI_DESIGN / ASSET_PIPELINE、角色上岗入口、资产生产 SOP 与非代码 Evals |
| Agent 新会话上岗 | AGENT_BOOTSTRAP | 新会话事实源优先级、启动流程、Context Pack 路由、核心禁止事项与验证入口 |
| 查看模块知识卡 | MODULE_CARDS/README | 核心模块职责、边界、入口、数据流、关键 ADR、常见坑与修改后必验 |
| 查代码知识映射 | KNOWLEDGE/CODE_KNOWLEDGE_MAP | 代码路径到 Module Card、Context Pack、TDD、ADR 与验证项的闭环映射 |
| 做影响面分析 | templates/IMPACT_ANALYSIS_TEMPLATE | 中大型改动编码前的模块、路径、ADR、资产、热路径与验证计划模板 |
| 做架构审查 | KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL + templates/ARCH_REVIEW_TEMPLATE | 跨模块/架构敏感改动编码前的审查协议与模板 |
| 做知识维护 | KNOWLEDGE/KNOWLEDGE_MAINTENANCE + templates/DOC_UPDATE_CHECKLIST | 重要变更后的文档同步、变更包、索引统计与 Skill 双路径检查 |
| 运行知识评估 | KNOWLEDGE/KNOWLEDGE_EVALS | 10 个标准任务评估 Agent 路由、设计、影响面、踩坑规避与验证闭环 |
| 查看首批知识评估结果 | KNOWLEDGE/KNOWLEDGE_EVALS_RUN_2026-07-14 | 首批 10 个标准任务评分、漏项与反向修正建议 |
| 查看真实编码评估结果 | KNOWLEDGE/KNOWLEDGE_EVALS_REALCODE_RUN_2026-07-14 | P1-4 真实编码 Evals（3 任务，平均 8.0 分，Editor-only 通过；PlayMode 缺失） |
| 开发 ShooterGame | SHOOTER_GAME/SG_GAME_DESIGN + SHOOTER_GAME/SG_UI_DESIGN | 飞行弹幕射击游戏设计 + UI/交互设计 |
| ShooterGame V2 技能系统 | SHOOTER_GAME/GDD/SG_GDD_INDEX → 01~06 | 技能系统 GDD v2.4（主动/被动/Buff/DOT/道具/工作流/路线图） |
| 实施 ShooterGame | SHOOTER_GAME/TDD/SG_TDD_INDEX → 01~05 | 核心 TDD：战斗系统 + 关卡 + UI + 摇杆 |
| 实施 V2 技能系统 | SHOOTER_GAME/V2_TDD/SG_V2_TDD_INDEX → 01~05 | V2 TDD：敌方射击 + 技能装备 + Buff/DOT + 关卡平衡 + 工具UI |
| ShooterGame 编辑器工具 | SHOOTER_GAME/TOOLS_TDD/SG_TOOLS_TDD_INDEX → 01~02 | 工具 TDD：波次编辑器 + Debug + Gizmo |
| 验收工具/P3/P4 | Archive/ShooterGame/ | 均已通过 ✅ → 已归档 |
| **统一设备验收（SG-V2-DEVICE）** | **SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE** | **65 项统一验收手册 v2.0（整合全部 Sprint + TDD + AppFlow）** |
| 查 SO 配置目录 | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX → 01~05 | 34 个 SO 类型 + 字段 + 创建流程 |
| 理解 Tick 执行顺序 | EC_TDD_02_CORE_ARCH §3.3 | TickOrders 常量表 |
| 理解 Entity 生命周期 | EC_TDD_03_ENTITY_POOL | Spawn/Despawn/Pool 流程 |

---

## 🔗 路由表 B：代码→文档映射

| 代码路径/模式 | 对应文档 | 说明 |
|--------------|---------|------|
| `EntitySystem/Scripts/Components/*.cs` | EC_TDD_05_COMPONENTS | 组件设计 |
| `EntitySystem/Scripts/Collision/*.cs` | EC_TDD_02 §3.5 + EC_TDD_05 §4.5 | Entity 碰撞桥接 + IsCollidable/GetEffectiveRadius |
| `DanmakuSystem/Scripts/Data/HitboxMath.cs` | SYSTEMS/OBB_TDD/OBB_TDD_INDEX | Hitbox 数学工具（含 SectorVsAABB） |
| `EntitySystem/Scripts/View/EntityViewBridge.cs` | EC_TDD_06_CONFIG §P1.9 + EC_TDD_05 §View | View 层桥接（Debug View + 正式 View 双路径） |
| `_Game/Scripts/View/SimpleEntityView.cs` | EC_TDD_06_CONFIG §P2.1 | IEntityView 实装：受击闪白 + Scale-to-Hitbox |
| `EntitySystem/Scripts/Components/Skill*` | EC_TDD_05_COMPONENTS §4.8 + SHOOTER_GAME/V2_TDD/SG_V2_TDD_06 | 技能子系统（含普攻升格 AimMode） |
| `EntitySystem/Scripts/Components/Buff*` | EC_TDD_05_COMPONENTS §4.10 | Buff 子系统 |
| `EntitySystem/Scripts/Core/*.cs` | EC_TDD_02_CORE_ARCH | Entity/Pool/EventBus |
| `EntitySystem/Scripts/Systems/*.cs` | EC_TDD_04_SYSTEMS | EntityManager/Spawner |
| `EntitySystem/Scripts/Config/*SO.cs` | EC_TDD_06_CONFIG + SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY | SO 配置 |
| `EntitySystem/Editor/*.cs` | EDITOR_TOOLS_MANUAL_04_INSPECTORS §SkillConfigSOEditor | 自定义编辑器 |
| `EntitySystemBootstrap.cs` | EC_TDD_04_SYSTEMS §Bootstrap + SHOOTER_GAME/V2_TDD/SG_V2_TDD_07 | 胶水层入口 + IBattleCleanup |
| `_Framework/BattleLifecycle/*.cs` | SHOOTER_GAME/V2_TDD/SG_V2_TDD_07 §3.1 | IBattleCleanup 接口 + BattleLifecycleEvent SO |
| `Danmaku/**/*.cs` | SYSTEMS/ATLAS_TDD/ATLAS_TDD_INDEX + SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_03_DANMAKU | 弹幕+渲染 |
| `RuntimeAtlas/**/*.cs` | SYSTEMS/ATLAS_TDD/ATLAS_TDD_INDEX | 动态图集 |
| `_Framework/Rendering/FloatingText*.cs` | SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD + ADR_06 §036 | 通用飘字系统（RBM 渲染） |
| `OBB/**/*.cs` | SYSTEMS/OBB_TDD/OBB_TDD_INDEX | OBB 碰撞 |
| `Editor/**/*.cs` | MODULE_CARDS/EditorTools + SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_INDEX → 01~04 + SHOOTER_GAME/V2_TDD/SG_V2_TDD_05 | 编辑器工具 |
| `_Framework/Editor/LocalHttpServerWindow.cs` | PLATFORM/WECHAT_INTEGRATION §Dev Server + EDITOR_TOOLS_MANUAL | Dev Server 一键 CDN 环境切换 |
| `*ConfigSO.cs` / `*SO.cs` | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX → 01~05 | SO 配置流程 |
| `_Framework/WeChatBridge/**` | PLATFORM/WECHAT_INTEGRATION + SHOOTER_GAME/TDD/SG_TDD_06 | 微信集成（广告/云开发/隐私/CDN/登录/同步） |
| `_Framework/DataSystem/**/Cloud*.cs` + `CloudFunctions/**` | SHOOTER_GAME/TDD/SG_TDD_06 | CloudSaveSystem + 云函数模板 |
| `Packages/com.anklebreaker.unity-mcp/**` | TOOLS/MCP_INTEGRATION | Unity MCP 集成 |
| `.codegraph/**` + `~/.workbuddy/mcp.json` (codegraph) | TOOLS/CODEGRAPH_INTEGRATION | CodeGraph 知识图谱索引 |
| `_Game/Scripts/ShooterGame/Core/BattleController.cs` | SHOOTER_GAME/TDD/SG_TDD_02 + ADR/ADR_06_LIFECYCLE + SHOOTER_GAME/V2_TDD/SG_V2_TDD_07 | 战斗状态 + 退场生命周期 |
| `_Game/Scripts/ShooterGame/**/*.cs` | SHOOTER_GAME/TDD/SG_TDD_01~05 + SHOOTER_GAME/V2_TDD/SG_V2_TDD_01~05 | SG 全部逻辑代码 |
| `_Game/Configs/ShooterGame/**/*.asset` | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY | SG 配置资产 |
| `_Framework/DataSystem/Scripts/Variables/Vector2Variable.cs` | SHOOTER_GAME/TDD/SG_TDD_05 | 框架新增 SO 变量 |
| `_Framework/Navigation/**/*.cs` | APPFLOW_TDD_01_CORE_DESIGN | AppFlow 栈式导航系统（含面板 Suspend/Resume） |
| `_Framework/UISystem/Scripts/IUIPanel.cs` | APPFLOW_TDD_01_CORE_DESIGN §3.5 | IPanelSuspendable 可选接口 |
| `_Framework/UISystem/Scripts/UIManager.cs` | APPFLOW_TDD_01_CORE_DESIGN §3.5 | UIManager Suspend/Resume API |
| `_Game/Scripts/GameStartupFlow.cs` | APPFLOW_TDD_03_INTEGRATION §4.3 + SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE 第六部分 | 启动流程 + 冷启动清栈 |
| `_Game/Scenes/Main.unity` | APPFLOW_TDD_03_INTEGRATION §4.4 + SHOOTER_GAME/TDD/SG_TDD_01 §4 | 非战斗宿主场景 |
| `_Game/ScriptableObjects/Config/SD_Main.asset` | APPFLOW_TDD_03_INTEGRATION §4.2 | Main 场景定义 SO |
| `UIProject/assets/SG_*/**` | SHOOTER_GAME/TDD/SG_TDD_04 §4.2 + SHOOTER_GAME/SG_UI_DESIGN | SG FairyGUI 白模包（4包16 XML） |

---

## 📖 路由表 C：概念速查

| 概念/术语 | 定义位置 | 一句话 |
|-----------|---------|--------|
| ComponentType 枚举 | EC_TDD_02 §3.2 | O(1) 组件数组索引（0~15，MAX=16） |
| TickOrders | EC_TDD_02 §3.3 | 组件 Tick 执行优先级常量（Buff=50→Anim=400） |
| PendingDespawn | EC_TDD_03 §池回收 | Entity 标记待回收但本帧不立即销毁 |
| DamageDealer / DamageContext | EC_TDD_05 §4.9 | 静态伤害工具类 + 伤害传递结构体（暴击/来源/修正） |
| EntityEventBus | EC_TDD_02 §3.4 | 预分配 Delegate[16,4] 零 GC 事件总线 |
| EntityPool | EC_TDD_03 | 预分配数组+空闲栈，零 GC 对象池 |
| RuntimeAtlas | ATLAS_TDD_01 §架构 | 运行时动态纹理合批（DC≤2） |
| FloatingTextSystem | SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD | 通用飘字系统：RBM + 环形缓冲区 128，零 GC 纯 GPU 渲染 |
| CampUtility | EC_TDD_04 §阵营 | 阵营判定工具类（Player/Enemy/Neutral） |
| Template_ 前缀 | CONV_01 §SO资产 | 模板 SO 资产命名约定（WF-009） |
| 变更包 | CONV_04 §归档 | 每次修改的 changes/ 归档记录 |
| SpeedModifierIds | EC_TDD_05 §4.10 | Buff by-ID 移速修正标识 |
| ISkillEffect | EC_TDD_05 §4.8 | 技能效果接口（FireBullets/AreaDamage/ApplyBuff） |
| ADR | ADR/ADR_INDEX | 架构决策记录（已接受/已废弃） |
| BattleLifecycleEvent | ADR/ADR_06_LIFECYCLE §2 + SHOOTER_GAME/V2_TDD/SG_V2_TDD_07 | SO 退场事件通道（统一退场清理触发） |
| IBattleCleanup | ADR/ADR_06_LIFECYCLE §2 + SHOOTER_GAME/V2_TDD/SG_V2_TDD_07 | 退场清理接口（OnBattleCleanup + CleanupOrder） |
| BattleState | SHOOTER_GAME/TDD/SG_TDD_02 §1.1 | 战斗状态枚举（None/Intro/Playing/Victory/Defeat） |
| BaseLineDetector | SHOOTER_GAME/TDD/SG_TDD_02 §2.2 | 底线检测器（纯 C#，扫描敌机越线扣基地 HP） |
| SG_Boot | SHOOTER_GAME/TDD/SG_TDD_01 §9 | ShooterGame 静态启动扩展（Progress 访问点） |
| SG_ProgressManager | SHOOTER_GAME/TDD/SG_TDD_03 §2.2 | ShooterGame 进度管理（ISaveSystem 封装） |
| IPanelSuspendable | APPFLOW_TDD_01_CORE_DESIGN §3.5 | 面板 Suspend/Resume 可选接口（OnSuspend + OnResume） |
| OwnedPanelTypes | APPFLOW_TDD_01_CORE_DESIGN §3.2 | StackEntry 跟踪每栈层面板类型列表（Suspend/Resume 用） |
| IUIControllers | SHOOTER_GAME/TDD/SG_TDD_04 §1 | Core↔UI 解耦接口（5 个接口） |
| CloudSaveSystem / WxAuth / CloudSync | SHOOTER_GAME/TDD/SG_TDD_06 §2~4 | V4 云端权威+纯内存：启动 Pull→内存，写入 内存→Upload，永不读写本地，启动阻塞重试 |
| SharedProgressData | SHOOTER_GAME/TDD/SG_TDD_06 §3.3 + SHOOTER_GAME/V2_TDD/SG_V2_TDD_02 §S2.5 | V3 共享进度 DTO（version + clearedLevels + 解锁/成就/星级） |
| CDN 单一数据源 / WXDataCDNHelper | PLATFORM/WECHAT_INTEGRATION §CDN | CDN 只在微信转换面板配一处，运行时 JS 层读取 |
| CDN 域名白名单 | PLATFORM/WECHAT_INTEGRATION §CDN缓存策略 ¶6 | 真机必须在微信后台加 request + downloadFile 合法域名，开发者工具 urlCheck 不绕过真机 |
| CodeGraph / codegraph_context | TOOLS/CODEGRAPH_INTEGRATION | 预索引代码知识图谱，Agent 代码检索首选工具 |
| PIT 编号体系 | skills/code-review-checklist/references/known-pitfalls.md | 已知坑统一编号；45 条活跃记录（当前最高 PIT-057）+ 12 条归档记录 |
| 踩坑速查 | INDEX §[路由表 D](#-路由表-d踩坑速查) | 按领域/模块速查 PIT + 模块卡常见错误 + DEBUG_PLAYBOOK + ADR Pitfalls |
| 主动技能/被动/Buff/DOT/道具 | SG_GDD_01~03 | 6 主动+4 被动+7 Buff+3 DOT+4 道具 |
| 技能系统路线图 | SG_GDD_06 §优先级 | 5 Sprint ~67.5h 实施路线 |
| EnemyShootComponent / InvincibilityModifier / DamageRedirectModifier | SHOOTER_GAME/V2_TDD/SG_V2_TDD_01 §S1.2~S1.3 | Sprint 1：敌机射击+无敌帧+伤害转发 |
| PickupSystem 磁吸飞行 / IsAttracting | SHOOTER_GAME/V2_TDD/SG_V2_TDD_02 §S2.5 磁吸飞行行为 | 道具进入磁吸半径→加速飞向玩家→拾取 |
| SimpleEntityView / IEntityView | EC_TDD_06 §P2.1 | 正式 View：受击闪白 + Scale-to-Hitbox + 池复用 |
| AttackComponent（已退役） | EC_TDD_05 §4.9 | v2.8 删除，普攻统一走 SkillComponent Slot[0] |
| SkillComponent / PickupSystem / DropTableSO / SkillUnlockManager | SHOOTER_GAME/V2_TDD/SG_V2_TDD_02 §S2.4~S2.7 | Sprint 2：技能槽+道具+掉落+解锁 |
| PassiveComponent / BuffDamageModifier | SHOOTER_GAME/V2_TDD/SG_V2_TDD_03 §S3.2/S3.5 | Sprint 3：被动系统+Buff 伤害修正 |
| BattleResultData / BattleResultCalculator / damageSourceTag | SHOOTER_GAME/V2_TDD/SG_V2_TDD_04 §S4.3~S4.4 | Sprint 4：战果+星级+伤害统计 |
| SortieBottomSheet / SG_SortieBinder / BtnSortie | SHOOTER_GAME/V2_TDD/SG_V2_TDD_05 §S5.7 | Sprint 5：出战准备面板 |
| EditorBulletSimulator / SOConsistencyValidator / DPSCalculatorWindow | SHOOTER_GAME/V2_TDD/SG_V2_TDD_05 §S5.1~S5.3 | Sprint 5：弹幕模拟+SO 校验+DPS 面板 |

---

## ⚠️ 路由表 D：踩坑速查

> **定位**：编码前先看对应领域的已知坑，避免重复犯错。
> **PIT 编号唯一来源**：`skills/code-review-checklist/references/known-pitfalls.md`（45 条活跃记录，当前最高 PIT-057；归档见 `known-pitfalls-archive.md`）
> **模块级踩坑**：各 `MODULE_CARDS/*.md` 第 10 节「常见错误」
> **调试踩坑**：`DEBUG_PLAYBOOK.md`（渲染/弹幕/RuntimeAtlas 排查）
> **架构踩坑**：`ADR/ADR_SCHEMA.md` 各 ADR 的 Pitfalls 字段

### 按领域速查

| 领域 | PIT 编号 | 模块卡常见错误 | 调试/ADR |
|------|----------|---------------|----------|
| **渲染/顶点/着色器** | PIT-028（顶点字段顺序错位）、PIT-029（材质纹理未绑定）、PIT-032（类名重名 VFXRenderer）、PIT-045（LaserTypeSO 未设纹理）、PIT-053（飘字大小不一致） | Rendering_RuntimeAtlas §10（R1~R5）、DanmakuSystem §10（D3）、VFXSystem §10（V1~V2） | DEBUG_PLAYBOOK §3.1~3.5（DrawCall≠可见、顶点顺序、Blit shader、RT 像素、排查顺序）、ADR-032（shaderKeywords 丢失） |
| **生命周期/退场清理** | PIT-037（Pool.FreeAll 不清 Data → 幽灵实体）、PIT-047（unscaledDeltaTime 伪 Bug）、PIT-048（DontDestroyOnLoad 三件套）、PIT-050（退场 Raise 后必须切离 Playing） | ShooterGame §10（S1）、EntitySystem §10（E3）、DanmakuSystem §10（D4）、VFXSystem §10（V4）、AppFlow §10（A2~A3） | ADR-035（退场清理协议 + BattleCleanupValidator） |
| **碰撞/阵营/命中** | PIT-022（先推进后检查 TickTimer）、PIT-023（Pierce 单 byte 多目标覆写）、PIT-044（无敌帧在 modifier 链之前） | EntitySystem §10（E5）、DanmakuSystem §10（D2） | ADR-012（Camp/Faction 语义一致性）、SYSTEMS/OBB_TDD/OBB_TDD_INDEX |
| **UI/FairyGUI** | PIT-014/015（包路径错误）、PIT-017（包名改漏）、PIT-024（OnRefresh→OnOpen 双绑）、PIT-025（Dialog 被遮挡）、PIT-026（ClosePanel 清回调）、PIT-030/031（不同步源文件/导出）、PIT-038（CaptureTouch 丢失）、PIT-055（引用不存在组件）、PIT-057（Tween timeScale 冻结） | UISystem_FairyGUI §10（U1~U6）、AppFlow §10（A4~A5） | ADR-034（Suspend/Resume 行为） |
| **数据/SO/Luban** | PIT-019（Luban groups 空→零输出）、PIT-020（YooAsset 收集空）、PIT-041（云函数数据不同步）、PIT-042（DamageRedirect 忘清暴击）、PIT-043（HP 同步非单一源）、PIT-046（localOffset 语义错误） | DataSystem_SO_Luban §10（DS1~DS5）、EntitySystem §10（E4）、EditorTools §10（ET3） | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX、ADR-033（SO Validator） |
| **平台/微信/构建** | PIT-018（场景路径不一致）、PIT-040（MCP 编译旧缓存） | WeChatBridge §10（W1~W5）、EditorTools §10（ET4） | PLATFORM/WECHAT_INTEGRATION（CDN 域名白名单、真机验证）、BUILD_MINIGAME.md |
| **通用编码陷阱** | PIT-007（命名空间 global::）、PIT-016（场景名≠文件名）、PIT-021（Timer 僵尸回调）、PIT-027（AssetService NRE）、PIT-034（fake-null 陷阱）、PIT-039（null 参数歧义 CS0121）、PIT-049（swap-remove 忘清尾）、PIT-051（旧系统未物理删除）、PIT-052（改模型未改 Editor 导致序列化错位）、PIT-054（Agent 假标通过）、PIT-056（死代码无调用者） | 所有模块卡 §10 | SYSTEMS/CONV/CONV_INDEX（命名/编码/平台约束）、KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL |

### 按模块速查

| 模块 | PIT 编号 | 模块卡 §10 | 其他来源 |
|------|----------|-----------|----------|
| ShooterGame | PIT-037, 042, 043, 044, 047, 048, 050 | S1~S5（退场/状态机/SO/旧验收/云存） | ADR-035 |
| EntitySystem | PIT-022, 023, 034, 037, 044, 049 | E1~E5（GameObject 引用/ComponentType/初始化/SO/碰撞） | ADR-033 |
| DanmakuSystem | PIT-022, 023, 028 | D1~D5（容量/阵营/可见性/清理/SO） | DEBUG_PLAYBOOK 全文 |
| Rendering/RuntimeAtlas | PIT-028, 029, 032, 045 | R1~R5（DrawCall≠可见/顶点/Blit/RT/shaderKeywords） | DEBUG_PLAYBOOK §3.1~3.5, ADR-028/031/032 |
| VFXSystem | PIT-032, 033, 035, 036, 045, 047 | V1~V5（可见性/fallback/SO/退场/边界） | - |
| UISystem/FairyGUI | PIT-014, 015, 017, 024, 025, 026, 030, 031, 038, 055, 057 | U1~U6（自动生成/双绑/Binder/SortOrder/Suspend/发布） | ADR-034 |
| AppFlow | PIT-024, 025, 026 | A1~A5（绕过/Suspend/PopAll/热启动恢复/行为） | ADR-034 |
| WeChatBridge | PIT-018, 040, 041 | W1~W5（Stub 验证/CDN/重构建/jslib/stripping） | PLATFORM/WECHAT_INTEGRATION |
| DataSystem/SO/Luban | PIT-019, 020, 041, 042, 043, 046 | DS1~DS5（Validator/Inspector/TablesExtension/进度/权威混用） | SO_WORKFLOWS |
| EditorTools | PIT-018, 040, 052 | ET1~ET5（asmdef/确认弹窗/SO结构/CDN/批量规则） | - |
| 通用 | PIT-007, 016, 021, 027, 034, 039, 049, 051, 052, 054, 056 | - | SYSTEMS/CONV/CONV_INDEX, KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL |

### 踩坑查询决策树

```text
我要改渲染/弹幕/RuntimeAtlas
  → 先读 DEBUG_PLAYBOOK.md + ADR-028/031/032 的 Pitfalls
  → 再读 Rendering_RuntimeAtlas §10 + DanmakuSystem §10
  → 确认 PIT-028/029/032/045

我要改战斗流程/退场
  → 先读 ADR-035 Pitfalls + Constraints
  → 再读 ShooterGame §10 + EntitySystem §10 + VFXSystem §10
  → 确认 PIT-037/047/048/050

我要改 UI/FairyGUI
  → 先读 UISystem_FairyGUI §10 + AppFlow §10
  → 再读 known-pitfalls.md 中所有 CL-4/FairyGUI 相关 PIT
  → 确认 PIT-014/015/017/024/025/026/030/031/038/055/057

我要改碰撞/阵营
  → 先读 ADR-012 Pitfalls
  → 再读 EntitySystem §10 + DanmakuSystem §10
  → 确认 PIT-022/023/044

我要改数据/SO/配置
  → 先读 DataSystem_SO_Luban §10
  → 再读 SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX
  → 确认 PIT-019/020/041/042/043/046

我不确定属于哪个领域
  → 先看 known-pitfalls.md 全部活跃 PIT（当前最高 PIT-057）
  → 再按领域速查表定位
```

---

## 📂 文件体系总览

| 前缀/系统 | INDEX | 子文件数 | 主题 |
|-----------|-------|---------|------|
| EC_TDD | SYSTEMS/EC_TDD/EC_TDD_INDEX | 8 | Entity-Component 框架 |
| ADR | ADR/ADR_INDEX | 6 | 架构决策记录 |
| ATLAS_TDD | SYSTEMS/ATLAS_TDD/ATLAS_TDD_INDEX | 3 | RuntimeAtlas 动态图集 |
| CONV | SYSTEMS/CONV/CONV_INDEX | 4 | 编码/命名/平台/工作流约定 |
| OBB_TDD | SYSTEMS/OBB_TDD/OBB_TDD_INDEX | 2 | OBB 碰撞检测 |
| — | ARCHITECTURE | — | 全局架构总览 |
| APPFLOW | SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX | 5 | AppFlow 栈式导航系统 TDD（✅ Phase 1~4 + 3 轮 PK + 面板 Suspend/Resume + 冷启动清栈 v1.8） |
| APPFLOW_ACCEPTANCE_PLAN | — | — | AppFlow 独立验收计划 → **已归档** `Archive/AppFlow/`，当前统一入口见 `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE` 第六部分 |
| APPFLOW_TDD_PK* | — | 3 | AppFlow TDD PK 评审记录 → **已归档** `Archive/AppFlow/` |
| SO_WORKFLOWS | SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX | 5 | SO 配置流程指南 |
| EDITOR_TOOLS_MANUAL | SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_INDEX | 4 | 编辑器工具使用手册 |
| — | TOOLS/MCP_INTEGRATION | — | Unity MCP 集成（Agent 操作 Unity） |
| — | TOOLS/CODEGRAPH_INTEGRATION | — | CodeGraph 代码知识图谱（Agent 代码检索加速） |
| — | DEBUG_PLAYBOOK | — | 调试手册 |
| — | NEWGAME_GUIDE | — | 新项目指南 |
| — | PLATFORM/WECHAT_INTEGRATION | — | 微信平台集成 |
| — | SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD | — | 飘字系统统一重构 TDD（ADR-036） |
| KNOWLEDGE/ROLE_AGENT_KNOWLEDGE_EXTENSION_PLAN | — | — | 策划/UI/美术资产职业 Agent 知识工程扩展方案（P9 提案） |
| DESIGN | DESIGN/README | 9 | 策划 Agent 设计卡片：支柱、玩家动线、关卡、平衡、技能、敌人、Buff/道具、经济 |
| UI_DESIGN | UI_DESIGN/README | 7 | UI Agent 设计系统：token、组件库、屏幕卡、动效、文案、FairyGUI 交接 |
| ASSET_PIPELINE | ASSET_PIPELINE/README | 10 | 资产 Agent 管线：Manifest、命名、sprite、VFX、UI icon、audio、font、导入与验收 |
| SG | SHOOTER_GAME/SG_GAME_DESIGN | — | ShooterGame 游戏设计文档 v2.1 |
| SG | SHOOTER_GAME/SG_UI_DESIGN | — | ShooterGame UI/交互设计文档 v1.0 |
| SG_GDD | SHOOTER_GAME/GDD/SG_GDD_INDEX | 6 | ShooterGame V2 技能系统 GDD v2.4 |
| SG_TDD | SHOOTER_GAME/TDD/SG_TDD_INDEX | 6 | ShooterGame 核心技术设计文档 |
| SG_V2_TDD | SHOOTER_GAME/V2_TDD/SG_V2_TDD_INDEX | 7 | ShooterGame V2 技能系统 TDD（敌方射击+技能装备+Buff/DOT+关卡平衡+工具UI+普攻升格+退场生命周期） |
| SG_TOOLS_TDD | SHOOTER_GAME/TOOLS_TDD/SG_TOOLS_TDD_INDEX | 2 | ShooterGame 编辑器工具 TDD |
| SG_TDD_PK* | — | 6 | SG_TDD PK 评审记录 → **已归档** `Archive/ShooterGame/TDD_PK/` + `Design_PK/` |
| SG_V2_TDD_PK* | — | 4 | V2 TDD PK 评审记录（R1~R4）→ **已归档** `Archive/ShooterGame/V2_TDD_PK/` |
| FLOATING_TEXT_PK* | — | 2 | 飘字 TDD PK 评审记录 → **已归档** `Archive/ShooterGame/` |
| TDD06_ACCEPTANCE_GUIDE | — | — | TDD-06 普攻升格验收指南 → **已归档** `Archive/ShooterGame/` |
| TDD07_ACCEPTANCE_GUIDE | — | — | TDD-07 退场生命周期验收指南 → **已归档** `Archive/ShooterGame/` |
| TDD05_S54_S56_ACCEPTANCE_GUIDE | — | — | S5.4~S5.6 UI 人工验收指南 → **已归档** `Archive/ShooterGame/Acceptance/`，当前统一入口见 `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE` |
| DANMAKU_*.md | — | 5 | 早期人类 Danmaku Guide → **已归档** `Archive/Guide/Danmaku/`，当前入口见 `CONTEXT_PACKS/Danmaku_Rendering` 与 `MODULE_CARDS/DanmakuSystem` |
| FRAMEWORK_MODULES*.md | — | 4 | 早期人类框架模块手册 → **已归档** `Archive/Guide/FrameworkModules/`，当前入口见 `MODULE_CARDS/README` 与 `ARCHITECTURE` |
| S5_FINAL_PLAYTEST_GUIDE | — | — | Sprint 5 最终 PlayTest 指南 → **已归档** `Archive/ShooterGame/` |
| SG_DEV_PLAN | — | — | ShooterGame V1 开发计划 → **已归档** `Archive/ShooterGame/` |
| ShooterGame-Design | — | — | 父目录早期游戏设计稿 → **已归档** `Archive/ShooterGame/Design/`，当前入口见 `SHOOTER_GAME/SG_GAME_DESIGN`, `SHOOTER_GAME/GDD/SG_GDD_INDEX` |
| ShooterGame-UI-Design | — | — | 父目录早期 UI/交互设计稿 → **已归档** `Archive/ShooterGame/Design/`，当前入口见 `SHOOTER_GAME/SG_UI_DESIGN`, `CONTEXT_PACKS/FairyGUI_UI` |
| SG_NEXT_PHASE_GUIDE | — | — | ShooterGame V1 下一阶段行动指导 → **已归档** `Archive/ShooterGame/` |
| SG_P0_ACCEPTANCE_PLAN | — | — | SG-P0 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_TOOLS_P0_ACCEPTANCE | — | — | 工具 P0 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_P3_ACCEPTANCE_PLAN | — | — | SG-P3 FairyGUI 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_P4_TASKLIST | — | — | SG-P4 集成验收 → **已归档** `Archive/ShooterGame/` |
| SG_V2_S1_ACCEPTANCE | — | — | V2 Sprint 1 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_V2_S2_ACCEPTANCE | — | — | V2 Sprint 2 验收 → **已归档** `Archive/ShooterGame/` |
| SG_V2_S3_ACCEPTANCE | — | — | V2 Sprint 3 验收 → **已归档** `Archive/ShooterGame/` |
| SG_V2_S4_ACCEPTANCE | — | — | V2 Sprint 4 验收 → **已归档** `Archive/ShooterGame/` |
| **SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE** | — | — | **V2 统一验收手册 v2.0（65 项，⬜ 待天命人验收）— 整合 S2~S5+TDD-06/07+碰撞+PIT-050+AppFlow** |
