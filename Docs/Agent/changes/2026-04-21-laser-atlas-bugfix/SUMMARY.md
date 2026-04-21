# Laser Atlas 不可见 Bug 修复（ADR-032）

## 动机

`LaserTypeSO.UseRuntimeAtlas = true` 时，激光和预警线在 Editor Play Mode 下完全不可见（alpha=0）。ADR-031 实施的 Laser Atlas 功能无法正常工作。

## 变更范围

### 核心修复
- **`RenderBatchManager.CreateBucket()`**：`new Material(templateMaterial)` 后显式复制 `shaderKeywords`（根因修复）
- **`RenderBatchManager.TryGetOrCreateBucket()`**：新增线性兜底扫描，防止字典索引不同步导致重复建桶

### Shader 变体
- **`DanmakuLaser.shader`**：新增 `#pragma multi_compile_local __ _ATLASMODE_ON` 分支
  - Atlas 模式：跳过 `tex2D` 采样，纯程序化渐变（CoreColor + GlowColor + smoothstep）
  - 非 Atlas 模式：保持原有纹理采样

### UV 语义分离
- **`LaserRenderer.cs`**：Atlas 模式 UV.x = [0,1] 渐变参数，UV.y = Atlas 子区域归一化
- **`LaserWarningRenderer.cs`**：同 LaserRenderer
- 新增 `_laserMaterialAtlas` 材质克隆（`EnableKeyword("_ATLASMODE_ON")`），Dispose 时销毁

## 关键决策

| 编号 | 决策 | 理由 |
|------|------|------|
| FIX-001 | `new Material()` 后必须显式赋值 `shaderKeywords` | Unity 行为不可靠，防御性编程，一行代码零成本 |
| FIX-002 | Atlas Laser 跳过纹理采样 | 避免 Atlas 子区域边缘溢出；激光以渐变为主，纹理贡献 <10% |
| FIX-003 | UV.x 始终保持 [0,1] | Shader 的 `abs(x-0.5)*2` 硬编码依赖归一化 UV |
| FIX-004 | 动态建桶增加线性去重 | 冷路径优先正确性，字典索引可能暂时失效 |

## 行为变化

- **AC-16（新增）**：项目级铁律——`new Material()` 后必须 `clone.shaderKeywords = source.shaderKeywords`

## 根因分析（三层叠加）

| 层级 | 根因 | 影响 |
|------|------|------|
| L1（Shader 变体） | `DanmakuLaser.shader` 缺 Atlas 分支 | UV.x 被压缩到子区域，coreMask=0 |
| L2（UV 映射） | UV.x 未保持 [0,1] 归一化 | 渐变参数语义被破坏 |
| **L3（Keyword 🔴）** | `new Material()` 不复制 `multi_compile_local` keyword | L1/L2 修复全部失效 |

## 排查思路（可复用）

```
确认渲染数据存在 → 确认纹理绑定 → 检查 shaderKeywords 数组 → 逆推克隆链路
```

## 已知遗留

- 无。修复完整，已通过编译和 Editor 运行时验证。

## 关联

- ADR: ADR-032（shaderKeywords 铁律）
- 修复对象: ADR-031（Laser 接入 RuntimeAtlas）
- 影响: ADR-028（RuntimeAtlas 核心）、ADR-030（TypeRegistry）
- Commits: 2f5afb9
