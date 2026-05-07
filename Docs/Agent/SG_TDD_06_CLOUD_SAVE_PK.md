# PK 评审记录 — SG_TDD_06_CLOUD_SAVE.md

> **目标文档**：`Docs/Agent/SG_TDD_06_CLOUD_SAVE.md`
> **文档类型**：TDD
> **攻方角色**：软件架构师（专精系统设计、API 设计、可维护性和关注点分离，10 年以上经验）
> **守方角色**：微信小程序开发者（专精微信 API、平台约束、真机行为、云开发实战）
> **开始时间**：2026-05-07 12:55
> **最大轮次**：8
> **PK 状态**：✅ 已收敛（Round 3，2 轮即收敛）

---

## 收敛总结

| 指标 | 数值 |
|------|------|
| 总轮次 | 3（R1 攻方提问 → R1 守方回应 → R2 攻方复审+新问题 → R2 守方回应 → R3 收敛） |
| 总问题数 | 10（CS-001~010） |
| 🔴 高 | 2（CS-001, CS-002）— 全部在 R1 解决 |
| 🟡 中 | 6（CS-003~006, CS-009~010）— 全部在 R1/R2 解决 |
| 🟢 低 | 2（CS-007, CS-008）— 全部在 R1 解决 |
| 文档版本 | v0.1 → v0.3 |
| 关键改进 | 去掉多余 wx.login / requestId 字典路由 / merge 事件通知 / 最新快照上传 / IWeChatBridge 依赖注入 |

---

## PK Round 1 — 攻方提问

### CS-001 | 严重度 🔴高 | 文档引用了不存在的 `WxLoginBridge` 和 `WxCloudBridge` 类，但未定义其与现有桥接架构的关系

**涉及章节**：§2.2, §3.3, §5
**质疑**：`WxAuthService` 中调用 `WxLoginBridge.Login(...)` 和 `WxCloudBridge.CallFunction(...)`，但现有架构中回调是通过 `WeChatBridgeWebGLCallbackHost`（单例 MonoBehaviour）+ `SendMessage` 机制分发的。文档 §5.3 仅给出了 CallbackHost 的方法签名骨架，但未说明：(1) 如何将 `OnLoginResult` / `OnCloudFunctionResult` 的回调路由到 `WxAuthService` / `CloudSyncService` 实例；(2) `WxLoginBridge` / `WxCloudBridge` 是独立新类还是现有 `WeChatBridgeWebGL` 的扩展。现有 `WeChatBridgeWebGL` 已有 `Login()` 方法（当前委托给 stub），新设计是否复用它。
**潜在风险**：回调路由机制不明确将导致实施时对架构理解分歧，可能出现多种不兼容实现。
**建议方向**：明确 `WxLoginBridge` / `WxCloudBridge` 的类定义（是 static 工具类？还是实例类？），并画出回调从 jslib → SendMessage → CallbackHost → 业务类的完整路由方式。
**状态**：✅ 已回应（Round 1）— 废弃独立 WxLoginBridge/WxCloudBridge，直接扩展 WeChatBridgeWebGL + requestId 字典路由机制，§2.2 新增完整桥接架构说明。

---

### CS-002 | 严重度 🔴高 | 启动时 merge 完成前 ProgressManager 已读取了旧本地数据，merge 后无法通知上层刷新

**涉及章节**：§4.2, §8.2
**质疑**：启动时序为：`CreateSaveSystem()` → `InitCloudSync()`（异步）→ `SG_ProgressManager` 构造（立即 `Load()` 读本地）。当云端 merge 完成后写回本地，但 `SG_ProgressManager` 内部 `_data` 已是旧数据的内存快照，不会自动刷新。文档说"下次 Load() 时自动读到最新数据"，但 `SG_ProgressManager` 没有 `Reload()` 方法，也没有任何机制触发重新加载。
**潜在风险**：用户换设备后首次启动，云端有更多通关数据但 UI 显示的仍是旧本地数据（空），直到下次冷启动。
**建议方向**：为 `SG_ProgressManager` 增加 `Reload()` 方法（或事件通知机制），在 merge 完成后主动触发；或者在 `InitCloudSync` 完成前阻塞 ProgressManager 的创建（但这与"不阻塞"原则冲突，需权衡）。
**状态**：✅ 已回应（Round 1）— 新增 CloudSaveSystem.OnCloudMergeCompleted 事件 + Reload() 方法；SG_ProgressManager 新增 Reload()；启动时注册事件链。

