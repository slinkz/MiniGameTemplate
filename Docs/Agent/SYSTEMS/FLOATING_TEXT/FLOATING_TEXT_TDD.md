---
system: rendering
scope: floating-text-unification
last_verified: 2026-06-03
depends_on: [ADR_06_LIFECYCLE, EC_TDD_04_SYSTEMS, ATLAS_TDD_01_DESIGN]
related_code:
  - Assets/_Framework/Rendering/FloatingTextSystem.cs
  - Assets/_Framework/Rendering/FloatingTextData.cs
  - Assets/_Framework/DanmakuSystem/Scripts/DanmakuSystem.Runtime.cs
  - Assets/_Framework/DanmakuSystem/Scripts/DanmakuSystem.UpdatePipeline.cs
  - Assets/_Framework/EntitySystem/Scripts/View/EntityHitReactionHandler.cs
  - Assets/_Framework/EntitySystem/Scripts/Core/EntitySystemBootstrap.cs
  - Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs
---

# 飘字系统统一重构 TDD

> **版本**：v2.3（PK-CR R2 回写） | **日期**：2026-06-03  
> **触发**：双飘字 Bug（弹幕层白色 + Entity 层黄色同时显示同一次命中）  
> **目标**：合并两套飘字实现为单一通用系统，消除数据不一致和重复渲染  
> **ADR**：ADR-036（本文档 §2 定义）  
> **PK-1**：3 轮收敛，10 问题全部解决（Unity 架构师 vs 软件架构师） | [PK 记录](FLOATING_TEXT_PK.md)  
> **PK-2**：3 轮收敛，10 问题全部解决（代码评审专家 vs 软件架构师） | [PK-CR 记录](FLOATING_TEXT_PK_CR.md)

---

## 目录

