# PK 评审记录 — SG_TDD_06 云存储系统（Unity 架构师视角）

> **目标文档**：`SG_TDD_06_CLOUD_SAVE.md`（v0.3）
> **文档类型**：TDD
> **攻方角色**：资深 Unity 架构师（10 年+ Unity 引擎开发经验，专精 WebGL 平台限制、MonoBehaviour 生命周期、DllImport/jslib 桥接、异步模式、内存管理）
> **守方角色**：微信小程序开发者（专精微信生态、wx API、云开发、真机兼容性）
> **开始时间**：2026-05-07
> **PK 状态**：✅ 收敛完成（1 轮）
> **最大轮次**：8（实际用 1 轮）

---

## PK Round 1 — 攻方提问（Unity 架构师视角）

---

### UA-001 | 严重度 🔴高 | `ProgressData` 类型作用域冲突——MergeProgress 无法访问 SG_ProgressManager 的 private class

**涉及章节**：§3.3（MergeProgress 方法）与 §8（SG_ProgressManager）
**质疑**：`CloudSyncService.MergeProgress()` 中使用 `JsonUtility.FromJson<ProgressData>(localJson)` 反序列化。然而，现有代码中 `ProgressData` 是 `SG_ProgressManager` 内部的 `private class`（`SG_ProgressManager.cs` 第 24-29 行）。`CloudSyncService` 位于 `MiniGameTemplate.Data` 命名空间，完全无法访问这个类型。

TDD 未明确说明：
1. 是否要在 `CloudSyncService` 中重新定义一个独立的 `ProgressData` struct/class？
2. 还是要把 `SG_ProgressManager.ProgressData` 提升为 public/internal？
3. 如果两处各自定义，字段命名和序列化格式是否保证一致？

**潜在风险**：实施时必须决定类型归属和可见性，否则编译不通过。如果两处各自定义 `ProgressData`，字段差异（如 `class` vs `struct`、默认值初始化）会导致 `JsonUtility.FromJson` 反序列化行为不一致。
**建议方向**：明确定义一个 shared 的 `ProgressData` 类型（建议 `[Serializable] public class` 放在 `MiniGameTemplate.Data` 命名空间），供 `SG_ProgressManager` 和 `CloudSyncService` 共用。文档需指定这是新增的共享 DTO 还是对现有类的可见性修改。
**状态**：🟡 待回应

---

### UA-002 | 严重度 🔴高 | `CloudProgressResult.data` 类型与嵌套 JSON 解析矛盾——访问 `.clearedLevelsJson` 但 data 是 string 类型

**涉及章节**：§3.3（CloudProgressResult struct + PullAndMerge）
**质疑**：TDD 定义 `CloudProgressResult` 为：
```csharp
[Serializable]
private struct CloudProgressResult
{
    public bool success;
    public string data;  // 云端返回的 progress JSON 字符串
}
```
但在 `PullAndMerge` 中使用了 `cloudResult.data.clearedLevelsJson`——即把 `data` 当成了一个具有 `.clearedLevelsJson` 属性的对象来访问。这与 struct 定义中 `data` 是 `string` 类型互相矛盾。

更关键：`HandleCloudFunctionResult` 中 `cb?.Invoke(resp.success, resp.success ? resp.result : resp.error)` 传递给回调的 `result` 是 jslib 层 `JSON.stringify(res.result)` 的结果。`PullAndMerge` 的回调拿到的 `result` 已经是**纯净的云函数返回 JSON**（`getProgress` 返回的 `{ success: true, data: {...} }`），此时再用 `JsonUtility.FromJson<CloudProgressResult>` 解析会导致字段映射混乱——因为外层 `success` 已在 jslib 消费过。

**潜在风险**：数据解析层混乱将导致运行时 `data` 永远为 null/空，merge 永远跳过，云同步形同虚设。
**建议方向**：明确回调链中每一层的 JSON 结构，画一个"数据经过每层的形态"表。明确 `PullAndMerge` 回调中 `result` 参数的实际 JSON 内容是什么结构。
**状态**：🟡 待回应

---

### UA-003 | 严重度 🔴高 | `IWeChatBridge` 接口扩展 `CallCloudFunction` 需要所有实现者同步修改

**涉及章节**：§2.2（IWeChatBridge 接口扩展）
**质疑**：文档标注 `IWeChatBridge.cs 新增方法 void CallCloudFunction(...)`。当前实际代码中 `IWeChatBridge` 无此方法。接口扩展意味着 **所有实现者**（包括 `WeChatBridgeStub`）必须同时实现此方法，否则编译失败。

此外，现有 `Login(Action<bool, string>)` 方法已在接口中（第 79 行），且当前 `WeChatBridgeWebGL.Login()` 实现是直接委托给 `_fallback.Login()`。TDD 中 `WxAuthService` 调用 `_bridge.CallCloudFunction("login", ...)` 代替了 `_bridge.Login()`。那么现有 `IWeChatBridge.Login()` 方法的语义变化和兼容性如何处理？是否废弃？

