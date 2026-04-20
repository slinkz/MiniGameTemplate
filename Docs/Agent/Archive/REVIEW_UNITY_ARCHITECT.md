# Unity 架构师视角评审：落地可行性 & 决策点

> 评审日期：2026-04-11 | 评审角色：Unity 架构师
> 架构输入：`REVIEW_SOFTWARE_ARCHITECT.md`（9 项架构缺陷，SA-001 ~ SA-009）
> 需求输入：`REVIEW_GAME_DESIGNER.md`（20 项需求盲区，GD-001 ~ GD-020）
> 目标平台：微信小游戏（Unity WebGL，GLES 3.0 / WebGL 2.0，单线程，无 Compute Shader）

---

## 一、评审总结

架构师提出的重构方案整体可行，但有 **5 个需要用户决策的关键问题** 和 **8 个技术落地风险**。本文逐项评审，给出 Unity 侧的具体落地方案和约束。

---

## 二、决策清单（需要用户拍板）

### 🎯 DEC-001: 多贴图方案选型

**背景**：SA-001 提出三种方案，架构师推荐方案 A（构建时合 Atlas）。

**Unity 落地分析**：

| 方案 | Unity 实现 | WebGL 兼容 | 风险 | 备注 |
|------|-----------|------------|------|------|
| **A. Editor 合 Atlas** | `AssetPostprocessor` + 自定义 `TextureAtlasPacker`，构建前将所有弹丸子图合并为一张 Runtime Atlas（2048×2048），自动回写 SO 的 UV | ✅ | 子图总面积超 2048² 时需要拆成多张→退化为方案 B | **推荐首选** |
| **B. 按贴图分桶** | 渲染器维护 `List<(Texture, Mesh, Material)>`，每帧按贴图分组提交 | ✅ | DrawCall 线性增长，N 张贴图 × 2 层 = 2N DC | **推荐作为 A 的后备** |
| **C. Texture2DArray** | `Texture2DArray` + Shader `UNITY_SAMPLE_TEX2DARRAY` | ⚠️ WebGL 2.0 支持但有限制 | 所有子图必须相同分辨率和格式；微信小游戏 iOS 端可能有兼容问题 | **不推荐** |

**决策选项**：
- [ ] **选 A**：Editor 合 Atlas（推荐，最简洁，零额外 DC，需要写 ~200 行 Editor 工具）
- [ ] **选 B**：按贴图分桶（更灵活，但 DC 更多，需改渲染器核心逻辑）
- [ ] **选 A+B 混合**：默认走合 Atlas，超过 2048² 自动拆分为多桶

**我的建议**：选 **A+B 混合**。绝大多数情况 A 够用，极端情况自动降级到 B。

---

### 🎯 DEC-002: 共享渲染层的程序集拓扑

**背景**：SA-002 提出新建 `SharedRendering` 模块，将 `DanmakuVertex` 提取出来。

**程序集依赖拓扑选项**：

```
选项 A：新建 SharedRendering.asmdef
  MiniGameFramework.Runtime
    ├── SharedRendering（L0.5，零依赖）
    ├── DanmakuSystem → SharedRendering
    └── VFXSystem → SharedRendering

选项 B：将共享类型直接放入 MiniGameFramework.Runtime
  MiniGameFramework.Runtime
    ├── Rendering/（RenderVertex, RenderBatchManager）
    ├── DanmakuSystem/（引用 Rendering/）
    └── VFXSystem/（引用 Rendering/）
```

**决策选项**：
- [ ] **选 A**：独立 asmdef（更干净，但多一个程序集 = 编译多一步）
- [ ] **选 B**：放在框架 Runtime 程序集内（更简单，适合模板项目体量）

**我的建议**：选 **B**。模板项目不大，独立 asmdef 增加维护成本收益不大。在 `_Framework/Rendering/` 下新建目录即可。

---

### 🎯 DEC-003: 渲染调度方式

**背景**：SA-003 提出统一 `RenderBatchManager`。

**Unity 落地选项**：

