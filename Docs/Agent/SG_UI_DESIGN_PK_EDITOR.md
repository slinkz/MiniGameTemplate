# SG_UI_DESIGN 可执行性 PK 评审（编辑器工具视角）

> **攻方**：Unity 编辑器工具开发者 Agent（关注编辑器配置效率、Inspector 可视化、SO 批量工具、预览能力、调试可观测性）  
> **守方**：UI 设计师 Agent  
> **评审对象**：`SG_UI_DESIGN.md` v1.0 + 前轮 PK 补充结论  
> **最大轮次**：6  
> **PK 状态**：✅ 已完成（4 轮收敛，10/10 问题关闭）

---

## PK Round 1 — 攻方提问（Unity 编辑器工具开发者）

> 作为编辑器工具开发者，我关注的核心命题是：**"这个 UI 设计在日常开发中能不能高效配置、快速预览、方便调试——独立开发者在迭代 5 关×6 界面时的编辑器体验是否被考虑过？"**

> 审视维度：① SO 配置 Inspector 体验 ② 编辑器内预览/验证能力 ③ FairyGUI 编辑器 ↔ Unity 编辑器的桥接工作流 ④ 调试可观测工具 ⑤ 批量配置/一致性校验自动化

---

### SGUI-E01 | SO 变量配置的 Inspector 体验

**质疑**：前轮 PK 确认了 21 个 SO 资产。但 UI 文档没有说明这些 SO **在 Inspector 中的编辑体验**应该是什么样的。

**具体问题**：

1. **21 个 SO 资产散落在 Assets/ 目录——独立开发者如何"一眼看到全局状态"？** 需不需要一个 EditorWindow 把所有 `SG_*` 变量的当前值集中展示？特别是 Play Mode 下实时观察 `SG_BaseHP`、`SG_CurrentWaveIndex`、`SG_KillCount` 的变化？

2. **FloatVariable / IntVariable / Vector2Variable 的 Inspector**：框架是否已有这些 SO 的 `CustomEditor`？如果没有——裸 Inspector 只显示 `public float Value;`，在 Play Mode 中需要手动展开 Asset 才能看到值变化。**能否在 Inspector 中直接显示一个实时刷新的值面板？**

3. **SG_BaseHP 是 ratio(0~1)**：Inspector 中只看到 `0.72` 这个数字——不如用 `[Range(0,1)]` + ProgressBar 预览来直观。这种"领域语义增强"的 PropertyDrawer 是否应该预置？

**期望守方回答**：21 个 SO 是否需要专门的 Inspector 增强或集中监视窗口？

**状态**：🟡 待回应

---

### SGUI-E02 | FairyGUI 编辑器 → Unity 编辑器的工作流断裂

**质疑**：文档 §五 规划了 5 个 FairyGUI 包和 8 个关键组件。但 **FairyGUI 编辑器和 Unity 编辑器是两个独立工具**——设计师在 FairyGUI 编辑器中改了布局后，Unity 端的 C# Controller 如何感知？

**具体问题**：

1. **命名一致性风险**：FairyGUI 中组件命名 `Btn_Primary` 在 C# 端需要用字符串 `GetChild("Btn_Primary")` 访问。如果设计师在 FairyGUI 编辑器中**重命名了组件**，Unity 端代码运行时才会空引用报错。**有没有自动化校验——在导出后立即检测 FairyGUI 组件名与 C# 代码中字符串引用的一致性？**

2. **FairyGUI 包发布工作流**：设计师改完 → 发布包 → 生成 `xxx_fui.bytes` + 图集。这个发布步骤是手动的吗？有没有 Unity 端的 `AssetPostprocessor` 或 MenuItem 能**一键从 FairyGUI 项目重新发布到 Unity Assets？**

3. **FairyGUI 与 Unity 坐标系预览**：文档定义了精确的 pt 尺寸（摇杆底座 120pt、最大偏移 60pt）。在 FairyGUI 编辑器中看到的效果与 Unity Game View 中**是否 1:1 对应**？还是需要一个编辑器内叠加预览工具来验证"FairyGUI 设计稿在真机分辨率下的实际呈现"？

**期望守方回答**：FairyGUI ↔ Unity 的日常迭代工作流是否有工具支撑？命名一致性如何保障？

**状态**：🟡 待回应

---

### SGUI-E03 | 关卡节点三态 × 5 关——批量配置与一致性校验

**质疑**：选关界面有 5 个 `LevelNode` 组件，每个有 3 态（已通关/可进入/未解锁）。加上颜色、脉冲动效参数、锁定图标等——**5 个节点是 5 个独立配置还是一个 Prefab/Template 的实例？**

**具体问题**：

1. **FairyGUI 层面**：5 个 `LevelNode` 是 FairyGUI 的同一个 Component 的 5 个实例？还是 5 个独立组件？如果是实例——修改模板自动同步所有关卡节点（好）。如果是独立组件——改一个忘改另外 4 个（坏）。

2. **状态切换的配置**：FairyGUI 的 `Controller`（状态切换器）可以管理三态。但**三态对应的颜色值、动效参数是硬编码在 FairyGUI 中还是从 SO 读取？** 如果硬编码——修改一次颜色需要进 FairyGUI 编辑器。如果 SO 驱动——可以在 Unity Inspector 中调色而无需切换工具。

3. **一致性校验**：如果有人手动把第 3 关节点的按钮尺寸改成 200pt（应为统一规格），**有没有自动化检测——"所有 LevelNode 实例的布局参数必须一致"？** 还是纯靠人工 review？

**期望守方回答**：5 关节点的复用策略和批量一致性保障。

**状态**：🟡 待回应

---

### SGUI-E04 | Play Mode 调试可观测性——血条、飘字、波次的运行时检视

**质疑**：前轮 PK 确认了血条预损动画（双值 Lerp）、飘字池化 8 个、波次切换提示等运行时行为。**但没有提及这些行为的编辑器可观测性。**

**具体问题**：

1. **血条预损调试**：`BattleHUDController` 内部的 `_displayHP` 和 `_targetHP` 是私有变量。Play Mode 中如何**直观地看到这两个值的差值**？Inspector 只能看到 SO 的最终值。是否需要一个 `[ShowInInspector]` 或自定义 Debug Overlay 显示当前 Lerp 状态？

2. **飘字池状态**：8 个池对象中有几个 Active、几个 Recycled？出现"飘字没有弹出"的 bug 时，怎么快速确认——是池耗尽了？还是坐标转换出错了？需不需要一个编辑器 Gizmo 或 Debug UI 显示飘字池的实时状态？

3. **波次 Spawner 状态 vs HUD 显示**：`SG_CurrentWaveIndex` 由 Spawner 写入 → HUD 读取显示 "Wave 2/5"。如果显示不对，debug 需要同时看 Spawner 和 HUD。**有没有一个集中的 Debug Dashboard（EditorWindow 或 Runtime Debug Panel）显示当前战斗状态：WaveIndex / HP / KillCount / Active Entities / Pool Status？**

