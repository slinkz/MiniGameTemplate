# PK 评审记录 — OBB 障碍物升级 TDD

> **目标文档**：docs/Agent/OBB_OBSTACLE_TDD.md
> **文档类型**：TDD
> **攻方角色**：资深 Unity 引擎架构师（专精碰撞检测、2D 数学、WebGL 平台限制）
> **守方角色**：软件架构师（专精 API 设计、可维护性、关注点分离）
> **开始时间**：2026-04-23 00:32
> **最大轮次**：3
> **PK 状态**：✅ 已收敛（Round 2）

---

## PK Round 1 — 攻方提问

### OBB-001 | 严重度 🔴高 | `CircleVsOBB` 碰撞命中时重复 `WorldToLocal` 调用
**涉及章节**：§3.4 `ObstacleCollisionMath.CircleVsOBB()`
**质疑**：`CircleVsOBB` 检测碰撞后调用 `GetOBBNormal(circleCenter, in obs)` 计算法线，而 `GetOBBNormal` 内部又会再次调用 `WorldToLocal`，在热路径上做了重复坐标变换。应该直接复用已算好的 `local` 变量在局部空间计算法线再旋转回世界。
**潜在风险**：碰撞密集场景下不必要的性能开销 + 降低对方案的信心。
**建议方向**：将法线计算内联到碰撞函数中，复用已有的 `local` 变量。
**状态**：✅ 已回应（Round 1）— 接受，重构 `CircleVsOBB` 内联法线计算，消除重复变换。(v1.2 修正)

---

### OBB-002 | 严重度 🔴高 | `UpdatePosition` OBB 版本缺具体实现
**涉及章节**：§3.3、§3.2
**质疑**：文档声明保留 `UpdatePosition`（BC-06），但没有给出 OBB 版本的实现代码。新结构下只需 `obs.Center = center;` 一行，但实施者可能模仿旧代码做不必要的计算。
**潜在风险**：实施者理解不一致。
**建议方向**：在 §3.3 中补充实现片段。
**状态**：✅ 已回应（Round 1）— 接受，补充 `UpdatePosition` 和 `UpdateTransform` 的实现片段。(v1.2 修正)

---

### OBB-003 | 严重度 🔴高 | `RotateVector` 辅助函数未定义
**涉及章节**：§3.7 ObstacleRegistrar 变更
**质疑**：§3.7 中使用了 `RotateVector(_collider.offset, transform.eulerAngles.z)` 但从未定义，项目中也不存在此函数。
**潜在风险**：实施者不知道如何实现，非零 offset 时碰撞区域会偏移。
**建议方向**：给出 `RotateVector` 的实现或说明可复用 `LocalDirToWorld`。
**状态**：✅ 已回应（Round 1）— 接受，在 §3.7 补充 `RotateVector` 内联实现。(v1.2 修正)

---

### OBB-004 | 严重度 🟡中 | `internal` 类与 `public` 方法的跨程序集可见性
**涉及章节**：§3.4
**质疑**：类为 `internal` 但方法为 `public`，不一致。更关键的是 `_Example` 和 `_Framework` 在不同程序集，`internal` 限制了未来扩展。
**潜在风险**：跨程序集访问限制。
**建议方向**：确认设计意图并在文档中说明。
**状态**：✅ 已回应（Round 1）— 接受，方法改为 `internal static`，补充说明 `internal` 是刻意设计。(v1.2 修正)

---

### OBB-005 | 严重度 🟡中 | `AddRect`/`UpdateTransform` 缺实现片段，Sin/Cos 预计算时机不明
**涉及章节**：§3.3
**质疑**：文档只给签名不给实现，特别是 Sin/Cos 预计算可能被 `UpdateTransform` 遗漏。
**潜在风险**：`UpdateTransform` 忘记更新 Sin/Cos。
**建议方向**：补充关键实现片段。
**状态**：✅ 已回应（Round 1）— 接受，补充 `AddRect` 和 `UpdateTransform` 关键实现。(v1.2 修正)

---

### OBB-006 | 严重度 🟡中 | 内存布局未考虑运行时对齐
**涉及章节**：§3.2
**质疑**：不同运行时/平台可能有 8 字节对齐，实际可能 40 bytes/obstacle。
**潜在风险**：低，+256 bytes 不影响设计决策。
**建议方向**：加一句对齐注释。
**状态**：✅ 已回应（Round 1）— 接受，补充对齐说明注释。(v1.2 修正)

---

### OBB-007 | 严重度 🟡中 | Phase 6 扇形 vs OBB 缺实现伪代码
**涉及章节**：§3.5、BC-04
**质疑**：Phase 6 的变更描述模糊，没有伪代码。实施者可能困惑于坐标空间选择。
**潜在风险**：距离计算的坐标空间歧义。
**建议方向**：补充伪代码，明确"距离在局部空间做，角度仍用世界空间中心点"。
**状态**：✅ 已回应（Round 1）— 接受，在 §3.5 补充 Phase 6 OBB 适配伪代码。(v1.2 修正)

