# Known Pitfalls Archive — 归档层

> 本文件存放从活跃层降级的踩坑记录。仅在活跃层未覆盖当前审查涉及的错误模式时才需查阅。
> 条目数量：13 条（PIT-001 ~ PIT-013）

---

## PIT-001: 跨文件字段缺失 — BulletTypeSO.SpeedOverLifetime
- **分类**: CL-1 跨文件引用完整性
- **日期**: 2026-04-08
- **现象**: `BulletMover` 引用 `BulletTypeSO.SpeedOverLifetime`，编译失败
- **根因**: 技术文档中规划了该字段，但生成 `BulletTypeSO.cs` 时遗漏
- **验证方法**: 对新增/修改的每个字段引用，grep 确认定义端存在
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-1 覆盖（典型案例已写入 CL-1 描述）

---

## PIT-002: SO 字段缺失 — LaserTypeSO.Length / SprayTypeSO.Duration
- **分类**: CL-1 跨文件引用完整性
- **日期**: 2026-04-08
- **现象**: `LaserTypeSO` 无 `Length` 字段、`SprayTypeSO` 无 `Duration` 字段，多处编译失败
- **根因**: 同 PIT-001，一次性生成大量 SO 时遗漏部分字段
- **验证方法**: SO 定义与使用端对照检查
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-1 覆盖（与 PIT-001 同模式）

---

## PIT-003: using 缺失 — GameStartupFlow.cs
- **分类**: CL-1 跨文件引用完整性
- **日期**: 2026-04-08
- **现象**: `GameStartupFlow.cs` 缺少 `using MiniGameTemplate.Events`，找不到 `GameEvent`
- **根因**: 新增 using 依赖时遗漏
- **验证方法**: 编辑文件后检查所有 using 是否覆盖文件中引用的类型
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-1 覆盖

---

## PIT-004: 方法实现遗漏 — RequestPrivacyAuthorizeAsync
- **分类**: CL-1 跨文件引用完整性
- **日期**: 2026-04-07
- **现象**: `GameStartupFlow.cs` 调用 `RequestPrivacyAuthorizeAsync()` 但方法体未实现
- **根因**: 在调用端写了方法调用，但忘记在被调用端添加实现
- **验证方法**: 对每个新增方法调用，确认方法定义存在
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-1 覆盖

---

## PIT-005: 命名空间与 Unity 内置类重名 — MiniGameTemplate.Debug
- **分类**: CL-2 命名空间安全
- **日期**: 2026-04-05
- **现象**: `namespace MiniGameTemplate.Debug` 导致所有 `Debug.LogWarning` 被解析为当前命名空间
- **根因**: C# 就近解析原则，自定义命名空间 `Debug` 与 `UnityEngine.Debug` 冲突
- **验证方法**: 新建命名空间前检查是否与常用类重名
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-2 覆盖（典型案例已写入 CL-2 描述）

---

## PIT-006: 类名与 Unity API 冲突 — BuildPipeline
- **分类**: CL-2 命名空间安全
- **日期**: 2026-04-05
- **现象**: 自定义 `BuildPipeline.cs` 与 `UnityEditor.BuildPipeline` 命名冲突
- **根因**: 类名与 Unity 内置类完全相同
- **验证方法**: 自定义类名前搜索 Unity API 是否有同名类
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-2 覆盖

---

## PIT-008: 第三方库命名空间错误 — SimpleJSON
- **分类**: CL-8 第三方库命名空间验证
- **日期**: 2026-04-06
- **现象**: `SimpleJSON` 实际命名空间是 `Luban.SimpleJSON`，5 个文件全报 CS0246
- **根因**: 假设第三方库命名空间而未验证实际源码
- **验证方法**: grep 第三方库源码中的 `namespace` 声明
- **严重度**: 🔴 编译阻塞（5 文件）
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-8 覆盖（典型案例已写入 CL-8 描述）

---

## PIT-009: YooAsset API 改名 — UnloadUnusedAssets
- **分类**: CL-3 Unity API 版本兼容
- **日期**: 2026-04-05
- **现象**: YooAsset 2.3.18 将 `UnloadUnusedAssets` 改名 `UnloadUnusedAssetsAsync`，`ForceUnloadAllAssets` → `UnloadAllAssetsAsync`
- **根因**: YooAsset 版本升级后 API 命名变化
- **验证方法**: 使用第三方库 API 时确认当前版本的实际方法签名
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-3 覆盖

---

## PIT-010: AudioImporterSampleSettings 无 overridden 字段
- **分类**: CL-3 Unity API 版本兼容
- **日期**: 2026-04-05
- **现象**: `AudioImporterSampleSettings` 没有 `overridden` 字段
- **根因**: 对 Unity API 结构体的成员假设错误
- **验证方法**: 不确定时查阅 Unity 文档确认结构体成员
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-3 覆盖

---

## PIT-011: Il2CppCodeGeneration 命名空间变化
- **分类**: CL-3 Unity API 版本兼容
- **日期**: 2026-04-05
- **现象**: `Il2CppCodeGeneration` 在 2022.3+ 属于 `UnityEditor.Build` 命名空间
- **根因**: Unity 跨版本 API 命名空间迁移
- **验证方法**: 版本敏感 API 必须确认目标版本的实际命名空间
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-3 覆盖

---

## PIT-012: 条件编译遗漏 — NamedBuildTarget
- **分类**: CL-3 Unity API 版本兼容
- **日期**: 2026-04-07
- **现象**: `NamedBuildTarget` 条件编译修了一处，漏了同文件另一处
- **根因**: 条件编译修复不完整，未全局搜索同一 API 的所有调用点
- **验证方法**: 添加条件编译后，全局搜索该 API 确认所有使用点都已覆盖
- **严重度**: 🔴 编译阻塞（修了又报）
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-3 覆盖（典型案例已写入 CL-3 描述）

---

## PIT-013: GGraph.DrawRect 参数签名错误
- **分类**: CL-3 Unity API 版本兼容
- **日期**: 2026-04-05
- **现象**: `GGraph.DrawRect` 签名是 5 参数，写了 3 参数
- **根因**: 对 FairyGUI API 的参数数量假设错误
- **验证方法**: 不确定方法签名时先查阅 API 文档或源码
- **严重度**: 🔴 编译阻塞
- **归档日期**: 2026-04-11
- **归档原因**: 已被 CL-3 覆盖

---

_（蒸馏降级的条目追加在此处）_
