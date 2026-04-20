# 落地可行性终审：能否直接编码？

> 评审日期：2026-04-11 | 角色：Unity 架构师（终审，面向执行）
> 评审标准：逐 Phase 逐任务，问"如果明天动手写，还有什么不确定的？"

---

## 审查方法

逐行对照了：
1. 四份评审文档（REFACTOR_PLAN / REVIEW_GAME_DESIGNER / REVIEW_SOFTWARE_ARCHITECT / REVIEW_UNITY_ARCHITECT）
2. **全部现有源代码**（DanmakuSystem 35 文件 + VFXSystem 8 文件 + 3 个 Shader + 所有配置/注册类）
3. 现有数据结构、API 签名、硬编码常量的真实值（逐行验证）

以下按 **可直接编码 ✅ / 需要补充设计 ⚠️ / 存在阻塞 ❌** 三档标记。

---

## 一、全局性问题（跨 Phase）

### ✅ GAP-001: RenderLayer 枚举归属已决


**现状**：
- `RenderLayer` 枚举定义在 `DanmakuEnums.cs`（`namespace MiniGameTemplate.Danmaku`，第 10-14 行）
- `VFXRenderLayer` 枚举定义在 `VFXEnums.cs`（`namespace MiniGameTemplate.VFX`，第 5-9 行）
- 两者语义完全相同：`Normal=0, Additive=1`，底层类型均为 `byte`

**问题**：Phase 0 要把 `DanmakuVertex` 提到 `Rendering/`，但 `RenderBatchManager` 的 `BatchKey` 需要一个统一的 `RenderLayer` 枚举。用哪个？

**可选方案**：
- **方案 A**：在 `_Framework/Rendering/` 新建 `RenderLayer.cs`，两边旧枚举标 `[Obsolete]` 渐进迁移
- **方案 B**：直接用 `DanmakuEnums.RenderLayer`，VFX 侧 `VFXRenderLayer` 保留不动（BatchManager 内部做映射）
- **方案 C**：BatchManager 不关心 Layer 枚举，用 `int layer` 参数，两边各自传自己的值

**已决**：采用 **方案 A**。在 `_Framework/Rendering/` 新建统一 `RenderLayer`，Danmaku 与 VFX 全量切换到共享枚举，旧枚举迁移后删除，不保留双轨映射。

**理由**：既然要建共享 `Rendering/` 模块，就应有统一枚举。迁移量小，且能避免后续批次键、调试语义和扩展层定义继续分裂。


**影响范围**（代码实况）：
- `DanmakuEnums.cs` 中 `RenderLayer` 被引用于：`BulletTypeSO.Layer`、`BulletRenderer`、`LaserTypeSO`（若有）
- `VFXEnums.cs` 中 `VFXRenderLayer` 被引用于：`VFXTypeSO.Layer`、`VFXBatchRenderer`
- 总替换点约 10-15 处，工作量 < 30 分钟

---

### ✅ GAP-002: RenderBatchManager 生命周期与归属已决

> **补充更新（2026-04-11 晚间修订）**：BatchManager 的共享实现现在同时服务 Bullet 与 VFX 两类“独立贴图优先”资源模型；DamageNumber 仍保持独立图集策略，不参与这轮资源自由化改造。



**现状**：
- `DanmakuSystem` 是 `DontDestroyOnLoad` 单例（520 行），跨场景存活
- `SpriteSheetVFXSystem` 是独立 MonoBehaviour，被 `DanmakuSystem` 通过 `[SerializeField] _hitVfxSystem` 引用
- 新的 `RenderBatchManager` 需要预创建 Mesh/Material

**问题**：谁拥有 `RenderBatchManager`？

**可选方案**：
- **方案 A**：独立 MonoBehaviour 单例（自己的 DontDestroyOnLoad）→ 两个单例初始化顺序问题
- **方案 B**：纯 C# 类，`DanmakuSystem.Awake()` 创建 → VFX 侧如何访问？
- **方案 C**：**不共享实例**——Danmaku 和 VFX 各自持有独立的 BatchManager 实例，只共享 `RenderVertex` 类型和 `RenderBatchManager` 类定义

**已决**：采用 **方案 C**。`RenderBatchManager` 作为共享实现类存在，但实例归属各系统自身：Danmaku / VFX / 其他系统各自持有实例，不共享全局单例。

**理由**：
1. 当前 VFX 和 Danmaku 的渲染完全独立（不同的材质、不同的贴图集、不同的 sortingOrder），没有跨系统合批需求
2. 共享的是**代码复用**，不是运行时数据和生命周期
3. 各自管理生命周期最简单，不引入跨系统初始化依赖


**如果选方案 C 对文档的影响**：
- `REVIEW_SOFTWARE_ARCHITECT.md` 2.1 节拓扑图需修改——不是"共享一个 RenderBatchManager 实例"，而是"共享 RenderBatchManager 类型定义"
- sortingOrder 的全局排序需要**约定**而非代码强制

---

### ✅ GAP-003: CollisionEventBuffer 消费方式已决


**现状**：
- `CollisionEventBuffer` 的**数据结构**设计很好（CollisionEvent 24B，预分配数组，TryWrite/AsSpan）
- 但**谁来读、怎么读、读了做什么**完全没有规定

**当前碰撞结果的消费方式**（代码实况）：
```csharp
// DanmakuSystem.Update() 约第 149-172 行
var result = _collisionSolver.SolveAll(...);
if (result.PlayerHit && _invincibleTimer <= 0) { ... }
if (result.NonPlayerHit) { PlayHitVFX(...); }
```

