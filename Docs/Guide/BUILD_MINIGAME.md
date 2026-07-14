# 微信小游戏构建操作指南

> **目标**：从 Unity 编辑器到微信开发者工具能正常运行游戏  
> **预计时间**：首次 20-40 分钟，后续增量构建 5-15 分钟  
> **最后更新**：2026-04-27 | 适用版本：Unity 2022.3 LTS + WX SDK 0.1.32 + YooAsset 2.3.18

---

## 📋 前置条件

在执行构建之前，确认以下条件全部满足：

- [ ] Unity Build Target 已切换到 **WebGL**（`File → Build Settings → WebGL → Switch Platform`）
- [ ] 微信小游戏转换工具已安装（`Packages/com.qq.weixin.minigame`）
- [ ] 微信开发者工具已安装并登录
- [ ] 首次构建前已完成 [环境搭建](GETTING_STARTED.md)

> 💡 **Switch Platform 耗时提醒**：首次从 Standalone 切到 WebGL 需要重新导入全部资源，耗时 5-15 分钟。之后不需要重复切换。

---

## 🚀 完整构建流程（三步走）

### 概览

```
步骤 ①  构建 YooAsset Bundle       →  Tools/MiniGame/切换到导出模式
步骤 ②  WebGL 编译 + 微信转换      →  微信小游戏/转换小游戏
```

> CDN/Dev Server 根目录已指向 `webgl/`，Bundle 和首包资源无需拷贝到 `minigame/`。

两步执行完毕后，微信开发者工具刷新即可看到最新版本。

---

### 步骤 ①：构建 YooAsset Bundle

**菜单**：`Tools → MiniGame → 切换到导出模式 (Build Bundle + WebGL)`

这一步做两件事：
1. 将 `AssetConfig.PlayMode` 切换为 `WebGL` 模式
2. 使用 YooAsset SBP（Scriptable Build Pipeline）构建 AssetBundle

**操作细节**：

1. 点击菜单后会弹出确认对话框，点击"开始"
2. 等待 Bundle 构建完成（首次约 2-5 分钟，增量更快）
3. 构建成功后 Console 会输出绿色日志：
   ```
   [BuildModeSwitch] ✅ 导出模式就绪！可以执行微信小游戏导出了。
   ```
4. 会弹出对话框询问是否立即拷贝 StreamingAssets——**先选"稍后手动执行"**，因为还没做微信转换

**构建产物位置**：
```
UnityProj/Bundles/WebGL/DefaultPackage/<版本号>/
UnityProj/Assets/StreamingAssets/yoo/DefaultPackage/   ← Bundle 的 Buildin 拷贝
```

> ⚠️ **如果构建失败并报 ErrorCode115**：说明上次构建的残留目录冲突。脚本已内建自动清理逻辑，正常情况不会出现。如果仍有问题，手动删除 `Bundles/WebGL/DefaultPackage/<冲突版本号>/` 目录后重试。

---

### 步骤 ②：WebGL 编译 + 微信转换

**菜单**：`微信小游戏 → 转换小游戏`

这一步打开微信小游戏转换工具面板。面板里点击**导出按钮**会自动完成：
1. **编译 C# → IL2CPP → WASM**（这就是"Build WebGL"的本质）
2. **将 WebGL 产物转换为微信小游戏格式**

**操作细节**：