| 方案 | 实现 | Z 排序 | WebGL | 性能 |
|------|------|--------|-------|------|
| **`Graphics.DrawMesh` + sortingOrder** | 每批一次 DrawMesh 调用 | ✅ 通过 MaterialPropertyBlock 或 sortingLayer | ✅ | 好 |
| **`CommandBuffer` + Camera.AddCommandBuffer** | 录制渲染指令 → 统一提交 | ✅ 完全控制顺序 | ⚠️ WebGL 支持但有限制（不支持 AsyncGPUReadback 等） | 好 |
| **多 Camera 分层** | 弹幕一个相机 + VFX 一个相机 | ✅ 通过 Camera depth | ✅ 但增加 Camera 开销 | 差 |

**决策选项**：
- [ ] **Graphics.DrawMesh**（最简单，推荐）
- [ ] **CommandBuffer**（更可控，但调试更难）

**我的建议**：继续用 **`Graphics.DrawMesh`**。当前方案已经在用，改动最小。通过控制调用顺序实现 Z 排序（先画背景层→弹丸→激光→特效→前景层）。

---

### 🎯 DEC-004: 碰撞事件 Buffer 实现

**背景**：SA-005 提出 `CollisionEventBuffer`。

**Unity 侧考量**：

| 方案 | 实现 | GC | WebGL 兼容 |
|------|------|----|------------|
| **NativeArray<CollisionEvent>** | Unity Collections 包的原生内存 | 零 GC | ⚠️ 需要 Collections 包；微信小游戏下 NativeArray 可用但需注意 Dispose |
| **预分配 CollisionEvent[]** | 固定大小 C# 数组 + 写指针 | 零 GC（预分配） | ✅ |
| **List<CollisionEvent>** | 托管列表 | 有 GC | ✅ |

**决策选项**：
- [ ] **预分配数组**（推荐，零依赖，零 GC，与现有风格一致）
- [ ] **NativeArray**（需要 Collections 包依赖）

**我的建议**：用**预分配数组**。与现有 BulletWorld 的空闲栈模式一致，不引入新包依赖。容量与 `MaxBullets` 相同即可（最坏情况每颗子弹都碰撞）。

---

### 🎯 DEC-005: 弹丸视觉动画的数据存储

**背景**：GD-002 要求弹丸支持缩放/Alpha/颜色随生命周期变化。

**问题**：当前 `BulletCore` 是 36 字节的紧凑热数据。增加动画曲线采样会增加每帧计算量和数据大小。

| 方案 | 存储 | 每帧开销 | 数据膨胀 |
|------|------|----------|----------|
| **A. 曲线在 SO 上，运行时采样** | `BulletTypeSO` 新增 `AnimationCurve ScaleOverLifetime` 等 | 每活跃弹丸 3 次 `Evaluate()` | BulletCore +0 字节 |
| **B. 烘焙曲线为查找表** | 初始化时将 AnimationCurve 采样为 `float[32]` LUT | 每活跃弹丸 3 次数组查找 | BulletTypeSO +384 字节/类型 |
| **C. 在 BulletCore 中存储当前动画值** | 每帧写入 scale/alpha/color 到 Core | 零查找（渲染时直接读） | BulletCore +12 字节 (→ 48B) |

**决策选项**：
- [ ] **方案 A**：最简单，但 `AnimationCurve.Evaluate()` 在 WebGL 上有微量 GC 风险
- [ ] **方案 B**：性能最好，LUT 查找零 GC（推荐）
- [ ] **方案 C**：渲染最快但增大热数据，影响 cache 效率

**我的建议**：选 **B**。在 `DanmakuTypeRegistry.Awake()` 时预烘焙所有曲线为 LUT。运行时查找用 `int index = (int)(lifetimePercent * 31)` 即可。

---

## 三、技术落地风险清单

### ⚠️ RISK-001: Atlas 打包工具的 UV 精度

**描述**：自动打 Atlas 后，UV 需要精确到子像素。如果 Atlas 使用了压缩纹理格式（如 ASTC/ETC2），压缩 artifact 可能导致弹丸边缘出现颜色渗透。

**缓解措施**：
1. Atlas 纹理格式使用无损压缩（RGBA32 或 ETC2 4bit 对 Alpha 质量已足够）
2. 每个子图周围留 1-2 像素 padding（防止双线性采样渗透）
3. AssetImportEnforcer 强制检查弹丸图集格式

---