4. **帧率覆盖**：文档提到 11 个并发 GTween。在低端机上若帧率骤降，**有没有自动化性能标记（Profiler Marker）在 FairyGUI GTween 和 BattleHUDController 的关键路径上？** 还是只能事后 Profiler 手动翻？

**期望守方回答**：运行时调试的工具支持计划。

**状态**：🟡 待回应

---

### SGUI-E05 | 转场动效的编辑器预览——无需进入 Play Mode

**质疑**：文档 §3.2 定义了 5 种转场，每种有明确的时长和 ease 曲线。**转场调参迭代是否必须每次进 Play Mode → 触发条件 → 观看效果 → 退出 → 改参数 → 再进？**

**具体问题**：

1. **转场预览工具**：是否可以做一个 EditorWindow 或 MenuItem `"Tools/SG/预览转场 → [选关→战斗]"`——点击后**在 Edit Mode 下模拟转场 Timeline**？不需要完整游戏循环，只需要播放 UI 动效序列 + 白闪/淡入的 overlay 效果。这能将转场调参效率提升 10x。

2. **Ease 曲线可视化**：`ease-out-back` 写在文档里是文字。Unity 的 `AnimationCurve` Inspector 可以可视化曲线。是否应该把 5 种转场的 timing 存为 `AnimationCurve` SO，让设计师在 Inspector 中拖曲线而不是改代码中的枚举？

3. **重试转场的快速验证**：重试 = 不重载场景 + EntitySpawner.Reset()。这是一个容易出 bug 的路径（状态没清干净）。**是否需要一个 `MenuItem "Tools/SG/模拟重试"` 一键触发 Reset 逻辑而不用打一整关再故意输？**

**期望守方回答**：转场调参的编辑器体验优化方案。

**状态**：🟡 待回应

---

### SGUI-E06 | 虚拟摇杆参数化——死区/最大半径/动效时长的 Inspector 调参

**质疑**：§3.5 定义了摇杆的精确参数（死区 8pt、最大半径 60pt、出现 0.1s、消失 0.15s）。这些参数的**迭代体验**如何？

**具体问题**：

1. **参数硬编码 vs SO 驱动**：这些参数是写死在代码常量里，还是暴露为可配置 SO？
   - 如果硬编码：每次调参需要改代码 → 等编译 → 重新测试。微信小游戏项目编译一次 ~15s，调 10 次手感就浪费 2.5 分钟。
   - 如果 SO 驱动：Inspector 中拖滑块 → Play Mode 热改即时生效 → 迭代效率 10x

2. **摇杆可视化 Gizmo**：Play Mode 中摇杆是"手指按下才出现"的隐形控件。调试时如何**在 Scene View 中看到摇杆的响应区域、死区圆、最大半径圆？** 是否需要一个 `#if UNITY_EDITOR` 的 Gizmo 绘制层？

3. **在编辑器中模拟触摸输入**：前轮 PK 确认摇杆走 FairyGUI `onTouchBegin/Move/End`。但**编辑器的鼠标 click 能否正确模拟 Touch 行为？** FairyGUI 在 Editor 模式下是否需要特殊配置才能用鼠标模拟手指？

**期望守方回答**：摇杆参数的配置方式和编辑器调试方案。

**状态**：🟡 待回应

---

### SGUI-E07 | 安全区适配的编辑器验证——不同机型模拟

**质疑**：§四定义了安全区适配规则（刘海屏兼容、底部指示条区域）。**这些适配规则在编辑器中如何验证？**

**具体问题**：

1. **不同机型的安全区模拟**：iPhone X 的安全区 inset(44,0,34,0) vs iPhone 6 的 inset(20,0,0,0) vs 微信小游戏的 `wx.getSystemInfoSync().safeArea`——在 Unity 编辑器中如何切换不同机型预览？是否有一个 `DeviceSimulator` 配置或自定义 EditorWindow 提供下拉选择机型 → 自动调整 Game View 比例 + 注入安全区 inset？

2. **暂停按钮"下移至安全区顶部内"**：这个"下移"量是动态计算的（基于设备安全区）。在 FairyGUI 布局中这是通过什么机制实现的？`GRoot.inst.margin`？自定义 C# 脚本在启动时读取安全区并调整？——**无论哪种方案，编辑器中如何验证不同设备的适配效果而不用每次推真机？**

3. **自动化验收**：是否可以写一个 `EditModeTest` 或 `EditorWindow` 验证——"在 iPhone X 安全区下，暂停按钮的 y 坐标 ≥ safeAreaTop"？这样每次 FairyGUI 包更新后自动跑一次，不需要人眼检查。

**期望守方回答**：安全区适配的编辑器验证方案。

**状态**：🟡 待回应

---

### SGUI-E08 | 飘字坐标转换的 Scene View 可视化

**质疑**：前轮 PK 确认了飘字坐标转换由 `BattleHUDController` 负责（WorldToScreenPoint → GlobalToLocal）。**这是一个经典的"看不到但很容易出错"的环节。**

**具体问题**：

1. **Scene View 辅助线**：击杀发生时，飘字应该出现在"击杀世界坐标映射到 UI 坐标"的位置。如果映射出错（比如 Camera 引用不对、UI 缩放因子不对），飘字会飘在奇怪的位置。**是否需要在 Scene View 中画一条 Debug 线——从世界坐标到 UI 坐标的映射关系？**

2. **极端坐标测试**：敌机在屏幕边缘被击杀时，飘字可能超出屏幕。文档没有说明超出屏幕时的行为——**是 clamp 到屏幕边缘？还是允许飘出？** 无论哪种，这需要一个编辑器测试工具——在不同屏幕坐标生成飘字来验证边界行为。

3. **坐标缓存 vs 实时计算**：Camera 可能在帧间移动。飘字生成时读取一次坐标还是每帧更新？如果读取一次——简单但 Camera 震屏时飘字不跟随。如果每帧更新——飘字跟随 Camera 抖动，视觉上"粘在场景上"。**文档没有定义这个行为。**

**期望守方回答**：坐标转换的可视化调试方案和边界行为定义。

**状态**：🟡 待回应

---

### SGUI-E09 | 按钮三态资源的完整性自动校验

**质疑**：§七 走查清单第一条"所有按钮有按下态/普通态/禁用态三态资源"。**这是一个人工检查项——能不能自动化？**

**具体问题**：

1. **FairyGUI 包发布后自动校验**：是否可以写一个 `AssetPostprocessor`（监控 `*.bytes` 导入）或 MenuItem，在 FairyGUI 包导入后自动解析包内容，检查：
   - 所有 Button 类型组件是否有 `over` / `down` / `disabled` 三个 Controller Page
   - ProgressBar 是否有 `bar` 子对象
   - LevelNode 是否有 3 态 Controller

