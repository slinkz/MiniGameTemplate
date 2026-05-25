# S5.7 出战准备面板 — 人工验收步骤

> **日期**：2026-05-25  
> **前置**：Unity Editor 已打开 UnityProj，Boot 场景已配置  
> **预计耗时**：10~15 分钟

---

## 1. FairyGUI 编辑器验收（~3 分钟）

### 步骤

1. 打开 FairyGUI 编辑器
2. 打开 SG_Sortie 包（路径：`MiniGameTemplate/UIProject/assets/SG_Sortie/`）
3. 双击 `SortieBottomSheet` 组件

### 验证项

| # | 检查点 | 预期 | PASS? |
|---|--------|------|-------|
| F1 | `btn_sortie` 正常显示 | 蓝色圆角矩形按钮，文字"出 击"居中 | ☐ |
| F2 | `list_skills` 可见 | 空列表区域可见，defaultItem 引用正确 | ☐ |
| F3 | `list_passives` 可见 | 同上 | ☐ |
| F4 | 点击 `btn_sortie` | 按下态(深蓝)→松开回弹(亮蓝) | ☐ |

### 操作：Publish 包

确认无误后，**Publish** SG_Sortie 包：
- 导出路径：`MiniGameTemplate/UnityProj/Assets/_Game/FairyGUI_Export/`
- 确认生成 `SG_Sortie_fui.bytes` 已更新

---

## 2. Unity PlayMode 验收（~7 分钟）

### 前置准备

- 确认 `Assets/_Game/Resources/ShooterGame/` 下有 `SkillUnlockTable.asset` 和 `PassiveUnlockTable.asset`
- 确认 Boot 场景中 `GameBootstrapper` 正常（Inspector 无黄色警告）

### 步骤

1. 打开 Boot 场景，Play
2. 等待初始化完成，进入主界面
3. 点击"开始游戏"→进入选关界面
4. 点击第 1 关（已解锁）

### 验证项

| # | 检查点 | 操作 | 预期 | PASS? |
|---|--------|------|------|-------|
| P1 | 面板弹出 | 点击关卡 | Bottom Sheet 从底部弹出，半透明遮罩覆盖 | ☐ |
| P2 | 标题正确 | 观察 | 显示"第 1 关" | ☐ |
| P3 | 技能列表 | 观察 | 显示当前已解锁技能卡片，全部默认选中(蓝色边框) | ☐ |
| P4 | 被动列表 | 观察 | 显示已解锁被动卡片，全部默认选中 | ☐ |
| P5 | 出击按钮 | 点击 | 关闭面板→跳转进入战斗场景 | ☐ |
| P6 | 遮罩关闭 | 再次弹出→点遮罩 | 面板关闭，回到选关界面 | ☐ |
| P7 | Console | 检查 | 无 Error，有 `[SortieBottomSheet] 出击!` Log | ☐ |

### 常见问题排查

| 现象 | 可能原因 | 解决 |
|------|----------|------|
| 面板空白无内容 | FUI bytes 未更新（Publish 后 Unity Refresh） | 重新 Publish + Ctrl+R |
| 技能列表为空 | UnlockTable SO 的 Entry 数组为空 | 检查 `SkillUnlockTable.asset` Inspector |
| 点出击后无响应 | Node_Battle SO 引用丢失 | 检查 `SG_FlowNodes.cs` 中的 Resources 路径 |
| Console 报 "UnlockManager is null" | Boot 初始化时序 | 检查 `SG_Boot.InitProgress()` 调用位置 |

---

## 3. 验收结果汇总

全部 PASS → 标记 S5.7 验收通过，推进到下一任务。

任何 FAIL → 在此文件下方记录问题描述，等待修复后重测。

---

_生成时间：2026-05-25 11:20_
