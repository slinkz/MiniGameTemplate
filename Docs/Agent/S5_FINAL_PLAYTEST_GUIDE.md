---
system: shootergame-v2
scope: sprint5-playtest
last_verified: 2026-05-25
---

# Sprint 5 最终 PlayTest 验收指南

> **前置**：所有自动化验证已通过（编译 0E/0W + SO 一致性 + ID 无冲突 + 关卡配置 + Resources.Load）。
> 本文档列出**必须通过人工 PlayMode 验证**的项目。

---

## 0. 准备工作

1. Unity 打开 `Boot` 场景（`Assets/Scenes/Boot.unity`）
2. 确保 **Game 窗口分辨率** 设为 750×1334（竖屏）或自定义竖屏比例
3. 点击 ▶️ 进入 PlayMode

---

## 1. 完整流程走通（G14）

**路径**：Boot → Loading → MainMenu → 点"射击游戏" → LevelSelect → 点关卡 → SortieBottomSheet → 选技能+被动 → 出击 → Battle → 通关/失败

**每关操作**：
1. 选关 → 出战面板弹出 → 选择至少 1 个技能 → 点"出击"
2. 等待 Intro 动画结束 → Playing 状态
3. 左右移动闪避，等待自动攻击清完全部波次
4. 通关 → 结算面板 → 确认 → 返回选关 → 选下一关

**通关标准**：5 关全部可进入战斗、至少 3 关通关（不要求全 3 星）

---

## 2. 战斗 HUD 验收（G5~G8）

在战斗 Playing 状态中观察：

| # | 检查项 | 操作 | 预期 |
|---|--------|------|------|
| G5 | 技能 CD | 技能释放后 | CD 环形进度 → 灰色 → 恢复高亮 |
| G6 | 被动栏 | 装备被动进入战斗 | 被动图标显示+激活时高亮 |
| G7 | 波次动效 | 新波次开始时 | "WAVE X" 文字弹跳动效；最后一波 "FINAL WAVE" |
| G8 | 拾取通知 | 拾取道具时 | 屏幕上方弹出通知条（最多同时 2 条） |

---

## 3. 暂停/结算 UI（G9~G13）

| # | 检查项 | 操作 | 预期 |
|---|--------|------|------|
| G9 | 暂停菜单 | 战斗中点暂停 | 显示当前 Build + Buff 列表 + 伤害统计 |
| G10 | 胜利面板 | 通关后 | 显示星级评价 + 技能贡献条形图 + 下次解锁预告 |
| G11 | 失败面板 | 故意死亡 | 显示"火力不足"提示 + 重试/退出按钮 |
| G12 | 解锁弹窗 | 第 2/4 关通关后 | 按序弹出解锁弹窗（每个一个） |
| G13 | 受伤反馈 | 被敌弹命中 | 屏幕边缘红闪 + 轻微屏幕震动 |

---

## 4. BuffConfigEditor 人工复核（G15）

> 如果信任 MCP 自动验证结果可跳过。

1. 在 Project 中选中 `Assets/_Game/Configs/ShooterGame/Buffs/SG_Buff_Berserk.asset`
2. Inspector 中确认：
   - Tag=Positive → 显示"被动/特殊效果"区域
   - 切换 Tag→Negative → "被动/特殊效果"区域消失
   - 设置 Duration=-1 → 红色 HelpBox 出现
   - 设置 BuffId=1002（与 SG_Buff_MoveUp 冲突）→ 黄色 HelpBox 出现
3. 展开"数值效果预览"折叠区 → 显示只读数值

---

## 5. EditMode Test（G16）

```
Unity 菜单 → Window → General → Test Runner → EditMode → Run All
```

确认 `SOValidationRulesTests` 全绿。

---

## 6. 性能快查（P1~P3）

| # | 操作 | 预期 |
|---|------|------|
| P1 | 战斗中打开 Profiler → UI 模块 | HUD 更新 < 0.5ms/frame |
| P2 | Profiler Memory → GC Alloc 列 | 通知条弹出时 0 bytes（对象池） |
| P3 | 打开 SkillPreview/BuffOverview 窗口 | 不卡编辑器（< 100ms 响应） |

---

## 验收判定

- **G1~G4**：✅ 已通过 MCP 自动验证
- **G5~G16**：按上述步骤逐项确认
- **P1~P3**：性能快查
- **全部 PASS** → Sprint 5 验收通过 → 可进入真机验收阶段

---

_生成于 2026-05-25 11:50_
