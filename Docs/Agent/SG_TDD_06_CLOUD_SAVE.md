# SG_TDD_06: 微信登录与云存储系统（V3 云端权威）

> 父文档：[SG_TDD_INDEX.md](SG_TDD_INDEX.md)  
> **版本**：v0.6 | **日期**：2026-05-17 | **状态**：✅ 实施完成（编译零错误，真机验证通过）  
> **前置依赖**：SG_TDD_03（V1 本地存储）已落地并验收通过

---

## 0. 设计目标与触发条件

### 0.1 要解决的问题

| 问题 | 影响 | V1 现状 |
|------|------|---------|
| 用户换设备后进度丢失 | 流失 + 差评 | 纯本地 `wx.setStorageSync` |
| 用户清除小游戏数据后进度丢失 | 高价值用户流失 | 无备份 |
| 无法识别用户身份 | 无法做任何用户相关运营 | 无 openid |
| 跨设备同步 | 多设备用户体验差 | 不支持 |

### 0.2 V2 设计原则

1. **零感知升级**：V1 老用户不需要任何操作，首次静默登录自动迁移
2. **离线优先**：网络不可用时游戏照常运行，联网后自动同步
3. **云端权威**（v0.6 改）：启动时拉取云端数据**无条件覆盖**本地；云端为空 = 新玩家/管理员重置。不再做 seed 或 union merge
4. **最小服务端**：使用微信云开发（TCB），零运维
5. **ISaveSystem 接口不变**：上层 `SG_ProgressManager` 代码零修改

### 0.3 实施触发条件

- ~~DAU > 1000~~（原计划，现提升为主线立即实施）
- 当前阶段：V2 与 P4 并行推进

---

## 1. 总体架构

### 1.1 分层架构图

```
┌─────────────────────────────────────────────────────┐
│  SG_ProgressManager（不变）                           │
│    ↓ ISaveSystem                                    │
├─────────────────────────────────────────────────────┤
│  CloudSaveSystem（新增，实现 ISaveSystem）            │
│    ├── 本地层：WxLocalStorage（读缓存 + 离线写缓冲） │
│    ├── 同步层：CloudSyncService（异步上传/下载）      │
│    └── 登录层：WxAuthService（静默登录 + Token 管理） │
├─────────────────────────────────────────────────────┤
│  微信云开发（TCB Cloud Function + Cloud DB）         │
│    ├── login 云函数：code → openid                   │
│    ├── getProgress 云函数：查询进度                   │
│    └── saveProgress 云函数：写入进度                  │
└─────────────────────────────────────────────────────┘
```

### 1.2 数据流概览

```
[通关时]
  SG_ProgressManager.MarkLevelCleared(levelIndex)
    → CloudSaveSystem.SaveString(key, json)
       ├── 1. 立即写入本地 wx.setStorageSync（V1 不变）
       ├── 2. 标记 dirty → 入队同步任务
       └── 3. CloudSyncService.EnqueueUpload()
              → 检查 WxAuthService.IsLoggedIn
                  ├── true → 直接调用 saveProgress 云函数
                  └── false → 先静默登录 → 再上传

[启动时]
  SG_Boot.InitProgress()
    → CloudSaveSystem 构造
       ├── 1. 从本地 Storage 加载（V1 兼容）
       ├── 2. 异步触发静默登录
       └── 3. 登录成功后 → 下拉云端进度 → 无条件覆盖本地（v0.6 改：不再 merge）
```

### 1.3 关键设计决策

| # | 决策 | 选择 | 理由 |
|---|------|------|------|
| D-01 | 服务端技术栈 | 微信云开发（TCB） | 零运维、免备案、天然 openid 信任链 |
| D-02 | 登录方式 | wx.login 静默登录 | 无需用户授权、无感知 |
| D-03 | 冲突策略 | 云端权威覆盖（v0.6 改） | 云端为唯一源头；管理员可在控制台删档重置；不再 seed 或 merge |
| D-04 | 同步时机 | 写后异步 + 启动时拉取 | 平衡实时性和性能 |
| D-05 | 离线处理 | 本地队列 + 指数退避重试 | 网络恢复后自动同步 |
| D-06 | ISaveSystem 替换 | 新建 CloudSaveSystem 实现 | 不改 V1 接口，透明升级 |

---

## 2. 微信登录模块（WxAuthService）

### 2.1 登录流程时序

> (v0.2 修正：去掉多余的 wx.login 步骤。微信云开发 `callFunction` 天然注入 OPENID，无需 code 换取。)

```
Client (Unity)                                          Cloud Function
     │                                                      │
     │── wx.cloud.callFunction("login", {}) ──────────────►│
     │                                                      │── cloud.getWXContext() → OPENID
     │                                                      │── 返回 { openid, expireIn }
     │◄── { openid, token, expireIn } ────────────────────│
     │                                                      │
     │── 存储 openid+token 到内存（不持久化）               │
```

**关键洞察**：微信云开发环境下 `callFunction` 自动携带调用者身份信息，服务端通过 `cloud.getWXContext().OPENID` 即可获取可信 openid。无需先调 `wx.login()` 再用 code 换取。这比传统服务器模式少一次网络往返。

### 2.2 桥接架构说明（v0.2 新增，回应 CS-001）

> **设计决策**：V2 Login / Cloud 功能直接扩展现有 `WeChatBridgeWebGL` 类，复用已有的 CallbackHost 回调机制。不新建独立的 `WxLoginBridge` / `WxCloudBridge` 类。