1. 打开面板后，确认以下配置正确：

   | 配置项 | 建议值 | 说明 |
   |--------|--------|------|
   | 游戏 AppID | 你的小游戏 AppID | 从 [微信公众平台](https://mp.weixin.qq.com) 获取 |
   | 导出路径 | 项目外的独立目录 | 如 `C:/output`，避免中文和空格 |
   | DevelopBuild | ✅ 开启 | 调试阶段方便看错误信息 |

2. 点击面板底部的**导出按钮**
3. 等待编译完成（首次 10-30 分钟，增量 3-10 分钟）
4. 完成后导出目录结构：
   ```
   <导出路径>/
   ├── webgl/                    ← WebGL 原始构建产物 + CDN/Dev Server 根目录
   │   ├── Build/                ← WASM + framework JS
   │   └── StreamingAssets/      ← YooAsset Bundle 文件
   ├── minigame/                 ← 微信小游戏格式（微信开发者工具打开这个）
   │   ├── game.js               ← 游戏入口
   │   ├── game.json
   │   ├── project.config.json
   │   ├── webgl.wasm.framework.unityweb.js
   │   └── wasmcode/             ← WASM 二进制分包
   └── ...
   ```

> ⚠️ **关键理解**：你**不需要**去 `File → Build Settings → Build` 手动点 Build 按钮。微信转换工具的导出流程已经**内含了 WebGL Build**（`WXConvertCore.DoExport(buildWebGL: true)`）。

> ℹ️ **CDN 指向 webgl/**：CDN/Dev Server 根目录直接服务 `webgl/` 目录，Bundle 和首包资源无需拷贝到 `minigame/`。



---

## ✅ 验证构建是否生效

构建三步走完后，使用以下方法验证改动已正确到位：

### 方法一：检查时间戳（推荐）

在 PowerShell 中执行：

```powershell
# 1. 检查 WASM 文件时间（应晚于你最后一次改代码的时间）
Get-ChildItem '<导出路径>\minigame\wasmcode' -File | Select Name, LastWriteTime

# 2. 检查 StreamingAssets（在 webgl/ 目录下，CDN 直接服务）
Test-Path '<导出路径>\webgl\StreamingAssets'
(Get-ChildItem '<导出路径>\webgl\StreamingAssets' -Recurse -File).Count
```

**验证标准**：
- WASM 文件的 `LastWriteTime` 必须**晚于**你修改 C# 代码的时间
- `StreamingAssets` 目录必须存在，文件数应与 `webgl/StreamingAssets` 一致

### 方法二：微信开发者工具 Console

在微信开发者工具中刷新后，检查 Console：
- ✅ 正常：看到 `[Bootstrapper] All systems initialized.` 
- ❌ 异常：`MissingMethodException`、`memory access out of bounds` 等

---

## 🔄 增量构建（日常开发）

并非每次修改都需要走完整三步。根据你修改的内容选择最小操作集：

| 你修改了什么 | 需要执行的步骤 |
|-------------|--------------|
| **C# 代码**（游戏逻辑、框架代码） | ② + ③ |
| **资源文件**（图片、音频、Prefab 等被 YooAsset 管理的） | ① + ② + ③ |
| **配置表**（Luban xlsx） | 先 `gen_config.bat` → ① + ② + ③ |
| **FairyGUI UI**（fui 包） | 先在 FairyGUI Editor 发布 → ① + ② + ③ |
| **只改了微信层 JS**（`game.js` 等） | 直接在微信开发者工具刷新 |
| **link.xml / PlayerSettings** | ② + ③ |

> 💡 **简化记忆**：改了 C# 就走②③，改了资源就走①②③。

---

## 🔧 常见问题

### Q1：`MissingMethodException: Default constructor not found for type XXX`

**原因**：IL2CPP 代码剥离（Code Stripping）把某些通过反射调用的类型构造函数给剥掉了。

**解决方案**：在 `Assets/link.xml` 中添加保护：
```xml
<linker>
    <assembly fullname="YourAssembly">
        <type fullname="Namespace.ClassName" preserve="all"/>
    </assembly>
</linker>
```

项目已在 `Assets/link.xml` 中保护了 YooAsset 相关类型。如果其他第三方库出现同样问题，按相同方式添加保护后重新执行步骤②。

### Q2：`RuntimeError: memory access out of bounds`

**原因**：通常是其他异常的连锁反应（如上面的 `MissingMethodException`）。先解决根本错误，这个一般会跟着消失。

### Q3：资源加载失败 / 404

**原因**：CDN/Dev Server 根目录未正确指向 `webgl/`，或 Bundle 未构建。

**解决方案**：确认 Dev Server 或 CDN 根目录指向 `<导出路径>/webgl/`，并确保步骤①已构建 Bundle。

### Q4：Bundle 构建失败，报 ErrorCode115

**原因**：YooAsset 检测到相同版本号的输出目录已存在。

**解决方案**：脚本已内建自动清理。如果仍有问题，手动删除 `Bundles/WebGL/DefaultPackage/` 下的冲突版本目录。

### Q5：构建后切回编辑器模式开发

使用 `Tools → MiniGame → 切换到编辑器模式 (EditorSimulate)` 切回，不需要 Bundle 即可在编辑器中运行。

---

## 📊 菜单速查表

| 菜单路径 | 功能 | 快捷场景 |
|---------|------|---------|
| `Tools/MiniGame/切换到导出模式 (Build Bundle + WebGL)` | 构建 YooAsset Bundle + 切 PlayMode | 资源变更后 |
| `Tools/MiniGame/切换到编辑器模式 (EditorSimulate)` | 切回编辑器模式 | 日常开发 |
| `微信小游戏/转换小游戏` | 打开微信转换面板（含 WebGL Build） | 编译 WASM + 转小游戏 |

---

## 🔗 相关文档

- [环境搭建与首次运行](GETTING_STARTED.md) — 从零开始搭建开发环境
- [常见问题与排错](FAQ.md) — 更多故障排查方案
- [Agent 微信集成文档](../Agent/WECHAT_INTEGRATION.md) — CDN、Dev Server、域名白名单、云开发与真机约束

---

_文档路径：`Docs/Guide/BUILD_MINIGAME.md`_
