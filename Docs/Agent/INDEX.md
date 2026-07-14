# Docs/Agent 索引

> **定位**：Agent 每次会话的 GPS。通过路由表一步定位目标文件，无需 grep 全目录。
>
> 最后更新：2026-07-14 | 文件总数：118（活跃） + 88（归档）

---

## 🎯 路由表 A：任务路由

| 我要做什么 | 读什么文件 | 备注 |
|-----------|-----------|------|
| 新建一种敌人 | SO_WORKFLOWS_02_ENTITY §EntityConfigSO | 字段 + 完整创建流程 |
| 新建一个技能 | SO_WORKFLOWS_02_ENTITY §SkillConfigSO | 技能 SO + Effect 链路 |
| 新建一个 Buff | SO_WORKFLOWS_02_ENTITY §BuffConfigSO | Buff SO + Duration/叠加 |
| 新增关卡 | CONTEXT_PACKS/ShooterGame_Battle + CONTEXT_PACKS/SO_Config_Workflow + CODE_KNOWLEDGE_MAP §常见任务反查 | Level/Wave/Progress 配置 + 解锁/保存/返回验证 |
| 新增子弹花样 | SO_WORKFLOWS_03_DANMAKU §BulletType/Pattern | 弹幕 SO + Atlas 纹理 |
| 修改碰撞逻辑 | EC_TDD_04_SYSTEMS §Collision + OBB_TDD_INDEX | 碰撞组件 + OBB 数学 |
| 修改 FairyGUI 面板 | CONTEXT_PACKS/FairyGUI_UI + MODULE_CARDS/UISystem_FairyGUI | UIProject 发布 + 导出代码 + `.Logic.cs` + AppFlow 验证 |
| 新增 ADR 决策 | ADR_INDEX → ADR_05_RECENT / ADR_06_LIFECYCLE | 追加到对应 ADR 子文件 |
| 查可执行 ADR 约束 | ADR_SCHEMA + ADR_INDEX | 编码前确认 ADR 的 AppliesTo、Constraints、Verification 与 Supersedes |
| 实施退场生命周期改造 | SG_V2_TDD_07_LIFECYCLE | ADR-035 实施——SO 事件通道 + IBattleCleanup |
| 配置微信广告/SDK/云开发/CDN | WECHAT_INTEGRATION | 广告 ID + 云开发 + CDN 单一数据源 + Dev Server 环境切换 + 域名白名单 |
| 理解/修改云存储系统 | SG_TDD_06_CLOUD_SAVE | V4 云端权威+纯内存（登录+云同步+CloudSaveSystem+启动阻塞重试） |
| 调试渲染不显示 | CONTEXT_PACKS/Danmaku_Rendering + DEBUG_PLAYBOOK + MODULE_CARDS/Rendering_RuntimeAtlas | active count、bucket、RT 像素、Game View、shaderKeywords |
| 调试渲染/性能 | DEBUG_PLAYBOOK | Profiler + DC + Atlas 排查 |
| 统一飘字系统重构 | FLOATING_TEXT_TDD + ADR_06 §ADR-036 | RBM 通用飘字系统（消除双飘字 Bug） |
| 从零开始新项目 | NEWGAME_GUIDE | 全流程 |
| 了解全局架构 | ARCHITECTURE | 分层 + Entity 战斗层图 |
| 了解导航系统 | APPFLOW_TDD_INDEX | 栈式 FlowNode + AppFlowNavigator |
| 验收 AppFlow 导航 | SG_V2_DEVICE_ACCEPTANCE 第六部分 + APPFLOW_TDD_INDEX | AppFlow 验收已并入统一设备验收，旧独立验收计划已归档 |
| 查命名/编码规范 | CONV_INDEX → CONV_01~04 | 命名/编码/平台/工作流 |
| 使用编辑器工具 | MODULE_CARDS/EditorTools + EDITOR_TOOLS_MANUAL_INDEX → 01~04 | 菜单工具 + Inspector + 自动处理器 |
| 操作 Unity Editor (MCP) | MCP_INTEGRATION | 编译验证/截图/执行代码/Play Mode |
| AI 代码检索（知识图谱） | CODEGRAPH_INTEGRATION | CodeGraph MCP 安装/配置/工具优先级 |
| 推进项目知识工程 | KNOWLEDGE_ENGINEERING_ROADMAP | 跨会话主任务：Agent 上岗入口、Context Pack、模块卡、ADR 可执行化、代码映射、架构审查、维护与评估 |
| Agent 新会话上岗 | AGENT_BOOTSTRAP | 新会话事实源优先级、启动流程、Context Pack 路由、核心禁止事项与验证入口 |
| 查看模块知识卡 | MODULE_CARDS/README | 核心模块职责、边界、入口、数据流、关键 ADR、常见坑与修改后必验 |
| 查代码知识映射 | CODE_KNOWLEDGE_MAP | 代码路径到 Module Card、Context Pack、TDD、ADR 与验证项的闭环映射 |
| 做影响面分析 | templates/IMPACT_ANALYSIS_TEMPLATE | 中大型改动编码前的模块、路径、ADR、资产、热路径与验证计划模板 |
| 做架构审查 | ARCHITECTURE_REVIEW_PROTOCOL + templates/ARCH_REVIEW_TEMPLATE | 跨模块/架构敏感改动编码前的审查协议与模板 |
| 做知识维护 | KNOWLEDGE_MAINTENANCE + templates/DOC_UPDATE_CHECKLIST | 重要变更后的文档同步、变更包、索引统计与 Skill 双路径检查 |
| 运行知识评估 | KNOWLEDGE_EVALS | 10 个标准任务评估 Agent 路由、设计、影响面、踩坑规避与验证闭环 |
| 查看首批知识评估结果 | KNOWLEDGE_EVALS_RUN_2026-07-14 | 首批 10 个标准任务评分、漏项与反向修正建议 |
| 开发 ShooterGame | SG_GAME_DESIGN + SG_UI_DESIGN | 飞行弹幕射击游戏设计 + UI/交互设计 |
| ShooterGame V2 技能系统 | SG_GDD_INDEX → 01~06 | 技能系统 GDD v2.4（主动/被动/Buff/DOT/道具/工作流/路线图） |
| 实施 ShooterGame | SG_TDD_INDEX → 01~05 | 核心 TDD：战斗系统 + 关卡 + UI + 摇杆 |
| 实施 V2 技能系统 | SG_V2_TDD_INDEX → 01~05 | V2 TDD：敌方射击 + 技能装备 + Buff/DOT + 关卡平衡 + 工具UI |
| ShooterGame 编辑器工具 | SG_TOOLS_TDD_INDEX → 01~02 | 工具 TDD：波次编辑器 + Debug + Gizmo |
| 验收工具/P3/P4 | Archive/ShooterGame/ | 均已通过 ✅ → 已归档 |
| **统一设备验收（SG-V2-DEVICE）** | **SG_V2_DEVICE_ACCEPTANCE** | **65 项统一验收手册 v2.0（整合全部 Sprint + TDD + AppFlow）** |
| 查 SO 配置目录 | SO_WORKFLOWS_INDEX → 01~05 | 34 个 SO 类型 + 字段 + 创建流程 |
| 理解 Tick 执行顺序 | EC_TDD_02_CORE_ARCH §3.3 | TickOrders 常量表 |
| 理解 Entity 生命周期 | EC_TDD_03_ENTITY_POOL | Spawn/Despawn/Pool 流程 |

