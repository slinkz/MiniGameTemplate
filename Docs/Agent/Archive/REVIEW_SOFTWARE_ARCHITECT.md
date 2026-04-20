# 软件架构师二次评审：基于决策的架构设计定稿

> 评审日期：2026-04-11（二次评审）| 评审角色：软件架构师
> 输入：用户已确认的 6 项架构决策（DEC-001 ~ DEC-006）
> 前置文档：`REVIEW_GAME_DESIGNER.md`（20 项需求）、首次评审（9 项缺陷）

---

## 一、决策影响总览

| 决策 | 选择 | 对架构的核心影响 |
|------|------|-----------------|
| DEC-001=B | 按贴图分桶 | 渲染管线变为 `(Layer, Texture)` 二维字典；DC 数量 = Σ(使用中的唯一贴图数 × 2 层) |
| DEC-002=B | 框架内目录 | 无独立 asmdef，共享代码放 `_Framework/Rendering/`，MiniGameFramework.Runtime.asmdef 内 |
| DEC-003 | DrawMesh+sortingOrder | 渲染器只管填 Mesh，调度层通过调用顺序控制 Z 排序 |
| DEC-005=C | Core 存储动画值 | BulletCore 36B → 48B（+Scale:4 +Alpha:4 +Color32:4），SoA 热路径更宽但渲染零查找 |
| DEC-006=VFX Sheet | 喷雾复用 VFX | 不新建 SprayRenderer，SprayUpdater 通过附着式 VFX API 驱动持续跟随表现，喷雾可视化为近似表现 |

---

## 二、确定后的目标架构

### 2.1 模块依赖拓扑

```
MiniGameFramework.Runtime (单一 asmdef)
│
├── _Framework/Rendering/           ← 共享渲染基础设施（DEC-002=B）
│   ├── RenderVertex.cs             ← 从 DanmakuVertex 提取
│   ├── RenderBatchManager.cs       ← 共享实现类；各系统各自持有实例
│   ├── IRenderBatchProvider.cs     ← 各子系统渲染器实现此接口
│   └── MODULE_README.md
│
├── _Framework/DanmakuSystem/       ← 弹幕系统
│   ├── Scripts/Core/
│   │   ├── BulletRenderer.cs       ← 实现 IRenderBatchProvider
│   │   ├── LaserRenderer.cs        ← 实现 IRenderBatchProvider
│   │   ├── BulletMover.cs          ← 含动画值写入（DEC-005=C）
│   │   ├── CollisionSolver.cs      ← 写入 CollisionEventBuffer
│   │   └── ...
│   ├── Scripts/Data/
│   │   ├── BulletCore.cs           ← 48B（DEC-005=C）
│   │   ├── CollisionEvent.cs       ← 碰撞事件结构体
│   │   ├── CollisionEventBuffer.cs ← 预分配数组（DEC-004）
│   │   └── ...
│   └── Scripts/Config/
│       ├── BulletTypeSO.cs         ← 新增 Texture2D 字段（DEC-001=B）
│       └── ...
│
├── _Framework/VFXSystem/           ← VFX 系统（无 Danmaku 编译依赖）
│   ├── Scripts/Core/
│   │   └── VFXBatchRenderer.cs     ← 实现 IRenderBatchProvider
│   └── Scripts/Config/
│       └── VFXTypeSO.cs            ← 新增源贴图字段，支持独立 SpriteSheet 资源
│
└── _Framework/DanmakuSystem/Scripts/Integration/
    └── SprayVFXBridge.cs           ← 喷雾→VFX 桥接（DEC-006）
```

**依赖方向**：
```
DanmakuSystem ──→ Rendering ←── VFXSystem
                     ↑
              （无循环依赖）
```

VFX 不再 `using MiniGameTemplate.Danmaku`。✅ SA-002 解决。

---

### 2.2 RenderBatchManager 详细设计

```csharp
/// <summary>
/// 统一渲染批次管理器。
/// 按 (RenderLayer, Texture2D) 二元组分桶，每桶一个 Mesh + 一个 MaterialPropertyBlock。
/// 不拥有数据——由各子系统渲染器（BulletRenderer/LaserRenderer/VFXBatchRenderer）
/// 通过 IRenderBatchProvider 接口提交顶点/索引/材质信息。
/// </summary>
public sealed class RenderBatchManager
{
    // ── 桶定义 ──
    struct BatchKey : IEquatable<BatchKey>
    {
        public RenderLayer Layer;   // Normal / Additive
        public Texture2D  Texture;  // null = 默认贴图
    }

    struct BatchSlot
    {
        public Mesh     Mesh;
        public Material SharedMaterial;  // 基于 Layer 的基础材质（克隆）
        public MaterialPropertyBlock Mpb;
        public int VertexCount;
        public int IndexCount;
    }

    // ── 生命周期 ──
    // Initialize()：从 TypeRegistry 扫描所有 SO，预创建桶
    // Rebuild()：遍历所有 Provider，收集顶点→写入对应桶的 Mesh
    // Draw()：按 sortingOrder 遍历桶，调用 Graphics.DrawMesh
    // Dispose()：销毁所有 Mesh 和 Material 实例
}
```

**调用顺序（Z 排序）**：

```
sortingOrder 0   : 背景层弹丸（如果有）
sortingOrder 100 : Normal 层弹丸
sortingOrder 200 : Additive 层弹丸
sortingOrder 300 : 激光
sortingOrder 400 : VFX Normal
sortingOrder 500 : VFX Additive
sortingOrder 600 : 飘字
```

每个 sortingOrder 内，不同贴图的桶按注册顺序绘制（同层同贴图必然同桶，不存在排序问题）。

---

### 2.3 BulletCore 48B 内存布局（DEC-005=C）

```
偏移  字段                 大小   说明
──────────────────────────────────────────
 0    Position.x          4B    float
 4    Position.y          4B    float
 8    Velocity.x          4B    float
12    Velocity.y          4B    float
16    Rotation            4B    float (radians)
20    Speed               4B    float
24    Lifetime            4B    float (已存活时间)
28    MaxLifetime         4B    float
32    TypeIndex           2B    ushort (→ BulletTypeSO)
34    Flags               2B    ushort (8 个标志位)
────── 以上为原 36B ──────
36    AnimScale           4B    float (当前缩放倍率)
40    AnimAlpha           4B    float (当前透明度 0~1)
44    AnimColor           4B    Color32 (当前叠加色)
──────────────────────────────────────────
合计：48B（= 16 × 3 = 64B cache line 的 3/4）
```

**权衡分析**：

| 维度 | 原 36B | 新 48B | 评估 |
|------|--------|--------|------|
| 2048 弹丸总内存 | 72 KB | 96 KB | +24 KB，仍远小于 L2 128KB line |
| Cache line 利用率 | 64B line 装 1.78 个 Core → 浪费 28B/line | 64B line 装 1.33 个 Core → 浪费 16B/line | ⚠️ 48B 不如 32B/64B 对齐完美，但比 36B 略好 |
| Mover 写入 | 无动画写入 | +3 次写入/弹丸/帧 | 可接受 |
| Renderer 读取 | 需查表/采样 | 直接读 Core | ✅ 零查找，最快路径 |

**结论**：48B 的 cache 效率比 36B 差一点点（浪费从 28B 变到 16B per line——实际上更好了），但渲染时零间接查找的收益远大于 Mover 多 3 次写入的开销。**这是正确的权衡。**

