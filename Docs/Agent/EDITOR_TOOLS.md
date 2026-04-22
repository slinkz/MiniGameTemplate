# Editor 工具目录

> **维护规则**：新增、删除或重命名编辑器菜单项时，**同步更新本文件**。本文件是 AI Agent 获知可用编辑器工具的唯一来源。
>
> **最后更新**：2026-04-22（整理 commit `383e45f`）

---

## 菜单总览

所有编辑器工具统一收归 **`Tools/MiniGame Template/`** 菜单下。

```
Tools/MiniGame Template/
├── SO Creation Wizard                     [150]
├── Validate/
│   ├── Architecture Check                 [200]
│   └── Asset Audit                        [210]
├── Open Docs Folder                       [300]
├── Build/
│   ├── Build WebGL (Development)          [400]
│   ├── Build WebGL (Release)              [401]
│   ├── Validate WeChat Settings           [410]
│   └── Open Build Folder                  [420]
├── Danmaku/
│   ├── Atlas Packer                       [—]
│   ├── Run Controlled Refresh             [—]
│   └── Clear Refresh Report               [—]
├── Integrations/Spine/
│   ├── Enable Spine (Current Target)      [500]
│   ├── Disable Spine (Current Target)     [501]
│   └── Validate Integration               [510]
├── Debug/
│   └── SO Runtime Viewer                  [500]
└── Find References Of Selected Asset      [600]
```

> 方括号内数字为 `MenuItem` 的 `priority` 参数。

---

## 工具详情

### SO Creation Wizard

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/SO Creation Wizard` |
| 文件 | `Assets/_Framework/Editor/SOCreationWizard.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 类型 | `EditorWindow` |
| 功能 | 快速创建常用 ScriptableObject 资产的向导窗口 |
| 支持类型 | IntVariable, FloatVariable, StringVariable, BoolVariable, GameEvent, IntGameEvent, FloatGameEvent, StringGameEvent, TransformRuntimeSet, PoolDefinition, State, SceneDefinition, AudioClip, AudioLibrary, **DanmakuBulletType** 等 |
| 默认保存路径 | `Assets/_Game/ScriptableObjects`（可在窗口内修改） |

---

### Validate / Architecture Check

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Validate/Architecture Check` |
| 文件 | `Assets/_Framework/Editor/MenuItems.cs` → 调用 `ArchitectureValidator.RunValidation()` |
| 实现 | `Assets/_Framework/Editor/ArchitectureValidator.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 功能 | 扫描 C# 脚本，检查层级依赖违规、禁用 API（`GameObject.Find`、`Resources.Load`、裸 `Debug.Log` 等）、MonoBehaviour 行数超限、`MODULE_README.md` 缺失等架构规则 |
| 调用时机 | 提交前自检（见 CONVENTIONS.md 代码提交检查清单） |

---

### Validate / Asset Audit

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Validate/Asset Audit` |
| 文件 | `Assets/_Framework/Editor/AssetAuditWindow.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 类型 | `EditorWindow` |
| 功能 | 扫描超尺寸纹理、Resources 目录中的未用资产、其他微信小游戏预算违规项 |
| 输出 | 窗口内列表，按 Error / Warning / Info 分级 |

---

### Open Docs Folder

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Open Docs Folder` |
| 文件 | `Assets/_Framework/Editor/MenuItems.cs` |
| 功能 | 在文件管理器中打开仓库根目录的 `Docs/` 文件夹 |
| 路径计算 | `Application.dataPath` → `../../Docs` |

---

### Build / Build WebGL (Development)

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Build/Build WebGL (Development)` |
| 文件 | `Assets/_Framework/Editor/BuildPipeline.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 类名 | `MiniGameBuildPipeline`（避免与 `UnityEditor.BuildPipeline` 冲突） |
| 功能 | 一键 WebGL 开发构建：自动切平台、设置 PlayerSettings、执行 Build、处理 Post-Build |
| 输出路径 | `Build/WebGL` |

---

### Build / Build WebGL (Release)

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Build/Build WebGL (Release)` |
| 文件 | 同上 `BuildPipeline.cs` |
| 功能 | 一键 WebGL 发布构建，关闭 Development Build 标志 |

