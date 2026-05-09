---
system: editor-tools
scope: build-export-devserver
last_verified: 2026-05-02
related_code: Assets/_Framework/Editor/BuildPipeline.cs, Assets/_Framework/Editor/Build/BuildModeSwitch.cs, Assets/_Framework/Editor/LocalHttpServerWindow.cs
---

# 编辑器工具手册 — 构建 / 导出 / Dev Server

## Build WebGL (Development)

**菜单路径**：`Tools/MiniGame Template/Build/Build WebGL (Development)`
**源码**：`Assets/_Framework/Editor/BuildPipeline.cs` → `MiniGameBuildPipeline.BuildDevelopment()`
**用途**：一键执行 WebGL 开发构建（含 Profiler 连接）

### 前置条件
- Build Settings 中至少有一个场景

### 操作步骤
1. 点击菜单或通过 MCP 调用
2. 自动切换到 WebGL 平台（如未切换）
3. 自动配置 PlayerSettings（Gamma、WASM、Disabled Compression 等）
4. 运行 Architecture Validation（仅顾问级，不阻断构建）
5. 执行构建，输出到 `Build/WebGL`

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Build/Build WebGL (Development)");
```

### 输出/副作用
- 产出目录：`<UnityProj>/Build/WebGL`
- 自动设置：ColorSpace=Gamma, Compression=Disabled, WASM, Incremental GC
- Development 模式特有：FullWithStacktrace 异常、External Debug Symbols、OptimizeSpeed

### 常见错误
| 错误信息 | 原因 | 解决 |
|---------|------|------|
| No scenes in Build Settings | Build Settings 场景列表为空 | 添加 Boot 场景 |
| Build failed | IL2CPP/WASM 编译错误 | 查看 Console 详细错误日志 |

---

## Build WebGL (Release)

**菜单路径**：`Tools/MiniGame Template/Build/Build WebGL (Release)`
**源码**：同上
**用途**：一键执行 WebGL 发布构建（High Stripping, OptimizeSize）

步骤与 Development 相同，区别：
- ManagedStrippingLevel = High
- ExceptionSupport = ExplicitlyThrownExceptionsOnly
- IL2CPP = OptimizeSize
- 无 Debug Symbols

---

## Validate WeChat Settings

**菜单路径**：`Tools/MiniGame Template/Build/Validate WeChat Settings`
**源码**：`BuildPipeline.cs` → `ValidateWeChatSettings()`
**用途**：检查当前 PlayerSettings 是否符合微信小游戏要求

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Build/Validate WeChat Settings");
```

### 检查项
| 项目 | 期望值 |
|------|-------|
| ColorSpace | Gamma |
| BuildTarget | WebGL |
| Incremental GC | true |

---

## Open Build Folder

**菜单路径**：`Tools/MiniGame Template/Build/Open Build Folder`
**用途**：在文件管理器中打开 `Build/WebGL` 输出目录

---

## 切换到导出模式 (Build Bundle + WebGL)

**菜单路径**：`Tools/MiniGame/切换到导出模式 (Build Bundle + WebGL)`
**源码**：`Assets/_Framework/Editor/Build/BuildModeSwitch.cs`
**用途**：一键完成导出前准备——切换 PlayMode 并构建 YooAsset Bundle

### 前置条件
- 非编译中、非 Play Mode
- `Assets/_Game/ScriptableObjects/Config/DefaultAssetConfig.asset` 存在

### 操作步骤
1. 弹窗确认
2. AssetConfig.PlayMode → WebGL
3. YooAsset SBP 构建 Bundle（LZ4 + ClearAndCopyAll）
4. 版本号自动生成 `yyyy-MM-dd-HHmm`
5. 构建完成后提示下一步操作

### Agent MCP 调用
```csharp
// 注意：此菜单有确认弹窗，MCP 直接调用会被弹窗阻断
// 建议通过 unity_execute_code 直接调用方法：
BuildModeSwitch.SwitchToExportMode();
```

### 常见错误
| 错误信息 | 原因 | 解决 |
|---------|------|------|
| 找不到 AssetConfig | SO 资产路径错误或被删除 | 检查 `Assets/_Game/ScriptableObjects/Config/DefaultAssetConfig.asset` |
| ErrorCode115 | 同版本输出目录已存在 | 工具自动清理，正常情况不会出现 |

---

## 切换到编辑器模式 (EditorSimulate)

**菜单路径**：`Tools/MiniGame/切换到编辑器模式 (EditorSimulate)`
**源码**：同上 `BuildModeSwitch.cs`
**用途**：切回 EditorSimulate，无需 Bundle 即可在编辑器中运行

### Agent MCP 调用
```csharp
BuildModeSwitch.SwitchToEditorMode();
```

---

## Dev Server（本地开发服务器）

**菜单路径**：`Tools/MiniGame Template/Dev Server`
**源码**：`Assets/_Framework/Editor/LocalHttpServerWindow.cs`
**用途**：一键启停 `npx http-server`，为微信开发者工具提供本地 Bundle 加载

### 前置条件
- Node.js 已安装（npx 可用）
- 微信导出目录已存在 `webgl/` 子目录

### 操作步骤
1. 打开窗口
2. 配置 Node.js 目录（可选，自动探测）、端口（默认 8001）、服务根目录（自动检测 webgl/）
3. 点击「▶ 启动服务器」
4. 自动执行 CDN 一致性检查
5. 使用「🔍 健康检查」验证服务可用

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Dev Server");
// 窗口打开后需手动操作按钮，或通过 unity_execute_code：
LocalHttpServerWindow.ShowWindow();
```

### 功能
| 功能 | 说明 |
|------|------|
| 启动/停止 | 一键控制 http-server 进程 |
| 健康检查 | 请求 `StreamingAssets/yoo/DefaultPackage/DefaultPackage.version` |
| CDN 一致性 | 对比 AssetConfig.CdnUrl 与 MiniGameConfig.CDN |
| 端口冲突检测 | 自动检测并提供「强制释放端口」按钮 |
| 域重载安全 | Unity 重编译后进程保持运行，重开窗口刷新状态可恢复 |

### 常见错误
| 错误信息 | 原因 | 解决 |
|---------|------|------|
| 找不到 npx | Node.js 未安装或不在 PATH | 在窗口中手动指定 Node.js 目录 |
| 端口被占用 | 上次残留进程 / 微信开发者工具 | 点击「强制释放端口」 |
| CDN 不一致 | AssetConfig 与 MiniGameConfig CDN 不匹配 | 在对应面板修改为一致 |
