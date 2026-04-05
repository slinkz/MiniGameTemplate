# 微信小游戏 SDK 接入指南

## 概述

MiniGameTemplate 提供了 `IWeChatBridge` 抽象接口层。模板内置了桩实现（`WeChatBridgeStub`），
在 Editor 和非微信平台下模拟所有 SDK 调用。接入真实 SDK 时只需实现接口，无需修改游戏逻辑代码。

## 当前状态

- ✅ `IWeChatBridge` 接口定义完成
- ✅ `WeChatBridgeStub` 桩实现（Editor/测试用）
- ✅ `WeChatBridgeFactory` 工厂模式
- ⬜ 真实 SDK 实现（按需接入）

## 接入步骤

### 1. 导入微信 Unity SDK

从 [微信小游戏官方文档](https://developers.weixin.qq.com/minigame/dev/guide/) 下载 Unity SDK 并导入项目。

### 2. 创建真实实现

```csharp
// Assets/_Game/Scripts/Platform/WeChatBridgeImpl.cs
using MiniGameTemplate.Platform;

public class WeChatBridgeImpl : IWeChatBridge
{
    public bool IsWeChatPlatform => true;

    public void ShowRewardedAd(System.Action<bool> onComplete)
    {
        // 调用微信 SDK 的激励视频广告 API
        // WX.CreateRewardedVideoAd(...)
    }

    // ... 实现所有接口方法
}
```

### 3. 注册到工厂

修改 `WeChatBridgeFactory.cs` 中的平台检测逻辑：

```csharp
public static IWeChatBridge Create()
{
    if (_instance != null) return _instance;

    #if UNITY_WEBGL && !UNITY_EDITOR
        _instance = new WeChatBridgeImpl();
    #else
        _instance = new WeChatBridgeStub();
    #endif

    return _instance;
}
```

### 4. 使用

游戏代码中统一通过工厂获取：

```csharp
var wx = WeChatBridgeFactory.Create();

// 展示激励视频
wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});

// 分享
wx.Share("我的小游戏", imageUrl);

// 提交排行榜
wx.SubmitScore(score);
```

## 接口功能清单

| 方法 | 说明 |
|------|------|
| `ShowRewardedAd(callback)` | 激励视频广告 |
| `ShowBannerAd()` | Banner 广告 |
| `HideBannerAd()` | 隐藏 Banner |
| `ShowInterstitialAd()` | 插屏广告 |
| `Share(title, imageUrl, query)` | 分享 |
| `SubmitScore(score)` | 提交排行榜分数 |
| `ShowRankingPanel()` | 显示排行榜 |
| `Login(callback)` | 登录 |
| `GetUserInfo()` | 获取用户信息 |
| `Vibrate(isLong)` | 振动反馈 |
| `IsWeChatPlatform` | 是否微信环境 |

## 注意事项

1. **不要在模板中锁死 SDK 版本** — 微信 SDK 更新频繁，每个项目按需拉最新
2. **桩实现默认返回成功** — 方便开发和测试，但上线前务必对接真实 SDK 测试
3. **广告单元 ID** — 在真实实现中配置，建议通过 SO 或配置文件管理，不要硬编码
4. **用户隐私** — 获取用户信息前确保已获得授权
