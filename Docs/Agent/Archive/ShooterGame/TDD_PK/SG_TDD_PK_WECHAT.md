# PK 评审记录 — ShooterGame 用户登录与关卡进度（微信真机视角）

> **目标文档**：`SG_TDD_03_LEVEL_PROGRESS.md` + `SG_TDD_04_UI_CONTROLLERS.md` + `SG_GAME_DESIGN.md` §九
> **文档类型**：TDD
> **攻方角色**：资深微信小程序开发者（10 年微信生态经验，专精登录、存储、真机兼容、网络中断恢复）
> **守方角色**：Unity 架构师（ShooterGame TDD 设计者）
> **开始时间**：2026-05-03
> **PK 状态**：✅ 已收敛（2 轮 / 11 问题 / 11/11 回应）
> **最大轮次**：8

---

## PK Round 1 — 攻方提问（微信小程序开发者视角）

### WX-001 | 严重度 🔴高 | wx.setStorageSync 容量上限与存储失败无处理

**涉及章节**：§2.2 Save()、§9.3（GDD）
**质疑**：`ISaveSystem` 在微信小游戏环境下底层映射到 `wx.setStorageSync`。该 API 有 **单 key 上限 1MB、总容量 10MB** 的硬性限制。当前 `Save()` 方法直接调用 `_saveSystem.SaveString(SAVE_KEY, json)` + `FlushIfDirty()`，没有任何异常捕获。在真机上，当用户设备存储满或微信清理缓存后，`wx.setStorageSync` 会抛出同步异常，导致：
1. `Save()` 方法崩溃
2. 用户通关后进度丢失（无反馈）
3. `MarkLevelCleared` 调用方（BattleController）不知道存储失败

**潜在风险**：真机上 1~5% 的用户会遇到存储失败（低端设备+微信后台清理），进度丢失 → 用户投诉 → 差评。
**建议方向**：`Save()` 需要 try-catch 包裹 + 返回 bool 标识成功/失败 + 失败时 UI 提示"保存失败，请释放存储空间"。

---

### WX-002 | 严重度 🔴高 | 无用户登录体系 → 跨设备进度丢失 + 换手机清零

**涉及章节**：§2.0 生命周期、GDD §9.3、GDD §2.1
**质疑**：文档明确说"无独立登录流程，打开即玩"，存储走本地 `wx.setStorageSync`。这意味着：
1. **用户换手机**：进度全部丢失
2. **微信清除小游戏数据**：进度丢失（设置 → 通用 → 存储空间 → 管理，用户误操作常见）
3. **用户 A 设备打到第 5 关 → 用户 B 设备从第 1 关开始**（同一微信号）

微信小游戏的标准做法是：**静默登录 `wx.login()` 获取 openid → 服务端存储进度 → 本地缓存加速读取**。即使"打开即玩"也需要后台静默获取 openid 做关联。

V1 如果确定不做服务端，至少需要在文档中明确：
- 用户换设备/清缓存的进度丢失是"已知限制"
- 预留 openid 关联存储的升级路径

**潜在风险**：微信平台上"换设备"是高频场景（用户换手机、双设备）。不做登录关联，上线后 DAU 超过 1000 就会收到投诉。
**建议方向**：V1 至少做 `wx.login()` 静默登录 + openid 落库（服务端最简 CRUD），本地作为读缓存；或在 TDD 中明确标注"V1 已知限制 + V2 升级路径"。

---

### WX-003 | 严重度 🟡中 | wx.setStorageSync 是同步阻塞调用 → 通关帧卡顿

**涉及章节**：§2.2 Save()
**质疑**：`Save()` → `_saveSystem.SaveString()` → `wx.setStorageSync()`。在微信小游戏真机上，`wx.setStorageSync` 是**主线程同步阻塞**的 I/O 操作。当数据量小（V1 仅几十字节）时影响不大，但：
1. 如果将来 `clearedLevels` 扩展为包含分数、星级等更多数据，序列化+写入耗时会增加
2. 通关瞬间（VictoryPanel 弹出前）调用同步存储 → 可能导致**帧卡顿**（尤其低端机）
3. 微信官方推荐存储操作用 `wx.setStorage`（异步版本）