2. **走查清单中的 8 项**——能不能全部转化为**自动化验证规则**？文档写了 8 个人工 check，但独立开发者没有 QA 团队——如果这些规则能在每次包发布后自动跑，等于免费的 QA。

3. **资源遗漏的早期发现**：如果设计师在 FairyGUI 中新增了一个 `Btn_Danger` 按钮但忘了做禁用态——**什么时候才会发现？** 如果没有自动校验，答案是"上线后玩家触发了禁用态才发现"。

**期望守方回答**：走查清单能否转化为自动化校验工具？

**状态**：🟡 待回应

---

### SGUI-E10 | 颜色/尺寸设计 Token 的集中管理

**质疑**：文档散落了大量具体的颜色值（`#1A1A2E`、`#4FC3F7`、`#EF5350`、`#3A3A4A`）和尺寸（`44pt`、`280pt`、`56pt`）。**这些值在 FairyGUI 和 Unity 两端如何保持一致？**

**具体问题**：

1. **设计 Token 的单一来源**：如果要把品牌色从 `#4FC3F7` 改为 `#42A5F5`——需要改几个地方？FairyGUI 编辑器里改组件 + Unity 端 SO/代码中改常量？如果是两处以上——**能否建立一个"设计 Token SO"作为唯一真相来源**（颜色、字号、间距等），FairyGUI 导出和 Unity 代码都从这个 SO 读取？

2. **FairyGUI 的局限**：FairyGUI 组件中的颜色值是编辑器内指定的——无法从运行时 SO 动态读取（除非代码中覆盖）。那实际的工作流是：
   - 设计 Token 文档 → 设计师在 FairyGUI 中手动设置 → C# 代码中也硬编码相同值
   - **两份拷贝必然会不同步。** 是否有工具能在 FairyGUI 导出后校验"FairyGUI 中的颜色值 vs Design Token SO 中的颜色值"一致？

3. **字号一致性**：文档提到 36pt/24pt/20pt/18pt 等多种字号。FairyGUI 使用 pt 单位，Unity 场景中的 TextMeshPro（如果有运行时文字）使用 Unity 单位。**两者的 pt 是否等价？** 如果不等价，需要一个转换系数——这个系数在哪里配置？

**期望守方回答**：设计 Token 的管理策略和跨工具一致性方案。

**状态**：🟡 待回应

---

**攻方总结**：10 个问题，聚焦 **"UI 设计文档描述了'做什么'但没回答'在编辑器中怎么高效做'"**。核心诉求：一个独立开发者需要**零 QA 团队也能高效迭代 UI**——这要求编辑器工具层提供：自动化校验（替代人工走查）、集中监视窗口（替代散落 SO 逐个点开）、参数化配置（替代改代码等编译）、坐标/适配可视化（替代推真机验证）。UI 文档应补充一个"编辑器工具需求"小节——即使 V1 不全做，也要列出优先级。

---

## PK Round 2 — 守方回应（UI 设计师）

> 逐条回应。立场：编辑器工具是**实施层**关注点，UI 设计文档的本职是定义"用户体验规格"而非"开发者编辑器体验"。但承认：独立开发者 = 设计师+程序员+QA 一肩挑，编辑器效率直接影响迭代速度。对合理质疑给出方案或接受建议。

---

### SGUI-E01 回应 | ✅ 部分接受

**核心观点**：攻方说得对——21 个 SO 散落在文件夹里不直观。但这是"编辑器工具需求"不是"UI 设计文档"的职责。

**具体回答**：

1. **集中监视窗口**：✅ 接受建议。V1 优先级 P1——一个简单的 `EditorWindow "Tools/SG/战斗状态面板"` 列出所有 `SG_*` 变量的实时值。实现成本 < 30 行代码（`FindAssets("t:FloatVariable") + FindAssets("t:IntVariable")` → 循环 `EditorGUILayout.FloatField`）。

2. **CustomEditor**：框架已有 `FloatVariable` / `IntVariable` 的基础 Inspector（显示 Value + Description）。Play Mode 下 Inspector 自动刷新（`[ExecuteInEditMode]` 不需要，SO 的 Inspector 本身就实时显示 Value 属性）。但同意——加一个 `EditorGUILayout.ProgressBar` 给 `SG_BaseHP` 这类 ratio 型 SO 会更直观。

3. **`[Range(0,1)]` + ProgressBar PropertyDrawer**：✅ 接受。在框架层给 `FloatVariable` 增加一个 `[RatioDisplay]` attribute，有这个 attribute 时 Inspector 额外显示一个可视化进度条。**但这属于 TDD / 编辑器工具 TDD 范畴，不在 UI 设计文档中指定。**

**结论**：接受"补充编辑器工具需求"的建议。但具体实现方案写在编辑器工具 TDD 中，UI 设计文档只需引用。

---

### SGUI-E02 回应 | ✅ 部分接受

**核心观点**：工作流断裂是真实问题——但 FairyGUI 本身有解决方案，只是文档没写。

**具体回答**：

1. **命名一致性**：FairyGUI 官方推荐用 `UIPanel` 组件的**代码生成功能**——发布时自动生成 `XXX.cs` 绑定类，所有子组件引用变成**强类型属性**而非字符串。如果重命名组件 → 重新发布 → 生成代码更新 → 编译报错（编译时发现而非运行时）。
   - **但条件是**：必须使用 FairyGUI 的代码生成而非手写 `GetChild("xxx")`。这是一个**工作流约定**——应该在编码规范中强制，而非 UI 设计文档中写。
   - ✅ 接受：在 UI 文档 §五 末尾补充一句——"所有界面使用 FairyGUI 代码生成绑定，禁止手写字符串引用"。

2. **一键发布**：FairyGUI 编辑器自身有"发布到 Unity 项目路径"的设置——设计师点"发布"就直接输出到 `Assets/FairyGUI/` 目录。Unity `AssetDatabase` 自动检测变更并 reimport。
   - **不需要额外工具**。FairyGUI 编辑器 → 发布 → Unity 自动 Reimport → 代码生成（如有变更则编译报错）。
   - 唯一值得做的：一个 `AssetPostprocessor` 在 `.bytes` 文件导入时自动 Log 一行 "FairyGUI 包 [Battle] 已更新"——帮助在 Console 中确认。

3. **pt 与 Unity 坐标**：FairyGUI 在 Unity 中是独立渲染层（GRoot），使用**逻辑坐标系**（pt）。FairyGUI 编辑器中设计的 750×1334 布局在 Unity 中由 `UIContentScaler` 自动适配到实际屏幕分辨率。**不需要转换工具**——FairyGUI 的设计稿就是最终效果。Game View 调成 750×1334 比例就是 1:1 预览。

