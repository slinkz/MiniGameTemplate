# 编码规范

> **适用范围**: 本规范同时约束人类开发者和 AI Agent。标记为 `[AGENT]` 的条目对 Agent 自动生成代码尤为关键。

---

## 命名规范

### 文件与类
| 类型 | 规则 | 示例 |
|------|------|------|
| C# 类 | PascalCase | `PlayerHealth`, `GameEvent` |
| C# 接口 | I + PascalCase | `ISaveSystem`, `IWeChatBridge` |
| MonoBehaviour | PascalCase，名称描述唯一职责 | `ScoreDisplay`（不是 `ScoreDisplayAndSFXAndAnimation`）|
| ScriptableObject | PascalCase + 后缀 SO/Variable/Event | `FloatVariable`, `AudioClipSO`, `GameEvent` |
| SO 资产文件 | PascalCase，描述用途 | `PlayerScore.asset`, `OnGameOver.asset` |
| 枚举 | PascalCase，值也是 PascalCase | `enum GameState { Menu, Playing, GameOver }` |

### 变量
| 类型 | 规则 | 示例 |
|------|------|------|
| private 字段 | _camelCase | `_playerHealth`, `_isActive` |
| [SerializeField] | _camelCase | `[SerializeField] private IntVariable _score` |
| public 属性 | PascalCase | `public float Value { get; }` |
| 局部变量 | camelCase | `var currentScore = _score.Value` |
| 常量 | UPPER_SNAKE_CASE | `const int MAX_POOL_SIZE = 100` |
| static readonly | PascalCase 或 UPPER_SNAKE_CASE | `static readonly Vector3 DefaultSpawn = Vector3.zero` |

### 命名空间
```
MiniGameTemplate.Core       // GameLifecycle
MiniGameTemplate.Events     // EventSystem
MiniGameTemplate.Data       // DataSystem
MiniGameTemplate.UI         // UISystem
MiniGameTemplate.Audio      // AudioSystem
MiniGameTemplate.Pool       // ObjectPool
MiniGameTemplate.FSM        // FSM
MiniGameTemplate.Timing     // Timer
MiniGameTemplate.Platform   // WeChatBridge
MiniGameTemplate.Debug      // DebugTools
MiniGameTemplate.Utils      // Utils
MiniGameTemplate.EditorTools // Editor
MiniGameTemplate.Game       // _Game（游戏逻辑）
```

## 代码风格

### 文件结构
```csharp
using UnityEngine;
using MiniGameTemplate.Events;
using MiniGameTemplate.Data;

namespace MiniGameTemplate.Game
{
    /// <summary>
    /// 一句话描述这个类的唯一职责。
    /// </summary>
    public class MyComponent : MonoBehaviour
    {
        #region SO References
        [SerializeField] private IntVariable _score;
        [SerializeField] private GameEvent _onScoreChanged;
        #endregion

        #region Unity Lifecycle
        private void OnEnable() { /* Subscribe */ }
        private void OnDisable() { /* Unsubscribe */ }
        #endregion

        #region Public API
        public void AddScore(int amount) { }
        #endregion

        #region Private Methods
        private void UpdateDisplay() { }
        #endregion
    }
}
```

### 行数限制
- 每个 MonoBehaviour **不超过 150 行**
- 如果超过，拆分成多个单一职责组件
- 运行 `Tools → MiniGame Template → Validate Architecture` 检查

### [AGENT] XML 文档注释
- 所有 `public` / `protected` 方法、属性、类必须带 `<summary>` 注释
- 注释用英文撰写，一句话说明 **做什么**，不是 **怎么做**
- `[SerializeField]` 字段如果用途不直观，加 `[Tooltip("...")]`

## 🚫 禁止事项

