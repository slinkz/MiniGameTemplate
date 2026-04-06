# 常见问题与排错指南

> 本文收录了使用 MiniGameTemplate 开发过程中的高频问题、常见报错和微信小游戏平台特有的坑点。如果你的问题不在列表中，欢迎提 Issue。

---

## 目录

- [一、启动与初始化](#一启动与初始化)
- [二、编译与导入错误](#二编译与导入错误)
- [三、FairyGUI / UI 相关](#三fairygui--ui-相关)
- [四、资源管理 / YooAsset](#四资源管理--yooasset)
- [五、微信小游戏构建与发布](#五微信小游戏构建与发布)
- [六、对象池与性能](#六对象池与性能)
- [七、数据存储](#七数据存储)
- [八、架构规范与编辑器工具](#八架构规范与编辑器工具)
- [九、杂项](#九杂项)

---

## 一、启动与初始化

### Q: 启动后控制台报 "FATAL: Initialization failed"

**原因**：`GameBootstrapper.Awake()` 中某个系统初始化时抛出了异常。

**排查步骤**：
1. 查看此错误**上方**的异常堆栈，定位是哪个系统初始化失败
2. 最常见原因：
   - `GameConfig` 或 `AssetConfig` 未在 Inspector 中赋值 → 打开 Boot 场景，选中 GameBootstrapper 对象，检查两个配置字段
   - YooAsset 包未正确安装 → 检查 `Packages/manifest.json` 中是否有 `com.tuyoogame.yooasset`
3. 如果是 `AssetConfig` 为空，Bootstrapper 会直接抛出 FATAL 错误（`AssetConfig is not assigned`）。打开 Boot 场景，选中 GameBootstrapper，确保 Asset Configuration 字段已赋值 `DefaultAssetConfig`。

### Q: 直接打开游戏场景运行就报空引用

**原因**：你没有从 Boot 场景启动。Boot 场景中的 `GameBootstrapper` 负责按正确顺序初始化所有框架系统（AssetService → ConfigManager → TimerService → AudioManager → UIManager → PoolManager）。跳过它，这些系统全是 null。

**解决**：始终从 `Assets/_Framework/GameLifecycle/Scenes/Boot.unity` 启动。可以把 Boot 场景设为 Unity 编辑器的默认 Play 场景（Edit → Project Settings → Editor → Enter Play Mode → 指定 Boot 场景）。

### Q: 控制台报 "No initial scene configured in GameConfig!"

**原因**：`GameConfig` SO 资产的 `InitialScene` 字段为空。

**解决**：在 Project 面板找到你的 GameConfig 资产，把目标游戏场景的 `SceneDefinition` SO 拖进去。

### Q: 控制台报 "Duplicate detected — destroying this instance."

**原因**：Boot 场景被重复加载，导致场景里出现了多个 `GameBootstrapper` 对象。框架会自动销毁重复实例，这条警告一般无害。

> 📝 v0.2.2+ 已修复常见触发场景：当 `GameConfig.InitialScene` 指向 Boot 场景自身时，Bootstrapper 会检测到当前已在目标场景并跳过加载，不再触发此警告。

如果仍然出现，检查：
- 是否有脚本在运行时手动加载了 Boot 场景
- Boot 场景是否被放进了 Additive 加载流程

---

## 二、编译与导入错误

### Q: `The type or namespace name 'FairyGUI' could not be found`

**原因**：FairyGUI SDK 的目录链接未建立。项目通过 Git submodule + 目录链接的方式引入 FairyGUI。

**解决**：
```bash
cd UnityProj
# Windows
Tools\setup_fairygui.bat
# macOS / Linux
bash Tools/setup_fairygui.sh
```

如果脚本报错，手动检查：
1. `ThirdParty/FairyGUI-unity/` 目录是否为空 → 运行 `git submodule update --init --recursive`
2. `Assets/FairyGUI/Scripts` 是否是一个目录链接 → 如果不是，脚本创建链接时可能权限不足（Windows 需要管理员权限或开启开发者模式）

### Q: `The type or namespace name 'YooAsset' could not be found`

**原因**：YooAsset 本地包引用路径无效，或 `ThirdParty/YooAsset/` 目录不存在。

**排查步骤**：
1. 确认 `UnityProj/ThirdParty/YooAsset/` 目录存在且包含 `package.json` 文件
2. 打开 `Packages/manifest.json`，确认包含如下本地引用：
   ```json
   "com.tuyoogame.yooasset": "file:../ThirdParty/YooAsset"
   ```
3. 如果 `ThirdParty/YooAsset/` 为空或不存在，说明 Git clone 时可能遗漏了文件，尝试重新 clone 或从备份恢复
4. 在 Unity 中执行 Window → Package Manager → 刷新，确认 YooAsset 2.3.18 已识别

### Q: 导入纹理后发现尺寸被自动改了

**这是预期行为**。`TextureImportEnforcer`（自动运行的 AssetPostprocessor）会强制执行以下规则：

| 规则 | 行为 |
|------|------|
| 纹理超过 1024px | 自动缩小到 1024（小游戏预算） |
| 文件名以 `_N` 结尾 | 自动设为 NormalMap 类型 |
| 位于 `/UI/` 或 `FairyGUI_Export/` 路径 | 自动关闭 mipmaps |
| Read/Write 开启 | 自动关闭（节省 2x 内存） |
| WebGL/Android/iOS 平台 | 自动设置 ASTC 压缩 |

如果你确实需要更大的纹理，修改 `Assets/_Framework/Editor/AssetImportEnforcer.cs` 中的 `MAX_TEXTURE_SIZE` 常量。

### Q: 音频文件被自动转为 Mono

**这是预期行为**。`AudioImportEnforcer` 对短于 3 秒的音效文件自动转为单声道，节省内存。长音频（BGM）不受影响。

WebGL 平台音频还会被自动设置为：CompressedInMemory / Vorbis / 50% 质量。

---

## 三、FairyGUI / UI 相关

### Q: 控制台报 "[UIBase] Failed to create: XXX/YYY"

**原因**：FairyGUI 包名或组件名不匹配。

**排查步骤**：
1. 打开 `UIConstants.cs`，检查你使用的包名和组件名
2. 用 FairyGUI Editor 打开 `UIProject/MiniGameTemplate.fairy`，确认包名和组件名与代码一致
3. 确认 FairyGUI 已发布（Publish）到 `UnityProj/Assets/_Game/FairyGUI_Export/`
4. 注意大小写——FairyGUI 的包名和组件名是大小写敏感的

### Q: WebGL 下 UI 加载卡死（浏览器无响应）

**原因**：你可能在 WebGL 上使用了 `UIBase.Open()`（同步方法）。同步路径调用 `Resources.Load`，而 WebGL 不支持同步文件 I/O。

**解决**：在 WebGL / 微信小游戏平台，**必须使用 `OpenAsync()`**：
```csharp
// ❌ 错误 — WebGL 下可能死锁
UIManager.Instance.OpenPanel<MyPanel>();

// ✅ 正确
await UIManager.Instance.OpenPanelAsync<MyPanel>();
```

另外，确保 FairyGUI 包资源已通过 `UIPackageLoader.PreCachePackageAssetsAsync()` 预加载，否则内部的同步回调会因为缓存未命中而卡住。

### Q: "[UIPackageLoader] FairyGUI asset not pre-cached" 警告

**原因**：FairyGUI 在加载包时会通过同步回调请求纹理等资源。如果你用的是 YooAsset 加载路径，这些资源需要提前缓存。

**解决**：在打开 UI 面板之前，调用预缓存：
```csharp
await UIPackageLoader.PreCachePackageAssetsAsync("MyPackage", new[] {
    "Assets/FairyGUI_Export/MyPackage/MyPackage_atlas0.png",
    // ... 其他资源路径
});
await UIPackageLoader.AddPackageAsync("MyPackage");
```

### Q: FairyGUI 设计分辨率应该用多少？

项目预设为 **720×1280**（竖屏小游戏）。可以在 FairyGUI Editor 的 Project Settings 中修改。改了之后，Unity 侧的 `GRoot.inst` 会自动适配。

---

## 四、资源管理 / YooAsset

### Q: "[AssetService] Not initialized! Call InitializeAsync() first."

**原因**：在 AssetService 初始化完成之前就尝试加载资源。通常是因为没从 Boot 场景启动。

**解决**：确保从 Boot 场景启动，并等待 `GameBootstrapper` 初始化完成后再加载资源。

### Q: "[AssetService] Failed to initialize" 错误

**可能原因**：
- **EditorSimulate 模式**：YooAsset 的模拟构建文件损坏。尝试删除 `Library/ScriptAssemblies` 和 `Bundles/` 目录后重新打开 Unity
- **Offline 模式**：`StreamingAssets` 中没有预构建的 Bundle 文件
- **Host 模式**：CDN 地址不可达。检查 `AssetConfig` 中的 `HostServerUrl`
- **WebGL 模式**：未导入 WechatFileSystem 扩展（见下方构建章节）

### Q: "[AssetService] SEC: HostServerUrl uses HTTP (insecure)."

**原因**：CDN 地址使用了 HTTP 而非 HTTPS。

- **编辑器中**：这只是警告，本地测试可以忽略
- **正式构建中**：这是错误，**必须改为 HTTPS**。HTTP 会导致中间人攻击风险，微信小游戏平台也强制要求 HTTPS

**解决**：在 `AssetConfig` SO 资产中把 URL 改为 `https://`。

### Q: 微信小游戏下 Bundle 加载失败

**检查清单**：
- [ ] Bundle 文件名是否包含中文？→ **微信不支持中文文件名**，改用英文命名
- [ ] 是否把资源放在了 StreamingAssets？→ **WebGL 不支持同步文件访问**，必须用 YooAsset 异步加载
- [ ] 是否所有加载调用都是异步的？→ 同步加载在 WebGL 上不可用
- [ ] 是否导入了 `WX-WASM-SDK-V2` 和 YooAsset 的 `WechatFileSystem` 扩展？

---

## 五、微信小游戏构建与发布

### Q: 构建前需要检查哪些设置？

运行 `Tools → MiniGame Template → Build → Validate WeChat Settings`，工具会自动检查：

| 检查项 | 要求 |
|--------|------|
| Color Space | 必须是 **Gamma**（微信不支持 Linear） |
| Build Target | 必须是 **WebGL** |
| Incremental GC | 必须**开启**（减少 GC 卡顿） |

### Q: 构建后微信开发者工具打开黑屏 / 加载失败

**排查步骤**：

1. **检查 ColorSpace**：打开 Edit → Project Settings → Player → Other Settings → Color Space，确认是 **Gamma**。如果是 Linear，微信小游戏会渲染异常。

2. **检查压缩格式**：PlayerSettings.WebGL.compressionFormat 应该是 **Disabled**。微信插件自带压缩，Unity 侧再压缩会导致双重压缩。

3. **检查转换步骤**：Unity 构建的 WebGL 输出不能直接在微信中运行，需要用微信小游戏 Unity 插件转换：
   ```bash
   minigame-unity-sdk-cli convert --input <build-path> --output <wx-project-path>
   ```

4. **检查 Build Settings**：Boot 场景必须在 Build Settings 列表中，且排在第一位。

### Q: 一键构建做了哪些事？

`Tools → MiniGame Template → Build → Build WebGL` 会自动：

1. 切换平台到 WebGL
2. 配置 PlayerSettings（详见下表）
3. 运行架构验证（不阻止构建）
4. 执行构建
5. 提示下一步的微信转换命令

**自动配置的 PlayerSettings**：

| 设置 | Development | Release |
|------|-------------|---------|
| Color Space | Gamma | Gamma |
| WebGL Memory | 256MB | 256MB |
| Linker Target | Wasm | Wasm |
| Compression | Disabled | Disabled |
| Decompression Fallback | Off | Off |
| Name Files As Hashes | On | On |
| Strip Engine Code | On | On |
| Managed Stripping | Medium | High |
| Exception Support | FullWithStacktrace | ExplicitlyThrownExceptionsOnly |
| Incremental GC | On | On |
| IL2CPP Code Gen | OptimizeSpeed | OptimizeSize | *(Unity 2022.3+)* |
| Debug Symbols | External | Off |

### Q: 构建出来的包体太大怎么办？

1. 运行 `Tools → MiniGame Template → Validate → Asset Audit`，检查：
   - 超大纹理（>1024px）
   - 未压缩纹理（RGBA32 → 改用 ASTC）
   - Read/Write 开启的纹理（浪费 2x 内存）
   - Resources/ 中超过 1MB 的文件（应改用 YooAsset）
   - 大 WAV 文件（>500KB → 转 OGG）
2. Release 构建会自动使用 High stripping + OptimizeSize，比 Development 构建小很多
3. 确认 `compressionFormat = Disabled`（微信插件会在转换时自行压缩）

---

## 六、对象池与性能

### Q: "[ObjectPool] Pool for XXX is exhausted (max=N)."

**原因**：对象池已达上限，无法创建更多实例。

**解决**：
1. 增大对应 `PoolDefinition` SO 资产的 `MaxSize`
2. 检查是否有对象未正确归还池中（忘了调用 `Return()`）
3. 考虑是否真的需要这么多同时存在的实例——如果是，增大上限；如果是泄漏，修复归还逻辑

### Q: "[ObjectPool] Trying to return an object not from this pool"

**原因**：你把一个对象归还到了错误的池中，或者这个对象本来就不是从对象池获取的。

**解决**：确保 `Get()` 和 `Return()` 使用相同的 `PoolDefinition`。

### Q: "[ObjectPool] Prefab is null — cannot create instance."

**原因**：`PoolDefinition` SO 资产中的 Prefab 字段为空。

**解决**：在 Inspector 中把预制件拖进去。

---

## 七、数据存储

### Q: 存档数据丢失 / 存档不生效

**排查步骤**：

1. **是否使用了共享 SaveSystem 实例？**
   正确做法是使用 `GameBootstrapper.SaveSystem`：
   ```csharp
   // ✅ 正确
   GameBootstrapper.SaveSystem.SetString("key", "value");
   
   // ❌ 错误 — 创建了新实例，数据不共享，FlushIfDirty 也不会触发
   var save = new PlayerPrefsSaveSystem();
   save.SetString("key", "value");
   ```

2. **数据是否被 Flush？**
   `GameBootstrapper` 会在 `OnApplicationPause` 和 `OnApplicationQuit` 时自动调用 `SaveSystem.FlushIfDirty()`。但如果你创建了独立的 SaveSystem 实例，这个自动 Flush 不会覆盖到它。

3. **微信小游戏特殊情况**：微信可能在切后台时直接杀进程，`OnApplicationQuit` 不一定被调用。框架已经在 `OnApplicationPause` 中处理了 Flush，但如果你有自定义存储逻辑，也要在 pause 时保存。

### Q: PlayerPrefs 的存储限制

`PlayerPrefsSaveSystem` 基于 Unity 的 `PlayerPrefs`：
- WebGL 下数据存储在浏览器的 IndexedDB 中
- 微信小游戏中数据存储在微信本地存储（`wx.setStorage`）
- 单个 key-value 没有严格大小限制，但总容量受平台限制（微信约 10MB）
- **不适合存储大量数据**。如果需要大量数据存储，考虑使用微信的文件系统 API

---

## 八、架构规范与编辑器工具

### Q: Architecture Check 报 "VIOLATION: GameObject.Find"

**原因**：代码中使用了 `GameObject.Find()`，这是项目架构的**硬性禁止项**。

**为什么禁止**：
- `GameObject.Find` 依赖字符串，重命名物体就会出 bug
- 性能差，遍历整个场景
- 与 SO 驱动架构的解耦理念冲突

**替代方案**：
```csharp
// ❌ 禁止
var player = GameObject.Find("Player");

// ✅ 使用 SO 引用
[SerializeField] private TransformRuntimeSet _players;
var player = _players.Items[0];

// ✅ 或者使用 SO 变量
[SerializeField] private FloatVariable _playerHP;
```

### Q: Architecture Check 报 "WARNING: Resources.Load"

**原因**：代码中使用了 `Resources.Load`，框架推荐使用 YooAsset。

**例外情况**：以下文件被列入白名单，允许使用 `Resources.Load` 作为回退：
- `Singleton.cs`
- `GameBootstrapper.cs`
- `UIPackageLoader.cs`（YooAsset 失败时回退）

如果你的代码确实需要 Resources.Load 作为回退，可以在 `ArchitectureValidator.cs` 的白名单中添加文件名。

### Q: Architecture Check 报 "WARNING: file is XXX lines"

**原因**：文件超过 200 行，建议拆分（单一职责原则）。框架推荐每个 MonoBehaviour **不超过 150 行**。

**解决**：把大文件拆分为更小的、职责单一的组件。例如，一个 500 行的 `GameManager` 应该拆成 `ScoreManager`、`LevelManager`、`UIController` 等。

### Q: Architecture Check 报 "WARNING: Missing MODULE_README.md"

**原因**：`_Framework/` 或 `_Game/` 的子目录中缺少 `MODULE_README.md`。

**解决**：为该模块目录添加一个 `MODULE_README.md`，记录模块的用途、核心类和使用方式。这对 AI Agent 和新加入的开发者都很有帮助。

### Q: Asset Audit 工具检查了哪些项？

通过 `Tools → MiniGame Template → Validate → Asset Audit` 运行，检查：

| 检查项 | 级别 | 说明 |
|--------|------|------|
| 纹理 > 1024px | Warning | 小游戏预算限制 |
| Read/Write 开启 | Warning | 浪费 2x 内存 |
| WebGL 纹理未压缩 (RGBA32) | Error | 应使用 ASTC |
| 音频无 WebGL 覆盖设置 | Warning | 可能使用未优化设置 |
| 大 WAV 文件 (>500KB) | Warning | 建议转 OGG |
| Resources/ 中超 1MB 文件 | Warning | 建议用 YooAsset |

---

## 九、杂项

### Q: SO 变量（如 IntVariable）在编辑器中修改后运行时不重置

**这是 ScriptableObject 的特性**。在编辑器中，SO 的运行时修改会持久化。框架中的 Variable 类在 `OnEnable` 时会自动重置为 `InitialValue`。

如果你发现值没有重置，检查：
1. SO 资产的 `InitialValue` 是否设置正确
2. 是否在 Play 模式下手动修改了 `InitialValue`（这会被持久化）

### Q: 计时器不工作 / Timer 报错

确保：
1. `TimerService` 已初始化（从 Boot 场景启动会自动初始化）
2. 场景中有活跃的 `TimerService` Singleton 实例
3. 使用框架 API 创建计时器：
   ```csharp
   var id = TimerService.Instance.StartTimer(5f, () => { /* callback */ });
   // 取消
   TimerService.Instance.StopTimer(id);
   ```

### Q: 如何正确使用 Luban 配置表？

1. 配置表源数据放在 `UnityProj/DataTables/`
2. 运行生成脚本（需要 .NET SDK 8.0+）：
   ```bash
   cd UnityProj
   Tools\gen_config.bat   # Windows
   bash Tools/gen_config.sh  # macOS/Linux
   ```
3. 生成的 C# 代码 → `Assets/_Framework/DataSystem/Scripts/Config/Generated/`
4. 生成的数据文件：
   - `Assets/_Game/ConfigData/*.bytes` — 运行时二进制（YooAsset）
   - `Assets/_Framework/Editor/ConfigPreview/*.json` — 编辑器预览（不打包）
5. 在代码中通过 `ConfigManager` 访问配置：
   ```csharp
   var tables = ConfigManager.Tables;
   var item = tables.TbItem.Get(1001);
   ```

> 💡 项目中提供了 `luban-config` Skill（`.codebuddy/skills/luban-config/`），AI 助手会自动使用它完成配置表操作。

### Q: Luban 生成报错 "table not exported" 或输出为空

**原因**：`DataTables/Defs/tables.xml` 中未定义 `default: true` 的 group。Luban v4.x 要求至少有一个默认 group，否则表不会被导出。

**解决**：确认 `tables.xml` 的 `<group>` 节点中有 `default="true"`：
```xml
<group name="c" default="true"/>
```

### Q: Luban xlsx 表头格式是什么？

本项目的 xlsx 格式约定：
| 行号 | 标记 | 内容 |
|------|------|------|
| 第 1 行 | `##var` | 字段名（英文，如 `id`, `name`, `price`） |
| 第 2 行 | `##type` | 类型（如 `int`, `string`, `float`） |
| 第 3 行 | `##` | 中文注释（如 `道具ID`, `名称`, `价格`） |
| 第 4 行起 | | 实际数据 |

> ⚠️ **常见坑**：`tables.xml` 中引用 xlsx 时，`input` 属性直接写文件名（如 `input="item.xlsx"`），**不需要** `*@` 前缀。

### Q: 微信 SDK 如何接入？

模板只提供桩实现（`WeChatBridgeStub`），编辑器和非微信平台下使用。接入真实 SDK：

1. 导入 **WX-WASM-SDK-V2** (`com.qq.weixin.minigame`)
2. 创建 `WeChatBridgeImpl : IWeChatBridge`，实现所有接口方法
3. 在 `WeChatBridgeFactory` 中注册新实现
4. 详细步骤参见 [Agent/WECHAT_INTEGRATION.md](../Agent/WECHAT_INTEGRATION.md)

### Q: 编辑器菜单在哪？

所有模板功能统一在 `Tools → MiniGame Template` 下：

| 菜单路径 | 功能 |
|----------|------|
| Create → Game Event / Variable / ... | 快速创建 SO 资产 |
| SO Creation Wizard | SO 创建向导窗口（更多选项） |
| Validate → Architecture Check | 架构规范检查 |
| Validate → Asset Audit | 资源审计 |
| Build → Build WebGL (Dev/Release) | 一键构建 |
| Build → Validate WeChat Settings | 检查微信相关设置 |
| Build → Open Build Folder | 打开构建输出目录 |
| Open Docs Folder | 打开文档目录 |

---

## 还有问题？

1. 先搜索 Console 中的错误消息关键词（如 `[Bootstrapper]`、`[AssetService]`、`[UIBase]`）——框架所有日志都有模块前缀
2. 查看对应模块目录下的 `MODULE_README.md`
3. 查看 [框架模块使用手册](FRAMEWORK_MODULES.md) 中的详细 API 说明
4. 如果是微信平台特有问题，查看 [Agent/WECHAT_INTEGRATION.md](../Agent/WECHAT_INTEGRATION.md)
5. 如果你在和 AI 助手协作，项目内置了 AI Skills（`.codebuddy/skills/`）可自动化常见操作（如配置表管理），详见 [环境搭建 → AI Skills 章节](GETTING_STARTED.md#ai-skills-工具链)