**结论**：接受补充"使用代码生成绑定"的约定。其他两点由 FairyGUI 自身机制解决，不需要额外工具。

---

### SGUI-E03 回应 | ✅ 完全接受（原文已有答案）

**核心观点**：文档 §5.2 已经定义了 `LevelNode` 是一个 **FairyGUI Component**。5 个关卡节点是该 Component 的 5 个**实例**。

**具体回答**：

1. **复用策略**：✅ 已明确——`LevelNode` 是 FairyGUI 的一个 Component 定义，在 `LevelSelectScreen` 中实例化 5 次。修改 Component 模板自动同步所有实例。这是 FairyGUI 的核心机制。

2. **三态驱动**：FairyGUI Controller 管理三态（FairyGUI 编辑器内定义视觉差异：颜色、图标、动效）。C# 端只负责 `node.controller.selectedIndex = stateEnum`。
   - 颜色"硬编码"在 FairyGUI 中——这是**预期行为**。UI 设计师在 FairyGUI 编辑器中所见即所得地调颜色，比在 Unity SO 中填 hex 值更直观。
   - 只有**需要运行时动态改变的值**才走 SO（如血条颜色随 HP 变化）。关卡节点的三态颜色是固定设计，不需要运行时变更——FairyGUI 内定义即可。

3. **一致性保障**：由于是同一个 Component 的实例——**天然一致**。除非有人"解除链接"（断开实例与模板的关系）。FairyGUI 编辑器中这个操作需要显式右键→"取消组件引用"——不会误触。

**结论**：完全收敛。FairyGUI 的 Component 实例机制已解决此问题。

---

### SGUI-E04 回应 | ✅ 部分接受

**核心观点**：调试可观测性确实是独立开发者的刚需。但分 V1/V2 优先级。

**具体回答**：

1. **血条预损调试**：✅ 接受。方案：给 `BattleHUDController` 加 `#if UNITY_EDITOR` 区域，用 `[SerializeField, Header("Debug (Editor Only)")]` 暴露 `_displayHP` 和 `_targetHP`。Inspector 中直接可见，零额外工具成本。

2. **飘字池状态**：✅ 接受，但 P1 优先级。V1 方案：在 `BattleHUDController` 的 Debug 区域暴露 `_activeFloatingTextCount`（一个 int）。如果池耗尽，Console 打一行 Warning "FloatingText pool exhausted!"。
   - 完整的 Gizmo 可视化是 V2——独立开发者 V1 用 Console Log 足够 debug。

3. **集中 Debug Dashboard**：✅ 接受，优先级 P1。与 SGUI-E01 的"战斗状态面板"合并——一个 EditorWindow 显示：
   - 当前 WaveIndex / TotalWaves
   - BaseHP (ratio + ProgressBar)
   - KillCount / TotalEnemyCount
   - Active Entity Count (from EntityPool)
   - FloatingText Active/Total

4. **Profiler Marker**：✅ 接受。在 `BattleHUDController.Update()` 中用 `Unity.Profiling.ProfilerMarker` 包裹关键路径（Lerp 更新 + 飘字更新）。成本 = 2 行代码 / 方法。V1 就做——零运行时开销（Release 构建中 Marker 自动移除）。

**结论**：全部接受，但分优先级：
- P0（V1 必做）：`#if UNITY_EDITOR` Debug 字段 + Console Warning + ProfilerMarker
- P1（V1 有则更好）：EditorWindow 集中面板

---

### SGUI-E05 回应 | 🟡 部分拒绝

**核心观点**：转场预览工具的 ROI 对独立开发者 V1 不划算。

**具体回答**：

1. **转场预览工具**：❌ V1 不做。理由：
   - 5 种转场，每种只有 2~3 个参数（时长 + ease + overlay 颜色）。
   - 进 Play Mode → 点选关节点 → 观看转场 = **10 秒一次迭代**。做一个 EditorWindow 预览工具开发需要 2~4 小时。
   - ROI：工具开发 3 小时 / 每次节省 5 秒 = **2160 次迭代才回本**。独立开发者不会迭代 2000 次转场。
   - **V2 备忘**：如果后续关卡增多（20+关），转场变化更复杂，再做。

2. **Ease 曲线 SO 化**：✅ 接受。`TransitionConfigSO` 暴露 `AnimationCurve`——Inspector 可拖曲线。比代码里写 `EaseType.OutBack` 枚举直观。**但注意 FairyGUI GTween 用自己的 EaseType 枚举，不用 Unity AnimationCurve。** 解决方案：SO 中存 `EaseType` 枚举 + `float duration`——Unity Inspector 用 `CustomEditor` 画一个 Ease 曲线预览图（只读，帮助可视化，但实际值还是枚举）。

3. **模拟重试 MenuItem**：✅ 接受。`[MenuItem("Tools/SG/模拟重试(Play Mode Only)")]` — 一行代码调用 `BattleController.SimulateRetry()`。Play Mode 下才生效。开发成本 < 5 分钟，价值大——避免"打一整关再输"来测试重试逻辑。

**结论**：转场预览 EditorWindow V1 不做（ROI 不划算）；Ease SO 化和模拟重试 MenuItem 接受。

---

### SGUI-E06 回应 | ✅ 完全接受

**核心观点**：摇杆参数化 = SO 驱动是正确的。

**具体回答**：

1. **SO 驱动**：✅ 所有摇杆参数存入 `JoystickConfigSO`：
   ```
   [CreateAssetMenu]
   public class JoystickConfigSO : ScriptableObject {
       [Header("感应参数")]
       public float DeadZone = 8f;
       public float MaxRadius = 60f;
       
       [Header("动效")]
       public float AppearDuration = 0.1f;
       public float DisappearDuration = 0.15f;
       public float Alpha_Base = 0.3f;
       public float Alpha_Stick = 0.6f;
   }
   ```
   - Play Mode 中改值即时生效（FairyGUI 组件每帧读 SO）→ 手感迭代无需重编译

2. **Gizmo 可视化**：✅ 接受。`#if UNITY_EDITOR` 在 Scene View 画：
   - 内圆（死区 8pt → 换算为世界单位）
   - 外圆（最大半径 60pt）
   - 当前摇杆方向向量线段
   - 但注意——摇杆在 FairyGUI 逻辑坐标系内，Gizmo 在 Unity 世界坐标系。需要一个坐标转换。**V1 用 OnGUI + Handles 在 Game View 叠加绘制更合适**。

3. **鼠标模拟 Touch**：FairyGUI 默认在 Editor 中将**鼠标事件映射为 Touch 事件**（`onTouchBegin` 响应 `mouseDown`）。无需特殊配置。这是 FairyGUI Runtime 内置行为——Editor 中鼠标 click = 手指 tap，鼠标 drag = 手指 move。

**结论**：完全接受。JoystickConfigSO + Editor Gizmo（Game View 叠加）+ 鼠标已原生模拟 Touch。

