# PK 评审记录 — FLOATING_TEXT_TDD

> **目标文档**：`Docs/Agent/FLOATING_TEXT_TDD.md`
> **文档类型**：TDD
> **攻方角色**：Unity 架构师（10 年+ Unity 引擎经验，专精渲染管线、WebGL 平台限制、ScriptableObject 驱动设计）
> **守方角色**：软件架构师（系统设计、解耦、数据驱动架构、迁移方案设计）
> **开始时间**：2026-06-03 09:45
> **PK 状态**：✅ 已收敛（3 轮，10 问题全部回应）
> **最大轮次**：8
> **攻方 Skill**：coding-standards（MiniGameTemplate C# 编码规范）

---

## PK Round 1 — 攻方提问（Unity 架构师）

### UA-001 | 严重度 🔴高 | §9 验收章节不符合两层验收体系

**涉及章节**：§9 验收标准
**质疑**：TDD §9 的验收标准混合了 Phase 门禁验收和全局集成验收，未按 coding-standards 中"验收分层设计原则"拆分。例如 F-1~F-5 需要 Play Mode 观察（当前环境可执行），但 F-7（暂停时飘字继续消散）需要实际暂停操作，P-3（Frame Debugger DC 检查）需要专用工具。所有验收项以扁平列表呈现，缺少"Phase 门禁验收"和"全局集成验收"的明确分层。
**潜在风险**：按照 Sprint 3 复盘经验（PIT 来源），混合的验收列表会导致编码者在当前 Sprint 环境下无法完成部分验收项，浪费时间创造验收条件（如启动 Frame Debugger 做 DC 验收应放在全局集成验收而非 Phase 门禁）。
**建议方向**：将 §9 重构为两层结构——Phase 门禁验收（只含编译+MCP/Editor 脚本可验的阻塞项）和全局集成验收（Play Mode 视觉+性能+真机），按 coding-standards 模板格式。

### UA-002 | 严重度 🔴高 | §13 系统退役必须物理删除 vs §8 Phase 3 的 `[Obsolete]` 策略冲突

**涉及章节**：§8.Phase 3 步骤 3.10 + §11 废弃清理
**质疑**：coding-standards §13 明确规定"系统退役必须物理删除"，禁止"先标 Obsolete 以后再删"。但 TDD §8 Phase 3 步骤 3.10 写道 `EntitySystemBootstrap.DamageNumberPool Inspector 字段标记 [Obsolete]（不立即删除，避免序列化丢失告警）`，然后 Phase 4 才真正删除。这是一个跨两个 Phase 的"先标后删"策略，直接违反 coding-standards §13 铁律。
**潜在风险**：Phase 3 编译通过后，如果因故未执行 Phase 4（中断/分支合并等），`[Obsolete]` 字段将永久残留，旧对象池引用仍在 .asset 中序列化，下游 AI 或开发者可能继续读取旧字段（PIT-051 踩坑经验）。
**建议方向**：Phase 3 和 Phase 4 合并为一个原子步骤：迁移调用方 → 清空 .asset 引用 → 物理删除旧字段/旧文件 → 编译验证。或者将 Phase 4 定义为 Phase 3 的必选尾步（同一个编译门禁周期内）。

### UA-003 | 严重度 🟡中 | §6.3 DOT 飘字路径引入 `DanmakuSystem.Instance` 单例访问

**涉及章节**：§6.3 DOT 飘字 + §6.4 依赖注入路径
**质疑**：§6.4 表格明确"选择方案 B（构造注入）"为最优方案。但 §6.3 DOT 飘字改造代码中写的是 `var danmaku = DanmakuSystem.Instance;`，走的是方案 A（单例访问）。同一文档内两处依赖注入路径不一致。
**潜在风险**：coding-standards §3 和 §7 要求避免单例/Find 等全局访问模式。DOT 飘字在 BattleController 中走单例，而普攻飘字在 EntityHitReactionHandler 中走构造注入——两条路径共存增加了耦合度和排查复杂度。
**建议方向**：统一走构造注入。BattleController 已持有 `_entityBootstrap` 引用，可以在 Bootstrap 上新增 `public FloatingTextSystem FloatingText` 只读属性，DOT 飘字改为 `_entityBootstrap.FloatingText?.Spawn(...)`，完全避免单例访问。

