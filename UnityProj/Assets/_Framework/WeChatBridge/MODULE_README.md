# WeChatBridge 模块

## 用途
微信小游戏SDK的抽象接口层。定义统一接口，通过工厂模式按平台返回实现。
模板内只提供桩实现（Editor/非微信平台），实际微信SDK接入时实现 `IWeChatBridge` 接口即可。

## 核心类
| 类 | 用途 |
|---|------|
| `IWeChatBridge` | 微信SDK接口定义（广告、分享、排行榜、登录、生命周期、系统工具等） |
| `WeChatBridgeStub` | 桩实现（Editor下使用，广告/登录等回调有延迟模拟） |
| `WeChatBridgeFactory` | 工厂类，根据平台返回对应实现 |
| `WeChatUserInfo` | 用户信息数据类（Nickname、AvatarUrl、OpenId） |
| `LaunchOptions` | 启动参数数据类（Scene、Query、ReferrerAppId） |

## 接口覆盖
- **广告**：PreloadRewardedAd / ShowRewardedAd / ShowBannerAd / HideBannerAd / ShowInterstitialAd
- **社交**：Share / SubmitScore / ShowRankingPanel / RequestSubscribeMessage
- **用户**：Login / GetUserInfo
- **生命周期**：OnShow / OnHide / GetLaunchOptions
- **系统**：Vibrate / SetClipboardData / GetClipboardData / IsWeChatPlatform

## 使用方式
```csharp
var wx = WeChatBridgeFactory.Create();
wx.PreloadRewardedAd(); // 预加载广告

wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});

// 获取启动参数（处理分享卡片入口）
var options = wx.GetLaunchOptions();
if (options.Query.ContainsKey("invite_id"))
    HandleInvite(options.Query["invite_id"]);
```

## 接入真实SDK
1. 导入微信Unity SDK（WX-WASM-SDK-V2）
2. 创建 `WeChatBridgeImpl : IWeChatBridge`，调用真实SDK
3. 在 `WeChatBridgeFactory` 中注册该实现
4. 详见 `Docs/WECHAT_INTEGRATION.md`