> ⚠️ **但我要指出一个隐患**：`AnimationCurve.Evaluate()` 在 WebGL/IL2CPP 下的 GC 行为需要实测。如果产生 GC，需要回退到预烘焙 LUT。建议 Phase 3 实施时第一件事就是写 benchmark。

---

### 2.4 CollisionEventBuffer 设计（DEC-004）

```csharp
public struct CollisionEvent
{
    public ushort BulletIndex;       // 2B  弹丸在 BulletWorld 中的索引
    public ushort TargetIndex;       // 2B  碰撞目标在 TargetRegistry 中的索引
    public CollisionType Type;       // 1B  枚举：BulletVsTarget/BulletVsObstacle/LaserVsTarget/...
    public byte Reserved;            // 1B  对齐
    public Vector2 ContactPoint;     // 8B  接触点世界坐标
    public int Damage;               // 4B  伤害值
    public ushort BulletTypeIndex;   // 2B  BulletTypeSO 的 RuntimeIndex
    public ushort Padding;           // 2B  对齐到 4 字节边界
}
// 合计：22B → padding 到 24B

public sealed class CollisionEventBuffer
{
    private readonly CollisionEvent[] _events;
    private int _count;
    public int Count => _count;
    public int Capacity { get; }

    public CollisionEventBuffer(int capacity) { ... }
    public void Clear() => _count = 0;
    public bool TryWrite(in CollisionEvent evt) { ... }  // 满了返回 false
    public ReadOnlySpan<CollisionEvent> AsSpan() => _events.AsSpan(0, _count);
}
```

**溢出策略**：Buffer 满时丢弃（`TryWrite` 返回 false），CollisionSolver 内部按优先级排序——目标碰撞 > 障碍物碰撞 > 屏幕边缘碰撞。

**容量选择**：默认 256。理由：2048 颗弹丸每帧不太可能同时产生超过 256 次碰撞。如果真的满了，说明弹幕密度超出设计预算，应该通过 DebugHUD 告警而非无声丢弃。

---

### 2.5 喷雾可视化方案（DEC-006 = VFX Sprite Sheet）

```
SprayUpdater (已有逻辑) 
  │
  ├── 检测喷雾激活 → VFXSystem.Play(sprayVfxType, spray.Position, spray.Arc → scale)
  ├── 每帧同步位置/旋转 → VFXSystem.UpdateAttached(...)
  └── 喷雾结束 → VFXSystem.Stop(handle)
```

**权衡分析**：

| 维度 | 扇形 Mesh（原建议） | VFX Sprite Sheet（用户选择） |
|------|---------------------|--------------------------|
| 视觉精确度 | ✅ 精确匹配逻辑扇形 | ⚠️ 帧动画只是近似，不与逻辑区域精确对应 |
| 实现复杂度 | 中（~200 行新代码） | 低（~50 行桥接代码，复用已有 VFX 管线） |
| 新增 DC | +1~2 | +0（复用 VFX 的桶） |
| 美术可定制性 | 低（程序化网格，颜色参数有限） | ✅ 高（任意帧动画，可以做出很丰富的效果） |
| 维护成本 | 多一个渲染器要维护 | 零（复用） |

**架构师评价**：用户的选择是**正确的**。理由：
1. 弹幕游戏的喷雾 AOE 视觉效果本来就不要求像素级精确——火焰喷射、毒雾这些效果用帧动画表现反而更自然
2. 减少了一个需要维护的渲染器（SprayRenderer），降低了系统复杂度
3. 充分利用了已有的 VFX 管线，**让 VFX 系统的 ROI 更高**

**但有一个注意事项**：VFX Sprite Sheet 是定位播放的，而喷雾是持续的。需要实现 `PlayAttached/UpdateAttached/StopAttached` 一类 API 来支持持续跟随的特效；FollowTarget 句柄应使用 `AttachSourceId` 抽象，而不是直接绑定 `Transform`。这是 Phase 3 需要新增的 VFX 能力。

---

## 三、基于决策的二次缺陷评审

### 原始 9 项缺陷的新状态

| ID | 缺陷 | 首次评审建议 | 用户决策后的实际方案 | 评审结论 |
|----|------|-------------|---------------------|---------|
| SA-001 | 单贴图渲染 | 方案 A 合 Atlas | **方案 B 按贴图分桶** | ✅ 更直接，DC 增长可控 |
| SA-002 | VFX↔Danmaku 耦合 | 提取共享层 | **Rendering/ 在框架内** | ✅ 方案 OK |
| SA-003 | 渲染器职责混乱 | 统一 RenderBatchManager | **RenderBatchManager + sortingOrder** | ✅ 方案 OK |
| SA-004 | 运动不可扩展 | 策略表 | **策略表**（未变） | ✅ |
| SA-005 | 碰撞不可扩展 | CollisionEventBuffer | **预分配数组** | ✅ |
| SA-006 | 容量硬编码 | 收拢到 Config SO | 未变 | ✅ |
| SA-007 | 入口过重 | 拆分 System/API/EventBus | 未变 | ✅ |
| SA-008 | Shader 简陋 | 增强 dissolve/glow | 未变 | ✅ |
| SA-009 | 内存对齐 | 对齐到 2 的幂 | **48B = 3×16B**（DEC-005=C） | ⚠️ 见下方新发现 |

### 新发现的架构风险（首次评审未覆盖）

#### ⚠️ NEW-001: RenderBatchManager 的桶生命周期管理

**背景**：DEC-001=B 意味着运行时可能存在大量桶。如果一个关卡有 8 种贴图 × 2 层 = 16 个桶，每个桶维护一个 Mesh + Material 实例。

**风险**：
1. 场景切换时，旧关卡的桶需要被销毁（Mesh.Destroy + Material.Destroy），否则泄漏
2. 如果动态加载新弹丸类型（如 Boss 出场时引入新贴图），需要运行时新增桶→Mesh 初始化有 GC 分配
3. 桶的 Material 克隆 `new Material(base)` 每次都是堆分配

**缓解方案**：
- 初始化时从 `DanmakuTypeRegistry` + `VFXTypeRegistrySO` 扫描所有 SO，预创建所有可能的桶
- 提供 `RenderBatchManager.WarmUp(BulletTypeSO[])` API，让游戏层在关卡加载时预热
- 禁止运行时创建新桶（如果遇到未注册贴图，使用 fallback 默认贴图 + 警告日志）

**严重度**：🟡 中——可通过预创建完全规避

---

#### ⚠️ NEW-002: BulletCore 48B 的 SoA 遍历代价

**背景**：DEC-005=C 让 BulletCore 从 36B 涨到 48B。Mover 每帧需要额外写 3 个字段，Renderer 每帧需要额外读 3 个字段。

**量化分析**：

```
2048 弹丸 × 48B = 96 KB（BulletCore 数组）
2048 弹丸 × 28B = 56 KB（BulletTrail 数组）
2048 弹丸 × 16B = 32 KB（BulletModifier 数组）
合计热路径：184 KB

L1 cache（典型移动芯片）：32~64 KB → 无法一次装下 BulletCore
L2 cache（典型移动芯片）：256~512 KB → 可以装下全部三层
```

**对比原方案**：

```
原 36B：2048 × 36B = 72 KB → L1 装不下，L2 OK
新 48B：2048 × 48B = 96 KB → L1 装不下，L2 OK
差异：+24 KB，在 L2 层面影响可忽略
```

**结论**：在移动端 L2 缓存范围内，48B 不会造成显著性能退化。**决策合理。**

