---
system: editor-tools
scope: validate-audit-reference
last_verified: 2026-05-02
related_code: Assets/_Framework/Editor/ArchitectureValidator.cs, Assets/_Framework/Editor/AssetAuditWindow.cs, Assets/_Framework/Editor/AssetReferenceFinder.cs, Assets/_Framework/Editor/SOCreationWizard.cs
---

# 编辑器工具手册 — 校验 / 审计 / 资源工具

## Architecture Check（架构校验）

**菜单路径**：`Tools/MiniGame Template/Validate/Architecture Check`
**源码**：`Assets/_Framework/Editor/ArchitectureValidator.cs`
**用途**：扫描全部运行时 C# 脚本，检查架构违规（禁用 API、SRP 超限、模块 README 缺失）

### 规则详情

**Error 级（违规必须修复）**：

| 模式 | 说明 |
|------|------|
| `GameObject.Find(` | 禁止场景全局查找，改用 SO 引用 |
| `FindObjectOfType<` | 禁止昂贵的运行时查找 |
| `FindObjectsOfType<` | 同上 |
| `Resources.Load<` | 禁止 Resources 加载，使用 YooAsset |

**Warning 级（建议修复）**：

| 模式 | 说明 |
|------|------|
| `static T Instance {` | 自制单例，应使用 `Singleton<T>` 基类或 SO 事件 |
| `DontDestroyOnLoad(` | 应仅存在于 Singleton<T>/Bootstrapper |
| 文件超 200 行 | SRP 指标——建议拆分 |

**白名单**（不检查）：`Singleton.cs`、`GameBootstrapper.cs`、所有 Editor/ 和 ThirdParty/ 目录

### 附加检查
- **MODULE_README.md**：扫描 `Assets/_Framework/` 和 `Assets/_Game/` 的一级子目录，缺失则 Warning
- **Spine 一致性**：`FAIRYGUI_SPINE` 和 `ENABLE_SPINE` 必须同步启用/禁用

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Validate/Architecture Check");
// 或直接：
MiniGameTemplate.EditorTools.ArchitectureValidator.RunValidation();
```

### 输出
- 每条违规输出到 Console（Error/Warning），包含文件路径和行号
- 末尾汇总：`X error(s), Y warning(s) in Z files`

### 常见错误
| 情况 | 原因 | 解决 |
|------|------|------|
| 注释中的匹配被误报 | 不会——工具先剥离注释再匹配 |  |
| Editor 脚本报 Find | 不会——自动跳过 Editor/ 目录 |  |

---

## Asset Audit（资源预算审计）

**菜单路径**：`Tools/MiniGame Template/Validate/Asset Audit`
**源码**：`Assets/_Framework/Editor/AssetAuditWindow.cs`
**用途**：可视化窗口，扫描项目贴图/音频/Resources 目录的预算违规

### 审计项

| 类别 | 检查内容 | 级别 |
|------|---------|------|
| Texture | maxTextureSize > 1024 | Warning |
| Texture | isReadable = true（2x 内存浪费） | Warning |
| Texture | WebGL override 使用 RGBA32（未压缩） | Error |
| Texture | 缺少 WebGL override | Warning |
| Audio | 缺少 WebGL override | Warning |
| Audio | .wav 文件 > 500KB | Warning |
| Resources | Resources/ 下文件 > 1MB | Warning |

### 操作步骤
1. 菜单打开窗口
2. 点击「Run Full Audit」按钮
3. 扫描完成后，列表显示所有违规条目
4. 点击条目右侧「Select」可跳转到对应资产

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Validate/Asset Audit");
```

> **注意**：这是 EditorWindow，需用户交互点击按钮运行。MCP 只能打开窗口。

---

## Find References Of Selected Asset（资源引用反查）

**菜单路径**：`Tools/MiniGame Template/Find References Of Selected Asset`
**右键菜单**：`Assets/Find References In Project`
**源码**：`Assets/_Framework/Editor/AssetReferenceFinder.cs`
**用途**：基于 GUID 扫描所有 YAML 序列化资产，查找谁引用了选中资源

### 支持的文件类型
`.unity` `.prefab` `.asset` `.mat` `.controller` `.overrideController` `.anim` `.playable` `.sbn`

### 操作步骤
1. 在 Project 视图选中目标资源
2. 执行菜单或右键
3. Console 输出所有引用者路径（可点击跳转）

### Agent MCP 调用
```csharp
// 先设置 Selection，再执行菜单
Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>("Assets/path/to/target.asset");
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Find References Of Selected Asset");

// 或直接代码调用（无需 Selection）：
var results = MiniGameTemplate.EditorTools.AssetReferenceFinder.FindReferencers("Assets/path/to/target.asset", showProgress: true);
foreach (var r in results) Debug.Log(r);
```

### 输出
- 汇总：目标路径 + 引用者数量
- 每条引用单独一行，点击 Console 条目可跳转到引用者

---

## SO Creation Wizard（ScriptableObject 创建向导）

**菜单路径**：`Tools/MiniGame Template/SO Creation Wizard`
**源码**：`Assets/_Framework/Editor/SOCreationWizard.cs`
**用途**：可视化窗口，快速创建项目中所有常用 ScriptableObject 类型

### 支持的 SO 类型

| 分类 | 类型 |
|------|------|
| 数据变量 | IntVariable, FloatVariable, StringVariable, BoolVariable |
| 事件 | GameEvent, IntGameEvent, FloatGameEvent, StringGameEvent |
| 运行时集合 | TransformRuntimeSet |
| 对象池 | PoolDefinition |
| 状态机 | State |
| 场景 | SceneDefinition |
| 音频 | AudioClipSO, AudioLibrary |
| 弹幕 | BulletType, LaserType, SprayType, ObstacleType, BulletPattern, PatternGroup, SpawnerProfile, DifficultyProfile, WorldConfig, RenderConfig, TimeScale |
| Entity | EntityConfig, AIBehavior, EntitySpawnWave |

### 操作步骤
1. 打开向导窗口
2. 选择类型、填写名称、确认保存路径
3. 点击「Create」

### 默认路径覆盖
| 类型 | 默认保存路径 |
|------|------------|
| EntityConfig | `Assets/_Game/Configs/Entity` |
| AIBehavior | `Assets/_Game/Configs/AI` |
| EntitySpawnWave | `Assets/_Game/Configs/SpawnWave` |
| 其他 | `Assets/_Game/ScriptableObjects` |

### 特殊行为
- **EntityConfig 创建时自动预填** State + Health + Movement + Collision 四个组件（WF-006）

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/SO Creation Wizard");
```

> **提示**：也可通过 Project 面板右键 `Create → MiniGameTemplate → ...` 创建 SO。