| 禁止 | 替代方案 |
|------|---------|
| `GameObject.Find()` | 使用 SO RuntimeSet 或 Inspector 直接引用 |
| `FindObjectOfType()` | 使用 SO RuntimeSet 的 `GetFirst()` |
| 游戏逻辑中的 Singleton | SO 事件/变量通信 |
| `GetComponent<>()` 跨系统引用 | SO 事件通道 |
| 魔法字符串 | `const` 或 SO 引用 |
| `Update()` 中的轮询逻辑 | SO Variable 的 `OnValueChanged` 事件 |
| `DontDestroyOnLoad`（Bootstrapper 外） | Singleton<T> 基类（仅框架内部） |
| `Resources.Load()` 直接加载 | AssetService 或 UIPackageLoader |
| `UnityEngine.Debug.Log/LogWarning` | `GameLog.Log` / `GameLog.LogWarning`（见日志规范） |
| `new PlayerPrefsSaveSystem()` | `GameBootstrapper.SaveSystem`（全局唯一实例） |
| `async void`（Unity 事件除外） | `async Task` 或 `async UniTask`（见异步规范） |
| `Thread` / `Task.Run` | WebGL 单线程，禁止多线程（见 WebGL 约束） |

## 目录规范

- 框架代码 → `UnityProj/Assets/_Framework/<模块名>/`
- 游戏代码 → `UnityProj/Assets/_Game/`
- 示例代码 → `UnityProj/Assets/_Example/`
- SO 资产 → 所属模块的 `Presets/` 或 `UnityProj/Assets/_Game/ScriptableObjects/`
- Editor 脚本 → `UnityProj/Assets/_Framework/Editor/`
- FairyGUI 工程 → `UIProject/`
- FairyGUI 导出 → `UnityProj/Assets/_Game/FairyGUI_Export/`
- 配置表源数据 → `UnityProj/DataTables/`
- 构建/生成脚本 → `UnityProj/Tools/`

## 路径规范

### 原则：优先使用相对路径

项目中**禁止硬编码系统绝对路径**（如 `C:\Users\...`、`/home/user/...`）。所有路径应通过以下方式获取：

| 场景 | 做法 |
|------|------|
| `.bat` 脚本 | `%~dp0` 取脚本所在目录，再用 `..` 做相对导航 |
| `.sh` 脚本 | `SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"`，再用 `../` 相对导航 |
| Unity C# 运行时 | `Application.streamingAssetsPath`、`Application.persistentDataPath` 等 API 动态获取 |
| Unity C# 编辑器 | `Application.dataPath`（指向 `Assets/`），通过 `Path.Combine` 向上导航到仓库根 |
| FairyGUI 发布路径 | Publish.json 中使用相对路径 `../UnityProj/Assets/_Game/FairyGUI_Export` |
| YooAsset 资源路径 | Unity 工程内的 `Assets/...` 形式路径（YooAsset 需要此前缀）|

### Unity 工程内的 `Assets/` 路径（非系统绝对路径）

以下路径是 **Unity AssetDatabase 约定的相对路径**，以 `Assets/` 开头。它们不是系统绝对路径，但属于硬编码字符串——如果 Unity 工程内的目录结构变化，需要同步修改：

| 文件 | 路径 | 说明 |
|------|------|------|
| `UIPackageLoader.cs` | `Assets/FairyGUI_Export/` | FairyGUI 包的 YooAsset 加载基路径 |
| `ConfigManager.cs` | `Assets/_Game/ConfigData/` | Luban 配置二进制数据的 YooAsset 加载基路径（`.bytes` 文件） |
| `SOCreationWizard.cs` | `Assets/_Game/ScriptableObjects` | SO 创建向导的默认保存路径（可在 Inspector 修改）|
| `ArchitectureValidator.cs` | `Assets`（`Directory.GetFiles` 起始目录） | 架构验证扫描范围 |

这些路径通过 `public static` 字段暴露，可在运行时覆盖，无需修改源码。

### 跨目录引用

仓库采用三区分离结构（`Docs/`、`UIProject/`、`UnityProj/`）。从 Unity 工程内引用仓库根目录的文件时，需要向上导航：

```csharp
// 从 Unity 编辑器代码引用仓库根的 Docs/ 目录
var docsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs"));
```

## Git 规范

