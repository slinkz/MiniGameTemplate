---
system: architecture
scope: tdd07-acceptance
last_verified: 2026-05-26
related_code: Assets/_Framework/BattleLifecycle/*.cs, Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs
---

# TDD-07 战斗退场生命周期 — 验收指南

> **版本**：v1.0  
> **日期**：2026-05-26  
> **TDD 来源**：SHOOTER_GAME/V2_TDD/SG_V2_TDD_07_LIFECYCLE.md

---

## 已通过的自动验收（Agent 已完成）

| 验收项 | 结果 | 说明 |
|--------|------|------|
| 编译 0 错误 0 警告 | ✅ | Unity MCP 确认 |
| IBattleCleanup 接口存在 | ✅ | MiniGameFramework.Runtime |
| BattleLifecycleEvent SO 类型存在 | ✅ | MiniGameFramework.Runtime |
| SG_OnBattleEnd.asset 资产已创建 | ✅ | Events 目录下 |
| DanmakuSystem : IBattleCleanup | ✅ | CleanupOrder=0 |
| EntitySystemBootstrap : IBattleCleanup | ✅ | CleanupOrder=100 |
| BattleHUDController : IBattleCleanup | ✅ | CleanupOrder=20 |
| CameraShaker : IBattleCleanup | ✅ | CleanupOrder=50 |
| Battle 场景 5 组件 SO 引用已赋值 | ✅ | MCP 自动赋值 |
| BattleCleanupValidator 0 遗漏 | ✅ | 类型+实例级验证 |

---

## 需要天命人验收的 PlayMode 测试（4 条退场路径）

### 准备工作

1. 打开 Unity 编辑器 → 打开 Boot 场景
2. 进入 Play Mode
3. 从主菜单进入关卡选择 → 选择一个已解锁关卡 → 进入战斗

### 测试 1：Victory 路径（通关退场）

**操作步骤**：
1. 正常游戏直到通关（杀完所有波次）
2. 胜利弹窗出现后，点击确认返回

**验证标准**：
- [ ] 退场后无飘字残留（屏幕上无漂浮数字/文字）
- [ ] 退场后无弹幕残留（无子弹/激光/喷雾残影）
- [ ] 相机位置正常（无震动残留偏移）
- [ ] 返回主菜单/关卡选择后 UI 正常

### 测试 2：Defeat 路径（基地被毁退场）

**操作步骤**：
1. 进入战斗后，故意不移动让敌机冲到底线
2. 基地 HP 归零，失败弹窗出现
3. 点击退出返回

**验证标准**：
- [ ] 同测试 1 的四项检查

### 测试 3：PauseQuit 路径（暂停退出）

**操作步骤**：
1. 进入战斗后，点击暂停按钮
2. 在暂停菜单中点击"退出"

**验证标准**：
- [ ] 同测试 1 的四项检查

### 测试 4：Retry 路径（重试）

**操作步骤**：
1. 进入战斗，等待几秒让敌机出现并有弹幕产生
2. 触发失败 → 点击"重试"（或暂停后点击重试）
3. 确认战场完全重置后能正常重新开始

**验证标准**：
- [ ] 重试后无旧弹幕残留
- [ ] 重试后无旧飘字残留
- [ ] 重试后波次计数重置为 1
- [ ] 重试后敌机正常刷新（不会翻倍）
- [ ] 重试后相机位置正常

### 测试 5：连续退场（快速压测）

**操作步骤**：
1. 进入战斗 → 暂停退出 → 立即重新进入 → 暂停退出
2. 重复 3 次以上

**验证标准**：
- [ ] 无累积残留（每次进入都是干净状态）
- [ ] 无 Console 错误/异常

---

## 快速检查方法

如果不想完整走完所有测试，至少做以下最小验收：

1. **进入战斗 → 暂停退出 → 检查无残留**（覆盖 PauseQuit 路径）
2. **再次进入 → 等几秒 → 暂停退出**（覆盖连续进出）
3. **Console 无红色错误**

---

_创建于 2026-05-26 | TDD-07 验收指南_
