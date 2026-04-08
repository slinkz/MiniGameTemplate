# 弹幕系统 — 渲染架构

> **预计阅读**：10 分钟 &nbsp;|&nbsp; **前置**：先读 [弹幕系统总览](DANMAKU_SYSTEM.md) 了解整体架构
>
> 本文档覆盖弹幕系统的渲染管线：交错顶点 Mesh 上传、分层合批、弹丸旋转、图集方案、拖尾系统、爆炸特效与子弹幕触发、伤害飘字渲染。

---

## Mesh 上传优化

弹丸渲染的核心瓶颈是 CPU 到 GPU 的数据上传。采用**交错顶点格式 + 单次上传**策略。

### 顶点格式

```csharp
[StructLayout(LayoutKind.Sequential)]
struct DanmakuVertex
{
    public Vector3 Position;   // 12 bytes
    public Vector2 UV;         // 8 bytes
    public Color32 Color;      // 4 bytes
}
// sizeof = 24 bytes
```

### BulletRenderer — 双 Mesh 分层

```csharp
public class BulletRenderer
{
    private Mesh _meshNormal;                   // Alpha Blend 层
    private Mesh _meshAdditive;                 // Additive 层
    private DanmakuVertex[] _verticesNormal;
    private DanmakuVertex[] _verticesAdditive;

    private VertexAttributeDescriptor[] _layout = new[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
    };

    public void Initialize(int maxQuads, DanmakuRenderConfig renderConfig) { /* ... */ }

    public void Rebuild(BulletWorld world, DanmakuTypeRegistry registry)
    {
        // 遍历活跃弹丸，按 RenderLayer 分拣到对应顶点数组
        // 各自 1 次 SetVertexBufferData + 1 次 DrawMesh
    }
}
```

**性能对比**：

| 方案 | 每帧拷贝次数 | 数据量（8192 quads） |
|------|:---:|---:|
| 旧方案：`SetVertices` + `SetUVs` + `SetColors` + `SetTriangles` | 4 次 | ~960 KB |
| 新方案：交错顶点 + `SetVertexBufferData` | **1 次** | ~768 KB |

> **WebGL 兼容**：Unity 2022 WebGL 不支持 `NativeArray` 版 `SetVertexBufferData`，但 `T[]` 版本可用。`Mesh.MarkDynamic()` 在 WebGL 上对应 `GL.DYNAMIC_DRAW`。

---

## 分层 Mesh 合批

```
┌─────────────────────────────────────────────────┐
│                   每帧渲染                        │
│  Mesh Layer 0: 弹丸主体 + 残影     ── 1 DC      │  ← Alpha Blend
│  Mesh Layer 1: 发光弹丸 + 残影     ── 1 DC      │  ← Additive
│  Mesh Layer 2: 伤害飘字（数字精灵）── 1 DC       │  ← Number Atlas
│  LaserPool:    激光                ── 1-2 DC     │
│  TrailPool:    重量级拖尾          ── 1-3 DC     │
│  EffectPool:   中/重特效           ── 3-5 DC     │
│  SprayVFX:     喷雾 ParticleSystem ── 1-3 DC     │
│  FairyGUI:     低频文本/UI         ── 已合批      │
│  总计: 7-11 Draw Call                            │
└─────────────────────────────────────────────────┘
```

---

## 弹丸旋转与排序

### 旋转

- **圆弹**（`RotateToDirection = false`）：轴对齐四边形，4 次加法
- **米粒弹**（`RotateToDirection = true`）：`Atan2` + `Sin/Cos` 旋转顶点

2048 颗全旋转额外 ~0.3-0.5ms。圆弹走快速路径跳过旋转。

### 排序

所有弹丸同一深度，不排序。Additive 天然不需排序，Alpha Blend 密集重叠时略有差异但弹幕游戏不在意。

---

## 图集方案

弹幕系统使用**自定义规则网格图集**，不用 Unity Sprite Atlas。

| | Sprite Atlas | 自定义图集 |
|---|-------------|-----------|
| 和自定义 Mesh 配合 | 需查 `Sprite.uv` | 整数除法算 UV，极快 |
| WebGL 兼容性 | Late Binding 坑 | 直接加载 Texture2D |
| 序列帧 UV 计算 | 布局随机，需查表 | 规则网格，`row × col` |

其他系统（UI、场景装饰）继续用 Sprite Atlas。图集打包工具是 Editor 菜单项：散图 → 规则网格图集 + `BulletAtlasConfig` SO。

---

## 拖尾系统

通过 `BulletTypeSO.Trail` 配置，支持两种模式：

### Ghost 模式（Mesh 内残影）

`BulletTrail` 存储最近 3 帧历史位置，合批时额外画 2-3 个缩小 + 降低 alpha 的四边形。

```
弹幕飞行方向 →
  [残影3]   [残影2]   [残影1]   [弹幕本体]
  α=0.15    α=0.3     α=0.6     α=1.0
  scale=0.5  scale=0.7  scale=0.85  scale=1.0
```

2048 × 4 四边形 = 8192 quads = 32K 顶点，仍然 1 Draw Call。

### Trail 模式（独立曲线拖尾）

连续曲线拖尾（激光蛇形弹道等），沿历史轨迹生成三角带 Mesh。`TrailPool` 预分配 16-32 条实例，共享 Material 自动 Dynamic Batching。16 条 ≈ 1-3 DC。

---

## 爆炸特效与子弹幕触发

### 轻量：Mesh 内爆炸帧（零额外开销）

弹丸命中后切换到 `Phase = Exploding`，渲染时按 `ExplosionFrameCount` 偏移 UV 到爆炸帧序列，播完后回收。500 颗同时消失 → 零额外 DC，零 GC。

### 重量：对象池特效

Boss 大招等走 `EffectPool`，通过 `PoolManager` 取预制件。同屏 ≤ 5 个。

### 子弹幕触发

弹丸消亡（HP=0 / 超时）且设了 `FLAG_HAS_CHILD` 时，`BulletMover` 在回收前以当前位置为 origin 发射 `ChildPattern`。

> **深度限制**：`DanmakuTypeRegistry.AssignRuntimeIndices()` 初始化时 DFS 检测环引用。运行时零开销。
>
> **子弹幕基准角（P2-5）**：子 Pattern 配 `AimAtPlayer` → 母弹→玩家方向；否则 → 母弹飞行方向。

---

## 伤害飘字渲染

### 高频飘字：数字精灵 Mesh 合批

- `NumberAtlas`（0-9 数字贴图）+ `DamageNumberSystem` 环形缓冲区（128 容量）
- 每帧和弹丸一起合批，按 digit 索引 UV
- 同屏 100 个飘字 ≈ 0 额外开销，1 DC

### 低频飘字：FairyGUI 文本对象池

Boss 名字、技能名等走 FairyGUI 富文本，同屏 ≤ 10 个。

---

**相关文档**：
- [弹幕系统总览](DANMAKU_SYSTEM.md) — 系统架构、武器类型、框架集成
- [弹幕系统 — 数据结构](DANMAKU_DATA.md) — 所有运行时 struct 和枚举
- [弹幕系统 — SO 配置体系](DANMAKU_CONFIG.md) — 所有 ScriptableObject 定义
- [弹幕系统 — 碰撞与运行时](DANMAKU_COLLISION.md) — 碰撞系统、延迟变速、DanmakuSystem 入口