- 提交前运行架构验证
- SO 资产的 `.asset` 文件正常提交（Force Text 模式，Git diff 友好）
- 大文件（图片、音频、模型）通过 Git LFS 追踪（已在 `.gitattributes` 中配置）

---

## [AGENT] 日志规范

### 规则：只用 `GameLog`，不用 `Debug.Log`

| 场景 | 使用 | 原因 |
|------|------|------|
| 常规调试信息 | `GameLog.Log(...)` | `[Conditional]` 编译，release 构建自动剥离（含字符串插值） |
| 可恢复的警告 | `GameLog.LogWarning(...)` | 同上 |
| **框架初始化失败等致命错误** | `Debug.LogError(...)` 或 `Debug.LogException(ex)` | 致命错误必须在 release 中可见 |

### [AGENT] 禁止的写法
```csharp
// ❌ 错误：release 中仍产生字符串分配
UnityEngine.Debug.Log($"[MySystem] value = {expensive.ToString()}");

// ✅ 正确：release 中整行代码被编译器剥离
GameLog.Log($"[MySystem] value = {expensive}");
```

### [AGENT] 日志 Tag 规范
- 格式: `[模块名]` 前缀，如 `[AudioManager]`, `[FSM]`, `[SaveSystem]`
- 安全: **绝不** 在日志中输出 auth code、token、密码、剪贴板内容等敏感数据

---

## [AGENT] 错误处理规范

### 原则

1. **优先防御性编程**：对外部输入（用户数据、网络、文件）做校验，对内部 API 使用 `Debug.Assert`
2. **不要吞掉异常**：捕获异常后必须记录（`Debug.LogException(ex)`），禁止空 catch
3. **快速失败**：检测到不可恢复状态时，打印错误并 return，不要继续执行后续逻辑

### [AGENT] 代码模式

```csharp
// ✅ 正确：检查 null、记录错误、提前 return
public void ProcessItem(ItemData item)
{
    if (item == null)
    {
        GameLog.LogWarning("[Inventory] ProcessItem called with null item.");
        return;
    }
    // ... 业务逻辑
}

// ✅ 正确：async 中的异常处理
private async Task LoadDataAsync()
{
    try
    {
        await AssetService.Instance.InitializeAsync(config);
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
        Debug.LogError("[MySystem] FATAL: Data loading failed.");
        // 切到错误状态或重试
    }
}

// ❌ 错误：空 catch
try { DoSomething(); }
catch { } // 异常被完全吞掉
```

### [AGENT] `SerializeField` 空引用防御
```csharp
// 所有 SerializeField 引用在使用前检查 null
private void Start()
{
    if (_requiredReference == null)
    {
        Debug.LogError($"[{GetType().Name}] Missing required reference: {nameof(_requiredReference)}", this);
        enabled = false; // 禁用组件，避免后续 NRE
        return;
    }
}
```

---

## [AGENT] 异步编程规范

### WebGL / 微信小游戏 异步约束

微信小游戏运行在 WebGL 上，**单线程**，以下操作会导致死锁或崩溃：

| 🚫 禁止 | ✅ 替代 |
|---------|--------|
| `Thread` / `Task.Run()` / `ThreadPool` | Coroutine 或 `async/await`（主线程） |
| `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` | `await` |
| `WaitForAsyncComplete()`（YooAsset） | `await handle.Task`（非编辑器环境） |
| `System.IO.File.ReadAllText()` | YooAsset `LoadRawFileAsync` 或 `Resources.Load` |
| `System.Net.Http.HttpClient` | `UnityWebRequest`（协程或 `await`） |

### [AGENT] `async void` 仅限 Unity 回调

```csharp
// ✅ 允许：Unity 生命周期回调
private async void Awake()
{
    try
    {
        await InitializeAsync();
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
    }
}

// ❌ 禁止：普通方法
private async void DoWork() // 异常无法被调用方捕获
{
    await SomethingAsync();
}

// ✅ 正确
private async Task DoWork()
{
    await SomethingAsync();
}
```

### [AGENT] `async void` 必须 try-catch

Unity 生命周期中使用 `async void` 时，**整个方法体必须包裹在 try-catch 中**，因为未捕获的异常不会冒泡，只会静默崩溃。

