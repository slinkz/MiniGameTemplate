---
system: entity-component
scope: config-planning
last_verified: 2026-05-02
depends_on: [EC_TDD_05_COMPONENTS]
related_code: Assets/_Framework/EntitySystem/Config/EntityConfigSO.cs
---

## 五、配置设计

> **v2.2 变更（GD-101/104/105）**：Phase 1 使用 EntityConfigSO（ScriptableObject），Luban 配置表降级为 Phase 2+ 预留。

### 5.0 EntityConfigSO（Phase 1 当前用）

> **PK R2 产物**：策划通过 SO Inspector 创建/编辑角色配置，无需 Luban 工具链。

```csharp
/// <summary>
/// Entity 角色配置资产。Phase 1 核心配置入口。
/// 策划在 Inspector 中创建和编辑，路径：Assets/_Game/Configs/Entity/
/// </summary>
[CreateAssetMenu(fileName = "NewEntityConfig", menuName = "Entity/EntityConfig")]
public class EntityConfigSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("全局唯一配置 ID。Phase 1 可不填（用 SO 引用），Phase 2 迁移 Luban 时必填。")]
    public int ConfigId;
    public string DisplayName;                            // 调试/UI 显示名
    public EnumCamp Camp;                                 // 阵营（统一使用 EnumCamp）

    [Header("组件列表")]
    [Tooltip("该 Entity 挂载的组件类型。EntityPool 根据此列表预创建组件。")]
    public ComponentType[] Components;                    // 枚举数组
    // ── v2.5 填写说明（ET-002）──
    // Inspector 中通过 EntityConfigSOEditor 渲染为 Checkbox Grid（非裸数组）。
    // 关键规则：
    //   - 勾选 "Skill" = 启用 AttackComponent（Phase 1 复用 Skill 槽位）
    //     Inspector 标签显示为 "☑ Skill (Attack)"
    //   - Control / AI 互斥：只能选其一，另一个自动灰化
    //   - 依赖建议（Warning）：AI→建议搭配 Movement；Collision→建议搭配 Health
    // Phase 3 SkillComponent 上线后标签改为 "Skill (Attack | Skill)"

    [Header("属性")]
    public int MaxHp = 100;
    public float MoveSpeed = 3f;
    public float TurnSpeed = 360f;
    public float CollisionRadius = 0.5f;

    [Header("攻击（v2.4 新增，GD-R4-003 / v2.6 修正 WF-003）")]
    public float AttackInterval = 1f;                     // 攻击间隔（秒），0 = 不攻击
    public BulletTypeSO AttackBulletType;                 // v2.6 修正（WF-003）：发射的弹幕类型（null = 不攻击）
    public Vector2 AttackFireOffset;                      // 发射点偏移（相对 Entity 位置）

    [Header("AI 行为（v2.4 新增，GD-R4-002）")]
    public AIBehaviorSO AIBehavior;                       // AI 条件-动作表配置（null = 无 AI）

    [Header("受击反馈")]
    public float HitFlashDuration = 0.1f;                 // 受击闪白持续时间
    public Color HitFlashColor = Color.white;             // 受击闪白颜色
    public float KnockbackDistance = 0.5f;                // v2.4：击退距离（GD-R4-004）
    public float KnockbackDuration = 0.15f;               // v2.4：击退持续时间
    public bool ShowDamageNumber = true;                  // v2.4：是否显示伤害数字（GD-R4-008）

    [Header("视觉特效")]
    public PoolDefinition SpawnEffect;                    // v2.4：生成特效（走 PoolManager，可选）（GD-R4-008）
    public PoolDefinition HitEffect;                      // v2.4：受击特效（走 PoolManager，可选）（GD-R4-008）
    public PoolDefinition DeathEffect;                    // 死亡特效（走 PoolManager，可选）
    public float DeathDelay = 0.3f;                       // 死亡延迟（播完表现再回收）

    [Header("受击反馈（Phase 2+ 扩展，暂不实现）")]
    // public int HitStopFrames;                          // v2.4 预留（GD-R4-011）：顿帧
    // public int IFrameCount;                            // 无敌帧
    // public AnimationCurve KnockbackCurve;              // 击退曲线

    [Header("视觉表现")]
    public Color DebugColor = Color.red;                  // Phase 1 Debug View 的颜色
    public GameObject ViewPrefab;                         // Phase 2+: 正式视觉 Prefab（null 时用 DebugView）
    public PoolDefinition ViewPoolDef;                    // Phase 2+: 正式 View 的 PoolDefinition

    [Header("对象池")]
    public int PoolInitial = 5;                           // 预热容量
    public int PoolMax = 20;                              // 硬上限
}
```