`CollisionResult` 是聚合摘要（"有没有命中、总伤害多少"），碰撞细节（哪颗子弹、打到谁、在什么位置）已在 `CollisionSolver` 内部消费（直接调 `target.OnBulletHit()`）。

**问题**：新的 `CollisionEventBuffer` 打算取代还是补充现有机制？

**可选方案**：
- **方案 A**：取代 `ICollisionTarget.OnBulletHit/OnLaserHit/OnSprayHit` → 需重写碰撞后处理
- **方案 B**：补充回调，回调管即时响应（击退），Buffer 管延迟处理（统计/VFX）→ 两套碰撞机制并存
- **方案 C**：保留 `ICollisionTarget` 回调不变，`CollisionEventBuffer` **仅用于**弹幕×VFX 联动和清屏转化

**已决**：采用 **方案 C**。保留 `ICollisionTarget` 回调不变，`CollisionEventBuffer` 仅用于弹幕×VFX 联动、清屏转化、调试观察等“旁观者消费”。

**消费约束**：
1. 只有 `CollisionSolver` 写入 Buffer
2. 只有 `DanmakuSystem` 在固定帧阶段做主消费
3. 调试/分析模块允许只读观察，不允许业务模块各自抢消费
4. Buffer 在帧末统一 Reset

**理由**：`ICollisionTarget` 回调已工作且设计良好，负责即时命中响应；Buffer 负责延迟消费和旁路观察，两者职责分离最稳定。


---

### ✅ GAP-004: MotionRegistry 策略表 API 已决


**现状**：REFACTOR_PLAN 2.3 说"实现 MotionRegistry 运动策略表 ~120 行"，但未给出：
- 策略的函数签名
- 如何从 `BulletTypeSO` 引用运动策略
- 与现有 `FLAG_HOMING` / `FLAG_SPEED_CURVE` / `FLAG_HAS_MODIFIER` 如何共存

**当前运动调度方式**（BulletMover.cs 实况，222 行）：
```
if FLAG_HAS_MODIFIER → CalculateModifierSpeed（延迟变速）
else if FLAG_SPEED_CURVE → AnimationCurve.Evaluate（速度曲线）
if FLAG_HOMING → 追踪转向
位置 += 速度 × 倍率 × dt
```
这是一个 **if/else 链**，新增运动类型需新增 Flag 位 + 新增 else if 分支。

**BulletCore.Flags 位映射实况**（BulletCore.cs）：
```
FLAG_ALIVE          = 1 << 0  // bit 0
FLAG_HOMING         = 1 << 1  // bit 1
FLAG_SPEED_CURVE    = 1 << 2  // bit 2
FLAG_HAS_MODIFIER   = 1 << 3  // bit 3
FLAG_HAS_TRAIL      = 1 << 4  // bit 4
FLAG_PIERCING       = 1 << 5  // bit 5
FLAG_HIT_EXPLODE    = 1 << 6  // bit 6
FLAG_DEATH_EXPLODE  = 1 << 7  // bit 7
```
**8 位全部用完**，无法再通过 Flag 区分运动类型。

**建议的设计**：
```csharp
// 策略签名
public delegate void MotionUpdate(
    ref BulletCore core,
    ref BulletModifier modifier,
    BulletTypeSO type,
    Vector2 playerPos,
    float dt);

// BulletCore 如何引用策略——通过 BulletTypeSO.MotionType
// BulletMover 通过 registry.BulletTypes[core.TypeIndex].MotionType 获取
// 不增加 BulletCore 体积
```

**已决**：MotionRegistry 采用**受控注册表**。从 `BulletTypeSO` 读取 `MotionType`，不增加 `BulletCore` 体积；`BulletMover` 通过 `TypeIndex -> BulletTypeSO -> MotionType` 获取策略。

**约束**：
- 不做运行时开放注册
- 不做反射注册
- 不做任意脚本策略热插拔
- 新增 Motion 仍通过编译期代码接入


**不阻塞 Phase 0/1**，Phase 2 启动前确认即可。

---

### ✅ GAP-005: 容量收拢策略已决


**现有硬编码常量**（代码实况，逐文件验证）：

| 类 | 常量名 | 值 | 文件 | 当前用法 |
|---|---|---|---|---|
| `BulletWorld` | `DEFAULT_MAX_BULLETS` | 2048 | BulletWorld.cs:14 | 构造函数默认值（已支持参数化） |
| `LaserPool` | `MAX_LASERS` | 16 | LaserPool.cs | `const int`，内联数组初始化 |
| `SprayPool` | `MAX_SPRAYS` | 8 | SprayPool.cs | `const int`，内联数组初始化 |
| `ObstaclePool` | `MAX_OBSTACLES` | 64 | ObstaclePool.cs | `const int` |
| `TrailPool` | `MAX_TRAILS` | 64 | TrailPool.cs | `const int` |
| `TrailPool` | `MAX_POINTS_PER_TRAIL` | 20 | TrailPool.cs | `const int` |
| `PatternScheduler` | `MAX_TASKS` | 64 | PatternScheduler.cs | `const int` |
| `SpawnerDriver` | `MAX_SPAWNERS` | 8 | SpawnerDriver.cs | `const int` |
| `DamageNumberSystem` | `MAX_NUMBERS` | 128 | DamageNumberSystem.cs | `const int` |
| `TargetRegistry` | `MAX_TARGETS` | 16 | TargetRegistry.cs | `const int` |
| `AttachSourceRegistry` | `MAX_SOURCES` | 24 | AttachSourceRegistry.cs | `const int` |
| `VFXPool` | `DEFAULT_CAPACITY` | 64 | VFXPool.cs | 构造函数默认值（已支持参数化） |