---

## 🔗 路由表 B：代码→文档映射

| 代码路径/模式 | 对应文档 | 说明 |
|--------------|---------|------|
| `EntitySystem/Scripts/Components/*.cs` | EC_TDD_05_COMPONENTS | 组件设计 |
| `EntitySystem/Scripts/Collision/*.cs` | EC_TDD_02 §3.5 + EC_TDD_05 §4.5 | Entity 碰撞桥接 + IsCollidable/GetEffectiveRadius |
| `DanmakuSystem/Scripts/Data/HitboxMath.cs` | OBB_TDD_INDEX | Hitbox 数学工具（含 SectorVsAABB） |
| `EntitySystem/Scripts/View/EntityViewBridge.cs` | EC_TDD_06_CONFIG §P1.9 + EC_TDD_05 §View | View 层桥接（Debug View + 正式 View 双路径） |
| `_Game/Scripts/View/SimpleEntityView.cs` | EC_TDD_06_CONFIG §P2.1 | IEntityView 实装：受击闪白 + Scale-to-Hitbox |
| `EntitySystem/Scripts/Components/Skill*` | EC_TDD_05_COMPONENTS §4.8 + SG_V2_TDD_06 | 技能子系统（含普攻升格 AimMode） |
| `EntitySystem/Scripts/Components/Buff*` | EC_TDD_05_COMPONENTS §4.10 | Buff 子系统 |
| `EntitySystem/Scripts/Core/*.cs` | EC_TDD_02_CORE_ARCH | Entity/Pool/EventBus |
| `EntitySystem/Scripts/Systems/*.cs` | EC_TDD_04_SYSTEMS | EntityManager/Spawner |
| `EntitySystem/Scripts/Config/*SO.cs` | EC_TDD_06_CONFIG + SO_WORKFLOWS_02_ENTITY | SO 配置 |
| `EntitySystem/Editor/*.cs` | EDITOR_TOOLS_MANUAL_04_INSPECTORS §SkillConfigSOEditor | 自定义编辑器 |
| `EntitySystemBootstrap.cs` | EC_TDD_04_SYSTEMS §Bootstrap + SG_V2_TDD_07 | 胶水层入口 + IBattleCleanup |
| `_Framework/BattleLifecycle/*.cs` | SG_V2_TDD_07 §3.1 | IBattleCleanup 接口 + BattleLifecycleEvent SO |
| `Danmaku/**/*.cs` | ATLAS_TDD_INDEX + SO_WORKFLOWS_03_DANMAKU | 弹幕+渲染 |
| `RuntimeAtlas/**/*.cs` | ATLAS_TDD_INDEX | 动态图集 |
| `_Framework/Rendering/FloatingText*.cs` | FLOATING_TEXT_TDD + ADR_06 §036 | 通用飘字系统（RBM 渲染） |
| `OBB/**/*.cs` | OBB_TDD_INDEX | OBB 碰撞 |
| `Editor/**/*.cs` | MODULE_CARDS/EditorTools + EDITOR_TOOLS_MANUAL_INDEX → 01~04 + SG_V2_TDD_05 | 编辑器工具 |
| `_Framework/Editor/LocalHttpServerWindow.cs` | WECHAT_INTEGRATION §Dev Server + EDITOR_TOOLS_MANUAL | Dev Server 一键 CDN 环境切换 |
| `*ConfigSO.cs` / `*SO.cs` | SO_WORKFLOWS_INDEX → 01~05 | SO 配置流程 |
| `_Framework/WeChatBridge/**` | WECHAT_INTEGRATION + SG_TDD_06 | 微信集成（广告/云开发/隐私/CDN/登录/同步） |
| `_Framework/DataSystem/**/Cloud*.cs` + `CloudFunctions/**` | SG_TDD_06 | CloudSaveSystem + 云函数模板 |
| `Packages/com.anklebreaker.unity-mcp/**` | MCP_INTEGRATION | Unity MCP 集成 |
| `.codegraph/**` + `~/.workbuddy/mcp.json` (codegraph) | CODEGRAPH_INTEGRATION | CodeGraph 知识图谱索引 |
| `_Game/Scripts/ShooterGame/Core/BattleController.cs` | SG_TDD_02 + ADR_06_LIFECYCLE + SG_V2_TDD_07 | 战斗状态 + 退场生命周期 |
| `_Game/Scripts/ShooterGame/**/*.cs` | SG_TDD_01~05 + SG_V2_TDD_01~05 | SG 全部逻辑代码 |
| `_Game/Configs/ShooterGame/**/*.asset` | SO_WORKFLOWS_02_ENTITY | SG 配置资产 |
| `_Framework/DataSystem/Scripts/Variables/Vector2Variable.cs` | SG_TDD_05 | 框架新增 SO 变量 |
| `_Framework/Navigation/**/*.cs` | APPFLOW_TDD_01_CORE_DESIGN | AppFlow 栈式导航系统（含面板 Suspend/Resume） |
| `_Framework/UISystem/Scripts/IUIPanel.cs` | APPFLOW_TDD_01_CORE_DESIGN §3.5 | IPanelSuspendable 可选接口 |
| `_Framework/UISystem/Scripts/UIManager.cs` | APPFLOW_TDD_01_CORE_DESIGN §3.5 | UIManager Suspend/Resume API |
| `_Game/Scripts/GameStartupFlow.cs` | APPFLOW_TDD_03_INTEGRATION §4.3 + SG_V2_DEVICE_ACCEPTANCE 第六部分 | 启动流程 + 冷启动清栈 |
| `_Game/Scenes/Main.unity` | APPFLOW_TDD_03_INTEGRATION §4.4 + SG_TDD_01 §4 | 非战斗宿主场景 |
| `_Game/ScriptableObjects/Config/SD_Main.asset` | APPFLOW_TDD_03_INTEGRATION §4.2 | Main 场景定义 SO |
| `UIProject/assets/SG_*/**` | SG_TDD_04 §4.2 + SG_UI_DESIGN | SG FairyGUI 白模包（4包16 XML） |

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
| FloatingTextSystem | FLOATING_TEXT_TDD | 通用飘字系统：RBM + 环形缓冲区 128，零 GC 纯 GPU 渲染 |
| CampUtility | EC_TDD_04 §阵营 | 阵营判定工具类（Player/Enemy/Neutral） |
| Template_ 前缀 | CONV_01 §SO资产 | 模板 SO 资产命名约定（WF-009） |
| 变更包 | CONV_04 §归档 | 每次修改的 changes/ 归档记录 |
| SpeedModifierIds | EC_TDD_05 §4.10 | Buff by-ID 移速修正标识 |
| ISkillEffect | EC_TDD_05 §4.8 | 技能效果接口（FireBullets/AreaDamage/ApplyBuff） |
| ADR | ADR_INDEX | 架构决策记录（已接受/已废弃） |
| BattleLifecycleEvent | ADR_06_LIFECYCLE §2 + SG_V2_TDD_07 | SO 退场事件通道（统一退场清理触发） |
| IBattleCleanup | ADR_06_LIFECYCLE §2 + SG_V2_TDD_07 | 退场清理接口（OnBattleCleanup + CleanupOrder） |
| BattleState | SG_TDD_02 §1.1 | 战斗状态枚举（None/Intro/Playing/Victory/Defeat） |
| BaseLineDetector | SG_TDD_02 §2.2 | 底线检测器（纯 C#，扫描敌机越线扣基地 HP） |
| SG_Boot | SG_TDD_01 §9 | ShooterGame 静态启动扩展（Progress 访问点） |
| SG_ProgressManager | SG_TDD_03 §2.2 | ShooterGame 进度管理（ISaveSystem 封装） |
| IPanelSuspendable | APPFLOW_TDD_01_CORE_DESIGN §3.5 | 面板 Suspend/Resume 可选接口（OnSuspend + OnResume） |
| OwnedPanelTypes | APPFLOW_TDD_01_CORE_DESIGN §3.2 | StackEntry 跟踪每栈层面板类型列表（Suspend/Resume 用） |
| IUIControllers | SG_TDD_04 §1 | Core↔UI 解耦接口（5 个接口） |
| CloudSaveSystem / WxAuth / CloudSync | SG_TDD_06 §2~4 | V4 云端权威+纯内存：启动 Pull→内存，写入 内存→Upload，永不读写本地，启动阻塞重试 |
| SharedProgressData | SG_TDD_06 §3.3 + SG_V2_TDD_02 §S2.5 | V3 共享进度 DTO（version + clearedLevels + 解锁/成就/星级） |
| CDN 单一数据源 / WXDataCDNHelper | WECHAT_INTEGRATION §CDN | CDN 只在微信转换面板配一处，运行时 JS 层读取 |
| CDN 域名白名单 | WECHAT_INTEGRATION §CDN缓存策略 ¶6 | 真机必须在微信后台加 request + downloadFile 合法域名，开发者工具 urlCheck 不绕过真机 |
| CodeGraph / codegraph_context | CODEGRAPH_INTEGRATION | 预索引代码知识图谱，Agent 代码检索首选工具 |
| 主动技能/被动/Buff/DOT/道具 | SG_GDD_01~03 | 6 主动+4 被动+7 Buff+3 DOT+4 道具 |
| 技能系统路线图 | SG_GDD_06 §优先级 | 5 Sprint ~67.5h 实施路线 |
| EnemyShootComponent / InvincibilityModifier / DamageRedirectModifier | SG_V2_TDD_01 §S1.2~S1.3 | Sprint 1：敌机射击+无敌帧+伤害转发 |
| PickupSystem 磁吸飞行 / IsAttracting | SG_V2_TDD_02 §S2.5 磁吸飞行行为 | 道具进入磁吸半径→加速飞向玩家→拾取 |
| SimpleEntityView / IEntityView | EC_TDD_06 §P2.1 | 正式 View：受击闪白 + Scale-to-Hitbox + 池复用 |
| AttackComponent（已退役） | EC_TDD_05 §4.9 | v2.8 删除，普攻统一走 SkillComponent Slot[0] |
| SkillComponent / PickupSystem / DropTableSO / SkillUnlockManager | SG_V2_TDD_02 §S2.4~S2.7 | Sprint 2：技能槽+道具+掉落+解锁 |
| PassiveComponent / BuffDamageModifier | SG_V2_TDD_03 §S3.2/S3.5 | Sprint 3：被动系统+Buff 伤害修正 |
| BattleResultData / BattleResultCalculator / damageSourceTag | SG_V2_TDD_04 §S4.3~S4.4 | Sprint 4：战果+星级+伤害统计 |
| SortieBottomSheet / SG_SortieBinder / BtnSortie | SG_V2_TDD_05 §S5.7 | Sprint 5：出战准备面板 |
| EditorBulletSimulator / SOConsistencyValidator / DPSCalculatorWindow | SG_V2_TDD_05 §S5.1~S5.3 | Sprint 5：弹幕模拟+SO 校验+DPS 面板 |