---

## [AGENT] GC 优化与内存规范

### 原则
微信小游戏受限于移动设备内存和 WebGL 堆大小。GC spike 会直接导致掉帧。

### [AGENT] 热路径禁止事项（Update / OnGUI / 高频回调）

| 🚫 禁止 | ✅ 替代 |
|---------|--------|
| `string` 拼接 / 插值 | 预分配 `char[]` 或 `StringBuilder`（缓存复用） |
| `new List<T>()` / `new Dictionary<T>()` | 字段级预分配，`Clear()` 复用 |
| LINQ（`Where`, `Select`, `ToList`） | `for` / `foreach` 手动过滤 |
| Lambda / 闭包（捕获局部变量） | 静态 lambda 或缓存委托 |
| `Enum.ToString()` / 装箱 | 查表或 `switch` |
| `foreach` on `Dictionary.Values`（产生 Enumerator 装箱） | C# 版本 ≥ 7.3 可用，但热路径优先 `for` |
| `params object[]` | 重载固定参数数量 |

### [AGENT] 预分配模式
```csharp
// ✅ 正确：字段级缓存，Update 中零分配
private readonly List<Enemy> _nearbyBuffer = new List<Enemy>(16);

private void Update()
{
    _nearbyBuffer.Clear();
    FindNearbyEnemies(_nearbyBuffer); // 填充已有 List
    foreach (var enemy in _nearbyBuffer)
        ProcessEnemy(enemy);
}
```

### [AGENT] 对象池使用规范
- 频繁创建/销毁的 GameObject **必须** 使用 `PoolManager`
- 获取: `PoolManager.Instance.Get(definition)`
- 归还: `PoolManager.Instance.Return(definition, gameObject)`
- 场景切换时自动 ReturnAll，不需手动清理

---

## [AGENT] 框架系统使用规范

### 存档系统
```csharp
// ✅ 正确：使用全局唯一实例
var save = GameBootstrapper.SaveSystem;
save.SaveInt("my_key", 42);
save.Save();

// ❌ 禁止：创建新实例（绕过 Bootstrapper 的 FlushIfDirty 逻辑）
var save = new PlayerPrefsSaveSystem(); // 不要这样做
```

### 事件系统
```csharp
// 注册必须与注销配对，放在 OnEnable/OnDisable 中
private void OnEnable()
{
    _onGameOver.RegisterListener(this);
    _score.OnValueChanged += OnScoreChanged;
}

private void OnDisable()
{
    _onGameOver.UnregisterListener(this);
    _score.OnValueChanged -= OnScoreChanged;
}

// ❌ 禁止：在 Start/Awake 中注册但忘记注销
// ❌ 禁止：在 OnDestroy 中注销（可能晚于 SO 销毁，导致 MissingReferenceException）
```

### 定时器系统
```csharp
// 创建定时器
_myTimer = TimerService.Instance.Delay(2f, OnTimerComplete);
_repeatTimer = TimerService.Instance.Repeat(0.5f, OnTick);

// 取消（清理时必须调用）
TimerService.Instance.Cancel(_myTimer);

// ⚠️ 持有 TimerHandle 的组件在 OnDisable 或 OnDestroy 中必须 Cancel
private void OnDisable()
{
    TimerService.Instance.Cancel(_myTimer);
}
```

### UI 系统 (FairyGUI)
```csharp
// 打开面板
UIManager.Instance.OpenPanel<MyPanel>(optionalData);

// 关闭面板
UIManager.Instance.ClosePanel<MyPanel>();

// 包加载（async 优先）
await UIPackageLoader.AddPackageAsync("CommonUI");
// 用完后
UIPackageLoader.RemovePackage("CommonUI");

// ⚠️ WebGL 禁止使用同步 AddPackage（它依赖 Resources.Load，不走 YooAsset）
```

