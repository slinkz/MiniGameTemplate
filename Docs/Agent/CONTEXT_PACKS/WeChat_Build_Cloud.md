---
system: knowledge-engineering
scope: context-pack-wechat-build-cloud
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Context Pack: WeChat Build Cloud

## 适用任务

- 微信小游戏构建、WebGL 转换、CDN/Dev Server、真机验证。
- 微信广告、隐私授权、云开发、登录、云存储、跨设备同步。
- 排查资源 404、WASM 异常、域名白名单、云函数问题。

## 必读文档

| 目的 | 文档 |
|------|------|
| 微信集成总入口 | `WECHAT_INTEGRATION.md` |
| 构建流程 | `Docs/Guide/BUILD_MINIGAME.md` |
| 手机导出完整指南 | `Docs/Guide/微信小游戏导出到手机完整指南.md` |
| 云存储 TDD | `SG_TDD_06_CLOUD_SAVE.md` |
| 平台编码约束 | `CONV_03_PLATFORM.md` |
| Asset/YooAsset 背景 | `Docs/Guide/FRAMEWORK_MODULES_02_INFRA.md` |
| MCP/Unity 操作 | `MCP_INTEGRATION.md` |

## 关键代码入口

```text
UnityProj/Assets/_Framework/WeChatBridge/
UnityProj/Assets/_Framework/DataSystem/**/Cloud*.cs
UnityProj/Assets/_Framework/Editor/LocalHttpServerWindow.cs
UnityProj/Assets/_Game/Scripts/ShooterGame/**Progress*.cs
CloudFunctions/
UnityProj/Tools/
UnityProj/Assets/link.xml
```

## 关键 SO / 配置路径

```text
Assets/_Game/Configs/Core/AssetConfig*.asset
Assets/_Framework/GameLifecycle/Presets/DefaultGameConfig.asset
Assets/_Game/Configs/ShooterGame/
```

## 关键 ADR / 约束

- WebGL/微信小游戏禁止线程、阻塞文件 IO、未验证平台 API。
- 持久化优先通过框架 SaveSystem / CloudSaveSystem，不直接散落调用 PlayerPrefs 或微信 JS。
- CDN 单一数据源和域名白名单必须同时考虑开发者工具与真机。
- 构建流程区分资源变更、C# 变更、配置表变更、FairyGUI 变更。

## 常见坑

- 只在微信开发者工具关闭 urlCheck，但真机域名白名单没配。
- 改资源后只重新 WebGL 转换，忘记构建 YooAsset Bundle。
- 改 C# 后以为刷新开发者工具就生效，实际 WASM 未重建。
- link.xml 缺失导致 IL2CPP stripping 后 MissingMethodException。
- 云端权威存储和本地缓存逻辑混用。
- 启动流程没有等待云同步完成就读进度。

## 修改后必验

- 根据修改类型执行正确构建步骤：C#、资源、配置表、FairyGUI 分开判断。
- 检查 WASM 和 StreamingAssets 时间戳。
- 微信开发者工具 Console 无 MissingMethodException、memory access out of bounds、资源 404。
- 真机验证 CDN 域名、下载、请求、云函数调用。
- 云存储改动验证登录、Pull、Upload、Reload、冲突/离线策略。