---

## 📂 文件体系总览

| 前缀/系统 | INDEX | 子文件数 | 主题 |
|-----------|-------|---------|------|
| EC_TDD | EC_TDD_INDEX | 8 | Entity-Component 框架 |
| ADR | ADR_INDEX | 6 | 架构决策记录 |
| ATLAS_TDD | ATLAS_TDD_INDEX | 3 | RuntimeAtlas 动态图集 |
| CONV | CONV_INDEX | 4 | 编码/命名/平台/工作流约定 |
| OBB_TDD | OBB_TDD_INDEX | 2 | OBB 碰撞检测 |
| — | ARCHITECTURE | — | 全局架构总览 |
| APPFLOW | APPFLOW_TDD_INDEX | 5 | AppFlow 栈式导航系统 TDD（✅ Phase 1~4 + 3 轮 PK + 面板 Suspend/Resume + 冷启动清栈 v1.8） |
| APPFLOW_ACCEPTANCE_PLAN | — | — | AppFlow 独立验收计划 → **已归档** `Archive/AppFlow/`，当前统一入口见 `SG_V2_DEVICE_ACCEPTANCE` 第六部分 |
| APPFLOW_TDD_PK* | — | 3 | AppFlow TDD PK 评审记录 → **已归档** `Archive/AppFlow/` |
| SO_WORKFLOWS | SO_WORKFLOWS_INDEX | 5 | SO 配置流程指南 |
| EDITOR_TOOLS_MANUAL | EDITOR_TOOLS_MANUAL_INDEX | 4 | 编辑器工具使用手册 |
| — | MCP_INTEGRATION | — | Unity MCP 集成（Agent 操作 Unity） |
| — | CODEGRAPH_INTEGRATION | — | CodeGraph 代码知识图谱（Agent 代码检索加速） |
| — | DEBUG_PLAYBOOK | — | 调试手册 |
| — | NEWGAME_GUIDE | — | 新项目指南 |
| — | WECHAT_INTEGRATION | — | 微信平台集成 |
| — | FLOATING_TEXT_TDD | — | 飘字系统统一重构 TDD（ADR-036） |
| SG | SG_GAME_DESIGN | — | ShooterGame 游戏设计文档 v2.1 |
| SG | SG_UI_DESIGN | — | ShooterGame UI/交互设计文档 v1.0 |
| SG_GDD | SG_GDD_INDEX | 6 | ShooterGame V2 技能系统 GDD v2.4 |
| SG_TDD | SG_TDD_INDEX | 6 | ShooterGame 核心技术设计文档 |
| SG_V2_TDD | SG_V2_TDD_INDEX | 7 | ShooterGame V2 技能系统 TDD（敌方射击+技能装备+Buff/DOT+关卡平衡+工具UI+普攻升格+退场生命周期） |
| SG_TOOLS_TDD | SG_TOOLS_TDD_INDEX | 2 | ShooterGame 编辑器工具 TDD |
| SG_TDD_PK* | — | 6 | SG_TDD PK 评审记录 → **已归档** `Archive/ShooterGame/TDD_PK/` + `Design_PK/` |
| SG_V2_TDD_PK* | — | 4 | V2 TDD PK 评审记录（R1~R4）→ **已归档** `Archive/ShooterGame/V2_TDD_PK/` |
| FLOATING_TEXT_PK* | — | 2 | 飘字 TDD PK 评审记录 → **已归档** `Archive/ShooterGame/` |
| TDD06_ACCEPTANCE_GUIDE | — | — | TDD-06 普攻升格验收指南 → **已归档** `Archive/ShooterGame/` |
| TDD07_ACCEPTANCE_GUIDE | — | — | TDD-07 退场生命周期验收指南 → **已归档** `Archive/ShooterGame/` |
| TDD05_S54_S56_ACCEPTANCE_GUIDE | — | — | S5.4~S5.6 UI 人工验收指南 → **已归档** `Archive/ShooterGame/Acceptance/`，当前统一入口见 `SG_V2_DEVICE_ACCEPTANCE` |
| DANMAKU_*.md | — | 5 | 早期人类 Danmaku Guide → **已归档** `Archive/Guide/Danmaku/`，当前入口见 `CONTEXT_PACKS/Danmaku_Rendering` 与 `MODULE_CARDS/DanmakuSystem` |
| FRAMEWORK_MODULES*.md | — | 4 | 早期人类框架模块手册 → **已归档** `Archive/Guide/FrameworkModules/`，当前入口见 `MODULE_CARDS/README` 与 `ARCHITECTURE` |
| S5_FINAL_PLAYTEST_GUIDE | — | — | Sprint 5 最终 PlayTest 指南 → **已归档** `Archive/ShooterGame/` |
| SG_DEV_PLAN | — | — | ShooterGame V1 开发计划 → **已归档** `Archive/ShooterGame/` |
| SG_NEXT_PHASE_GUIDE | — | — | ShooterGame V1 下一阶段行动指导 → **已归档** `Archive/ShooterGame/` |
| SG_P0_ACCEPTANCE_PLAN | — | — | SG-P0 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_TOOLS_P0_ACCEPTANCE | — | — | 工具 P0 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_P3_ACCEPTANCE_PLAN | — | — | SG-P3 FairyGUI 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_P4_TASKLIST | — | — | SG-P4 集成验收 → **已归档** `Archive/ShooterGame/` |
| SG_V2_S1_ACCEPTANCE | — | — | V2 Sprint 1 验收 → **已归档** `Archive/ShooterGame/Acceptance/` |
| SG_V2_S2_ACCEPTANCE | — | — | V2 Sprint 2 验收 → **已归档** `Archive/ShooterGame/` |
| SG_V2_S3_ACCEPTANCE | — | — | V2 Sprint 3 验收 → **已归档** `Archive/ShooterGame/` |
| SG_V2_S4_ACCEPTANCE | — | — | V2 Sprint 4 验收 → **已归档** `Archive/ShooterGame/` |
| **SG_V2_DEVICE_ACCEPTANCE** | — | — | **V2 统一验收手册 v2.0（65 项，⬜ 待天命人验收）— 整合 S2~S5+TDD-06/07+碰撞+PIT-050+AppFlow** |