**但如果未来容量从 2048 扩展到 4096**：
- 4096 × 48B = 192 KB（可能超出部分低端芯片的 L2）
- 届时需要考虑将 AnimScale/AnimAlpha/AnimColor 拆到第四层 SoA `BulletVisual[]`

---

#### ⚠️ NEW-003: VFX 系统需要新增「附着特效」能力

**背景**：DEC-006=VFX Sheet 意味着喷雾的可视化完全依赖 VFX 系统。但当前 VFX 系统只有 `Play(type, position, scale)` 和 `PlayOneShot()` ——都是「发射后不管」的一次性播放。

**喷雾需要的是**：
1. **持续跟随**：喷雾在发射期间持续改变位置和方向
2. **循环播放**：喷雾持续时间由逻辑控制，不是固定帧数
3. **参数同步**：喷雾的角度/范围可能动态变化

**所需 VFX API 扩展**：

```csharp
// 新增 API
int PlayAttached(VFXTypeSO type, Vector3 position, float rotation, bool loop = true);
void UpdateAttached(int handle, Vector3 position, float rotation);
void StopAttached(int handle, bool immediate = false);
```

**影响评估**：这不是重构，是 VFX 系统的功能扩展。需要在 `VFXInstance` 中新增 `AttachHandle` 和 `IsLooping` 字段。Phase 3.7 的工作量从"50 行桥接代码"上调为"~150 行 VFX 扩展 + 50 行桥接代码"。

**严重度**：🟡 中——不影响架构，但影响工时估算

---

#### ⚠️ NEW-004: 按贴图分桶的 DrawCall 预算需要约束

**背景**：DEC-001=B 让 DC 数量与使用中的唯一贴图数成正比。

**最坏情况估算**：

```
弹丸：8 种贴图 × 2 层 = 16 DC
激光：2 种贴图 × 1 层 = 2 DC
VFX ：4 种贴图 × 2 层 = 8 DC
飘字：1 DC
────────────────────────────
合计：27 DC（加上场景本身的 DC，总 DC 可能到 40+）
```

**低端机 DC 预算**：微信小游戏在 iPhone SE 2 / 红米 Note 系列上，50 DC 以内才安全。

**建议强制约束**：
1. `RenderBatchManager` 维护一个 `int MaxBatchCount` 配置项（默认 24）
2. 超出时在 DebugHUD 显示红色警告
3. 在 `MODULE_README.md` 中明确告知开发者：**每种独立贴图会增加 2 个 DC**，引导他们合理规划弹丸贴图

**但这不是架构问题，是使用规范问题。** 架构侧只需提供监控手段即可。

---

#### ⚠️ NEW-005: AnimationCurve.Evaluate() 的 GC 风险（已修正）

**背景**：此风险**并非 DEC-005=C 引入**，而是**已有代码中既存的问题**。当前代码中有 4 处热路径调用 `AnimationCurve.Evaluate()`：

| 调用点 | 文件 | 频率 |
|--------|------|------|
| 弹丸速度曲线 | `BulletMover.cs:93` | 每帧 × 每个速度曲线弹丸（**大头**） |
| 激光宽度曲线 | `LaserUpdater.cs:50,77` | 每帧 × 每条激光（最多 16） |
| 激光沿长度宽度分布 | `LaserRenderer.cs:204-205` | 每帧 × 激光段数 × 2 |
| 拖尾宽度曲线 | `TrailPool.cs:194` | 重建时 × 采样点数 |

DEC-005=C 选择将采样结果写入 BulletCore，**不改变 Evaluate() 的调用次数**——Mover 本来就在调。C 方案改变的只是"采样结果存在哪"。

**已知问题**：
- Unity 2022 的 `AnimationCurve.Evaluate()` 在 Mono 后端会产生少量 GC（每次调用约 40B boxing）
- IL2CPP 后端（微信小游戏必然走 IL2CPP）通常不会，但无 100% 保证

**缓解方案**：
1. Phase 1 完成后写 benchmark 脚本，在 WebGL IL2CPP build 中实测 GC 行为
2. 如果有 GC → 仅对大头（`SpeedOverLifetime`）做预烘焙 LUT；激光和拖尾调用量低，可不处理
3. 如果无 GC → 维持原样

**严重度**：🟢 低——既存问题 + 有明确的回退路径，不影响架构

---

## 四、最终架构评审结论

### 4.1 总体评价

**用户的六项决策组合是一个务实且自洽的方案。** 具体来说：

| 决策 | 评价 |
|------|------|
| DEC-001=B | ✅ **比我原来推荐的 A+B 混合更简单**。Atlas 打包是额外的构建步骤和维护负担，对于模板项目来说 overkill。DC 增长在合理范围内（8 种弹丸贴图 = 16 DC，远在预算内）。 |
| DEC-002=B | ✅ **正确**。模板项目不需要独立 asmdef 的编译隔离。 |
| DEC-003 | ✅ **与首次评审一致**。 |
| DEC-004 | ✅ **与首次评审一致**。 |
| DEC-005=C | ✅ **比 LUT 更直观**。代价是 BulletCore 变胖，但 48B 对齐到 16B 倍数，cache 效率反而比原来的 36B 好。唯一风险是 `Evaluate()` 的 GC，但有回退路径。 |
| DEC-006=VFX | ✅ **比扇形 Mesh 更好**。减少维护面，提升美术可定制性。代价是需要扩展 VFX 的附着/循环 API。 |

### 4.2 方案自洽性检查

| 检查项 | 通过？ | 说明 |
|--------|--------|------|
| 模块依赖无环 | ✅ | Danmaku→Rendering←VFX，箭头方向一致 |
| 零 GC 路径 | ✅ | 预分配数组 + 预创建桶 + SoA（唯一风险点 AnimationCurve.Evaluate 有回退方案） |
| 微信小游戏兼容 | ✅ | DrawMesh + 无 NativeArray + 无 Compute + WebGL Shader 限制内 |
| 已有 Demo 可回归 | ✅ | Phase 0 纯重构不改功能，Phase 1 后 Demo 应正常运行 |
| SO 序列化迁移 | ✅ | FormerlySerializedAs + 合理默认值 |
| 可逆性 | ✅ | 每个 Phase 的改动可独立编译验证，失败可回退到上一个 Phase 的 commit |

### 4.3 方案的三个「不完美但可接受」

1. **48B BulletCore 不是完美的 cache 对齐**——但比 36B 好，且渲染零查找收益更大
2. **喷雾视觉不精确匹配逻辑区域**——但帧动画的美术表现力更强，且真实弹幕游戏的 AOE 提示本来就是近似的
3. **DC 数量与贴图种类线性增长**——但微信小游戏的弹幕不太可能用到超过 10 种独立贴图，20 DC 在预算内

### 4.4 方案的一个「需要追加的能力」

VFX 系统需要扩展 `PlayAttached` / `UpdateAttached` / `StopAttached` API 来支持 DEC-006 的喷雾方案。这应该被加入 Phase 2 或 Phase 3 的任务列表。

---

## 五、修订后的架构重构路线图

### 阶段 0：基础设施层（无功能变化）
```
新增 _Framework/Rendering/：
├── RenderVertex.cs          ← 从 DanmakuVertex 改名提取
├── RenderBatchManager.cs    ← (Layer, Texture) 分桶 + DrawMesh 调度
├── IRenderBatchProvider.cs  ← 子系统渲染器接口
└── MODULE_README.md
```
- 迁移 `DanmakuVertex` → `RenderVertex`，更新所有 `using`
- 容量硬编码收拢到 `DanmakuWorldConfig` / `VFXRenderConfig`（SA-006）
- 编译验证 + Demo 回归

