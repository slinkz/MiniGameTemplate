# Debug Playbook（Agent 调试经验手册）

> 目的：把高价值、可复用的排查经验沉淀下来，给广智和其他 Agent 后续处理 Unity / 渲染 / 弹幕 / RuntimeAtlas 问题时直接复用。
>
> 原则：先确认现象，再验证链路；先看运行时真实数据，再猜原因；每次排查都要把“错误假设”和“最终证据”分开记录。

---

## 1. 适用范围

本文主要覆盖以下问题类型：

- Unity 运行时“看起来没报错，但画面不对/不显示”
- 弹幕系统子弹、激光、VFX 不可见
- RuntimeAtlas 接入后的贴图丢失、透明、错位
- RenderBatchManager / Mesh 顶点流 / Shader / Blit 链路问题
- 使用 Unity MCP 做运行时排查时的步骤和注意事项

---

## 2. 这次案例的最终结论

### 2.1 表面现象

- 子弹不显示
- 控制台一度有 `Mesh vertex buffer attributes were supplied in non-standard order` 警告
- 修掉顶点布局警告后，子弹仍然不可见

### 2.2 最终根因（不是一个，是两个串联问题）

#### 根因 A：CPU 顶点结构体与 GPU 顶点声明顺序不一致

- `RenderVertex` 字段顺序一度是 `Position -> UV -> Color`
- `RenderBatchManager.VertexLayout` / Unity 实际标准顺序是 `Position -> Color -> TexCoord0`
- Unity 会在 `SetVertexBufferParams()` 时**静默重排**为标准顺序
- 结果：CPU 写入的内存布局与 GPU 读取布局错位，UV / Color 被错读

#### 根因 B：RuntimeAtlasBlit shader 的顶点变换写错

- `RuntimeAtlasBlit.shader` 使用了：

```hlsl
o.vertex = UnityObjectToClipPos(v.vertex);
```

- 但 RuntimeAtlas 的 Blit 是在 `CommandBuffer + SetRenderTarget` 路径下执行的，不是稳定的 Camera 渲染上下文
- 此时 VP 矩阵不可依赖，fullscreen quad 会被变换到错误位置
- 结果：Blit 没真正写进 Atlas RT，RenderTexture 里全是透明像素
- 子弹虽然有位置、有颜色、有 UV，但采样到的纹理 alpha = 0，所以还是不可见

### 2.3 最终修复

#### 修复 A：统一顶点布局为 Unity 标准顺序

```csharp
// RenderVertex
Position -> Color -> UV

// VertexLayout
Position -> Color -> TexCoord0
```

#### 修复 B：RuntimeAtlasBlit 直接 passthrough NDC 顶点

```hlsl
o.vertex = float4(v.vertex.xy, 0, 1);
```

---

## 3. 这次排查里最重要的经验

## 3.1 不要把“有 DrawCall”误判成“画面一定能看到”

这次一开始已经确认：

- `BulletRenderer.TotalDrawCount > 0`
- bucket `QuadCount > 0`
- Mesh 顶点位置已写入
- `Graphics.DrawMesh()` 在执行

但最后画面还是看不到。

**结论：**
“有 DrawCall”只能证明**几何提交链路**是通的，不能证明：

- 顶点属性没错位
- shader 采样到了正确纹理
- 纹理里真的有像素
- alpha 不是 0
- quad 在正确 clip space

所以后续排查必须分层：

1. **逻辑层**：对象有没有生成
2. **批处理层**：bucket / quad / mesh 有没有提交
3. **顶点层**：position / color / uv 是否正确
4. **纹理层**：采样源纹理/图集是否真的有像素
5. **shader 层**：顶点变换 / 片元输出是否合理

别在第 2 层通了以后就宣布胜利。那样很容易被打脸。

---

## 3.2 Unity 的顶点属性顺序不能“凭感觉”写

### 规则
Unity 的标准顶点属性顺序是：

```text
Position -> Normal -> Tangent -> Color -> TexCoord0 -> TexCoord1 ... -> TexCoord7 -> BlendWeight -> BlendIndices
```

如果你声明成非标准顺序，比如：

```text
Position -> TexCoord0 -> Color
```

Unity 会**静默重排**。

### 风险
一旦 `RenderVertex` 结构体字段顺序没有跟着这个“标准顺序”走，CPU 和 GPU 的内存理解就分家了。

