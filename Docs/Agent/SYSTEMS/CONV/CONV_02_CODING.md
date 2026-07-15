---
system: conventions
scope: coding-standards
last_verified: 2026-05-02
depends_on: [CONV_01_NAMING]
related_code: Assets/_Framework/**, Assets/_Game/**
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

### [AGENT] ScriptableObject 引用约束

**铁律：SO 不能引用场景对象**。ScriptableObject 是项目级资产，序列化后场景引用变 null。

```csharp
// ❌ 错误：SO 字段引用 Transform（场景对象）
[SerializeField] private Transform _targetPoint;  // 运行时 = null！

// ✅ 正确：场景引用只放 MonoBehaviour，SO 只引用其他 SO/Prefab
[SerializeField] private EntityConfigSO _entityConfig;  // 项目资产 → 项目资产 ✅
```

**场景引用传递方式**：通过 `Init(Entity owner)` 参数或 `EntitySystemBootstrap` 注入，不通过 SO 序列化。

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

### UI 系统 (FairyGUI Extension)
```csharp
// 打开面板（async only，T 必须是 GComponent + IUIPanel）
await UIManager.Instance.OpenPanelAsync<Common.LoadingPanel>();

// 关闭面板
UIManager.Instance.ClosePanel<Common.LoadingPanel>();

// 注册 Binder（启动时调用一次）
UIManager.RegisterBinder("Common", Common.CommonBinder.BindAll);

// 包加载（async only）
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