**`DanmakuWorldConfig`** 已有 `MaxBullets/MaxLasers/MaxSprays` 字段（DanmakuWorldConfig.cs，25 行），但 `LaserPool/SprayPool` 内部仍用 `const int` 而不读取配置。

**关键卡点**：
1. `CollisionSolver` 第 48 行：`static readonly bool[] _sprayTickedThisFrame = new bool[SprayPool.MAX_SPRAYS]` — 如果 `MAX_SPRAYS` 不再是 `const`，这行编不过
2. 将 `const` 改为构造函数传参需要改 **8-10 个类的构造函数签名** + 所有调用方
3. `CollisionSolver` 的静态数组需改为实例变量

**已决**：容量配置采用**分层收拢**，不做一次性全动态化。
- 0.4a: 先收拢主链路容量（Bullets / Lasers / Sprays / VFX / CollisionEventBuffer）
- 0.4b: 再按依赖序逐步改造其余容量点（如 Targets / Obstacles / DamageNumbers / AttachSources）
- PatternScheduler / SpawnerDriver 等低频辅助模块本轮暂不纳入统一配置化

**理由**：文档原先低估了 `const`、静态数组和构造签名联动改造的工程量，分层收拢能控制 Phase 0 范围，避免基础设施改造膨胀成全系统返工。


---

### ✅ GAP-006: DanmakuSystem 拆分边界已决


**现状**：Phase 2.6 说"拆分 DanmakuSystem → System + API + EventBus"，当前 `DanmakuSystem.cs` 520 行，职责：
- 初始化所有子系统
- 驱动 Update/LateUpdate 循环
- 提供 Fire/Register/Clear 公共 API
- 处理碰撞后逻辑（无敌帧、飘字、VFX）
- 直接 `[SerializeField]` 引用 `SpriteSheetVFXSystem`

**需要确认的拆分方式**：
- `DanmakuSystem.cs`：保留初始化 + Update 循环驱动（MonoBehaviour 入口）
- `DanmakuAPI.cs`：Fire/Register/Clear 方法 → **partial class 还是独立 facade**？
- `DanmakuEventBus.cs`：碰撞后事件分发 → **静态 Action 事件还是 ScriptableObject 事件频道**？

**已决**：DanmakuSystem 采用**保留 Facade 入口、内部拆职责**的方式演进，不拆成多个互相依赖的碎系统。

**落地方式**：
- `DanmakuSystem` 保留 MonoBehaviour 入口、生命周期、Update 驱动
- 对外 API 采用 `partial class` 承载，避免无意义 facade 转发
- 内部拆出运行时、碰撞流程、效果桥接等职责模块
- 事件分发优先使用轻量静态委托或内部调度，不引入 ScriptableObject 事件频道

**理由**：当前目标是降低复杂度并保持模板工程可理解性，不是把一个系统拆成一堆初始化顺序更脆的子系统。


---

## 二、逐 Phase 落地检查

### Phase 0：基础设施层

| 任务 | 可直接编码？ | 阻塞项 | 备注 |
|------|-------------|--------|------|
| 0.1 新建 `Rendering/` + MODULE_README | ✅ | — | 纯新建 |
| 0.2 `DanmakuVertex` → `RenderVertex` | ⚠️ | GAP-001 | 需同时决定 RenderLayer 归属 |
| 0.3 更新 using 引用 | ✅ | — | 涉及 BulletRenderer / LaserRenderer / VFXBatchRenderer / DamageNumberSystem / TrailPool |
| 0.4 容量硬编码收拢 | ⚠️ | GAP-005 | CollisionSolver 静态数组 + 8-10 个类构造函数改造 |
| 0.5 batchmode 编译验证 | ✅ | — | CLI 模板已有 |

**Phase 0 启动前必须决定**：GAP-001（RenderLayer 方案）

**0.3 补充发现**：
- `DamageNumberSystem.cs` 也使用了 `DanmakuVertex` 类型（用于飘字渲染），重命名需包含此文件
- `TrailPool.cs` **不使用** `DanmakuVertex`（拖尾用独立的线条顶点格式），无需改
- 总 using 变更文件数：**5 个**（BulletRenderer / LaserRenderer / VFXBatchRenderer / DamageNumberSystem / CollisionSolver 中如果有引用的话）

---

### Phase 1：渲染管线重构

| 任务 | 可直接编码？ | 阻塞项 | 备注 |
|------|-------------|--------|------|
| 1.1 `RenderBatchManager` 实现 | ⚠️ | GAP-002 | 需先确认共享实例 vs 各自实例 |
| 1.2 重构 `BulletRenderer` | ✅ | 依赖 1.1 | 当前 335 行，单图集 → 多贴图分桶 |
| 1.3 重构 `VFXBatchRenderer` | ✅ | 依赖 1.1 | 当前 207 行，同上 |
| 1.4 `BulletTypeSO` +Texture2D | ✅ | — | 新增字段 + FormerlySerializedAs |
| 1.5 `VFXTypeSO` +Texture2D | ⚠️ | MISS-004 | VFX 是否也改为多贴图分桶？见下文 |
| 1.6 重构 `LaserRenderer` | ✅ | — | 当前 267 行，单材质 → BatchManager |
| 1.7 编译验证 | ✅ | — | — |