---

### OBB-008 | 严重度 🟡中 | `lossyScale` 问题 + R6 代码不一致
**涉及章节**：§3.7
**质疑**：1) `lossyScale` 在有旋转父级时不准确；2) R6 风险表提到 `Mathf.Abs` 但代码片段没写。
**潜在风险**：碰撞尺寸不准确 + 文档内部不一致。
**建议方向**：代码补 `Mathf.Abs()`，添加限制说明。
**状态**：✅ 已回应（Round 1）— 接受，代码补 `Mathf.Abs` + 加注释说明不支持旋转父级。(v1.2 修正)

---

### OBB-009 | 严重度 🟡中 | `RayVsOBB` 的 `dir` 单位向量前提未文档化
**涉及章节**：§3.4、BC-03
**质疑**：BC-03 的"t 值在世界空间有效"论述依赖 `dir` 是单位向量的前提，但文档未明确声明。
**潜在风险**：多次反射后浮点漂移。
**建议方向**：在 BC-03 补充前提条件。
**状态**：✅ 已回应（Round 1）— 接受，在 BC-03 补充前提假设。(v1.2 修正)

---

### OBB-010 | 严重度 🟡中 | `Update` 中每帧无条件调 `UpdateTransform` 的性能问题
**涉及章节**：§3.7
**质疑**：静态障碍物每帧也要 `Mathf.Sin/Cos`，不必要。
**潜在风险**：违反零不必要开销设计哲学。
**建议方向**：检查旋转是否变化，只变化时才调 `UpdateTransform`。
**状态**：✅ 已回应（Round 1）— 接受，`Update` 中增加变化检测逻辑。(v1.2 修正)

---

### OBB-011 | 严重度 🟡中 | `internal` 类缺少 `InternalsVisibleTo` 配置，无法被测试覆盖
**涉及章节**：§3.4、§6
**质疑**：`internal` 类在测试程序集中不可见，且 AC 中无单元测试项。
**潜在风险**：核心碰撞数学缺少自动化验证。
**建议方向**：添加 `InternalsVisibleTo` + 在实施计划中加测试步骤。
**状态**：✅ 已回应（Round 1）— 部分接受。`InternalsVisibleTo` 作为实施备注加入；单元测试作为遗留项记录（与现有项目基础设施状态一致）。(v1.2 修正)

---

### OBB-012 | 严重度 🟢低 | BEFORE 内存大小注释 "4(bytes)" 表述歧义
**涉及章节**：§3.2
**质疑**：注释中 "4(bytes)" 可能让人困惑——是 4 个 byte 字段合计 4 bytes。
**潜在风险**：无。
**建议方向**：改为 `4×1(Faction+Phase+pad×2)` 更精确。
**状态**：✅ 已回应（Round 1）— 接受，澄清注释。(v1.2 修正)

---

### OBB-013 | 严重度 🟢低 | Tuple swap 语法与现有代码风格不一致
**涉及章节**：§3.4
**质疑**：TDD 用 `(t1, t2) = (t2, t1)` 但现有代码用传统 swap。
**潜在风险**：无功能风险，代码审阅噪声。
**建议方向**：统一为传统 swap 风格。
**状态**：❌ 已拒绝（Round 1）— 项目 LangVersion=9.0，tuple swap 是合法的现代 C# 写法。新代码不必迁就旧风格，实施时自由选择。这不阻塞实施。

---

### OBB-014 | 严重度 🟢低 | BoxCollider2D 对 Physics2D 引擎的隐性同步开销
**涉及章节**：§3.7、§8
**质疑**：`Physics2D.autoSyncTransforms` 默认开启，64 个 Collider2D 有同步开销。
**潜在风险**：低，64 个可忽略。
**建议方向**：风险表加一条，考虑 `Physics2D.simulationMode = Script`。
**状态**：✅ 已回应（Round 1）— 接受，新增 R7 风险项。(v1.2 修正)

---

### OBB-015 | 严重度 🟢低 | Gizmo 旋转绘制缺异常安全
**涉及章节**：§3.8
**质疑**：`DrawCube` 和 `matrix = identity` 之间如果异常，matrix 会残留。
**潜在风险**：极低。
**建议方向**：可以不改，标准 Unity Gizmo 写法。
**状态**：❌ 已拒绝（Round 1）— 这是 Unity 社区标准 Gizmo 模式，OnDrawGizmos 中异常概率趋近于零。过度防御会增加代码复杂度。不改。

---

## Round 1 守方回应摘要

| 分类 | 数量 |
|------|------|
| ✅ 接受并修正 | 12 |
| ❌ 拒绝 | 2（OBB-013、OBB-015） |
| ✅ 部分接受 | 1（OBB-011） |

---

## PK Round 2 — 攻方复审

### Round 1 回应评估

