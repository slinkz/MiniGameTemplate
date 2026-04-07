# WeChatBridge 模块

## 用途
微信小游戏 SDK 抽象接口层。通过工厂按平台返回实现：
- Editor / 非 WebGL：`WeChatBridgeStub`
- WebGL：`WeChatBridgeWebGL`（广告能力走 jslib，其他能力可按需逐步替换）

## 核心类
| 类 | 用途 |
|---|------|
| `IWeChatBridge` | 微信 SDK 接口定义（广告、分享、排行榜、登录、生命周期、系统工具、隐私） |
| `IWeChatAdConfigurable` | 可选广告配置扩展接口（广告位 ID 注入） |
| `WeChatBridgeStub` | 桩实现（Editor 下使用，广告/登录等回调有延迟模拟） |
| `WeChatBridgeWebGL` | WebGL 实现（激励视频、Banner、插屏） |
| `WeChatBridgeWebGLCallbackHost` | jslib 回调宿主（Unity SendMessage 入口） |
| `WeChatBridgeFactory` | 工厂类，根据平台返回对应实现，并下发广告配置 |
| `WeChatUserInfo` | 用户信息数据类（Nickname、AvatarUrl、OpenId） |
| `LaunchOptions` | 启动参数数据类（Scene、Query、ReferrerAppId） |

## 插件文件
- `Plugins/WebGL/WeChatBridge.jslib`：微信广告与基础能力 JS 桥接

## 接口覆盖
- **广告**：PreloadRewardedAd / ShowRewardedAd / ShowBannerAd / HideBannerAd / ShowInterstitialAd
- **社交**：Share / SubmitScore / ShowRankingPanel / RequestSubscribeMessage
- **用户**：Login / GetUserInfo
- **生命周期**：OnShow / OnHide / GetLaunchOptions
- **系统**：Vibrate / SetClipboardData / GetClipboardData / IsWeChatPlatform
- **隐私**：CheckPrivacyAuthorize / RequirePrivacyAuthorize / GetPrivacySettingName

## 使用方式
```csharp
WeChatBridgeFactory.SetAdUnitIds(rewardedId, bannerId, interstitialId);
var wx = WeChatBridgeFactory.Create();

wx.PreloadRewardedAd();
wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});
```

## 项目接入点
- 推荐在 `GameStartupFlow` 中注入广告位并预加载激励广告
- 业务层仅依赖 `IWeChatBridge`，不直接调用 jslib

## 详细文档
- `Docs/Agent/WECHAT_INTEGRATION.md`
