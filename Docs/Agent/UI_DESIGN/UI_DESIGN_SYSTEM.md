---
system: role-agent
scope: ui-design-system
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SG_UI_DESIGN.md, Docs/Agent/ASSET_PIPELINE/UI_ICON_PIPELINE.md
---

# UI Design System

> 定位：UI token 和基础规则。修改 FairyGUI 前先对齐这里。

## 基准

| 项 | 值 |
|----|----|
| 设计分辨率 | 750 x 1334 pt |
| 方向 | 竖屏 |
| 适配 | 宽度固定，高度自适应，遵守安全区 |
| 最小触摸热区 | 44 x 44 pt |
| UI 框架 | FairyGUI |

## 颜色 Token

| Token | 值 | 用途 |
|-------|----|------|
| Brand_Primary | `#4FC3F7` | 主按钮、重点高亮 |
| BG_Dark | `#1A1A2E` | Loading/深色背景 |
| Panel_Dark | `#2D2D44` | 弹窗/面板 |
| Btn_Secondary | `#3A3A4A` | 次按钮 |
| Danger | `#EF5350` | 失败、警告、退出 |
| Success | `#2ECC71` | 确认、回血、正反馈 |
| Text_White | `#FFFFFF` | 主文字 |
| Text_LightGray | `#C7CCD8` | 次文字 |

## 字号 Token

| Token | 大小 | 用途 |
|-------|------|------|
| Title_Large | 36pt | 游戏名、胜利标题 |
| Title_Medium | 24pt | 面板标题、加载 |
| Body | 20pt | 数据、波次 |
| Caption | 18pt | 血量、说明 |
| Button_Primary | 24pt | 主按钮 |
| Button_Secondary | 20pt | 次按钮 |

## 组件规则

- 主按钮宽 280pt，高 56pt。
- 图标按钮热区 44pt，图标 28pt。
- 血条高 12pt，颜色随血量变化。
- 弹窗遮罩黑色 alpha 0.6。
- 所有 HUD 组件避开安全区和摇杆触摸路径。

## 禁止事项

- 不用 Unicode 星星、锁、箭头等当图形 UI。
- 不让 HUD 遮挡核心战斗区域。
- 不新建一次性按钮样式，优先复用 Common。
- 不让文字溢出按钮或卡片。

