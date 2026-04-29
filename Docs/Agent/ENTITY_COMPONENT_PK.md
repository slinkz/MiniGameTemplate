# PK 评审记录 — Entity-Component TDD v2.0

> **目标文档**：Docs/Agent/ENTITY_COMPONENT_TDD.md
> **文档类型**：TDD
> **攻方角色**：Unity 架构师（10+ 年 Unity 引擎开发经验，专精渲染管线、ECS 模式、WebGL 平台限制、微信小游戏性能优化）
> **守方角色**：软件架构师（专精系统设计、DDD、API 设计、可维护性和关注点分离）
> **开始时间**：2026-04-26 00:42
> **最大轮次**：3
> **PK 状态**：✅ 已收敛（Round 2 结束）

---

## Round 1 — 攻方（Unity 架构师）

### 问题清单

| ID | 严重度 | 标题 | 涉及章节 |
|----|--------|------|----------|
| EC-001 | 🔴高 | BC-02.1 的 `Init(Entity, config)` 签名与 §3.2 接口定义不一致 | §二 BC-02.1 vs §3.2 |
| EC-002 | 🔴高 | CollisionComponent 未保存 RegisterTarget 返回的槽位索引，动态注册策略缺乏可编码方案 | §3.5, §3.9 |
| EC-003 | 🔴高 | EntityEventBus "零 GC" 声明与 Delegate 数组实现存在矛盾 | §3.4, §七 |
| EC-004 | 🟡中 | Entity.GetComponent\<T\>() O(1) 查询的实现策略未说明 | §二 BC-01.2, §3.2 |
| EC-005 | 🟡中 | EntityManager.Despawn 中 `_activeEntities.Remove(entity)` 是 O(n) 操作 | §3.7 |
| EC-006 | 🟡中 | PierceHitMask 与动态注册策略的冲突 | §3.5, §3.9 |
| EC-007 | 🟡中 | Entity 目录结构中 EntityEventBus.cs 出现在两个位置 | §3.1 |
| EC-008 | 🟡中 | BC-01.6 复用 BulletFaction 枚举但语义存在偏差 | §二 BC-01.6, §五 5.1 |
| EC-009 | 🟡中 | Tick 管线时序的依赖关系描述不够精确 | §二 BC-06.4 |
| EC-010 | 🟡中 | CollisionComponent 在池化场景下有接口引用泄漏风险 | §3.5 |
| EC-011 | 🟡中 | EntityPool 预分配 maxCapacity 全量 Entity 的内存开销未评估 | §3.6, §五 5.1 |
| EC-012 | 🟢低 | AutoAimComponent 依赖未定义的 API `FindEntitiesInRadius()` | §4.6 |
| EC-013 | 🟢低 | 缺少 Entity Tick 期间 Despawn 的安全策略 | §3.7, §二 BC-06 |
| EC-014 | 🟢低 | 状态标签 BitFlags uint64 的 64 种上限及互斥规则表性能 | §4.1 |
| EC-015 | 🟢低 | Phase 1 验收标准过于模糊——多处只有"编译通过" | §六 Phase 1 |

### 整体评价

TDD v2.0 在架构分层（行为契约层 vs 技术方案层）、与现有弹幕系统的集成设计、以及 Luban 配置驱动方面做得相当扎实，整体可读性和结构质量在**中上水平**；但在三个关键实现细节上存在阻塞性歧义（接口签名不一致 EC-001、EventBus 零 GC 可行性 EC-003、动态注册策略缺乏可编码方案 EC-002），需要在开始编码前解决后方可安全启动 Phase 1。

---

## Round 1 — 守方（软件架构师）回应

TDD 已更新为 **v2.1**（2026-04-26）。以下是逐条回应：

### 🔴高优问题回应

| ID | 判定 | 回应摘要 | TDD 变更 |
|----|------|----------|----------|
| EC-001 | ✅ 接受 | 统一为 `Init(Entity owner)` 单参数，组件通过 `owner` 间接访问配置 | BC-02.1 描述修正；§3.2 补充说明 |
| EC-002 | ⚠️ 部分接受 | Phase 1 场景 ≤16 Entity，直接全部注册；Phase 2+ 启用动态策略并补充伪代码 | §3.9 分阶段策略 + CollisionRegistrationPass 伪代码 |
| EC-003 | ✅ 接受 | 改为预分配二维数组 `Delegate[16,4]`，替代 Delegate.Combine；补充 TypeId<T> 实现 | §3.4 完整重写 |

### 🟡中优问题回应

