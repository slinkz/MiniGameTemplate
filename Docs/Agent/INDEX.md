# Docs/Agent 索引

> **定位**：Agent 每次会话的 GPS。通过路由表一步定位目标文件，无需 grep 全目录。
>
> 最后更新：2026-05-19 22:10 | 文件总数：85

---

## 🎯 路由表 A：任务路由

| 我要做什么 | 读什么文件 | 备注 |
|-----------|-----------|------|
| 新建一种敌人 | SO_WORKFLOWS_02_ENTITY §EntityConfigSO | 字段 + 完整创建流程 |
| 新建一个技能 | SO_WORKFLOWS_02_ENTITY §SkillConfigSO | 技能 SO + Effect 链路 |
| 新建一个 Buff | SO_WORKFLOWS_02_ENTITY §BuffConfigSO | Buff SO + Duration/叠加 |
| 新增子弹花样 | SO_WORKFLOWS_03_DANMAKU §BulletType/Pattern | 弹幕 SO + Atlas 纹理 |
| 修改碰撞逻辑 | EC_TDD_04_SYSTEMS §Collision + OBB_TDD_INDEX | 碰撞组件 + OBB 数学 |
| 新增 ADR 决策 | ADR_INDEX → ADR_05_RECENT | 追加到最新 ADR 子文件 |
| 配置微信广告/SDK/云开发/CDN | WECHAT_INTEGRATION | 广告 ID + 云开发 + CDN 单一数据源 + Dev Server 环境切换 |
| 理解/修改云存储系统 | SG_TDD_06_CLOUD_SAVE | V3 云端权威模式（登录+云同步+CloudSaveSystem） |
| 调试渲染/性能 | DEBUG_PLAYBOOK | Profiler + DC + Atlas 排查 |
| 从零开始新项目 | NEWGAME_GUIDE | 全流程 |
| 了解全局架构 | ARCHITECTURE | 分层 + Entity 战斗层图 |
| 了解导航系统 | APPFLOW_TDD_INDEX | 栈式 FlowNode + AppFlowNavigator |
| 验收 AppFlow 导航 | APPFLOW_ACCEPTANCE_PLAN | 10 验收项 + PlayMode + 冷启动清栈（热启动恢复已禁用） |
| 查命名/编码规范 | CONV_INDEX → CONV_01~04 | 命名/编码/平台/工作流 |
| 使用编辑器工具 | EDITOR_TOOLS_MANUAL_INDEX → 01~04 | 菜单工具 + Inspector + 自动处理器 |
| 操作 Unity Editor (MCP) | MCP_INTEGRATION | 编译验证/截图/执行代码/Play Mode |
| 开发 ShooterGame | SG_GAME_DESIGN + SG_UI_DESIGN | 飞行弹幕射击游戏设计 + UI/交互设计 |
| ShooterGame V2 技能系统 | SG_GDD_INDEX → 01~06 | 技能系统 GDD v2.4（主动/被动/Buff/DOT/道具/工作流/路线图） |
| 实施 ShooterGame | SG_TDD_INDEX → 01~05 | 核心 TDD：战斗系统 + 关卡 + UI + 摇杆 |
| 实施 V2 技能系统 | SG_V2_TDD_INDEX → 01~05 | V2 TDD：敌方射击 + 技能装备 + Buff/DOT + 关卡平衡 + 工具UI |
| ShooterGame 编辑器工具 | SG_TOOLS_TDD_INDEX → 01~02 | 工具 TDD：波次编辑器 + Debug + Gizmo |
| 验收工具 P0 | SG_TOOLS_P0_ACCEPTANCE | 11 项验收步骤 + 行动指导 |
| 验收 P3 FairyGUI | SG_P3_ACCEPTANCE_PLAN | 白模包导入 + 发布 + PlayMode 验证（✅ 通过） |
| 执行 SG-P4 集成验收 | SG_P4_TASKLIST | 资产收口 + 波次编排 + 全链路验收 + 发布前检查 |
| ShooterGame 下一步 | SG_NEXT_PHASE_GUIDE | 下一阶段行动指导（工具P0→P3→P4） |
| 查 SO 配置目录 | SO_WORKFLOWS_INDEX → 01~05 | 34 个 SO 类型 + 字段 + 创建流程 |
| 理解 Tick 执行顺序 | EC_TDD_02_CORE_ARCH §3.3 | TickOrders 常量表 |
| 理解 Entity 生命周期 | EC_TDD_03_ENTITY_POOL | Spawn/Despawn/Pool 流程 |