**潜在风险**：遗漏 `WeChatBridgeStub.CallCloudFunction` 实现 → 编译失败。现有 Login 方法与新 CallCloudFunction("login") 语义重叠 → 调用者困惑。
**建议方向**：
1. 明确 `WeChatBridgeStub` 需新增 `CallCloudFunction` 的 stub 实现（直接 `onComplete?.Invoke(false, "stub")`）
2. 明确现有 `IWeChatBridge.Login()` 是保留（供其他模块继续使用）还是标记 `[Obsolete]`
**状态**：🟡 待回应

---

### UA-004 | 严重度 🟡中 | `WxAuthService` 命名空间在 `MiniGameTemplate.Platform` 但 `CloudSyncService` 在 `MiniGameTemplate.Data`——跨命名空间依赖方向违反分层约定

**涉及章节**：§2.3 / §3.3
**质疑**：`WxAuthService` 定义在 `MiniGameTemplate.Platform`，`CloudSyncService` 定义在 `MiniGameTemplate.Data`。`CloudSyncService` 引用 `WxAuthService`，这意味着 Data 层依赖 Platform 层。但按项目分层约定，应该是 Platform → Data 的依赖方向（或两者平级由上层组合）。

**潜在风险**：如果 Data 和 Platform 在不同的 asmdef 中有单向引用限制，可能导致循环引用或编译错误。
**建议方向**：明确分层方向。建议：`CloudSyncService` 也放到 `MiniGameTemplate.Platform` 命名空间（因为它依赖微信平台能力），或者通过构造函数注入 `IWeChatBridge` + 一个 `IAuthService` 接口来打破直接依赖。
**状态**：🟡 待回应

---

### UA-005 | 严重度 🟡中 | jslib 中 `requestId` 作为 `int` 传递——Emscripten jslib 对 int 参数无需 UTF8ToString 但需确认不丢精度

**涉及章节**：§5.1 / §5.2
**质疑**：DllImport 签名 `WXBridge_CallCloudFunction(int requestId, string name, string data)` 中 `requestId` 是 `int`。在 jslib 中，int 参数直接作为 JavaScript number 传入（Emscripten 行为），不需要 UTF8ToString。这部分是正确的。

但需要确认：当 `_nextRequestId` 超过 `int.MaxValue`（2^31-1）时 overflow 行为。C# 中 `_nextRequestId++` 在 unchecked 上下文中 overflow 为负数。JavaScript 中负数仍然是有效的 number，JSON.stringify 也能正确序列化负数 int。但 `_cloudCallbacks` 的 Dictionary key 使用 int，负数 key 可以正常工作。

实际上对于云存储场景，requestId 不可能达到 int.MaxValue（需要 20 亿次调用），所以这是**理论问题**。

**潜在风险**：低概率，实际不会发生。但文档中可以加一句澄清。
**建议方向**：这个问题很低风险，可以在编码期间通过简单注释解决。不阻塞。
**状态**：🟡 待回应

---

### UA-006 | 严重度 🟡中 | V2-BC-06 行为契约"超时 5s"未在代码中实现

**涉及章节**：§9（行为契约表）/ §3.3 / §5.1
**质疑**：行为契约 `V2-BC-06` 声明"超时 5s 自动放弃本次云函数调用"，验证方式为"jslib 层 setTimeout 或 WeChatBridgeWebGL 内置超时回调"。但：

1. jslib 代码（§5.1）中 `wx.cloud.callFunction` 没有设置 `timeout` 配置项（微信云开发 callFunction 支持 `timeout` 参数但默认无超时）
2. C# 侧 `WeChatBridgeWebGL.HandleCloudFunctionResult` 也没有超时机制
3. `CloudSyncService.DoUpload()` 中的重试是靠 `TimerService.Delay` 做延迟重试，但没有对单次调用做超时取消

也就是说，如果云函数由于网络原因完全没有响应（fail/success 都不回调），回调字典中的 `Action` 将永远不会被清除，造成内存泄漏和状态永久卡在 Syncing。

**潜在风险**：弱网环境下云函数无响应 → `_isSyncing` 永远为 true → 后续所有上传请求被 DoUpload 开头的 `if (_isSyncing) return` 拦截 → 整个云同步系统瘫痪直到 App 重启。
**建议方向**：在 jslib 层加 `setTimeout` 超时回调（5s 后强制 sendToUnity fail），或在 C# 侧 `CallCloudFunction` 中启动一个 TimerService 超时计时器。
**状态**：🟡 待回应

---

### UA-007 | 严重度 🟢低 | `MergeProgress` 中 `new HashSet<int>` + `new List<int>` 产生 GC 分配