---

### CS-003 | 严重度 🟡中 | `Save()` 方法直接委托本地不触发云同步——语义缺口

**涉及章节**：§4.2
**质疑**：`CloudSaveSystem.Save()` 直接委托给 `_local.Save()`，不触发云同步。如果有代码直接调用 `Save()` 而非 `SaveString`，进度不会上传。`SG_ProgressManager.Save()` 调用链是 `SaveString` + `FlushIfDirty`，看起来没问题。但 `ISaveSystem` 的语义使得其他使用者可能走 SaveInt/SaveFloat → 然后 Save() 刷盘，这些路径不触发同步。
**潜在风险**：语义不一致，未来扩展时可能有遗漏路径。
**建议方向**：确认只有通过 `SaveString(PROGRESS_KEY, ...)` 路径才触发同步，并在文档中明确说明这是有意设计（只同步进度数据，其他 KV 不需要上云）。
**状态**：✅ 已回应（Round 1）— 在代码注释中明确说明"有意设计：仅 PROGRESS_KEY 触发同步，其他 KV 不上云"。

---

### CS-004 | 严重度 🟡中 | `wx.cloud.init` 双重初始化：game.js + `WXBridge_InitCloud`

**涉及章节**：§5.1, §6.3
**质疑**：§6.3 要求在 `game.js` 中调用 `wx.cloud.init()`，§5.1 又新增了 `WXBridge_InitCloud` 函数。文档未说明两者的调用关系——是二选一还是都需要？`wx.cloud.init` 重复调用是否有副作用？
**潜在风险**：如果 game.js 已初始化，C# 侧再调用可能无害但也可能报 warning；如果只依赖 C# 侧调用，则时序上可能太晚。
**建议方向**：明确只在 game.js 初始化一次，移除 `WXBridge_InitCloud` 或将其定位为"兜底/env 切换"用途。
**状态**：✅ 已回应（Round 1）— 移除 WXBridge_InitCloud，明确只在 game.js 初始化一次。§5.1 已更新。

---

### CS-005 | 严重度 🟡中 | 云函数 `login` 中 `wx.login()` 获取 code 是多余步骤

**涉及章节**：§3.2.1, §2.1, §2.2
**质疑**：`login` 云函数通过 `cloud.getWXContext()` 直接获取 OPENID（微信云开发自动注入），根本不需要客户端传 `code`。但 §2.1 时序图和 §2.2 代码中客户端先 `wx.login()` 获取 code 再传给云函数——这个 code 在云函数中完全未使用（没有 `auth.code2Session` 调用）。
**潜在风险**：`wx.login()` 调用是多余网络请求，增加启动延迟（尤其云函数冷启动场景 +200-500ms）。
**建议方向**：省去 `wx.login()` 步骤，直接 `callFunction("login", {})` 即可。简化时序并减少一次网络调用。
**状态**：✅ 已回应（Round 1）— 完全采纳。去掉 wx.login 步骤，时序图/代码/jslib 全部更新。省掉一次网络往返。

---

### CS-006 | 严重度 🟡中 | `EnqueueUpload` 在 `_isSyncing=true` 时丢失最新进度

**涉及章节**：§3.3
**质疑**：`DoUpload` 中 `if (_isSyncing) return;`——快速连续通关时后续调用被丢弃。虽然 `_hasPendingUpload=true`，但没有机制在上传成功后用最新数据重新上传。
**潜在风险**：中间进度更新丢失（只有首次 `progressJson` 被上传）。云函数有服务端 union merge 兜底，但前提是下次上传带最新数据——文档未保证。
**建议方向**：上传成功后检查 `_hasPendingUpload`，若仍为 true 则重新读取最新本地数据再上传；或改为"上传最新快照"而非"上传传入参数"。
**状态**：✅ 已回应（Round 1）— 改为"最新快照"模式。新增 _latestProgressJson 字段，上传成功后比较是否有新数据，有则自动重传。

---

### CS-007 | 严重度 🟢低 | 行为契约 V2-BC-10 提到 `CloudSaveSystem.Reload()` 但类定义中无此方法

