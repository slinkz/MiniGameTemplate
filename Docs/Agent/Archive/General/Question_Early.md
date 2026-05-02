# RuntimeAtlasSystem TDD v2.1 — 架构师 PK 评审记录

> 自动生成，用于记录 Unity 架构师质疑 & 软件架构师回应

---

## PK Round 1 — Unity 架构师质疑（共 4 个问题）

### UA-001 | 🔴高 | BucketKey 仍包含 RenderLayer 维度，与 TDD "BucketKey 降维为纯 Texture" 的声明自相矛盾
**涉及 TDD 章节**：§3.0、§3.5、§5.2~5.4、§5.7
**质疑**：TDD §3.0 声明 "BucketKey 降维为纯 Texture"，但 §3.5 初始化流程伪代码、§5.2~5.4 迁移伪代码全部使用 `(Layer, AtlasRT)` 二元组。§5.7 还提到 "×2 Layer = 10-20 桶"。现有代码 `RenderBatchManager.cs` 的 `BucketKey` 确实仍包含 `RenderLayer` 字段（尽管只有 `Normal` 一个值）。
**潜在风险**：1) 实施者不知道该实现哪个版本；2) 如果真去掉 RenderLayer，BucketKey 的 Hash/Equals 都要改；3) 如果保留 RenderLayer（值恒为 Normal），§3.0 声明就是误导。
**建议方向**：统一口径——要么承认 BucketKey 仍保留 RenderLayer（值恒 Normal），要么在 R2.1 任务中显式列出去除 RenderLayer 的改动。
**状态**：⚠️ 已回应（Round 1）— BucketKey 保留 (RenderLayer, Texture) 结构但 Layer 恒为 Normal，代码与逻辑等价纯 Texture。TDD §3.0 措辞已修正。

---

### UA-002 | 🔴高 | 激光 UV 映射迁移严重低估——UV.y 是累积长度值而非 0→1，与常规 Atlas 子区域映射不兼容
**涉及 TDD 章节**：§5.3、§6.1 BC-11、§9 Phase R2.3
**质疑**：LaserRenderer 的 `UV.y` 是 world-space 累积长度（`uvYAccum`），**不归一化到 0→1**，依赖 Shader 端 `frac()` / `repeat` 实现纹理滚动。Blit 到 Atlas 子区域后，`UV.y` 无法简单映射到 `uvRect.y→uvRect.yMax`——这需要 Shader 端在 `frac()` 后再映射到 Atlas 子区域 Y 范围，当前通用 Shader 不支持。
**潜在风险**：1) 激光纹理滚动效果完全失效；2) 需要专门 Shader variant，破坏"统一材质"目标；3) R2.3 工期估算不足。
**建议方向**：评估激光是否适合 Blit 到 Atlas——可能需要走独立贴图 fallback（类似 TrailPool 方案 A），或为 Laser Channel 使用独立 Shader。
**状态**：✅ 已回应（Round 1）— 激光不入 Atlas，保持独立贴图注册统一 RBM。UV 滚动依赖 repeat 采样，Atlas 子区域不兼容。TDD §5.3 / BC-11 / UD-10 / AC-07 已全面修正。

---

### UA-003 | 🔴高 | RT Lost 全量重 Blit 在低端安卓微信小游戏上可能导致严重卡顿甚至 OOM
**涉及 TDD 章节**：§6.1 BC-08、§6.3 UD-03
**质疑**：UD-03 确认"全量重 Blit"，但安卓微信中 RT Lost 频率极高（接电话、切微信、下拉通知栏都会触发）。假设 Bullet 4 页 × 40 张 + 其他 Channel = 200+ 次 Blit，单次 0.1ms × 200 = 20ms，低端安卓翻 4x = 80ms+。同时源纹理在 WebGL 内存压力下可能被回收，导致重 Blit 时数据无效。
**潜在风险**：1) RT Lost 恢复时 80ms+ 卡顿导致掉帧/ANR；2) 源纹理 GPU 数据被回收则 Atlas 恢复为黑色/花屏。
**建议方向**：1) 增加分帧重建策略（每帧最多 N 张）；2) 评估源纹理 CPU 端备份需求；3) BC-08 增加"源纹理也丢失"的处理。
**状态**：⚠️ 已回应（Round 1）— 量化分析实际 Blit ~61 张/12ms 可接受。补充分帧重建为 P1 优化项。源纹理引用已保持（UD-04），无丢失风险。

---