### 阶段 1：多贴图渲染管线（核心重构）
```
改造：
├── BulletRenderer    → 实现 IRenderBatchProvider，按贴图分桶
├── LaserRenderer     → 实现 IRenderBatchProvider，按贴图分桶
├── VFXBatchRenderer  → 实现 IRenderBatchProvider，按贴图分桶
├── BulletTypeSO      → +Texture2D BulletTexture
├── VFXTypeSO         → +SourceTexture/Sheet 资源字段（支持独立贴图）

└── DanmakuRenderConfig / VFXRenderConfig → 移除单一贴图字段
```
- RenderBatchManager 统一调度 DrawMesh，控制 sortingOrder
- DebugHUD 增加 DC 计数器 + 预警

### 阶段 2：事件与扩展性（功能补全）
```
新增/改造：
├── CollisionEventBuffer.cs      ← 预分配 CollisionEvent[256]
├── CollisionSolver.cs           ← 保留即时命中回调，并把旁路事件写入 Buffer
├── MotionRegistry.cs            ← delegate[] 策略表
├── BulletMover.cs               ← 使用策略表
├── DanmakuSystem.cs             ← 保留 Facade，内部拆运行时/碰撞/效果桥接职责
├── VFXSystem 扩展               ← PlayAttached/UpdateAttached/StopAttached
└── ClearScreen API
```

### 阶段 3：视觉增强（迭代优化）
```
改造/新增：
├── BulletCore.cs                ← +12B 动画字段 → 48B
├── BulletMover.cs               ← 写入 AnimScale/AnimAlpha/AnimColor
├── BulletRenderer.cs            ← 读取 Core 动画值
├── BulletTypeSO.cs              ← +AnimationCurve 曲线字段
├── SprayVFXBridge.cs            ← SprayUpdater ↔ VFX 桥接
├── Shader 增强                  ← dissolve + glow
├── LaserWarningRenderer.cs      ← 预警线
└── SpriteSheetVFXSystem.cs      ← 时间缩放联动
```

### 阶段 4：工作流与工具
（与原方案一致，此处省略）

---

## 六、ADR 记录

### ADR-001: 多贴图方案选择按贴图分桶

**状态**：已接受

**上下文**：弹幕系统只支持单张贴图，无法满足真实项目多种弹丸共存的需求。三个候选方案：A.构建时合 Atlas、B.运行时按贴图分桶、C.Texture2DArray。

**决策**：选择方案 B——运行时按 (RenderLayer, Texture2D) 分桶渲染。

**后果**：
- 更容易：新增弹丸类型只需在 SO 上引用贴图，无需任何构建步骤
- 更容易：运行时不依赖 Atlas 打包工具，内容生产可直接使用独立贴图
- 更容易：若后期需要优化，可通过可选 Atlas 工具回写映射，不改变运行时资源入口

- 更难：DrawCall 与贴图种类数量线性增长，需要监控 DC 预算
- 更难：RenderBatchManager 需要管理桶的生命周期（创建/预热/销毁）

---

### ADR-002: 弹丸视觉动画值存储在 BulletCore

**状态**：已接受

**上下文**：弹丸需要随生命周期变化的 Scale/Alpha/Color。三个候选方案：A.运行时曲线采样、B.LUT 查找表、C.Core 存储当前值。

**决策**：选择方案 C——BulletCore 新增 3 个字段（Scale:float + Alpha:float + Color:Color32），由 BulletMover 每帧写入，BulletRenderer 直接读取。

**后果**：
- 更容易：渲染器零间接查找，最快读取路径
- 更容易：代码直观，不需要 LUT 初始化/管理
- 更难：BulletCore 从 36B 膨胀到 48B，每弹丸每帧多 3 次 float 写入
- 风险：AnimationCurve.Evaluate() 在 WebGL/IL2CPP 下可能有微量 GC（有 LUT 回退方案）

---

### ADR-003: 喷雾可视化复用 VFX Sprite Sheet

**状态**：已接受

**上下文**：喷雾系统有完整逻辑但无渲染。三个候选方案：扇形 Mesh、VFX Sprite Sheet、粒子模拟。

**决策**：选择 VFX Sprite Sheet——喷雾的视觉表现由预制帧动画实现，通过 SprayUpdater → VFXSystem 桥接。

**后果**：
- 更容易：不需要新写 SprayRenderer，减少维护面
- 更容易：美术可完全自定义喷雾视觉效果（任意帧动画）
- 更难：VFX 系统需要扩展附着/循环播放 API
- 更难：视觉效果不与逻辑扇形区域精确对应（近似表现）

---

## 七、补充闭环（2026-04-11 夜间）

基于同日软件架构师复核，以下 6 项从“执行疑问”升级为“正式契约”，本评审文档与 `ARCHITECT_DECISION_RECORD.md` / `REVIEW_LANDING_AUDIT.md` / `REFACTOR_PLAN.md` 保持一致：

### 7.1 RenderBatchManager 桶生命周期正式定稿
- 桶在初始化阶段按注册表预热
- 运行时禁止因未知贴图隐式创建新桶
- 未知 `(RenderLayer, Texture)`：
  - Editor/Dev：错误日志 + 计数
  - Release：跳过渲染，不自动补建

**架构含义**：渲染链路保持“注册期可验证、运行期只消费”，不退化成热路径动态建模。

### 7.2 Bullet / VFX 统一资源描述语义，保留各自行为模型
- 统一资源入口语义：`SourceTexture`、`UVRect`、`MaterialKey/BlendMode`、可选 `AtlasBinding`
- Bullet 保留自身运动/命中语义字段
- VFX 保留自身 Sheet / Playback / Attach 语义字段
- DamageNumber 不强行并入同一资源策略，只复用共享渲染基础设施

**架构含义**：统一的是值对象语言，不是把 Bullet/VFX 强行揉成一个超级类型。

### 7.3 Atlas 输出协议正式定稿
- Atlas 是可逆派生产物，不是源数据真相
- 源事实仍然是原始 `SourceTexture + UVRect`
- 工具输出至少包括：`AtlasTexture` + `AtlasMappingSO`（或等价映射资产）
- 批量回写 SO 仅为可选能力，不得成为唯一工作流
- Bullet / VFX / DamageNumber atlas 分域维护，不混打

**架构含义**：atlas 是优化层，不是生产前置层。

### 7.4 CollisionEventBuffer 溢出语义正式定稿
- Buffer 明确定义为表现 / 联动 / 观察通道，不承载主业务事实
- 溢出不影响：伤害、击退、死亡、状态变更
- 溢出只影响：VFX、飘字、调试统计、非关键联动
- 必须记录 overflow count，并接入 profiler / debug HUD
- 若实现优先级，只允许轻量分档，不引入复杂业务优先级树

**架构含义**：主逻辑和表现逻辑边界固定，避免 buffer 漂成第二套业务系统。

### 7.5 VFX FollowTarget 句柄模型正式定稿
- `World`：直接存世界坐标
- `FollowTarget`：持有 `AttachSourceId`
- `Socket`：持有 `AttachSourceId + SocketName/SocketIndex`
- VFX 通过位置解析接口获取世界坐标，不直接持有 `Transform`
- 默认失效语义：目标失效时冻结到最后有效位置并播完，不立即销毁