---

### Build / Validate WeChat Settings

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Build/Validate WeChat Settings` |
| 文件 | 同上 `BuildPipeline.cs` |
| 功能 | 检查微信小游戏必要设置：ColorSpace=Gamma、BuildTarget=WebGL、Compression、Strip Engine Code 等 |

---

### Build / Open Build Folder

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Build/Open Build Folder` |
| 文件 | 同上 `BuildPipeline.cs` |
| 功能 | 在文件管理器中打开 `Build/WebGL` 输出目录 |

---

### Danmaku / Atlas Packer

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Danmaku/Atlas Packer` |
| 文件 | `Assets/_Framework/Editor/Rendering/DanmakuAtlasPackerWindow.cs` |
| 命名空间 | `MiniGameTemplate.Editor.Rendering` |
| 类型 | `EditorWindow` |
| 功能 | 将多张独立弹幕/VFX 贴图打包成 Atlas + 生成 `AtlasMappingSO` |
| 域选项 | Bullet / VFX |
| 尺寸选项 | 512 / 1024 / 2048 / 4096 |
| 注意 | 打包期间 `IsPackingInProgress = true`，`TextureImportEnforcer` 会跳过 `isReadable` 还原 |

---

### Danmaku / Run Controlled Refresh

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Danmaku/Run Controlled Refresh` |
| 文件 | `Assets/_Framework/DanmakuSystem/Scripts/Editor/DanmakuEditorRefreshCoordinator.cs` |
| 命名空间 | `MiniGameTemplate.Danmaku.Editor` |
| 功能 | 协调弹幕系统的编辑器刷新流程：dirty → registry rebuild → batch warmup → result report |
| 自动触发 | `[InitializeOnLoad]`，Play Mode 切换时自动运行 |

---

### Danmaku / Clear Refresh Report

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Danmaku/Clear Refresh Report` |
| 文件 | 同上 `DanmakuEditorRefreshCoordinator.cs` |
| 功能 | 清除上次刷新生成的报告数据 |

---

### Integrations / Spine / Enable Spine (Current Target)

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Integrations/Spine/Enable Spine (Current Target)` |
| 文件 | `Assets/_Framework/Editor/SpineIntegrationTools.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 功能 | 为当前 BuildTarget 添加 `FAIRYGUI_SPINE` + `ENABLE_SPINE` 宏定义，启用 Spine 运行时集成 |
| 验证条件 | BuildTargetGroup ≠ Unknown |

---

### Integrations / Spine / Disable Spine (Current Target)

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Integrations/Spine/Disable Spine (Current Target)` |
| 文件 | 同上 `SpineIntegrationTools.cs` |
| 功能 | 移除 Spine 宏定义，禁用集成 |

---

### Integrations / Spine / Validate Integration

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Integrations/Spine/Validate Integration` |
| 文件 | 同上 `SpineIntegrationTools.cs` |
| 功能 | 检查 Spine runtime DLL 是否存在于 `Assets/Spine` 和 `Assets/SpineCSharp`，输出集成状态报告 |

---

### Debug / SO Runtime Viewer

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Debug/SO Runtime Viewer` |
| 文件 | `Assets/_Framework/Editor/SORuntimeViewer.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 类型 | `EditorWindow` |
| 功能 | Play Mode 下实时查看所有活跃 SO 的值（Variables / Events / RuntimeSets），逐帧刷新 |
| 限制 | **仅 Play Mode 有效** |
| Tab 页 | Variables / Events / RuntimeSets |

---

### Find References Of Selected Asset

| 项目 | 内容 |
|------|------|
| 菜单路径 | `Tools/MiniGame Template/Find References Of Selected Asset` |
| 右键菜单 | `Assets/Find References In Project`（Project 视图右键） |
| 文件 | `Assets/_Framework/Editor/AssetReferenceFinder.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 功能 | 基于 GUID 扫描 YAML 文本资产，查找谁引用了选中资源 |
| 搜索范围 | `.unity` / `.prefab` / `.asset` / `.mat` / `.controller` / `.anim` / `.overrideController` / `.playable` / `.sbn` |
| 输出 | Console 日志，每条可点击跳转到引用位置 |
| 也可代码调用 | `AssetReferenceFinder.FindReferencers(target)` |