**涉及章节**：§3.3
**质疑**：每次 merge 会创建 `new HashSet<int>` 和 `new List<int>`。对于这个场景（启动时 1 次 + 极少的热启动 Reload），频率极低，GC 影响可忽略。

**潜在风险**：无实际风险。提出仅为 TDD 完整性记录。
**建议方向**：无需修改，编码期间可加注释说明"此处 GC 可接受，因调用频率极低"。
**状态**：🟡 待回应

---

## PK Round 1 — 守方回应（微信小程序开发者视角）

---

### UA-001 回应 | ✅ 接受并修正

**处理方式**：接受建议。新增 `SharedProgressData` 公共 DTO 类（`MiniGameTemplate.Data` 命名空间），供 `SG_ProgressManager` 和 `CloudSyncService` 共用。

**文档变更**：
- §3.3 新增 `SharedProgressData` 定义
- §3.5 `MergeProgress` 改为使用 `SharedProgressData`
- 添加迁移说明：`SG_ProgressManager` 内部 `private class ProgressData` 将替换为引用共享类型

**状态**：✅ 已修正

---

### UA-002 回应 | ✅ 接受并修正

**处理方式**：完全接受。这是一个真实的编译错误级问题。

**文档变更**：
- §3.4 新增「数据流形态表」，画出每一层的 JSON 结构
- §3.5 `PullAndMerge` 回调改为使用 `GetProgressResult` 反序列化（含嵌套 `SharedProgressData data`）
- 删除原有的 `CloudProgressResult` struct，替换为 `GetProgressResult` class
- 明确两步反序列化逻辑

**状态**：✅ 已修正

---

### UA-003 回应 | ✅ 接受并修正

**处理方式**：接受。在 §2.2 后追加了 `WeChatBridgeStub.CallCloudFunction` 的 stub 实现，以及 `IWeChatBridge.Login()` 方法的语义澄清。

**关键澄清**：
- `Login()` 保留不废弃——它走传统 `wx.login → code2session` 路径
- `CallCloudFunction("login", ...)` 走云开发路径——自动注入 openid
- 两者并存不冲突，服务不同场景

**状态**：✅ 已修正

---

### UA-004 回应 | ✅ 接受并修正

**处理方式**：接受。`CloudSyncService` 命名空间从 `MiniGameTemplate.Data` 改为 `MiniGameTemplate.Platform`（因为它依赖微信平台能力）。`SharedProgressData` DTO 保留在 `MiniGameTemplate.Data`。

**依赖方向**：Platform → Data ✅（无反向依赖）

**状态**：✅ 已修正

---

### UA-005 回应 | 🟢 知悉，不修改

**处理方式**：攻方自己也承认"实际不会发生"。同意在编码期间加注释即可，TDD 不需要为此增加篇幅。

**理由**：requestId 在应用生命周期内最多几十次调用（登录 1 次 + 每次通关上传 1 次），距离 int.MaxValue 差 10 个数量级。

**状态**：✅ 确认，编码期间注释

---

### UA-006 回应 | ✅ 接受并修正

**处理方式**：完全接受。这是一个真实的弱网场景风险。

**文档变更**：§5.1 jslib 层 `WXBridge_CallCloudFunction` 新增 `setTimeout(5000ms)` 超时保护。超时后强制 sendToUnity `success=false`，确保回调一定触发。同时 success/fail 回调加了 `if (timeoutId === null) return` 防止超时后重复回调。

**V2-BC-06 现在有实现支撑**。

**状态**：✅ 已修正

---

### UA-007 回应 | 🟢 知悉，不修改

**处理方式**：同意攻方分析——调用频率极低（启动 1 次 + 极少的热启动），GC 分配可忽略。编码期间加注释说明即可。

**状态**：✅ 确认，不修改

---

## 收敛判定

| 问题 | 严重度 | 处理 | 收敛？ |
|------|--------|------|--------|
| UA-001 | 🔴高 | 新增 SharedProgressData 共享类型 | ✅ |
| UA-002 | 🔴高 | 数据流形态表 + GetProgressResult | ✅ |
| UA-003 | 🔴高 | Stub 实现 + Login 语义澄清 | ✅ |
| UA-004 | 🟡中 | 命名空间修正 → Platform | ✅ |
| UA-005 | 🟡中 | 知悉，编码注释 | ✅ |
| UA-006 | 🟡中 | jslib setTimeout 超时保护 | ✅ |
| UA-007 | 🟢低 | 知悉，不修改 | ✅ |

**结论**：7/7 问题全部收敛（5 项已修正文档，2 项确认编码期间处理）。**无需第 2 轮。PK 收敛完成。**

---

## 总结

- **攻方提出**：7 个问题（3🔴 + 3🟡 + 1🟢）
- **守方修正**：5 处实质文档变更
- **文档版本**：v0.3 → v0.4
- **收敛轮次**：1 轮
- **PK 状态**：✅ 收敛完成