### ⚠️ RISK-002: DrawCall 预算

**描述**：方案 A+B 混合方案下，极端情况每种贴图 × 2 层 = 2N DrawCall。加上激光、喷雾、飘字、VFX，总 DC 可能达到 10+。

**WebGL 端 DrawCall 预算参考**：
- 低端机（iPhone SE 2/红米等）：< 50 DC 保安全
- 中端机：< 100 DC
- 当前弹幕系统：4 DC（弹丸 Normal/Additive + 激光 + 飘字）

**缓解措施**：增加 DC 计数器到 DebugHUD，设定预警阈值。

---

### ⚠️ RISK-003: 序列化迁移

**描述**：重构过程中 `BulletTypeSO` / `VFXTypeSO` 字段会变化。已存在的 SO 资产实例需要迁移。

**缓解措施**：
1. 新增字段使用合理默认值 + `[FormerlySerializedAs]`
2. 编写 `SOmigrationTool` Editor 脚本批量更新
3. **踩坑提醒**：之前已记录——脚本默认值变更或新增 `[SerializeField]` 后，必须检查已存在的 SO 实例是否正确写入新值

---

### ⚠️ RISK-004: WebGL Shader 兼容性

**描述**：新增 Shader 效果（溶解/发光/描边）需要确保 GLES 3.0 / WebGL 2.0 兼容。

**已知限制**：
- 无 `Compute Shader`
- 无 `Geometry Shader`
- `#pragma target 3.0` 最高
- `discard` 可用但有性能代价（打断 early-Z）
- `tex2D` / `texture2D` 均可用
- `SV_Position` / `VPOS` 可用

**缓解措施**：所有新 Shader 必须在编辑器中用 WebGL 2.0 Graphics API 模式验证编译。

---

### ⚠️ RISK-005: RenderBatchManager 内存分配

**描述**：如果按贴图分桶，运行时首次遇到新贴图时需要创建新的 Mesh + Material 实例。

**缓解措施**：
1. 初始化时根据 `DanmakuTypeRegistry` 中注册的所有 SO 预创建桶（不运行时动态创建）
2. 使用对象池管理桶

---

### ⚠️ RISK-006: AnimationCurve LUT 精度

**描述**：32 点 LUT 对于陡峭曲线可能出现阶梯感。

**缓解措施**：可选 64 点 LUT（+256 字节/类型/曲线）或线性插值两相邻 LUT 值。

---

### ⚠️ RISK-007: CollisionEvent Buffer 溢出

**描述**：极端弹幕密度下一帧可能产生数千次碰撞。

**缓解措施**：Buffer 固定容量（如 512），溢出时丢弃最低优先级碰撞（屏幕边缘碰撞 < 障碍物碰撞 < 目标碰撞）。

---

### ⚠️ RISK-008: 喷雾渲染方案选型

**描述**：SA-003 提到喷雾需要渲染器，但喷雾形状（锥形 AOE）与弹丸（点 Quad）、激光（线 Strip）都不同。

**方案对比**：

| 方案 | 视觉 | 实现复杂度 | DC |
|------|------|-----------|-----|
| **扇形 Mesh** | 程序化扇形网格 + 颜色/透明度渐变 | 中 | +1~2 |
| **全屏后处理** | 在喷雾区域叠加颜色（类似 URP 的 Screen Space 效果）| 高 | WebGL 兼容性差 |
| **粒子模拟** | 在扇形区域内生成大量小 Quad 粒子 | 中 | 复用弹丸渲染器 |
| **VFX Sprite Sheet** | 预制喷雾帧动画 | 低 | 复用 VFX 渲染器 |

**决策选项**：
- [ ] **扇形 Mesh**（最精确，适合逻辑+视觉一致性）
- [ ] **VFX Sprite Sheet**（最简单，但不精确）
- [ ] **粒子模拟**（视觉最丰富，但 CPU 成本高）

---

## 四、重构执行计划建议（Unity 视角）

### Phase 0: 基础设施（1-2 天）

