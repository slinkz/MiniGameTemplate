# 弹幕系统 — 渲染架构

> **预计阅读**：10 分钟 &nbsp;|&nbsp; **前置**：先读 [弹幕系统总览](DANMAKU_SYSTEM.md) 了解整体架构
>
> 本文档覆盖弹幕系统的渲染管线：统一顶点格式、RenderBatchManager 分桶提交、RuntimeAtlas 动态图集、renderQueue GPU 级层序、拖尾系统、VFX 编排、伤害飘字渲染。

---

## 统一顶点格式

所有 2D Quad 渲染（弹丸、激光、VFX、飘字）共享同一顶点结构，位于 `_Framework/Rendering/RenderVertex.cs`：

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct RenderVertex
{
    public Vector3 Position;   // 12 bytes, offset=0
    public Color32 Color;      // 4 bytes,  offset=12
    public Vector2 UV;         // 8 bytes,  offset=16
}
// sizeof = 24 bytes
```

**重要**：字段顺序必须严格遵循 Unity 标准属性排序 `Position → Color → TexCoord0`。
详见 `CONVENTIONS.md` 的"Mesh 顶点布局规范"。

---

## RenderBatchManager 分桶渲染

弹幕渲染的核心是 `RenderBatchManager`（简称 RBM），位于 `_Framework/Rendering/`。

### 架构概览

```
每个 Renderer 持有独立的 RBM 实例：
  BulletRenderer  → RBM（RuntimeAtlas 纹理，renderQueue=3100）
  LaserRenderer   → RBM（独立贴图，renderQueue=3120）
  LaserWarning    → RBM（独立贴图，renderQueue=3120）
  VFXBatchRenderer → RBM（RuntimeAtlas 纹理，renderQueue=3200）
  DamageNumber    → RBM（RuntimeAtlas DamageText，renderQueue=3300）
  TrailPool       → 独立 Mesh + Graphics.DrawMesh（renderQueue=3090）
```

### 分桶规则

每个 RBM 初始化时通过 `BucketRegistration` 注册所有桶：

```csharp
new BucketRegistration(
    key: new BucketKey(RenderLayer.Normal, texture),
    templateMaterial: bulletMaterial,
    sortingOrder: RenderSortingOrder.Bullet   // 100
)
```

- **BucketKey** = `(RenderLayer, Texture)` 二元组
- 每个桶创建材质实例，设 `material.renderQueue = 3000 + sortingOrder`
- 初始化末尾按 SortingOrder 升序排列 `_buckets` 数组

### 每帧流程

```
batchManager.ResetAll()           ← 帧头清零所有桶的 QuadCount
// 遍历实体，按纹理查桶，写入顶点
batchManager.UploadAndDrawAll()   ← 帧尾 SetVertexBufferData + Graphics.DrawMesh
```

> **注意**：ADR-029 v2 移除了 Additive Blend。RenderLayer 枚举只剩 `Normal = 0`。

---

## 渲染提交顺序（renderQueue）

渲染层序由 `material.renderQueue` 值控制，**不依赖代码调用顺序**：

| 子系统 | renderQueue | 说明 |
|--------|-------------|------|
| Trail | 3090 | 在弹丸后方 |
| Bullet | 3100 | 主体 |
| Laser / LaserWarning | 3120 | — |
| VFX | 3200 | 特效在弹丸前方 |
| DamageNumber | 3300 | 最前方（飘字不被遮挡） |

> **经验教训**：`Graphics.DrawMesh` 跨 RBM 实例的层级控制必须靠 `renderQueue`，不能靠调用顺序。

### LateUpdate 管线

```
DanmakuSystem.RunLateUpdatePipeline()
  RenderBatchManagerRuntimeStats.BeginFrame()
  ├── TrailPool.Render()                     ← 独立 Mesh + Graphics.DrawMesh
  ├── BulletRenderer.Rebuild + UploadAndDrawAll
  ├── LaserRenderer.Rebuild + UploadAndDrawAll
  ├── LaserWarningRenderer.Rebuild + UploadAndDrawAll
  ├── IDanmakuVFXRuntime.RenderVFX()         ← R4.0 收编
  └── DamageNumberSystem.Rebuild(dt) + UploadAndDrawAll
  RenderBatchManagerRuntimeStats.EndFrame()