### UA-004 | 严重度 🟡中 | §14 编码规范冲突：`EntityHitReactionHandler` 用 TextMesh 做飘字是 P0 铁律

**涉及章节**：§2 ADR-036 + coding-standards §14
**质疑**：coding-standards §14 PIT-053 明确规定"伤害飘字禁止 FairyGUI"并指定"必须用框架层 `EntityHitReactionHandler` 的 TextMesh 对象池（世界空间）"。TDD 的核心决策是从 TextMesh 迁移到 RBM。这意味着 **执行完此 TDD 后，coding-standards §14 的规定将过时**（它指定的实现方案被废弃了）。但 TDD §11.2 的文档更新清单中没有提到更新 coding-standards §14。
**潜在风险**：TDD 完成后，§14 仍然写着"必须用 EntityHitReactionHandler 的 TextMesh 对象池"，但这个系统已被删除。下次 AI 读到 §14 时会产生矛盾指令。
**建议方向**：在 §11.2 文档更新清单中增加一条：更新 coding-standards §14 为"伤害飘字必须走 FloatingTextSystem（RBM 渲染），禁止 FairyGUI/TextMesh"。

### UA-005 | 严重度 🟡中 | §5.1 数据结构声称"与原 DamageNumberData 的差异：无"但实际 Color 字段处理不同

**涉及章节**：§5.1 FloatingTextData
**质疑**：§5.1 末尾声称"与原 DamageNumberData 的差异：无（字段完全相同，仅重命名类型 + 迁移命名空间）"。但现有 `DamageNumberData.Color` 字段是在 `Spawn()` 方法内部赋值的（由 `isCritical` 硬编码颜色），而新系统的 `Color` 由调用方显式传入。虽然 struct 字段确实相同，但数据填充的语义完全不同。这个"差异：无"的表述会误导实施者认为数据流没有变化。
**潜在风险**：实施者可能认为只需要改文件名和命名空间就够了，忽略了 `Spawn` 方法内部颜色赋值逻辑的重构。
**建议方向**：修正为"字段定义相同，但 `Color` 字段的赋值语义变更：由系统内部硬编码→调用方显式传入"。

### UA-006 | 严重度 🟢低 | §4.1 FloatingTextColors 用 `static readonly` 而非 SO 暴露

**涉及章节**：§4.4 预定义颜色常量
**质疑**：`FloatingTextColors` 使用 `static readonly Color32` 硬编码颜色值。按 coding-standards §3 的 ScriptableObject 驱动设计原则，面向设计师可调的数据应存 SO。颜色属于视觉调参，频繁需要在 Inspector 中微调（"暴击金的亮度不够"/"DOT 紫色看不清"——TDD R-2 自己也提到了这个风险）。
**潜在风险**：每次调整颜色需要改代码+重编译，设计师无法在编辑器中实时预览效果。
**建议方向**：将颜色定义从代码常量迁移到 SO 配置（比如直接放在 `DanmakuRenderConfig` 中增加 `FloatingTextColors` 字段），代码中的 `static class` 作为 fallback 默认值。可标注为"编码期间迭代"。

### UA-007 | 严重度 🟢低 | §3.2 文件布局"重命名"描述不准确

**涉及章节**：§3.2 文件布局
**质疑**：§3.2 表格第一行写的是"操作=重命名，旧路径=DamageNumberSystem.cs → 新路径=FloatingTextSystem.cs"。但 §8 Phase 1 步骤 1.1 实际操作是"复制"而非"重命名"（`DamageNumberSystem.cs` 在 Phase 4 才删除）。§3.2 的"重命名"与 §8 的"复制→删除"不一致。
**潜在风险**：如果实施者按 §3.2 直接重命名文件（git mv），Phase 2 中 DanmakuSystem.Runtime.cs 仍引用 `DamageNumberSystem` 类型时会编译失败（因为旧类已不存在）。§8 的"复制→两套并存→切换→删旧"策略是正确的，但 §3.2 的描述应一致。
**建议方向**：§3.2 表格的操作列改为"复制+删旧（§8 Phase 1+4）"以准确反映实际步骤。