**潜在风险**：V1 数据量极小，实际影响≤1ms。但架构上如果 `ISaveSystem` 的微信实现用的是同步版本，后续扩展时会踩坑。
**建议方向**：确认 `WxSaveSystem` 实现是否使用 `wx.setStorageSync`（同步）还是 `wx.setStorage`（异步回调）。如果是同步版本，建议 TDD 中记录"V1 可接受，V2 需改异步"。

---

### WX-004 | 严重度 🟡中 | 存储数据无校验 → 被篡改后游戏行为未定义

**涉及章节**：§2.2 Load()
**质疑**：`Load()` 中 try-catch 只处理了 JSON 解析异常（数据损坏 → 重置）。但没有处理**数据被恶意篡改**的场景：
1. 微信开发者工具 / 第三方工具可以直接修改 storage
2. 用户将 `clearedLevels` 改为 `[1,2,3,4,5]` → 直接全通关
3. 用户将 `clearedLevels` 改为 `[99, -1, 0]` → 越界访问

当前 `IsLevelCleared(levelIndex)` 使用 `List.Contains()`，不会越界崩溃，但 `IsLevelUnlocked()` 依赖前一关通关，如果 `clearedLevels=[5]`（跳过 1~4），行为是否正确？

**潜在风险**：对于单机休闲游戏，作弊不是核心问题。但越界数据（负数、超出总关卡数）是否会导致 `LevelSelectController.RefreshAllNodes()` 行为异常？
**建议方向**：`Load()` 后加一步 `ValidateData()`：过滤 clearedLevels 中 <1 或 >5 的值。防御性编程，2 行代码。

---

### WX-005 | 严重度 🟡中 | SG_ProgressManager 静态持有 → 微信小游戏热启动内存残留

**涉及章节**：§2.0 生命周期
**质疑**：`SG_ProgressManager` 通过 `GameStartupFlow.Progress` 静态字段持有。在微信小游戏环境中，Unity WebGL 实例有**热启动**特性——用户从后台切回小游戏时，JS 运行时可能保留之前的内存状态（非完全冷启动）。

如果用户在游戏期间通过微信的"关闭小游戏但不销毁 WebGL 实例"路径返回（微信胶囊按钮 → 回到聊天 → 再次点击小游戏），静态字段持有的是**旧数据**。如果其他场景也改了 storage（比如微信同步清理），可能出现内存中的进度与磁盘不一致。

**潜在风险**：热启动下 `SG_ProgressManager._data` 与实际 storage 内容不一致 → 进度显示错误。概率低但存在。
**建议方向**：`SG_Boot.InitProgress()` 中加一个 `if (Progress != null && 需要重载) Progress.Reload()` 检查；或 `OnApplicationFocus(true)` 时重新从 storage 加载。

---

### WX-006 | 严重度 🟡中 | 通关后返回 Boot 场景 → SceneManager.LoadScene 丢失内存中最新进度

**涉及章节**：§8.1 HandleVictoryConfirm()、§2.0 生命周期
**质疑**：
```csharp
private IEnumerator HandleVictoryConfirm()
{
    _progressManager.MarkLevelCleared(_currentLevelIndex.Value + 1);  // 存储
    yield return StartCoroutine(TransitionOut(0.4f));
    SceneManager.LoadScene("Boot");  // 重新加载 Boot 场景
}
```

`LoadScene("Boot")` 后，Boot 场景的 `GameStartupFlow.Awake()` 会重新创建 `SG_ProgressManager`（构造函数调用 `Load()`）。**问题是**：`MarkLevelCleared` 刚写入 storage，然后立刻 `LoadScene` → 新场景 Awake → `Load()` → 能否保证读到刚写入的数据？

在微信小游戏中 `wx.setStorageSync` 写入后**同一进程内**立即读取是安全的（同步操作保证顺序）。但如果 `ISaveSystem` 有内部缓存/延迟写入（`FlushIfDirty` 暗示有 dirty flag），**是否存在写入还没 flush 就 LoadScene 了**的时序风险？