**关键设计决策（GD-104）**：
- Phase 1 API 以 `EntityConfigSO` 引用为主键（`EntityManager._pools` 的 Dictionary key）
- `ConfigId` 字段为 Phase 2 Luban 迁移预留——策划填上与 Luban TbEntityConfig.id 相同的值即可无缝切换
- Phase 2 迁移路径：EntityManager 增加 `Spawn(int configId, ...)` 重载，内部查 Luban 表；旧 `Spawn(EntityConfigSO, ...)` 保留

### 5.1~5.4 Luban 配置表设计（Phase 2+ 预留）

> 以下 Luban 表结构仅作为 Phase 2 迁移参考，Phase 1 不实现。

<details>
<summary>点击展开 Phase 2 Luban 表结构</summary>

#### 5.1 TbEntityConfig（角色配置表）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | 配置 ID（与 EntityConfigSO.ConfigId 对应） |
| name | string | 角色名（调试用） |
| faction | int | 阵营（0=Enemy, 1=Player, 2=Neutral） |
| maxHp | int | 最大生命值 |
| moveSpeed | float | 基础移速 |
| turnSpeed | float | 转向速度 |
| collisionRadius | float | 碰撞半径 |
| components | list\<int\> | 挂载的组件类型 ID 列表 |
| poolInitial | int | 对象池初始容量 |
| poolMax | int | 对象池最大容量 |

#### 5.2 TbStateConfig（状态配置表）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | 状态标签 ID（对应 BitFlags 的 bit 位） |
| name | string | 状态名 |
| exclusions | list\<int\> | 互斥状态 ID 列表 |

#### 5.3 TbAIBehavior（AI 行为表）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | 行为 ID |
| entityConfigId | int | 关联的角色配置 |
| priority | int | 优先级（数字越小越先检查） |
| conditionType | int | 条件类型枚举 |
| conditionParam | float | 条件参数（如血量阈值 0.2 = 20%） |
| actionType | int | 动作类型枚举 |
| actionParam | float | 动作参数 |

#### 5.4 TbAnimMapping（动画映射表）

| 字段 | 类型 | 说明 |
|------|------|------|
| entityConfigId | int | 关联的角色配置 |
| stateId | int | 状态标签 ID |
| animId | string | 动画片段 ID |
| priority | int | 同时多状态时的优先级 |

</details>

---

## 六、实施计划

### Phase 1：核心层 + 碰撞集成（预估 5 天）

> **v2.1 变更（EC-015）**：每步补充功能性 AC，"编译通过"不再作为唯一验收标准。

