# SG_UI_DESIGN 可执行性 PK 评审

> **攻方**：Unity 架构师 Agent（关注数据驱动、解耦、SO模式、组件单一职责）  
> **守方**：游戏设计师 Agent  
> **评审对象**：`SHOOTER_GAME/SG_UI_DESIGN.md` v1.0  
> **最大轮次**：6  
> **PK 状态**：✅ 已完成（4 轮收敛，10/10 问题关闭）

---

## PK Round 1 — 攻方提问（Unity 架构师）

> 作为 Unity 架构师，我从**数据驱动设计、系统解耦、SO 配置化、组件职责划分、FairyGUI 与 Entity 框架的集成边界**五个维度审视 UI 设计文档的可执行性。

---

### SGUI-001 | UI 界面生命周期与框架 UIManager 的关系

**质疑**：文档定义了 6 个界面和它们的流转关系（§一），但**完全没有提及与框架现有 UIManager / ScreenManager 的集成方式**。

**具体问题**：

1. 框架层是否已有 `IScreen` / `IPanel` 接口规范？如果有，文档中 6 个界面的**类命名**应与框架接口对齐——目前只定义了 FairyGUI 包名和组件名，没有 C# 层的 Screen/Panel 映射规范。
2. `LoadingScreen`（全屏过渡页）和 `BattleHUD`（常驻叠加层）明显是两种不同生命周期的 UI——一个是"显示→自动消失"，一个是"场景存续期间常驻"。框架层如何区分这两种类型？用同一个 `ShowScreen()` API 还是不同的方法？
3. 界面状态总览表（§六）的 **"冻结"** 状态——在 FairyGUI + Entity 框架上下文中，"冻结" 意味着什么？是停止 UI 更新（不响应数据变更通知）？还是 `TimeScale=0` 让动效暂停？两者实现完全不同。

**期望守方回答**：界面与框架 UIManager 的集成规范（命名、接口、生命周期分类）。

**状态**：🟡 待回应

---

### SGUI-002 | 数据绑定方式——FairyGUI 组件如何监听 SO 数据变更

**质疑**：GDD v3.1 已确定所有运行时数据走 ScriptableObject 变量（`IntVariable`、`FloatVariable`）。UI 文档描述了血条显示百分比、波次显示 "Wave 2/5"——但**没有说明 UI 组件如何订阅这些 SO 数据变更**。

**具体问题**：

1. `ProgressBar_HP` 血条组件需要监听 `FloatVariable`（基地当前 HP）的变更。FairyGUI 组件是 `GComponent`，不是 MonoBehaviour——**无法直接使用 `OnEnable/OnDisable` 订阅事件**。谁负责桥接？
2. 波次指示 "Wave 2/5"——当前波次索引从哪来？`EntitySpawner.CurrentWaveIndex` 是运行时状态，不是 SO。是否需要一个 `IntVariable` 来桥接 Spawner → UI？
3. 击杀计数 "25" / "18/25"——这个数据源是什么？GDD 里没有定义 `KillCount` 变量。是否需要新增 SO？

**期望守方回答**：FairyGUI ↔ SO 数据绑定的技术方案，以及缺失的 SO 变量清单。

**状态**：🟡 待回应

---

### SGUI-003 | 虚拟摇杆——输入系统边界与组件职责

**质疑**：虚拟摇杆的交互规范很详细（§3.5），但存在**架构职责不清**的问题。

**具体问题**：

1. **输入读取权**：虚拟摇杆组件（FairyGUI `Joystick`）处理触摸事件并产出方向向量。但 `MovementComponent`（Entity 框架层）需要消费这个方向向量。**跨层通信方式是什么？**
   - 方案 A：Joystick → 写入 `Vector2Variable` SO → MovementComponent 每帧读取
   - 方案 B：Joystick → 直接调用 `PlayerEntity.GetComponent<MovementComponent>().SetDirection()`
   - 方案 C：Joystick → 通过 `GameEvent` 广播方向变更
   
   方案 A 是数据驱动最佳实践；方案 B 是强耦合反模式；方案 C 对每帧方向变更来说事件粒度过细。**文档应明确选择哪种方案。**

2. **响应区域冲突**：文档说"全屏都是摇杆响应区"但"暂停按钮除外"。在 FairyGUI 的事件分发模型中，这意味着：暂停按钮必须**拦截触摸并阻止向下层穿透**。而 HUD 其他元素（波次文字、血条）声明为"不阻挡触摸穿透"。**FairyGUI 的层级设置和 `touchable` 属性配置方案是什么？**

3. **摇杆 vs 暂停按钮的首帧判定**：如果手指从暂停按钮区域开始按下然后拖动到摇杆区域，应该触发暂停还是摇杆？反之呢？

**期望守方回答**：输入 → Entity 的数据通路方案 + FairyGUI 触摸层级配置。

**状态**：🟡 待回应

---

### SGUI-004 | 转场动效的实现归属——FairyGUI Transition vs Unity 代码

**质疑**：文档定义了 7 种转场动效（§3.2），但没有说明**每种动效的实现层归属**。

**具体问题**：

1. "选关 → 战斗：选中关卡缩放放大 → 全屏白闪 → 战斗场景淡入" — 这跨越了**两个 Scene** 的边界。FairyGUI Transition 只能操作同一个 GRoot 下的对象——**跨场景白闪和淡入必须走 Unity 层**（Camera overlay / CanvasGroup 淡入）。文档应区分哪些动效由 FairyGUI 处理、哪些由 Unity 代码层处理。

2. "关卡缩放放大" 必须在场景卸载**之前**完成。这意味着转场流程是：
   ```
   LevelNode 缩放动画（FairyGUI）→ 动画完成回调 → 触发场景加载 → 白闪叠加层（Unity）→ 场景加载完毕 → 淡入
   ```
   这个**异步编排**是否有现成的框架支持？还是需要新建一个 `TransitionSequencer`？

3. **重试转场**："面板滑出 → 短暂黑屏 0.2s → 战斗重新开始"——"重新开始"是 `SceneManager.LoadScene(same)` 还是 `EntitySpawner.Reset()`（不重新加载场景）？两种实现对转场动效的归属完全不同。