**涉及章节**：§9, §4.2
**质疑**：V2-BC-10 声称"热启动时重新拉取云端"通过 `CloudSaveSystem.Reload()` 实现，但 §4.2 的类定义中没有 `Reload()` 方法。
**潜在风险**：契约不可验证，实施者不清楚热启动逻辑应该放哪里。
**建议方向**：在 §4.2 中补充 `Reload()` 方法定义。
**状态**：✅ 已回应（Round 1）— §4.2 已补充完整的 Reload() 方法定义 + 事件通知机制。

---

### CS-008 | 严重度 🟢低 | `JsonHelper.FromJsonArray<int>` 未定义

**涉及章节**：§3.3
**质疑**：`MergeProgress` 中调用 `JsonHelper.FromJsonArray<int>(cloudLevelsJson)`，Unity 内置 `JsonUtility` 不支持直接反序列化数组。该工具类是否已存在于项目中未说明。
**潜在风险**：实施时需要额外编写此工具方法。
**建议方向**：注明需要新建的工具方法，或改用 wrapper struct 方式解析。
**状态**：✅ 已回应（Round 1）— 改为直接用 ProgressData struct 解析云端返回的 JSON，去掉 JsonHelper 依赖。

---

## PK Round 2 — 攻方复审

### Round 1 回应评估

- CS-001: 🟢 桥接架构完整，requestId 路由清晰
- CS-002: 🟢 事件通知 + Reload 链路完整
- CS-003: 🟢 注释明确有意设计
- CS-004: 🟢 去掉 InitCloud，干净
- CS-005: 🟢 完全采纳，少一次网络往返
- CS-006: 🟢 最新快照模式正确
- CS-007: 🟢 Reload 已补充
- CS-008: 🟢 去掉 JsonHelper 依赖

### 新问题

---

### CS-009 | 严重度 🟡中 | CloudSyncService 中 `_cloud = new WxCloudBridge()` 引用了未定义类

**涉及章节**：§3.3, §2.2
**质疑**：§2.2 明确说"V2 功能直接扩展现有 WeChatBridgeWebGL，不新建独立的 WxCloudBridge 类"。但 §3.3 的 CloudSyncService 构造函数中仍然写 `_cloud = new WxCloudBridge()`。这与桥接架构决策矛盾。CloudSyncService 应通过 `IWeChatBridge` 接口调用 `CallCloudFunction`。
**潜在风险**：实施者不知道该依赖什么类型，或误创建一个不存在的 WxCloudBridge 类。
**建议方向**：CloudSyncService 构造函数改为接收 `IWeChatBridge` 参数，内部调用 `_bridge.CallCloudFunction()`。
**状态**：✅ 已回应（Round 2）— CloudSyncService 构造函数改为 `(WxAuthService auth, IWeChatBridge bridge)`，内部所有 `_cloud.CallFunction` 改为 `_bridge.CallCloudFunction`。去掉幽灵 WxCloudBridge 类。

---

### CS-010 | 严重度 🟡中 | 工厂方法 `new WxAuthService()` 与类定义 `WxAuthService(IWeChatBridge bridge)` 签名不匹配

**涉及章节**：§4.3, §2.3
**质疑**：§4.3 工厂代码写 `var auth = new WxAuthService()`（无参），但 §2.3 类定义的构造函数需要 `IWeChatBridge bridge` 参数。
**潜在风险**：编译失败。
**建议方向**：工厂中传入 bridge 参数：`new WxAuthService(wechatBridge)`。
**状态**：✅ 已回应（Round 2）— 工厂改为 `new WxAuthService(wechatBridge)` + `new CloudSaveSystem(auth, wechatBridge)`。签名一致。

---

## PK Round 3 — 攻方最终判定

### Round 2 回应评估

- CS-009: 🟢 CloudSyncService 构造函数已改为 DI，幽灵类彻底清除
- CS-010: 🟢 工厂签名与类定义一致，编译无问题

### 最终判定：✅ PK 收敛

理由：
1. ✅ 所有 🔴 高严重度问题（CS-001, CS-002）在 Round 1 充分解决
2. ✅ 所有 🟡 中严重度问题（CS-003~006, CS-009~010）在 Round 1/2 充分解决
3. ✅ 文档无自相矛盾的逻辑内容
4. ✅ 代码路径中无幽灵引用
5. ✅ 关键 API 签名在所有章节间完全一致





