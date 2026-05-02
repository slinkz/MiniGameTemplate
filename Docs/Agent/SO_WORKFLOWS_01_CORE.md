---
system: so-config
scope: core-configs
last_verified: 2026-05-02
related_code: Assets/_Framework/GameLifecycle/Scripts/GameConfig.cs, Assets/_Framework/GameLifecycle/Scripts/SceneDefinition.cs, Assets/_Framework/AssetSystem/Scripts/AssetConfig.cs
---

# SO 配置流程 — 01 核心配置

## GameConfig

**菜单路径**：`Create → MiniGameTemplate/Core/Game Config`
**命名空间**：`MiniGameTemplate.Core`
**源码**：`Assets/_Framework/GameLifecycle/Scripts/GameConfig.cs`
**实例目录**：`Assets/_Game/Configs/Core/`
**实例数量**：项目唯一（1 个）

### 字段清单

| 字段 | C# 属性 | 类型 | 默认值 | 说明 |
|------|---------|------|--------|------|
| `_gameName` | `GameName` | `string` | `"My Mini Game"` | 游戏显示名 |
| `_version` | `Version` | `string` | `"0.1.0"` | 版本号 |
| `_initialScene` | `InitialScene` | `SceneDefinition` | null | 启动后加载的首个场景 |
| `_targetFrameRate` | `TargetFrameRate` | `int` | `60` | 目标帧率 |
| `_runInBackground` | `RunInBackground` | `bool` | `true` | 后台是否继续运行 |

### Agent 创建代码

```csharp
var gc = ScriptableObject.CreateInstance<GameConfig>();
// 私有字段需通过 SerializedObject 设置
var so = new SerializedObject(gc);
so.FindProperty("_gameName").stringValue = "ShooterGame";
so.FindProperty("_version").stringValue = "1.0.0";
so.FindProperty("_targetFrameRate").intValue = 60;
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.CreateAsset(gc, "Assets/_Game/Configs/Core/GameConfig.asset");
AssetDatabase.SaveAssets();
```

---

## SceneDefinition

**菜单路径**：`Create → MiniGameTemplate/Core/Scene Definition`
**命名空间**：`MiniGameTemplate.Core`
**源码**：`Assets/_Framework/GameLifecycle/Scripts/SceneDefinition.cs`
**实例目录**：`Assets/_Game/Configs/Core/`
**实例数量**：每个场景一个

### 字段清单

| 字段 | C# 属性 | 类型 | 默认值 | 说明 |
|------|---------|------|--------|------|
| `_sceneName` | `SceneName` | `string` | `""` | Build Settings 中的场景名 |
| `_scenePath` | `ScenePath` | `string` | `""` | YooAsset 资源路径（空=SceneManager 回退） |
| `_isAdditive` | `IsAdditive` | `bool` | `false` | 是否叠加加载 |
| `_description` | — | `string` | `""` | 编辑器注释（Editor Only） |

### Agent 创建代码

```csharp
var sd = ScriptableObject.CreateInstance<SceneDefinition>();
var so = new SerializedObject(sd);
so.FindProperty("_sceneName").stringValue = "GameScene";
so.FindProperty("_scenePath").stringValue = "Assets/Scenes/GameScene.unity";
so.FindProperty("_isAdditive").boolValue = false;
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.CreateAsset(sd, "Assets/_Game/Configs/Core/Scene_Game.asset");
AssetDatabase.SaveAssets();
```

---

## AssetConfig

**菜单路径**：`Create → MiniGameTemplate/Core/Asset Config`
**命名空间**：`MiniGameTemplate.Asset`
**源码**：`Assets/_Framework/AssetSystem/Scripts/AssetConfig.cs`
**实例目录**：`Assets/_Game/Configs/Core/`
**实例数量**：项目唯一（1 个）

### 字段清单

| 字段 | C# 属性 | 类型 | 默认值 | 合法值 | 说明 |
|------|---------|------|--------|--------|------|
| `_defaultPackageName` | `DefaultPackageName` | `string` | `"DefaultPackage"` | 非空 | YooAsset 包名 |
| `_playMode` | `PlayMode` | `EAssetPlayMode` | `EditorSimulate` | 枚举 | 资源加载模式 |
| `_cdnUrl` | `CdnUrl` | `string` | `""` | URL 格式 | CDN 基础 URL（SSOT） |
| `_fallbackCdnUrl` | `FallbackCdnUrl` | `string` | `""` | URL 格式 | 备用 CDN |

### EAssetPlayMode 枚举

| 值 | 用途 |
|----|------|
| `EditorSimulate` | 编辑器直接加载（无需构建 Bundle） |
| `Offline` | 从 StreamingAssets 离线加载 |
| `Host` | 远程 CDN + 本地缓存 |
| `WebGL` | 微信小游戏模式（需 CDN + WX-WASM-SDK-V2） |

### 派生属性

| 属性 | 计算方式 |
|------|---------|
| `HostServerUrl` | `{CdnUrl}/StreamingAssets/yoo/{PackageName}` |
| `FallbackHostServerUrl` | `{FallbackCdnUrl}/StreamingAssets/yoo/{PackageName}` |

### 注意事项

- `CdnUrl` 必须与 `MiniGameConfig.ProjectConf.CDN`（微信配置）保持一致
- Dev Server 工具可校验一致性：`Tools → MiniGame → Build → Verify CDN`

### Agent 创建代码

```csharp
var ac = ScriptableObject.CreateInstance<AssetConfig>();
var so = new SerializedObject(ac);
so.FindProperty("_defaultPackageName").stringValue = "DefaultPackage";
so.FindProperty("_playMode").enumValueIndex = 0; // EditorSimulate
so.FindProperty("_cdnUrl").stringValue = "https://cdn.example.com";
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.CreateAsset(ac, "Assets/_Game/Configs/Core/AssetConfig.asset");
AssetDatabase.SaveAssets();
```