---

## 🔗 路由表 B：代码→文档映射

| 代码路径/模式 | 对应文档 | 说明 |
|--------------|---------|------|
| `EntitySystem/Scripts/Components/*.cs` | EC_TDD_05_COMPONENTS | 组件设计 |
| `EntitySystem/Scripts/Components/Skill*` | EC_TDD_05_COMPONENTS §4.8 | 技能子系统 |
| `EntitySystem/Scripts/Components/Buff*` | EC_TDD_05_COMPONENTS §4.10 | Buff 子系统 |
| `EntitySystem/Scripts/Core/*.cs` | EC_TDD_02_CORE_ARCH | Entity/Pool/EventBus |
| `EntitySystem/Scripts/Systems/*.cs` | EC_TDD_04_SYSTEMS | EntityManager/Spawner |
| `EntitySystem/Scripts/Config/*SO.cs` | EC_TDD_06_CONFIG + SO_WORKFLOWS_02_ENTITY | SO 配置 |
| `EntitySystem/Editor/*.cs` | EDITOR_TOOLS_MANUAL_04_INSPECTORS §SkillConfigSOEditor | 自定义编辑器 |
| `EntitySystemBootstrap.cs` | EC_TDD_04_SYSTEMS §Bootstrap | 胶水层入口 |
| `Danmaku/**/*.cs` | ATLAS_TDD_INDEX + SO_WORKFLOWS_03_DANMAKU | 弹幕+渲染 |
| `RuntimeAtlas/**/*.cs` | ATLAS_TDD_INDEX | 动态图集 |
| `OBB/**/*.cs` | OBB_TDD_INDEX | OBB 碰撞 |
| `Editor/**/*.cs` | EDITOR_TOOLS_MANUAL_INDEX → 01~04 + SG_V2_TDD_05 | 编辑器工具 |
| `_Framework/Editor/LocalHttpServerWindow.cs` | WECHAT_INTEGRATION §Dev Server + EDITOR_TOOLS_MANUAL | Dev Server 一键 CDN 环境切换 |
| `*ConfigSO.cs` / `*SO.cs` | SO_WORKFLOWS_INDEX → 01~05 | SO 配置流程 |
| `_Framework/WeChatBridge/**` | WECHAT_INTEGRATION | 微信集成（广告+云开发+隐私+CDN） |
| `_Framework/WeChatBridge/Scripts/WXDataCDNHelper.cs` | WECHAT_INTEGRATION §CDN地址架构 | 运行时从 DATA_CDN 读取 CDN 地址 |
| `_Framework/WeChatBridge/Scripts/WxAuth*.cs` | SG_TDD_06 §2.3 | 微信静默登录服务 |
| `_Framework/WeChatBridge/Scripts/CloudSync*.cs` | SG_TDD_06 §3.5 | 云端进度同步服务 |
| `_Framework/DataSystem/Scripts/Persistence/Cloud*.cs` | SG_TDD_06 §4.2 | CloudSaveSystem + SharedProgressData |
| `CloudFunctions/**/*.js` | SG_TDD_06 §3.2 | 微信云函数模板 |
| `Packages/com.anklebreaker.unity-mcp/**` | MCP_INTEGRATION | Unity MCP 集成 |
| `_Game/Scripts/ShooterGame/Core/*.cs` | SG_TDD_01~02 + SG_V2_TDD_01~04 + SG_DEV_PLAN | SG 战斗核心 |
| `_Game/Scripts/ShooterGame/Config/*.cs` | SG_TDD_02~03 + SG_V2_TDD_02~03 + SG_DEV_PLAN | SG 配置 SO |
| `_Game/Scripts/ShooterGame/Progress/*.cs` | SG_TDD_03 + SG_TDD_06 + SG_V2_TDD_04 + SG_DEV_PLAN | SG 进度管理 |
| `_Game/Scripts/ShooterGame/Input/*.cs` | SG_TDD_05 + SG_DEV_PLAN | SG 输入桥接 |
| `_Game/Scripts/ShooterGame/UI/*.cs` | SG_TDD_04 + SG_V2_TDD_05 + SG_DEV_PLAN | SG UI Controllers |
| `_Game/Configs/ShooterGame/**/*.asset` | SG_P4_TASKLIST §P4.1 + SO_WORKFLOWS_02_ENTITY | SG 配置资产 |
| `_Framework/DataSystem/Scripts/Variables/Vector2Variable.cs` | SG_TDD_05 | 框架新增 SO 变量 |
| `_Framework/Navigation/**/*.cs` | APPFLOW_TDD_01_CORE_DESIGN | AppFlow 栈式导航系统（含面板 Suspend/Resume） |
| `_Framework/UISystem/Scripts/IUIPanel.cs` | APPFLOW_TDD_01_CORE_DESIGN §3.5 | IPanelSuspendable 可选接口 |
| `_Framework/UISystem/Scripts/UIManager.cs` | APPFLOW_TDD_01_CORE_DESIGN §3.5 | UIManager Suspend/Resume API |
| `_Game/Scripts/GameStartupFlow.cs` | APPFLOW_TDD_03_INTEGRATION §4.3 + APPFLOW_ACCEPTANCE_PLAN | 启动流程 + 冷启动清栈 |
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
| DamageDealer | EC_TDD_05 §4.9 附注 | 静态伤害工具类（重入保护+PendingDespawn 安全检查） |
| DamageContext | EC_TDD_05 §4.9 | 伤害传递结构体（暴击/来源/修正） |
| EntityEventBus | EC_TDD_02 §3.4 | 预分配 Delegate[16,4] 零 GC 事件总线 |
| EntityPool | EC_TDD_03 | 预分配数组+空闲栈，零 GC 对象池 |
| RuntimeAtlas | ATLAS_TDD_01 §架构 | 运行时动态纹理合批（DC≤2） |
| CampUtility | EC_TDD_04 §阵营 | 阵营判定工具类（Player/Enemy/Neutral） |
| Template_ 前缀 | CONV_01 §SO资产 | 模板 SO 资产命名约定（WF-009） |
| 变更包 | CONV_04 §归档 | 每次修改的 changes/ 归档记录 |
| SpeedModifierIds | EC_TDD_05 §4.10 | Buff by-ID 移速修正标识 |
| ISkillEffect | EC_TDD_05 §4.8 | 技能效果接口（FireBullets/AreaDamage/ApplyBuff） |
| ADR | ADR_INDEX | 架构决策记录（已接受/已废弃） |
| BattleState | SG_TDD_02 §1.1 | 战斗状态枚举（None/Intro/Playing/Victory/Defeat） |
| BaseLineDetector | SG_TDD_02 §2.2 | 底线检测器（纯 C#，扫描敌机越线扣基地 HP） |
| SG_Boot | SG_TDD_01 §9 | ShooterGame 静态启动扩展（Progress 访问点） |
| SG_ProgressManager | SG_TDD_03 §2.2 | ShooterGame 进度管理（ISaveSystem 封装） |
| IPanelSuspendable | APPFLOW_TDD_01_CORE_DESIGN §3.5 | 面板 Suspend/Resume 可选接口（OnSuspend + OnResume） |
| OwnedPanelTypes | APPFLOW_TDD_01_CORE_DESIGN §3.2 | StackEntry 跟踪每栈层面板类型列表（Suspend/Resume 用） |
| IUIControllers | SG_TDD_04 §1 | Core↔UI 解耦接口（5 个接口） |
| CloudSaveSystem | SG_TDD_06 §4.2 | V3 云端权威 ISaveSystem 实现（local+cloud 覆盖，不 merge） |
| WxAuthService | SG_TDD_06 §2.3 | 微信静默登录（cloud function auto-inject openid） |
| CloudSyncService | SG_TDD_06 §3.5 | 云端进度同步（Pull覆盖+EnqueueUpload+Retry，V3 不再 seed/merge） |
| SharedProgressData | SG_TDD_06 §3.3 + SG_V2_TDD_02 §S2.5 | V3 共享进度 DTO（version + clearedLevels + 解锁/成就/星级） |
| WXDataCDNHelper | WECHAT_INTEGRATION §CDN地址架构 | 运行时从 JS 层 DATA_CDN 读取 CDN 地址（单一数据源） |
| CDN 单一数据源 | WECHAT_INTEGRATION §CDN地址架构 | CDN 只在微信转换面板配一处，运行时 WXDataCDNHelper 读取 |
| 主动技能（6种） | SG_GDD_01 §各技能节 | 散射/穿透/追踪/范围/护盾/激光 |
| 被动技能（4种） | SG_GDD_02 §被动技能 | 暴击/闪避/弹幕扩展/速度 |
| Buff 系统 | SG_GDD_02 §Buff | 7 种 Buff + 叠加/互斥/优先级规则 |
| DOT 系统 | SG_GDD_02 §DOT | 3 种 DOT（烧灼/腐蚀/电弧）+ Tick 调度 |
| 道具掉落 | SG_GDD_03 §道具 | 4 类道具（Buff/修复/弹药/金币）+ 概率表 |
| 技能系统配置表 | SG_GDD_03 §配置表 | SkillConfig/BuffConfig/ItemConfig Luban 表结构 |
| 技能系统路线图 | SG_GDD_06 §优先级 | 5 Sprint ~67.5h 实施路线 |
| EnemyShootComponent | SG_V2_TDD_01 §S1.2 | 敌机射击组件（TickOrder=150，首次开火延迟） |
| InvincibilityModifier | SG_V2_TDD_01 §S1.3 | 无敌帧伤害修正器（Priority=-1，最高优先级） |
| DamageRedirectModifier | SG_V2_TDD_01 §S1.3 | 伤害转发修正器（Priority=0，飞机→基地） |
| SkillComponent | SG_V2_TDD_02 §S2.6 | 6 技能槽单组件+内部 SkillSlot[6]（TickOrder=200） |
| PassiveComponent | SG_V2_TDD_03 §S3.5 | 被动技能组件（3 槽 CD 周期触发，TickOrder=60） |
| BuffDamageModifier | SG_V2_TDD_03 §S3.2 | Buff 伤害修正器（DamageTaken 倍率，Priority=10） |
| BattleResultData | SG_V2_TDD_04 §S4.4 | 战斗结果值对象（星级+击杀+伤害统计快照） |
| damageSourceTag | SG_V2_TDD_04 §S4.3 | 弹丸伤害来源标记（int，NativeArray 4KB） |
| PickupSystem | SG_V2_TDD_02 §S2.7 | 道具拾取系统（碰撞检测+DropTable概率+磁吸半径） |
| DropTableSO | SG_V2_TDD_02 §S2.7 | 扁平概率掉落表 SO（无嵌套，无条件，无保底） |
| SkillUnlockManager | SG_V2_TDD_02 §S2.4 | 技能解锁管理器（关卡→技能映射+持久化） |
| EditorBulletSimulator | SG_V2_TDD_05 §S5.1 | Editor 模式弹幕模拟器（SimBullet + Handles 绘制） |
| SOConsistencyValidator | SG_V2_TDD_05 §S5.3 | 构建卡口 SO 一致性验证器（L1+L2 深度检查） |
| BattleResultCalculator | SG_V2_TDD_04 §S4.4 | 静态星级计算工具类（HP 比例→1~3 星） |
| DPSCalculatorWindow | SG_V2_TDD_04 §S4.5 | 编辑器 DPS 计算面板（裸 DPS + 被动期望 + HP 预算对照） |

