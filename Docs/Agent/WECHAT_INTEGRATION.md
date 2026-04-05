# 微信小游戏 SDK 接入指南

## 概述

MiniGameTemplate 提供了 `IWeChatBridge` 抽象接口层。模板内置了桩实现（`WeChatBridgeStub`），
在 Editor 和非微信平台下模拟所有 SDK 调用（含异步延迟模拟）。接入真实 SDK 时只需实现接口，无需修改游戏逻辑代码。

## 当前状态

- ✅ `IWeChatBridge` 接口定义完成（广告 / 社交 / 用户 / 生命周期 / 系统工具）
- ✅ `WeChatBridgeStub` 桩实现（Editor / 测试用，广告/登录有延迟模拟）
- ✅ `WeChatBridgeFactory` 工厂模式
- ✅ `AssetService` WebGL 模式（WechatFileSystem 接入口）
- ✅ `MiniGameBuildPipeline` 微信小游戏硬性 PlayerSettings
- ⬜ 真实 SDK 实现（按需接入）

## 接入步骤

### 1. 导入微信 Unity SDK

从 [微信小游戏官方文档](https://developers.weixin.qq.com/minigame/dev/guide/) 下载 WX-WASM-SDK-V2（com.qq.weixin.minigame），导入项目。

同时导入 YooAsset 的 WechatFileSystem 扩展包。

### 2. 配置 AssetService

在 AssetConfig SO 中将 Play Mode 切换为 **WebGL**。
然后取消 `AssetService.cs` 中 WebGL case 下被注释的 WechatFileSystem 初始化代码。

### 3. 创建真实实现

```csharp
// Assets/_Game/Scripts/Platform/WeChatBridgeImpl.cs
using System;
using System.Collections.Generic;
using MiniGameTemplate.Platform;

public class WeChatBridgeImpl : IWeChatBridge
{
    public bool IsWeChatPlatform => true;

    public void PreloadRewardedAd()
    {
        // WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam { adUnitId = "..." })
    }

    public void ShowRewardedAd(Action<bool> onComplete)
    {
        // 调用微信 SDK 的激励视频广告 API
    }

    public void OnShow(Action<Dictionary<string, string>> callback)
    {
        // WX.OnShow(res => callback(res.query));
    }

    public void OnHide(Action callback)
    {
        // WX.OnHide(() => callback());
    }

    public LaunchOptions GetLaunchOptions()
    {
        // var res = WX.GetLaunchOptionsSync();
        // return new LaunchOptions { Scene = res.scene, Query = res.query };
        return new LaunchOptions();
    }

    // ... 实现所有接口方法
}
```

### 4. 注册到工厂

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

### 5. 使用

游戏代码中统一通过工厂获取：

```csharp
var wx = WeChatBridgeFactory.Create();

// 预加载广告（越早越好，如进入场景时）
wx.PreloadRewardedAd();

// 展示激励视频
wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});

// 分享
wx.Share("我的小游戏", imageUrl);

// 处理分享卡片入口
var options = wx.GetLaunchOptions();
if (options.Query.ContainsKey("invite_id"))
    HandleInvite(options.Query["invite_id"]);

// 前后台切换
wx.OnShow(query => Debug.Log("回到前台"));
wx.OnHide(() => PauseGame());

// 剪贴板
wx.SetClipboardData("复制的文本");
```

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
3. **广告单元 ID** — 在真实实现中配置，建议通过 SO 或配置文件管理，不要硬编码
4. **用户隐私** — 获取用户信息前确保已获得授权
5. **所有资源加载必须异步** — WebGL 单线程不支持 WaitForAsyncComplete()
6. **ConfigManager 使用 InitializeAsync()** — 同步 Initialize() 仅限 Editor 回退