1. [问题诊断](#1-问题诊断)
2. [ADR-036：飘字系统统一到 RBM 渲染管线](#2-adr-036)
3. [方案设计](#3-方案设计)
4. [接口规约](#4-接口规约)
5. [数据结构](#5-数据结构)
6. [调用方迁移](#6-调用方迁移)
7. [渲染管线集成](#7-渲染管线集成)
8. [实施步骤](#8-实施步骤)
9. [验收标准](#9-验收标准)
10. [风险与缓解](#10-风险与缓解)
11. [废弃清理](#11-废弃清理)

---

## 1. 问题诊断

### 1.1 现状：两套并行飘字系统

| 维度 | 弹幕层 `DamageNumberSystem` | Entity 层 `EntityHitReactionHandler` |
|---|---|---|
| **命名空间** | `MiniGameTemplate.Danmaku` | `MiniGameTemplate.Entity` |
| **渲染方式** | RBM + 数字贴图 UV 拼字（纯 GPU，零 GC） | TextMesh 对象池（GameObject 生命周期管理） |
| **容量** | 128（环形缓冲区覆盖写） | 32（硬上限，满则丢弃） |
| **动画** | 随机 X 偏移 + 减速上浮 + 透明度淡出 | 纯线性上浮 + 缩放衰减（无淡出） |
| **颜色** | 白色（普通）/ 金色（暴击，硬编码 `damage >= 10`） | Prefab 默认金黄色 `(1, 0.9, 0.1)` / 紫色（DOT，调用方传入） |
| **暴击判定** | `damage >= 10`（数值硬编码） | `context.IsCritical`（语义化布尔） |
| **时间源** | `Time.unscaledDeltaTime`（LateUpdate Rebuild） | `Time.unscaledDeltaTime`（Tick 内部） |
| **生命周期** | DanmakuSystem（DontDestroyOnLoad 全局单例） | EntitySystemBootstrap（场景级 MonoBehaviour） |
| **GC** | **零**（struct 环形缓冲区，无 ToString） | **每次飘字 ToString + GetComponent**（低但非零） |
| **调用入口** | `UpdatePipeline` 内部碰撞结果（1 处） | `OnHit` 内部（1 处）+ `BattleController.OnEnemyDamaged`（1 处，DOT） |

### 1.2 Bug 根因

同一次命中可同时触发两套飘字——弹幕碰撞结果在 `UpdatePipeline` 触发 `DamageNumberSystem.Spawn`，随后 `CollisionEventBuffer` 事件回调到 `EntityHitReactionHandler.OnHit` 又触发 `SpawnDamageNumber`。玩家看到 **两个不同数值的飘字重叠**（因为数据来源不同：`PlayerDamage` 累加值 vs `FinalDamage` 单次值）。

### 1.3 统一的必要性

- **调试困难**：排查飘字问题需同时追踪两条独立管线
- **数据不一致**：两套系统各自拿到的 damage 数值可能不同
- **浪费资源**：Entity 层的 TextMesh 对象池在有 RBM 渲染的情况下完全多余
- **配色不统一**：弹幕层白色 vs Entity 层金黄色，玩家困惑

---

## 2. ADR-036

### ADR-036：飘字系统统一到 RBM 渲染管线

- **日期**：2026-06-03
- **状态**：提议
- **触发**：双飘字 Bug + 飘字系统排查耗时过长

#### 上下文

项目中存在两套独立的伤害飘字系统，各有独立的渲染管线、数据结构、颜色方案。它们服务同一个用户需求——在屏幕上显示伤害数字——但互不知晓对方的存在，导致重复渲染和数据不一致。

#### 决策

**将两套飘字系统合并为一套，基于 `DamageNumberSystem`（RBM + 环形缓冲区）升级。**

选择 RBM 方案而非 TextMesh 方案的理由：
1. **零 GC**：struct 环形缓冲区 + 纯 GPU 渲染，无 `ToString()`、无 `GetComponent`
2. **容量弹性**：环形缓冲区 128 容量覆盖写，不丢帧、不阻塞
3. **表现更丰富**：已有透明度淡出 + 随机 X 偏移 + 减速上浮
4. **无 GameObject 依赖**：不需要 PoolManager、不需要 Prefab、不需要 TextMesh 组件

**放弃什么**：
- Entity 层 TextMesh 飘字的 `!` 暴击后缀（改为颜色 + 缩放区分）
- TextMesh 的任意字体支持（RBM 使用数字贴图，只支持 0-9）

**为什么不反过来（统一到 TextMesh）**：
- TextMesh 每帧有 GC（`ToString`）
- 对象池管理复杂度高于环形缓冲区
- 容量天花板低（32 vs 128）
- 在弹幕射击游戏中飘字频率极高，性能差距会放大

#### 后果

- ✅ 飘字只有一个排查入口
- ✅ 颜色方案统一可配
- ✅ 删除约 120 行 EntityHitReactionHandler 飘字代码 + DamageNumber Prefab
- ⚠️ Entity 层新增对 DanmakuSystem 的弱运行时引用（通过 `FloatingTextSystem` 公开属性）
- ⚠️ 暴击标识从文字 `!` 变为颜色+缩放

---

## 3. 方案设计

### 3.1 架构变更

```
改造前：
┌─────────────────┐          ┌───────────────────────────┐
│  DanmakuSystem  │          │ EntityHitReactionHandler   │
│  ├ DamageNumber │          │  ├ DamageNumberState[32]   │
│  │  System      │          │  ├ PoolManager + TextMesh  │
│  │  (RBM)       │          │  └ SpawnDamageNumber()     │
│  └ Spawn()      │          └───────────────────────────┘
│    ↑ 碰撞结果    │                     ↑ OnHit + DOT
└─────────────────┘
      独立管线                        独立管线

改造后：
┌───────────────────────────────────────┐
│      FloatingTextSystem (通用飘字)      │
│  namespace: MiniGameTemplate.Rendering │
│  渲染: RBM + 数字贴图 + 环形缓冲区 128  │
│  API: Spawn(pos, damage, color, crit)  │
│  生命周期: DanmakuSystem 持有+驱动      │
│  公开: DanmakuSystem.FloatingText 属性  │
└────────────┬──────────────┬───────────┘
             │              │
    ┌────────┴──────┐  ┌───┴──────────────┐
    │ UpdatePipeline │  │ EntityHitReaction │
    │ 碰撞→Spawn()   │  │ OnHit→Spawn()    │
    └───────────────┘  │ + BattleController│
                       │   DOT→Spawn()     │
                       └──────────────────┘
```

### 3.2 文件布局

| 操作 | 旧路径 | 新路径 |
|------|--------|--------|
| **复制+删旧**（§8 Phase 1 复制，Phase 3 删除原件） | `DanmakuSystem/Scripts/Core/DamageNumberSystem.cs` | `_Framework/Rendering/FloatingTextSystem.cs` |
| **复制+删旧** | `DanmakuSystem/Scripts/Data/DamageNumberData.cs` | `_Framework/Rendering/FloatingTextData.cs` |
| **修改** | `DanmakuSystem/Scripts/DanmakuSystem.Runtime.cs` | 字段类型 + 初始化 |
| **修改** | `DanmakuSystem/Scripts/DanmakuSystem.UpdatePipeline.cs` | 调用方法名 |
| **修改** | `DanmakuSystem/Scripts/DanmakuSystem.API.cs` | 新增公开属性 |
| **修改** | `EntitySystem/Scripts/View/EntityHitReactionHandler.cs` | **删除** 飘字相关代码 |
| **修改** | `EntitySystem/Scripts/Core/EntitySystemBootstrap.cs` | 构造函数参数变更 |
| **修改** | `_Game/Scripts/ShooterGame/Core/BattleController.cs` | DOT 飘字调用方式 |
| **废弃** | `_Game/Prefabs/Debug/DamageNumber.prefab` | 标记删除 |

### 3.3 命名空间决策

`FloatingTextSystem` 放入 `MiniGameTemplate.Rendering`（而非 `Danmaku`），因为：
- 飘字是**纯渲染层**关注点，不属于弹幕业务逻辑
- `Rendering` 命名空间已有 `RenderBatchManager`、`RenderVertex`、`RenderSortingOrder` 等基础设施
- Entity 层引用 `Rendering` 命名空间不产生对 `Danmaku` 的依赖

但 `FloatingTextSystem` 的**初始化仍依赖** `DanmakuRenderConfig` + `RuntimeAtlasManager`，这是物理耦合——选择接受，因为这两个类型也在 `Rendering` 命名空间下（`RuntimeAtlasManager` 在 `MiniGameTemplate.Rendering`）。`DanmakuRenderConfig` 仍在 `Danmaku` 命名空间，所以 `FloatingTextSystem` 的 `Initialize` 方法签名需要 `using MiniGameTemplate.Danmaku`——这是可接受的初始化时依赖。

> **PK-R2 UA-008 asmdef 分析**：`DanmakuRenderConfig`、`RuntimeAtlasManager`、`RenderBatchManager`、`FloatingTextSystem` 均在 `MiniGameFramework.Runtime` 同一个 asmdef 内（`Assets/_Framework/MiniGameFramework.Runtime.asmdef`）。无跨程序集引用问题，不需要修改 asmdef 依赖。

---

## 4. 接口规约

### 4.1 `FloatingTextSystem` 公开 API

```csharp
namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 通用飘字系统——环形缓冲区 + RBM 纯 GPU 渲染。
    /// 独立于 Entity/Danmaku 业务逻辑，任何系统均可通过 Spawn 显示飘字。
    /// 
    /// 生命周期由 DanmakuSystem 管理：
    ///   Initialize → 每帧 Rebuild → ClearAll → Dispose
    /// </summary>
    public class FloatingTextSystem
    {
        public const int MAX_NUMBERS = 128;

        /// <summary>当前活跃飘字产生的 Quad 总数（Debug HUD 用）</summary>
        public int TotalDrawCount { get; }

        /// <summary>
        /// 初始化渲染资源。由 DanmakuSystem.InitializeSubsystems() 调用。
        /// </summary>
        /// <param name="renderConfig">弹幕渲染配置（提供 NumberAtlas + BulletMaterial）</param>
        /// <param name="sharedAtlas">共享 RuntimeAtlasManager（可选，null 则 fallback 原始贴图）</param>
        public void Initialize(DanmakuRenderConfig renderConfig, 
                               RuntimeAtlasManager sharedAtlas = null);

        /// <summary>
        /// 生成一个伤害飘字。线程安全：否（仅主线程调用）。
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <param name="damage">伤害数值（0~99999，超出截断为 5 位）</param>
        /// <param name="color">飘字颜色</param>
        /// <param name="isCritical">暴击标记（true → 1.5x 缩放 + 放大动画）</param>
        public void Spawn(Vector2 position, int damage, 
                          Color32 color, bool isCritical = false);

        /// <summary>
        /// 每帧更新位置/透明度 + 重建 GPU 批次。
        /// 由 DanmakuSystem.RunLateUpdatePipeline() 调用，传入 unscaledDeltaTime。
        /// </summary>
        public void Rebuild(float dt);

        /// <summary>清除所有活跃飘字（战斗退场/关卡切换）。</summary>
        public void ClearAll();

        /// <summary>释放 GPU 资源。</summary>
        public void Dispose();
    }
}
```

### 4.2 关键变更：`Spawn` 签名

**旧签名**（`DamageNumberSystem`）：
```csharp
public void Spawn(Vector2 position, int damage, bool isCritical = false)
// 颜色硬编码在方法内部
```

**新签名**（`FloatingTextSystem`）：
```csharp
public void Spawn(Vector2 position, int damage, Color32 color, bool isCritical = false)
// 颜色由调用方决定，系统不做假设
```

**理由**：颜色语义属于业务层（普攻白色、DOT 紫色、治疗绿色等），飘字系统只负责"以指定颜色渲染数字"。

### 4.3 DanmakuSystem 公开属性

```csharp
// DanmakuSystem.API.cs 新增
/// <summary>
/// 通用飘字系统（只读引用，供 Entity 层等外部系统调用 Spawn）。
/// 生命周期由 DanmakuSystem 内部管理，外部只调用 Spawn。
/// </summary>
public FloatingTextSystem FloatingText => _floatingText;
```

### 4.4 预定义颜色常量

为避免颜色魔法数字散落各处，在 `FloatingTextSystem` 中提供推荐常量：

```csharp
public static class FloatingTextColors
{
    public static readonly Color32 Normal    = new Color32(255, 255, 255, 255); // 白色
    public static readonly Color32 Critical  = new Color32(255, 200, 50, 255);  // 暴击金
    public static readonly Color32 Dot       = new Color32(153, 51, 255, 255);  // DOT 紫
    public static readonly Color32 Heal      = new Color32(50, 255, 100, 255);  // 治疗绿（预留）
}
```

> **PK-R1 UA-006 迭代项**：当前 `static readonly` 作为 v1.0 实现。编码期间可迭代升级为 SO 配置（在 `DanmakuRenderConfig` 中增加 `FloatingTextColorConfig` 字段），使设计师可在 Inspector 中实时调色。`static readonly` 值保留为 fallback 默认值。

---

## 5. 数据结构

### 5.1 `FloatingTextData`（原 `DamageNumberData`）

```csharp
namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 飘字运行时数据。由 FloatingTextSystem 环形缓冲区管理。
    /// </summary>
    public struct FloatingTextData
    {
        public Vector2 Position;     // 当前位置
        public Vector2 Velocity;     // 飘动速度
        public float Lifetime;       // 总生命周期（0 = 无效槽位）
        public float Elapsed;        // 已过时间
        public int Damage;           // 伤害数值
        public byte DigitCount;      // 位数（预计算）
        public byte Flags;           // bit0=暴击
        public float Scale;          // 缩放（暴击 1.5x）
        public Color32 Color;        // 飘字颜色（调用方指定）
    }
}
```

与原 `DamageNumberData` 的差异：**字段定义相同**，但 `Color` 字段的赋值语义变更——旧系统在 `Spawn()` 内部由 `isCritical` 硬编码颜色，新系统由调用方显式传入 `Color32 color` 参数直接赋值。实施时需注意 `Spawn` 方法内部删除颜色 if/else 分支。

> **PK-CR R1 CR-005 假设注释**：`Position` 为 `Vector2`，假设纯 2D 游戏（z=0）。`WriteNumber` 内部将 z 硬编码为 0f。如需支持 z 分层渲染，需升级为 `Vector3` 并同步修改 `WriteNumber`。标注为远期迭代项。

---

## 6. 调用方迁移

### 6.1 弹幕层（`DanmakuSystem.UpdatePipeline.cs`）

**改动前**（第 60 行）：
```csharp
_damageNumbers.Spawn(result.PlayerHitPosition, result.PlayerDamage, 
                     result.PlayerDamage >= 10);
```

**改动后**：
```csharp
var color = result.PlayerDamage >= 10 
    ? FloatingTextColors.Critical 
    : FloatingTextColors.Normal;
_floatingText.Spawn(result.PlayerHitPosition, result.PlayerDamage, 
                    color, result.PlayerDamage >= 10);
```

**注意**：暴击判定条件 `damage >= 10` 是弹幕层的业务逻辑（"高伤害视为暴击"），保留在调用方而非下沉到飘字系统。

### 6.2 Entity 层 OnHit（`EntityHitReactionHandler.cs`）

**改动前**（第 154-159 行）：
```csharp
if (config.ShowDamageNumber && _damageNumberPool != null && _poolManager != null)
{
    int displayDmg = context.FinalDamage > 0 ? context.FinalDamage : context.BaseDamage;
    SpawnDamageNumber(entity.Position, displayDmg, context.IsCritical);
}
```

**改动后**：
```csharp
if (config.ShowDamageNumber && _floatingText != null)
{
    int displayDmg = context.FinalDamage > 0 ? context.FinalDamage : context.BaseDamage;
    var color = context.IsCritical 
        ? FloatingTextColors.Critical 
        : FloatingTextColors.Normal;
    // PK-R2 UA-009：保持与旧 TextMesh 飘字一致的 +0.5f Y 偏移
    _floatingText.Spawn(entity.Position + new Vector2(0, 0.5f), displayDmg, color, context.IsCritical);
}
```

### 6.3 DOT 飘字（`BattleController.cs`）

**改动前**（第 550 行附近）：
```csharp
private static readonly Color DOT_DAMAGE_COLOR = new Color(0.6f, 0.2f, 1f);

if (evt.SourceId >= 100 && _entityBootstrap != null)
{
    _entityBootstrap.HitReactionHandler.SpawnDamageNumber(
        evt.TargetPosition, evt.Damage, false, DOT_DAMAGE_COLOR);
}
```

**改动后**：
```csharp
// PK-R1 UA-003：统一走构造注入，不走 DanmakuSystem.Instance 单例
// PK-R2 UA-010：保留 _entityBootstrap null 安全检查
if (evt.SourceId >= 100 && _entityBootstrap != null)
{
    _entityBootstrap.FloatingText?.Spawn(
        evt.TargetPosition, evt.Damage, FloatingTextColors.Dot, false);
}
```

**注意**：DOT 飘字不再经过 `EntityHitReactionHandler`，直接通过 Bootstrap 公开的 `FloatingText` 属性调用通用飘字系统。路径更短、依赖更少、无单例访问。

### 6.4 依赖注入路径

Entity 层获取 `FloatingTextSystem` 引用的方式：

| 方案 | 实现 | 耦合度 | 推荐 |
|------|------|--------|------|
| A. DanmakuSystem.Instance.FloatingText | 单例访问 | Entity→Danmaku 弱引用 | ❌ 不推荐（违反 coding-standards §3 + §7） |
| B. Bootstrap 属性 + 构造注入 | Bootstrap.Awake 查找并注入，公开只读属性 | 构造时确定 | ⭐ 最优（选用） |
| C. 全局静态 FloatingTextSystem.Instance | 自持静态单例 | 最低 | ❌ 不推荐（生命周期不可控） |

**选择方案 B**：在 `EntitySystemBootstrap.Awake()` 创建 `EntityHitReactionHandler` 时，通过构造函数传入 `FloatingTextSystem` 引用。同时 Bootstrap 公开 `FloatingText` 只读属性供 `BattleController` 等游戏层调用。

```csharp
// EntitySystemBootstrap.Awake() 改造
// PK-CR R2 CR-009：Unity Object 不用 ?.（PIT-034 fake-null），改为显式 != null
var danmaku = FindObjectOfType<DanmakuSystem>();
var floatingText = (danmaku != null) ? danmaku.FloatingText : null;
_hitHandler = new EntityHitReactionHandler(PoolManager.Instance, floatingText);

// 公开属性（PK-R1 UA-003：DOT 飘字等游戏层调用入口）
public FloatingTextSystem FloatingText { get; private set; }
// 在 Awake 中赋值：FloatingText = floatingText;
```

**构造函数变更**：
```csharp
// 旧
public EntityHitReactionHandler(PoolManager poolManager, PoolDefinition damageNumberPool)

// 新
public EntityHitReactionHandler(PoolManager poolManager, FloatingTextSystem floatingText)
```

> **PK-CR R1 CR-001 范围声明**：本次重构**仅迁移飘字相关**的 `DanmakuSystem.Instance` 访问（BattleController DOT 飘字）。弹幕发射（`FireBulletsEffect`、`EnemyShootComponent`）、碰撞注册（`CollisionComponent`）、激光/喷雾控制等保留 `DanmakuSystem.Instance` 单例模式——DanmakuSystem 本身是 DontDestroyOnLoad 合法全局单例，§6.4 表格中的"❌ 不推荐"仅针对飘字注入路径选型，不波及弹幕业务 API。

---

## 7. 渲染管线集成

### 7.1 DanmakuSystem.Runtime.cs

**字段变更**：
```csharp
// 旧
private DamageNumberSystem _damageNumbers;

// 新
private FloatingTextSystem _floatingText;
```

**InitializeSubsystems() 变更**：
```csharp
// 旧（第 92-93 行）
_damageNumbers = new DamageNumberSystem();
_damageNumbers.Initialize(_renderConfig, _sharedAtlas);

// 新
_floatingText = new FloatingTextSystem();
_floatingText.Initialize(_renderConfig, _sharedAtlas);
```

**DisposeSubsystems() 变更**：
```csharp
// 旧
_damageNumbers?.Dispose();

// 新
_floatingText?.Dispose();
```

### 7.2 DanmakuSystem.UpdatePipeline.cs

> **PK-CR R1 CR-006 时序说明**：`Spawn()` 发生在 `Update` 阶段（Entity Tick / 碰撞回调）→ `Rebuild(dt)` 发生在 `LateUpdate` 阶段（`RunLateUpdatePipeline`）。由于 Unity 帧内执行顺序 Update < LateUpdate，**同帧生成的飘字会在当帧 LateUpdate 就被 Rebuild 处理并渲染**，无一帧延迟。删除 `EntityHitReactionHandler.TickDamageNumbers` 后不引入额外 latency。

**RunLateUpdatePipeline() 变更**（第 90 行）：
```csharp
// 旧
_damageNumbers.Rebuild(Time.unscaledDeltaTime);

// 新
_floatingText.Rebuild(Time.unscaledDeltaTime);
```

### 7.3 DanmakuSystem.API.cs

**ClearAll() 变更**（第 229 行）：
```csharp
// 旧
_damageNumbers.ClearAll();

// 新
_floatingText.ClearAll();
```

**新增公开属性**：
```csharp
/// <summary>通用飘字系统引用</summary>
public FloatingTextSystem FloatingText => _floatingText;
```

### 7.4 渲染排序不变

`RenderSortingOrder.DamageNumber` 仍然有效，`FloatingTextSystem` 继续使用该排序值。

> **PK-CR R1 CR-003 迭代项**：枚举值保持 `DamageNumber` 不改名（避免 diff 扩散到渲染管线），后续版本可统一重命名为 `FloatingText`。当前命名是历史遗留，不影响功能。

---

## 8. 实施步骤

> 每步均需编译通过后再推进下一步（门禁 = 编译绿灯）。

### Phase 1：创建 FloatingTextSystem（~30 分钟）

| # | 操作 | 文件 | 验证 |
|---|------|------|------|
| 1.1 | **复制** `DamageNumberSystem.cs` → `_Framework/Rendering/FloatingTextSystem.cs` | 新文件 | 文件存在 |
| 1.2 | **重命名类**：`DamageNumberSystem` → `FloatingTextSystem` | FloatingTextSystem.cs | — |
| 1.3 | **修改命名空间**：`MiniGameTemplate.Danmaku` → `MiniGameTemplate.Rendering` | FloatingTextSystem.cs | — |
| 1.4 | **修改 `Spawn` 签名**：新增 `Color32 color` 参数（第三位），删除内部颜色硬编码 | FloatingTextSystem.cs | — |
| 1.5 | **Spawn 内部**：`data.Color = color;`（直接赋值，不再 if/else） | FloatingTextSystem.cs | — |
| 1.6 | **复制** `DamageNumberData.cs` → `_Framework/Rendering/FloatingTextData.cs` | 新文件 | — |
| 1.7 | **重命名 struct**：`DamageNumberData` → `FloatingTextData`，命名空间改 `Rendering` | FloatingTextData.cs | — |
| 1.8 | **更新 FloatingTextSystem 内部引用**：`DamageNumberData` → `FloatingTextData` | FloatingTextSystem.cs | — |
| 1.9 | **新增 `FloatingTextColors` 静态类**（在 FloatingTextSystem.cs 底部或独立文件） | FloatingTextSystem.cs | — |
| 1.10 | **删除废弃的 `GetAtlasStats()` 方法**（已标记 `[Obsolete]`，新系统不继承旧包袱）。**PK-CR CR-010**：前置 grep `_damageNumbers.GetAtlasStats` / `DamageNumberSystem.*GetAtlasStats` 确认 = 0 外部调用 | FloatingTextSystem.cs | — |
| 1.11 | **编译验证** | — | ✅ 0 error |

**门禁**：编译通过。此时 DamageNumberSystem 仍存在但不再被使用（将在 Phase 3 清理）。

### Phase 2：接入 DanmakuSystem（~30 分钟）

| # | 操作 | 文件 | 验证 |
|---|------|------|------|
| 2.1 | `DanmakuSystem.Runtime.cs`：字段 `_damageNumbers` → `_floatingText`（类型 `FloatingTextSystem`） | Runtime.cs | — |
| 2.2 | `InitializeSubsystems()`：创建 `FloatingTextSystem` 替代 `DamageNumberSystem` | Runtime.cs | — |
| 2.3 | `DisposeSubsystems()`：`_floatingText?.Dispose()` | Runtime.cs | — |
| 2.4 | `DanmakuSystem.API.cs`：新增 `public FloatingTextSystem FloatingText => _floatingText;` | API.cs | — |
| 2.5 | `DanmakuSystem.API.cs` ClearAll：`_floatingText.ClearAll()` | API.cs | — |
| 2.6 | `UpdatePipeline.cs` Spawn 调用：改颜色参数 + 字段名。**PK-CR CR-007**：grep 全项目 `_damageNumbers.Spawn` / `DamageNumberSystem.*Spawn` 确认 = 1 处调用 | UpdatePipeline.cs | — |
| 2.7 | `UpdatePipeline.cs` Rebuild 调用：`_floatingText.Rebuild(...)` | UpdatePipeline.cs | — |
| 2.8 | **编译验证** | — | ✅ 0 error |

**门禁**：编译通过 + Play Mode 弹幕碰撞飘字正常显示。

### Phase 3：迁移 Entity 层 + 废弃清理（~60 分钟，原子步骤）

> PK-R1 UA-002：Phase 3+4 合并为原子步骤——coding-standards §13 要求系统退役必须物理删除，
> 禁止"先标 Obsolete 以后再删"。迁移→清空引用→删除旧代码在同一个编译门禁周期内完成。

| # | 操作 | 文件 | 验证 |
|---|------|------|------|
| 3.1 | `EntityHitReactionHandler` 构造函数：`PoolDefinition damageNumberPool` → `FloatingTextSystem floatingText` | Handler.cs | — |
| 3.2 | **删除字段**：`_damageNumberPool`、`_damageNumbers[]`、`_damageNumberCount` | Handler.cs | — |
| 3.2a | **PK-CR CR-002 前置验证**：grep 全项目 `SpawnDamageNumber`，确认调用者 = 2（Handler.OnHit 内部 + BattleController.OnEnemyDamaged），无其他外部引用 | grep | 结果 = 2 |
| 3.3 | **删除方法**：`SpawnDamageNumber()`、`TickDamageNumbers()` | Handler.cs | — |
| 3.4 | **删除内部 struct**：`DamageNumberState` | Handler.cs | — |
| 3.5 | **新增字段**：`private readonly FloatingTextSystem _floatingText;` | Handler.cs | — |
| 3.6 | **修改 OnHit**：飘字逻辑改为调用 `_floatingText.Spawn(...)` | Handler.cs | — |
| 3.7 | **修改 Tick**：删除 `TickDamageNumbers` 调用 | Handler.cs | — |
| 3.8 | **修改 ClearAll**：删除飘字 GO 回收逻辑（RBM 无 GO） | Handler.cs | — |
| 3.9 | `EntitySystemBootstrap.Awake()`：查找 DanmakuSystem 获取 FloatingText 引用，新增 `public FloatingTextSystem FloatingText` 只读属性。**PK-CR CR-004/CR-009**：使用显式 `!= null` 判断（不用 `?.`，PIT-034 fake-null），即使 DanmakuSystem 存在但未初始化，也安全降级为 null。 | Bootstrap.cs | — |
| 3.10 | `EntitySystemBootstrap`：**物理删除** `DamageNumberPool` 字段（同步清空所有 .asset 中的序列化引用） | Bootstrap.cs + .asset | — |
| 3.11 | `BattleController.OnEnemyDamaged`：DOT 飘字改为 `_entityBootstrap.FloatingText?.Spawn(...)` | BattleController.cs | — |
| 3.12 | **删除** `DanmakuSystem/Scripts/Core/DamageNumberSystem.cs` + `.meta` | 删除 | — |
| 3.13 | **删除** `DanmakuSystem/Scripts/Data/DamageNumberData.cs` + `.meta` | 删除 | — |
| 3.14 | **删除** `_Game/Prefabs/Debug/DamageNumber.prefab` + `.meta`。**PK-CR CR-008**：前置 grep `Template_DamageNumberPool` 确认全项目引用 = 1（Bootstrap Inspector），无其他 SO 引用同一 Prefab | 删除 | — |
| 3.15 | **删除** `_Game/Configs/_Template/Pool/Template_DamageNumberPool.asset` + `.meta` | 删除 | — |
| 3.16 | **编译验证** | — | ✅ 0 error, 0 warning |

**门禁**：编译通过 + 全项目 grep `DamageNumberSystem` / `SpawnDamageNumber` / `DamageNumberData` / `DamageNumberPool` 全部返回 0 结果。

---

## 9. 验收计划

> PK-R1 UA-001 修正：拆分为两层验收体系。

### 9.1 Phase 门禁验收（每 Phase 完成时必须通过）

> **原则**：只列"不通过则下一 Phase 无法启动"的阻塞项。
> **要求**：所有验收项在当前 Phase 环境下可执行（无需额外 UI/美术/真机）。

| ID | Phase | 验收项 | 验收手段 | 通过标准 |
|----|-------|--------|----------|----------|
| G-1 | 1 | FloatingTextSystem.cs + FloatingTextData.cs 编译通过 | 编译 | 0 error |
| G-2 | 1 | `Spawn` 签名含 `Color32 color` 参数 | 代码检查 / grep | 签名匹配 §4.1 |
| G-3 | 2 | DanmakuSystem 字段切换到 `_floatingText` | 代码检查 | 无 `_damageNumbers` 引用 |
| G-4 | 2 | 弹幕碰撞飘字走 FloatingTextSystem | MCP execute_code 触发碰撞 | 飘字 Spawn 被调用 |
| G-5 | 3 | EntityHitReactionHandler 构造函数接受 `FloatingTextSystem` | 编译 | 0 error |
| G-6 | 3 | `SpawnDamageNumber` 方法已删除 | grep | 0 结果 |
| G-7 | 3 | `DamageNumberState` 内部 struct 已删除 | grep | 0 结果 |
| G-8 | 3 | DOT 飘字走 `_floatingText.Spawn()` | 代码检查 | 无 `DanmakuSystem.Instance` 调用 |
| G-9 | 3 | 旧文件物理删除（DamageNumberSystem.cs / DamageNumberData.cs / Prefab / Pool SO） | 文件存在检查 | 均不存在 |
| G-10 | 3 | `DamageNumberPool` 字段从 Bootstrap 中物理删除 | grep | 0 结果 |
| G-11 | 3 | 编译 0 error / 0 warning | 编译 | ✅ |

### 9.2 全局集成验收（全部 Phase 完成后）

> **原则**：需要 Play Mode / 真机 / 性能工具才能验证的项集中在此。
> **时机**：全部开发完成后统一执行。

| ID | 验收项 | 验证方式 | 通过标准 |
|----|--------|----------|----------|
| I-1 | 敌机被普攻命中→白色飘字，数值 = FinalDamage | Play Mode 观察 | 正确 |
| I-2 | 敌机被暴击命中→金色飘字 1.5x 缩放 | Play Mode | 正确 |
| I-3 | 敌机受 DOT 伤害→紫色飘字 | 电弧 Buff 触发 | 正确 |
| I-4 | 玩家受弹幕命中→白色/金色飘字 | Play Mode | 正确 |
| I-5 | 同一次命中仅出现一个飘字 | 逐帧截图 | 无双飘字 |
| I-6 | 战斗退场后飘字清除 | Victory/Defeat 操作后截图 | 屏幕无残留 |
| I-7 | 暂停时飘字继续消散 | 暂停后观察 | unscaledDeltaTime 驱动正常 |
| I-8 | 飘字 GC = 零 | Profiler GC Alloc | 0 bytes |
| I-9 | 场景中无 DamageNumber(Clone) GO | Hierarchy 搜索 | 0 个 |
| I-10 | 飘字 DC = 1（RBM 合批） | Frame Debugger | DC ≤ 1 |

---

## 10. 风险与缓解

| # | 风险 | 等级 | 缓解策略 |
|---|------|------|----------|
| R-1 | Entity 层拿不到 FloatingTextSystem（DanmakuSystem 未初始化时 EntityBootstrap 先 Awake） | 🔴 高 | DanmakuSystem 是 DontDestroyOnLoad，先于场景级 Bootstrap。但需验证 Script Execution Order 或使用 `FindObjectOfType` 懒查找 + null 判断 |
| R-2 | DOT 紫色在数字贴图上显示不清晰 | 🟡 中 | 数字贴图白底 + Color32 顶点着色，理论可行。需真机验证可读性，不行则调亮紫色值 |
| R-3 | 暴击 `!` 后缀消失，玩家不易辨识暴击 | 🟢 低 | 暴击已有 1.5x 缩放 + 专属金色，视觉区分度足够。后续可在贴图中追加 `!` 字符 |
| R-4 | `DamageNumberPool` Inspector 字段删除导致 SO 反序列化警告 | 🟡 中 | PK-R1 UA-002：Phase 3 中物理删除字段前，先清空所有 .asset 引用（MCP Editor 脚本批量清空），然后同步删除字段。原子步骤，无 Obsolete 过渡期 |
| R-5 | Entity 层飘字 Y 偏移不同（TextMesh 有 +0.5f 偏移） | 🟢 低 | 在 OnHit 调用 Spawn 时传入 `entity.Position + new Vector2(0, 0.5f)`，保持一致 |

### R-1 详细分析：初始化时序

```
执行顺序（Unity 默认）：
1. DanmakuSystem.Awake()     ← DontDestroyOnLoad，首次加载场景时
   → InitializeSubsystems()  ← FloatingTextSystem 已创建
2. EntitySystemBootstrap.Awake() ← 场景级
   → FindObjectOfType<DanmakuSystem>()
   → danmaku.FloatingText    ← 此时已有值 ✅
```

如果场景中没有 DanmakuSystem（如纯 Entity 测试场景），`FindObjectOfType` 返回 null，`_floatingText` 为 null，OnHit 中走 null 判断跳过飘字——**降级不崩溃**。

> **PK-CR R1 CR-004 场景直启补充**：如果从战斗场景直接启动（Editor 调试，跳过 Preload），DanmakuSystem 和 EntitySystemBootstrap 在同一场景的 Awake 顺序未定义。此时 `FindObjectOfType<DanmakuSystem>()` 可能返回一个尚未执行 `InitializeSubsystems` 的实例（`FloatingText` 为 null）。通过 `?.FloatingText` 安全取值 + OnHit 中 `_floatingText != null` 守卫，降级为不显示飘字而非崩溃。

---

## 11. 废弃清理

### 11.1 删除文件清单

| 文件 | 类型 | 原用途 |
|------|------|--------|
| `DanmakuSystem/Scripts/Core/DamageNumberSystem.cs` | C# | 旧弹幕层飘字系统 |
| `DanmakuSystem/Scripts/Data/DamageNumberData.cs` | C# | 旧飘字数据结构 |
| `_Game/Prefabs/Debug/DamageNumber.prefab` | Prefab | TextMesh 飘字 GO 模板 |
| `_Game/Configs/_Template/Pool/Template_DamageNumberPool.asset` | SO | 对象池定义 |

### 11.2 需更新的文档

| 文档 | 更新内容 |
|------|----------|
| `ADR/ADR_INDEX.md` | 添加 ADR-036 条目 |
| `ADR/ADR_06_LIFECYCLE.md` | 追加 ADR-036 全文 |
| `INDEX.md` | 路由表 B 新增 `FloatingTextSystem` 映射 |
| `SYSTEMS/EC_TDD/EC_TDD_04_SYSTEMS.md` | 更新 EntityHitReactionHandler 职责（删除飘字相关） |
| `Archive/Guide/Danmaku/DANMAKU_RENDERING.md` | 已归档的早期 Guide，仅保留历史迁移线索；当前事实以本文和 `CONTEXT_PACKS/Danmaku_Rendering.md` 为准 |
| `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY.md` | 删除 DamageNumberPool 字段文档 |
| `coding-standards SKILL.md §14` | **PK-R1 UA-004**：更新为"伤害飘字必须走 FloatingTextSystem（RBM 渲染），禁止 FairyGUI/TextMesh 对象池" |

---

## 附录 A：工作量估算

| Phase | 内容 | 时长 |
|-------|------|------|
| 1 | 创建 FloatingTextSystem + FloatingTextData | 30 min |
| 2 | 接入 DanmakuSystem | 30 min |
| 3 | 迁移 Entity 层 + 废弃清理（原子步骤） | 60 min |
| — | 全局集成验收（Play Mode） | 30 min |
| — | 文档更新（含 coding-standards §14） | 25 min |
| **合计** | | **~3 小时** |

## 附录 B：回滚方案

如果迁移过程中出现难以解决的问题：

1. `git stash` 或 `git checkout` 回到改动前
2. 两套旧系统仍可正常工作（双飘字 Bug 是表现问题，不影响功能）
3. 回滚后重新评估方案

---

_文档结束_