**回调路由完整链路**：

```
jslib (WXBridge_CallCloudFunction)
  → SendMessage(unityGameObject, "OnCloudFunctionResult", json)
    → WeChatBridgeWebGLCallbackHost.OnCloudFunctionResult(json)
      → WeChatBridgeWebGL.HandleCloudFunctionResult(json)
        → 通过 requestId 路由到对应的 Action<bool, string> 回调
```

**类职责划分**：

| 类 | 职责 | 变更 |
|---|---|---|
| `WeChatBridgeWebGL` | 桥接层：DllImport + 回调注册/分发 | **新增** Login/CallCloudFunction 方法 + 回调字典 |
| `WeChatBridgeWebGLCallbackHost` | MonoBehaviour 接收 SendMessage | **新增** `OnLoginResult` / `OnCloudFunctionResult` 方法 |
| `WxAuthService` | 业务层：登录状态机 + token 管理 | 新增类，通过 `IWeChatBridge.Login()` 触发 |
| `CloudSyncService` | 业务层：同步队列 + merge | 新增类，通过 `IWeChatBridge.CallCloudFunction()` 调用 |

**`IWeChatBridge` 接口扩展**（V2 新增）：

```csharp
// IWeChatBridge.cs 新增方法
/// <summary>调用微信云函数。</summary>
void CallCloudFunction(string functionName, string dataJson, Action<bool, string> onComplete);
```

**Stub 实现（v0.4 新增，回应 UA-003）**：

```csharp
// WeChatBridgeStub.cs 新增
public void CallCloudFunction(string functionName, string dataJson, Action<bool, string> onComplete)
{
    // Editor / 非微信环境：直接返回失败，不触发任何网络操作
    onComplete?.Invoke(false, "stub: not in wechat environment");
}
```

> **`IWeChatBridge.Login()` 语义澄清（v0.4，回应 UA-003）**：
> - 现有 `Login(Action<bool, string>)` 方法**保留不废弃**，其他模块（如未来的好友排行榜）仍可直接使用。
> - V2 云存储模块通过 `CallCloudFunction("login", ...)` 实现登录，这是**另一条路径**（云开发专用）。
> - 两者并存不冲突：前者走传统 `wx.login → code2session`，后者走云开发 `callFunction` 自动注入 openid。

**回调分发机制**（字典路由，而非全局单回调）：

```csharp
// WeChatBridgeWebGL 内部
private int _nextRequestId;
private readonly Dictionary<int, Action<bool, string>> _cloudCallbacks = new(4);

public void CallCloudFunction(string name, string data, Action<bool, string> onComplete)
{
    int reqId = _nextRequestId++;
    _cloudCallbacks[reqId] = onComplete;
    WXBridge_CallCloudFunction(reqId, name, data);  // reqId 透传到 JS 再带回
}

internal void HandleCloudFunctionResult(string json)
{
    var result = JsonUtility.FromJson<CloudFunctionResponse>(json);
    if (_cloudCallbacks.TryGetValue(result.requestId, out var cb))
    {
        _cloudCallbacks.Remove(result.requestId);
        cb?.Invoke(result.success, result.result);
    }
}
```

### 2.3 类设计

```csharp
namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// 微信静默登录服务。
    /// 职责：管理云函数 login → 获取 openid 的完整链路。
    /// 线程安全：WebGL 单线程，无需锁。
    /// (v0.2 修正：不调 wx.login，直接 callFunction 即可获得 openid)
    /// </summary>
    public class WxAuthService
    {
        public enum AuthState { NotLoggedIn, LoggingIn, LoggedIn, Failed }
        
        private AuthState _state = AuthState.NotLoggedIn;
        private string _openId;
        private string _token;
        private float _tokenExpireTime;  // Time.realtimeSinceStartup
        private readonly IWeChatBridge _bridge;
        
        public WxAuthService(IWeChatBridge bridge)
        {
            _bridge = bridge;
        }
        
        /// <summary>当前登录状态</summary>
        public AuthState State => _state;
        
        /// <summary>是否已登录且 token 未过期</summary>
        public bool IsLoggedIn => _state == AuthState.LoggedIn 
                                  && Time.realtimeSinceStartup < _tokenExpireTime;
        
        /// <summary>已登录时返回 openid，否则返回 null</summary>
        public string OpenId => IsLoggedIn ? _openId : null;
        
        /// <summary>
        /// 发起静默登录。可重复调用，内部防重入。
        /// 实现：直接调用 login 云函数，云端自动注入 openid。
        /// </summary>
        public void Login(Action<bool, string> onComplete)
        {
            if (_state == AuthState.LoggingIn)
            {
                _pendingCallbacks.Add(onComplete);
                return;
            }
            
            if (IsLoggedIn)
            {
                onComplete?.Invoke(true, _openId);
                return;
            }
            
            _state = AuthState.LoggingIn;
            _pendingCallbacks.Clear();
            _pendingCallbacks.Add(onComplete);
            
            // 直接调用 login 云函数（云开发自动注入 OPENID，无需 wx.login code）
            _bridge.CallCloudFunction("login", "{}", (success, result) =>
            {
                if (!success)
                {
                    CompleteLogin(false, "cloud function failed");
                    return;
                }
                
                var loginResult = JsonUtility.FromJson<LoginResult>(result);
                _openId = loginResult.openid;
                _token = loginResult.token;
                _tokenExpireTime = Time.realtimeSinceStartup + loginResult.expireIn;
                
                CompleteLogin(true, _openId);
            });
        }
        
        /// <summary>Token 过期时刷新登录</summary>
        public void RefreshIfNeeded(Action<bool> onComplete = null)
        {
            if (IsLoggedIn)
            {
                onComplete?.Invoke(true);
                return;
            }
            Login((success, _) => onComplete?.Invoke(success));
        }
        
        // --- 内部 ---
        
        private readonly List<Action<bool, string>> _pendingCallbacks = new(4);
        
        private void CompleteLogin(bool success, string result)
        {
            _state = success ? AuthState.LoggedIn : AuthState.Failed;
            foreach (var cb in _pendingCallbacks)
                cb?.Invoke(success, result);
            _pendingCallbacks.Clear();
        }
        
        [Serializable]
        private struct LoginResult
        {
            public string openid;
            public string token;
            public int expireIn;  // 秒
        }
    }
}
```