---

### SGUI-E07 回应 | ✅ 部分接受

**核心观点**：安全区验证确实需要——但方案应务实。

**具体回答**：

1. **设备模拟**：Unity 2021+ 自带 **Device Simulator**（Window → General → Device Simulator）。内置 iPhone X / iPhone 8 / Samsung 等设备档案，自动注入 `Screen.safeArea`。FairyGUI 的 `UIContentScaler` 读取 `Screen.safeArea` 并应用 margin。
   - **不需要自定义工具**——Unity Device Simulator 已覆盖此需求。
   - 补充文档中注明：_"开发时使用 Unity Device Simulator 验证不同设备安全区适配"_。

2. **安全区适配机制**：C# 脚本 `SafeAreaAdapter`（框架已有）在启动时读取 `Screen.safeArea` → 调整 FairyGUI `GRoot` 的 `margin`。Editor 中通过 Device Simulator 注入不同 safeArea → 自动适配。

3. **自动化验收 Test**：✅ 接受，P2 优先级。一个 EditMode Test：
   ```
   [Test] void PauseButton_IsWithinSafeArea_iPhoneX() {
       // Mock Screen.safeArea = new Rect(0, 44, 750, 1246)
       // Assert pauseButton.y >= 44
   }
   ```
   - V1 用 Device Simulator 目视即可。V2 补自动化测试。

**结论**：Unity Device Simulator 已解决大部分需求。补文档引用 + V2 自动化测试。

---

### SGUI-E08 回应 | ✅ 部分接受

**核心观点**：坐标转换调试有价值，但 V1 的复杂度不大（固定 Camera），不需要重型方案。

**具体回答**：

1. **Scene View 辅助线**：✅ 接受 P1。在 `BattleHUDController` 的 `#if UNITY_EDITOR` 区域，每次生成飘字时 `Debug.DrawLine(worldPos, screenPosAsWorldPoint, Color.yellow, 0.8f)`。Scene View 中可见黄色连线——从击杀点到飘字目标位置。

2. **边界行为定义**（这确实是 UI 设计文档该回答的）：
   - **飘字不做 clamp**。理由：飘字有 0.8s 淡出动效，在屏幕边缘被击杀时飘字可能有 1~2 帧超出屏幕——但它正在淡出，玩家不会注意到"少了一个飘字"。Clamp 反而看起来不自然（飘字贴在边缘停住）。
   - **V1 行为**：飘字从击杀点生成，向上漂浮+淡出。不 clamp、不跟随、不遮挡——自然物理行为。
   - 补充到 UI 文档 §3.4 飘字行为中。

3. **坐标计算时机**：生成时**读取一次世界坐标 → 转换为 UI 坐标 → 飘字在 UI 坐标系中向上偏移**。不每帧重算。
   - 理由：Camera 震屏时飘字"脱离场景"是预期效果——飘字是 HUD 层元素，不属于世界空间。玩家直觉中飘字是"UI 上的文字"而非"场景中的物体"。
   - 这个决策补充到 UI 文档。

**结论**：接受 Debug 辅助线 + 明确边界行为定义（不 clamp、一次计算不跟随）。

---

### SGUI-E09 回应 | ✅ 接受（精简方案）

**核心观点**：自动化校验好主意——但要控制实现复杂度。

**具体回答**：

1. **FairyGUI 包结构自动校验**：✅ 接受，但用最简方案。FairyGUI 的 `.bytes` 文件是二进制格式，解析成本高。更好的方案：
   - FairyGUI 编辑器发布时会生成 **package.xml**（描述文件）——列出所有组件及其子元素。
   - 写一个 `MenuItem "Tools/SG/校验 FairyGUI 包"` 读取 `package.xml` → 检查：
     - Button 组件是否有 `over`/`down` page
     - ProgressBar 是否有 `bar` 子对象
   - 输出：Console 中列出"通过/失败"项。

2. **走查清单 → 自动化**：8 项中可自动化的：
   | # | 走查项 | 可自动化？ | V1 做？ |
   |---|--------|-----------|---------|
   | 1 | 按钮三态 | ✅ package.xml 解析 | P1 |
   | 2 | 血条三色 | ❌ 需视觉判断 | 人工 |
   | 3 | 摇杆资源 | ✅ 检查组件存在性 | P1 |
   | 4 | 关卡节点三态 | ✅ Controller page 数量 | P1 |
   | 5 | 弹窗背景统一 | ❌ 视觉一致性 | 人工 |
   | 6 | 字体统一 | ✅ 检查 font 属性 | P2 |
   | 7 | 飘字描边 | ❌ 视觉判断 | 人工 |
   | 8 | 波次文字关键帧 | ✅ Transition 存在性 | P2 |

   8 项中 5 项可自动化。V1 做前 3 项（P1），其余 V2。

3. **发现时机**：有了自动校验 → 每次 FairyGUI 包发布后跑一次 → 立即发现遗漏。**不再依赖上线后玩家触发。**

**结论**：接受自动化校验方案。V1 做 3 项高价值检查（MenuItem），V2 扩展。

---

### SGUI-E10 回应 | 🟡 部分拒绝

**核心观点**：设计 Token 中央管理对大团队有价值，但独立开发者 V1 的 ROI 不划算。

**具体回答**：

1. **设计 Token SO 方案**：❌ V1 不做。理由：
   - 独立开发者 = 1 个人同时操作 FairyGUI 和 Unity。不存在"两个人各改各的不同步"场景。
   - FairyGUI 中改颜色 → 发布 → 自己去 Unity 测试。如果颜色不对，自己改——反馈环路 < 30 秒。
   - 做一个 Design Token SO + 校验工具 → 开发 2~4 小时。**为一个人的项目做跨工具同步校验 = 过度工程化。**

2. **V2 备忘**：如果后续有美术外包参与（多人协作），那 Design Token 中央管理有价值。记在 backlog。

3. **字号一致性**：FairyGUI 的 pt = **逻辑像素**（等效于设计稿像素）。整个项目全部文字都在 FairyGUI 体系内渲染——**不使用 TextMeshPro**。所以不存在 pt 换算问题。FairyGUI 36pt = 屏幕上 36 逻辑像素 = 设计稿 36pt。一一对应，无需转换。

**结论**：V1 不做 Design Token 中央管理（独立开发者单人操作无同步问题）。确认 pt 单位一致无需转换。V2 多人协作时再评估。

---

## Round 2 守方统计

