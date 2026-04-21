# PK 评审记录 — 分阶段实施方案（三项 RuntimeAtlas 深化任务）

> **目标文档**：`docs/Agent/PHASED_IMPLEMENTATION_PLAN.md`
> **文档类型**：TDD
> **攻方角色**：Unity 架构师（10 年+ Unity 引擎开发经验，专精渲染管线、WebGL 平台限制、内存优化）
> **守方角色**：软件架构师（专精系统设计、API 设计、可维护性和关注点分离）
> **开始时间**：2026-04-21 09:17
> **最大轮次**：3
> **PK 状态**：✅ 已完成（Round 1 全面收敛，无需 Round 2/3）
> **结束时间**：2026-04-21 09:43

---

## PK Round 1 — 攻方提问（Unity 架构师）

### PI-001 | 严重度 🔴高 | Laser/Trail 各自独立 `new RuntimeAtlasManager()` 导致 RT 资源膨胀和 Channel 冗余初始化

**涉及章节**：§2.3 L2.2, §3.3 T3.1

**质疑**：
文档建议 LaserRenderer 和 TrailPool 各自独立 `new RuntimeAtlasManager()`。然而 `RuntimeAtlasManager.Initialize()` 的当前实现（L42-49）会遍历 `AtlasChannel` 枚举的全部 6 个值，为每一个都调用 `InitChannel()`：

```csharp
Array channels = Enum.GetValues(typeof(AtlasChannel));
for (int i = 0; i < channels.Length; i++)
{
    AtlasChannel channel = (AtlasChannel)channels.GetValue(i);
    InitChannel(channel, config.GetChannelConfig(channel));
}
```

这意味着：
1. 系统将存在 **3 个 RuntimeAtlasManager 实例**（BulletRenderer + LaserRenderer + TrailPool），每个都持有 6 个 Channel 状态。LaserRenderer 的实例只用 `AtlasChannel.Laser`，其余 5 个 Channel 完全浪费。
2. 一旦有代码误用了错误 Channel，会静默成功但产生新 RT，消费方永远不会使用它。
3. 3 个实例 × 6 Channel 的 `AllocationCache` Dict = 18 个 Dictionary 实例化，大部分永远为空。

**潜在风险**：
- 微信小游戏 WebGL 环境下，多余 RT 可能突破纹理资源限制。
- 与文档 §1.1 声称的"节省 RT 内存"目标矛盾。

**建议方向**：
1. 走共享实例架构（DanmakuSystem 持有唯一 RuntimeAtlasManager，注入各 Renderer）。
2. 或提供"单 Channel 初始化"模式（`Initialize(config, AtlasChannel[])`），避免创建未使用 Channel 状态。

**状态**：✅ 已收敛 — 守方接受，改为共享实例注入。文档 §L2.2/§T3.1/§R-05 已全面更新。

---

### PI-002 | 严重度 🔴高 | Laser UV.y 当前为世界空间累积长度值，非 [0,1] 归一化——UV Remap 公式语义变更未说明

**涉及章节**：§2.3 L2.4

**质疑**：
文档 §2.3 L2.4 给出的 UV Remap 公式用 `uvYAccum / totalLength` 做归一化。但现有 `LaserRenderer.cs` L148-183 中 `uvYAccum` 是世界空间长度原始累积值（可远大于 1.0），直接写入顶点 UV：

```csharp
float uvYEnd = uvYAccum + seg.Length;  // seg.Length 是世界单位
UV = new Vector2(0f, uvYAccum),  // UV.y 可以远大于 1.0
```

当前不走 Atlas 时，Shader 通过 `wrapMode=Repeat` 处理 UV 环绕。但 Atlas RT 的 `wrapMode=Clamp`（`CreatePage()` L373），UV 超出子区域后被 clamp 到边缘像素。

文档的归一化公式将**原本按世界单位 1:1 映射纹理**改为**整条激光映射完整纹理一次**——短激光纹理被压缩，长激光纹理被拉伸，这是一个未声明的语义变更。

