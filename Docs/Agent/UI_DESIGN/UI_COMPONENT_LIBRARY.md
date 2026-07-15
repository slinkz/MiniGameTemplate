---
system: role-agent
scope: ui-component-library
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SG_UI_DESIGN.md, skills/fairygui-tools/SKILL.md
---

# UI Component Library

> 定位：FairyGUI 组件规格。新增组件前先查本表。

| 组件 | 包 | 状态 | 必备状态/属性 |
|------|----|------|---------------|
| CommonButton | Common | active | up/down/disabled，title，icon loader |
| CommonProgressBar | Common | active | bg、bar、title |
| LevelNode | SG_LevelSelect | active | locked/available/cleared Controller |
| HPBar | SG_Battle | active | bg、bar、damageDelay、percent |
| Joystick | SG_Battle | active | base、stick、touchArea |
| SkillSlot | SG_Battle | active | icon、cdMask、level、locked |
| PassiveSlot | SG_Battle | active | icon、activeBar、cdBar |
| PickupNotification | SG_Battle | active | icon、title、transition |
| PauseButton | SG_Battle | active | icon button |
| PopupButton | SG_Popup | active | primary/secondary variant |
| StarDisplay | SG_LevelSelect/Popup | active | 0-3 stars without Unicode glyph dependency |

## 新增组件流程

1. 判断是否能扩展现有 Common 组件。
2. 定义状态矩阵和数据字段。
3. 在 FairyGUI 中创建独立组件文件。
4. `package.xml` resources 注册组件。
5. 开启代码导出，生成强类型绑定。
6. 业务逻辑写 `.Logic.cs`。
7. 更新本文件和 `SCREEN_CARDS.md`。

## 组件验收

- 三态资源完整。
- 所有引用组件存在。
- `defaultItem` 使用 `ui://包ID资源ID`。
- 导出类命名、包名、publish name 一致。
- Unity 中 Binder 已注册，面板能加载。