**潜在风险**：如果 `FlushIfDirty` 是延迟写入（batch flush），通关存档可能在场景切换时丢失。
**建议方向**：确认 `Save()` 中的 `FlushIfDirty()` 是立即持久化还是延迟。如果立即持久化，在文档中明确标注；如果延迟，需要在 `HandleVictoryConfirm` 中确保 flush 完成后再 LoadScene。

---

### WX-007 | 严重度 🟡中 | 关卡进度 JSON 无版本迁移测试路径

**涉及章节**：§2.2 MigrateData()
**质疑**：`MigrateData()` 是一个空方法（"V1→V2 迁移逻辑预留"）。文档中没有定义：
1. V2 如果需要新增字段（如 `bestScore`、`starRating`），迁移逻辑具体怎么写
2. 如果 `version` 字段在 V1 被意外改为 0 或负数会怎样
3. 迁移失败的回退策略（是保留旧数据还是重置）

当前 `version < CURRENT_VERSION` 会触发迁移，但 `version=0`（损坏数据）也会进入迁移分支，迁移逻辑为空 → 只是把 version 改为 1 → 数据本身可能不完整。

**潜在风险**：V2 上线时如果迁移逻辑写错，老用户进度丢失。V1 需要为 V2 留好接口，但当前"预留"太模糊。
**建议方向**：TDD 中增加 V2 迁移规范：明确 `ProgressData` 字段扩展规则（只追加、不删除）+ version 必须 ≥1 的校验 + 迁移失败回退策略。

---

### WX-008 | 严重度 🟢低 | 应用暂停/退出的 FlushIfDirty 时机在微信小游戏环境中不可靠

**涉及章节**：§2.3 存储时机
**质疑**：文档写"应用暂停/退出 → `ISaveSystem.FlushIfDirty()`"。在微信小游戏中：
1. `Application.OnApplicationPause(true)` 对应用户切到后台——这个回调在微信 iOS 上**不保证触发**（微信可能直接冻结 WebGL 运行时）
2. `Application.OnApplicationQuit()` 在微信小游戏中**几乎永远不会触发**（用户是通过微信 UI 关闭小游戏，Unity 收不到 quit 事件）

所以"应用暂停/退出时 flush"在微信真机上是**不可靠的安全网**。

**潜在风险**：如果有其他写入路径（不止通关时）依赖这个 flush，那些写入在真机上可能丢失。V1 只有通关时写入（`Save()` 内部已经 `FlushIfDirty`），所以实际不影响。但文档描述会误导后续开发者。
**建议方向**：在存储时机表中注明"⚠️ 微信小游戏环境中 OnApplicationPause/Quit 不保证触发，关键数据必须在操作时立即持久化"。

---

> **PK 收敛评估（Round 1）**：
> - 🔴 高 × 2（WX-001 存储失败无处理、WX-002 无登录进度丢失）
> - 🟡 中 × 5（WX-003~007）
> - 🟢 低 × 1（WX-008）
> - **不收敛**，等待守方回应。

---

## PK Round 1 — 守方回应（Unity 架构师）

### WX-001 ✅ 已接受并修正

**回应**：完全合理。`Save()` 已改为 try-catch 包裹 + 返回 bool。`MarkLevelCleared` 返回 bool，调用方在失败时 Toast 提示用户。

**文档变更**：§2.2 `Save()` → try-catch + bool 返回；§2.3.1 新增存储失败处理规范

---

### WX-002 ✅ 已接受（标注已知限制 + V2 路径）

**回应**：V1 定位是快速验证玩法的 MVP，不上服务端。但攻方说得对，必须在文档中明确标注这是"已知限制"而非"遗漏"。

**文档变更**：§2.0.2 新增"V1 已知限制与 V2 升级路径"完整章节——明确了：
- V1 纯本地，用户换设备/清缓存进度丢失
- V2 路径：wx.login → openid → 服务端 CRUD → 本地做读缓存
- 冲突策略：取并集
- V2 不阻塞 V1 编码（ISaveSystem 接口不变）

**不做 V1 服务端的理由**：
1. 独立开发者，上线前验证玩法是第一优先级
2. 服务端引入后端部署+运维成本，5 关休闲游戏 DAU <1000 前投入产出比极低
3. `ISaveSystem` 抽象层已预留升级接口，V2 改动局限在 `WxSaveSystem` 内部