### UA-004 | 🟡中 | DamageNumberSystem 迁移到全局单 RBM 后 SortingOrder 机制断裂
**涉及 TDD 章节**：§5.4、§5.7
**质疑**：`RenderBatchManager.UploadAndDrawAll()` 第 277 行 `Graphics.DrawMesh(bucket.Mesh, Matrix4x4.identity, bucket.Material, 0)` **没有传入 sortingOrder 参数**。虽然 `RenderBucket` 存储了 `SortingOrder` 字段，但 `UploadAndDrawAll()` 完全没使用它。统一到全局单 RBM 后，Bullet/Laser/VFX/DamageNumber 的桶提交顺序取决于 `_buckets` 数组注册顺序，不是 SortingOrder。
**潜在风险**：飘字可能被子弹/特效遮挡，Z-order 正确性破坏。
**建议方向**：TDD 明确要求 RBM 改造包含"按 SortingOrder 排序桶后再提交"，或使用 `Graphics.DrawMesh` 的 sortingOrder 重载。
**状态**：✅ 已回应（Round 1）— UploadAndDrawAll 必须按 SortingOrder 排序桶后提交。已在 §5.7 和 R2.1 任务中明确。

---

## PK Round 2 — Unity 架构师复审（Round 1 评估 + 3 个新问题）

### Round 1 回应评估
- UA-001: 🟢 满意 — TDD §3.0 措辞修正消除了矛盾
- UA-002: 🟢 满意 — 激光不入 Atlas 决策正确，DanmakuLaser.shader 的 repeat 采样确认不兼容 Atlas 子区域
- UA-003: 🟡 部分解决 — 量化分析有价值，但分帧重建期间视觉表现未描述（不阻塞编码）
- UA-004: 🟢 满意 — UploadAndDrawAll 排序方向正确

---

### UA-005 | 🟡中 | 全局单 RBM 的材质绑定问题——Laser Shader 与 Bullet Shader 的 Blend 模式不同
**涉及 TDD 章节**：§3.5、§5.3、§5.7
**质疑**：TDD 要求全局单 RBM 将激光桶与子弹/VFX 桶共存。但 Laser 使用 `DanmakuLaser.shader`（`Blend SrcAlpha One`，Additive + CoreColor/GlowColor 参数），Bullet 使用 `DanmakuBullet.shader`（`Blend SrcAlpha OneMinusSrcAlpha`，Alpha Blend + Dissolve/Glow 参数）。当前 `RBM.Initialize()` 只接受**一个** `Material material` 模板参数（第 113 行），所有桶的材质实例都由同一个模板克隆。如果全局单 RBM 只能接受一个模板材质，激光桶和子弹桶无法共存。
**潜在风险**：1) RBM API 不支持多模板材质；2) 强行用 BulletMaterial 初始化 Laser 桶会导致 Blend 模式错误 + 缺少 CoreColor 参数。
**建议方向**：`Initialize` API 改为接受 `IReadOnlyList<(BucketKey key, Material templateMaterial)>` 或 `Func<BucketKey, Material>` 委托，让每个桶可以绑定各自的模板材质。此为 R2.1 必做改造。
**状态**：✅ 已回应（Round 2）— Initialize API 改为 `IReadOnlyList<BucketRegistration>`（key + templateMat + sortingOrder），每桶独立绑定材质。TDD §3.5 + R2.1 已全面更新。

---

### UA-006 | 🟡中 | §3.3/§3.4/§7.1/§7.3 "与 v1.0 相同不再赘述"——独立文档不应有外部引用依赖
**涉及 TDD 章节**：§3.3、§3.4、§7.1、§7.3
**质疑**：TDD v2.2 在四处关键技术章节写着"与 v1.0 相同"，但 v2.2 是独立交付文档。Shelf Packing 算法逻辑、RuntimeAtlasManager API 签名、Blit 策略的 CommandBuffer 细节无从查阅。
**潜在风险**：换人实施时信息断裂。
**建议方向**：将 v1.0 中被引用的内容内联回 v2.2（摘要形式即可），或确保 v1.0 在同目录下保留。不阻塞编码（开发者就是 TDD 编写者可从记忆补全），但建议编码前补全。
**状态**：✅ 已回应（Round 2）— §3.3 Shelf Packing 算法、§3.4 Blit 策略、§7.1 RuntimeAtlasManager API、§7.3 RuntimeAtlasStats 全部内联补全到 v2.3。TDD 现在是完全独立的文档。

---

### UA-007 | 🟢低 | RBM 排序实现策略未明确——每帧排序 vs 注册时排序
**涉及 TDD 章节**：§5.7
**质疑**：§5.7 声明按 SortingOrder 排序但未说明排序时机。方案 A 每帧排序（10-15 桶可忽略），方案 B 注册时排序（零运行时开销）。
**潜在风险**：低，两种方案均可行。
**建议方向**：推荐方案 B（注册时排序），但不阻塞编码。
**状态**：✅ 已回应（Round 2）— 采纳方案 B：Initialize 阶段排序 _buckets 数组，运行时零排序开销。TDD §5.7 已明确。

---

> **PK 收敛评估**：Unity 架构师建议 UA-005 回应后即可收敛进入编码，不需要继续 PK 轮次。