**架构含义**：VFX 与 Unity 场景对象生命周期解耦，后续更容易扩展到骨骼点、逻辑实体和临时挂点。

### 7.6 容量配置化边界正式定稿
- 本轮范围必须通过显式表格控制
- 表格至少包含：模块、当前容量来源、本轮是否纳入、纳入原因、计划 Phase、是否阻塞其他模块、备注
- 未列入项默认不在本轮范围内

**架构含义**：用范围表防止执行期 scope 漂移，而不是靠口头约定。

---

## 八、软件架构师终审结论（2026-04-11 夜间补充）

### 8.1 结论

从软件架构师视角看，**修正后的重构计划已经达到“可以启动执行”的状态**。

当前不再存在会阻塞 Phase 0 启动的架构级未决项。前一轮评审里担心的 3 类问题：
- Batch 预热与桶生命周期
- Bullet / VFX 统一资源描述语义
- VFX FollowTarget 的句柄边界

现在都已经被 ADR、落地审计和重构计划正文吸收为正式约束。

换句话说，问题已经从“方案是否成立”，转成了“执行时是否严格按约束落地”。这两者差很多。前者不能开工，后者可以开工但要盯验收口径。现在属于后者。

### 8.1.1 针对本轮复审的三类问题结论

#### A. 条款之间是否互相打架

**结论：没有新的架构级互斥，但有 3 处高张力点，必须按分层口径理解。**

1. **“30 分钟上手配置” vs “迁移/刷新/可选 atlas 工具链”**
   - 这不是架构冲突，而是产品体验目标与工程治理成本之间的张力。
   - 正确理解应是：30 分钟约束的是“内容使用者基于模板完成一次配置闭环”，不是“新开发者 30 分钟理解整套框架内部机制”。

2. **“新增运动类型不改核心热路径” vs “序列帧子弹需要改 BulletRenderer”**
   - 这两条看起来容易被误读成冲突，实际不冲突。
   - 前者约束的是 **Motion 扩展边界**，后者属于 **Bullet 视觉采样能力扩展**。
   - 如果后续评审把它们混成“任何新增能力都不能改旧文件”，那是口径错，不是计划打架。

3. **“atlas 非前置” vs “atlas 工具必须补”**
   - 这也不是冲突。
   - 正确分层是：**atlas 是优化出口，不是资源真相层，也不是生产前置条件**。

#### B. 验收口径是否可执行

**结论：整体可执行，但有几条必须补成“有输入/有输出/有失败判定”的硬口径。**

当前最容易失真的 4 类验收：
1. **55fps 目标**：必须绑定测试平台、机型、场景、持续时长、统计口径；正式口径为“预热完成后进入稳定运行窗口再计时”，不把编辑器刷新、Registry 重建、Batch 预热或首次资源冷启动成本计入 30 秒平均帧率；同一窗口内必须同时记录平均 FPS、平均/峰值 DrawCall、活跃 Batch 数、未知桶错误计数、`CollisionEventBuffer overflow count`，并以“未知桶错误计数 = 0、overflow count = 0、DrawCall ≤ 50、活跃 Batch ≤ 24”作为共同通过条件。
2. **30 分钟上手目标**：必须限定起点（基于模板 demo）、允许材料（Guide 文档）、完成定义（不改代码，仅改 SO 并跑通指定场景）；同时明确允许使用 Inspector/已有 Editor 工具、允许复制现有 `BulletTypeSO` / `VFXTypeSO` / Registry 示例资产作为模板并修改字段，但不允许临时写脚本、手工改 prefab/scene 序列化文本、依赖未文档化的场景内临时对象或隐藏入口、或引入额外构建步骤。
3. **附着式 VFX**：必须补齐 Resolver 失败语义、重复 Stop、失效 handle、恢复后是否重新跟随等规则；正式口径为旧 handle 在进入冻结收尾态后不允许自动恢复跟随，恢复只能重新 `PlayAttached`；同一 `AttachSourceId + VFXType` 的重复 `PlayAttached` 必须有单一语义（默认要求先停止旧 handle 再创建新 handle，旧 handle 不允许继续隐式存活）。
4. **迁移器 `dry-run -> apply -> report`**：必须补齐阻断错误/警告分级，以及 report 的归档要求；并补上兼容退出机制——当阻断错误清零且实例扫描通过后，下一 schema 版本必须删除旧兼容字段与 fallback，禁止长期双轨。

#### C. 是否混淆了架构原则与实现细节

**结论：仍有少量混层风险，但已经可以通过“原则 / 当前实现建议”二分法压住。**

最典型的 3 个点：
1. **sortingOrder 单一真相** 是架构原则；`RenderSortingOrder` 静态类只是当前实现建议。
2. **Motion 受控扩展** 是架构原则；`enum + registry + delegate table` 只是当前实现形态。
3. **AttachSource 句柄解耦** 是架构原则；`IAttachSourceResolver` 这种接口命名与目录归属属于实现细节。

后续所有文档和代码评审，都应优先问：
- 守住的到底是边界，还是只是某个第一版实现？
- 被写死的是架构原则，还是只是当前建议实现？

---

### 8.2 针对本轮 Unity 架构师追问的正式回应

#### 1. 关于生命周期：这已经不是开放问题，而是执行约束

结论先说死：**RenderBatchManager 不共享实例；实例生命周期跟随各自系统；运行时禁止隐式建桶；编辑器侧刷新必须走“标脏 → Registry 重建 → Batch 预热 → 结果报告”固定链路。**

这意味着：
- Danmaku/VFX 各自拥有自己的 BatchManager、Registry、RuntimeIndex 和预热结果
- `OnValidate`、Inspector 改值、迁移器、Atlas 工具都只能把系统打脏，不能直接顺手重建运行时对象
- 进入 PlayMode 前如果仍有 dirty，必须先刷新成功；失败就阻断验收链路，而不是带着旧缓存继续跑

所以 Unity 侧如果继续问“生命周期到底归谁管”，软件架构口径就是：**归各自系统自己管，编辑器只负责触发受控刷新，不负责偷偷修运行时。** 这点已经定稿，不再讨论全局渲染单例或热路径自愈。

#### 2. 关于 attached VFX 语义：这不是“跟随特效差不多就行”，而是三段式契约

结论：**attached VFX = 句柄绑定 + 每帧解析 + 显式停止。**

必须按三段理解：
1. `PlayAttached`：创建实例并绑定 `AttachSourceId`
2. `UpdateAttached`：刷新派生姿态参数，不负责决定生命周期
3. `StopAttached`：显式结束或切换收尾策略

默认失效语义也已经定死：
- Resolver 失效时，冻结到最后有效位置并播完
- 只有显式配置的类型，才允许目标失效即结束
- 重复 Stop、无效 handle、解析失败都必须幂等且可观测

所以这件事不是“VFX 要不要直接拿 Transform 更方便”，而是**架构上明确禁止直接拿 Transform 作为主句柄**。否则生命周期又会重新耦死。

#### 3. 关于 migration 边界：迁移属于资产层，不属于运行时

结论：**migration 的边界是 SchemaVersion 驱动的资产升级链路，不是运行时 fallback 系统。**

已经明确的硬约束：
- 迁移只能走 `dry-run -> apply -> report`
- 迁移验收必须覆盖 prefab/scene 实例，不只看 SO 资产本体
- `OnValidate` 只做轻量补值与标脏，不承担跨版本迁移
- runtime 只消费当前 schema 数据；旧版本数据在 Editor/Dev 直接报错并阻断正式链路
- fallback 只允许作为过渡期读兼容，不允许长期双轨并存

