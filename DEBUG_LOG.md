# FairyGUI 微信小游戏点击问题调试日志

> **⚠️ AI 必读**：每次开始新对话处理此问题时，**先读这个文件**，不要凭记忆。
> 
> 路径：`MiniGameTemplate/DEBUG_LOG.md`
> 最后更新：2026-04-29 19:13

---

## 📌 当前状态：✅ 根因已定位 → 修复已实施，待验证

### 2026-04-29 19:13 第四轮日志 — 根因定位完成 🎯

```
GetHitTarget(touch): finger=0, phase=Began, hitResult=FairyGUI.Shape(parent=FairyGUI.Container, stage=True), touch.began=False
Begin: target=FairyGUI.Shape(parent=, stage=False)
Begin: downTargets.Count=2
ClickTest: ALL LEVELS FAILED — return null
```

**根因完整链路：**
1. `GetHitTarget(touch)` → HitTest → 命中 Shape（**parent=Container, stage=True** ✅）→ `touch.target = Shape`
2. `HandleTouchEvents()` → 第 1514 行 `touch.lastRollOver != touch.target` → **`HandleRollOver(touch)`**
3. HandleRollOver → 冒泡到 GButton → 派发 **`onRollOver`** 事件
4. GButton.**`__rollover()`** → **`SetState(OVER)`** → **Controller 切页** → **Shape 被替换/移除**
5. `touch.target`（还指向旧 Shape）→ parent=null, stage=null
6. **`touch.Begin()`** → 记录 downTargets → 旧 Shape 无 parent → downTargets 只有 [Shape, Shape]
7. `ClickTest()` → downTargets 缺少祖先 → 三级 fallback 全失败 → onClick 不触发

**修复**：在 Began 分支的 `Begin()` 之前检查 `touch.target.stage == null`，如果脱离则重新 HitTest 获取新 target

### 历史日志（已解决，留作参考）

<details>
<summary>第二轮 — ClickTest 诊断（18:10）</summary>

- downTargets.Count=2，两个 Shape，两个 stage=False
- GButton 等祖先完全缺失
</details>

<details>
<summary>第三轮 — Begin 冒泡链诊断（18:18）</summary>

- Begin 时 Shape.parent=null, stage=False
- 确认 Shape 在 Begin 前就脱离显示树
</details>

---

## 🔑 关键事实（不要忘！）

### 1. `FAIRYGUI_INPUT_SYSTEM` 宏 **没有定义**
- WebGL 平台 Scripting Define Symbols 只有 `WEIXINMINIGAME`
- **所有 `#if FAIRYGUI_INPUT_SYSTEM` 包裹的代码都不编译**
- `_useLegacyInput`、`HandleMouseEventsLegacy`、`HandleTouchEventsLegacy` 等 **全部不存在于运行时**
- 实际编译路径是 `#else` 分支，使用旧版 `Input.xxx` API

### 2. 输入路径（实际运行的）
```
Stage() 构造函数
  → touchScreen = false  (因为 WebGLPlayer)
  → _touchSupportDetected = false

HandleEvents() 每帧调用
  → 自动检测：if (Input.touchCount > 0) → touchScreen = true
  → GetHitTarget()
    → touchScreen=false → mouse 分支：Input.mousePosition + HitTest
    → touchScreen=true  → touch 分支：遍历 Input.GetTouch(i) + HitTest
  → touchScreen=false → HandleMouseEvents()
    → Input.GetMouseButtonDown(0) → Begin → onTouchBegin
    → Input.GetMouseButtonUp(0) → End → ClickTest → onClick
  → touchScreen=true → HandleTouchEvents()
    → Input.GetTouch(i) → TouchPhase.Began → onTouchBegin
    → TouchPhase.Ended → ClickTest → onClick
```

### 3. 已修复的问题（理论上）
- ✅ **首触 bug**：自动检测已移到 `GetHitTarget()` 之前（防止 touchId 未分配）
- ✅ **ClickTest + GButton 状态切换**：增加了第二级 fallback