| 问题编号 | 守方裁定 | 具体动作 |
|----------|----------|----------|
| SGUI-E01 | 部分接受 | V1 P1 做集中监视 EditorWindow + FloatVariable ProgressBar 增强 |
| SGUI-E02 | 部分接受 | 补充"使用 FairyGUI 代码生成绑定"约定 + AssetPostprocessor Log |
| SGUI-E03 | 完全接受 | FairyGUI Component 实例机制已解决，无需额外动作 |
| SGUI-E04 | 部分接受 | P0: Debug 字段+Console Warning+ProfilerMarker / P1: EditorWindow |
| SGUI-E05 | 部分拒绝 | 转场预览 EditorWindow V1 不做 / Ease SO+模拟重试 MenuItem 接受 |
| SGUI-E06 | 完全接受 | JoystickConfigSO + Game View Gizmo 叠加 + 鼠标已模拟 Touch |
| SGUI-E07 | 部分接受 | Unity Device Simulator 已覆盖 / 文档补引用 / V2 自动化测试 |
| SGUI-E08 | 部分接受 | Debug.DrawLine 辅助线 + 明确飘字不 clamp、一次计算不跟随 |
| SGUI-E09 | 接受 | V1 做 3 项高价值 FairyGUI 包校验（MenuItem）/ V2 扩展 5 项 |
| SGUI-E10 | 部分拒绝 | V1 不做 Design Token 中央管理 / 确认 pt 无需转换 |

**守方总结**：10 个问题中完全接受 2 个、部分接受 6 个、部分拒绝 2 个。拒绝理由统一：**独立开发者 V1 的 ROI 不划算——做工具的时间 > 手动操作节省的时间**。核心原则：**"先做游戏，再做工具。工具只在痛点重复 >10 次时才值得投入。"**

---

## PK Round 3 — 攻方反驳/追问（编辑器工具开发者）

> 守方务实，大部分决策合理。但有 3 处需要追问——不是为了"加更多工具"，而是**确认工具缺失不会在 V1 实施中造成实际阻塞**。

---

### SGUI-E02 追问 | FairyGUI 代码生成的实操约束

**守方说**：使用 FairyGUI 代码生成绑定，重命名时编译报错即可发现。

**追问**：

1. **FairyGUI 代码生成 ≠ 零配置**。代码生成需要在 FairyGUI 编辑器中为每个需要绑定的组件设置"导出类型=自定义类"，并且在 Unity 端有对应的 partial class 模板。**如果漏了配置——某个组件没有生成绑定类——那它的子元素引用就只能用字符串 `GetChild()`。** 问题：
   - 5 个包 × 8 个关键组件——每个都需要手动设置"导出类型"。**有没有一个批量检查——"所有在 §5.2 列出的组件是否都配置了代码生成"？**
   - 如果没有这个检查，等同于走查清单 §七 的"按钮三态"问题——漏一个到运行时才发现。

2. **代码生成的文件输出位置**：生成的 `XXXBinder.cs` 放在哪个目录？如果放在 FairyGUI 包的导出目录（会被下次发布覆盖）vs 放在 `Scripts/UI/Generated/`（手动维护路径映射）。**生成代码的目录约定是什么？**

3. **增量 vs 全量生成**：设计师只改了 `Battle` 包中的 `ProgressBar_HP` 组件——是全量重新生成所有 5 个包的绑定代码？还是只生成 `Battle` 包的？如果全量生成——每次小改动后 Git diff 会很大（所有生成文件 timestamp 变更）。

**期望守方回答**：代码生成的具体配置约定和文件管理策略。

**状态**：🟡 待回应

---

### SGUI-E05 追问 | "模拟重试"MenuItem 的边界条件

**守方说**：接受 `[MenuItem("Tools/SG/模拟重试")]` 一行代码调用 `BattleController.SimulateRetry()`。

**追问**：

1. **SimulateRetry 的前置条件**：这个 MenuItem 在什么状态下才能调用？
   - Play Mode + 战斗场景加载 + BattleController 实例存在——全部满足才行
   - 如果在选关界面误触这个 MenuItem → `BattleController` 为 null → NullRef
   - **是否需要 `[MenuItem("Tools/SG/模拟重试", validate = true)]` 的 validate 方法——检查当前是否在战斗中？**

2. **除了"模拟重试"，还有哪些 Debug MenuItem 值得 V1 做？** 以我的编辑器工具经验，战斗类游戏通常需要：
   - `模拟重试`（已接受）
   - `直接胜利`（跳过所有波次，直接触发胜利流程）
   - `直接失败`（基地 HP 归零）
   - `跳到下一波`（当前波次立即结束，开始下一波）
   - `设置基地 HP`（输入百分比 → 写入 SO → 立刻看 UI 变化）
   
   这些每个 < 5 行代码，合计 < 30 分钟开发。**但能将"测试完整战斗流程"的时间从 3 分钟/次 降到 3 秒/次。** 这个 ROI 对 5 关 × 多次迭代来说肯定回本。

**期望守方回答**：是否扩展 Debug MenuItem 集？

**状态**：🟡 待回应

---

### SGUI-E10 追问 | 独立开发者的"记忆力"问题

**守方说**：独立开发者 = 1 个人，不存在两个人不同步的问题。改了 FairyGUI 自己去 Unity 测——反馈环路 < 30 秒。

**追问**：

1. **独立开发者最大的对手不是队友——是"一周后的自己"。** 今天改了品牌色 `#4FC3F7` → `#42A5F5`，在 FairyGUI 5 个包的 `Btn_Primary` 中都改了。3 天后回来做另一个功能，代码里 `_highlightColor = new Color(0.31f, 0.76f, 0.97f)` 还是旧颜色——**自己已经忘了上次改过什么。**

2. **最小成本的"真相来源"方案**：不需要 Design Token SO + 自动校验。只需要一个**纯文档**——`DESIGN_TOKENS.md`：
   ```markdown
   ## 颜色
   | Token | Hex | 用途 |
   |-------|-----|------|
   | Brand_Primary | #42A5F5 | 主按钮底色 |
   | BG_Dark | #1A1A2E | Loading背景 |
   ...
   ```
   - 改颜色时：先改文档 → 再改 FairyGUI + 代码。文档是 checklist。
   - 零代码成本，但解决"一周后忘了改哪里"的问题。

3. **这不是工具需求——是文档需求。** UI 设计文档应该有一个 **Design Token 表**——不需要自动化执行，只需要"有这张表"就够了。当下次改颜色时，开发者知道去看这张表确认所有引用点。

**期望守方回答**：是否接受在 UI 文档中补充一个 Design Token 参考表（纯文档，不做自动化）？

**状态**：🟡 待回应

---

### Round 3 已收敛条目（无需追问）

| 编号 | 状态 |
|------|------|
| SGUI-E01 | ✅ 完全收敛（P1 EditorWindow + ProgressBar 增强） |
| SGUI-E03 | ✅ 完全收敛（FairyGUI Component 实例机制） |
| SGUI-E04 | ✅ 完全收敛（P0 Debug 字段 + ProfilerMarker / P1 Dashboard） |
| SGUI-E06 | ✅ 完全收敛（JoystickConfigSO + Game View Gizmo） |
| SGUI-E07 | ✅ 完全收敛（Device Simulator + V2 测试） |
| SGUI-E08 | ✅ 完全收敛（Debug.DrawLine + 不 clamp + 一次计算） |
| SGUI-E09 | ✅ 完全收敛（V1 3 项 MenuItem 校验） |