```

---

## RuntimeAtlas 动态图集

弹丸/VFX/飘字的纹理通过 `RuntimeAtlasSystem` 在运行时按需 Blit 到动态 Atlas RenderTexture。

### 纹理解析链（RuntimeAtlasBindingResolver）

```
优先级：RuntimeAtlas 分配 → AtlasBinding → SourceTexture → fallback
```

- **Bullet/VFX**：优先走 RuntimeAtlas（Channel=Bullet/VFX）
- **DamageNumber**：走 RuntimeAtlas（Channel=DamageText）
- **Laser/LaserWarning**：不入 Atlas（UV.y 是 world-space 累积长度，Atlas 子区域会破坏 wrap 采样）

### 关键设计

- **Shelf Packing**：Best-Fit 算法，O(N) 搜索，零 GC
- **Blit 方式**：CommandBuffer + SetRenderTarget（WebGL 2.0 兼容）
- **切关清空**：场景切换时 Reset 所有 Channel
- **RT Lost 恢复**：HandleRTLost + RestoreDirtyPages 两阶段恢复

> 详细设计：`Docs/Agent/RUNTIME_ATLAS_SYSTEM_TDD.md`（v2.10.1）

---

## 弹丸旋转

- **圆弹**（`RotateToDirection = false`）：轴对齐四边形，4 次加法
- **米粒弹**（`RotateToDirection = true`）：`cos/sin` 旋转顶点（BulletCore 预计算缓存）

2048 颗全旋转额外 ~0.3-0.5ms。圆弹走快速路径跳过旋转。

---

## 激光渲染

激光由 `LaserRenderer` 和 `LaserWarningRenderer` 各自通过独立 RBM 渲染。

### LaserRenderer 管线

```
LaserPool.Data[]
  │  遍历活跃激光（Phase > 0 且 SegmentCount > 0）
  ├→ GetPhaseAlpha() 计算阶段透明度
  │    Charging(1): 正弦闪烁 0.3~0.8
  │    Firing(2):   1.0 全亮
  │    Fading(3):   线性衰减 → 0
  └→ 每段 LaserSegment → WriteSegmentQuad()
       ├→ 沿线段方向展开 Quad（4 顶点）
       ├→ 宽度由 WidthProfile 沿总长度归一化采样
       ├→ UV.x: 0→1 横跨宽度
       ├→ UV.y: 沿长度方向连续映射
       └→ Color32: CoreColor × Phase alpha
```

### Shader

- `DanmakuLaser.shader`：`Blend SrcAlpha One`（叠加发光），`ZTest Always`
- `DanmakuBullet.shader`：弹丸/VFX/飘字通用 Alpha Blend

---

## 拖尾系统

通过 `BulletTypeSO.Trail` 配置，支持三种模式（`TrailMode` 枚举）：

### Ghost 残影（TrailMode.Ghost / Both）

BulletRenderer 在渲染弹丸时额外画 2-3 个缩小 + 降低 alpha 的残影四边形。

```
弹丸飞行方向 →
  [残影3]   [残影2]   [残影1]   [弹丸本体]
  α=0.15    α=0.3     α=0.6     α=1.0
  scale=0.5  scale=0.7  scale=0.85  scale=1.0