**期望守方回答**：转场动效的实现层划分 + 重试时是否重载场景。

**状态**：🟡 待回应

---

### SGUI-005 | 血条预损动画的数据时序

**质疑**：§2.3 血条交互描述了"先扣白色预损，再扣实际血量，0.3s 延迟"。这是一个**双值动画**需求。

**具体问题**：

1. **数据源问题**：`FloatVariable` SO 只有一个 `Value`。预损动画需要**两个值**：`当前显示值`（白色段目标）和 `实际值`（绿色段目标）。当 HP 从 80→60→50 连续快速变化时：
   - 白色段是否应该"追赶"到 60 后再开始向 50 追赶？
   - 还是白色段直接跳到最新的"已确认损失"？
   
2. **实现归属**：这个双值追赶逻辑应该在哪里实现？
   - 方案 A：FairyGUI 组件内部维护 `displayValue` 和 `targetValue`，每帧 Lerp
   - 方案 B：再新增一个 `FloatVariable` 叫 `BaseHP_DisplayValue`，由一个 MonoBehaviour 驱动延迟追赶
   
   方案 A 简单但逻辑散落在 UI 代码里；方案 B 符合数据驱动但多一个 SO 显得 over-engineering。

3. **时间源**：暂停时 `TimeScale=0`，如果用 `Time.deltaTime` 驱动预损动画会冻住。是否需要 `Time.unscaledDeltaTime`？但文档说暂停时 HUD 也"冻结"——那预损动画冻住是否是预期行为？

**期望守方回答**：预损动画的双值追赶策略 + 暂停时行为。

**状态**：🟡 待回应

---

### SGUI-006 | 关卡节点三态——数据驱动 vs 硬编码

**质疑**：选关界面的 `LevelNode` 组件有三态（已通关/可进入/未解锁）。但文档没有说明**节点状态的数据源和刷新时机**。

**具体问题**：

1. **数据源**：GDD v3.1 §4.4 用 `SG_LevelConfigSO.UnlockRequirement` 定义解锁条件。那**运行时进度数据**（哪关通关了、哪关解锁了）存在哪里？
   - `ISaveSystem`（持久化）→ 反序列化后的 `LevelProgress[]`？
   - 还是一个 `RuntimeSet<int>` 记录已通关关卡？
   - 选关界面打开时如何从持久化数据刷新 5 个节点的状态？

2. **新解锁动效触发**：文档说"从战斗结果返回 → 新解锁关卡有'锁打开'动效"。这要求 UI 能**区分"首次变为可进入"和"本来就是可进入"**。谁负责维护这个 diff 状态？UI 层还是数据层？

3. **组件控制器归属**：FairyGUI `LevelNode` 组件通过 `Controller` 切换三态。那控制 Controller 状态切换的**逻辑代码**在哪个 MonoBehaviour 里？是否应该是一个 `LevelSelectScreenController : MonoBehaviour`（单一职责：读进度数据 → 设置 5 个节点 Controller 状态）？

**期望守方回答**：关卡进度数据来源 + 节点状态刷新逻辑的归属。

**状态**：🟡 待回应

---

### SGUI-007 | FairyGUI 包加载时机与内存预算

**质疑**：§5.1 定义了 5 个 FairyGUI 包，但没有说明**加载策略**。

**具体问题**：

1. **全量预加载 vs 按需加载**？
   - 如果全量预加载 5 个包：在微信小游戏首包限制（通常 20~30MB）内是否有压力？FairyGUI 包编译后的 bin 文件大小预估是多少？
   - 如果按需加载：Battle 包在战斗场景加载时才加载，选关→战斗的转场可能出现**短暂空白**（UI 还没加载完）

2. **包依赖关系**：`Popup` 包中的弹窗用到了 `Common` 包的按钮。FairyGUI 的包引用关系如何设置？是否需要确保 `Common` 先于 `Popup` 加载？

3. **卸载策略**：从战斗返回选关后，`Battle` 包是否卸载？反复进出关卡时如果不卸载会逐渐累积内存。如果卸载，重新进入战斗时有加载延迟。

**期望守方回答**：5 个包的加载/卸载策略 + 微信小游戏首包大小影响评估。

**状态**：🟡 待回应

---

### SGUI-008 | 飘字系统（FloatingText）的对象池设计

**质疑**：§3.4 定义"击杀位置飘出 +10 分数"。在弹幕射击中，一波 10 架敌机同时被击杀完全可能——这意味着**同时 10 个飘字实例**。

**具体问题**：

1. **FairyGUI 动态创建 vs 对象池**：每次击杀 `new GComponent()` 创建飘字？还是预创建一个 `FloatingText[]` 池？频繁创建 GComponent 在微信小游戏 WebGL 环境下 GC 压力如何？

2. **世界坐标 → UI 坐标转换**：击杀位置是**世界空间坐标**，飘字是 FairyGUI **UI 空间**。坐标转换公式需要 `Camera.WorldToScreenPoint()` → FairyGUI `GRoot.inst.GlobalToLocal()`。这个转换逻辑放在哪个组件里？

3. **最大并发数**：是否应该定义一个 `MAX_FLOATING_TEXT = N` 上限？超出上限时是丢弃新飘字、还是立即回收最旧的？

**期望守方回答**：飘字的创建/回收策略 + 坐标转换归属 + 并发上限。

**状态**：🟡 待回应

---

### SGUI-009 | "无敌帧"反馈与 Entity 框架的事件连接

**质疑**：§3.4 列出"无敌帧：飞机半透明闪烁"。但 GDD v3.0 明确说**"飞机不挂 HealthComponent、被撞不受伤"**。

**具体问题**：

1. **"无敌帧"概念是否还存在？** GDD v3.0 决策：飞机撞敌机 → 敌机死、飞机无事。如果飞机不受伤，哪来的"无敌帧"？是文档遗留（来自更早版本 GDD）还是有新的设计意图？

2. 如果无敌帧**仍然存在**——那它的含义是什么？是"被撞后短暂不触发 ScreenShake"？还是"被撞后短暂不计入 ContactDamage 到敌机"（那等于飞机暂时不能撞死敌机）？