### 资源加载
```csharp
// ✅ 正确：通过 AssetService 异步加载
var handle = AssetService.Instance.LoadAssetAsync<GameObject>(path);
await handle.Task;
var prefab = handle.AssetObject as GameObject;

// ✅ 正确：用完后释放 handle
handle.Release();

// ❌ 禁止：直接 Resources.Load（绕过 YooAsset 管线）
var obj = Resources.Load<GameObject>("Prefabs/Player"); // 不要这样做
```

### 微信桥接
```csharp
// ✅ 正确：通过工厂获取，不关心平台
var wx = WeChatBridgeFactory.Create();
wx.Share("标题", "", "score=100");

// ⚠️ 安全：绝不日志 auth code / token
wx.Login((success, code) =>
{
    if (success)
        SendCodeToBackend(code); // 直接发后端，不要 Log
});
```

---

## [AGENT] WebGL / 微信小游戏专用约束

### 内存
- **总堆上限约 256MB**（WeChat 实际更低），加载大纹理前检查内存余量
- 场景切换后调用 `AssetService.Instance.UnloadUnusedAssetsAsync()`
- 纹理最大 1024px（AssetImportEnforcer 自动限制）

### 渲染
- 使用 Built-in Render Pipeline（非 URP/HDRP）
- 小心 `OnGUI` 在 release 构建中的开销（仅 Debug 工具使用，用 `#if` 守卫）
- Draw Call 预算：尽量 < 50 DC（移动 WebGL）

### 音频
- 短音效强制 Mono（AudioImportEnforcer 自动处理）
- 压缩格式: Vorbis，质量 50%
- 加载方式: CompressedInMemory

### 文件 I/O
- WebGL 无文件系统，**禁止 `System.IO`**
- 持久化只用 `PlayerPrefs`（通过 `ISaveSystem` 接口）
- 配置数据通过 `ConfigManager` 加载二进制 `.bytes`（仅 YooAsset，无 Resources fallback）

---

## [AGENT] 安全编码规范

### 输入验证
```csharp
// ✅ 文件名/路径验证——防止路径穿越
if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\") || fileName.Contains("\0"))
{
    Debug.LogError($"[System] SEC: Invalid file name rejected: '{fileName}'");
    return null;
}

// ✅ 数值范围钳制
health = Mathf.Clamp(health, 0, MAX_HEALTH);
```

### 网络安全
```csharp
// ✅ CDN / API URL 必须 HTTPS（已有 ValidateUrlSecurity 辅助方法）
// ❌ 禁止 HTTP（MITM 攻击风险）

// ✅ UnityWebRequest 加超时
var request = UnityWebRequest.Get(url);
request.timeout = 10; // 秒
```

### 数据完整性
- 所有 PlayerPrefs 存档 **必须** 通过 `ISaveSystem`（自带 HMAC 签名）
- 竞技类数据必须服务端校验，客户端 HMAC 仅防休闲篡改
- `DeleteAll()` 需要二次确认 UI，防误操作

### PII（个人隐私信息）保护
- **绝不** 日志以下内容: OpenId、手机号、身份证号、auth code、token、密码、剪贴板内容
- 微信用户昵称/头像仅在 UI 显示，不写入本地日志
- Debug 构建中如需查看敏感数据，使用 `GameLog`（release 自动剥离）

### 条件编译守卫
```csharp
// 仅在 Editor / Development 构建中存在的代码
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 调试面板、作弊码、测试入口
#endif

// 编辑器专用（如 AssetDatabase 调用）
#if UNITY_EDITOR
    AssetDatabase.Refresh();
#endif
```

---

## [AGENT] 模块依赖规范

### 层级依赖图

```
L0: Utils (GameLog, Singleton, MathUtils, CoroutineRunner)
L1: EventSystem, DataSystem, Timer, AssetSystem
L2: UISystem, AudioSystem, ObjectPool
L3: FSM, WeChatBridge
L4: GameLifecycle (GameBootstrapper, SceneLoader)
L5: DebugTools
──────────────────────────────
L6: Game（_Game/ 目录，可引用 L0-L5）
```