**Phase 1 启动前必须决定**：GAP-002（BatchManager 归属）

**Phase 1 追加发现**：

#### ✅ NEW-001: BulletTypeSO 的 UV 策略已决

**现状**（BulletTypeSO.cs，135 行）：
- `Rect AtlasUV`：弹丸在图集内的 UV 子区域
- Phase 1 新增 `Texture2D BulletTexture`

**问题**：新增 `BulletTexture` 后——
- 每种弹丸独占整张贴图（UV 全覆盖 0-1）？
- 还是仍允许一张贴图放多种弹丸子图（保留 AtlasUV 语义）？

**已决**：保留 `UVRect`/`AtlasUV` 表达，但语义升级为“在当前源贴图内的采样区域”。这样：
- 每种子弹可直接使用独立贴图（UV = 0,0,1,1）
- 多种子弹也可共用同一张贴图的不同区域
- atlas 只是可选优化结果，不再是生产前置条件
- 旧字段通过 `[FormerlySerializedAs]` 或迁移器平滑迁移


#### 🟡 NEW-002: BulletTypeSO 旧 SO 资产迁移

当 `BulletTexture` 字段新增后，现有 SO 实例的 `BulletTexture` 为 `null`。需要：
- **运行时 fallback**：`BulletTexture ?? DanmakuRenderConfig.BulletAtlas`（渐进迁移）
- 或 **Editor 迁移脚本**：批量将现有 SO 的 `BulletTexture` 指向旧 Atlas
- 建议用 fallback 方式，更安全

---

### Phase 2：事件与扩展性

| 任务 | 可直接编码？ | 阻塞项 | 备注 |
|------|-------------|--------|------|
| 2.1 `CollisionEventBuffer` | ✅ | — | 数据结构已设计 |
| 2.2 `CollisionSolver` 写入 Buffer | ⚠️ | GAP-003 | 取代 or 补充 ICollisionTarget？ |
| 2.3 `MotionRegistry` | ⚠️ | GAP-004 | 策略签名 + Core 引用方式 |
| 2.4 `BulletMover` 使用策略表 | ⚠️ | 依赖 2.3 | — |
| 2.5 新增运动模式 | ✅ | 一旦 2.3 确定 | 正弦波 / 螺旋 |
| 2.6 拆分 `DanmakuSystem` | ⚠️ | GAP-006 | partial class vs facade + 事件方案 |
| 2.7 弹幕×VFX 联动 | ⚠️ | 依赖 GAP-003 | 碰撞 Buffer 消费方式决定联动机制 |
| 2.8 清屏 API | ✅ | — | FreeAll + 转化 |
| 2.9 编译验证 | ✅ | — | — |

**Phase 2 启动前必须决定**：GAP-003 + GAP-004 + GAP-006

#### 🟡 NEW-003: 多阵营碰撞过滤的扩展点

REFACTOR_PLAN 覆盖追踪表中 GD-006"多阵营系统"标注覆盖于 Phase 2（扩展碰撞过滤），但实际任务列表中**没有对应任务项**。

**现状**（CollisionSolver.cs, BulletCore.cs）：
- `BulletFaction : byte { Player=0, Enemy=1, Neutral=2 }`
- CollisionSolver 硬编码了 Player 和 Enemy 的碰撞对：`if (core.Faction == BulletFaction.Enemy)` → 只检测 PlayerTarget
- 没有通用的 Faction×Faction 碰撞矩阵

**问题**：Phase 2 是否需要加入碰撞矩阵？还是仅预留扩展点（CollisionEventBuffer 中带 Faction 信息即可）？

**建议**：Phase 2 只预留——`CollisionEvent` 带 `SourceFaction` + `TargetFaction` 字段就够了。实际的碰撞矩阵等有真实多阵营需求时再做。

---

### Phase 3：视觉增强

| 任务 | 可直接编码？ | 阻塞项 | 备注 |
|------|-------------|--------|------|
| 3.1 `BulletCore` +12B → 48B | ✅ | — | Scale(4) + Alpha(4) + Color(4) |
| 3.2 `BulletMover` 写动画值 | ✅ | — | 从 SO 曲线采样写入 Core |
| 3.3 `BulletRenderer` 读动画值 | ✅ | — | 应用到顶点色和大小 |
| 3.4 Shader dissolve | ✅ | — | 改 2 个 Shader |
| 3.5 Shader glow | ✅ | — | 改 2 个 Shader |
| 3.6 激光预警线渲染器 | ✅ | — | 新建 ~100 行 |
| 3.7 喷雾 VFX 桥接 | ⚠️ | NEW-004 | VFX PlayAttached API 设计未定 |
| 3.8 VFX 时间缩放 | ✅ | — | 改 SpriteSheetVFXSystem |
| 3.9 编译验证 | ✅ | — | — |

#### 🟡 NEW-004: VFX "附着模式" 设计缺失

**问题**：Phase 3.7 要求喷雾区域播放 VFX Sprite Sheet 动画，VFX 必须**附着在喷雾上移动**。当前 VFXInstance 结构（VFXInstance.cs，26 行）：
```csharp
public struct VFXInstance
{
    public Vector3 Position;        // 固定位置
    public Color32 Color;
    public float RotationDegrees;
    public float Scale;
    public float Elapsed;
    public ushort TypeIndex;
    public byte CurrentFrame;
    public byte Flags;
}
```