### 2.3 安全约束

| 规则 | 说明 |
|------|------|
| code 一次性使用 | wx.login 返回的 code 5 分钟内有效且只能用一次 |
| session_key 不下发客户端 | 云函数内部使用，永不返回给前端 |
| token 不持久化 | 仅保存在内存中，App 冷启动时重新 login |
| openid 不暴露给用户 | 仅用于服务端标识，客户端不显示 |

### 2.4 失败处理

| 场景 | 处理方式 |
|------|---------|
| wx.login 失败（极罕见） | 标记 Failed，游戏照常运行（纯本地模式） |
| 云函数超时（>5s） | 超时回退本地模式，30s 后自动重试一次 |
| 网络不可用 | 本地模式运行，OnShow 时重试登录 |
| 连续失败 3 次 | 本次会话放弃登录，下次冷启动重试 |

---

## 3. 云存储同步模块（CloudSyncService）

### 3.1 云端数据格式

```json
{
    "_id": "{openid}",
    "version": 2,
    "clearedLevels": [1, 2, 3],
    "lastSyncTime": 1715068800000,
    "clientVersion": "1.0.0"
}
```

> 用 openid 作为文档 _id，天然唯一且无需额外索引。

### 3.2 云函数 API 设计

#### 3.2.1 `login` 云函数

```javascript
// cloudfunctions/login/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();
    
    // 生成简单 token（云开发场景下 openid 本身已通过微信验证）
    // V2 简化：直接返回 openid，不做自定义 token
    return {
        openid: OPENID,
        token: OPENID,  // V2 简化：云开发环境下 openid 即信任凭证
        expireIn: 7200   // 2 小时后建议重新 login
    };
};
```

#### 3.2.2 `getProgress` 云函数

```javascript
// cloudfunctions/getProgress/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();
    
    try {
        const result = await db.collection('progress').doc(OPENID).get();
        return { success: true, data: result.data };
    } catch (e) {
        if (e.errCode === -1) {
            // 文档不存在 = 新用户
            return { success: true, data: null };
        }
        return { success: false, error: e.errMsg };
    }
};
```

#### 3.2.3 `saveProgress` 云函数

```javascript
// cloudfunctions/saveProgress/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();
    const { clearedLevels, version, clientVersion } = event;
    
    // v0.6 改：直接覆盖写入（不再做 union merge）
    // 客户端是唯一写入源，云端直接 set 覆写
    const data = {
        version: version || 2,
        clearedLevels: clearedLevels || [],
        lastSyncTime: Date.now(),
        clientVersion: clientVersion || "unknown"
    };
    
    try {
        await db.collection('progress').doc(OPENID).set({ data });
        return { success: true, data };
    } catch (e) {
        return { success: false, error: e.errMsg };
    }
};
```

### 3.3 共享数据类型（v0.4 新增，回应 UA-001）

```csharp
namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V2 进度数据 DTO（共享类型）。
    /// 供 CloudSyncService（merge）和 SG_ProgressManager（Load/Save）共同使用。
    /// 
    /// v0.4 修正：从 SG_ProgressManager 内部 private class 提升为独立共享类型。
    /// SG_ProgressManager 将改为引用此类型（替换其内部 private ProgressData）。
    /// 
    /// JsonUtility 要求：字段名必须与 JSON key 完全一致。
    /// </summary>
    [Serializable]
    public class SharedProgressData
    {
        public int version = 1;
        public List<int> clearedLevels = new List<int>();
    }
}
```

> **迁移说明**：`SG_ProgressManager` 的内部 `private class ProgressData` 将被替换为引用 `SharedProgressData`。字段完全一致，仅可见性变更。序列化格式不变。

### 3.4 数据流形态表（v0.4 新增，回应 UA-002）

回调链中每一层的 JSON 结构：

| 层 | 变量 | JSON 形态 | 说明 |
|----|------|-----------|------|
| 云函数 `getProgress` return | `res.result` | `{ "success": true, "data": { "version": 2, "clearedLevels": [1,2,3], ... } }` | 云函数返回值 |
| jslib `success` 回调 | `JSON.stringify(res.result)` | `'{"success":true,"data":{"version":2,"clearedLevels":[1,2,3],...}}'` | 字符串化后传给 Unity |
| C# `HandleCloudFunctionResult` | `resp.result` | `'{"success":true,"data":{"version":2,"clearedLevels":[1,2,3],...}}'` | 从外层 CloudFunctionResponse 中提取 |
| `PullAndMerge` 回调 `result` | 同上 | 同上（纯净的云函数返回 JSON） | 需要二次反序列化 |
| 解析后 | `GetProgressResult` | `success=true, data={version=2, clearedLevels=[1,2,3]}` | 结构化对象 |

