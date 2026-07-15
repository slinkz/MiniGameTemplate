---
system: entity-component
scope: workflow-backlog
last_verified: 2026-05-02
depends_on: [EC_TDD_05_COMPONENTS, EC_TDD_06_CONFIG]
related_code: Assets/_Framework/EntitySystem/**
---

## 十、策划工作流

> **v2.2 新增（PK R1 GD-003 产物）**。
> **v2.6 更新（WF-001~011）**：新增 10.0 前置条件、依赖关系图、必填标注、Bootstrap 步骤。

### 10.0 前置条件（v2.6 新增，WF-001）

> ⚠️ **场景中必须有一个 EntitySystemBootstrap 组件。** 如缺失，Play Mode 不会有任何 Entity 生成。

```
1. 在场景中创建空 GO（命名建议：_EntitySystem）
2. 挂上 EntitySystemBootstrap 组件
3. 将 Debug View 的 PoolDefinition 拖入 DebugViewPool 字段
4. 完成——Bootstrap 会在 Awake 时自动创建 EntityManager/ViewBridge/Spawner 并发现场景中的 SpawnPoint
```

### 10.1 创建新敌兵（端到端流程）

> **v2.4 更新**：新增 AI 行为 + 攻击 + 特效配置步骤。
> **v2.6 更新（WF-003/008/009/010）**：类型修正 + 依赖图 + 必填标注 + 推荐右键菜单。

**依赖关系图**（v2.6 WF-008，从下到上创建）：
```
创建顺序（从下到上）：
┌─────────────────────────────────────────────┐
│ EntitySpawnWaveSO                            │ ← 步骤 4：编排关卡波次
│   └→ SpawnGroup.EntityConfigSO              │
├─────────────────────────────────────────────┤
│ EntityConfigSO                              │ ← 步骤 2-3：创建 Entity 配置
│   ├→ AIBehaviorSO                           │
│   ├→ BulletTypeSO (AttackBulletType)        │
│   └→ PoolDefinition (SpawnEffect/HitEffect) │
├─────────────────────────────────────────────┤
│ AIBehaviorSO / BulletTypeSO / PoolDefinition│ ← 步骤 1：底层资产（可复用已有）
└─────────────────────────────────────────────┘
```

```
1. 创建 AI 行为配置（可选，可复用已有的）：
   - 右键 Assets/_Game/Configs/AI/ → Create → Entity/AIBehavior（推荐右键菜单，WF-010）
   - 按优先级配置条件-动作表：
     Entry 0: Condition=TargetInRange(3.0), Action=Attack
     Entry 1: Condition=TargetInRange(8.0), Action=MoveToTarget
     Entry 2: Condition=TargetLost, Action=Patrol, Param=5.0（巡逻半径）
     Entry 3: Condition=Always, Action=Idle  ← ⚠️ 必须有 Always 兜底（WF-005）

2. 右键 Assets/_Game/Configs/Entity/ → Create → Entity/EntityConfig（推荐右键菜单，WF-010）
3. 在 Inspector 中填写：
   - DisplayName: "史莱姆"
   - Camp: Enemy
   - Components: **[必填]** 至少勾选 State（WF-006）。完整示例：[State, Health, Movement, Collision, AI, Skill]
     Skill 槽位由 AttackComponent 使用
   - MaxHp: 50, MoveSpeed: 2, CollisionRadius: 0.4
   - AttackInterval: 1.5, AttackBulletType: (拖入弹幕 **BulletTypeSO**), AttackFireOffset: (0, 0.3)
     ← v2.6 修正（WF-003）：类型为 BulletTypeSO（非 VFXTypeSO）
   - AIBehavior: (拖入步骤 1 创建的 AIBehaviorSO)
   - HitFlashDuration: 0.1, HitFlashColor: white, KnockbackDistance: 0.5
   - ShowDamageNumber: true
   - SpawnEffect: (可选), HitEffect: (可选), DeathEffect: (可选)
   - DeathDelay: 0.3
   - DebugColor: red
   - PoolInitial: 5, PoolMax: 20
4. 保存 SO 资产
```

### 10.2 编排关卡波次

> **v2.6 更新（WF-001）**：步骤 3 明确 Bootstrap 前置。
> **P2.5 更新**：新增 TriggerZone 触发启动模式。

```
0. [前置] 确认场景中已有 EntitySystemBootstrap（见 §10.0）
1. 右键 Assets/_Game/Configs/SpawnWave/ → Create → Entity/SpawnWaveConfig
2. 在 Inspector 中编排：
   - Wave 0: Groups=[{史莱姆, Enemy, 3, 0.5s}], TriggerMode=Timer, TriggerDelay=2s
   - Wave 1: Groups=[{史莱姆, Enemy, 2}, {精英哥布林, Enemy, 1}], TriggerMode=AllCleared
3. 在场景中创建空 GO → 挂 EntitySpawnPoint → 拖入 WaveConfig SO
   （EntitySystemBootstrap 会在 Awake 时自动发现并启动 AutoStartOnEnable=true 的 SpawnPoint）
4. 调整 AreaRadius（Scene View 中可见黄色圆圈 + 名称标签）
5. Play Mode → 观察波次按配置生成
```

**使用 TriggerZone 触发启动（P2.5 新增）**：

当整个刷怪点需要**等玩家进入特定区域后才开始刷怪**时，给 SpawnPoint 关联一个 TriggerZone：

```
1. 创建触发区域 GO：
   a. 场景中新建空 GO（命名建议：TriggerZone_SpawnPoint1）
   b. 添加 BoxCollider2D 或 CircleCollider2D → 在 Inspector 中调整大小/位置
      （Collider2D 会被自动设为 IsTrigger=true）
   c. 添加 EntityTriggerZone 脚本
      - TargetCamp = Player（检测玩家进入）
      - OneShot = true（进入一次即永久激活）

2. 配置 SpawnPoint：
   - 在 EntitySpawnPoint 的 TriggerZone 字段中拖入步骤 1 创建的 TriggerZone GO
   - WaveConfig 照常配置波次 SO
   - AutoStartOnEnable 此时不生效（有 TriggerZone 时以 TriggerZone 为准）

3. Play → 验证：
   - 场景加载后该 SpawnPoint 不会立即刷怪
   - 玩家 Entity 移入 TriggerZone 区域 → IsTriggered = true → SpawnPoint 启动刷怪
   - 启动后波次内部的 Timer/AllCleared/OnCallback 逻辑照常推进
   - Scene View 中 TriggerZone 标签从绿色变为红色 "[TRIGGERED]"

💡 设计理念：TriggerZone 是 SpawnPoint 级的"启动开关"，不是波次级的。
   SpawnPoint.TriggerZone == null → 自动开始
   SpawnPoint.TriggerZone != null → 等触发后才开始
```

### 10.3 调试与迭代

> **v2.4 措辞修正（GD-R4-007）**：明确 SO 热修改的实际限制。

```
1. Play Mode 中：
   - EntityGizmoDrawer 显示碰撞圈（红=敌、绿=友、灰=中立）
   - EntityViewBridge Debug View 显示彩色圆 + HP 文本
   - 弹幕命中 → DamageContext → 闪白 → 击退 → 伤害数字 → 扣血 → HP 文本更新 → 死亡延迟 → 特效 → 回收
2. 运行时修改 SO 参数对 **已存在的 Entity 不生效**（它们在 Init 时已读取配置快照）。
   新从池中取出的 Entity 会使用新配置。
   v2.6（WF-002）：使用 **Entity Debug Overview** 窗口（Window/Entity/Debug Overview）的
   **"Restart All Waves"** 按钮可快速清除所有 Entity 并重新启动波次，无需退出 Play Mode。
   （Phase 2 可选：EntityManager.HotReloadConfig(EntityConfigSO) 热刷新 API）
3. 批量调整：选中多个 EntityConfigSO → 在 Inspector 中批量修改字段
4. v2.6（WF-002）：EntityConfigSO Inspector 在 Play Mode 下会显示黄色提示，提醒修改仅对新 Entity 生效
```

---

---

## 十一、未决项清单（六轮 PK 汇总）

> **v2.5 新增，v2.6 更新**：汇总 R1~R6 共 69 个问题中所有非 Phase 1 事项。天命人待决策项 = 0（D-01~D-04 全部已决）。

### Phase 2 待办（25 项）

| # | 来源 | 描述 |
|---|------|------|
| 1 | R1 EC-002 | 碰撞动态注册策略：Entity > 64 时启用 CollisionRegistrationPass |
| 2 | R2 BL-01 / GD-101 | EntityConfigSO 扩充受击参数（击退曲线 KnockbackCurve / 无敌帧 IFrameCount） |
| 3 | R2 BL-02 / GD-102 | ~~TriggerZone 触发区域启动控制~~ ✅ P2.5 |
| 4 | R2 BL-03 / GD-102 | 刷怪阵型排列模式（Line/Circle，Random 已在 Phase 1） |
| 5 | R2 BL-04 / GD-007 | AI 行为表 conditionType/actionType 迁移 Luban 时改用 enum 类型 |
| 6 | R2 BL-05 / GD-104 | Luban 迁移：添加 `Spawn(int configId,...)` 重载 |
| 7 | R2 GD-006 | Luban 配置表整体迁移（Phase 2 可选保留 SO 或迁移 Luban） |
| 8 | R2 GD-001/103 | EntityViewBridge Phase 2 切换：ViewPrefab 正式渲染（Spine / 序列帧选型） |
| 9 | R4 GD-R4-001 | DamageContext Phase 2 扩展：DamageType / CritMultiplier 等 |
| 10 | R4 GD-R4-001 | HealthComponent 增加 IDamageModifier 接口（减伤/免伤/反弹） |
| 11 | R4 GD-R4-002 | 状态互斥规则 SO 配置化（Phase 1 硬编码 < 5 条） |
| 12 | R4 GD-R4-004 | 受击顿帧（HitStop）实现（Phase 1 已预留 PauseFor/IsPaused） |
| 13 | R4 GD-R4-005 | 刷怪条件分支（HP 判断出精英怪） |
| 14 | R4 GD-R4-005 | 刷怪难度缩放参数（HpMultiplier / CountMultiplier per wave） |
| 15 | R4 GD-R4-007 | EntityManager.HotReloadConfig(EntityConfigSO) 热刷新 API（可选） |
| 16 | R4 GD-R4-008 | 受击变色方案多样化（弹性缩放等）→ ViewBridge 扩展 |
| 17 | R4 GD-R4-008 | Animation 速度倍率 → AnimationComponent 扩展 |
| 18 | R5 ET-005 | AIBehaviorSOEditor 深度：ConditionParam 上下文提示 + 模拟测试按钮 |
| 19 | R5 ET-007 | EntitySpawnWaveSOEditor 深度：拖拽排序、时间线可视化 |
| 20 | R5 ET-008 | EntityDebugWindow 扩展：EventBus 事件追踪 + AI 决策链可视化 |
| 21 | R5 ET-011 | EntityConfigValidator 软警告：SO 资产不在推荐目录下 |
| 22 | R5 ET-004 | asmdef 独立拆分评估（Phase 2+ 如需拆分模块） |
| 23 | R6 WF-007 | PoolDefinition 深度方案：引入 EffectPoolDefinition 子类或 Tag 标记（Phase 1 已有 Tooltip + 预览行） |
| 24 | R6 WF-008 | EntityConfigSOEditor Inspector 内嵌反向引用查询（需缓存机制避免全扫库） |
| 25 | R6 WF-009 | SOCreationWizard "从模板创建" 功能（Phase 1 已有右键 + 复制模板 SO） |

### Phase 3 待办（4 项）

| # | 来源 | 描述 |
|---|------|------|
| 1 | R4 GD-R4-003 | 完整 SkillComponent 技能系统（Phase 1 AttackComponent 为最小替代） |
| 2 | R4 GD-R4-002 | 完整 FSM 状态机编辑器（可能走 behaviac/行为树） |
| 3 | R4 GD-R4-009 | 直接伤害路径（不走弹幕）→ 需 `EntityManager.FindEntitiesInRadius()` |
| 4 | R5 ET-002 | Phase 3 SkillComponent 上线后 ComponentType Skill 标签改为 `Skill (Attack | Skill)` |

### Backlog（已知限制 / 非阻塞记录，5 项）

| # | 来源 | 描述 |
|---|------|------|
| 1 | R3 SA-003 | SO 做 Dictionary Key 的 Skip Domain Reload 兼容性——已知限制（§八风险表） |
| 2 | R4 GD-R4-006 | 跑酷(⭐⭐)和放置(⭐⭐)品类需重大扩展——§一设计目标已诚实记录 |
| 3 | R4 GD-R4-004 | 镜头抖动（ScreenShake）——框架外职责，游戏层订阅 OnDeath 自行实现 |
| 4 | R4 GD-R4-005 | SpawnFormation 枚举已预留 Line/Circle，Phase 1 只实现 Random |
| 5 | R5 ET-011 | SO 资产命名/目录组织无强制约束——Phase 1 资产少，文档推荐即可 |

### 天命人待决策（0 项）

> D-01~D-04 全部已决。五轮 PK 中无遗留待决策项。

---

> **文档维护说明**：此文档随开发进度迭代更新，每次架构变更需在此文档中同步修改并更新版本号。行为契约（BC-xx）变更需 ADR 审批。
