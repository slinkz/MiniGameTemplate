---
system: rendering
scope: so-vfx-render-configs
last_verified: 2026-05-02
related_code: Assets/_Framework/VFXSystem/Scripts/Config/*.cs, Assets/_Framework/Rendering/*.cs
---

# SO 配置流程 — 04 VFX + 渲染

## VFXTypeSO

**菜单路径**：`Create → MiniGameTemplate/VFX/VFX Type`
**命名空间**：`MiniGameTemplate.VFX`
**源码**：`Assets/_Framework/VFXSystem/Scripts/Config/VFXTypeSO.cs`
**实例目录**：`Assets/_Game/Configs/VFX/`

### 字段清单

**资源描述**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SourceTexture` | `Texture2D` | null | 源贴图 |
| `UVRect` | `Rect` | (0,0,1,1) | UV 区域 |
| `AtlasBinding` | `AtlasMappingSO` | null | Atlas 绑定 |

**Sprite Sheet**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Columns` | `int` | 1 | 列数 |
| `Rows` | `int` | 1 | 行数 |
| `TotalFrames` | `int` | 1 | 有效帧数 |
| `FramesPerSecond` | `float` | 12 | 播放帧率 |

**播放**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Loop` | `bool` | false | 循环（false=播完回收） |
| `RotateWithInstance` | `bool` | false | 随实例旋转 |

**渲染**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Size` | `float` | 1.0 | 世界单位尺寸 |
| `Tint` | `Color` | white | 颜色 |

**附着模式**（ADR-013）

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AttachMode` | `VFXAttachMode` | `World` | World=固定 / FollowTarget=跟随 |

### 运行时辅助

| 属性/方法 | 说明 |
|-----------|------|
| `MaxFrameCount` | 有效帧数 |
| `Duration` | 播放总时长 |
| `GetResolvedTexture()` | 解析实际贴图 |
| `GetResolvedBaseUV()` | 解析基础 UV |
| `GetFrameUV(frameIndex)` | 帧 UV 计算 |

---

## VFXRenderConfig

**菜单路径**：`Create → MiniGameTemplate/VFX/Render Config`
**命名空间**：`MiniGameTemplate.VFX`
**源码**：`Assets/_Framework/VFXSystem/Scripts/Config/VFXRenderConfig.cs`
**实例数量**：项目唯一（1 个）

### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `NormalMaterial` | `Material` | 基础混合材质 |
| `AtlasTexture` | `Texture2D` | 共用图集（Fallback） |
| `RuntimeAtlasConfig` | `RuntimeAtlasConfig` | 运行时图集（空=旧路径） |

---

## AtlasMappingSO

**菜单路径**：`Create → MiniGameTemplate/Rendering/Atlas Mapping`
**命名空间**：`MiniGameTemplate.Rendering`
**源码**：`Assets/_Framework/Rendering/AtlasMappingSO.cs`

### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `AtlasTexture` | `Texture2D` | 打包生成的图集 |
| `Padding` | `int` | 像素间距 |
| `Entries` | `AtlasEntry[]` | 子图映射条目 |

### AtlasEntry

| 字段 | 类型 | 说明 |
|------|------|------|
| `SourceTexture` | `Texture2D` | 源贴图引用 |
| `SourceGUID` | `string` | 源贴图 GUID |
| `UVRect` | `Rect` | 归一化 UV |
| `PixelRect` | `RectInt` | 像素区域 |

### API

| 方法 | 说明 |
|------|------|
| `TryFindEntry(source, out entry)` | 按引用或 GUID 查找 |
| `GetUVRectForSource(source)` | 快速获取 UV |

### 注意

AtlasMappingSO 通常由 `DanmakuAtlasPackerWindow` 工具自动生成，不需要手动创建。

---

## RuntimeAtlasConfig

**菜单路径**：`Create → MiniGameTemplate/Rendering/Runtime Atlas Config`
**命名空间**：`MiniGameTemplate.Rendering`
**源码**：`Assets/_Framework/Rendering/RuntimeAtlasSystem/RuntimeAtlasConfig.cs`
**实例数量**：项目唯一（1 个）

### 字段清单

| 字段 | 类型 | 预设 | 说明 |
|------|------|------|------|
| `Bullet` | `AtlasChannelConfig` | Default | 弹丸通道 |
| `VFX` | `AtlasChannelConfig` | Default | 特效通道 |
| `DamageText` | `AtlasChannelConfig` | Small | 飘字通道 |
| `Laser` | `AtlasChannelConfig` | Small | 激光通道 |
| `Trail` | `AtlasChannelConfig` | Small | 拖尾通道 |
| `Character` | `AtlasChannelConfig` | Default | 角色通道 |

### API

| 方法 | 说明 |
|------|------|
| `GetChannelConfig(channel)` | 按枚举获取通道配置 |
| `Validate()` | 校验所有通道参数合法 |

### AtlasChannelConfig 预设

| 预设 | 含义 |
|------|------|
| `Default` | 标准尺寸（适合弹丸/VFX/角色） |
| `Small` | 小尺寸（适合飘字/激光/拖尾） |