| 步骤 | 内容 | AC（全部需满足） |
|------|------|-----------------|
| P1.0 | ~~阵营枚举统一迁移（BulletFaction → EnumCamp）~~ ✅ 2026-04-30 | ✅ 全项目零 `BulletFaction` 引用 + 编译通过 + DanmakuDemo 行为不变（SA-002，v2.3 新增） |
| P1.1 | ~~IEntityComponent / ITickable / Entity 容器~~ ✅ 2026-04-30 | ✅ 编译通过 + GetComponent(ComponentType) O(1) 返回正确组件 + GetComponent 未注册类型返回 null |
| P1.2 | ~~EntityEventBus（零 GC 泛型事件总线）~~ ✅ 2026-04-30 | ✅ 编译通过 + Publish→Subscribe 正确分发 + ClearAll 后无残留 + Profiler 验证 100 次 Pub/Sub 周期 GC = 0 |
| P1.3 | ~~EntityPool + EntityManager~~ ✅ 2026-04-30 | ✅ 编译通过 + Profiler 验证 50 次 Acquire+Release 周期 GC = 0 + 池满 LogWarning 不崩 + 延迟销毁在 Tick 中不崩 |
| P1.4 | ~~StateComponent + HealthComponent~~ ✅ 2026-04-30 | ✅ 编译通过 + 互斥状态冲突时正确阻止 + OnDamaged 事件携带正确来源 + HP=0 触发 OnDeath |
| P1.5 | ~~CollisionComponent（ICollisionTarget 桥接）~~ ✅ 2026-04-30 | ✅ 编译通过 + DanmakuDemo 中弹丸命中 Entity 触发 OnCollisionHit + 注册/注销不泄漏槽位 |
| P1.6 | ~~MovementComponent + AnimationComponent（纯逻辑）~~ ✅ 2026-04-30 | ✅ 编译通过 + Entity 位置按速度更新 + CurrentAnimId 随状态切换 |
| P1.7 | ~~ControlComponent + AIComponent + AttackComponent~~ ✅ 2026-04-30 | ✅ 编译通过 + 同 Entity 互斥挂载校验 + AI 条件-动作表（AIBehaviorSO）驱动行为切换 + IAIAction 有状态 Action（Patrol 多帧上下文保持）+ AttackComponent 定时发射弹幕 (**v2.4 扩展**) |
| P1.8 | ~~EntityConfigSO 配置驱动验证 + Editor 工具~~ ✅ 2026-04-30 | ✅ 从 EntityConfigSO 创建完整 Entity（含正确组件列表）+ Inspector 可编辑所有 Phase 1 字段（含 AIBehavior/Attack/Effect 新增字段）+ **EntityConfigSOEditor 条件显示 + HelpBox 警告正常工作**（ET-001/002）+ **Components CheckboxGrid 互斥校验正常**（ET-002）+ **EntityConfigValidator MenuItem 批量校验输出正确**（ET-006）+ **AIBehaviorSOEditor 可读摘要标题正常显示**（ET-005）+ **EntitySpawnWaveSOEditor 摘要面板正常显示**（ET-007）+ **EntityDebugWindow Play Mode 概览面板可打开并显示数据**（ET-008）+ **SOCreationWizard 含 Entity 系列 3 种 SO 类型**（ET-010）(**v2.5 扩展**) |
| P1.9 | ~~EntityViewBridge + Debug View~~ ✅ 2026-04-30 | ✅ Entity 生成时自动创建 Debug GO（彩色圆 + HP 文本）+ Despawn 时归还 PoolManager + 每帧位置同步 |
| P1.10 | ~~刷怪系统（EntitySpawner + EntitySpawnPoint）+ EntitySystemBootstrap~~ ✅ 2026-04-30 | ✅ 场景放置 EntitySpawnPoint + 配置 EntitySpawnWaveSO → 按波次生成 Entity + Timer/AllCleared/OnCallback 三种触发模式 + Loop 循环正常工作 + **场景中放 EntitySystemBootstrap → 自动驱动刷怪系统（v2.6 WF-001）** |
| P1.11 | ~~集成验收~~ ✅ 2026-04-30 | ✅ Demo 场景：1 玩家（ControlComponent 手动发射）+ 3 敌人（AIBehaviorSO 驱动追击+AttackComponent 自动射击）+ 双向弹幕交互 + 敌人被命中→DamageContext 传递→受击闪白→击退→伤害数字弹出→死亡延迟→死亡特效→回收 + Entity 总内存 < 2MB + **Demo SO 资产保留为模板（存放 `Assets/_Game/Configs/_Template/`，文件名 `Template_` 前缀）（v2.6 WF-009）** |

### Phase 2：渲染升级 + Entity vs Entity 碰撞 + Luban 迁移（预估 4 天）

> **v2.2 变更**：EntityViewBridge 已提前到 Phase 1（P1.9）；Phase 2 聚焦渲染升级和 Luban。