### 4. 当前未解决的问题
- ❌ **点击完全没反应**——既不走 mouse 路径也不走 touch 路径？还是走了但事件被丢弃？
- 需要日志确认

---

## 📍 当前诊断日志位置

文件：`Assets/FairyGUI/Scripts/Core/Stage.cs`

所有日志 Tag 前缀：`[FGUI-Diag]`

| # | 位置 | 分支 | Tag | 触发条件 |
|---|------|------|-----|----------|
| 1 | 构造函数 ~line 249 | `#else`(无宏) | `Stage() init` | 启动一次 |
| 2 | HandleEvents 自动检测 ~line 1130 | `#else`(无宏) | `AutoDetect` | touchCount>0 时一次 |
| 3 | GetHitTarget mouse分支 ~line 988 | `#else`(无宏) | `GetHitTarget(mouse)` | mouseDown 帧 |
| 4 | HandleMouseEvents ~line 1308 | `#else`(无宏) | `Mouse: ButtonDown` | mouseDown 帧 |
| 5 | HandleMouseEvents ~line 1330 | `#else`(无宏) | `Mouse: ButtonUp/ClickTest` | mouseUp 帧 |
| 6 | HandleTouchEvents ~line 1488 | `#else`(无宏) | `Touch: NO matching` | touch 匹配失败 |
| 7 | HandleTouchEvents ~line 1510 | `#else`(无宏) | `Touch: Began` | touchBegan |
| 8 | HandleTouchEvents ~line 1530 | `#else`(无宏) | `Touch: Ended/ClickTest` | touchEnded |
| 9 | StageEngine.LateUpdate | 无条件 | `StageEngine.LateUpdate frame=N` | 前3帧 + 每300帧 |

### 日志解读指南

**场景 A：如果启动时看到 init 日志**
- Stage 初始化正常
- 继续看点击时输出什么

**场景 B：如果点击时看到 Mouse 日志**
- 走 mouse 路径，看 GetHitTarget 的 target 是什么
- target = Stage → HitTest 没命中 UI → 问题在 UI 层级/碰撞
- target = 某个 Shape/GObject → HitTest 正常，看 ClickTest

**场景 C：如果点击时看到 Touch 日志**
- 走 touch 路径，看 finger 匹配和 ClickTest

**场景 D：如果点击时完全没有任何日志（但 init 有）**
- HandleEvents() 在跑（因为 StageEngine 帧日志在输出）
- 说明 Input.GetMouseButtonDown(0) 和 Input.touchCount 全部返回 false
- 微信 WebGL 下输入事件没有到达 Unity Input 层
- 需要检查：weapp-adapter 是否正常、Canvas 是否遮挡、Input 是否被全局拦截

**场景 D2：如果连 StageEngine 帧日志都没有**
- FairyGUI 引擎根本没在跑
- 可能 Stage 没初始化、StageEngine 没挂载、或者 Debug.Log 被禁用

**场景 E：如果启动时连 init 日志都没有**
- Stage 没被创建，或者 Debug.Log 在微信环境被禁用
- 检查微信开发者工具的 Console 面板是否过滤了 Log 级别

---

## 🔧 待验证清单

- [ ] 重新打包部署后，微信开发者工具 Console 里能否看到 `[FGUI-Diag] Stage() init` 日志
- [ ] 点击按钮后，Console 里有没有任何 `[FGUI-Diag]` 日志
- [ ] 如果完全没日志，在 `Stage.LateUpdate` 入口加一条计数日志确认帧循环在跑
- [ ] 确认微信开发者工具 Console 没有过滤 Log 级别（只显示 Warning/Error）

---

## 📜 修改历史