3. 如果无敌帧**已废弃**——那 §3.4 的这一行应该删除，避免实施阶段产生困惑。

**期望守方回答**：确认"无敌帧"在 v3.0 设计中是否仍有意义，如果有则明确触发条件和效果。

**状态**：🟡 待回应

---

### SGUI-010 | 微信小游戏性能约束对动效的影响

**质疑**：文档定义了大量动效（按钮弹性、呼吸灯、脉冲闪烁、粒子光效、预损动画等），但**没有针对微信小游戏 WebGL 环境的性能约束做裁剪说明**。

**具体问题**：

1. **FairyGUI Transition 在 WebGL 的性能特征**：FairyGUI Transition 依赖 `DOTween` 或内部 `GTween`——在微信小游戏环境下，大量同时进行的 Tween（如 10 个飘字 + 血条预损 + 脉冲闪烁 + 波次文字动效）是否有性能基准数据？

2. **"粒子/光效"（VictoryPanel §2.5）**：Victory 面板说"大字 + 粒子/光效"。这是 Unity ParticleSystem？还是 FairyGUI 动效模拟？如果是 ParticleSystem——它在 UI 层级如何叠加？DrawCall 开销多少？

3. **建议**：文档应增加一个 **"V1 动效裁剪表"**——标注哪些动效是 V1 必须、哪些是"有则更好但可降级"、哪些明确 V2。避免实施时每个动效都要再做一次"要不要做"的决策。

**期望守方回答**：动效性能预算 + 裁剪优先级表。

**状态**：🟡 待回应

---

## Round 1 攻方统计

| # | 问题 ID | 核心关切 | 维度 |
|---|---------|----------|------|
| 1 | SGUI-001 | UI 生命周期与框架 UIManager 集成 | 系统解耦 |
| 2 | SGUI-002 | FairyGUI ↔ SO 数据绑定方案 | 数据驱动 |
| 3 | SGUI-003 | 虚拟摇杆输入通路与触摸分层 | 组件职责 |
| 4 | SGUI-004 | 转场动效实现层归属 | 系统解耦 |
| 5 | SGUI-005 | 血条预损动画数据时序 | 数据驱动 |
| 6 | SGUI-006 | 关卡节点三态数据源 | 数据驱动 |
| 7 | SGUI-007 | FairyGUI 包加载策略 | 性能/内存 |
| 8 | SGUI-008 | 飘字对象池设计 | 性能/GC |
| 9 | SGUI-009 | "无敌帧"与 GDD v3.0 矛盾 | 设计一致性 |
| 10 | SGUI-010 | 动效性能裁剪 | 微信小游戏约束 |

**攻方总结**：10 个问题，聚焦 **"UI 文档描述了'想要什么'但没回答'如何集成到 Entity+SO 架构中'"**。核心诉求：UI 设计文档应补充实施级的**架构集成规范**——数据绑定方式、组件职责划分、性能约束裁剪——否则实施阶段每个 UI 都要重新做架构决策。

---

## PK Round 2 — 守方回应（游戏设计师）

> 逐条回应。立场：承认架构层缺失合理，但拒绝 UI 设计文档变成 TDD——架构细节应在 TDD 中补充，GDD-UI 定义的是"要什么效果"。对合理质疑给出明确结论。

---

### SGUI-001 回应 | ✅ 部分接受

**核心观点**：攻方说得对——UI 文档确实没有交代框架集成规范。但这是**有意为之**：UI 设计文档定义"设计需求"，不定义"技术实现方案"。后者属于 TDD 范畴。

**但接受攻方要求补充"接口层约定"**：

1. **框架现有能力**：MiniGameTemplate 已有 `UIManager`（基于 FairyGUI 的包管理 + 界面池）。6 个界面的 C# 层映射：
   - `LoadingScreen` → `UIScreen`（全屏，自动生命周期）
   - `LevelSelectScreen` → `UIScreen`（全屏，主界面，常驻不销毁）
   - `BattleHUD` → `UILayer`（叠加层，场景级生命周期）
   - `PausePanel / VictoryPanel / DefeatPanel` → `UIPanel`（模态弹窗，按需创建/销毁）

2. **生命周期分类已回答**：
   - `UIScreen`：全屏界面，互斥显示（显示 A 时 B 隐藏）
   - `UILayer`：叠加在 Screen 上，跟随场景生存期
   - `UIPanel`：模态弹窗，栈式管理（后开先关）

3. **"冻结"含义**：`TimeScale=0`。FairyGUI 动效默认用 `Time.deltaTime`——暂停时 UI 动效自然冻结。这是**预期行为**：暂停时血条不应该继续跳动。

**结论**：接受。在 UI 文档中补充一个 **"§八 框架集成约定"** 小节，列出 6 个界面的类型映射。

---

### SGUI-002 回应 | ✅ 完全接受

**核心观点**：数据绑定确实需要明确。独立开发者不需要 MVVM 框架——用最简单的方案。

**技术方案**：

1. **FairyGUI ↔ SO 桥接方案**：每个界面有一个 `XXXController : MonoBehaviour`，负责：
   - `OnEnable()` 订阅 SO 事件（`floatVar.OnValueChanged += UpdateHP`）
   - `OnDisable()` 取消订阅
   - 持有对 FairyGUI `GComponent` 的引用（通过 `UIManager.GetScreen<T>()` 获取）
   
   这是框架已有的模式——`BattleHUDController` 订阅 `BaseHP` FloatVariable，更新 `ProgressBar_HP`。

2. **缺失 SO 变量清单**（需新增）：
   | SO 变量 | 类型 | 来源 | 消费者 |
   |---------|------|------|--------|
   | SG_BaseHP | FloatVariable | HealthComponent.OnHpChanged | BattleHUDController |
   | SG_CurrentWaveIndex | IntVariable | EntitySpawner.OnWaveChanged | BattleHUDController |
   | SG_TotalWaveCount | IntVariable | Bootstrap 初始化时写入 | BattleHUDController |
   | SG_KillCount | IntVariable | CollisionComponent.OnEntityKilled | VictoryPanel / DefeatPanel |
   | SG_TotalEnemyCount | IntVariable | Bootstrap 初始化时计算写入 | DefeatPanel ("18/25") |

