# AssetSystem — 资源管理模块

## 概述

基于 [YooAsset](https://www.yooasset.com/) 的轻量资源管理封装。提供统一的资源加载、场景加载和热更新 API，无需直接依赖 YooAsset 内部类型。

## 核心组件

| 文件 | 职责 |
|------|------|
| `AssetConfig.cs` | SO 配置资产，控制 YooAsset 初始化参数（包名、运行模式、CDN 地址） |
| `AssetService.cs` | 单例服务，封装 YooAsset 的初始化、加载、卸载、热更新流程 |

## 运行模式

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| **EditorSimulate** | 直接从 AssetDatabase 加载，无需构建 Bundle | 编辑器开发阶段 |
| **Offline** | 从 StreamingAssets 中的预构建 Bundle 加载 | 单机发布、微信小游戏首包 |
| **Host** | 从远程 CDN 加载，本地缓存 | 线上热更新 |

## 使用方式

### 1. 创建配置资产

右键 Project 面板 → Create → MiniGameTemplate → Core → Asset Config

### 2. 初始化（在 GameBootstrapper 中自动完成）

```csharp
await AssetService.Instance.InitializeAsync(assetConfig);
```

### 3. 加载资源

```csharp
// 加载预制件
var handle = AssetService.Instance.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemy.prefab");
await handle.Task;
var prefab = handle.AssetObject as GameObject;
var instance = Object.Instantiate(prefab);

// 用完释放引用
handle.Release();
```

### 4. 加载场景

```csharp
var sceneHandle = AssetService.Instance.LoadSceneAsync("Assets/Scenes/GameScene.unity");
await sceneHandle.Task;
```

### 5. 热更新流程（Host 模式）

```csharp
var version = await AssetService.Instance.RequestPackageVersionAsync();
await AssetService.Instance.UpdatePackageManifestAsync(version);
var downloader = AssetService.Instance.CreateResourceDownloader();
if (downloader != null)
{
    downloader.BeginDownload();
    await downloader.Task;
}
```

## 微信小游戏注意事项

- Bundle 文件名**不能含中文**
- **不要**将资源放在 StreamingAssets（WebGL 不支持同步文件访问）
- 仅支持**异步加载**
- 需要额外引入 `WX-WASM-SDK-V2` 和 YooAsset 的 `WechatFileSystem` 扩展

## 依赖

- [YooAsset 2.3.x](https://github.com/tuyoogame/YooAsset)（通过 OpenUPM 引入）