| 问题 | 判定 | 理由 |
|------|------|------|
| OBB-001 | 🟢 满意 | CircleVsOBB 已内联法线计算，复用 local 变量，消除重复 WorldToLocal |
| OBB-002 | 🟢 满意 | UpdatePosition/UpdateTransform 实现片段已补充，语义清晰 |
| OBB-003 | 🟢 满意 | RotateVector 已内联定义，标准 2D 旋转实现 |
| OBB-004 | 🟢 满意 | 方法统一 internal static + 设计意图说明 |
| OBB-005 | 🟢 满意 | AddRect/UpdateTransform 关键赋值已补充，Sin/Cos 预计算无歧义 |
| OBB-006 | 🟢 满意 | 对齐说明含 8 字节最坏估计，结论仍可忽略 |
| OBB-007 | 🟢 满意 | Phase 6 伪代码明确坐标空间选择 |
| OBB-008 | 🟢 满意 | 代码补 Mathf.Abs + 旋转父级限制注释 |
| OBB-009 | 🟢 满意 | BC-03 前提假设已补充 |
| OBB-010 | 🟢 满意 | _lastRotZ 变化检测，静态障碍物零三角函数 |
| OBB-011 | 🟡 部分解决 | InternalsVisibleTo 已加入备注，单元测试作为遗留项。pragmatic 决策合理，不再追问 |
| OBB-012 | 🟢 满意 | 注释已澄清 |
| OBB-013 | 🟢 满意 | 拒绝合理——LangVersion=9.0，tuple swap 合法 |
| OBB-014 | 🟢 满意 | R7 风险项已添加 |
| OBB-015 | 🟢 满意 | 拒绝合理——标准 Unity Gizmo 模式 |

### Round 2 新问题

---

### OBB-016 | 严重度 🟡中 | Phase 6 伪代码调用了 `private` 的 `ClampLocal`
**涉及章节**：§3.5 Phase 6 伪代码、§3.4 `ObstacleCollisionMath`
**质疑**：Phase 6 伪代码直接调用 `ClampLocal(localOrigin, obs.HalfExtents)`，但 `ClampLocal` 是 `private static`。`CollisionSolver` 无法访问。
**潜在风险**：实施者按伪代码写会编译报错，或被迫破坏工具类封装。
**建议方向**：在 `ObstacleCollisionMath` 新增 `DistanceSqToOBB` 封装方法。
**状态**：✅ 已回应（Round 2）— 接受，新增 `DistanceSqToOBB` internal 方法，Phase 6 伪代码改用该方法。(v1.3 修正)

---

### OBB-017 | 严重度 🟡中 | `Update()` 中 `worldCenter` 计算不完整
**涉及章节**：§3.7 ObstacleRegistrar
**质疑**：Update 伪代码中 `worldCenter` 变量未展示完整计算，特别是 offset 非零时在不同分支的处理逻辑不清。
**潜在风险**：实施者对 worldCenter 计算理解不一致。
**建议方向**：补充 Update 中 worldCenter 的完整计算。
**状态**：✅ 已回应（Round 2）— 接受，补充完整 Update 伪代码：offset 零时直接用 position（零三角函数），非零时用 RotateVector。(v1.3 修正)

---

### OBB-018 | 严重度 🟢低 | `ConeAngle` 半角/全角语义不明
**涉及章节**：§3.5 Phase 6 伪代码
**质疑**：`ConeAngle` 在伪代码中无注释说明是半角还是全角。
**潜在风险**：极低，现有代码已是同写法。
**建议方向**：加一句注释。
**状态**：✅ 已回应（Round 2）— 接受，Phase 6 伪代码角度检查行加注释 `// ConeAngle 是半角（弧度）`。(v1.3 修正)

---

## Round 2 守方回应摘要

| 分类 | 数量 |
|------|------|
| ✅ 接受并修正 | 3（OBB-016、OBB-017、OBB-018） |
| ❌ 拒绝 | 0 |

---

## PK 收敛判定

### 收敛条件检查

| 条件 | 状态 |
|------|------|
| 🔴 高严重度问题全部解决 | ✅ Round 1 的 3 个 🔴（OBB-001/002/003）全部修正 |
| 🟡 中严重度问题全部解决或有明确理由 | ✅ 全部修正或部分接受（OBB-011 有 pragmatic 理由） |
| Round 2 无新 🔴 问题 | ✅ Round 2 仅 2🟡 + 1🟢，全部修正 |
| 攻方确认可以开始实施 | ✅ 攻方明确表示"PK 可以收敛" |

### 结论

**✅ PK 收敛——文档已足够好可以开始编码实施。**

**统计**：
- Round 1：15 问题（3🔴 + 8🟡 + 4🟢）→ 12 接受、2 拒绝、1 部分接受
- Round 2：3 问题（0🔴 + 2🟡 + 1🟢）→ 3 接受
- 总计 18 个问题，2 轮收敛
- TDD 版本 v1.0 → v1.3

**遗留项**（不阻塞实施）：
- 单元测试：OBB 碰撞数学关键角度自动化测试，待测试基础设施建立后补充


