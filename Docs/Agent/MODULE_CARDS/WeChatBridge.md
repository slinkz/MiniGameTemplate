---
system: knowledge-engineering
scope: module-card-wechat-bridge
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/WeChat_Build_Cloud.md
---

# Module Card: WeChatBridge

## 1. 模块职责

WeChatBridge 提供微信小游戏 SDK 抽象层：广告、分享、登录、用户信息、隐私授权、生命周期、云函数、振动、剪贴板，以及 Editor/非微信平台 Stub fallback。

## 2. 不负责什么

- 不直接保存游戏进度，进度持久化由 DataSystem / SaveSystem 承担。
- 不决定游戏业务奖励逻辑，只返回 SDK 调用结果。
- 不管理 CDN 资源构建流程，但会受微信平台和域名白名单约束影响。
- 不绕过 `GameBootstrapper` 初始化；业务侧不应散落直接调用微信 JS。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `IWeChatBridge` | 平台能力抽象接口 |
| `WeChatBridgeFactory` | 根据平台创建 WebGL 或 Stub 实现 |
| `WeChatBridgeWebGL` | WebGL/微信环境真实桥接 |
| `WeChatBridgeWebGLCallbackHost` | 接收 jslib SendMessage 回调 |
| `WeChatBridgeStub` | Editor/非微信平台桩实现 |
| `WxAuthService` | 登录状态机、openid/token 内存管理 |
| `WeChatBridge.jslib` | 微信 JS API 调用层 |

## 4. 数据流

```text
GameBootstrapper / GameStartupFlow
  -> WeChatBridgeFactory
  -> IWeChatBridge
  -> WebGL jslib / Stub
  -> CallbackHost
  -> C# callback
  -> Game/UI/DataSystem
```

## 5. 生命周期

```text
Bootstrap 注入配置 -> 创建 Bridge -> 隐私授权/广告预加载/Cloud Init -> 业务调用 -> 回调分发 -> 退出清理
```

微信环境与 Editor 环境必须保持同一接口语义；Editor 用 Stub 模拟成功/失败路径。

### 登录语义澄清

当前代码里有两条登录相关路径，不能混用理解：

- `IWeChatBridge.Login(Action<bool, string>)` 保留传统微信登录接口语义，接口注释仍以 auth code / code2session 为安全边界说明。
- `WxAuthService.Login()` 是云存储 V4 实际使用的静默登录路径，直接调用 `IWeChatBridge.CallCloudFunction("login", "{}")`，由云开发注入 openid，并在内存中维护 openid/token。

因此修改云存储、Pull、Upload、Reload 时，应优先审查 `WxAuthService`、`CloudSyncService`、`CloudSaveSystem` 和 `CallCloudFunction` 回调链；只有修改传统登录/用户信息能力时，才把 `IWeChatBridge.Login` 当作主入口。

## 6. 依赖关系

WeChatBridge 属于平台抽象层。Game 层、DataSystem、UI 可以依赖 `IWeChatBridge`，但 WeChatBridge 不反向依赖 ShooterGame 业务规则。

## 7. 关键配置 / 资产路径

```text
UnityProj/Assets/_Framework/WeChatBridge/
UnityProj/Assets/_Framework/WeChatBridge/Plugins/WebGL/WeChatBridge.jslib
UnityProj/Assets/_Framework/GameLifecycle/
UnityProj/Assets/_Framework/Editor/LocalHttpServerWindow.cs
CloudFunctions/
UnityProj/Assets/link.xml
```

## 8. 关键 ADR / 约束

- WebGL/微信小游戏禁止线程、阻塞文件 IO、未验证平台 API。
- 业务侧通过 `IWeChatBridge` 和框架服务调用平台能力，不直接散落 JS 调用。
- 云函数回调必须有 requestId 或等价机制，避免并发回调串线。
- 隐私授权、域名白名单、真机限制是当前事实源，不以 Editor Stub 结果替代真机结论。

## 9. 热路径 / 平台约束

- 不在热路径频繁调用 JS bridge。
- 回调 JSON 解析和字符串分配应限制在异步回调路径。
- IL2CPP stripping 风险要检查 `link.xml`。
- 微信开发者工具通过不等于真机通过。

## 10. 常见错误

- 只验证 Editor Stub，未验证微信开发者工具或真机。
- 改广告/云函数后忘记隐私授权、域名白名单或 Cloud Init。
- C# 改动后只刷新微信开发者工具，未重建 WebGL/WASM。
- jslib 回调名、CallbackHost 方法名、C# requestId 分发不一致。
- 忘记 link.xml 导致 IL2CPP stripping 后 MissingMethodException。

## 11. 修改前必读

- `CONTEXT_PACKS/WeChat_Build_Cloud.md`
- `WECHAT_INTEGRATION.md`
- `SG_TDD_06_CLOUD_SAVE.md`
- `CONV_03_PLATFORM.md`
- `MCP_INTEGRATION.md`

## 12. 修改后必验

- Editor Stub 路径不崩溃，成功/失败回调可模拟。
- 微信开发者工具 Console 无 JS/C# bridge 异常。
- 真机验证隐私授权、广告、登录、云函数或本次改动涉及的具体 API。
- 检查 CDN/request/downloadFile 域名白名单。
- C# 改动后确认 WebGL/WASM 已重建。
- 涉及 stripping 时检查 `link.xml` 与真机 MissingMethodException。