### 2026-04-29 18:18 — 在 GetHitTarget(touch) 加诊断
- 第三轮日志确认 Begin 时 Shape 已经脱离（parent=null, stage=False）
- 在 GetHitTarget 的 `#else` touch 分支加了日志：HitTest 结果的 parent、stage、touch.began
- 文件：`Assets/FairyGUI/Scripts/Core/Stage.cs` GetHitTarget() ~line 962
- 目的：确认 HitTest 命中时 Shape 是否真的在显示树上、以及 touch.began 此时是什么

### 2026-04-29 18:10 — 在 Begin() 加冒泡链诊断
- 第二轮日志确认 downTargets 只有 2 个 Shape，缺少 GButton 等祖先
- 在 Begin() 中加了完整冒泡链打印：`target → parent → grandparent → ...`
- 打印 Begin 时 downTargets 的最终数量
- 目的：确认 Shape 在 Begin() 时的 parent 是什么（null 还是 GButton？）
- 如果 parent 是 null → 说明 Shape 在 Begin 前就已脱离显示树（时序问题更严重）
- 如果 parent 有值 → 说明 Begin() 的冒泡链逻辑有 bug（不应该只记录 2 个）
- 文件：`Assets/FairyGUI/Scripts/Core/Stage.cs` Begin() 方法

### 2026-04-29 17:48 — 在 ClickTest 内部加详细诊断
- 首次日志确认问题在 ClickTest 返回空
- 在 ClickTest 的每个级别（L1/L2/L3）加了诊断日志
- 打印 downTargets 完整列表、每个元素的 stage 状态、当前 target
- 下次日志能精确看到哪级 fallback 走了、为什么都 miss

### 2026-04-29 17:35 — 加 StageEngine 帧计数日志
- 在 `StageEngine.LateUpdate` 入口加帧计数日志（前 3 帧 + 每 300 帧）
- 作为保底检测：如果连这条都没有，说明 FairyGUI 引擎根本没在跑
- 文件：`Assets/FairyGUI/Scripts/Core/StageEngine.cs`

### 2026-04-29 16:56 — 加诊断日志（第二次，修正分支）
- 第一次加的日志全在 `#if FAIRYGUI_INPUT_SYSTEM` 内 → 宏没定义 → 不编译 → 白加了
- 第二次改在 `#else` 分支加日志，覆盖实际编译路径
- **教训**：改代码前先确认预处理宏状态，不要假设宏已定义

### 2026-04-29 早些时候 — ClickTest 第二级 fallback
- 修复了 GButton 状态切换导致 downTarget 脱离 stage 的问题
- 但点击仍然没反应 → 问题可能更靠前（事件根本没到 ClickTest）

### 2026-04-29 早些时候 — 自动检测前移
- 将 HandleEvents 中的 touchCount 自动检测从 GetHitTarget 之后移到之前
- 修复了首触 touchId 未分配的问题
- 但点击仍然没反应 → 问题可能不在 touch 路径

### 2026-04-29 早些时候 — 删除强制 touchScreen = true
- 从 GameBootstrapper 中删除了 `Stage.touchScreen = true`
- 理论修复了首次点击丢失
- 但点击仍然没反应

---

## 🚫 已排除的可能性

- ~~强制 touchScreen = true~~ → 已删除
- ~~首触 touchId 未分配~~ → 自动检测已前移
- ~~ClickTest downTarget 脱离 stage~~ → 已加 fallback
- ~~FAIRYGUI_INPUT_SYSTEM 宏下的 Input System 兼容问题~~ → 宏没开，不走那条路

---

## 🎯 下一步

1. **天命人重新打包部署**
2. 点击按钮，看是否出现 `[FGUI-Diag] Touch Began: target lost stage, re-HitTest:` 日志
3. 如果 re-HitTest 日志出现且 onClick 生效 → **修复成功** 🎉
4. 如果 re-HitTest 后 ClickTest 仍然失败 → 看 downTargets.Count 和内容
5. **修复成功后**：清理所有诊断日志，保留修复代码