3. **波次 "Wave 2/5"**：`SG_CurrentWaveIndex` + `SG_TotalWaveCount` 两个 IntVariable。Spawner 在 `StartNextWave()` 时写入 `SG_CurrentWaveIndex.Value++`。

**结论**：完全接受。UI 文档回写新增 5 个 SO 变量定义 + 数据绑定模式说明。

---

### SGUI-003 回应 | ✅ 部分接受

**核心观点**：输入通路问题合理。选择方案 A（数据驱动）。

**明确回答**：

1. **输入通路**：✅ **方案 A** — `Joystick` 组件每帧将方向向量写入 `Vector2Variable` SO。`MovementComponent`（Entity 层）每 Tick 读取 `Vector2Variable.Value` 作为移动方向。
   - 理由：解耦——摇杆可以被键盘输入替换（调试用）而不改 Entity 代码。
   - 性能：每帧写 SO 的 Vector2 = 一次赋值，零 GC。

2. **FairyGUI 触摸层级配置**：
   - 暂停按钮：`touchable = true`，正常事件分发
   - HUD 其他元素（波次文字、血条）：`touchable = false`——触摸穿透
   - 摇杆响应区：不是 FairyGUI 组件——是 **Unity 层的全屏 `MonoBehaviour`**（`JoystickInputHandler`），监听 `Input.GetTouch()`
   - 事件优先级：FairyGUI 按钮先检测（在 GRoot 层拦截），未命中才落到 Unity Input 层的摇杆 Handler

3. **首帧判定**：手指起始于暂停按钮热区（44×44 pt）→ 暂停按钮响应 → **不传递给摇杆**。反之，手指起始于其他区域 → 摇杆激活 → 即使后续拖入暂停按钮区域也**不触发暂停**（因为触摸已被摇杆 claim）。

**结论**：接受。回写明确"方案 A + 全屏 MonoBehaviour 摇杆 + FairyGUI 按钮优先拦截"。

---

### SGUI-004 回应 | ✅ 完全接受

**核心观点**：转场动效的实现层划分确实应该写清楚。

**明确回答**：

1. **转场动效归属表**：

   | 转场 | FairyGUI 负责 | Unity 代码负责 |
   |------|---------------|----------------|
   | 加载→选关 | LoadingScreen 淡出 | — |
   | 选关→战斗 | LevelNode 缩放动画 | 白闪 overlay + 场景加载 + 淡入 |
   | 战斗→选关（胜利）| VictoryPanel 淡出 | 场景卸载 + 选关场景淡入 |
   | 战斗→选关（失败返回）| DefeatPanel 滑出 | 场景卸载 + 选关场景淡入 |
   | 重试 | DefeatPanel 滑出 | **不重载场景** — EntitySpawner.Reset() + Bootstrap.ResetBattle() |

2. **异步编排**：框架已有 `SceneLoader`（异步加载 + 进度回调）。转场序列用 `async/await` 或简单协程编排：
   ```
   await LevelNode.PlayScaleAnimation();  // FairyGUI Transition
   await SceneLoader.LoadAsync("Battle"); // Unity 场景加载
   FadeIn();                               // Unity overlay 动画
   ```
   不需要新建"TransitionSequencer"——协程 + 回调足够 V1 的 5 种转场。

3. **重试 ≠ 重载场景**：`EntitySpawner.Reset()` + 清空所有 Active Entity + 重置 HP + 重置波次索引。**避免场景重载的加载延迟**（微信小游戏环境下场景重载需要 GC + 重新反序列化）。

**结论**：完全接受。回写转场归属表 + 重试不重载场景的明确决策。

---

### SGUI-005 回应 | ✅ 部分接受

**核心观点**：预损动画确实需要明确实现方案。选方案 A。

**明确回答**：

1. **双值追赶策略**：
   - **方案 A**（FairyGUI 组件内部）— `BattleHUDController` 维护两个本地变量：
     - `_displayHP`（白色段，延迟追赶）
     - `_targetHP`（绿色段，立即跟踪 SO 值）
   - 当 SO 值变更 → `_targetHP` 立即更新 → `_displayHP` 以每帧 Lerp 速率追赶 `_targetHP`
   - 连续快速扣血：`_targetHP` 持续更新到最新值，`_displayHP` 始终"慢半拍"追赶 → 视觉上就是白色段逐渐缩短

2. **不需要额外 SO**：方案 B（多一个 FloatVariable）over-engineering。预损只是 UI 表现层逻辑，不需要暴露给其他系统。

3. **暂停时行为**：`TimeScale=0` → `Time.deltaTime=0` → Lerp 不推进 → 白色段冻住。**这是预期行为**——暂停时一切冻结，包括 UI 动效。恢复后继续追赶。

**结论**：接受方案 A。UI 文档回写一句"预损动画由 HUDController 内部 Lerp 驱动，无需额外 SO"。

---

### SGUI-006 回应 | ✅ 完全接受

**核心观点**：关卡进度数据源确实需要明确。

**明确回答**：

1. **数据源**：`ISaveSystem` 持久化 → 反序列化为 `SG_LevelProgress`（简单结构）：
   ```json
   { "completedLevels": [1, 2], "currentMaxLevel": 3 }
   ```
   - V1 极简：通关即解锁下一关，无星级、无评分
   - `currentMaxLevel` = 已解锁的最高关卡编号

2. **新解锁动效的 diff 判定**：
   - `LevelSelectScreenController` 打开时记录 `_prevMaxLevel`
   - 战斗胜利返回后，读取最新 `currentMaxLevel`
   - 如果 `currentMaxLevel > _prevMaxLevel` → 对 LevelNode[currentMaxLevel] 播放"解锁动效"
   - **由 UI Controller 维护 diff**——不是数据层职责

3. **组件归属**：`LevelSelectScreenController : MonoBehaviour`（单一职责）：
   - 打开时：从 `ISaveSystem` 读进度 → 设置 5 个 `LevelNode` 的 FairyGUI Controller 状态
   - 监听：节点点击事件 → 设置 `SG_CurrentLevelIndex.Value` → 触发场景加载