| ID | 判定 | 回应摘要 | TDD 变更 |
|----|------|----------|----------|
| EC-004 | ✅ 接受 | 新增 `ComponentType` 枚举 + 固定数组索引实现 O(1) | §3.2 补充枚举 + GetComponent 实现 |
| EC-005 | ✅ 接受 | 改为延迟销毁 + swap-remove O(1) | §3.7 完整重写 |
| EC-006 | ⚠️ 接受但降级为🟢低 | Phase 1 不启用动态注册，问题不触发；§3.9 补充 PierceHitMask 风险说明 | §3.9 新增风险段落 |
| EC-007 | ✅ 接受 | 移除 Core/ 下的重复项 | §3.1 目录修正 |
| EC-008 | ⚠️ 部分接受 | Phase 1 继续复用 BulletFaction；新增 §3.11 阵营扩展预留 | BC-01.6 修订 + §3.11 新增 |
| EC-009 | ✅ 接受作为已知限制 | 1 帧延迟在小游戏场景可接受，补充量化评估 | BC-06.4 标注 + §3.12 新增 |
| EC-010 | ✅ 接受 | 补充 IsAlive 检查 + 注销防护 | §3.9 新增安全防护段落 |
| EC-011 | ✅ 接受 | 补充单 Entity 内存估算（~832B）+ 总内存预算 < 2MB | §3.13 新增 + §七 新增行 |

### 🟢低优问题回应

| ID | 判定 | 回应摘要 | TDD 变更 |
|----|------|----------|----------|
| EC-012 | ✅ 接受 | §3.7 补充 `FindEntitiesInRadius` 签名预留（含预分配缓冲区参数） | §3.7 新增方法 |
| EC-013 | ✅ 接受 | 与 EC-005 合并为延迟销毁方案 | §3.7 延迟销毁模式 |
| EC-014 | ✅ 接受 | 封装 `StateMask` 值类型 + 预计算互斥矩阵 | §4.1 修订 |
| EC-015 | ✅ 接受 | 每步 AC 强化为功能性验收（含 Profiler/功能检查） | §六 Phase 1 完整重写 |

### 守方总结

15 个问题中：
- **13 个完全接受**，TDD 已同步修改
- **2 个部分接受**（EC-002 分阶段实施、EC-008 延后新枚举引入），给出了明确的触发条件和扩展路径
- **0 个驳回**

TDD 从 v2.0 升级至 v2.1，主要变更：
1. Init 签名统一 + ComponentType 枚举（EC-001/004）
2. EventBus 零 GC 实现细化（EC-003）
3. EntityManager 延迟销毁 + swap-remove（EC-005/013）
4. 碰撞注册分阶段策略 + 安全防护（EC-002/006/010）
5. 新增 §3.11~§3.13（阵营预留/时序说明/内存预算）
6. Phase 1 验收标准全面强化（EC-015）

---

## Round 2 — 攻方（Unity 架构师）复审

### Round 1 问题复核

| ID | 原严重度 | 判定 | 复核意见 |
|----|----------|------|----------|
| EC-001 | 🔴 | ✅ 已解决 | BC-02.1 和 §3.2 已统一为 `Init(Entity owner)` 单参数，签名一致性问题消除 |
| EC-002 | 🔴 | ✅ 已解决 | §3.9 分阶段策略清晰：Phase 1 全注册（≤16 约束明确），Phase 2+ 补充了完整伪代码 |
| EC-003 | 🔴 | ✅ 已解决 | §3.4 完整重写，预分配 `Delegate[16,4]` + `TypeId<T>` 泛型静态字段，方案完整可行 |
| EC-004 | 🟡 | ✅ 已解决 | `ComponentType` 枚举 + 固定数组索引实现 O(1)，设计合理 |
| EC-005 | 🟡 | ✅ 已解决 | §3.7 延迟销毁 + swap-remove O(1)，Entity 内部记录 `ActiveListIndex`，方案干净 |
| EC-006 | 🟡 | ✅ 已解决 | §3.9 明确标注 PierceHitMask 冲突风险 + 两条缓解方案，Phase 1 不触发 |
| EC-007 | 🟡 | ✅ 已解决 | 目录中 EntityEventBus.cs 现只出现在 Events/ 下 |
| EC-008 | 🟡 | ✅ 已解决 | BC-01.6 修订 + §3.11 扩展预留，触发条件明确 |
| EC-009 | 🟡 | ✅ 已解决 | BC-06.4 标注已知限制 + §3.12 量化评估（30fps ~2px，60fps ~4px），结论合理 |
| EC-010 | 🟡 | ✅ 已解决 | §3.9 多层防护充分：Reset 静默跳过 + IsAlive + 现有 null 检查 |
| EC-011 | 🟡 | ✅ 已解决 | §3.13 内存预算估算合理，200 Entity ~163KB，< 2MB 目标 |
| EC-012 | 🟢 | ✅ 已解决 | §3.7 补充 `FindEntitiesInRadius` 签名（含预分配缓冲区） |
| EC-013 | 🟢 | ✅ 已解决 | 与 EC-005 合并的延迟销毁方案逻辑安全 |
| EC-014 | 🟢 | ✅ 已解决 | `StateMask` 值类型封装 + 预计算互斥矩阵，可扩展 |
| EC-015 | 🟢 | ✅ 已解决 | Phase 1 每步 AC 均强化为功能性验收标准 |

