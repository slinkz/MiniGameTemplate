---
system: wechat
scope: sdk-integration
last_verified: 2026-05-24
related_code: Assets/_Framework/WeChatBridge/**, Assets/_Framework/AssetSystem/WechatFileSystem/**, Assets/_Framework/Editor/LocalHttpServerWindow.cs
---

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
- ✅ CDN 单一数据源架构（运行时通过 `WXDataCDNHelper.GetDataCDN()` 从 JS 层读取 DATA_CDN，AssetConfig 不再存储 CDN 地址）
- ✅ Dev Server 一键 CDN 环境切换（`LocalHttpServerWindow`：本地调试 ↔ 远程真机）
- ✅ `MiniGameBuildPipeline` 微信小游戏硬性 PlayerSettings
- ✅ 启动时隐私授权检查（PrivacyDialog → ConfirmDialog 二次确认）
- ✅ 云开发 Cloud Init 自动化（jslib 自包含，导出后零操作）
- ✅ Cloud Function 全链路（InitCloud → CallCloudFunction → 5s 超时回调）
- ✅ CDN 域名白名单文档化（request + downloadFile 双域名必配，真机强制校验）
- ✅ CDN 元数据 Cache-Buster（RemoteServices 对 .version/.hash/.bytes/.json 追加 `?t={ticks}`）

## 接入步骤

### 1. 导入微信环境依赖

1. 从 [微信小游戏官方文档](https://developers.weixin.qq.com/minigame/dev/guide/) 导入 WX-WASM-SDK-V2（com.qq.weixin.minigame）
   - 该 SDK 会自动定义 `WEIXINMINIGAME` 编译符号
2. WechatFileSystem 扩展包已内置（`Assets/_Framework/AssetSystem/WechatFileSystem/`），无需额外导入
3. CDN 地址只需在**微信转换面板**（`MiniGameConfig.ProjectConf.CDN`）配置一处。运行时通过 `WXDataCDNHelper.GetDataCDN()` 自动读取，无需在 `AssetConfig` SO 中重复配置

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
| 云开发 | `InitCloud(envId)` | 初始化云开发环境（必须在 CallCloudFunction 前调用） |
| 云开发 | `CallCloudFunction(name, data, callback)` | 调用微信云函数（5s 超时保护） |

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
│   ├── AssetService.cs                    # 入口，WebGL case 使用 WechatFileSystem
│   └── AssetConfig.cs                     # Play Mode + 包名配置（不含 CDN 地址）
└── WechatFileSystem/
    ├── WechatFileSystem.cs                # IFileSystem 实现 + WechatFileSystemCreater 工厂
    └── Operation/
        ├── WXFSInitializeOperation.cs     # 初始化（获取 WXFileSystemManager）
        ├── WXFSRequestPackageVersionOperation.cs  # 版本请求
        ├── WXFSLoadPackageManifestOperation.cs    # 清单加载
        ├── WXFSLoadBundleOperation.cs     # Bundle 加载（缓存命中 → 本地加载，未命中 → CDN 下载）
        ├── WXFSDownloadFileOperation.cs   # 文件下载（通过 UnityWebRequest）
        └── WXFSClearCacheOperations.cs    # 缓存清理（全清 / 清理未使用）

Assets/_Framework/WeChatBridge/
├── Scripts/
│   └── WXDataCDNHelper.cs                 # 运行时从 JS 层读取 DATA_CDN（单一数据源）
└── Plugins/WebGL/
    └── WeChatBridge.jslib                 # 含 WXBridge_GetDataCDN 函数
```

### 初始化流程

`AssetService.InitializeAsync()` 在 `EAssetPlayMode.WebGL` 模式下的执行路径：

```
#if UNITY_WEBGL && WEIXINMINIGAME
  1. 读取 CDN: WXDataCDNHelper.GetDataCDN()（从 JS 层 DATA_CDN 读取，单一数据源）
  2. 计算缓存根目录: WX.env.USER_DATA_PATH + "/__GAME_FILE_CACHE/yoo"
  3. 设置时间切片: YooAssets.SetOperationSystemMaxTimeSlice(100)
  4. 计算 hostUrl: {cdnUrl}/StreamingAssets/yoo/{PackageName}
  5. 创建 WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices)
  6. 配置 WebPlayModeParameters.WebGLForceSyncLoadAsset = true
  7. 执行 InitializeAsync → RequestPackageVersionAsync → UpdatePackageManifestAsync
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

CDN 地址（`MiniGameConfig.ProjectConf.CDN`）**生产环境必须使用 HTTPS**。
`AssetService` 已内置 URL 安全校验——非 HTTPS 在 Release 构建中会报错阻断初始化。
本地开发时允许 HTTP（私网地址自动豁免）。

#### 4. URL 路径格式

`RemoteServices` 已内置 URL 规范化（`NormalizeUrl`），自动处理：
- 反斜杠 `\` → 正斜杠 `/`（防止 Windows 路径泄漏）
- 双斜杠合并（协议 `://` 除外）
- URL 末尾斜杠裁剪

**⚠️ 踩坑经验**：微信环境下 URL 包含双斜杠 `//`（如 `https://cdn.example.com//bundles/file.bundle`）
会导致请求返回 200 但内容为空或 CDN 302 到错误地址，且不抛出任何错误日志——完全静默失败。

#### 5. CDN 目录结构建议

```
https://cdn.example.com/webgl/{package_name}/
├── PackageManifest_{version}.version    # 版本号文件
├── PackageManifest_{version}.bytes      # 清单文件
├── {bundle_hash}.bundle                 # AssetBundle 文件
└── ...
```

`MiniGameConfig.ProjectConf.CDN` 只需配到 `https://cdn.example.com` 级别。
运行时自动派生完整路径：`{DATA_CDN}/StreamingAssets/yoo/{PackageName}`。

#### 6. CDN 域名白名单（⚠️ 必做，否则真机报错）

CDN 域名必须在**微信公众平台**后台加入合法域名白名单，否则真机 `wx.downloadFile` 会报 `url not in domain list` 错误。

**操作路径**：`mp.weixin.qq.com` → 开发管理 → 开发设置 → 服务器域名 → 修改

**必须同时添加到两个位置**：

| 域名类型 | 填写内容 |
|---------|---------|
| **request 合法域名** | CDN 域名（如 `https://cloud1-xxx.tcloudbaseapp.com`） |
| **downloadFile 合法域名** | 同上 |

**踩坑经验（2026-05-24）**：
- 开发者工具中 `urlCheck: false` **只对模拟器生效**，真机不会绕过白名单检查
- 云开发静态托管域名（`cloud1-xxx.tcloudbaseapp.com`）不会自动加入白名单，必须手动配置
- 每月只能修改 5 次服务器域名配置，确认无误再提交
- 如果使用了多个子域名（如 CDN + 云函数），需要逐个添加

#### 7. CDN 元数据 Cache-Buster

`AssetService.RemoteServices` 对元数据文件（`.version` / `.hash` / `.bytes` / `.json`）自动追加 `?t={ticks}` 查询参数，
强制绕过 CDN 边缘节点缓存，确保客户端始终拉取最新版本文件。`.bundle` 文件不加——它们靠 content-hash 文件名天然防缓存。

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

## 本地开发测试（Dev Server）

没有线上 CDN 时，使用内置的 **Dev Server** 在本地提供 HTTP 文件服务进行测试。

### CDN 地址架构（2026-05-17 重构）

```
MiniGameConfig.ProjectConf.CDN  ← 唯一配置点 (Single Source of Truth)
    ↓ 微信转换导出
    ↓
game.js DATA_CDN               ← 导出产物（自动生成）
    ↓ 运行时
    ↓
WXDataCDNHelper.GetDataCDN()   ← C# 运行时读取（jslib WXBridge_GetDataCDN）
    ↓
AssetService.InitializeAsync() ← 自动派生 HostServerUrl
    └→ HostServerUrl = {DATA_CDN}/StreamingAssets/yoo/{PackageName}
```

**铁律**：CDN 地址只在微信转换面板配置一处。`AssetConfig` SO 不再存储 CDN 地址。禁止双配置。

### 操作步骤

```
1. 配置 CDN 地址（二选一）

   方式 A：手动配置
   - 微信转换面板 → 游戏资源CDN → 填入地址

   方式 B：Dev Server 一键切换（推荐）
   - Unity 菜单 → Tools → MiniGame Template → Dev Server
   - 点击「🏠 本地调试」自动写入 http://{本机IP}:{端口}
   - 点击「☁️ 远程真机」自动写入生产 CDN 地址
   - 切换后需重新「转换小游戏」导出才生效

2. 构建 AssetBundle
   Unity 菜单 → YooAsset → AssetBundle Builder
   - Build Target: WebGL / Compression: LZ4
   - Copy Buildin File Option: ClearAndCopyAll

3. 导出微信小游戏
   Unity 菜单 → 微信小游戏 → 生成并转换

4. 启动 Dev Server（本地调试时）
   Unity 菜单 → Tools → MiniGame Template → Dev Server
   - 服务根目录指向 webgl/ 导出目录
   - 点击「▶ 启动服务器」
   - 启动时会自动检测 CDN 是否匹配，不匹配弹窗询问是否切换

5. 微信开发者工具
   导入构建产物 → 预览 / 真机调试
```

### 控制台日志

启动成功时会输出：
```
[AssetService] WebGL mode: DATA_CDN=http://192.168.x.x:8001 → HostServerUrl=http://192.168.x.x:8001/StreamingAssets/yoo/DefaultPackage
```

看到这条日志说明 CDN 配置生效。

### 切换到生产 CDN 模式

使用 Dev Server 面板的「☁️ 远程真机」按钮，一键切换到生产 CDN 地址。
也可手动在微信转换面板修改 CDN 地址：
```
MiniGameConfig.ProjectConf.CDN: https://cdn.yoursite.com
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


## 云开发（Cloud）

### 概述

微信云开发（`wx.cloud`）提供免服务器的云函数调用能力。本模板通过自有 jslib 实现 cloud.init + callFunction，
**导出后零操作**——无需手动编辑 `game.js`。

### 架构与调用链路

```
[C# 层]                         [JS 层 (WeChatBridge.jslib)]              [微信 Runtime]
                                                                          
GameStartupFlow                                                            
  └→ WeChatBridgeFactory.SetCloudEnvId(envId)                              
       └→ bridge.InitCloud(envId)                                          
            └→ WXBridge_InitCloud(envIdPtr)  ──→  wx.cloud.init({env})  ──→ ✅ Cloud Ready
                                                                          
WxAuthService / CloudSyncService                                           
  └→ bridge.CallCloudFunction(name, data, cb)                              
       └→ WXBridge_CallCloudFunction(id, name, data)                       
            └→ callCloudFunctionImpl(...)  ──→  wx.cloud.callFunction()    
                 ├→ success → SendMessage("OnCloudFunctionResult", json)    
                 └→ fail/timeout(5s) → SendMessage("OnCloudFunctionResult") 
```

### 配置方法

在 `GameStartupFlow` Inspector 面板中配置：

| 字段 | 说明 | 示例 |
|------|------|------|
| `_cloudEnvId` | 云开发环境 ID | `cloud1-2abc3def456` |

留空则使用默认环境（`wx.cloud.init()` 无参数）。

### 时序保证

1. `WeChatBridgeFactory.SetCloudEnvId()` 在 `Create()` 之前调用
2. `Create()` 内部自动执行 `ApplyCloudConfig()` → `bridge.InitCloud(envId)`
3. 后续任何 `CallCloudFunction()` 调用时 `wx.cloud` 已就绪

### 设计决策

**Cloud Init 方案**：自有 jslib `WXBridge_InitCloud`（方案 A）。在 jslib 中自包含 `wx.cloud.init(config)` 调用，
C# 端通过 DllImport 驱动。整条链路在项目代码内，不依赖 SDK 内部 API，不怕 SDK 版本升级。
导出后零操作——无需手动编辑 `game.js`。详见 ADR 或 2026-05-08 调研记录。

### 注意事项

1. `InitCloud` 是幂等的——重复调用不会报错（JS 层 wx.cloud.init 是幂等的）
2. 如果 `envId` 为空字符串，调用 `wx.cloud.init()` 无参版本（使用默认环境）
3. 非微信环境（Editor / 非 WebGL）走 Stub 空实现，日志提示但不报错
4. Cloud Function 有 5s 超时保护（`callCloudFunctionImpl` 内 `setTimeout`）