**结论**：完全接受。回写明确数据源 + diff 判定归属。

---

### SGUI-007 回应 | ✅ 部分接受

**核心观点**：加载策略确实值得明确，但 V1 规模不需要复杂方案。

**明确回答**：

1. **V1 加载策略：全量预加载**。
   - 理由：5 关 + 6 界面 → FairyGUI 包预估总大小 < 500KB（全是矢量 + 小贴图）。微信小游戏首包限制 30MB，FairyGUI 包占比 < 2%。
   - 在 `LoadingScreen` 阶段全量加载 5 个包，加载完毕后界面池已就绪。
   - **零按需加载延迟** → 转场丝滑

2. **包依赖**：FairyGUI 编辑器原生支持"包引用"——`Popup` 引用 `Common` 中的按钮。加载顺序由 FairyGUI Runtime 自动处理（依赖包先加载）。开发者无需手动控制顺序。

3. **卸载策略**：V1 **不卸载**。全部 5 个包总 < 500KB 常驻内存。微信小游戏 JS Heap 限制约 256MB，500KB 完全不值得卸载回收带来的复杂性。

**结论**：接受。回写"V1 全量预加载、不卸载"策略。V2 包体膨胀后再评估按需加载。

---

### SGUI-008 回应 | ✅ 完全接受

**核心观点**：飘字池化确实必要。

**明确回答**：

1. **对象池**：预创建 `FloatingText[8]`（最大并发 8 个）。用完后回收到池中、重置位置和文本。
   - FairyGUI 的对象池 = 隐藏 + 重置状态（`visible=false, x=0, y=0`），不 `Dispose()`
   - 零 GC 创建/销毁

2. **坐标转换归属**：`BattleHUDController` 负责——它同时持有 Camera 引用和 GRoot 引用。
   ```
   Vector2 screenPos = Camera.main.WorldToScreenPoint(killWorldPos);
   Vector2 uiPos = GRoot.inst.GlobalToLocal(screenPos);
   floatingText.SetPosition(uiPos);
   ```

3. **最大并发数**：`MAX_FLOATING_TEXT = 8`。超出时**回收最旧的**（FIFO 环形缓冲）。8 个对弹幕射击足够——同时击杀 10 架的概率极低，且前 2 个飘字在 0.8s 内会自动回收。

**结论**：完全接受。回写池大小 + 坐标转换归属 + FIFO 回收策略。

---

### SGUI-009 回应 | ✅ 接受——删除无敌帧

**核心观点**：攻方抓到了 GDD v3.0 与 UI 文档 v1.0 的**不一致**。

**明确回答**：

1. **"无敌帧"在 v3.0 设计中已无意义**。飞机不受伤 → 没有"受伤后的保护期"概念。
2. 这一行来自 v1.x GDD 的遗留（当时飞机有 HP）。UI 文档写于 v2.1 时期，v3.0 GDD 改了设计但 UI 文档没同步。
3. **决策**：从 §3.4 HUD 反馈规范表中**删除"无敌帧"行**。

**结论**：接受删除。这是文档同步问题，不是设计争议。

---

### SGUI-010 回应 | ✅ 部分接受

**核心观点**：动效裁剪表有价值——但不应让"性能焦虑"阻止设计文档定义完整的体验目标。

**明确回答**：

1. **FairyGUI GTween 性能**：GTween 是 C# 层轻量 Tween（无反射、无 GC），100 个同时活跃 Tween 在微信 WebGL 环境下 < 0.5ms。V1 同时最多：8 飘字 + 1 预损 + 1 脉冲 + 1 波次文字 = **11 个 Tween**。完全无压力。

2. **Victory "粒子/光效"**：V1 **不用 Unity ParticleSystem**。用 FairyGUI 动效模拟（序列帧 + 缩放 + 旋转 = "光效感"）。DrawCall = 0 额外开销（在 FairyGUI 批次内）。

3. **V1 动效裁剪表**：

   | 动效 | V1 优先级 | 说明 |
   |------|-----------|------|
   | 按钮缩放反馈 | P0 必须 | 2 行 GTween，零成本 |
   | 血条预损动画 | P0 必须 | 1 个 Lerp，零成本 |
   | 飘字上浮淡出 | P0 必须 | 池化，8 并发 |
   | 波次文字闪现 | P0 必须 | 单个 GTween |
   | 基地受伤红闪 | P0 必须 | 全屏 GGraph alpha 动画 |
   | 呼吸灯（关卡节点）| P1 有则更好 | 选关界面单个节点循环 Tween |
   | 屏幕暗角（<30% HP）| P1 有则更好 | 4 个半透明 GImage |
   | 关卡解锁动效 | P1 有则更好 | 选关界面一次性播放 |
   | Victory 光效 | P1 有则更好 | FairyGUI 序列帧动效 |
   | 锁定节点摇晃 | V2 | 可先用 Toast 文字代替 |

**结论**：接受补充裁剪表。但 V1 的 11 个并发 Tween 远低于性能瓶颈——"裁剪"更多是优先级管理而非性能必要。

---

## Round 2 守方统计

| 问题编号 | 守方裁定 | 具体动作 |
|----------|----------|----------|
| SGUI-001 | 部分接受 | 补充 §八 框架集成约定（6 界面类型映射 + 生命周期分类） |
| SGUI-002 | 完全接受 | 新增 5 个 SO 变量 + 数据绑定模式说明 |
| SGUI-003 | 部分接受 | 方案 A（Vector2Variable）+ 全屏 MonoBehaviour 摇杆 + 触摸层级说明 |
| SGUI-004 | 完全接受 | 转场归属表 + 重试不重载场景 |
| SGUI-005 | 部分接受 | 方案 A（Controller 内部 Lerp）+ 暂停冻结是预期 |
| SGUI-006 | 完全接受 | ISaveSystem + Controller diff 判定 + 单一职责 Controller |
| SGUI-007 | 部分接受 | V1 全量预加载 + 不卸载 |
| SGUI-008 | 完全接受 | 池化 8 + BattleHUDController 坐标转换 + FIFO |
| SGUI-009 | 接受删除 | §3.4 删除无敌帧行 |
| SGUI-010 | 部分接受 | 补充 V1 动效裁剪表 |

