# 微信小游戏 SDK 接入指南

## 概述

MiniGameTemplate 提供了 `IWeChatBridge` 抽象接口层。模板内置了桩实现（`WeChatBridgeStub`），
在 Editor 和非微信平台下模拟所有 SDK 调用（含异步延迟模拟）。接入真实 SDK 时只需实现接口，无需修改游戏逻辑代码。

## 当前状态

- ✅ `IWeChatBridge` 接口定义完成（广告 / 社交 / 用户 / 生命周期 / 系统工具 / 隐私授权）
- ✅ `WeChatBridgeStub` 桩实现（Editor / 测试用，广告/登录有延迟模拟，隐私授权有状态跟踪）
- ✅ `WeChatBridgeFactory` 工厂模式（WebGL 自动切 `WeChatBridgeWebGL`，其余平台走 Stub）
- ✅ WebGL 广告桥接已落地：
  - `Assets/_Framework/WeChatBridge/Scripts/WeChatBridgeWebGL.cs`
  - `Assets/_Framework/WeChatBridge/Scripts/WeChatBridgeWebGLCallbackHost.cs`
  - `Assets/_Framework/WeChatBridge/Plugins/WebGL/WeChatBridge.jslib`
- ✅ `GameStartupFlow` 支持广告位注入与激励广告预加载
- ✅ `MainMenuPanel` / `ClickCounterPanel` 已接入 Banner、插屏、激励广告示例流程
- ✅ `AssetService` WebGL 模式（WebPlayModeParameters + WechatFileSystem 完整接入）
- ✅ `WechatFileSystem` 扩展包（IFileSystem 实现 + 6 个 Operation + WechatFileSystemCreater 工厂）
- ✅ `RemoteServices` URL 规范化（反斜杠 / 双斜杠 / TrimEnd 防御）
- ✅ WebGL 本地测试模式（HostServerUrl 为空时自动 fallback 到 StreamingAssets，无需 CDN）
- ✅ `MiniGameBuildPipeline` 微信小游戏硬性 PlayerSettings
- ✅ 启动时隐私授权检查（PrivacyDialog → ConfirmDialog 二次确认）

## 接入步骤

### 1. 导入微信环境依赖