**潜在风险**：
- 视觉效果不一致且未在验收标准 LC-2 中覆盖。
- 文档声称"禁用滚动"，但实际改变的不只是滚动，还有纹理空间映射语义。

**建议方向**：
1. 明确定义 Atlas 模式下激光纹理的 UV 映射语义。
2. 验收标准增加"不同长度激光的纹理密度视觉验证"。
3. 如果选择"按世界单位平铺"，需要 Shader 配合子区域 `frac()` 环绕或放弃入 Atlas。

**状态**：✅ 已收敛 — 守方接受。新增 UV 映射语义说明块（§L2.4）、Atlas/非 Atlas 双路径代码、验收标准 LC-2a。

---

### PI-003 | 严重度 🟡中 | `GetStats()` 中 `Math.Max(1, Pages.Count)` 在懒建页后产生误导性 fillRate 分母

**涉及章节**：§1.4 Step 4

**质疑**：
文档声称 `GetStats()` 对空 Pages 的处理已正确，"无需修改"。但 L228：

```csharp
long totalPixels = (long)state.Config.AtlasSize * state.Config.AtlasSize * Math.Max(1, state.Pages.Count);
```

`Pages.Count=0` 时 `Math.Max(1,0)=1`，`totalPixels` = 一页像素量。虽然当前 `fillRate` 数值结果正确（0/非零=0），但后续如果有人基于 `totalPixels` 做"剩余空间"计算，会得到错误结果。

**潜在风险**：
- 不阻塞实施，但会给后续自动化决策代码埋坑。

**建议方向**：
在实施 R4.4A 时顺手修复：`Pages.Count == 0` 时 `totalPixels = 0`。

**状态**：✅ 已收敛 — 守方接受，Step 4 代码已修正（移除 `Math.Max(1, ...)`，Pages.Count=0 时 totalPixels=0）。

---

### PI-004 | 严重度 🟡中 | Trail RT Lost 恢复路径不完整——仅回退 Material 但未重新分配 Atlas

**涉及章节**：§3.3 T2.5

**质疑**：
文档 §3.3 T2.5 的 RT Lost 处理仅做了 `_material.mainTexture = Texture2D.whiteTexture`。但之后如果 `RestoreDirtyPages()` 重建了 Atlas RT，TrailPool 没有任何机制重新将 `_atlasRT` 指回新的 RT，所有 Trail 将永久退回纯色模式直到下次场景切换。

相比之下，BulletRenderer 通过每帧 `Rebuild → ResolveBullet → Allocate` 自然重新获取 Atlas RT，天然恢复。但 TrailPool 的 `Allocate()` 只在弹丸生成时调用一次，之后不会再触发 Atlas 查询。

**潜在风险**：
- RT Lost 后 Trail 纹理永远不恢复，必须重启关卡。

**建议方向**：
在 `Render()` 中检测到 RT Lost 后，不仅回退 Material，还需要在下一帧尝试重新 Allocate 或监听 `RestoreDirtyPages` 完成事件。

**状态**：✅ 已收敛 — 守方接受，T2.5 新增 RT Lost 检测 + 恢复尝试逻辑（通过 TryGetAllocation 探测 Atlas 是否重建）。

---

### PI-005 | 严重度 🟡中 | `ResolveLaser()` 方法签名混乱——同时传 `fallbackTexture` 和 `type`（含 `LaserTexture`）

**涉及章节**：§2.3 L1.2

**质疑**：
文档 §2.3 L1.2 给出的 `ResolveLaser()` 签名为：
```csharp
public static ResolvedTextureBinding ResolveLaser(
    RuntimeAtlasManager atlasManager,
    Texture2D fallbackTexture,  // ← 语义不清
    MiniGameTemplate.Danmaku.LaserTypeSO type)
```

但在 §2.3 L2.3 调用处传入的是 `type.LaserTexture` 作为 `fallbackTexture`：
```csharp
var binding = RuntimeAtlasBindingResolver.ResolveLaser(
    _runtimeAtlas, type.LaserTexture, type);
```