没有"附着到外部对象"的概念，`Position` 在 Play 时设定后不更新。

**需要的扩展**：
- `VFXInstance` 新增：`byte AttachFlags`（bit0=IsAttached, bit1=IsLooping）
- attached 特效每帧由外部更新 Position（通过 index 回写或 delegate 回调）
- attached 特效不由帧计数控制生命周期，而是外部调 `StopAttached(int index)` 显式销毁
- `SpriteSheetVFXSystem` 需要新增 `PlayAttached(VFXTypeSO type, Func<Vector3> positionProvider)` 或类似 API

**结论**：不阻塞 Phase 0-2，但 Phase 3 启动前必须设计好。

#### 🟡 NEW-005: AnimationCurve.Evaluate 热路径调用

**现有热路径调用点**（逐文件验证）：
| 文件 | 行 | 调用 | 频率 |
|------|---|------|------|
| BulletMover.cs | 93 | `type.SpeedOverLifetime.Evaluate(t)` | 每活跃弹丸每帧 |
| LaserUpdater.cs | 50 | `type.WidthOverLifetime.Evaluate(t)` | 每活跃激光每帧 |
| LaserUpdater.cs | 77 | 同上（不同分支） | 同上 |
| LaserRenderer.cs | 204-205 | `WidthProfile.Evaluate(u)` × 2 | 每段激光每帧 |
| TrailPool.cs | 194 | `WidthCurve.Evaluate(t)` | 每拖尾点每帧 |

**Phase 3 将新增**：
- `BulletMover` 写 Scale/Alpha/Color 动画值 → 新增 3 个 `AnimationCurve.Evaluate` 调用/弹丸/帧

**风险评估**：
- 2048 弹丸 × 4 次 Evaluate/帧 = **8192 次/帧**
- Unity `AnimationCurve.Evaluate` 在 IL2CPP 下约 ~50ns/次 = ~0.4ms/帧 → **可接受**
- 但在 Mono 解释器下约 ~200ns/次 = ~1.6ms/帧 → **微信小游戏用 IL2CPP 没问题**
- **备选**：RISK-008 已识别，若性能不足则回退 LUT

---

### ✅ NEW-005: Atlas 打包工具设计不能缺席

**问题**：既然 Bullet/VFX 都改为“独立贴图优先”，计划里如果完全不设计 atlas 工具，后面就会缺少一个正式的优化出口。

**已决**：增加 Editor Atlas 工具设计，但明确其定位为**可选优化工具**，不是运行前置条件。

**工具职责**：
- 输入：Bullet/VFX 源贴图列表、分组规则、最大尺寸、Padding、命名规则
- 输出：AtlasTexture、映射清单（Texture -> UVRect）、可选回写 SO 引用
- 支持 Bullet/VFX 两类资源批量打包
- DamageNumber atlas 仅做维护增强，不纳入同一条主工作流

**架构约束**：
- 未打包资源必须可直接运行
- 打包后资源也必须可回退到未打包状态
- 工具生成物必须是显式资产，不允许隐式运行时打包

---

### Phase 4：工作流与工具


| 任务 | 可直接编码？ | 阻塞项 | 备注 |
|------|-------------|--------|------|
| 4.1 弹丸子图选择器 | ✅ | — | Editor 窗口 |
| 4.2 SO 热重载 | ✅ | — | OnValidate |
| 4.3 碰撞 Gizmos | ✅ | — | Scene view overlay |
| 4.4 ProfilerMarker | ✅ | — | 改多个核心文件 |
| 4.5 文档更新 | ✅ | — | — |
| 4.6 CHANGELOG | ✅ | — | — |

Phase 4 **无阻塞项**。

---

## 三、遗漏清单（文档没提到但执行会撞到的）

### 🔴 MISS-001: BulletModifier 实际 20 字节

**代码实况**（BulletModifier.cs，28 行）：
```csharp
public struct BulletModifier  // 注释说 16B
{
    public float DelayEndTime;      // 4B
    public float DelaySpeedScale;   // 4B
    public float AccelEndTime;      // 4B
    public float HomingStartTime;   // 4B
    public float HomingStrength;    // 4B
}
// 实际 = 20 bytes，不是 16 bytes
```

**影响**：内存预算计算偏差
- 旧（文档）：2048 × (36+28+16) = 160 KB
- 实际（现在）：2048 × (36+28+20) = **168 KB**
- 新（Phase 3 后）：2048 × (48+28+20) = **192 KB**

不影响架构，但需修正文档中所有引用 "16B" 的地方。

---

### ⚠️ MISS-002: TrailPool 的独立渲染不应纳入 BatchManager

`TrailPool` 有独立的 Mesh + Material（`TrailPool.Render()` 直接调 `Graphics.DrawMesh`）。Phase 1 的 `RenderBatchManager` **不应**接管 TrailPool——拖尾是线条 Mesh，不是 Quad，走不同的顶点格式。

**建议**：保持 TrailPool 独立渲染，在 sortingOrder 约定中给它一个固定位置。

---

### ✅ MISS-003: DamageNumberSystem 的独立渲染边界已明确