### 新发现问题

#### EC-016 | 🟢低 | §3.5 RegisterTarget 返回值语义与 §3.9 描述不一致

**涉及章节**：§3.5, §3.9

**质疑**：§3.5 调用 `DanmakuSystem.Instance.RegisterTarget(this)` 返回 `bool`，但 §3.9 描述说"注册失败（返回 -1）"——`-1` 是底层 `TargetRegistry.Register()` 的语义。§3.5 伪代码也缺少失败处理分支。

**潜在风险**：实现时 API 层级混淆；`_targetSlot` 字段声明但永远不被赋值。

**建议方向**：§3.5 伪代码明确用哪层 API + 补充失败分支。

#### EC-017 | 🟢低 | §3.9 安全防护假设 ClearAll 调用 FreeAll 与代码不符

**涉及章节**：§3.9

**质疑**：§3.9 写道"DanmakuSystem.ClearAll() 已调用 TargetRegistry.FreeAll()"，但实际代码注释明确写"不清除 TargetRegistry"。

**潜在风险**：场景切换时如依赖 ClearAll 清空 TargetRegistry 则不生效。CollisionComponent.Reset() 主动注销是实际主防线。

**建议方向**：修正描述，删除错误假设。

### Round 2 整体评价

TDD v2.1 对 Round 1 的 15 个问题做出了系统性、高质量的回应。3 个高优问题全部彻底解决，8 个中优全部解决，4 个低优全部落实。新引入的设计未产生阻塞性问题。仅发现 2 个低优级指引性偏差，不阻塞编码。

**结论：TDD v2.1 已达到"可安全启动 Phase 1 编码"的状态。**

---

## Round 2 — 守方（软件架构师）回应

### EC-016 回应

| 判定 | ✅ 接受 |
|------|---------|

**处理**：§3.5 伪代码改为直接调用 `ds.TargetRegistry.Register(this)` 获取 `int` 槽位索引，补充失败分支（LogWarning + 标记 `_isCollisionEnabled = false`）。Reset 中增加 `_targetSlot >= 0` 前置检查。

### EC-017 回应

| 判定 | ✅ 接受 |
|------|---------|

**处理**：§3.9 删除"DanmakuSystem.ClearAll() 已调用 TargetRegistry.FreeAll()"的错误假设。修正为：ClearAll 不清除 TargetRegistry（代码明确标注），CollisionComponent.Reset() 主动注销是唯一清理路径；场景切换时 EntityManager 需遍历所有池化 Entity 执行 Reset。

---

## PK 收敛总结

### 统计

| 指标 | 数值 |
|------|------|
| 总轮次 | 2 |
| 总问题数 | 17（EC-001 ~ EC-017） |
| 🔴 高优 | 3 → Round 2 全部 ✅ 已解决 |
| 🟡 中优 | 8 → Round 2 全部 ✅ 已解决 |
| 🟢 低优 | 6（Round 1: 4 + Round 2: 2）→ 全部 ✅ 已解决 |
| 完全接受 | 15 / 17 |
| 部分接受 | 2 / 17（EC-002 分阶段、EC-008 延后扩展） |
| 驳回 | 0 |
| 最终 🔴 | 0 |

### TDD 版本演进

| 版本 | 主要变更 |
|------|----------|
| v2.0 | 初始 TDD，行为契约/技术方案分层 |
| v2.1 | Round 1 修订：Init 签名统一、ComponentType 枚举、EventBus 零 GC 重写、延迟销毁、碰撞分阶段策略、§3.11~3.13 新增 |
| v2.1+ | Round 2 修订：CollisionComponent API 层级明确、ClearAll 假设修正 |

### 最终结论

**TDD v2.1 已达到"可安全启动 Phase 1 编码"的状态。**

关键成果：
1. 3 个阻塞性高优问题全部解决——接口签名统一、EventBus 零 GC 方案可行、碰撞注册策略可编码
2. 验收标准从"编译通过"强化为功能性 AC（含 Profiler GC=0 验证）
3. 新增内存预算、时序说明、阵营扩展预留等关键架构决策
4. 与现有弹幕系统代码的接口对齐已验证（TargetRegistry / ICollisionTarget / CollisionSolver）

**建议下一步**：按 Phase 1 步骤 P1.1 → P1.9 顺序启动编码。

---

> PK 评审结束时间：2026-04-26 15:15