**关键洞察**：`PullAndMerge` 回调拿到的 `result` 字符串 = `JSON.stringify(云函数return值)`。需要**两步反序列化**：
1. 第一步：`JsonUtility.FromJson<GetProgressResult>(result)` → 拿到 `data` 对象
2. 第二步：直接使用 `data.clearedLevels`（因为 JsonUtility 可以嵌套反序列化）

### 3.5 客户端同步类设计

> (v0.4 修正，回应 UA-004：`CloudSyncService` 命名空间从 `MiniGameTemplate.Data` 改为 `MiniGameTemplate.Platform`，
> 因为它依赖 `IWeChatBridge` 平台能力。`SharedProgressData` DTO 保留在 `MiniGameTemplate.Data`。
> 依赖方向：Platform（含 CloudSyncService/WxAuthService）→ Data（含 SharedProgressData/ISaveSystem）✅)

```csharp
namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// 云端进度同步服务。
    /// 职责：将本地进度异步同步到微信云开发数据库。
    /// V3 设计：启动时云端权威覆盖本地 + 通关后异步上传。不做 merge/seed。
    /// </summary>
    public class CloudSyncService
    {
        private readonly WxAuthService _auth;
        private readonly IWeChatBridge _bridge;  // v0.3 修正：使用接口而非独立类
        
        private bool _isSyncing;
        private bool _hasPendingUpload;
        private int _retryCount;
        private const int MAX_RETRY = 3;
        private const float RETRY_BASE_DELAY = 2f;  // 指数退避基础：2s, 4s, 8s
        
        public enum SyncState { Idle, Syncing, Failed }
        public SyncState State { get; private set; } = SyncState.Idle;
        
        /// <summary>上次成功同步的时间戳（本地 realtimeSinceStartup）</summary>
        public float LastSyncTime { get; private set; } = -1f;
        
        /// <summary>
        /// (v0.3 修正，回应 CS-009：通过 IWeChatBridge 接口调用云函数，不依赖 WxCloudBridge)
        /// </summary>
        public CloudSyncService(WxAuthService auth, IWeChatBridge bridge)
        {
            _auth = auth;
            _bridge = bridge;
        }
        
        /// <summary>
        /// 启动时拉取云端进度。云端权威：无条件覆盖本地。
        /// (v0.6 改：不再 merge，不再 seed)
        /// </summary>
        /// <param name="localData">Unused (kept for API compat). Cloud is authoritative — empty cloud = new player.</param>
        /// <param name="onComplete">(success, cloudJson). Empty string when cloud has no data.</param>
        public void PullAndMerge(string localData, Action<bool, string> onComplete)
        {
            if (!_auth.IsLoggedIn)
            {
                onComplete?.Invoke(false, localData);  // 未登录，返回本地数据
                return;
            }
            
            State = SyncState.Syncing;
            _bridge.CallCloudFunction("getProgress", "{}", (success, result) =>
            {
                if (!success)
                {
                    State = SyncState.Failed;
                    onComplete?.Invoke(false, localData);
                    return;
                }
                
                // v0.4 修正（回应 UA-002）：result = JSON.stringify(云函数返回值)
                // 云函数返回 { success: true, data: { version, clearedLevels, ... } }
                var cloudResult = JsonUtility.FromJson<GetProgressResult>(result);
                if (!cloudResult.success || cloudResult.data == null 
                    || cloudResult.data.clearedLevels == null 
                    || cloudResult.data.clearedLevels.Count == 0)
                {
                    // v0.6 改：云端无数据 = 新玩家/管理员重置。不 seed，直接返回空
                    State = SyncState.Idle;
                    onComplete?.Invoke(true, "");
                    return;
                }
                
                // v0.6 改：云端权威覆盖（不再 merge，直接用云端数据）
                string cloudJson = JsonUtility.ToJson(cloudResult.data);
                State = SyncState.Idle;
                LastSyncTime = Time.realtimeSinceStartup;
                onComplete?.Invoke(true, cloudJson);
            });
        }
        
        /// <summary>
        /// 通关时异步上传进度到云端。
        /// 失败时入队重试，不阻塞游戏。
        /// (v0.2 修正：改为"最新快照"模式，上传时总是读取最新本地数据)
        /// </summary>
        public void EnqueueUpload(string progressJson)
        {
            _latestProgressJson = progressJson;  // 总是保留最新快照
            _hasPendingUpload = true;
            
            if (!_auth.IsLoggedIn)
            {
                // 登录后自动重试
                _auth.Login((success, _) =>
                {
                    if (success) DoUpload();
                });
                return;
            }
            
            DoUpload();
        }
        
        private string _latestProgressJson;  // 最新待上传快照（v0.2 新增）
        
        private void DoUpload()
        {
            if (_isSyncing) return;  // 正在上传中，等当前完成后会自动重试
            _isSyncing = true;
            State = SyncState.Syncing;
            
            string dataToUpload = _latestProgressJson;  // 取最新快照
            
            _bridge.CallCloudFunction("saveProgress", dataToUpload, (success, result) =>
            {
                _isSyncing = false;
                
                if (success)
                {
                    _retryCount = 0;
                    State = SyncState.Idle;
                    LastSyncTime = Time.realtimeSinceStartup;
                    
                    // v0.2 修正：检查上传期间是否有新数据产生
                    if (_latestProgressJson != dataToUpload)
                    {
                        // 有新数据，再上传一次
                        DoUpload();
                    }
                    else
                    {
                        _hasPendingUpload = false;
                    }
                }
                else
                {
                    _retryCount++;
                    if (_retryCount < MAX_RETRY)
                    {
                        float delay = RETRY_BASE_DELAY * Mathf.Pow(2, _retryCount - 1);
                        TimerService.Instance.Delay(delay, () => DoUpload(), true);
                    }
                    else
                    {
                        State = SyncState.Failed;
                        Debug.LogWarning("[CloudSync] 上传失败次数超限，本次会话放弃同步");
                    }
                }
            });
        }
        
        /// <summary>
        /// [V3 已弃用] Union merge：取两份 clearedLevels 的并集。
        /// V3 改为云端权威覆盖，此方法不再被调用，保留仅供参考。
        /// </summary>
        private static string MergeProgress(string localJson, string cloudJson)
        {
            var localData = JsonUtility.FromJson<SharedProgressData>(localJson);
            var cloudData = JsonUtility.FromJson<SharedProgressData>(cloudJson);
            
            // 并集
            var merged = new HashSet<int>(localData.clearedLevels);
            if (cloudData?.clearedLevels != null)
            {
                foreach (var lv in cloudData.clearedLevels)
                    merged.Add(lv);
            }
            
            localData.clearedLevels = new List<int>(merged);
            localData.clearedLevels.Sort();
            localData.version = 2;  // 升级版本号
            
            return JsonUtility.ToJson(localData);
        }
        
        /// <summary>
        /// getProgress 云函数返回值反序列化类型。
        /// (v0.4 新增，回应 UA-002：明确 JSON 反序列化目标结构)
        /// </summary>
        [Serializable]
        private class GetProgressResult
        {
            public bool success;
            public SharedProgressData data;  // 嵌套对象，JsonUtility 自动递归反序列化
        }
    }
}
```