### 规则
- **只能向下依赖**：L2 可引用 L1 和 L0，不可引用 L3+
- **禁止循环依赖**：如果 A 引用 B，B 不可引用 A
- **跨模块通信**：必须通过 SO 事件/变量，不可直接引用对方类
- **Game 层** 可引用框架任意层，但框架层不可引用 Game 层
- 运行 `Tools → MiniGame Template → Validate Architecture` 检查违规

---

## [AGENT] SO 设计模式速查

| 需求 | 使用 | 创建菜单 |
|------|------|---------|
| 跨组件通信（无参） | `GameEvent` | MiniGameTemplate/Events/Game Event |
| 跨组件通信（带参） | `IntGameEvent` / `FloatGameEvent` / `StringGameEvent` | MiniGameTemplate/Events/... |
| 共享数值状态 | `IntVariable` / `FloatVariable` | MiniGameTemplate/Variables/... |
| 共享开关状态 | `BoolVariable` | MiniGameTemplate/Variables/Bool |
| 共享文本状态 | `StringVariable` | MiniGameTemplate/Variables/String |
| 追踪运行时对象集合 | `RuntimeSet<T>` + `RuntimeSetRegistrar` | 自定义 |
| 音频配置 | `AudioClipSO` / `AudioLibrary` | MiniGameTemplate/Audio/... |
| 对象池配置 | `PoolDefinition` | MiniGameTemplate/Pool/Pool Definition |
| FSM 状态/转换 | `State` / `StateTransition` | MiniGameTemplate/FSM/... |
| 场景引用 | `SceneDefinition` | MiniGameTemplate/Scene Definition |
| 全局配置 | `GameConfig` / `AssetConfig` | MiniGameTemplate/... |

### [AGENT] 创建新 SO 类型的检查清单
1. `[CreateAssetMenu]` 指定 menuName（前缀 `MiniGameTemplate/`）和 order
2. 如果有运行时可变状态，在 `OnEnable` 中重置（防止跨 Play Mode 残留）
3. Editor 专用功能用 `#if UNITY_EDITOR` 守卫
4. 更新 `Docs/SO_CATALOG.md` 中的类型目录

---

## [AGENT] 集合迭代安全规范

### 禁止迭代中修改集合
```csharp
// ❌ 错误：foreach 中修改字典
foreach (var panel in _activePanels.Values)
    panel.Close(); // 如果 Close() 内部修改了 _activePanels → InvalidOperationException

// ✅ 正确：先快照再迭代
var snapshot = new List<UIBase>(_activePanels.Values);
_activePanels.Clear();
foreach (var panel in snapshot)
    panel.Close();
```

### List 遍历中删除元素
```csharp
// ✅ 正确：倒序遍历
for (int i = list.Count - 1; i >= 0; i--)
{
    if (ShouldRemove(list[i]))
        list.RemoveAt(i); // O(n) per removal — 少量元素可接受
}

// ✅ 正确（大量删除）：批量标记后 RemoveAll
```

---

## [AGENT] 代码提交检查清单

Agent 在完成代码编写后，提交前必须自检以下项目：

- [ ] **日志**: 全部使用 `GameLog`，无裸 `Debug.Log`（致命错误除外）
- [ ] **null 检查**: 所有 `[SerializeField]` 引用在使用前检查
- [ ] **事件配对**: `OnEnable` 注册 → `OnDisable` 注销，1:1 对应
- [ ] **定时器清理**: 持有 `TimerHandle` 的组件在禁用/销毁时 Cancel
- [ ] **async 安全**: 无 `.Result` / `.Wait()`，`async void` 有 try-catch
- [ ] **GC 安全**: Update/OnGUI 中无字符串拼接、new 集合、LINQ、闭包
- [ ] **WebGL 安全**: 无 `System.IO`、无 `Thread`、无同步等待
- [ ] **安全**: 无 PII 日志、文件名已验证、URL 使用 HTTPS
- [ ] **命名**: 遵循命名规范表
- [ ] **行数**: MonoBehaviour ≤ 150 行
- [ ] **依赖方向**: 不违反层级依赖图
- [ ] **MODULE_README**: 新模块目录包含 `MODULE_README.md`