`DamageNumberSystem` 有自己的 Mesh + 数字精灵图集（DamageNumberSystem.cs，233 行）。本轮正式结论：
- **继续保持独立渲染链路**，不并入 Bullet/VFX 的资源自由化主链路
- **默认继续使用共享数字图集**，因为飘字资源增长慢、字符集有限、atlas ROI 高
- 仍需在 Phase 0 将 `DanmakuVertex` 重命名迁移为 `RenderVertex`
- 可复用共享排序常量和调试监控，但不强行改成每个飘字类型独立贴图


---

### ✅ MISS-004: VFX 渲染器改为支持多贴图分桶（资源自由优先）


**现状**：
- `VFXRenderConfig` 当前仍是单张 `AtlasTexture`
- `VFXBatchRenderer` 当前仍按共享图集渲染所有 VFX
- 这与用户最新确认的“资源组织自由优先”原则冲突

**已决**：VFX 与 Bullet 一样，改为支持独立贴图 / 独立 SpriteSheetTexture 输入，运行时按 `(RenderLayer, Texture)` 分桶；atlas 仅为可选优化结果。

**理由**：
- 特效不应被强制绑定到 atlas 打包流程
- 设计师/美术添加新特效时，不应先经过图集重打包这道门槛
- DrawCall 增长是可接受代价，框架职责是承载设计自由，而不是反过来限制内容生产

**执行含义**：
- 保留 UV/Sheet 表达，但其语义升级为“在当前源贴图内的采样区域”
- `VFXTypeSO` 需要新增明确的源贴图字段
- `VFXBatchRenderer` 需要真正接入按贴图分桶，而不再是假共享实现
- atlas 工具改为 Phase 4 的可选优化工具，而不是 VFX 的前置依赖


---

### ⚠️ MISS-005: sortingOrder 配置方式

REVIEW_SOFTWARE_ARCHITECT 2.2 节给了 sortingOrder 值（0/100/200/300/400/500/600），但没定义写在哪。

**建议**：硬编码为 `static class RenderSortingOrder` 中的 const int，加注释说明排序逻辑；该文件应作为 sortingOrder 的代码唯一来源。配置化在此阶段 ROI 为负。

```csharp
// _Framework/Rendering/RenderSortingOrder.cs
public static class RenderSortingOrder
{
    public const int Background     = 0;
    public const int BulletNormal   = 100;
    public const int BulletAdditive = 200;
    public const int LaserNormal    = 300;
    public const int VFXNormal      = 400;
    public const int VFXAdditive    = 500;
    public const int DamageNumber   = 600;
    public const int Trail          = 150;  // 在 Bullet 之间
}
```

---

### 🟡 MISS-006: SpriteSheetVFXSystem.Play 每次调用 RebuildRegistryRuntimeIndices

**代码实况**（SpriteSheetVFXSystem.cs，约第 80 行）：
```csharp
public int Play(VFXTypeSO type, Vector3 position, ...)
{
    _registry.RebuildRuntimeIndices();  // 每次 Play 都重建！
    ...
}
```

`RebuildRuntimeIndices()` 遍历 `List<VFXTypeSO>` 重新赋值 RuntimeIndex。这在频繁触发特效时是不必要的开销。

**建议**：改为初始化时调用一次，或 dirty flag 模式（只在 Registry 内容变化时重建）。Phase 0 或 Phase 1 顺手修掉。

---

### 🟡 MISS-007: DanmakuSystem 对 VFX 的硬引用

**代码实况**（DanmakuSystem.cs）：
```csharp
using MiniGameTemplate.VFX;

[SerializeField] private SpriteSheetVFXSystem _hitVfxSystem;
[SerializeField] private VFXTypeSO _hitVfxType;
```

Phase 2.6 要拆分 DanmakuSystem，但如果碰撞后 VFX 触发逻辑仍在 DanmakuSystem 中，这个硬引用就必须保留。只有 Phase 2.7（弹幕×VFX 联动）改为通过 EventBus 触发后，才能解耦。

**影响**：Phase 2.6（拆分）和 2.7（联动）必须**同步执行**，不能先拆后联——否则拆出去的事件总线无法触发 VFX，而 DanmakuSystem 本体又已删除了 VFX 引用。

**建议执行顺序**：2.1 → 2.2 → 2.6+2.7 同步 → 2.3 → 2.4 → 2.5 → 2.8

---

## 四、风险表更新

在 REFACTOR_PLAN 的 RISK-001~008 基础上，补充代码审计中发现的实际风险：

| 风险 | 来源 | 严重度 | 描述 | 缓解 |
|------|------|--------|------|------|
| RISK-009 | MISS-001 | 🟢 低 | BulletModifier 实际 20B，文档标 16B | 修正文档 |
| RISK-010 | GAP-005 | 🟡 中 | CollisionSolver 静态数组依赖 const，容量改造需同步 | 拆两步执行 |
| RISK-011 | GAP-002 | 🟡 中 | 共享 BatchManager 会引入跨系统生命周期依赖 | 建议方案 C 避免 |
| RISK-012 | NEW-002 | 🟡 中 | BulletTypeSO 新增字段后旧 SO 的 null 引用 | 运行时 fallback |
| RISK-013 | NEW-004 | 🟡 中 | VFX 附着模式未设计，Phase 3.7 可能延期 | Phase 2 期间并行设计 |
| RISK-014 | NEW-005 | 🟢 低 | AnimationCurve.Evaluate 热路径调用增加 | IL2CPP 下可接受，Mono 下需 LUT |
| RISK-015 | MISS-006 | 🟢 低 | VFX Play 每次重建索引 | 初始化时调一次即可 |
| RISK-016 | MISS-007 | 🟡 中 | Phase 2.6 和 2.7 有隐式执行顺序依赖 | 同步执行，不拆开 |

