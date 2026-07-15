---
system: role-agent
scope: font-text-pipeline
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/UI_DESIGN/UI_COPY_GUIDE.md, skills/fairygui-tools/SKILL.md
---

# Font Text Pipeline

> 定位：字体、文本和缺字风险管理。

## 规则

- UI 关键图形不依赖 Unicode 字符。
- 中文文案优先使用项目确认字体或系统字体。
- 按钮文案要按最长中文词检查宽度。
- WebGL/微信上必须注意字体 fallback 和缺字。

## 检查

| 项 | 方法 |
|----|------|
| 缺字 | 真机或 WebGL 构建查看 |
| 溢出 | 小屏宽度截图 |
| 对比度 | 深浅背景都检查 |
| 语言 | 当前以中文为主，英文标题如 VICTORY/DEFEAT 可保留 |

## 禁止

- 不用 `★`、`🔒`、`▶` 等作为唯一信息来源。
- 不在按钮中塞长句。
- 不让 HUD 文本覆盖战斗对象。