| 步骤 | 内容 | 状态 |
|------|------|------|
| P2.1 | 正式 ViewPrefab 渲染（IEntityView + EntitySpriteAnimator + dual-path ViewBridge） | ✅ 2026-04-30 |
| P2.2 | Entity vs Entity 碰撞（EntityCollisionSolver，圆 vs 圆） | ✅ 2026-04-30 |
| P2.3 | ~~Luban 配置迁移（TbEntityConfig + Spawn(int configId,...) 重载）~~ ✅ 2026-05-01 | ✅ tables.xml EntityConfig bean + entityconfig.xlsx(3条模板数据) + gen_config 生成 C#/.bytes + EntityConfigRegistry(ConfigId↔SO O(1)桥接) + EntityManager.Spawn(int configId) 重载 + Bootstrap 自动注册 |
| P2.4 | ~~受击扩展参数（击退/无敌帧/击退曲线）~~ ✅ 2026-05-01 | ✅ DamageContext 扩展（DamageType/CritMultiplier/IsCritical/FinalDamage）+ IDamageModifier 伤害拦截链 + HealthComponent 无敌帧(IFrameCount)+HitStop 顿帧(HitStopFrames) + MovementComponent 击退曲线(KnockbackCurve) + EntityConfigSO 新增 IFrameCount/HitStopFrames/KnockbackCurve + HitReactionHandler 暴击伤害数字放大 |
| P2.5 | ~~TriggerZone 触发区域启动控制~~ ✅ 2026-05-01 | ✅ EntitySpawnPoint.TriggerZone 字段（SpawnPoint 级开关：有=等触发，无=自动开始）+ EntityTriggerZone 场景组件（Collider2D.OverlapPoint 轮询检测，零 GC，Gizmo 可视化）+ Bootstrap.CheckPendingTriggerPoints 每帧检查 |
| P2.6 | 集成验收 | 🔲 |

### Phase 3：高级功能（预估 3 天）

| 步骤 | 内容 |
|------|------|
| P3.1 | SkillComponent + 技能效果管理器 |
| P3.2 | AutoAimComponent |
| P3.3 | 伤害管理器（跨 Entity 伤害流水线） |
| P3.4 | 真机性能验证（55fps / 20 Entity + 弹幕） |

---

## 七、质量属性

| 属性 | 目标 | 验证方式 |
|------|------|----------|
| **零 GC** | Entity/组件全池化，事件用 struct，EventBus 预分配 | Profiler GC Alloc = 0 |
| **Tick 性能** | 20 Entity + 2048 弹幕 < 2ms（总 Tick） | Profiler Deep Profile |
| **内存预算** | EntitySystem 总内存 < 2MB（含所有池 + Manager） | Profiler Memory |
| **同屏上限** | 由 EntityPool.MaxCapacity 控制，开发期报警 | LogWarning |
| **可维护性** | 新增组件只需实现接口 + 注册 ComponentType 枚举 | 代码审查 |
| **可测试性** | Entity/组件不依赖 MonoBehaviour，可单元测试 | NUnit |

---

## 八、风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| TargetRegistry 64 槽位不够 | 超大量 Entity 无法被弹幕命中 | 已从 16 扩到 64（D-01）；Phase 2+ 动态注册策略（3.9 节）|
| Entity 纯逻辑层与 View 层桥接复杂 | View 生命周期管理 | Phase 1 EntityViewBridge + Debug Prefab 走 PoolManager（§3.15） |
| SO → Luban 迁移成本 | Phase 2 API 签名变更 | ConfigId 双路桥接（§5.0），渐进迁移不改旧 API |
| Entity vs Entity 碰撞性能 | O(n²) 在 Entity 多时吃力 | Phase 2 用空间分区（Grid/Quadtree） |
| SO 做 Dictionary Key 的 Domain Reload 限制（SA-003） | Enter Play Mode Settings 关闭 Domain Reload 时，SO InstanceID 可能变化导致 `_pools` Dictionary 查询失效 | Phase 1 不使用 Skip Domain Reload；如需支持则改用 int configId 为 Key（Phase 2 Luban 迁移时自然解决） |
| PierceHitMask 扩容内存开销（SA-001） | BulletCore 结构体 +8 bytes（48→56），2048 弹丸多占 16KB | 仍在 L2 缓存范围内，接受此开销 |
| BulletCore.OwnerEntityId 内存开销（GD-R4-001） | BulletCore +4 bytes（56→60），2048 弹丸多占 8KB | 仍在 L2 缓存范围内，接受此开销。必须修改 DanmakuSystem.Fire() API 签名（新增可选 ownerId 参数） |
| AttackComponent 复用 Skill 槽位（GD-R4-003） | Phase 3 SkillComponent 上线时需处理与 AttackComponent 的槽位共存或替代关系 | Phase 3 决策：SkillComponent 可包含 AttackComponent 能力，或两者共存（Skill 槽位拆分为 Attack + Skill） |

---