---

## 五、决策矩阵总览

| ID | 问题 | 影响 Phase | 阻塞性 | 建议方案 |
|---|---|---|---|---|
| GAP-001 | RenderLayer 枚举归属 | **Phase 0** | ❌ 阻塞 | **A**（新建统一枚举） |
| GAP-002 | RenderBatchManager 生命周期 | **Phase 0-1** | ❌ 阻塞 | **C**（各自实例，共享类型） |
| GAP-003 | CollisionEventBuffer 消费方式 | Phase 2 | ❌ 阻塞 | **C**（保留回调，Buffer 给旁观者） |
| GAP-004 | MotionRegistry API | Phase 2 | ⚠️ 建议确认 | 从 BulletTypeSO 读 MotionType |
| GAP-005 | 容量收拢工程量 | Phase 0 | ⚠️ 知晓 | 拆两步执行 |
| GAP-006 | DanmakuSystem 拆分边界 | Phase 2 | ⚠️ 建议确认 | partial class + 静态 Action |
| NEW-001 | BulletTypeSO UV 策略 | Phase 1 | ✅ 已决 | 独立贴图优先 + 保留 UV 表达 |
| NEW-002 | 旧 SO 资产迁移 | Phase 1 | 🟢 不阻塞 | 编辑器迁移器 + 必要 fallback |
| NEW-003 | 多阵营碰撞矩阵 | Phase 2 | 🟢 不阻塞 | 只预留字段，不实现矩阵 |
| NEW-004 | VFX 附着模式 | Phase 3 | ⚠️ Phase 2 并行设计 | World / FollowTarget / Socket |
| NEW-005 | Atlas 打包工具 | Phase 4 | ✅ 已决 | 可选优化工具，不是生产前置 |
| MISS-004 | VFX 多贴图分桶 | Phase 1 | ✅ 已决 | VFX 支持独立贴图，atlas 仅可选优化 |


---

## 六、Phase 启动前置条件检查清单

### Phase 0 启动需要：
- [x] DEC-001~006 全部确认 ✅
- [x] **GAP-001 已决** — RenderLayer 统一上收至 `_Framework/Rendering/`
- [x] **GAP-002 已决** — BatchManager 共享实现、不共享实例
- [x] **GAP-005 已决** — 容量按主链路优先分层收拢


### Phase 1 启动需要（Phase 0 完成后）：
- [ ] Phase 0 编译通过 + Demo 回归正常
- [x] **NEW-001 已决** — BulletTypeSO 独立贴图优先并保留 UV 表达
- [x] **MISS-004 已决** — VFX 支持独立贴图，atlas 仅为可选优化



### Phase 2 启动需要（Phase 1 完成后）：
- [ ] Phase 1 编译通过 + 多贴图渲染验证
- [x] **GAP-003 已决** — Buffer 仅作旁路消费，主消费点固定在 DanmakuSystem
- [x] **GAP-004 已决** — MotionRegistry 采用受控注册表
- [x] **GAP-006 已决** — DanmakuSystem 保留 Facade，内部拆职责


### Phase 3 启动需要（Phase 2 完成后）：
- [ ] Phase 2 编译通过 + 事件系统验证
- [x] **NEW-004 已决** — VFX 附着模式的架构边界已确定为 `World / FollowTarget / Socket`，其中本轮至少落 `World + FollowTarget`，`Socket` 先定契约后补实现
- [ ] `IAttachSourceResolver`（或等价命名）的接口归属在代码中一次定准，不回退为直接传 `Transform`
- [ ] `PlayAttached / UpdateAttached / StopAttached` 三段式语义已在代码与验收用例中对齐：创建、跟随、停止各司其职；Resolver 失败、重复 Stop、无效 handle 均按幂等且可观测规则处理；同一 `AttachSourceId + VFXType` 的重复 `PlayAttached` 必须按统一规则处理（默认先停止旧 handle 再创建新 handle，禁止旧 handle 隐式并存）
- [ ] 已覆盖“目标失效冻结后目标恢复但旧 handle 不自动恢复跟随”的验收用例，恢复必须通过重新 `PlayAttached` 完成


### Phase 4 启动需要：
- [ ] Phase 3 编译通过 + 全功能 Demo 验证
- [ ] 迁移器 `dry-run -> apply -> report` 工作流已形成统一实现与验收输出
  - `dry-run` 必须输出：待迁移资产数、风险项、缺失引用、prefab/scene 实例扫描结果
  - `dry-run` 必须按“阻断错误 / 警告”分级输出结果
  - 阻断错误至少包括：缺失 `SourceTexture`、非法 `Static + PlaybackMode`、非法 `SpriteSheet + Reverse/PingPong/RandomStartFrame`、prefab/scene 实例引用断裂、共享契约字段缺失导致无法生成合法注册项
  - 警告至少包括：旧字段仍存在但可自动补齐、atlas 映射缺失但仍可回退到 `SourceTexture + UVRect`、默认值可自动修正但需要归档说明
  - `apply` 只允许处理已通过预检的数据集
  - `report` 必须区分阻断错误与警告，且作为可归档验收产物保留
  - 阻断错误禁止进入 apply；警告允许 apply 但不得在 report 中缺席
  - migration 边界必须保持在资产层 schema 升级；`OnValidate` 不得承担跨版本迁移，runtime 也不得承担长期兼容修补
  - 兼容退出机制必须满足：基线资产集 + prefab/scene 实例扫描的阻断错误均为 0，且已完成一次正式 `report` 归档；满足后下一 schema 版本必须删除旧兼容字段与运行时 fallback