---

> **Round 1 严重度分布**：🔴×2 + 🟡×3 + 🟢×2 = 共 7 个问题

---

## PK Round 1 — 守方回应（软件架构师）

| ID | 严重度 | 状态 | 回应摘要 |
|----|--------|------|----------|
| UA-001 | 🔴高 | ✅ 已回应 | §9 重构为两层验收体系（Phase 门禁 G-1~G-11 + 全局集成 I-1~I-10），格式符合 coding-standards 模板 |
| UA-002 | 🔴高 | ✅ 已回应 | Phase 3+4 合并为原子步骤（3.1~3.16）。迁移→清空引用→物理删除→编译验证在同一门禁周期内完成。R-4 风险策略同步更新 |
| UA-003 | 🟡中 | ✅ 已回应 | §6.3 DOT 飘字从 `DanmakuSystem.Instance`（方案 A）改为 `_entityBootstrap.FloatingText?.Spawn()`（方案 B）。§6.4 表格中方案 A 标记为 ❌ 不推荐。Bootstrap 新增 `FloatingText` 只读属性 |
| UA-004 | 🟡中 | ✅ 已回应 | §11.2 新增条目：更新 coding-standards §14 为"飘字必须走 FloatingTextSystem（RBM），禁止 FairyGUI/TextMesh 对象池" |
| UA-005 | 🟡中 | ✅ 已回应 | §5.1 修正为"字段定义相同，但 Color 赋值语义变更：系统硬编码→调用方显式传入" |
| UA-006 | 🟢低 | ✅ 已回应 | 在 §4.4 添加迭代项标注：v1.0 用 static readonly，编码期间可升级为 SO 配置，保留 static 值作 fallback |
| UA-007 | 🟢低 | ✅ 已回应 | §3.2 表格"重命名"改为"复制+删旧（§8 Phase 1+3）" |

**文档版本**：v1.0 → v2.0（7 处修正）

---

## PK Round 2 — 攻方复审（Unity 架构师）

### Round 1 回应评估

| ID | 评分 | 理由 |
|----|------|------|
| UA-001 | 🟢 满意 | §9 完美拆分为两层，G-1~G-11 Phase 门禁项全部可在编译/grep/MCP 环境下执行 |
| UA-002 | 🟢 满意 | Phase 3 合并为 16 步原子步骤，R-4 策略同步更新为"MCP 批量清空→物理删除" |
| UA-003 | 🟢 满意 | DOT 飘字改走 Bootstrap 属性，方案 A 标 ❌，注入路径完全统一 |
| UA-004 | 🟢 满意 | §11.2 新增 coding-standards §14 更新条目 |
| UA-005 | 🟢 满意 | "字段定义相同，但 Color 赋值语义变更"——准确 |
| UA-006 | 🟢 满意 | 迭代项标注清晰 |
| UA-007 | 🟢 满意 | "复制+删旧"准确反映两步操作 |

### Round 2 新问题

### UA-008 | 严重度 🟡中 | §3.3 FloatingTextSystem.Initialize 依赖 Danmaku 命名空间未验证 asmdef

**涉及章节**：§3.3 命名空间决策
**质疑**：TDD 承认 `FloatingTextSystem.Initialize` 需要 `using MiniGameTemplate.Danmaku`（因 `DanmakuRenderConfig`）。但未分析 asmdef 依赖——如果 Rendering 模块和 DanmakuSystem 在不同 asmdef 中，编译将失败（coding-standards §2）。
**潜在风险**：跨程序集引用缺失导致编译失败。
**建议方向**：§3.3 补充 asmdef 依赖分析结论。

### UA-009 | 严重度 🟡中 | §6.2 Entity 层 OnHit 代码缺少 Y 偏移

**涉及章节**：§6.2 Entity 层 OnHit 改动后代码
**质疑**：§10 R-5 建议"在 OnHit 调用 Spawn 时传入 `entity.Position + new Vector2(0, 0.5f)`"，但 §6.2 示例代码传入的是 `entity.Position`——文档内部矛盾。
**潜在风险**：实施者照抄 §6.2 代码，飘字从脚底弹出。
**建议方向**：§6.2 代码中 Spawn 位置参数加上 Y 偏移。