所以 Unity 侧如果继续追问“那旧字段要不要长期保留兜底”，软件架构答案是：**可以短期读兼容，但不能把兼容逻辑升级成正式运行时责任。**

#### 4. 关于 DanmakuSystem 类级拆分：保留 Facade，不准拆成碎系统

结论：**DanmakuSystem 保留 MonoBehaviour Facade；内部按职责拆，不按场景对象数量拆。**

推荐且已收敛的类级边界是：
- `DanmakuSystem` / Facade：生命周期编排、对外 API 单一入口
- `RuntimeContext`：持有世界、池、注册表、配置引用
- `UpdatePipeline`：逐帧推进与阶段调度
- `EffectsBridge`：消费旁路事件并桥接 VFX/飘字/调试输出

这里的关键不是类名，而是边界：
- 外部仍只认一个入口
- 内部拆分不允许反向再长出多个互相回调的小单例
- 不允许为了“看起来更解耦”把初始化顺序问题扩散到多个 MonoBehaviour

所以继续质疑“为什么不彻底拆成多个系统组件”，我的回答很直接：**因为那会把一个可控中心入口，拆成一组更难维护的生命周期网络，收益不值代价。**

#### 5. 关于 SO / runtime 边界：SO 描述资源与规则，runtime 只消费派生结果

结论：**SO 负责声明式配置；runtime 负责索引、缓存、预热和逐帧消费；两边不互相偷职责。**

具体落点已经明确：
- SO 层：`SourceTexture + UVRect + MaterialKey/BlendMode + 可选 AtlasBinding + SchemaVersion`
- runtime 层：Registry、RuntimeIndex、Batch 预热结果、attached handle、overflow 统计等派生状态
- `OnValidate` 不直改 runtime
- runtime 不承担 schema 迁移
- Bullet 的序列帧能力属于 Bullet 主链路采样策略扩展，不借道 VFX

一句话概括：**SO 是真相，runtime 是消费层；SO 不保存运行时派生缓存，runtime 不替 SO 修历史债。**

### 8.3 仍需持续盯住的 5 个执行级关注点



以下不是新的架构未决项，而是**最容易在实现阶段长歪的地方**：

1. **RenderBatchManager 的注册表来源必须保持单一真相**
   - Bullet 侧和 VFX 侧都应各自通过显式 TypeRegistry 预热
   - 不允许在运行时偷偷补注册、偷偷建桶
   - 任何“先跑起来再补桶”的做法都会把热路径重新污染

2. **统一资源描述要落成代码级共享值对象，而不是只停留在文档语义**
   - 文档里已经统一为 `SourceTexture + UVRect + MaterialKey/BlendMode + 可选 AtlasBinding`
   - 实现时应收敛成共享 descriptor / value object
   - 否则后面 Inspector、迁移器、Atlas 工具会各自长一套字段模型

3. **AttachSourceId 的解析职责必须由独立接口承担**
   - VFX 系统只能依赖位置解析接口
   - 不要在具体实现阶段为了图省事回退成直接传 `Transform`
   - 一旦回退，后面附着、失效语义、场景切换都会重新耦死

4. **MotionRegistry 的成功标准必须按真实边界理解**
   - 正确口径不是“新增一种运动完全不改旧文件”
   - 正确口径是：**不改 BulletMover 等核心热路径逻辑，只在受控扩展点增量接入**
   - 这点现在计划里已经基本修正，但执行验收时要按这个标准看，不要被过度承诺反咬

5. **迁移器验收不能只看 SO 资产本体，必须看 prefab / scene 实例**
   - 这是 Unity 项目最容易漏的坑
   - 资产字段迁移成功，不等于场景实例引用仍然正确
   - 迁移器必须输出实例扫描和缺失引用报告，否则验收不算过

---

### 8.3 当前剩余“未决项”判断

我的判断是：

**严格意义上的架构未决项，当前已经清零。**

也就是说，当前文档集的统一口径应理解为：
- **架构级未决项：无**
- **执行级待落文件项：有，但不阻塞 Phase 0 启动**
- **验收时不得再把执行级待落项误表述成架构方向未定**

上一轮 PK 新增的三项执行契约缺口，现已正式落为 ADR-023 ~ ADR-025：
- `OnValidate` / 域重载 / 热重载边界
- 统一资源描述 `SchemaVersion` 与顺序迁移链路
- 资源变更 → Registry 重建 → Batch 预热 的编辑器刷新工作流

如果还要挑“尚未完全落到代码文件、但文档口径已明确”的点，当前主要剩下这些实现级事项：

- `RenderSortingOrder` 需要在代码中形成唯一来源
- `CollisionEventBuffer` 默认容量需要在实现时正式固化（建议 256）
- `IAttachSourceResolver` 这类接口命名与归属需要在编码时一次定准
- 迁移器的 dry-run / apply / 报告输出格式需要在工具实现时补齐
- 子弹资源描述需要正式扩展为 `Static/SpriteSheet` 双采样模式，并补齐旧 SO 迁移与验收口径
- 子弹序列帧的时间源、循环策略与 UV 采样规则需要在代码实现时保持与 Bullet 生命周期一致，不能借道 VFX 播放模型

这些都属于**实现细节待落文件**，不再属于**架构边界未定**。

### 8.3.1 序列帧子弹的实现级落地判断（2026-04-12 补充）

对 ADR-026 再补一句更落地的判断：

- 这次改造的本质是 **Bullet 视觉采样策略扩展**，不是 `BulletCore` 热路径数据重构
- 第一版最稳的落地方式，是在 `BulletTypeSO` 引入 `Static/SpriteSheet` 双采样模式，并由 `BulletRenderer` 统一解释 `Lifetime/Elapsed -> frameIndex -> UVRect`
- 当前项目的 `BulletRenderer` 仍直接读取 `type.AtlasUV` 写顶点，说明代码层面尚未支持序列帧子弹；这不是理念问题，是明确的实现缺口
- 我**不建议**第一版把 `frameIndex`、播放状态或随机起始帧缓存下沉进 `BulletCore`，因为这会把纯视觉派生值提前固化进热路径数据，复杂度先涨、收益不成比例
- 我**也不建议**在本轮顺手统一飞行阶段序列帧子弹与 `ExplosionMode.MeshFrame` 爆炸帧动画；两者虽然都叫“帧动画”，但生命周期语义不同，硬统一只会扩大改造面
- 正确的工程顺序应是：先补 `BulletTypeSO` 新字段模型 -> 再补 `BulletTypeMigrationTool` -> 最后切 `BulletRenderer.ResolveBulletUV()`；而不是一边改渲染一边手工补资产

这部分结论的核心不是“能不能做”，而是“别把一个清楚的视觉能力扩展，做成新的系统级重构”。

---

### 8.4 全量 Phase 的统一验收口径（补充）

为了避免后续其他评审把“实现待落项”重新误判成“方向未定”，这里把全量 Phase 的统一验收口径再收一遍：