1. 从 [微信小游戏官方文档](https://developers.weixin.qq.com/minigame/dev/guide/) 导入 WX-WASM-SDK-V2（com.qq.weixin.minigame）
   - 该 SDK 会自动定义 `WEIXINMINIGAME` 编译符号
2. WechatFileSystem 扩展包已内置（`Assets/_Framework/AssetSystem/WechatFileSystem/`），无需额外导入
3. 在 `AssetConfig` SO 中配置 CDN URL（HostServerUrl / FallbackHostServerUrl），确保 HTTPS

### 2. 配置广告位 ID（必须）

在 `GameStartupFlow` 组件中填写：
- `_rewardedAdUnitId`
- `_bannerAdUnitId`
- `_interstitialAdUnitId`

这些值会在运行时通过 `WeChatBridgeFactory.SetAdUnitIds(...)` 注入桥接层。

### 3. 运行时调用方式（业务侧不变）

```csharp
var wx = WeChatBridgeFactory.Create();

wx.PreloadRewardedAd();
wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});

wx.ShowBannerAd();
wx.HideBannerAd();
wx.ShowInterstitialAd();
```

### 4. 默认行为说明

- **Editor / 非 WebGL**：始终使用 `WeChatBridgeStub`
- **WebGL + 微信环境**：广告能力走 `WeChatBridgeWebGL + jslib`
- **WebGL 但非微信环境或广告位为空**：自动回退 `Stub` 行为（不崩溃）

### 5. 仍可按项目扩展真实能力

当前 WebGL 实现优先补齐广告链路；社交/登录/排行榜/订阅消息等能力仍可按项目需求继续在 `WeChatBridgeWebGL` 中逐步替换为真实 JS 调用。


## 接口功能清单

| 分类 | 方法 | 说明 |
|------|------|------|
| 广告 | `PreloadRewardedAd()` | 预加载激励视频 |
| 广告 | `ShowRewardedAd(callback)` | 激励视频广告 |
| 广告 | `ShowBannerAd()` | Banner 广告 |
| 广告 | `HideBannerAd()` | 隐藏 Banner |
| 广告 | `ShowInterstitialAd()` | 插屏广告 |
| 社交 | `Share(title, imageUrl, query)` | 分享 |
| 社交 | `SubmitScore(score)` | 提交排行榜分数 |
| 社交 | `ShowRankingPanel()` | 显示排行榜 |
| 社交 | `RequestSubscribeMessage(ids, callback)` | 订阅消息授权 |
| 用户 | `Login(callback)` | 登录 |
| 用户 | `GetUserInfo()` | 获取用户信息 |
| 生命周期 | `OnShow(callback)` | 前台回调 |
| 生命周期 | `OnHide(callback)` | 后台回调 |
| 生命周期 | `GetLaunchOptions()` | 获取启动参数 |
| 系统 | `Vibrate(isLong)` | 振动反馈 |
| 系统 | `SetClipboardData(text, callback)` | 复制到剪贴板 |
| 系统 | `GetClipboardData(callback)` | 读取剪贴板 |
| 系统 | `IsWeChatPlatform` | 是否微信环境 |
| 隐私 | `CheckPrivacyAuthorize(callback)` | 检查隐私授权状态（callback 参数 needAuthorize 表示是否需要弹窗授权） |
| 隐私 | `RequirePrivacyAuthorize(callback)` | 发起隐私授权请求（用户同意/拒绝后回调） |
| 隐私 | `GetPrivacySettingName()` | 获取隐私设置名称（用于 UI 显示） |

## 构建配置

使用 `Tools → MiniGame Template → Build → Build WebGL` 一键构建。MiniGameBuildPipeline 会自动配置：

| 设置 | 值 | 原因 |
|------|------|------|
| Color Space | Gamma | 微信小游戏不支持 Linear |
| Compression | Disabled | 微信插件自带压缩，避免双重压缩 |
| Decompression Fallback | Off | 微信环境不需要 |
| Name Files As Hashes | On | CDN 缓存友好 |
| Incremental GC | On | 减少 GC 卡顿 |
| Managed Stripping | High (Release) | 减小 WASM 体积 |
| IL2CPP Code Gen | OptimizeSize (Release) | 减小 WASM 体积（`#if UNITY_2022_3_OR_NEWER`） |

构建后使用微信小游戏 Unity 插件转换 WebGL 输出为微信小游戏项目。

## YooAsset 微信小游戏资源系统

### 架构概览

微信小游戏的资源加载使用 YooAsset 2.3.x 的 `WebPlayModeParameters` + 自定义 `WechatFileSystem`。
这套方案解决了三个核心问题：

1. **缓存管理** — 微信不支持标准文件系统，需通过 `WX.GetCachePath()` 和 `WXFileSystemManager` 操作
2. **CDN 加载** — Bundle 从远程 CDN 下载，通过微信缓存系统避免重复下载
3. **URL 安全** — 微信环境对 URL 格式极其敏感，双斜杠会导致静默加载失败

### 文件结构

```
Assets/_Framework/AssetSystem/
├── Scripts/
│   └── AssetService.cs                    # 入口，WebGL case 使用 WechatFileSystem
└── WechatFileSystem/
    ├── WechatFileSystem.cs                # IFileSystem 实现 + WechatFileSystemCreater 工厂
    └── Operation/
        ├── WXFSInitializeOperation.cs     # 初始化（获取 WXFileSystemManager）
        ├── WXFSRequestPackageVersionOperation.cs  # 版本请求
        ├── WXFSLoadPackageManifestOperation.cs    # 清单加载
        ├── WXFSLoadBundleOperation.cs     # Bundle 加载（缓存命中 → 本地加载，未命中 → CDN 下载）
        ├── WXFSDownloadFileOperation.cs   # 文件下载（通过 UnityWebRequest）
        └── WXFSClearCacheOperations.cs    # 缓存清理（全清 / 清理未使用）
```

### 初始化流程

`AssetService.InitializeAsync()` 在 `EAssetPlayMode.WebGL` 模式下的执行路径：

```
#if UNITY_WEBGL && WEIXINMINIGAME
  1. 计算缓存根目录: WX.env.USER_DATA_PATH + "/__GAME_FILE_CACHE/yoo"
  2. 设置时间切片: YooAssets.SetOperationSystemMaxTimeSlice(100)
  3. 创建 WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices)
  4. 配置 WebPlayModeParameters.WebGLForceSyncLoadAsset = true
  5. 执行 InitializeAsync → RequestPackageVersionAsync → UpdatePackageManifestAsync
#else
  回退到标准 DefaultWebServerFileSystem（非微信 WebGL 环境）
#endif
```

### 条件编译符号

| 符号 | 用途 |
|------|------|
| `UNITY_WEBGL` | Unity WebGL 平台宏 |
| `WEIXINMINIGAME` | 微信小游戏 SDK (com.qq.weixin.minigame) 提供的宏 |

两个符号同时定义时才启用 WechatFileSystem；否则回退到标准 Web 文件系统。

### CDN 缓存策略要求（必读）

微信小游戏的 CDN 配置必须满足以下要求，否则会出现资源加载异常：

#### 1. Cache-Control 响应头

| 资源类型 | 推荐 Cache-Control | 说明 |
|----------|---------------------|------|
| AssetBundle 文件 | `public, max-age=31536000, immutable` | Bundle 文件名含 Hash，内容不变 |
| 版本文件 `PackageManifest_*.version` | `no-cache` 或 `max-age=60` | 每次热更需拉最新版本号 |
| 清单文件 `PackageManifest_*.bytes` | `public, max-age=31536000` | 版本号不同文件名不同，可长缓存 |

#### 2. CORS 配置

CDN 必须返回以下响应头（微信 WebView 跨域要求）：

```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, OPTIONS
Access-Control-Allow-Headers: Content-Type
```

#### 3. HTTPS 强制

`AssetConfig` 中的 `HostServerUrl` 和 `FallbackHostServerUrl` **必须使用 HTTPS**。
`AssetService` 已内置 URL 安全校验——非 HTTPS 在 Release 构建中会报错阻断初始化。

#### 4. URL 路径格式

`RemoteServices` 已内置 URL 规范化（`NormalizeUrl`），自动处理：
- 反斜杠 `\` → 正斜杠 `/`（防止 Windows 路径泄漏）
- 双斜杠合并（协议 `://` 除外）
- URL 末尾斜杠裁剪

**⚠️ 踩坑经验**：微信环境下 URL 包含双斜杠 `//`（如 `https://cdn.example.com//bundles/file.bundle`）
会导致请求返回 200 但内容为空或 CDN 302 到错误地址，且不抛出任何错误日志——完全静默失败。

#### 5. CDN 目录结构建议

```
https://cdn.example.com/minigame/{package_name}/
├── PackageManifest_{version}.version    # 版本号文件
├── PackageManifest_{version}.bytes      # 清单文件
├── {bundle_hash}.bundle                 # AssetBundle 文件
└── ...
```

`AssetConfig.HostServerUrl` 应配置到 `https://cdn.example.com/minigame/{package_name}` 级别。

### 缓存空间管理

微信小游戏有 **200MB** 本地存储上限（`wx.getStorageInfoSync` 查询），框架提供两种清理方式：

```csharp
// 通过 YooAsset 的标准接口调用
var package = YooAssets.GetPackage("DefaultPackage");

// 清理全部缓存（用户手动"清理缓存"功能）
var clearAllOp = package.ClearCacheFilesAsync(new ClearCacheFilesOptions { 
    ClearMode = EFileClearMode.ClearAllBundleFiles.ToString() 
});

// 清理未使用的缓存（版本更新后自动调用）
var clearUnusedOp = package.ClearCacheFilesAsync(new ClearCacheFilesOptions { 
    ClearMode = EFileClearMode.ClearUnusedBundleFiles.ToString() 
});
```

## 无 CDN 本地真机测试

没有 CDN 服务器时，可以用 **本地测试模式** 在微信开发者工具和真机上验证项目。

### 原理

`AssetService` 的 WebGL 模式在检测到 `AssetConfig.HostServerUrl` **为空** 时，
自动切换为 `DefaultWebServerFileSystem`——从 StreamingAssets 目录加载 Bundle。
微信开发者工具本身提供 HTTP 服务，StreamingAssets 中的文件天然可通过 HTTP 访问。

### 操作步骤

```
1. Unity 切换到 WebGL 平台
   File → Build Settings → WebGL → Switch Platform

2. 确认编译符号
   Player Settings → Other Settings → Scripting Define Symbols
   确保包含 WEIXINMINIGAME（安装 WX-WASM-SDK-V2 后通常自动添加）

3. 构建 AssetBundle
   Unity 菜单 → YooAsset → AssetBundle Builder
   - Build Pipeline: ScriptableBuildPipeline
   - Build Target: WebGL
   - Compression: LZ4
   - Copy Buildin File Option: ClearAndCopyAll  ← 关键！把 Bundle 复制到 StreamingAssets
   - 点击 Build

4. 配置 AssetConfig SO（Inspector 中）
   - Play Mode: WebGL
   - Host Server Url: 留空           ← 触发本地测试模式
   - Fallback Host Server Url: 留空

5. 构建微信小游戏
   Unity 菜单 → Tools → MiniGame Template → Build → Build WebGL (Release)
   或使用微信 SDK 提供的导出工具

6. 微信开发者工具
   导入构建产物 → 预览 / 真机调试
```

### 控制台日志

本地测试模式启动时会输出：
```
[AssetService] WebGL LOCAL mode: loading bundles from StreamingAssets.
Set HostServerUrl in AssetConfig to enable CDN mode for production.
```

看到这条日志说明本地模式生效。

### 切换到生产 CDN 模式

在 `AssetConfig` 中填入 CDN URL 即可自动切换：
```
Host Server Url: https://cdn.yoursite.com/minigame/DefaultPackage
```

此时 Bundle 构建时 `Copy Buildin File Option` 改为 **None**（不再放 StreamingAssets），
所有 Bundle 从 CDN 下载并由 WechatFileSystem 缓存。

### ⚠️ 注意事项

- 本地模式的 Bundle 会打进包体，**包体会变大**——仅用于测试阶段
- 微信小游戏首包限制 **20MB**（代码包）+ **200MB**（小游戏分包），Bundle 过大时注意分包
- 测试完毕后务必切回 CDN 模式，避免把 StreamingAssets 里的 Bundle 带进生产构建

## 注意事项

1. **不要在模板中锁死 SDK 版本** — 微信 SDK 更新频繁，每个项目按需拉最新
2. **桩实现带延迟模拟** — 广告回调 1.5s、登录 0.5s，更接近真实环境的异步行为
3. **广告单元 ID** — 当前默认从 `GameStartupFlow` Inspector 注入；生产项目建议改为安全配置源（SO/远端配置）并支持热更新
4. **Banner API 兼容性** — 微信文档已建议新项目优先 `wx.createCustomAd`；当前模板保留 `createBannerAd` 作为最低门槛示例
5. **用户隐私** — 框架内置了隐私授权流程（`CheckPrivacyAuthorize` → `RequirePrivacyAuthorize`），`GameStartupFlow` 会在启动时自动检查。真实实现需对接微信 `wx.requirePrivacyAuthorize` 和 `wx.getPrivacySetting` API
6. **所有资源加载必须异步** — WebGL 单线程不支持 WaitForAsyncComplete()
7. **ConfigManager 使用 InitializeAsync()** — 同步 Initialize() 仅限 Editor 回退
8. **YooAsset 时间切片** — 已配置 `SetOperationSystemMaxTimeSlice(100)` 防止每帧只处理一个异步操作导致加载龟速
9. **WebGLForceSyncLoadAsset** — 已启用，让 WebGL 下的资源加载行为接近同步，避免逐帧滴答式加载


## 隐私授权流程

框架在 `GameStartupFlow` 中实现了完整的启动时隐私授权检查：

```
GameStartupFlow.CheckPrivacyAsync()
  └→ IWeChatBridge.CheckPrivacyAuthorize(needAuthorize =>
       ├→ needAuthorize == false → 已授权，继续
       └→ needAuthorize == true
            └→ 弹出 PrivacyDialog（SortOrder = 700，高于 LoadingPanel）
                 ├→ 用户点"同意" → RequirePrivacyAuthorize() → 授权完成
                 └→ 用户点"拒绝" → ConfirmDialog 二次确认
                      ├→ 用户改主意"同意" → 重试授权
                      └→ 坚持拒绝 → throw OperationCanceledException
```

**关键注意**：PrivacyDialog 和 ConfirmDialog 的 `SortOrder` 必须高于 `LAYER_LOADING`（600），否则对话框会被 LoadingPanel 遮挡导致界面卡死。当前设置为 `LAYER_LOADING + 100 = 700`。