### UA-010 | 严重度 🟢低 | §6.3 DOT 飘字移除了 null 安全检查

**涉及章节**：§6.3 DOT 飘字改动后代码
**质疑**：改动前有 `_entityBootstrap != null` 检查，改动后代码移除了。`?.` 只防 `FloatingText` 为 null，不防 `_entityBootstrap` 为 null。
**潜在风险**：极端场景 NullReferenceException。
**建议方向**：保留 `_entityBootstrap != null` 判断。

---

> **Round 2 严重度分布**：🟡×2 + 🟢×1 = 共 3 个新问题
> **趋势**：🔴 2→0，🟡 3→2，总问题数 7→3，严重度单调下降
> **攻方收敛意见**：UA-008/009/010 全部解决后即可收敛，无阻塞项

---

## PK Round 2 — 守方回应（软件架构师）

| ID | 严重度 | 状态 | 回应摘要 |
|----|--------|------|----------|
| UA-008 | 🟡中 | ✅ 已回应 | §3.3 补充 asmdef 分析：所有涉及类型均在 `MiniGameFramework.Runtime` 同一 asmdef 内，无跨程序集问题 |
| UA-009 | 🟡中 | ✅ 已回应 | §6.2 代码 position 参数改为 `entity.Position + new Vector2(0, 0.5f)` |
| UA-010 | 🟢低 | ✅ 已回应 | §6.3 代码恢复 `_entityBootstrap != null` 判断 |

**文档版本**：v2.0 → v2.1（3 处修正）

---

## PK Round 3 — 攻方最终复审（Unity 架构师）

### Round 2 回应评估

| ID | 评分 | 理由 |
|----|------|------|
| UA-008 | 🟢 满意 | asmdef 分析结论明确——同一 asmdef 内，无跨程序集问题 |
| UA-009 | 🟢 满意 | §6.2 代码已加 `+ new Vector2(0, 0.5f)`，与 R-5 风险缓解一致 |
| UA-010 | 🟢 满意 | `_entityBootstrap != null` 检查已恢复 |

### Round 3 新问题

**无新问题。**

文档 v2.1 已解决所有 Round 1~2 提出的 10 个问题（2🔴 + 5🟡 + 3🟢），无遗留阻塞项。验收体系、退役策略、注入路径、代码示例均已自洽。

> **PK 收敛评估**：无新问题，PK 可以收敛。

---

## PK 总结报告

| 维度 | 状态 |
|------|------|
| **PK 轮次** | 3 轮完成（最大 8 轮内收敛） |
| **总问题数** | 10 个（Round 1: 7 + Round 2: 3） |
| **全部回应** | 10/10 ✅ |
| **文档版本** | v1.0 → v2.0 → v2.1 |
| **阻塞编码的问题** | 0 个 |
| **攻方收敛意见** | "无新问题，PK 可以收敛" |

**结论：PK 收敛。文档 v2.1 可以进入编码。**

收敛理由：
1. 🔴 高优问题从 Round 1 的 2 个降至 Round 2 的 0 个，严重度单调下降
2. Round 3 攻方无新问题
3. 所有 coding-standards 冲突均已修复（§9 验收分层 / §13 系统退役 / §14 飘字规范 / §2 asmdef）

### 最有价值的 Top 3 变更

1. **UA-001 — 验收分层**：§9 从扁平列表重构为两层体系（Phase 门禁 G-1~G-11 + 全局集成 I-1~I-10），直接避免了 Sprint 3 复盘中的验收条件创造浪费
2. **UA-002 — Phase 原子合并**：Phase 3+4 合并为原子步骤（16 步），消除了 `[Obsolete]` 过渡期风险，符合 coding-standards §13 铁律
3. **UA-003 — 注入路径统一**：DOT 飘字从单例访问改为 Bootstrap 属性注入，全局零单例访问，完全符合 ScriptableObject 驱动 + 构造注入哲学

### 遗留项

| 优先级 | 项目 | 处理方式 |
|--------|------|----------|
| 🟢低 | UA-006 颜色 SO 化 | 编码期间迭代，v1.0 先用 `static readonly` |