- [ ] 编辑器刷新链路（资源变更 → Registry 重建 → Batch 预热）已具备失败回退与显式报错

  - Registry 重建失败：不得进入 Batch 预热，保留旧运行时状态
  - Batch 预热失败：本次刷新整体视为失败，不允许“部分成功但静默继续”
  - Atlas 工具、迁移器、Inspector 修改、批量工具全部走同一 orchestration，不允许私有刷新捷径

### 六点最终落地核查（最后一次文档定稿后新增）
- [ ] `RenderLayer` 只表达语义分层；`RenderSortingOrder` 是 sortingOrder 唯一代码来源；`MaterialKey/BlendMode` 映射由共享渲染层统一维护
- [ ] Laser 已按“共享渲染基础设施消费者”落地：遵守 `RenderLayer`、`RenderSortingOrder`、`MaterialKey/BlendMode`，但未偷偷绕回统一资源描述值对象
- [ ] 序列帧子弹已按唯一时间源规则落地：`StretchToLifetime -> lifetime/maxLifetime`，`FixedFpsLoop/Once -> elapsedSeconds`，禁止双时间源混用；`ResolveBulletUV()` 只负责 UV 解析
- [ ] attached VFX 已按唯一语义落地：`PlayAttached / UpdateAttached / StopAttached` 强制三段式；同一 `AttachSourceId + VFXType` 重复 Play 先停旧再建新，不允许一帧并存
- [ ] `CollisionEventBuffer` 与 `EffectsBridge` 已守住旁路边界：不承载主逻辑事实、不反向改主状态；性能验收窗口内 `overflow count > 0` 直接判失败
- [ ] 范围控制总表已明确“第一版必须做 / 允许做但非阻塞 / 明确不做 / 未来扩展点”，实现中无顺手扩 scope 行为



---

## 6.5 软件架构师补充闭环（2026-04-11 夜间）

基于同日软件架构师复核，以下 6 项从“执行疑问”升级为“正式约束”，不再作为开放讨论项：

1. **RenderBatchManager 桶生命周期已定**
   - 初始化按注册表预热
   - 运行时禁止隐式建桶
   - 未知贴图在开发期报错计数，运行时跳过渲染

2. **Bullet/VFX 资源描述统一方式已定**
   - 统一资源描述值对象语义：`SourceTexture + UVRect + MaterialKey/BlendMode + 可选 AtlasBinding`
   - 不强行统一 Bullet/VFX 的全部行为字段

3. **Atlas 输出协议已定**
   - Atlas 是可逆派生产物，不是源数据真相
   - 工具输出显式映射资产，回写 SO 仅为可选能力
   - Bullet/VFX/DamageNumber atlas 分域维护，不混打

4. **CollisionEventBuffer 溢出语义已定**
   - Buffer 是可丢的表现/联动/观察通道
   - 溢出不影响伤害、击退、死亡、状态变更
   - 必须记录 overflow count，接 profiler/debug HUD

5. **VFX FollowTarget 句柄模型已定**
   - 默认使用 `AttachSourceId`，不直接绑定 `Transform`
   - 目标失效默认冻结到最后有效位置并播完
   - `Socket` 契约先定义，完整实现可后置

6. **容量配置化边界已定**
   - 以显式范围表控制本轮纳入项
   - 未列入项默认不在本轮范围内，避免执行期 scope 漂移

**审查结论更新**：
- 上述 6 项补齐后，本轮计划剩余问题主要转为“按约束实现”，不再属于架构边界未定
- 当前真正需要盯的是执行顺序、迁移器质量和性能验收口径，而不是继续扩方案
- 当前状态应统一表述为：**架构级阻塞项已清零，剩余为执行级验收约束**
- 对其他 agent 的复审也应统一按此口径理解：后续若指出问题，应优先归类为“实现是否守约”或“验收是否可执行”，而不是把已定边界重新打回开放决策

## 七、结论


**方案整体是好的**。六项决策自洽，四个 Phase 的拆分粒度合理。

**但直接动手的阻塞项有 3 个**：
1. **GAP-001**（RenderLayer）— Phase 0 第一步就需要
2. **GAP-002**（BatchManager 归属）— 影响 Phase 0 的目录结构和 Phase 1 的核心实现
3. **GAP-003**（CollisionEventBuffer 消费）— Phase 2 的核心设计问题

**新发现 5 个补充设计点**（NEW-001~005）和 **7 个文档遗漏**（MISS-001~007），其中大部分已被转化为执行约束。


**总工时评估**：
- 文档预估 8-14 天 Agent 工时
- 审计后修正：考虑到 GAP-005（容量收拢比预期复杂）和 MISS-007（Phase 2.6+2.7 同步执行限制），实际预估 **10-16 天**

**快速决策建议**（定掉这些 Phase 0 就能明天开工）：
- GAP-001 → **方案 A**（新建统一 RenderLayer）
- GAP-002 → **方案 C**（各自实例）
- MISS-004 → VFX **支持独立贴图 / atlas 仅可选优化**


---

*本文档应与 REFACTOR_PLAN.md 配合阅读。如果上述建议被采纳，REFACTOR_PLAN 需做对应更新。*