**攻方 Round 3 总结**：7/10 已收敛。剩余 3 个追问：1 个关于工作流约定细节（E02），1 个关于 Debug 工具集扩展（E05），1 个关于文档层 Design Token 表（E10）。方向无争议——都是"再确认一下具体怎么做"。

---

## PK Round 4 — 守方二次回应（UI 设计师）

> 3 个追问都合理，快速对齐。

---

### SGUI-E02 二次回应 | ✅ 同意补充约定

**逐条回答**：

1. **批量检查代码生成配置**：✅ 接受。但不需要编辑器工具——用**文档 checklist** 替代：
   - 在 UI 文档 §五 新增一列"代码生成"：

   | 组件 | 类型 | 代码生成 | 生成类名 |
   |------|------|----------|----------|
   | Btn_Primary | Button | ✅ | Btn_Primary |
   | Btn_Secondary | Button | ✅ | Btn_Secondary |
   | ProgressBar_HP | ProgressBar | ✅ | ProgressBar_HP |
   | LevelNode | Component | ✅ | LevelNode |
   | Joystick | Component | ✅ | Joystick |
   | FloatingText | Component | ✅ | FloatingText |
   | WaveNotice | Component | ✅ | WaveNotice |
   | Btn_Icon | Button | ✅ | Btn_Icon |

   - 规则：**§5.2 列出的所有组件必须配置代码生成**。发布时逐项确认。
   - V2 可以做自动化（解析 package.xml 验证），但 V1 8 个组件手动确认 < 1 分钟。

2. **生成代码目录约定**：
   - 输出路径：`Assets/_Game/Scripts/UI/Generated/` — 独立目录，不与手写代码混合
   - 文件命名：`{PackageName}Binder.cs`（如 `BattleBinder.cs`）
   - **Git 管理**：生成代码**纳入 Git**（不是 .gitignore）。理由：确保其他人（或未来的自己）clone 后直接编译通过，不依赖本地 FairyGUI 编辑器环境。
   - 这个约定写在编码规范文档中，UI 文档 §五 引用即可。

3. **增量 vs 全量**：FairyGUI 代码生成是**按包**生成的——改 `Battle` 包只重新生成 `BattleBinder.cs`，不影响 `PopupBinder.cs`。Git diff 只有改动的包。这是 FairyGUI 代码生成的默认行为，不需要配置。

**SGUI-E02 最终结论**：✅ 完全收敛。补充 §5.2 代码生成列 + 目录约定。

---

### SGUI-E05 二次回应 | ✅ 完全接受——扩展 Debug MenuItem 集

**逐条回答**：

1. **validate 方法**：✅ 必须加。所有 SG Debug MenuItem 都加 validate：
   ```csharp
   [MenuItem("Tools/SG/模拟重试", true)]
   static bool ValidateSimulateRetry() => Application.isPlaying && BattleController.Instance != null;
   ```
   非战斗状态下 MenuItem 灰色不可点。

2. **V1 Debug MenuItem 完整集**：✅ 全部接受。确认如下：

   | MenuItem | 快捷键 | 功能 | 实现成本 |
   |----------|--------|------|----------|
   | Tools/SG/模拟重试 | — | BattleController.SimulateRetry() | 5 行 |
   | Tools/SG/直接胜利 | — | EntitySpawner.ForceAllWavesCleared() | 3 行 |
   | Tools/SG/直接失败 | — | SG_BaseHP.Value = 0 | 2 行 |
   | Tools/SG/跳到下一波 | — | EntitySpawner.ForceNextWave() | 5 行 |
   | Tools/SG/设置基地 HP... | — | 弹 EditorInputDialog 输入 0~100 → SG_BaseHP.Value = input/100f | 8 行 |

   总计 ~25 行代码，开发 < 20 分钟。但后续每次测试战斗流程节省 2~3 分钟。5 关 × 5 次迭代 = 节省 ~1 小时。**ROI 3:1，值得。**

3. **统一放在一个类中**：`SG_DebugMenuItems.cs`，放在 `Assets/_Game/Editor/` 目录。

**SGUI-E05 最终结论**：✅ 完全收敛。V1 做 5 个 Debug MenuItem + validate。

---

### SGUI-E10 二次回应 | ✅ 接受——补充 Design Token 表

**核心观点**：攻方说得对——"一周后的自己"确实会忘。纯文档表 = 零代码成本 + 解决记忆问题。

**具体回答**：

1. **接受在 UI 文档中新增 Design Token 表**。具体内容：

   **§十 设计 Token 参考表**

   #### 颜色
   | Token | Hex | RGB | 用途 |
   |-------|-----|-----|------|
   | Brand_Primary | #4FC3F7 | 79,195,247 | 主按钮底色 |
   | BG_Dark | #1A1A2E | 26,26,46 | Loading/战斗背景 |
   | Btn_Secondary | #3A3A4A | 58,58,74 | 次按钮底色 |
   | Danger | #EF5350 | 239,83,80 | 危险操作/红闪 |
   | HP_Green | — | — | 血量 >50% |
   | HP_Yellow | — | — | 血量 30~50% |
   | HP_Red | — | — | 血量 <30% |
   | Text_White | #FFFFFF | — | 主文字 |
   | Text_LightGray | — | — | 次要文字 |

   #### 字号
   | Token | 大小 | 用途 |
   |-------|------|------|
   | Title_Large | 36pt | 游戏名/VICTORY |
   | Title_Medium | 24pt | 加载文字/DEFEAT |
   | Body | 20pt | 波次指示/统计数据 |
   | Caption | 18pt | 血量数字/飘字 |
   | Button_Primary | 24pt | 主按钮文字 |
   | Button_Secondary | 20pt | 次按钮文字 |

   #### 间距与尺寸
   | Token | 值 | 用途 |
   |-------|-----|------|
   | Btn_Primary_W | 280pt | 主按钮宽 |
   | Btn_Primary_H | 56pt | 主按钮高 |
   | Btn_Corner | 28pt | 主按钮圆角 |
   | HitArea_Min | 44pt | 最小触摸热区 |
   | HP_Bar_H | 12pt | 血条高度 |
   | Joystick_DeadZone | 8pt | 摇杆死区 |
   | Joystick_MaxRadius | 60pt | 摇杆最大偏移 |

2. **使用方式**：改颜色/尺寸时——先改 Token 表 → 再改 FairyGUI + 代码。Token 表 = 自查 checklist。

3. **不做自动化校验**（V1）：Token 表是"文档级"的参考，不是代码级的约束。V2 多人协作时再评估自动化。

