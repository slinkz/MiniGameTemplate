# FairyGUI 微信小游戏点击无反应 — 修复报告

> **日期**：2026-04-29  
> **状态**：✅ 已修复  
> **影响范围**：FairyGUI + 微信小游戏（WebGL）环境下，所有使用 Controller 状态切换的 GButton 点击事件  
> **修改文件**：`Assets/FairyGUI/Scripts/Core/Stage.cs`

---

## 1. 问题描述

在微信小游戏（WebGL）环境下，FairyGUI 的 GButton 点击完全没有反应。`onClick` 事件不触发，用户无法与 UI 交互。

**影响条件**：
- 平台：WebGL（微信小游戏）
- GButton 使用了 Controller 控制按钮外观状态（OVER / DOWN / UP 等）
- FairyGUI 的 `touchScreen` 自动检测生效（`Input.touchCount > 0`）

**表现**：点击按钮无任何响应，无 onClick 回调。

---

## 2. 根因分析

### 2.1 核心时序问题

`Stage.HandleTouchEvents()` 中，`HandleRollOver()` 在 `touch.Begin()` **之前**执行。当手指首次触碰 GButton 时：

```
GetHitTarget(touch)
  → HitTest → 命中 Shape（GButton 内部的显示对象）
  → touch.target = Shape ✅（此时 Shape.stage != null）

HandleTouchEvents()
  → touch.lastRollOver != touch.target → HandleRollOver(touch)
      → 冒泡到 GButton → 派发 onRollOver 事件
      → GButton.__rollover() → SetState(OVER)
      → Controller 切页 → 旧 Shape 被替换/移除
      → touch.target（旧 Shape）→ parent=null, stage=null ❌

  → touch.Begin()
      → 记录 downTargets → 旧 Shape 无 parent
      → downTargets 冒泡链只有 [Shape, Shape]（缺少 GButton 等祖先）

  → ... touch.End() → touch.ClickTest()
      → L1: downTargets[0].stage == null → miss
      → L2: current target 冒泡匹配 downTargets → miss
      → L3: 扫描 downTargets 找 stage != null → 全部 null → miss
      → 返回 null → onClick 不触发
```

### 2.2 根因一句话总结

**`HandleRollOver` 在 `Begin()` 之前执行，rollOver 事件触发 GButton 状态切换，导致 `touch.target` 指向的 Shape 脱离显示树，`Begin()` 记录了不完整的 downTargets 冒泡链，最终 `ClickTest()` 三级 fallback 全部失败。**

---

## 3. 修复方案

### 3.1 方案选型

| 方案 | 描述 | 优缺点 |
|------|------|---------|
| A. 移动 HandleRollOver 到 Begin 之后 | 调整调用顺序 | ❌ 会影响 Move 阶段的 rollOver 语义 |
| B. 跳过 Began 帧的 HandleRollOver | 条件跳过 | ❌ Began 帧不触发 rollOver 不符合预期 |
| **C. Begin 前检测 target 脱离并重新 HitTest** | 最小侵入 | ✅ 不改变原有时序，仅补救异常情况 |

**选择方案 C**：影响范围最小，仅在 target 确实脱离时触发补救逻辑。

### 3.2 修复代码

文件：`Assets/FairyGUI/Scripts/Core/Stage.cs`  
位置：`HandleTouchEvents()` 方法的 `TouchPhase.Began` 分支

```csharp
if (uTouch.phase == UnityEngine.TouchPhase.Began)
{
    if (!touch.began)
    {
        // [FIX] HandleRollOver 可能触发 GButton.__rollover → SetState → Controller 切页
        // 导致 touch.target（原 Shape）被移出显示树。此处检测并重新 HitTest。
        if (touch.target == null || touch.target.stage == null)
        {
            DisplayObject newTarget = HitTest(pos, true);
            touch.target = newTarget ?? this;
        }

        _touchCount++;
        touch.Begin();
        touch.button = 0;
        SetFocus(touch.target);
        // ...
    }
}
```

### 3.3 修复原理

1. `HandleRollOver` 执行后，如果 GButton 切状态导致旧 Shape 脱离，`touch.target.stage` 变为 `null`
2. 检测到 `stage == null` 后，重新执行一次 `HitTest`，获取当前帧实际在屏幕上的新 Shape
3. 新 Shape 有完整的 parent 链（Shape → Container → GButton → ...）
4. `Begin()` 记录完整的 downTargets 冒泡链
5. 即使后续 `onTouchBegin` 再次切状态（DOWN），`downTargets` 中已有 GButton 等祖先节点
6. `ClickTest()` 的 L3 fallback 能在 downTargets 中找到仍在 stage 上的 Container 或 GButton
7. `BubbleEvent("onClick")` 从 Container/GButton 冒泡 → `onClick` 正常触发

---

## 4. 关联修复（之前已实施）

本次调试过程中，还修复/确认了以下问题：

| 修复项 | 描述 | 状态 |
|--------|------|------|
| 首触 touchId 未分配 | 自动检测移到 GetHitTarget 之前 | ✅ 已修复 |
| ClickTest L2/L3 fallback | downTargets[0] 脱离时扫描祖先 | ✅ 已修复 |
| 强制 touchScreen=true | 删除 GameBootstrapper 中的强制赋值 | ✅ 已修复 |

---

## 5. 测试验证

### 验证环境
- 微信开发者工具（模拟器）
- 真机（微信小游戏）

### 验证步骤
1. 打包 WebGL → 微信小游戏
2. 在微信开发者工具中运行
3. 点击任意使用 Controller 状态切换的 GButton
4. 确认 onClick 事件正常触发

### 验证结果
✅ 点击响应正常

---

## 6. 影响评估

- **性能影响**：仅在 `Began` 帧且 `target.stage == null` 时才多一次 HitTest，正常情况（target 在 stage 上）零开销
- **兼容性**：不影响 PC/移动端原有行为（HandleRollOver 不触发 GButton 状态切换时 target 不会脱离）
- **回归风险**：极低，修复逻辑完全是条件性的补救，不改变正常流程

---

## 7. 经验教训

1. **FairyGUI 事件时序敏感**：`HandleRollOver` / `Begin` / `BubbleEvent` 的顺序决定了 downTargets 的完整性
2. **GButton + Controller 切页是高风险操作**：任何在 `Begin()` 前派发的事件，如果触发了 Controller 状态切换，都可能导致显示树变化
3. **调试 WebGL 输入问题的关键**：对比 `GetHitTarget` 时和 `Begin` 时的 `target.stage` 状态，能快速定位"中间被偷换"的问题
4. **诊断日志必须加在正确的预编译分支**：`FAIRYGUI_INPUT_SYSTEM` 宏未定义时，`#if` 分支内的日志不会编译

---

## 8. 文件变更清单

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `Assets/FairyGUI/Scripts/Core/Stage.cs` | 修改 | HandleTouchEvents Began 分支加 re-HitTest 保护 |
| `Assets/FairyGUI/Scripts/Core/Stage.cs` | 清理 | 移除所有 `[FGUI-Diag]` 诊断日志 |
| `Assets/FairyGUI/Scripts/Core/StageEngine.cs` | 清理 | 移除帧计数诊断日志和 `_frameCount` 字段 |