---

## 📂 文件体系总览

| 前缀/系统 | INDEX | 子文件数 | 主题 |
|-----------|-------|---------|------|
| EC_TDD | EC_TDD_INDEX | 8 | Entity-Component 框架 |
| ADR | ADR_INDEX | 5 | 架构决策记录 |
| ATLAS_TDD | ATLAS_TDD_INDEX | 3 | RuntimeAtlas 动态图集 |
| CONV | CONV_INDEX | 4 | 编码/命名/平台/工作流约定 |
| OBB_TDD | OBB_TDD_INDEX | 2 | OBB 碰撞检测 |
| — | ARCHITECTURE | — | 全局架构总览 |
| APPFLOW | APPFLOW_TDD_INDEX | 5 | AppFlow 栈式导航系统 TDD（✅ Phase 1~4 + 3 轮 PK + 面板 Suspend/Resume + 冷启动清栈 v1.8） |
| — | APPFLOW_TDD_PK | — | AppFlow TDD PK 评审记录 |
| — | APPFLOW_TDD_PK2 | — | AppFlow TDD PK #2 评审记录（Unity架构师） |
| — | APPFLOW_TDD_PK3 | — | AppFlow TDD PK #3 评审记录（编辑器工具开发者） |
| SO_WORKFLOWS | SO_WORKFLOWS_INDEX | 5 | SO 配置流程指南 |
| EDITOR_TOOLS_MANUAL | EDITOR_TOOLS_MANUAL_INDEX | 4 | 编辑器工具使用手册 |
| — | MCP_INTEGRATION | — | Unity MCP 集成（Agent 操作 Unity） |
| — | DEBUG_PLAYBOOK | — | 调试手册 |
| — | NEWGAME_GUIDE | — | 新项目指南 |
| — | WECHAT_INTEGRATION | — | 微信平台集成 |
| SG | SG_GAME_DESIGN | — | ShooterGame 游戏设计文档 v2.1 |
| SG | SG_UI_DESIGN | — | ShooterGame UI/交互设计文档 v1.0 |
| SG_GDD | SG_GDD_INDEX | 6 | ShooterGame V2 技能系统 GDD v2.4 |
| SG_TDD | SG_TDD_INDEX | 6 | ShooterGame 核心技术设计文档 |
| SG_V2_TDD | SG_V2_TDD_INDEX | 5 | ShooterGame V2 技能系统 TDD（敌方射击+技能装备+Buff/DOT+关卡平衡+工具UI） |
| SG_TOOLS_TDD | SG_TOOLS_TDD_INDEX | 2 | ShooterGame 编辑器工具 TDD |
| SG_TDD_PK | — | — | SG_TDD PK 评审记录（10 问题 / 已收敛） |
| SG_TOOLS_TDD_PK | — | — | SG_TOOLS_TDD PK 评审记录（10 问题 / 已收敛） |
| SG_TDD_PK_TOOLS | — | — | SG_TDD PK 第二轮（工具开发者视角 / 10 问题 / 已收敛） |
| SG_TOOLS_TDD_PK_ARCH | — | — | SG_TOOLS_TDD PK 第二轮（架构师视角 / 10 问题 / 已收敛） |
| SG_TDD_PK_PM | — | — | SG_TDD PK 第三轮（PM 视角 / 10 问题 / 已收敛） |
| SG_TOOLS_TDD_PK_PM | — | — | SG_TOOLS_TDD PK 第三轮（PM 视角 / 10 问题 / 已收敛） |
| SG_TDD_PK_WECHAT | — | — | SG_TDD PK 微信真机视角（微信小程序开发者 vs Unity 架构师 / 11 问题 / 已收敛） |
| SG_V2_TDD_PK_R1 | — | — | V2 TDD PK-R1（Unity 架构师 vs 软件架构师 / 11 问题 / 已收敛） |
| SG_V2_TDD_PK_R2 | — | — | V2 TDD PK-R2（Unity 编辑器工具开发者 vs 软件架构师 / 15 问题 / 已收敛） |
| SG_DEV_PLAN | — | — | ShooterGame 开发计划总览（Phase/子任务/架构/决策汇总） |
| SG_NEXT_PHASE_GUIDE | — | — | ShooterGame 下一阶段行动指导（P0验收后→工具P0→P3→P4） |
| SG_P0_ACCEPTANCE_PLAN | — | — | SG-P0 验收计划（✅ PlayMode 验收通过） |
| SG_TOOLS_P0_ACCEPTANCE | — | — | 🔧 工具 P0 验收手册（✅ 验收通过） |
| SG_P3_ACCEPTANCE_PLAN | — | — | SG-P3 FairyGUI 白模包验收计划（✅ 验收通过 2026-05-06） |
| SG_P4_TASKLIST | — | — | SG-P4 集成验收任务清单（✅ 全部通过 2026-05-17） |
| APPFLOW_ACCEPTANCE_PLAN | — | — | AppFlow 导航系统验收计划（⬜ 待天命人验收） |
| SG_V2_S1_ACCEPTANCE | — | — | V2 Sprint 1 验收手册（✅ 验收通过 2026-05-19） |
| SG_V2_S2_ACCEPTANCE | — | — | V2 Sprint 2 验收手册（⬜ 待验收） |