**守方总结**：10 个问题中完全接受 4 个、部分接受 5 个、接受删除 1 个。守方零否决——攻方质疑全部命中实际缺失。UI 设计文档 v1.0 确实缺乏架构集成层的描述，需要补充一个"框架集成"章节。但核心原则不动：**UI 设计文档定义体验目标，TDD 定义技术实现——PK 收敛的结论写在 UI 文档的集成约定章节中作为"契约"，详细实现由 TDD 展开。**

---

## PK Round 3 — 攻方反驳/追问（Unity 架构师）

> 守方回应质量高，大部分决策清晰。攻方聚焦 3 个仍需对齐的技术细节。

---

### SGUI-002 追问 | SO 变量与 GDD v3.1 SO 清单的一致性

**守方说**：新增 5 个 SO 变量（SG_BaseHP / SG_CurrentWaveIndex / SG_TotalWaveCount / SG_KillCount / SG_TotalEnemyCount）。

**追问**：

1. **GDD v3.1 SO 清单（14 个资产）没有包含这 5 个变量。** GDD 只列了 1 个 `SG_CurrentLevelIndex`（IntVariable）。这 5 个是否应该追加到 GDD 的 SO 清单中？否则 GDD 和 UI 文档的 SO 清单不一致——实施时哪个为准？

2. **`SG_BaseHP` 类型确认**：守方写 `FloatVariable`。但基地 HP 在 GDD v3.1 §3.4 中是 `HP=100` 这样的整数值。用 `FloatVariable` 还是 `IntVariable`？
   - 如果是 `FloatVariable`：HP 百分比显示 = `value / maxValue * 100`，需要额外知道 `maxHP`
   - 如果是 `IntVariable`：直接显示 `value`，但百分比需要 `maxHP`
   - **建议**：用 `FloatVariable` 存 `currentHP`，再加一个 `FloatVariable` 存 `maxHP`——或者用 `HealthComponent` 自身提供 `Ratio` 属性，UI 直接读 ratio

3. **SO 资产总数更新**：14 → 14 + 5 + 1（Vector2Variable）= **20 个 SO 资产**。这个总数需要在某处集中管理（一个 master list），避免遗漏。

**期望守方回答**：5 个新增 SO 是否追加到 GDD 清单？`SG_BaseHP` 的精确类型和 maxHP 获取方式？

**状态**：🟡 待回应

---

### SGUI-003 追问 | 全屏 MonoBehaviour 摇杆与 FairyGUI 触摸的兼容性

**守方说**：摇杆用全屏 `MonoBehaviour`（`JoystickInputHandler`）监听 `Input.GetTouch()`，FairyGUI 按钮先拦截。

**追问**：

1. **在微信小游戏 WebGL 环境中**，`Input.GetTouch()` 的行为与原生移动端不同——微信小游戏用 `wx.onTouchStart/Move/End` 透过 JS Bridge 传递。Unity 的 `Input.GetTouch()` **在 WebGL 构建中能正常工作吗？** 还是需要用 `Input.mousePosition` + `Input.GetMouseButton()` 模拟？

2. **FairyGUI 和 Unity Input 的事件消费顺序**：FairyGUI 默认在 `Update()` 中处理触摸。如果 `JoystickInputHandler` 也在 `Update()` 中读 `Input.GetTouch()`——两者都能读到同一个触摸事件。**FairyGUI 消费了暂停按钮的点击后，`JoystickInputHandler` 能否知道这个触摸已被 FairyGUI 消费？**
   - 如果不能 → 点击暂停按钮时摇杆也会激活 → bug
   - 解决方案：`JoystickInputHandler` 在处理触摸前先检查 `GRoot.inst.touchTarget != null`（即 FairyGUI 是否拦截了该触摸）

3. **替代方案**：是否应该直接用 FairyGUI 的全屏透明 `GGraph` 做摇杆响应区（`touchable=true`，`z-index` 低于暂停按钮）？这样所有触摸事件都在 FairyGUI 体系内处理，避免跨系统判断。

**期望守方回答**：微信 WebGL 触摸兼容性确认 + 事件消费冲突的解决方案。

**状态**：🟡 待回应

---

### SGUI-004 追问 | 重试不重载场景的 UI 重置链路

**守方说**：重试 = `EntitySpawner.Reset()` + 清空 Entity + 重置 HP/波次，不重载场景。

**追问**：

1. **UI 层的重置**：不重载场景意味着 `BattleHUD` 和所有 FairyGUI 组件仍然存活。那以下 UI 状态需要手动重置：
   - 血条值 → 100%
   - 波次文字 → "Wave 1/5"
   - 飘字池全部回收
   - 屏幕暗角（如果之前 HP<30% 激活了）→ 关闭
   - 红闪覆盖层 → 确保不在播放
   
   **谁负责触发这个 UI 重置？** 是 Bootstrap 调用 `BattleHUDController.ResetUI()`？还是 SO 变量写回初始值后 UI 自动响应？

2. **如果走 SO 自动响应**：那 SO 变量的 `OnValueChanged` 事件在**同一帧内连续触发**（Reset 时 HP=100, WaveIndex=0, KillCount=0 一口气全写）——UI 会不会在中间状态触发不必要的动画（如血条从当前值 Lerp 到 100）？
   - 建议：Reset 时先 `mute` SO 事件通知 → 批量写值 → 恢复通知 → 手动刷新一次 UI

3. **DefeatPanel 滑出时机**：DefeatPanel 滑出 → 黑屏 0.2s → 重置 → 淡入。这个序列中，"重置"发生在黑屏期间（玩家看不到）。**编排代码在哪个组件里？** `DefeatPanel` 自身？还是 `BattleController`？

**期望守方回答**：UI 重置的触发方和具体机制。

**状态**：🟡 待回应

---

### Round 3 已收敛条目（无需追问）

