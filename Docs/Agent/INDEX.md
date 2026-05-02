# Docs/Agent 索引

> **定位**：Agent 每次会话的 GPS。通过路由表一步定位目标文件，无需 grep 全目录。
>
> 最后更新：2026-05-02 | 文件总数：43

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
| 配置微信广告/SDK | WECHAT_INTEGRATION | 广告 ID + 回调 + jslib |
| 调试渲染/性能 | DEBUG_PLAYBOOK | Profiler + DC + Atlas 排查 |
| 从零开始新项目 | NEWGAME_GUIDE | 全流程 |
| 了解全局架构 | ARCHITECTURE | 分层 + Entity 战斗层图 |
| 查命名/编码规范 | CONV_INDEX → CONV_01~04 | 命名/编码/平台/工作流 |
| 使用编辑器工具 | EDITOR_TOOLS_MANUAL_INDEX → 01~04 | 菜单工具 + Inspector + 自动处理器 |
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
| `Editor/**/*.cs` | EDITOR_TOOLS_MANUAL_INDEX → 01~04 | 编辑器工具 |
| `*ConfigSO.cs` / `*SO.cs` | SO_WORKFLOWS_INDEX → 01~05 | SO 配置流程 |
| `Plugins/WeChatSDK/**` | WECHAT_INTEGRATION | 微信集成 |

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
| SO_WORKFLOWS | SO_WORKFLOWS_INDEX | 5 | SO 配置流程指南 |
| EDITOR_TOOLS_MANUAL | EDITOR_TOOLS_MANUAL_INDEX | 4 | 编辑器工具使用手册 |
| — | DEBUG_PLAYBOOK | — | 调试手册 |
| — | NEWGAME_GUIDE | — | 新项目指南 |
| — | WECHAT_INTEGRATION | — | 微信平台集成 |
| — | AUDIT_REPORT | — | D3 审计报告（临时） |