---

## 4. CloudSaveSystem（ISaveSystem V2 实现）

### 4.1 设计定位

`CloudSaveSystem` 是 `ISaveSystem` 的新实现，内部组合了：
- 本地读写（复用 `PlayerPrefsSaveSystem` 逻辑）
- 云端异步同步（通过 `CloudSyncService`）

**上层 `SG_ProgressManager` 完全不需要修改。**

### 4.2 类设计

```csharp
namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V2 云存储实现。
    /// 策略：写入时立即落本地 + 异步入队上传云端。
    /// 读取时优先本地（毫秒级），启动时一次性 merge 云端。
    /// 
    /// 替换条件：微信小游戏环境 + 云开发已配置。
    /// 非微信环境自动降级为 PlayerPrefsSaveSystem。
    /// </summary>
    public class CloudSaveSystem : ISaveSystem
    {
        private readonly PlayerPrefsSaveSystem _local;  // 本地存储层
        private readonly CloudSyncService _syncService;
        private readonly WxAuthService _authService;
        
        private bool _initialMergeDone;
        
        public CloudSaveSystem(WxAuthService authService, IWeChatBridge bridge)
        {
            _local = new PlayerPrefsSaveSystem();
            _authService = authService;
            _syncService = new CloudSyncService(authService, bridge);
        }
        
        /// <summary>
        /// 启动时调用：异步登录 + 下拉云端 + merge。
        /// 不阻塞——调用后立即返回，merge 在后台完成。
        /// (v0.2 修正：merge 完成后触发 OnCloudMergeCompleted 事件)
        /// </summary>
        public void InitCloudSync()
        {
            _authService.Login((success, openid) =>
            {
                if (!success) return;
                
                // 拉取云端并 merge
                string localProgress = _local.LoadString("sg_progress", "");
                _syncService.PullAndMerge(localProgress, (merged, mergedJson) =>
                {
                    if (merged)
                    {
                        // v0.6 改：无条件覆盖本地（含空值），确保云端删档/重置能传播
                        _local.SaveString("sg_progress", mergedJson);
                        _local.FlushIfDirty();
                        Debug.Log("[CloudSave] 云端进度已覆盖本地");
                    }
                    _initialMergeDone = true;
                    // 通知上层刷新（v0.2 新增）
                    OnCloudMergeCompleted?.Invoke(mergedJson ?? localProgress);
                });
            });
        }
        
        // === Merge 后通知机制（v0.2 新增，回应 CS-002 / CS-007）===
        
        /// <summary>
        /// 云端 merge 完成后触发，UI 层监听此事件刷新显示。
        /// 参数：merge 后的 progress JSON。
        /// </summary>
        public event Action<string> OnCloudMergeCompleted;
        
        /// <summary>
        /// 热启动时调用：重新拉取云端并 merge。
        /// (v0.2 新增，回应 CS-007：补充 Reload 方法)
        /// </summary>
        public void Reload()
        {
            // 重读本地（可能被其他模块修改）
            string localProgress = _local.LoadString(PROGRESS_KEY, "");
            
            if (!_authService.IsLoggedIn)
            {
                // 未登录时只刷新本地数据
                OnCloudMergeCompleted?.Invoke(localProgress);
                return;
            }
            
            _syncService.PullAndMerge(localProgress, (merged, mergedJson) =>
            {
                if (merged && mergedJson != localProgress)
                {
                    _local.SaveString(PROGRESS_KEY, mergedJson);
                    _local.FlushIfDirty();
                }
                _initialMergeDone = true;
                OnCloudMergeCompleted?.Invoke(mergedJson);
            });
        }
        
        // === ISaveSystem 实现（全部委托 _local + 进度键触发云同步）===
        
        private const string PROGRESS_KEY = "sg_progress";
        
        public void SaveString(string key, string value)
        {
            _local.SaveString(key, value);
            
            // 仅进度数据触发云同步（v0.2 说明，回应 CS-003：有意设计）
            // 其他 KV（如音量设置等）不需要上云，只同步 PROGRESS_KEY。
            if (key == PROGRESS_KEY)
            {
                _syncService.EnqueueUpload(value);
            }
        }
        
        public string LoadString(string key, string defaultValue = "")
            => _local.LoadString(key, defaultValue);
        
        // --- 其余方法直接委托（非进度数据不触发云同步，这是有意设计）---
        public void SaveInt(string key, int value) => _local.SaveInt(key, value);
        public int LoadInt(string key, int defaultValue = 0) => _local.LoadInt(key, defaultValue);
        public void SaveFloat(string key, float value) => _local.SaveFloat(key, value);
        public float LoadFloat(string key, float defaultValue = 0f) => _local.LoadFloat(key, defaultValue);
        public void SaveBool(string key, bool value) => _local.SaveBool(key, value);
        public bool LoadBool(string key, bool defaultValue = false) => _local.LoadBool(key, defaultValue);
        public bool HasKey(string key) => _local.HasKey(key);
        public void DeleteKey(string key) => _local.DeleteKey(key);
        public void DeleteAll() => _local.DeleteAll();
        public void Save() => _local.Save();
        public void FlushIfDirty() => _local.FlushIfDirty();
    }
}
```