- **Phase 0**：验收重点不是“新建了多少共享文件”，而是共享 Rendering 边界、容量收拢边界、sortingOrder 唯一来源是否落成单一真相；新增层位只能经由 `RenderSortingOrder` 进入系统
- **Phase 1**：验收重点不是“能不能先跑起来”，而是 Bullet/VFX 是否真正共享统一资源描述语义，且 atlas 仍保持可选优化而非生产前置；序列帧子弹样例必须覆盖 `FixedFpsOnce`
- **Phase 2**：验收重点不是“事件系统看起来更高级”，而是 `CollisionEventBuffer` 是否始终停留在旁路通道定位、`MotionRegistry` 是否守住受控扩展点边界、Danmaku × VFX 是否在无中间态硬耦合的前提下完成解耦；目标压测基线中持续 overflow 视为不通过
- **Phase 3**：验收重点不是“视觉效果堆了多少”，而是附着式 VFX 语义、AttachSource 解析边界、Bullet 视觉动画性能回退口径是否全部自洽；旧 handle 进入冻结收尾态后不得自动恢复跟随
- **Phase 4**：验收重点不是“工具数量”，而是迁移器、热重载、Atlas 工具是否形成统一工作流，并且失败时有明确回退与报告机制；migration 必须区分阻断错误与警告

统一理解为一句话：

> **所有 Phase 都应按“边界是否守住、契约是否单一、回退是否明确”验收，而不是按“功能先堆出来再说”验收。**

### 8.5 最终评审意见

**评审结论：通过。**

**附带要求：**
- 后续不建议继续扩方案分支
- 新约束先写 ADR，再同步计划和审计文档
- 所有 Phase 的补充条款都应视为正式执行契约，不再作为开放讨论项
- 开工后优先验证的是“边界是否守住”，不是“功能是否堆得更快”

说白了：

> 这版计划现在已经够稳，可以开工。
> 真正的风险不在“想不清楚”，而在“实现时手痒乱加后门”。

只要守住这条线，这次重构大概率不会长歪。

---

## 九、对 Unity 架构师最终 25 个问题的正式答复（最终定稿）

> 本节不是再提方向，而是把最后一次评审中的所有问题逐条落成可执行口径。若与实现发生冲突，以本节 + ADR + REFACTOR_PLAN 的一致结论为准。

### 9.1 单一真相类问题

#### Q1. `RenderLayer`、`sortingOrder`、`MaterialKey/BlendMode` 三者最终归属是什么？
**答复：**
- `RenderLayer` = 语义层。只表达“这类内容属于哪一层渲染语义”，例如 BulletNormal / BulletAdditive / VFXNormal / VFXAdditive 所属域，不承载具体排序数值。
- `sortingOrder` = 表现层。唯一代码来源是 `RenderSortingOrder`，业务代码禁止写裸 `int`。
- `MaterialKey/BlendMode` = 材质层。由共享渲染层统一维护映射关系，禁止 Bullet/VFX/Laser 各自解释。
- 禁止重复定义的点：
  1. 业务类中禁止内联新的 sortingOrder 常量
  2. 各系统禁止私有维护 `MaterialKey -> BlendMode` 对照表
  3. 调试 HUD、验收截图、文档说明必须引用同一命名体系

#### Q2. Laser 是统一资源描述的一部分，还是共享渲染基础设施消费者？
**答复：**
- 第一版结论：Laser 是**共享渲染基础设施消费者**，不是统一资源描述值对象的强制覆盖对象。
- 但 Laser **必须**遵守共享渲染契约：`RenderLayer`、`RenderSortingOrder`、`MaterialKey/BlendMode`。
- 如果未来 Laser 也要纳入 `SourceTexture + UVRect` 语义，必须新增 ADR，不允许实现阶段自行扩张。
- 因此，“Laser 特殊”只允许特殊在资源描述模型，不允许特殊在排序和材质契约。

#### Q3. `RenderBatchManager` 是否硬性禁止运行时隐式建桶？
**答复：**
- 是，硬性禁止。
- 未注册 `(RenderLayer, SourceTexture, MaterialKey/BlendMode)` 的行为：
  - Editor/Dev：错误日志 + 计数 + 保留旧状态
  - Release：跳过渲染 + 计数，不自动补桶
- 动态加载新资源的正式路径固定为：
  `注册/标脏 -> Registry 重建 -> Batch 预热 -> 结果报告 -> 允许显示`
- 任何热路径（Play/Spawn/Render）都不得承担补注册、补预热、补刷新职责。

### 9.2 资源模型与迁移问题

#### Q4. `SchemaVersion` 第一版覆盖哪些资产？
**答复：**
- 第一版强制纳入版本链：`BulletTypeSO`、`VFXTypeSO`
- `LaserTypeSO`、`SprayTypeSO` 本轮不纳入统一资源描述版本链
- 但若 `LaserTypeSO` / `SprayTypeSO` 引用共享渲染契约字段，仍必须遵守共享契约，不得自行分叉
- 未纳入版本链的资产，不允许偷偷承载统一资源描述演进责任；需要演进时必须显式升级到版本链

#### Q5. migration 的阻断错误和警告如何分级？
**答复：**
- **阻断错误**：
  - 缺失 `SourceTexture`
  - 非法 `Static + PlaybackMode`
  - 非法 `SpriteSheet + Reverse/PingPong/RandomStartFrame`
  - prefab/scene 实例引用断裂
  - 共享契约字段缺失导致无法生成合法注册项
- **警告**：
  - 旧字段仍存在但新字段可自动补齐
  - atlas 映射缺失但仍可回退到 `SourceTexture + UVRect`
  - 默认值可自动修正但需要归档说明
- `dry-run` 必须输出阻断错误/警告分级；任一阻断错误存在时禁止进入 `apply`

#### Q6. 兼容退出机制是否是版本级硬门槛？
**答复：**
- 是，属于版本级硬门槛。
- “阻断错误为 0”必须同时覆盖：基线资产集 + prefab/scene 实例扫描
- 必须完成一次正式 `report` 归档，才允许进入下一 schema 版本的兼容删除阶段
- “最多保留一个过渡版本周期”按 `schema+1` 解释：在下一个 schema 版本中必须删除旧兼容字段与运行时 fallback

### 9.3 序列帧子弹问题

#### Q7. `SpriteSheet` 子弹允许几种时间源？
**答复：**
- 第一版只允许两类时间源，并与播放模式一一绑定：
  - `StretchToLifetime` -> `lifetime / maxLifetime`
  - `FixedFpsLoop` / `FixedFpsOnce` -> `elapsedSeconds`
- 同一配置禁止混用双时间源
- 这意味着配置层不会出现“生命周期归一化 + 固定 FPS 同时生效”的歧义状态

#### Q8. `ResolveBulletUV()` 的职责边界是什么？
**答复：**
- 只负责：`采样模式 -> frameIndex -> UVRect`
- 不负责：颜色、缩放、Alpha、爆炸、命中、材质切换等其他视觉逻辑
- 残影必须复用同一 UV 解析入口，禁止再长一套“残影专用帧解析”逻辑
- 若未来函数开始承担额外职责，应拆分而不是继续膨胀

#### Q9. 序列帧子弹与 `ExplosionMode.MeshFrame` 是否保持两套模型？
**答复：**
- 是，当前明确保持两套模型
- 这不是“暂时没空统一”，而是“第一版明确不统一”
- 若未来要统一，必须新增 ADR，且统一依据必须写清楚（资源描述统一还是播放语义统一）
- 当前验收明确禁止借“顺手统一爆炸帧动画”扩大改造面

### 9.4 attached VFX 问题