这意味着 `fallbackTexture` 和 `type.LaserTexture` 是同一个对象，参数冗余且语义混乱。对比现有 `ResolveBullet()` 方法，`fallbackTexture` 是全局 fallback Atlas（不同于 type 内的 SourceTexture），用途明确。

**潜在风险**：
- API 混乱增加后续维护负担。

**建议方向**：
去掉 `fallbackTexture` 参数，直接在方法内部读 `type.LaserTexture`。

**状态**：✅ 已收敛 — 守方接受，`ResolveLaser()` 签名简化为 2 参数（去掉 `fallbackTexture`）。

---

> **Round 1 小结**：2 🔴高 + 3 🟡中 + 0 🟢低 = 5 个问题

---

## Round 1 收敛判定

| ID | 严重度 | 攻方质疑核心 | 守方回应 | 判定 |
|---|---|---|---|---|
| PI-001 | 🔴高 | 独立实例导致冗余 Channel 初始化 | 改为共享实例注入，§L2.2/§T3.1/§R-05 全面更新 | ✅ 收敛 |
| PI-002 | 🔴高 | Laser UV 语义变更未说明 | 新增语义说明块 + 双路径代码 + LC-2a 验收 | ✅ 收敛 |
| PI-003 | 🟡中 | GetStats fillRate 分母 | Step 4 代码已修正 | ✅ 收敛 |
| PI-004 | 🟡中 | Trail RT Lost 恢复不完整 | T2.5 新增检测+恢复逻辑 | ✅ 收敛 |
| PI-005 | 🟡中 | ResolveLaser 签名冗余 | 签名简化为 2 参数 | ✅ 收敛 |

**收敛统计**：
- 🔴高 收敛：2/2 = 100%
- 🟡中 收敛：3/3 = 100%
- **总收敛率：5/5 = 100%**

**结论：Round 1 已全面收敛，无需进入 Round 2。**

---

## PK 评审总结报告

### 基本信息

| 项目 | 值 |
|---|---|
| 目标文档 | `PHASED_IMPLEMENTATION_PLAN.md` v1.0 → v1.1 |
| PK 轮次 | 1/3（第 1 轮即收敛） |
| 攻方 | Unity 架构师 |
| 守方 | 软件架构师 |
| 问题总数 | 5（2 🔴高 + 3 🟡中） |
| 收敛率 | 100%（5/5） |

### 关键架构变更

| 变更 | 问题来源 | 影响范围 |
|---|---|---|
| **独立实例 → 共享实例注入** | PI-001 | DanmakuSystem / BulletRenderer / LaserRenderer / LaserWarningRenderer / TrailPool |
| **Laser UV 映射语义显式声明** | PI-002 | LaserRenderer.WriteSegmentQuad / 验收标准 LC-2a |
| **GetStats 懒建页兼容修复** | PI-003 | RuntimeAtlasManager.GetStats |
| **Trail RT Lost 恢复路径补全** | PI-004 | TrailPool.Render |
| **ResolveLaser 签名简化** | PI-005 | RuntimeAtlasBindingResolver / LaserRenderer |

### 质量评估

**攻方表现**：★★★★☆
- 5 个问题全部基于代码实证（行号精确到 L42-49 / L148-183 / L228 / L373）
- 2 个 🔴高 问题均抓住了架构层面的关键缺陷（资源膨胀 + 语义变更）
- 问题分布均匀覆盖三个方案，无重复或低价值提问

**守方表现**：★★★★☆
- 全部接受并修正，没有"绕过"或"推迟"
- 共享实例方案清晰，注入路径完整
- UV 语义变更说明块写得很到位，设计师可以理解

**文档 v1.1 质量**：
- ✅ 所有架构决策点有明确结论和理由
- ✅ 变更追溯清晰（每处修正标注 PI-xxx 和版本号）
- ✅ 验收标准覆盖新增的语义变更
- ✅ 风险表同步更新
- **可以开始实施。**