---

### WX-003 ✅ 已接受（标注 V1 可接受 + V2 考虑）

**回应**：V1 数据量 <100 字节，`wx.setStorageSync` 耗时 <1ms，对帧率无影响。但文档应明确记录这个决策和升级条件。

**文档变更**：§2.3.1 新增"同步 vs 异步"说明

---

### WX-004 ✅ 已接受并修正

**回应**：`ValidateData()` 是正确的防御性编程，成本极低。已添加。

**文档变更**：§2.2 Load() 后新增 `ValidateData()` 调用；新增 `ValidateData()` 方法定义

---

### WX-005 ✅ 已接受并修正

**回应**：热启动是真实场景。`Reload()` 方法 + Boot 时判断已有实例则重载，是正确方案。

**文档变更**：§2.0.1 新增"微信小游戏热启动处理"章节 + `Reload()` 方法 + `SG_Boot.InitProgress()` 改写

---

### WX-006 ✅ 已接受（已澄清）

**回应**：`wx.setStorageSync` 是同步调用，`Save()` 返回后数据已落盘，不存在时序风险。文档中已明确标注这一铁律。

**文档变更**：§2.3.1 "场景切换时序安全"段落

---

### WX-007 ✅ 已接受并修正

**回应**：迁移规范确实应该在 V1 就定好规矩，否则 V2 容易踩坑。

**文档变更**：`MigrateData()` 注释中新增迁移规范（只追加不删除、失败保留旧数据、version<1 视为损坏）

---

### WX-008 ✅ 已接受（文档澄清）

**回应**：完全正确。V1 的关键数据已经在操作时立即持久化，不依赖 OnApplicationPause/Quit。文档中已明确标注"仅为额外安全网"。

**文档变更**：§2.3.1 "OnApplicationPause/Quit 不可靠"段落 + 行为契约 SG-BC-09

---

## PK Round 2 — 攻方复审（微信小程序开发者）

### Round 1 回应评估

- WX-001: 🟢 满意 — Save() try-catch + bool 返回 + 调用方 Toast，闭环了
- WX-002: 🟢 满意 — V1 已知限制明确标注 + V2 路径完整（wx.login → openid → CRUD → 并集策略），作为独立开发者 MVP 策略合理
- WX-003: 🟢 满意 — 明确记录了 V1 同步可接受 + V2 升级条件
- WX-004: 🟢 满意 — ValidateData() 2 行代码，防御到位
- WX-005: 🟢 满意 — Reload() + InitProgress() 判断，热启动场景解决
- WX-006: 🟢 满意 — 同步写入铁律已明确标注，时序安全有保证
- WX-007: 🟢 满意 — 迁移规范清晰（只追加不删除 + 失败保留旧数据）
- WX-008: 🟢 满意 — "仅为额外安全网"定位准确 + SG-BC-09 契约

### 新问题

### WX-009 | 严重度 🟡中 | MarkLevelCleared 返回 false 后 BattleController 处理未在 TDD 中定义

**涉及章节**：§2.3.1（存储失败处理）、SG_TDD_02（BattleController HandleVictoryConfirm）
**质疑**：§2.3.1 提到"调用方检查返回值"并 Toast 提示，但 `SG_TDD_02_BATTLE_SYSTEM.md` 中的 `HandleVictoryConfirm` 代码还是：
```csharp
_progressManager.MarkLevelCleared(_currentLevelIndex.Value + 1);
```
没有接收返回值。需要在战斗系统 TDD 中同步更新这段代码。

**潜在风险**：TDD 文档间不一致 → 编码时看战斗系统 TDD 的开发者不会加返回值检查。
**建议方向**：在 SG_TDD_02 的 HandleVictoryConfirm 中补充 `bool saved = _progressManager.MarkLevelCleared(...)` 的处理逻辑。不需要改得很复杂，Toast + 正常流程即可。

---

### WX-010 | 严重度 🟡中 | ValidateData 中 TOTAL_LEVELS=5 硬编码 → V2 加关时必踩坑