### 硬规则
以后凡是自定义 Mesh 顶点流，必须同时满足：

- `VertexAttributeDescriptor[]` 按 Unity 标准顺序声明
- `[StructLayout(LayoutKind.Sequential)]` 结构体字段顺序与声明顺序完全一致
- 运行时若出现 `non-standard order` 警告，必须视为红灯，立刻修

---

## 3.3 CommandBuffer 下的 fullscreen blit，不能依赖 UnityObjectToClipPos

这是这次最值钱的经验之一。

### 错误认知
以前容易默认：

- 只要是 shader 顶点函数
- 只要输入是 quad
- 就可以直接 `UnityObjectToClipPos(v.vertex)`

这在**普通 Camera 渲染**里通常没问题，但在：

- `CommandBuffer`
- `SetRenderTarget`
- 非标准 camera 上下文
- 或 editor/runtime 混合路径

里，VP 矩阵上下文可能根本不是你以为的那个。

### 正确做法
如果 fullscreen quad 顶点本来就是 NDC（-1~1），就直接 passthrough：

```hlsl
o.vertex = float4(v.vertex.xy, 0, 1);
```

### 判断标准
如果你在做的是“把一张纹理画进 RenderTexture 的某个 viewport”，优先问自己：

> 我现在需要的是 object space -> clip space 变换，还是已经拿着 clip/NDC 顶点了？

如果已经是 NDC，就别再多做一层变换。

---

## 3.4 透明问题不要只看材质参数，要直接读 RT 像素

这次真正破案的关键证据不是日志，而是：

- 直接 `ReadPixels` 读取 RuntimeAtlas 的 RenderTexture
- 发现 atlas 整块区域都是 `RGBA(0,0,0,0)`

这一步一下子把问题从“猜测材质、猜测 UV、猜测 blend”收缩成：

> Blit 根本没成功写进去。

### 经验
当你怀疑：

- Atlas 没内容
- RT Lost
- Blit 没执行
- shader 输出透明

不要只看 inspector，不要只看 `Texture.name`，直接读像素。

### 推荐检查项

- RT 是否 `IsCreated()`
- RT 尺寸是否符合预期
- 目标 UV 区域是否存在非透明像素
- 中心区域 / 左下角 / 分配区域采样值是否合理

一句话：**纹理问题，最终要回到像素证据。**

---

## 3.5 排查顺序必须从“现象”推进到“证据”，不要一上来改代码

这次中间走弯路，核心原因之一就是：

- 看到“不显示”就开始猜 bucket、材质、顶点、shader
- 有些修改是基于推断，不是基于证据

### 正确顺序

#### 第一步：确认系统是否真的在跑

检查：

- 是否进入 Play Mode
- `DanmakuSystem` 是否初始化
- `BulletRenderer.TotalDrawCount`
- 活跃子弹数量
- 控制台是否有错误/警告

#### 第二步：确认对象有没有生成

检查：

- `BulletWorld.activeBullets`
- 子弹位置、速度、phase、animScale、animAlpha
- TypeIndex 是否正确

#### 第三步：确认批处理链路是否通

检查：

- bucket 是否命中
- `QuadCount`
- `Mesh.vertexCount`
- `Material.mainTexture`
- `SortingOrder`

#### 第四步：确认顶点数据是否正确

检查：

- position 是否在视野内
- color 是否为预期 tint / alpha
- uv 是否落在预期 atlas 区域

#### 第五步：确认纹理源是否有内容

检查：

- RuntimeAtlas allocation 是否 valid
- RT 是否 created
- RT 像素是否非透明

#### 第六步：最后才怀疑 shader 变换/混合

检查：

- vertex 输出是否正确
- fragment 是否采样透明
- blend / ztest / cull 是否合理

**原则：每一步都要拿到运行时证据，再进入下一步。**

---

## 4. 本次案例的标准排查模板（后续复用）

以后遇到“某个渲染对象不显示”，按这个模板走。

### 4.1 先问 5 个问题

1. 对象有没有生成？
2. 有没有进入批处理/DrawCall？
3. 顶点 position / uv / color 对不对？
4. 纹理/图集里到底有没有像素？
5. shader 最终输出是不是透明了？

### 4.2 推荐检查命令/动作