| 任务 | 描述 | 文件影响 |
|------|------|----------|
| 0.1 | 新建 `_Framework/Rendering/` 目录 | 新建 |
| 0.2 | 提取 `RenderVertex.cs`（从 DanmakuVertex 改名迁移） | 改 DanmakuVertex.cs → Rendering/RenderVertex.cs |
| 0.3 | 修改 BulletRenderer/LaserRenderer/VFXBatchRenderer 的 using | 改 3 文件 |
| 0.4 | 验证编译通过 + 现有 Demo 不受影响 | — |

### Phase 1: 渲染管线重构（3-5 天）

| 任务 | 描述 | 文件影响 |
|------|------|----------|
| 1.1 | 实现 `RenderBatchManager`（按 Layer+Texture 分桶） | 新建 |
| 1.2 | 重构 `BulletRenderer` 使用 BatchManager | 大改 |
| 1.3 | 重构 `VFXBatchRenderer` 使用 BatchManager | 大改 |
| 1.4 | `BulletTypeSO` 新增 `Texture2D BulletTexture` 字段 | 小改 |
| 1.5 | `VFXTypeSO` 新增 `Texture2D VFXTexture` 字段 | 小改 |
| 1.6 | 实现 Atlas 打包 Editor 工具（可选，不影响运行时） | 新建 |
| 1.7 | 实现 `SprayRenderer`（扇形 Mesh） | 新建 |

### Phase 2: 事件与扩展性（2-3 天）

| 任务 | 描述 | 文件影响 |
|------|------|----------|
| 2.1 | 实现 `CollisionEventBuffer` | 新建 |
| 2.2 | 改造 `CollisionSolver` 写入事件 Buffer | 大改 |
| 2.3 | 实现运动策略表 `MotionRegistry` | 新建 |
| 2.4 | 改造 `BulletMover` 使用策略表 | 大改 |
| 2.5 | 拆分 `DanmakuSystem` 入口 | 大改 |
| 2.6 | 容量收拢到配置 SO | 改 8-10 文件 |

### Phase 3: 视觉增强（2-3 天）

| 任务 | 描述 | 文件影响 |
|------|------|----------|
| 3.1 | 弹丸视觉曲线（LUT 烘焙 + 渲染器读取） | 改 BulletTypeSO + BulletRenderer |
| 3.2 | Shader 增强（溶解 + 发光参数） | 改 3 Shader |
| 3.3 | 预警线渲染器 | 新建 |
| 3.4 | 时间缩放联动 VFX | 改 SpriteSheetVFXSystem |

---

## 五、决策汇总表

| ID | 决策 | 用户选择 | 状态 | 影响 |
|----|------|----------|------|------|
| DEC-001 | 多贴图方案 | **B. 按贴图分桶** | ✅ 已确认 | DC 增长换取灵活性，无需 Atlas 构建步骤 |
| DEC-002 | 共享层程序集 | **B. 框架内目录** | ✅ 已确认 | `_Framework/Rendering/` |
| DEC-003 | 渲染调度方式 | **Graphics.DrawMesh + sortingOrder** | ✅ 已确认 | 最简洁 |
| DEC-004 | 碰撞事件实现 | **预分配 CollisionEvent[]** | ✅ 已确认 | 零 GC，零依赖 |
| DEC-005 | 视觉动画数据 | **C. Core 存储当前值** | ✅ 已确认 | BulletCore 36B → 48B |
| DEC-006 | 喷雾渲染方案 | **VFX Sprite Sheet** | ✅ 已确认 | 复用 VFX 渲染器，不写 SprayRenderer |

> 所有决策于 2026-04-11 由用户确认。

---

## 六、不需要用户决策的确认事项

以下我已经确认可行，直接推进：

1. ✅ `DanmakuVertex` 改名为 `RenderVertex`，移到 `_Framework/Rendering/`
2. ✅ 所有硬编码容量收拢到 `DanmakuWorldConfig` + `VFXRenderConfig`
3. ✅ `CollisionSolver` 保留现有碰撞响应逻辑，额外写入事件 Buffer
4. ✅ `BulletMover` 保留现有运动逻辑，新增运动类型走策略表扩展
5. ✅ 保持 `Graphics.DrawMesh` 方式，不引入 CommandBuffer
6. ✅ 新 Shader 必须通过 WebGL 2.0 编译验证
7. ✅ SO 迁移使用 `[FormerlySerializedAs]` + Editor 批量工具