---

## 自动后处理器（AssetPostprocessor）

> 这些不是菜单工具，而是每次资源导入时**自动执行**的规则执行器。

### TextureImportEnforcer

| 项目 | 内容 |
|------|------|
| 文件 | `Assets/_Framework/Editor/AssetImportEnforcer.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 触发时机 | `OnPreprocessTexture` — 每张贴图导入/重导入时 |
| 跳过范围 | `ThirdParty/` 和 `Assets/FairyGUI/`（子模块） |
| 规则 | |
| &nbsp;&nbsp;最大尺寸 | 1024px（`MAX_TEXTURE_SIZE`） |
| &nbsp;&nbsp;法线贴图 | 文件名以 `_N` 结尾自动设为 NormalMap 类型 |
| &nbsp;&nbsp;MipMap | **全局禁用**（小游戏不需要 MipMap，节省 ~33% 纹理内存） |
| &nbsp;&nbsp;Read/Write | 全局禁用（Atlas 打包期间自动豁免） |
| &nbsp;&nbsp;压缩格式 | WebGL 平台自动设 ASTC_6x6（法线贴图 ASTC_4x4） |

### AudioImportEnforcer

| 项目 | 内容 |
|------|------|
| 文件 | 同上 `AssetImportEnforcer.cs` |
| 命名空间 | `MiniGameTemplate.EditorTools` |
| 触发时机 | `OnPreprocessAudio` + `OnPostprocessAudio` |
| 规则 | |
| &nbsp;&nbsp;WebGL 压缩 | Vorbis 50% / CompressedInMemory |
| &nbsp;&nbsp;短音效 | < 3 秒自动强制 Mono |

---

## 已删除工具（归档记录）

| 工具 | 文件 | 删除原因 | 删除时间 |
|------|------|---------|---------|
| BulletTypeMigrationTool | `Editor/Danmaku/BulletTypeMigrationTool.cs` | Phase 1 迁移已完成，一次性工具 | 2026-04-22 |
| VFXAssetBootstrapper | `Editor/VFXAssetBootstrapper.cs` | Stage2 Demo 资产生成器，阶段性工具 | 2026-04-22 |
| MenuItems Create/ 子菜单 | `Editor/MenuItems.cs`（Create 部分） | 与 SOCreationWizard 功能重复 | 2026-04-22 |

---

## Agent 快速参考

需要做某件事时，对照此表找工具：

| 我想... | 用这个 |
|---------|-------|
| 创建 SO 资产 | SO Creation Wizard |
| 检查代码架构违规 | Validate / Architecture Check |
| 检查资源预算违规 | Validate / Asset Audit |
| 构建微信小游戏 | Build / Build WebGL |
| 检查微信构建设置 | Build / Validate WeChat Settings |
| 打包弹幕贴图 Atlas | Danmaku / Atlas Packer |
| 刷新弹幕编辑器状态 | Danmaku / Run Controlled Refresh |
| 启用/禁用 Spine 集成 | Integrations / Spine |
| 运行时查看 SO 值 | Debug / SO Runtime Viewer |
| 查找资源被谁引用 | Find References Of Selected Asset |
| 打开文档目录 | Open Docs Folder |
