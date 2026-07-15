---
system: role-agent
scope: ui-icon-pipeline
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/UI_DESIGN/UI_COMPONENT_LIBRARY.md, skills/fairygui-tools/SKILL.md
---

# UI Icon Pipeline

> 定位：技能图标、Buff 图标、按钮图标和状态资源生产。

## 规格

| 类型 | 尺寸 | 状态 |
|------|------|------|
| 技能图标 | 64 x 64 | normal/locked/cooldown mask |
| Buff 图标 | 32 x 32 | normal/expiring |
| 按钮图标 | 28 x 28 | up/down/disabled |
| 关卡状态 | 48 x 48 | cleared/available/locked |

## 流程

1. 从 UI 组件库确认组件和状态。
2. 生成图标 PNG，透明底。
3. 导入 FairyGUI 包。
4. 替换白模 GGraph 或 loader 占位。
5. 发布到 Unity。
6. 检查生成代码和 Logic 引用。
7. 截图验收普通/禁用/锁定/CD 状态。

## 禁止

- 不用字体符号代替关键图标。
- 不只交付 normal 态，必须交付状态资源或说明由代码 tint/mask 生成。