### 4.3 工厂升级

```csharp
// GameBootstrapper 中的 SaveSystem 创建逻辑变更：
public static ISaveSystem CreateSaveSystem(IWeChatBridge wechatBridge)
{
    // 微信环境 + 非 Editor → 使用 CloudSaveSystem
    if (wechatBridge.IsWeChatPlatform)
    {
        var auth = new WxAuthService(wechatBridge);  // v0.3 修正：传入 bridge
        var cloudSave = new CloudSaveSystem(auth, wechatBridge);
        cloudSave.InitCloudSync();  // 后台异步，不阻塞
        return cloudSave;
    }
    
    // Editor / 非微信 → V1 本地存储
    return new PlayerPrefsSaveSystem();
}
```

---

## 5. JS 桥接层（WeChatBridge.jslib 扩展）

### 5.1 WeChatBridge.jslib 新增函数

> (v0.2 修正：去掉 `WXBridge_InitCloud`，`wx.cloud.init` 只在 game.js 中执行一次。
> C# 侧不重复初始化。`callFunction` 前检查 `wx.cloud` 是否已 ready 即可。)

```javascript
// 追加到现有 WeChatBridge.jslib

WXBridge_CallCloudFunction: function (requestId, namePtr, dataPtr) {
    var state = window.MiniGameTemplateWXBridge;
    if (!state || typeof wx === "undefined" || !wx.cloud) {
        sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({ 
            success: false, requestId: requestId, error: "no wx.cloud" 
        }));
        return;
    }

    var name = UTF8ToString(namePtr);
    var data = UTF8ToString(dataPtr);
    var parsedData = {};
    try { parsedData = JSON.parse(data); } catch (e) {}

    // v0.4 新增（回应 UA-006）：5s 超时保护，防止无响应导致回调永远不触发
    var timeoutId = setTimeout(function () {
        timeoutId = null;
        sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({ 
            success: false, 
            requestId: requestId,
            name: name,
            error: "timeout: 5000ms exceeded" 
        }));
    }, 5000);

    wx.cloud.callFunction({
        name: name,
        data: parsedData,
        success: function (res) {
            if (timeoutId === null) return;  // 已超时，丢弃
            clearTimeout(timeoutId);
            sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({ 
                success: true, 
                requestId: requestId,
                name: name,
                result: JSON.stringify(res.result) 
            }));
        },
        fail: function (err) {
            if (timeoutId === null) return;  // 已超时，丢弃
            clearTimeout(timeoutId);
            sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({ 
                success: false, 
                requestId: requestId,
                name: name,
                error: stringifyError(err) 
            }));
        }
    });
}
```

> **注意**：`requestId` 参数用于客户端回调路由（同一时刻可能有多个并发云函数调用）。

### 5.2 C# 侧 DllImport 新增

```csharp
// WeChatBridgeWebGL.cs 新增（v0.2 精简：去掉 Login 和 InitCloud）

[DllImport("__Internal")]
private static extern void WXBridge_CallCloudFunction(int requestId, string name, string data);
```

### 5.3 回调处理新增

```csharp
// WeChatBridgeWebGLCallbackHost.cs 新增回调方法

public void OnCloudFunctionResult(string jsonResult)
{
    _bridge?.HandleCloudFunctionResult(jsonResult);
}
```

```csharp
// WeChatBridgeWebGL.cs 内部回调处理（v0.2 完整定义）

[Serializable]
private struct CloudFunctionResponse
{
    public bool success;
    public int requestId;
    public string name;
    public string result;
    public string error;
}

internal void HandleCloudFunctionResult(string json)
{
    var resp = JsonUtility.FromJson<CloudFunctionResponse>(json);
    if (_cloudCallbacks.TryGetValue(resp.requestId, out var cb))
    {
        _cloudCallbacks.Remove(resp.requestId);
        cb?.Invoke(resp.success, resp.success ? resp.result : resp.error);
    }
}
```

---

## 6. 微信云开发配置

### 6.1 环境要求

| 项目 | 配置 |
|------|------|
| 云开发环境 | 在小游戏管理后台开通（免费额度够用） |
| 数据库集合 | `progress`（需手动创建） |
| 云函数 | `login` / `getProgress` / `saveProgress`（3 个） |
| 权限规则 | 仅创建者可读写自己的文档 |

### 6.2 数据库权限规则

```json
{
  "progress": {
    ".read": "doc._id == auth.openid",
    ".write": "doc._id == auth.openid"
  }
}
```

### 6.3 小程序端云开发初始化

在微信开发者工具导出的 `game.js` 中（或 `minigame/game.js` 模板）需要：

```javascript
// game.js 顶部
if (typeof wx !== 'undefined' && wx.cloud) {
    wx.cloud.init({
        env: 'your-env-id',   // 替换为实际环境 ID
        traceUser: true
    });
}
```

---

## 7. 数据迁移方案（V1 → V2）

### 7.1 迁移策略

> **v0.6 变更**：删除 seed 机制。微信小游戏从模板诞生就内置云存档，不存在"中途接入"的迁移场景。

| 场景 | 处理 |
|------|------|
| ~~V1 老用户首次联网~~ | ~~上传本地 seed~~ → **V3 不再 seed**。云端为空 = 新玩家，从零开始 |
| 全新用户 | 本地无数据 + 云端无数据 → 正常创建 |
| V2/V3 用户换设备 | 新设备本地无数据 + 云端有数据 → 下拉云端数据覆盖本地 |
| 管理员在控制台删除云端记录 | 下次启动拉取到空数据 → 本地也清空（重新开始） |

### 7.2 版本号约定

| version | 含义 |
|---------|------|
| 1 | V1 纯本地（SG_TDD_03） |
| 2 | V2 云同步 |

> **迁移铁律**：`version` 只升不降。`MigrateData()` 中 version < 2 → 补充默认字段 → 设为 2。

### 7.3 不可逆保护

- 一旦用户进入 V2（version=2），**不回退到 V1**
- 如果云同步失败，本地 version 仍为 2，但游戏照常运行

---

## 8. 启动集成变更

### 8.1 SG_Boot.InitProgress() 升级

```csharp
public static void InitProgress()
{
    if (Progress != null)
    {
        Progress.Reload();  // WX-005: 热启动
        return;
    }
    
    // V2: 使用 CloudSaveSystem
    var saveSystem = GameBootstrapper.SaveSystem;  // 已经是 CloudSaveSystem 实例
    Progress = new SG_ProgressManager(saveSystem);
}
```

**关键点**：`GameBootstrapper` 层面已经把 `SaveSystem` 替换为 `CloudSaveSystem`，`SG_Boot` 不需要任何修改。

### 8.2 启动时序

> (v0.2 修正，回应 CS-002：merge 完成后通过事件通知 ProgressManager 重新加载)

```
GameBootstrapper.Awake()
  └→ CreateSaveSystem(wechatBridge)
       ├── [微信环境] → new CloudSaveSystem(new WxAuthService(bridge))
       │                   └── .InitCloudSync()  ← 后台异步登录+merge
       └── [其他环境] → new PlayerPrefsSaveSystem()
  └→ SG_Boot.InitProgress()  ← 立即可用（读本地缓存）
       └→ 注册 cloudSave.OnCloudMergeCompleted += Progress.Reload
  └→ GameStartupFlow.RunAsync()  ← UI 流程正常启动

// ~~1-3 秒后（后台）~~
CloudSaveSystem.InitCloudSync() 完成
  → 如果云端有更新数据 → 静默 merge 到本地 → 触发 OnCloudMergeCompleted
  → SG_ProgressManager.Reload() → 重新 Load() → UI 自动刷新
```

**SG_ProgressManager 需要新增 `Reload()` 方法**：

```csharp
// SG_ProgressManager.cs 新增（V2）
public void Reload()
{
    Load();  // 重新从 ISaveSystem 读取最新数据
}
```

> 这是唯一需要改动 `SG_ProgressManager` 的地方——新增一个 public Reload()。
> ISaveSystem 接口本身不变，符合 V2-BC-08。

---

## 9. 行为契约

| ID | 契约 | 验证方式 |
|----|------|---------|
| V2-BC-01 | 登录失败不阻塞游戏启动 | 代码路径：Login 回调 false → 游戏正常运行 |
| V2-BC-02 | 本地写入始终同步完成（不等云端） | CloudSaveSystem.SaveString → _local.SaveString 是同步的 |
| V2-BC-03 | 云端上传失败不影响本地存档 | 上传失败仅打 Warning，本地数据已落盘 |
| V2-BC-04 | ~~Merge 策略为 Union~~ → V3 云端权威覆盖 | PullCloudProgress 返回云端原始数据，不做 merge；空 = 新玩家 |
| V2-BC-05 | Token 不持久化到 Storage | WxAuthService._token 仅内存变量 |
| V2-BC-06 | 超时 5s 自动放弃本次云函数调用 | jslib 层 setTimeout 或 WeChatBridgeWebGL 内置超时回调 |
| V2-BC-07 | 指数退避最多重试 3 次 | MAX_RETRY=3, delay=2/4/8s |
| V2-BC-08 | ISaveSystem 接口无变化 | 编译验证：SG_ProgressManager 仅新增 Reload()，ISaveSystem 接口零修改 |
| V2-BC-09 | 非微信环境降级为 V1 | CreateSaveSystem 条件分支 |
| V2-BC-10 | 热启动时重新拉取云端 | CloudSaveSystem.Reload() → PullAndMerge → OnCloudMergeCompleted → ProgressManager.Reload() |

---

## 10. 实施计划

### Phase 总览

| Phase | 内容 | 预估 | 依赖 |
|-------|------|------|------|
| **V2-P0** | JS 桥接（Login + Cloud 函数） | 2h | 现有 WeChatBridge.jslib |
| **V2-P1** | WxAuthService + C# 回调 Host | 2h | V2-P0 |
| **V2-P2** | CloudSyncService + MergeProgress | 2.5h | V2-P1 |
| **V2-P3** | CloudSaveSystem + 工厂升级 | 1.5h | V2-P2 |
| **V2-P4** | 微信云开发部署（3 个云函数 + DB） | 1h | 微信后台 |
| **V2-P5** | 集成验收 + 真机测试 | 3h | V2-P0~P4 |

**总计**：纯编码 ~12h / 含调试 ~16h（单人 2~3 天）

### 验收清单

| # | 验收项 | 期望结果 |
|---|--------|---------|
| 1 | Editor 下游戏正常运行 | PlayerPrefsSaveSystem（V1 行为不变） |
| 2 | 真机首次启动静默登录 | Console 看到 `[WxAuth] Login success, openid=xxx` |
| 3 | 通关后云端有数据 | 云开发控制台查询 progress 集合有记录 |
| 4 | 清除本地 Storage 后重启 | 从云端拉回进度，关卡解锁状态恢复 |
| 5 | 断网状态通关 | 本地正常保存，联网后自动上传 |
| 6 | 双设备同步 | A 设备通关第 3 关 → B 设备重启后看到第 3 关已解锁 |
| 7 | wx.login 失败（模拟） | 游戏正常运行，仅本地存储 |
| 8 | 云函数超时（模拟） | 本地不受影响，2s 后自动重试 |
| 9 | V1→V2 迁移 | 老用户首次登录后进度上云，不丢失 |
| 10 | 性能：登录+同步不卡顿 | 启动到可操作 <2s（与 V1 持平） |

---

## 11. 风险与待决事项

| # | 风险/待决 | 影响 | 缓解方案 |
|---|----------|------|---------|
| R-01 | 微信云开发免费额度限制 | 调用次数超限 | 监控用量；DAU>1万时评估付费/迁移自建 |
| R-02 | wx.cloud 初始化时机 | 过早调用 API 失败 | game.js 最早初始化 + C# 侧 ready 检查 |
| R-03 | 多设备并发写入冲突 | 极小概率数据覆盖 | V3 直接 set 覆写（最后一次写入胜出）；单设备场景下无冲突 |
| R-04 | PlayerPrefs WebGL 限制 | 微信环境下 PlayerPrefs 底层走 localStorage | 已验证可用，V1 已在用 |
| R-05 | 云函数冷启动延迟 | 首次调用可能 >3s | 启动时 Login 调用即预热云函数 |

---

## 变更日志

| 版本 | 日期 | 变更 |
|------|------|------|
| v0.1 | 2026-05-07 | 初稿：完整架构 + 登录 + 同步 + 云函数 + 迁移 + 实施计划 |
| v0.2 | 2026-05-07 | PK R1 回应 CS-001~008：桥接架构明确(requestId路由) / 去掉多余wx.login / merge事件通知+Reload / 最新快照上传 / 去掉InitCloud / JsonHelper→ProgressData直接解析 |
| v0.3 | 2026-05-07 | PK R2 回应 CS-009~010：CloudSyncService 改为 IWeChatBridge 依赖注入 / 工厂方法签名修正 / 去掉幽灵 WxCloudBridge 类 |
| v0.4 | 2026-05-07 | PK R3（Unity架构师攻方）回应 UA-001~007：SharedProgressData 共享 DTO / 数据流形态表+GetProgressResult / Stub 实现+Login语义澄清 / 命名空间修正→Platform / jslib 5s setTimeout 超时保护 |
| v0.5 | 2026-05-07 | 实施完成：jslib CallCloudFunction(5s超时) + IWeChatBridge.CallCloudFunction + WxAuthService + CloudSyncService(PullMerge+Upload+Retry) + CloudSaveSystem(ISaveSystem工厂) + SG_ProgressManager.Reload + 云函数模板×3 |
| v0.6 | 2026-05-17 | **V3 云端权威模式**：①删除 seed 机制（cloud empty 不再回传本地数据）②启动时无条件用云端覆盖本地（含空值）③saveProgress 云函数直接 set 覆写（不做 union merge）④修正过时 XML 文档注释 |