| 编号 | 状态 |
|------|------|
| SGUI-001 | ✅ 完全收敛（6 界面类型映射已明确） |
| SGUI-005 | ✅ 完全收敛（方案 A + 暂停冻结是预期） |
| SGUI-006 | ✅ 完全收敛（ISaveSystem + Controller diff） |
| SGUI-007 | ✅ 完全收敛（V1 全量预加载 + 不卸载） |
| SGUI-008 | ✅ 完全收敛（池化 8 + FIFO + BattleHUDController 坐标转换） |
| SGUI-009 | ✅ 完全收敛（删除无敌帧） |
| SGUI-010 | ✅ 完全收敛（V1 动效裁剪表已列出） |

**攻方 Round 3 总结**：7/10 已收敛。剩余 3 个追问都是"具体实施级细节对齐"——方向无争议，只需确认技术路径。

---

## PK Round 4 — 守方二次回应（游戏设计师）

> 3 个追问全是好问题。快速对齐。

---

### SGUI-002 二次回应 | ✅ 同意追加 + 明确类型

**逐条回答**：

1. **追加到 GDD SO 清单**：✅ 同意。5 个 UI 相关 SO + 1 个 Vector2Variable 应该追加到 GDD §十一 或单独的 "V1 SO 完整资产表" 中。**GDD 为 master list**，UI 文档引用但不另起清单。

2. **`SG_BaseHP` 类型决策**：
   - **用 `FloatVariable` 存归一化比例（0~1）**——而非原始 HP 值
   - `HealthComponent` 在 HP 变化时写入 `SG_BaseHP.Value = currentHP / maxHP`
   - UI 直接读 `SG_BaseHP.Value` 显示百分比，无需知道 maxHP
   - 理由：UI 只关心"还剩百分之几"，不关心"100 还是 500"。最简绑定，零额外 SO。
   - **不再需要 maxHP 变量**

3. **SO 资产总数更新**（完整 V1 master list）：

   | SO 资产 | 类型 | 用途 |
   |---------|------|------|
   | SG_Player | EntityConfigSO | 玩家飞机配置 |
   | SG_Base | EntityConfigSO | 基地配置 |
   | SG_Enemy_Normal | EntityConfigSO | 普通敌机 |
   | SG_Enemy_Fast | EntityConfigSO | 快速敌机 |
   | SG_Level_01~05 | SG_LevelConfigSO ×5 | 关卡元数据 |
   | SG_Wave_01~05 | EntitySpawnWaveSO ×5 | 波次数据 |
   | SG_PlayerBullet_Straight | BulletPatternSO | 子弹模式 |
   | SG_ScreenShake_Default | ScreenShakeConfigSO | 震动配置 |
   | SG_CurrentLevelIndex | IntVariable | 当前关卡索引 |
   | SG_BaseHP | FloatVariable | 基地 HP 比例 (0~1) |
   | SG_CurrentWaveIndex | IntVariable | 当前波次 |
   | SG_TotalWaveCount | IntVariable | 总波次数 |
   | SG_KillCount | IntVariable | 击杀计数 |
   | SG_TotalEnemyCount | IntVariable | 本关总敌机数 |
   | SG_InputDirection | Vector2Variable | 摇杆方向输入 |
   | **总计** | **21 个 SO 资产** | |

**SGUI-002 最终结论**：✅ 完全收敛。

---

### SGUI-003 二次回应 | ✅ 接受替代方案——全部走 FairyGUI

攻方的第 3 个建议（全屏透明 GGraph 做摇杆响应区）确实更优。修正方案：

1. **微信 WebGL 兼容性**：攻方怀疑有道理——`Input.GetTouch()` 在微信 WebGL 中**不可靠**（微信 JS Bridge 模拟的 Touch 可能丢事件或坐标异常）。历史经验：FairyGUI 自身通过 `onTouchBegin/Move/End` 内部处理微信触摸事件，比 Unity Input 层更稳定。

2. **修正方案**：
   - ❌ ~~全屏 MonoBehaviour + Input.GetTouch()~~
   - ✅ **FairyGUI 全屏 `GGraph`**（`touchable=true`，铺满 GRoot，`z` 低于 HUD 元素）作为摇杆响应区
   - 暂停按钮 `z` 高于 GGraph → 事件分发时暂停按钮优先拦截 → 自然解决冲突
   - 摇杆逻辑写在 `JoystickComponent`（FairyGUI Component 的扩展脚本）中，通过 `onTouchBegin/Move/End` 处理
   - 每帧将方向向量写入 `SG_InputDirection` Vector2Variable SO

3. **完全在 FairyGUI 事件体系内** → 无跨系统消费冲突 → 无需检查 `GRoot.touchTarget`

**SGUI-003 最终结论**：✅ 完全收敛。摇杆 = FairyGUI GGraph + onTouch 事件 → Vector2Variable SO。

---

### SGUI-004 二次回应 | ✅ 明确 UI 重置机制

**逐条回答**：

1. **UI 重置触发方**：**SO 自动响应 + 一次性直接赋值**。
   - Reset 时：`SG_BaseHP.Value = 1.0f`（触发 OnValueChanged → 血条立刻跳到 100%）
   - `SG_CurrentWaveIndex.Value = 0`（触发 → 波次文字更新为 "Wave 1/5"）
   - `SG_KillCount.Value = 0`
   - 飘字池：`BattleHUDController.RecycleAllFloatingTexts()`（一行代码批量隐藏）
   - 屏幕暗角/红闪：由 HP 值变更驱动——HP > 30% → 自动关闭暗角。无需手动重置。

2. **关于"不必要动画"**：
   - 血条 Lerp 问题：当 `SG_BaseHP` 从 0.2 跳到 1.0 时，预损动画会 Lerp 追赶。但这发生在**黑屏期间**（玩家看不到），0.2s 黑屏结束后 UI 已经追赶完毕（HP=1.0 时差值=0，Lerp 瞬间完成）。
   - **无需 mute 机制**。重置在黑屏内完成，视觉上对玩家透明。
   - 如果极端情况下 Lerp 没追完：开战首帧 `BattleHUDController.ForceRefresh()` 强制同步一次值（不走 Lerp）。

