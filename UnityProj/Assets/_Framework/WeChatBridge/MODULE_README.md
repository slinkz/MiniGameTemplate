# WeChatBridge 模块

## 用途
微信小游戏SDK的抽象接口层。定义统一接口，通过工厂模式按平台返回实现。
模板内只提供桩实现（Editor/非微信平台），实际微信SDK接入时实现 `IWeChatBridge` 接口即可。

## 核心类
| 类 | 用途 |
|---|------|
| `IWeChatBridge` | 微信SDK接口定义（广告、分享、排行榜、登录等） |
| `WeChatBridgeStub` | 桩实现（Editor下使用，所有方法打日志+返回模拟数据） |
| `WeChatBridgeFactory` | 工厂类，根据平台返回对应实现 |

## 使用方式
```csharp
var wx = WeChatBridgeFactory.Create();
wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});
```

## 接入真实SDK
1. 导入微信Unity SDK
2. 创建 `WeChatBridgeImpl : IWeChatBridge`，调用真实SDK
3. 在 `WeChatBridgeFactory` 中注册该实现
4. 详见 `Docs/WECHAT_INTEGRATION.md`
