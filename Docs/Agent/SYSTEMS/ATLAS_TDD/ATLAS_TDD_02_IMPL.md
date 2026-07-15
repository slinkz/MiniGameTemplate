---
system: runtime-atlas
scope: implementation-migration
last_verified: 2026-05-02
depends_on: [ATLAS_TDD_01_DESIGN]
related_code: Assets/_Framework/RuntimeAtlas/*.cs, Assets/_Framework/Rendering/*.cs
---


### 4.1 与 Editor Atlas 工具链的关系（v2.0 重要修正）

**Editor Atlas 工具链保留，不删除。**

| 维度 | Editor Atlas (Phase 4.1/4.2) | RuntimeAtlasSystem |
|------|------------------------------|---------------------|
| 执行时机 | 编辑器手动操作 | 运行时自动 |
| 产物 | `AtlasMappingSO` + `Texture2D` (资产) | `RenderTexture` (运行时临时) |
| 持久化 | 是（持久化到项目中） | 否（切关即销毁） |
| **地位** | 离线工具——预览、资产管理、导出验证 | **运行时渲染核心基础设施** |
| **删除风险** | **不删除** | — |

保留 Editor Atlas 的理由：
1. **资产预览**：策划在 Inspector 中需要看到"打包后的效果"
2. **导出验证**：可以对比 Editor Atlas 和 Runtime Atlas 的结果是否一致
3. **兼容回退**：极端情况下可降级为静态 Atlas 方案
4. **零成本保留**：不删代码 = 零风险

**共存策略变化（v2.0 vs v1.0）：**

| 场景 | v1.0 行为 | v2.0 行为 |
|------|----------|----------|
| TypeSO 有 AtlasBinding | RuntimeAtlas 跳过，用 Editor Atlas | **RuntimeAtlas 接管**，忽略 AtlasBinding（运行时统一走 RuntimeAtlas） |
| TypeSO 无 AtlasBinding | RuntimeAtlas 接管 | RuntimeAtlas 接管 |
| Editor 预览模式 | — | Editor Atlas 仍可用于 Inspector 预览 |

> **⚠️ 未决项 UD-09**：运行时是否完全忽略 AtlasBinding（v2.0 推荐），还是保留"如果有 AtlasBinding 就跳过 RuntimeAtlas"的兼容路径？
>
> **推荐**：运行时完全由 RuntimeAtlasSystem 接管，AtlasBinding 仅用于 Editor 预览。理由：统一管线不应有两条纹理解析路径。

### 4.2 纹理解析链变更

当前解析优先级（v1.0 / 旧系统）：
```
AtlasBinding.AtlasTexture > SourceTexture > Renderer fallback
```

v2.0 新链：
```
RuntimeAtlas(SourceTexture) > SourceTexture(fallback，仅 Atlas 分配失败时)
```

AtlasBinding 在运行时不再参与解析链，仅在 Editor 预览中使用。

### 4.3 与 ADR-015/017 的关系

（与 v1.0 一致。ADR-015 的扩展点：Atlas 溢出时受控建桶。）

---

## 五、迁移计划 — 6 条渲染路径统一

### 5.1 迁移总览

| # | 渲染器 | 当前状态 | 迁移目标 | 迁移难度 | 关键变更 |
|---|--------|---------|---------|---------|---------|
| 1 | BulletRenderer | 已用 RBM | 改用 RuntimeAtlas 纹理 | 🟢 低 | GetResolvedTexture → RuntimeAtlas |
| 2 | LaserRenderer | 已用 RBM | 统一到全局 RBM（纹理保持独立） | 🟡 中 | 不入 Atlas，独立贴图注册统一 RBM（v2.2 修正） |
| 3 | LaserWarningRenderer | 已用 RBM | 统一到全局 RBM（纹理保持独立） | 🟢 低 | 同 LaserRenderer（v2.2） |
| 4 | **DamageNumberSystem** | **自管 Mesh** | **迁移到 RBM** | 🟡 中 | 拆出渲染逻辑，改用 RBM.WriteQuad |
| 5 | **TrailPool** | **自管 Mesh** | **迁移到 RBM** | 🟡 中 | TriangleStrip → Quad 化，或保持独立但接入统计 |
| 6 | VFXBatchRenderer | 独立 RBM 实例 | 改用统一 RBM + RuntimeAtlas | 🟢 低 | 同 BulletRenderer |

### 5.2 BulletRenderer 迁移（难度：🟢 低）

**当前逻辑**：
```csharp
// Initialize: 遍历 BulletTypeSO → bt.GetResolvedTexture() → 收集 BucketKey
// Rebuild:    bt.GetResolvedTexture() → _batchManager.TryGetBucket(key) → WriteQuad
```

**迁移后**：
```csharp
// Initialize: 遍历 BulletTypeSO → runtimeAtlas.Allocate(channel, bt.SourceTexture)
//             → 收集 (Layer, AtlasRT) BucketKey
// Rebuild:    runtimeAtlas.GetAllocation(bt.SourceTexture) → 用 allocation.UVRect 替换 baseUV
//             → _batchManager.TryGetBucket((Layer, AtlasRT)) → WriteQuad
```

**关键变更点**：
1. `GetResolvedTexture()` 不再调用 — 纹理统一由 RuntimeAtlas 管理
2. `GetResolvedBaseUV()` 不再调用 — baseUV 由 `AtlasAllocation.UVRect` 提供
3. BucketKey 的 Texture 从 `Texture2D` 变为 `RenderTexture`（Atlas Page）
4. fallback 逻辑简化：RuntimeAtlas 分配失败 → 用原始 SourceTexture 作为 BucketKey

### 5.3 LaserRenderer / LaserWarningRenderer 迁移（难度：🟡 中 — v2.2 修正）

**当前逻辑**：
```csharp
// Initialize: 遍历 LaserTypeSO → lt.LaserTexture → 收集 BucketKey
// Rebuild:    type.LaserTexture → TryGetBucket → WriteSegmentQuad
```

**迁移后（v2.2 修正——激光不入 Atlas，保持独立贴图）**：
```csharp
// Initialize: 遍历 LaserTypeSO → 不走 RuntimeAtlas.Allocate
//             直接以 (Normal, lt.LaserTexture) 注册桶到统一 RBM
// Rebuild:    type.LaserTexture → TryGetBucket → WriteSegmentQuad（UV 逻辑不变）
```

**不入 Atlas 的技术理由（v2.2 新增）**：
1. 激光 `UV.y` 是 world-space 累积长度（`uvYAccum`），不归一化到 0→1，依赖 Shader 端 `repeat/wrap` 采样模式实现纹理滚动
2. Blit 到 Atlas 子区域后，`frac(uvYAccum)` 的 0→1 范围对应整张 Atlas 而非子区域——纹理滚动效果彻底错乱
3. 强行支持需要 Shader variant + 额外 atlas_uvRect uniform，破坏"统一材质"目标
4. 激光贴图种类极少（通常 1-3 种），独立贴图仅增加 1-3 个 DC，合批收益微乎其微

**迁移关键变更**：
1. LaserRenderer 改为使用统一 RBM（而非自建 RBM 实例），但桶的 Texture 仍为独立 `Texture2D`
2. `WriteSegmentQuad` 的 UV 逻辑完全不变
3. 激光桶与子弹/VFX 的 Atlas RT 桶共存于同一 RBM，由 `UploadAndDrawAll` 统一提交

> ~~⚠️ 未决项 UD-10~~：~~修改 WriteSegmentQuad~~。**v2.2 修正**：激光不入 Atlas，UV 映射无需修改。UD-10 不再适用。

### 5.4 DamageNumberSystem 迁移（难度：🟡 中）

**当前逻辑**：
```csharp
// 自管 Mesh + Material
// _numberAtlas 绑定到独立 Material
// WriteNumber() 直接写 _vertices 数组
// UpdateAndRender() 直接调 Graphics.DrawMesh
```

**迁移策略**：DamageNumberSystem 的 NumberAtlas Blit 到 RuntimeAtlas 的 DamageText Channel。

```csharp
// 迁移后：
// Initialize: runtimeAtlas.Allocate(DamageText, numberAtlas) → 获得 UVRect
//             但 NumberAtlas 是 10 个数字水平排列，需要在 UVRect 内再切分
// Rebuild:    通过 RBM.TryGetBucket → WriteQuad
```

**关键挑战**：
1. DamageNumberSystem 当前的 `DIGIT_UV_WIDTH = 0.1f` 假设贴图宽度的 10% = 一个数字。Blit 到 Atlas 后，这个比例要相对于 `AtlasAllocation.UVRect` 重新计算
2. DamageNumberSystem 当前持有自己的 `_mesh`、`_material`、`_indices`，迁移后这些由 RBM 管理
3. 渲染排序：当前由独立 `Graphics.DrawMesh` 的 sortingOrder 控制，迁移后需要在 RBM 中注册 `RenderSortingOrder.DamageNumber` 对应的桶

### 5.5 TrailPool 迁移（难度：🟡 中，方案待定）

**当前逻辑**：
```csharp
// TriangleStrip 展开为 TriangleList
// 使用 whiteTexture，纯靠 Vertex Color 着色
// 每条拖尾 N 个点 → 2N 顶点 → (N-1)*6 索引
```

**迁移方案 A：保持独立但接入统计**
- TrailPool 保持自管 Mesh（因为它不是 Quad 化的，是 TriangleStrip）
- 但接入 `RenderBatchManagerRuntimeStats`，贡献 DC 统计
- 最小侵入性

**迁移方案 B：Quad 化迁移到 RBM**
- 将拖尾改为 Quad 链（每段一个 Quad）
- 好处：完全统一
- 坏处：视觉质量可能下降（Quad 拐角处不连续），且 Quad 化后顶点数增加

> **⚠️ 未决项 UD-11**：TrailPool 迁移方案 A（保持独立+接入统计）还是方案 B（Quad 化）？推荐 A，因为拖尾的 TriangleStrip 拓扑与 RBM 的 Quad 拓扑不匹配。

### 5.6 VFXBatchRenderer 迁移（难度：🟢 低）

与 BulletRenderer 完全对称的迁移方式。

### 5.7 统一渲染调度（新增）

迁移完成后，`DanmakuSystem.RunLateUpdatePipeline()` 中的渲染调用变为：

```csharp
private void RunLateUpdatePipeline()
{
    float dt = Time.deltaTime * (_timeScale != null ? _timeScale.TimeScale : 1f);

    RenderBatchManagerRuntimeStats.BeginFrame();

    // 所有通过 RBM 的渲染器
    _bulletRenderer.Rebuild(...);           // → 统一 RBM
    _laserRenderer.Rebuild(...);            // → 统一 RBM
    _laserWarningRenderer.Rebuild(...);     // → 统一 RBM
    _damageNumbers.Rebuild(dt);             // → 统一 RBM（迁移后）

    // 统一提交（v2.2+v2.3：桶已在 Initialize 时按 SortingOrder 排序，UploadAndDrawAll 顺序遍历即可）
    _renderBatchManager.UploadAndDrawAll(); // 一次提交全部，桶已按 SortingOrder 排好序

    // TrailPool 独立提交（方案 A）
    _trailPool.Render();                    // 独立 Mesh，但接入统计
    RenderBatchManagerRuntimeStats.AccumulateBatch(trailDC, trailBatches, 0);

    RenderBatchManagerRuntimeStats.EndFrame();
}
```

> **⚠️ 未决项 UD-12**：是否所有 Renderer 共用一个 RBM 实例（全局统一），还是按系统域分多个 RBM 实例但共享 RuntimeAtlas？
>
> **推荐**：全局单 RBM 实例。理由：1 个 RBM = 所有 DC 在一次 UploadAndDrawAll 中提交，减少 `Graphics.DrawMesh` 调用次数。如果多个 RBM，每个 RBM 都要独立 UploadAndDrawAll。
>
> **风险**：单 RBM 的 BucketKey 空间变大（Bullet + Laser + VFX + DmgText 的所有 (Normal, Texture) 组合），但实际桶数仍然可控（每 Channel 1~2 张 Atlas Pages + Laser 独立贴图 1-3 个 = 10-15 桶）。
>
> **v2.2 新增 + v2.3 修正**：统一到单 RBM 后，桶的渲染顺序由 `SortingOrder` 决定。**排序时机：Initialize 阶段**——`_buckets` 数组在初始化末尾按 SortingOrder 升序排列并重建 `_bucketIndex` 映射。运行时 `UploadAndDrawAll()` 顺序遍历即可，零排序开销。（v2.3 UA-007：采纳方案 B — 注册时排序）

---

## 六、边界条件与风险

### 6.1 边界条件

| # | 场景 | 处理策略 |
|---|------|----------|
| BC-01 | 单张纹理超过 Atlas 尺寸 | 拒绝分配，返回 `AtlasAllocation.Invalid`，回退到独立贴图 |
| BC-02 | Channel 达到 MaxPages 上限 | 拒绝分配，返回 Invalid，LogWarning，回退到独立贴图 |
| BC-03 | 预热阶段 Atlas 已满 | 自动创建新 Page（仍在初始化期） |
| BC-04 | 战斗中首次出现新贴图种类 | Allocate + Blit 一次性开销 ~0.1ms |
| BC-05 | 一帧内大量新贴图涌入 | 可选分帧加载策略（预热机制可缓解） |
| BC-06 | 源纹理 Read/Write = false | `CommandBuffer.Blit()` 不要求 CPU 可读，无问题 |
| BC-07 | 源纹理使用压缩格式 | GPU Blit 处理格式转换，输出到 ARGB32 RT |
| BC-08 | RT Lost（WebGL Tab 切换） | 标记 dirty，重新 Blit 所有缓存条目。源纹理引用已保持（UD-04），不存在源纹理丢失风险。当缓存条目 >50 时启用分帧重建（每帧最多 20 张，P1 优化项）(v2.2 补充) |
| BC-09 | 序列帧 UV 计算 | AtlasAllocation.UVRect 替换 baseUV，GetFrameUV 在 UVRect 内再分帧 |
| BC-10 | 多个 TypeSO 引用同一 SourceTexture | 缓存去重：按 InstanceID 只 Blit 一次 |
| **BC-11** | ~~激光 UV 从全贴图变为子区域~~ | ~~LaserRenderer 的 UV 计算需要适配 Atlas 子区域映射~~ **(v2.2 取消：激光不入 Atlas，UV 不变)** |
| **BC-12** | **DamageNumber 数字切分从绝对 UV 变为相对 UV** | 数字宽度从 `0.1 * 全贴图` 变为 `0.1 * allocation.UVRect.width` |

### 6.2 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| WebGL RT Lost | 中 | 高 | dirty flag + 重建机制 |
| Blit Shader 兼容性 | 低 | 高 | fallback 降级 |
| 内存超预算 | 低 | 中 | MaxPages 硬上限 + Debug HUD |
| 首帧 Blit 卡顿 | 中 | 中 | 预热机制 |
| BucketKey 类型变更引发回归 | 低 | 中 | 编译期可发现 |
| **DamageNumber 迁移后视觉差异** | 中 | 中 | 迁移后逐帧对比验证 |
| ~~LaserRenderer UV 映射错误~~ | ~~中~~ | ~~高~~ | **(v2.2 取消：激光不入 Atlas，无 UV 映射变更风险)** |
| **迁移期间两套系统并存的混乱** | 低 | 中 | 分 Phase 迁移，每 Phase 保持可运行 |

### 6.3 未决项清单（UD - Undecided） — 全部已确认 ✅

> **2026-04-18 20:47 天命人逐一确认完毕，全部 12 项已定。**

| ID | 问题 | **最终决策** | 决策影响 |
|----|------|-------------|----------|
| ~~UD-01~~ | BucketKey.Texture 拓宽 | ✅ **方案 A：直接从 `Texture2D` 拓宽为 `Texture`** | RBM + 所有 Renderer |
| ~~UD-02~~ | Atlas 溢出时运行时创建新桶 | ✅ **选项 2：受控建桶 + MaxPages 硬上限** | ADR-015 + RBM |
| ~~UD-03~~ | WebGL RT Lost 重建策略 | ✅ **全量重 Blit（dirty flag → 遍历缓存全部重新 Blit）** | 缓存数据结构 |
| ~~UD-04~~ | 源纹理 Blit 后是否可卸载 | ✅ **保持引用（不卸载）**。省 <2MB 内存但加载逻辑复杂度翻倍，不值得 | 内存策略 |
| ~~UD-05~~ | 是否支持热更新纹理 | ✅ **第一版不支持，预留接口**。后续如需重构再加 | API 设计 |
| ~~UD-06~~ | 是否引入 RuntimeAtlasConfigSO | ✅ **引入**。参数未定需要反复调优，ConfigSO 省掉改常量→编译→测试循环 | 配置方式 |
| ~~UD-07~~ | 溢出时行为 | ✅ **回退到独立贴图**。宁可多 DC 也不丢画面 | Renderer 逻辑 |
| ~~UD-08~~ | ADR-007/008/010 Superseded | ✅ **已确认**：标记 Superseded by ADR-028（运行时），Editor 环境仍生效 | ADR 体系 |
| ~~UD-09~~ | 运行时忽略 AtlasBinding | ✅ **是**。统一管线不走两条路径 | 纹理解析链 |
| ~~UD-10~~ | 激光 UV 映射 | ✅ ~~修改 WriteSegmentQuad~~ → **(v2.2 废弃：激光不入 Atlas，UV 不变，UD-10 不再适用)** | LaserRenderer |
| ~~UD-11~~ | TrailPool 迁移方案 | ✅ **方案 A：保持独立 Mesh + 接入 RenderBatchManagerRuntimeStats 统计** | TrailPool |
| ~~UD-12~~ | 全局单 RBM vs 多 RBM | ✅ **全局单 RBM**。一次 UploadAndDrawAll 提交全部 DC | 渲染调度 |

---

## 七、API 设计草案

### 7.1 RuntimeAtlasManager（核心入口）

**(v2.3 内联补全——原 v1.0 §6.1 内容)**

```csharp
public class RuntimeAtlasManager : System.IDisposable
{
    /// <summary>初始化指定 Channel（创建首页 Atlas RT）</summary>
    public void InitChannel(AtlasChannel channel, AtlasChannelConfig config);

    /// <summary>
    /// 分配源纹理到指定 Channel 的 Atlas。
    /// 缓存命中：O(1)，零 GC。
    /// 未命中：执行 Blit，返回新分配。
    /// 分配失败（Atlas 满 + 超 MaxPages）：返回 AtlasAllocation.Invalid。
    /// </summary>
    public AtlasAllocation Allocate(AtlasChannel channel, Texture2D source);

    /// <summary>获取指定 Channel 指定 Page 的 Atlas RenderTexture</summary>
    public RenderTexture GetAtlasTexture(AtlasChannel channel, int pageIndex);

    /// <summary>批量预热——关卡加载时调用</summary>
    public void WarmUp(AtlasChannel channel, IReadOnlyList<Texture2D> sources);

    /// <summary>切关清空——释放所有 RT + 清除缓存</summary>
    public void Reset();

    /// <summary>RT Lost 恢复——标记 dirty 后全量/分帧重 Blit</summary>
    public void HandleRTLost();

    /// <summary>获取统计信息</summary>
    public RuntimeAtlasStats GetStats();

    public void Dispose();
}
```

### 7.2 RuntimeAtlasConfig（配置 SO）

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Rendering/Runtime Atlas Config")]
public class RuntimeAtlasConfig : ScriptableObject
{
    [Header("Bullet Channel")]
    public AtlasChannelConfig Bullet = AtlasChannelConfig.Default;

    [Header("VFX Channel")]
    public AtlasChannelConfig VFX = AtlasChannelConfig.Default;

    [Header("DamageText Channel")]
    public AtlasChannelConfig DamageText = AtlasChannelConfig.Small;

    [Header("Laser Channel")]
    public AtlasChannelConfig Laser = AtlasChannelConfig.Small;

    [Header("Trail Channel (Reserved)")]
    public AtlasChannelConfig Trail = AtlasChannelConfig.Small;

    [Header("Character Channel (Reserved)")]
    public AtlasChannelConfig Character = AtlasChannelConfig.Default;
}
```

### 7.3 RuntimeAtlasStats

**(v2.3 内联补全——原 v1.0 §6.3 内容)**

```csharp
public readonly struct RuntimeAtlasStats
{
    /// <summary>各 Channel 的页面数</summary>
    public readonly int[] PageCountPerChannel;

    /// <summary>各 Channel 已分配的纹理数</summary>
    public readonly int[] AllocationCountPerChannel;

    /// <summary>各 Channel 的填充率（已用像素 / 总像素）</summary>
    public readonly float[] FillRatePerChannel;

    /// <summary>总 RT 内存占用（字节）</summary>
    public readonly long TotalMemoryBytes;

    /// <summary>总分配次数（含缓存命中）</summary>
    public readonly int TotalAllocations;

    /// <summary>缓存命中次数</summary>
    public readonly int CacheHits;

    /// <summary>Blit 次数（未命中时执行的实际 Blit）</summary>
    public readonly int BlitCount;
}
```

---

## 八、文件结构规划
