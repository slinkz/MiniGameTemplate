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
- ✅ `AssetService` WebGL 模式（WechatFileSystem 接入口）
- ✅ `MiniGameBuildPipeline` 微信小游戏硬性 PlayerSettings
- ✅ 启动时隐私授权检查（PrivacyDialog → ConfirmDialog 二次确认）

## 接入步骤

### 1. 导入微信环境依赖

1. 从 [微信小游戏官方文档](https://developers.weixin.qq.com/minigame/dev/guide/) 导入 WX-WASM-SDK-V2（com.qq.weixin.minigame）
2. 按需导入 YooAsset WechatFileSystem 扩展包

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

## 注意事项

1. **不要在模板中锁死 SDK 版本** — 微信 SDK 更新频繁，每个项目按需拉最新
2. **桩实现带延迟模拟** — 广告回调 1.5s、登录 0.5s，更接近真实环境的异步行为
3. **广告单元 ID** — 当前默认从 `GameStartupFlow` Inspector 注入；生产项目建议改为安全配置源（SO/远端配置）并支持热更新
4. **Banner API 兼容性** — 微信文档已建议新项目优先 `wx.createCustomAd`；当前模板保留 `createBannerAd` 作为最低门槛示例
5. **用户隐私** — 框架内置了隐私授权流程（`CheckPrivacyAuthorize` → `RequirePrivacyAuthorize`），`GameStartupFlow` 会在启动时自动检查。真实实现需对接微信 `wx.requirePrivacyAuthorize` 和 `wx.getPrivacySetting` API
6. **所有资源加载必须异步** — WebGL 单线程不支持 WaitForAsyncComplete()
7. **ConfigManager 使用 InitializeAsync()** — 同步 Initialize() 仅限 Editor 回退


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