#### Q10. `PlayAttached / UpdateAttached / StopAttached` 是强制三段式吗？
**答复：**
- 是，强制三段式，不是建议式
- `PlayAttached` 只负责创建与绑定
- `UpdateAttached` 必须是显式调用，不允许系统偷偷轮询某个 `Transform` 自行跟随
- `StopAttached` 是唯一合法的主动结束入口

#### Q11. 同一 `AttachSourceId + VFXType` 重复 `PlayAttached` 的语义是否唯一？
**答复：**
- 唯一语义：先停止旧 handle，再创建新 handle
- “停止旧 handle”按立即切换处理，不允许旧 handle 进入并存过渡态
- 新旧 handle 切换过程中不允许一帧并存
- 这样做的目的就是彻底消灭“偶现双播”

#### Q12. Resolver 失败的可观测性暴露到什么层级？
**答复：**
- 至少暴露以下三个层级：
  1. 失败计数
  2. 最近一次失败原因
  3. 验收报告统计入口
- Debug 日志允许存在，但日志不是唯一可观测手段
- HUD 展示可选，不作为唯一依赖

#### Q13. 目标失效后冻结并播完，是不是默认语义？
**答复：**
- 是，默认统一为“冻结到最后有效位置并播完”
- “立即结束”只能通过显式配置开启
- 进入冻结收尾态后，目标恢复也绝不允许旧 handle 自动恢复跟随；恢复只能重新 `PlayAttached`

### 9.5 事件与运行时边界问题

#### Q14. `CollisionEventBuffer` 是否永远禁止承载主逻辑事实？
**答复：**
- 是，永远禁止
- 伤害、击退、死亡、状态变更不得依赖 Buffer 消费结果
- 命名、注释、文档、验收都按“Visual/Observer Only”理解

#### Q15. `CollisionEventBuffer` overflow 的验收口径是否够硬？
**答复：**
- 统计口径固定为：**按事件计数、按性能验收窗口累计**
- 统计窗口与 55fps 性能窗口完全一致
- 性能窗口内 `overflow count > 0` 直接判失败，不区分“偶发一次”还是“持续增长”

#### Q16. `EffectsBridge` 是否只消费旁路事件，不允许反向写回主状态？
**答复：**
- 是，只能消费旁路事件
- 允许做：VFX、飘字、调试输出、统计
- 禁止修改：BulletWorld、CollisionSolver、MotionRegistry、主状态机、主伤害事实

### 9.6 编辑器工作流问题

#### Q17. `OnValidate` 的禁止事项是否是代码审查红线？
**答复：**
- 是，属于代码审查红线
- 一律不允许：
  - `OnValidate` 直接重建 Registry
  - `OnValidate` 直接预热 Batch
  - `OnValidate` 直接改运行时池
  - `OnValidate` 偷做跨版本迁移

#### Q18. 关闭 Domain Reload 时，谁负责清理静态状态？
**答复：**
- 规则不是“某一个总控类兜底”，而是“每个持有静态缓存的模块都必须提供显式重置入口”
- 统一由编辑器 orchestration 在进入 PlayMode 前触发这些重置入口
- 任一模块缺失重置能力，视为验收不通过

#### Q19. “标脏 -> Registry 重建 -> Batch 预热 -> 结果报告”是否是唯一合法刷新链路？
**答复：**
- 是，唯一合法链路
- Atlas 工具、迁移器、Inspector 修改、批量工具都必须走同一 orchestration
- 任一步失败都必须中断后续步骤并保留旧状态，禁止“部分成功但静默继续”

### 9.7 验收与范围控制问题

#### Q20. 30 分钟上手目标的测试起点是否完全固定？
**答复：**
- 是，固定为：模板默认状态 + 已有示例资产 + 指定 Demo 场景
- 允许：查看 Guide、复制现有 `BulletTypeSO` / `VFXTypeSO` / Registry 示例资产、使用现有 Editor 工具与 Unity 原生 Inspector
- 不允许：改代码、写临时脚本、手改 prefab/scene 序列化文本、依赖隐藏入口
- 验收应由未接触过模板的开发者实际走一遍，不接受纯理论通过

#### Q21. 55fps 验收目标的测试环境是否唯一？
**答复：**
- 是，必须唯一
- 固定项：指定基线机型、固定 Demo 场景、Release/IL2CPP、持续 30 秒、关闭会污染结果的调试开关
- 必须记录 build hash / 配置快照，避免“不是同一包”
- 不采用“多机型取最低值”的模糊口径；以指定基线机型为准

#### Q22. `DrawCall ≤ 50`、`活跃 Batch ≤ 24` 是硬失败还是预警失败？
**答复：**
- 最终验收一律按硬失败处理
- 任一项超限即判不通过
- 开发阶段允许预警，但最终验收不允许“视觉正确但预算超限先放行”

#### Q23. 是否需要一版“必须做 / 允许做 / 明确不做 / 未来扩展点”总表？
**答复：**
- 需要，而且已经作为正式范围控制要求写入
- 特别是以下内容必须明确归类为“第一版明确不做”或“未来扩展点”：
  - `PingPong/Reverse/RandomStartFrame`
  - `frameIndex` 下沉 `BulletCore`
  - 爆炸帧动画统一
  - 完整阵营矩阵编辑器
  - Socket 完整实现
  - 运行时动态补桶
  - 长期 fallback 兼容

### 9.8 两个元问题

#### Q24. 当前文档是否已经把“原则”和“建议实现”分层写清楚？
**答复：**
- 现在的正式口径是：
  - 原则：单一真相、运行时禁后门、迁移只在资产层、attached VFX 三段式、唯一刷新链路、硬失败验收
  - 当前推荐实现：`RenderSortingOrder`、受控 `MotionRegistry`、`ResolveBulletUV()`、显式 Resolver 接口等
- 后续评审与实现必须按这套分层理解，不允许把“当前推荐实现”误写成“不可变架构原则”

#### Q25. 现在到底能不能开工，还是必须先补完这些答案？
**答复：**
- 现在可以开工
- 因为这些问题已经不是“待回答”，而是已经全部回写成正式文档条款
- 后续不再做评审和修正；实现阶段只允许按文档落地，不允许重新开问题分支


---

## 九、给 Phase 0 的具体执行指令

Phase 0 可以立即开始，以下是精确的文件操作清单：

```
1. 新建 _Framework/Rendering/MODULE_README.md
2. 新建 _Framework/Rendering/Scripts/RenderVertex.cs
   ← 从 DanmakuSystem/Scripts/Data/DanmakuVertex.cs 复制，改命名空间为 MiniGameTemplate.Rendering
3. 新建 _Framework/Rendering/Scripts/RenderBatchManager.cs（骨架）
4. 新建 _Framework/Rendering/Scripts/IRenderBatchProvider.cs
5. 改 BulletRenderer.cs：using MiniGameTemplate.Rendering + 将 DanmakuVertex 替换为 RenderVertex
6. 改 LaserRenderer.cs：同上
7. 改 VFXBatchRenderer.cs：移除 using MiniGameTemplate.Danmaku + using MiniGameTemplate.Rendering
8. 删 DanmakuVertex.cs（或保留为 [Obsolete] 的 type alias）
9. 容量收拢：ObstaclePool/TrailPool/PatternScheduler/SpawnerDriver/TargetRegistry/AttachSourceRegistry/VFXPool 的容量按分层策略收拢；DamageNumberSystem 单独评估，不强绑主链路

10. batchmode 编译验证
11. 打开 DanmakuDemo + VFXDemo 场景验证功能正常
```

**等你确认后启动。**