**SGUI-E10 最终结论**：✅ 完全收敛。补充 §十 Design Token 参考表。

---

## Round 4 守方统计

| 追问编号 | 结论 |
|----------|------|
| SGUI-E02 追问 | ✅ 完全收敛 — §5.2 代码生成列 + 目录约定 + 按包增量生成 |
| SGUI-E05 追问 | ✅ 完全收敛 — V1 做 5 个 Debug MenuItem + validate |
| SGUI-E10 追问 | ✅ 完全收敛 — 补充 §十 Design Token 参考表（纯文档） |

**守方 Round 4 总结**：10/10 全部收敛。攻守双方无分歧。

---

## PK Round 5 — 攻方终轮确认（编辑器工具开发者）

> 全部 10 个问题已收敛。攻方确认无新质疑。

**确认结论**：守方在"ROI 驱动"原则下做了正确的取舍——V1 只做高 ROI 的编辑器工具，拒绝了两个过度工程化提案（转场预览 EditorWindow、Design Token 自动校验）但接受了文档层的轻量替代方案。作为编辑器工具开发者，认可以下结果：

1. **参数化配置**：JoystickConfigSO + TransitionConfigSO（Ease 枚举 + duration） → Inspector 热改无需重编译 ✅
2. **Debug 可观测性**：`#if UNITY_EDITOR` Debug 字段 + ProfilerMarker + Console Warning → 零运行时开销 ✅
3. **Debug MenuItem 集**：5 个战斗调试快捷方式（重试/胜利/失败/跳波/设HP） → 单次测试 3min → 3s ✅
4. **FairyGUI 代码生成约定**：强类型绑定 + 按包增量生成 + Generated 目录纳入 Git ✅
5. **自动化校验（P1）**：3 项 FairyGUI 包完整性 MenuItem 校验 ✅
6. **集中监视面板（P1）**：EditorWindow 实时显示战斗状态 SO 值 ✅
7. **Design Token 表（文档级）**：颜色/字号/尺寸参考表作为 self-check 清单 ✅
8. **Device Simulator**：已有方案无需自建 ✅

**攻方唯一补充建议**（非质疑）：

建议 UI 文档新增一个章节：
- **§十一 编辑器工具需求（优先级表）**：将本轮 PK 收敛的所有编辑器工具需求汇总为 P0/P1/P2 优先级表，作为 TDD 实施的输入。

**攻方最终裁定**：✅ 全部通过，无遗留问题。

---

## PK Round 6 — 最终收敛判定

### 收敛状态

| 问题编号 | 最终状态 | 收敛轮次 |
|----------|----------|----------|
| SGUI-E01 | ✅ 收敛 | R2 |
| SGUI-E02 | ✅ 收敛 | R4 |
| SGUI-E03 | ✅ 收敛 | R2 |
| SGUI-E04 | ✅ 收敛 | R2 |
| SGUI-E05 | ✅ 收敛 | R4 |
| SGUI-E06 | ✅ 收敛 | R2 |
| SGUI-E07 | ✅ 收敛 | R2 |
| SGUI-E08 | ✅ 收敛 | R2 |
| SGUI-E09 | ✅ 收敛 | R2 |
| SGUI-E10 | ✅ 收敛 | R4 |

### PK 评审结论

- **总问题数**：10
- **收敛率**：100%（10/10）
- **实际使用轮次**：4 轮（Round 5-6 为确认+判定，无新争议）
- **UI 文档需回写条目数**：7 处修改
- **拒绝的提案**：2 个（转场预览 EditorWindow / Design Token 自动校验），均因 V1 ROI 不划算

### UI 文档回写清单（待执行）

| # | 修改位置 | 修改内容 |
|---|----------|----------|
| 1 | §五 §5.2 | 新增"代码生成"列——8 个组件全部标注✅ + 生成类名 |
| 2 | §五 末尾 | 补充"FairyGUI 代码生成约定"：导出类型设置 + 输出目录 `Scripts/UI/Generated/` + 纳入 Git |
| 3 | §3.4 飘字行 | 补充"飘字不做 clamp、生成时一次计算坐标不跟随 Camera" |
| 4 | 新增 §十 | Design Token 参考表（颜色 9 项 + 字号 6 项 + 间距尺寸 7 项） |
| 5 | 新增 §十一 | 编辑器工具需求优先级表（P0/P1/P2） |
| 6 | §七 走查清单 | 标注哪些可自动化（5/8 项）+ 引用 MenuItem 校验工具 |
| 7 | §2.3 摇杆 | 注明参数来源为 `JoystickConfigSO`（SO 驱动，非硬编码） |

### 编辑器工具 V1 需求优先级总表

| 优先级 | 工具 | 实现成本 | 收益 |
|--------|------|----------|------|
| **P0** | Debug 字段（`#if UNITY_EDITOR`）+ ProfilerMarker | 30 分钟 | 运行时可观测 |
| **P0** | 5 个 Debug MenuItem（重试/胜利/失败/跳波/设HP） | 20 分钟 | 测试效率 60x |
| **P0** | JoystickConfigSO（摇杆参数 SO 化） | 15 分钟 | 手感调参无需编译 |
| **P1** | 战斗状态监视 EditorWindow | 1 小时 | 集中看所有 SO 值 |
| **P1** | FairyGUI 包校验 MenuItem（3 项检查） | 1 小时 | 替代人工走查 |
| **P1** | Debug.DrawLine 飘字坐标辅助线 | 15 分钟 | 坐标问题快速定位 |
| **P1** | Game View 摇杆 Gizmo 叠加 | 30 分钟 | 可视化死区/半径 |
| **P2** | TransitionConfigSO（Ease + Duration）| 30 分钟 | 转场参数 SO 化 |
| **P2** | SafeArea 自动化 EditMode Test | 1 小时 | 适配回归测试 |
| **P2** | FairyGUI 字体/Transition 校验扩展 | 1 小时 | 走查清单全自动化 |

**P0 合计**：~1 小时 | **P1 合计**：~3 小时 | **P2 合计**：~2.5 小时

### PK 最终评价

**攻方视角**：UI 设计文档 v1.0 完全没有涉及"编辑器体验"——这对大团队不是问题（有专门的工具程序员），但对独立开发者是效率瓶颈。经过 4 轮对齐，确定了 P0 (~1h) + P1 (~3h) 的编辑器工具需求清单，合计 ~4 小时开发投入能在后续 5 关迭代中节省数十小时。

**守方视角**：攻方从工具开发者角度补了 UI 设计师不会主动思考的维度（Debug MenuItem、SO 参数化、代码生成约定、Design Token 文档）。守方正确拒绝了 2 个 V1 ROI 不划算的提案，但接受了文档层替代方案。**"先做游戏，再做工具"原则得到维护——但 P0 工具（~1h）必须在实施前就位。**

> **PK 状态**：✅ 已完成  
> **结束时间**：2026-05-03