**涉及章节**：§2.2 ValidateData()
**质疑**：`const int TOTAL_LEVELS = 5` 硬编码在 ValidateData 中。V2 如果加到 10 关但忘改这个常量，第 6~10 关的通关数据会被过滤掉 → 进度丢失。

**潜在风险**：V2 加关时的隐性 Bug，很难追踪。
**建议方向**：改为构造函数参数 `SG_ProgressManager(ISaveSystem, int totalLevels)` 或从外部注入 `_levelConfigs.Length`。

---

### WX-011 | 严重度 🟢低 | Reload() 在战斗中被调用的边界情况

**涉及章节**：§2.0.1
**质疑**：`Reload()` 只在 Boot 时调用，但如果将来有人在战斗中调用（比如恢复前台时的 OnApplicationFocus 回调），正在进行的战斗数据会被重载 → `_data` 被覆盖。

**潜在风险**：V1 风险为零（只在 Boot 时调用）。但接口是 public，建议加注释说明调用约束。
**建议方向**：在 `Reload()` 的 XML doc 注释中加一句"仅在非战斗状态下调用（Boot 场景）"。

---

> **PK 收敛评估（Round 2）**：
> - 🔴 高 × 0
> - 🟡 中 × 2（WX-009 文档同步、WX-010 硬编码）
> - 🟢 低 × 1（WX-011 注释）
> - Round 1 全部 🟢 满意。严重度明显下降（🔴→0），如果 WX-009/010 解决即可收敛。

---

## PK Round 2 — 守方回应（Unity 架构师）

### WX-009 ✅ 已接受并修正

**回应**：确实需要同步更新。`SG_TDD_04_UI_CONTROLLERS.md` §8.1 的 `HandleVictoryConfirm` 已补充 bool 接收 + 失败 Toast。

**文档变更**：SG_TDD_04 §8.1 HandleVictoryConfirm 更新

---

### WX-010 ✅ 已接受并修正

**回应**：硬编码确实是隐患。改为构造函数参数 `totalLevels`（默认值 5 保持 V1 兼容），ValidateData 使用 `_totalLevels` 字段。

**文档变更**：§2.2 构造函数签名 `SG_ProgressManager(ISaveSystem, int totalLevels = 5)` + ValidateData 使用 `_totalLevels`

---

### WX-011 ✅ 已接受并修正

**回应**：已在 Reload() 的 XML doc 中加上"⚠️ 仅在非战斗状态下调用"的约束说明。

**文档变更**：§2.2 Reload() XML 注释补充

---

## PK Round 3 — 攻方最终复审

### Round 2 回应评估

- WX-009: 🟢 满意 — HandleVictoryConfirm 已同步更新，闭环
- WX-010: 🟢 满意 — 构造函数参数注入 totalLevels，V2 加关时不会忘
- WX-011: 🟢 满意 — 注释清晰标注调用约束

### 新问题

**无新问题。** 所有关键的微信真机场景（存储失败、热启动、跨设备丢失、时序安全、数据校验）都已在文档中明确处理或标注为已知限制。文档质量足够开始编码。

> **PK 可以收敛。**

---

## PK 总结报告

| 维度 | 状态 |
|------|------|
| **PK 轮次** | 2 轮完成（Round 3 无新问题，确认收敛） |
| **总问题数** | 11 个（Round 1: 8 + Round 2: 3） |
| **全部回应** | 11/11 ✅ |
| **文档版本** | SG_TDD_03 v1.0 → v1.1 |
| **阻塞编码的问题** | 0 个（Round 1 的 2 个 🔴 已全部修正） |

### 最有价值的 Top 3 变更

1. **Save() try-catch + bool 返回**（WX-001）：防止真机存储失败导致崩溃，通知用户
2. **V2 升级路径明确标注**（WX-002）：wx.login → openid → 服务端 CRUD，独立开发者知道什么时候该做、怎么做
3. **热启动 Reload() 机制**（WX-005）：微信小游戏特有的后台恢复场景，很容易被 Unity 开发者忽略

### 遗留项

- 无阻塞遗留项
- V2 待办已在文档中标注：服务端云存储、异步写入、totalLevels 可配置化