3. **重试序列编排归属**：`BattleController`（游戏层控制器，单一职责 = 战斗流程编排）：
   ```
   async void OnRetryClicked() {
     DefeatPanel.SlideOut();                  // FairyGUI Transition
     await BlackScreen.FadeIn(0.2f);          // Unity overlay
     EntitySpawner.Reset();                   // Entity 层重置
     BattleHUDController.ForceRefresh();      // UI 强制刷新
     await BlackScreen.FadeOut(0.2f);         // 淡出黑屏
     BattleController.StartBattle();          // 开始新一轮
   }
   ```
   - `DefeatPanel` 只负责播动画和触发按钮事件
   - `BattleController` 负责编排完整序列（单一指挥点）

**SGUI-004 最终结论**：✅ 完全收敛。

---

## Round 4 守方统计

| 追问编号 | 结论 |
|----------|------|
| SGUI-002 追问 | ✅ 完全收敛 — FloatVariable 存 ratio(0~1) + 21 个 SO master list |
| SGUI-003 追问 | ✅ 完全收敛 — 修正为 FairyGUI GGraph + onTouch 方案 |
| SGUI-004 追问 | ✅ 完全收敛 — SO 自动响应 + 黑屏内重置 + BattleController 编排 |

**守方 Round 4 总结**：10/10 全部收敛。攻守双方无分歧。

---

## PK Round 5 — 攻方终轮确认（Unity 架构师）

> 全部 10 个问题已收敛。攻方确认无新质疑。

**确认结论**：守方回应务实、技术方案清晰、修正果断（SGUI-003 当场改方案、SGUI-009 当场删除无效设计）。作为 Unity 架构师，认可以下结果：

1. **数据驱动完整性**：21 个 SO 完整 master list 覆盖了 Entity + UI 的全部运行时数据流
2. **输入解耦**：摇杆走 FairyGUI 事件体系 → Vector2Variable SO → Entity MovementComponent，干净的单向数据流
3. **FairyGUI ↔ Entity 桥接**：每界面一个 MonoBehaviour Controller（单一职责：订阅 SO → 更新 UI），不发明 MVVM
4. **性能约束明确**：飘字池化 8 / 全量预加载 < 500KB / GTween 11 并发远低于瓶颈
5. **重试零重载**：EntitySpawner.Reset() + SO 自动响应 + BattleController 编排，避免场景重载 GC
6. **设计一致性**：无敌帧已删除，与 GDD v3.0 对齐

**攻方唯一补充建议**（非质疑）：

建议 UI 设计文档新增两个章节：
- **§八 框架集成约定**：汇总界面类型映射 + 数据绑定方案 + SO 变量清单 + 摇杆方案 + 转场归属
- **§九 V1 动效裁剪表**：P0/P1/V2 分级

这样 TDD 阶段直接从这两个章节展开 UI 实施，不需要翻 PK 记录。

**攻方最终裁定**：✅ 全部通过，无遗留问题。

---

## PK Round 6 — 最终收敛判定

### 收敛状态

| 问题编号 | 最终状态 | 收敛轮次 |
|----------|----------|----------|
| SGUI-001 | ✅ 收敛 | R2 |
| SGUI-002 | ✅ 收敛 | R4 |
| SGUI-003 | ✅ 收敛 | R4 |
| SGUI-004 | ✅ 收敛 | R4 |
| SGUI-005 | ✅ 收敛 | R2 |
| SGUI-006 | ✅ 收敛 | R2 |
| SGUI-007 | ✅ 收敛 | R2 |
| SGUI-008 | ✅ 收敛 | R2 |
| SGUI-009 | ✅ 收敛 | R2 |
| SGUI-010 | ✅ 收敛 | R2 |

### PK 评审结论

- **总问题数**：10
- **收敛率**：100%（10/10）
- **实际使用轮次**：4 轮（Round 5-6 为确认+判定，无新争议）
- **UI 文档需回写条目数**：10 处修改

### UI 文档回写清单（待执行）

| # | 修改位置 | 修改内容 |
|---|----------|----------|
| 1 | 新增 §八 | 框架集成约定：6 界面类型映射（UIScreen/UILayer/UIPanel） |
| 2 | §八 | 数据绑定方案：MonoBehaviour Controller 订阅 SO → 更新 FairyGUI |
| 3 | §八 | UI 相关 SO 变量表（6 个：BaseHP/WaveIndex/TotalWave/KillCount/TotalEnemy/InputDirection） |
| 4 | §八 | 摇杆方案：FairyGUI GGraph + onTouch → Vector2Variable SO |
| 5 | §八 | 转场动效归属表（FairyGUI vs Unity 代码层） |
| 6 | §八 | 重试机制：不重载场景 + SO 自动响应 + BattleController 编排 |
| 7 | §八 | FairyGUI 包加载策略：V1 全量预加载 + 不卸载 |
| 8 | §八 | 飘字池化：预创建 8 个 + FIFO 回收 + BattleHUDController 坐标转换 |
| 9 | §3.4 | 删除"无敌帧"反馈行 |
| 10 | 新增 §九 | V1 动效裁剪表（P0/P1/V2 分级） |

### 附加影响：GDD v3.1 需同步更新

| # | GDD 修改 |
|---|----------|
| A | SO 资产清单从 14 → 21 个（追加 6 个 UI 相关 + 1 个 Vector2Variable） |
| B | §十一 编辑器工具清单中补充 SO 资产总数更新 |

### PK 最终评价

**攻方视角**：UI 设计文档 v1.0 在交互体验层面完成度高，但**零架构集成信息**——所有"怎么接"的问题都悬空。经过 4 轮对齐，文档将补充完整的集成约定章节，形成"设计→架构→实施"的完整链路。

**守方视角**：攻方从架构师角度补了 UI 设计师不会主动思考的维度（SO 绑定、触摸分层、重载 vs 重置、对象池、微信兼容性）。守方修正了 1 个方案（SGUI-003 摇杆从 Unity Input 改为 FairyGUI 事件）和 1 个遗留错误（SGUI-009 无敌帧删除），PK 价值充分体现。

> **PK 状态**：✅ 已完成
> **结束时间**：2026-05-03