#### 运行时状态
- 查 `DanmakuSystem` / renderer / pool / world 的实例状态
- 看 `TotalDrawCount` / active count / capacity

#### 顶点数据
- 直接读 bucket.Vertices 前几个 quad
- 看 position / color / uv

#### 纹理数据
- 读 RuntimeAtlas allocation
- 对 RT 做 `ReadPixels`
- 采样目标区域像素

#### 画面验证
- 截 Game View
- 不要只凭“我觉得应该能看到”
- 截图和数值要互相印证

---

## 5. 这次踩过的坑

## 5.1 “红蓝两个圆点”不一定是子弹

一开始截图里看到红蓝两个圆，以为子弹恢复了。
后来结合位置、数量、运行时数据再看，才发现那只是场景里的可见对象/标记，不是弹幕真正的飞行效果。

### 经验
截图不能脱离上下文解释。
必须结合：

- 子弹数量
- 子弹位置
- 预期弹型贴图
- 截图时间点

一起判断。

---

## 5.2 “修掉一个警告”不代表问题结束

顶点布局警告修掉后，确实解决了一个真实 bug。
但子弹还是不可见，说明还有第二层问题。

### 经验
如果“一个修复解释不了全部现象”，就别强行收工。
要继续追问：

> 这个修复解释了什么？还解释不了什么？

这句话非常重要。

---

## 5.3 运行时反射验证比主观猜测靠谱

这次大量使用了运行时反射/执行代码去验证：

- `VertexLayout` 实际顺序
- `RenderVertex` 字段 offset
- `BulletRenderer.TotalDrawCount`
- `RenderBucket.QuadCount`
- `RuntimeAtlas` allocation / page / cache

### 经验
对于 Unity 这种“编辑器状态 + 运行时状态 + 资源状态”混在一起的系统，
**运行时真实值**比代码静态阅读更关键。

代码告诉你“理论上应该怎样”，
反射和运行时检查告诉你“现在实际上怎样”。

---

## 6. 后续强制规则（给 Agent）

## 6.1 遇到“渲染不显示”时，禁止直接下结论

至少要拿到下面 4 类证据中的 3 类：

- 运行时对象数量证据
- 顶点数据证据
- 纹理像素证据
- Game View 截图证据

证据不够，不准宣布“已修复”。

## 6.2 遇到自定义 Mesh 顶点流修改时，必须做 4 个检查

- 检查 `VertexAttributeDescriptor[]` 顺序
- 检查结构体字段顺序
- 检查 `Marshal.OffsetOf`
- 检查 Play Mode 是否还有 `non-standard order` 警告

## 6.3 遇到 RenderTexture / Atlas 问题时，必须读像素

只看：

- allocation valid
- texture name
- mainTexture 绑定成功

都不够。

必须至少做一次像素采样或读回。

## 6.4 遇到 CommandBuffer blit 问题时，优先怀疑坐标空间

优先检查：

- 输入顶点是不是已经是 NDC
- shader 是否错误使用了 `UnityObjectToClipPos`
- viewport / render target 是否正确

不要先去怀疑“是不是 Unity 抽风”。多数时候不是。

---

## 7. 推荐后续补强项

这次虽然修好了，但还可以继续补强。

### 7.1 给 RuntimeAtlas 增加开发期自检

建议：

- 开发环境下，首次 Blit 后自动抽样 atlas 像素
- 如果目标区域全透明，直接报错/警告

这样能更早发现“Blit 没写进去”。

### 7.2 给 RenderBatchManager 增加顶点布局断言

建议：

- 开发环境下校验 `RenderVertex` offset 与 `VertexLayout` 是否一致
- 不一致直接报错，而不是等画面出问题

### 7.3 给 Debug 文档持续追加案例

本文不要只停留在这次。
后续：

- 材质污染
- sorting order 错乱
- RT Lost 恢复失败
- SpriteSheet UV 越界
- Laser 独立贴图回归问题

都应该继续往这份文档里加。

---

## 8. 一句话总结

这次最核心的教训不是“某一行 shader 写错了”，而是：

> **渲染问题必须按链路分层排查，并且每一层都要拿运行时证据；否则你很容易修掉一个真问题，却错过另一个更深的问题。**

再说得更狠一点：

> **不要因为看到 DrawCall、看到 Mesh、看到材质绑定了纹理，就以为东西一定能显示。真正决定可见性的，是最终像素。**