```

**关键实现**：
- `GhostFrameCounter` + `GhostInterval`（默认 5 帧）控制采样间隔，避免低速弹丸残影堆叠
- `GhostFilledCount` 避免首几帧显示假残影
- 残影使用弹丸预计算的 cos/sin 正确旋转
- 残影写入顺序在弹丸之前（先 Ghost 后弹丸），确保弹丸覆盖残影

### Trail 曲线拖尾（TrailMode.Trail / Both）

`TrailPool` 管理独立 Mesh 三角带拖尾，通过 `Graphics.DrawMesh` 提交。

```
TrailPool (64 条实例容量)
  ├── 每条 Trail：20 个采样点 × 2 顶点 = 40 顶点
  ├── MIN_SAMPLE_DISTANCE = 0.15（低速弹丸距离门槛）
  ├── 末尾点始终更新为弹丸当前位置（头部紧贴）
  ├── alpha = t（t=0 尾部透明，t=1 头部不透明）
  ├── width *= t（尾部细，头部粗）
  └── IndexFormat.UInt32（匹配 int[] 类型）
```

- **Material**：与弹丸共享 `DanmakuBullet.shader`（Alpha Blend）
- **renderQueue**：3090（在 Bullet 后方）
- **统计**：已接入 `RenderBatchManagerRuntimeStats`

### Both 模式

同时启用 Ghost + Trail。Ghost 跟随弹丸本体渲染（RBM 内部），Trail 作为独立 Mesh 提交。

---

## VFX 特效编排

### 轻量 VFX：SpriteSheetVFXSystem

R4.0 后 `SpriteSheetVFXSystem` 退化为纯 API 入口——不再自驱 `Update/LateUpdate`。
由 `DanmakuSystem` 管线通过 `IDanmakuVFXRuntime.TickVFX(dt)` / `.RenderVFX()` 统一驱动。

- 碰撞命中 → `CollisionEventBuffer` → `EffectsBridge` → `SpriteSheetVFXSystem.PlayOneShot`
- 清屏 → `ClearAllBulletsWithEffect()` → 逐弹丸触发 VFX
- 喷雾 VFX：Attached 走 `PlayAttached`，Detached 走 `Play`（世界空间固定位置）

### 重量特效

Boss 大招等走 `PoolManager` 对象池，取预制件。同屏 ≤ 5 个。

### 子弹幕触发

弹丸消亡且设了 `FLAG_HAS_CHILD` 时，`BulletMover` 在回收前以当前位置发射 `ChildPattern`。

> **深度限制**：`DanmakuTypeRegistry.AssignRuntimeIndices()` 初始化时 DFS 检测环引用。
>
> **子弹幕基准角**：子 Pattern 配 `AimAtPlayer` → 母弹→玩家方向；否则 → 母弹飞行方向。

---

## 伤害飘字渲染

### DamageNumberSystem（R3 迁移到 RBM + RuntimeAtlas）

- `NumberAtlas`（0-9 数字贴图）通过 RuntimeAtlas（Channel=DamageText）分配
- 128 容量环形缓冲区，弹出+淡出动画
- 数字 UV 计算基于 `_fallbackAtlas.width / 10` 像素宽度推导（不依赖魔法数）
- 通过独立 RBM 实例提交，`renderQueue = 3300`（最前方，不被遮挡）

### 低频飘字

Boss 名字、技能名等走 FairyGUI 富文本，同屏 ≤ 10 个。

---

## Draw Call 预算

| 子系统 | 典型 DC | 说明 |
|--------|---------|------|
| Bullet（含 Ghost） | 1-3 | 按纹理分桶，2-3 种弹丸类型 |
| Laser + LaserWarning | 1-2 | 各 1 DC |
| Trail | 1 | 独立 Mesh |
| VFX | 1 | 独立 RBM |
| DamageNumber | 1 | 独立 RBM |
| **典型总计** | **4-8** | 远低于 50 DC 预算 |

---

**相关文档**：
- [弹幕系统总览](DANMAKU_SYSTEM.md) — 系统架构、武器类型、框架集成
- [弹幕系统 — 数据结构](DANMAKU_DATA.md) — 所有运行时 struct 和枚举
- [弹幕系统 — SO 配置体系](DANMAKU_CONFIG.md) — 所有 ScriptableObject 定义
- [弹幕系统 — 碰撞与运行时](DANMAKU_COLLISION.md) — 碰撞系统、延迟变速、DanmakuSystem 入口
