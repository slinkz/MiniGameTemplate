---
system: role-agent
scope: fairygui-handoff-checklist
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/CONTEXT_PACKS/FairyGUI_UI.md, skills/fairygui-tools/SKILL.md
---

# FairyGUI Handoff Checklist

> 定位：UI 设计到 FairyGUI 工程的交接清单。

## 设计交付

- [ ] Screen/Component 名称。
- [ ] 包名、publish name、导出前缀、代码 packageName 一致。
- [ ] 状态矩阵和 Controller page。
- [ ] 数据字段和 SO 绑定。
- [ ] 交互事件。
- [ ] Transition 名称和触发时机。
- [ ] 资源尺寸和替换策略。

## XML / 工程约束

- [ ] 白模占位使用 `<graph>`，不使用 `<image>`。
- [ ] 所有引用组件在 `package.xml` resources 中声明。
- [ ] 扩展组件是独立 XML 文件，父组件用 `<component src>` 引用。
- [ ] `<controller>` 在 `<displayList>` 前。
- [ ] `<transition>` 在 `<displayList>` 后。
- [ ] 禁止 Unicode 图形字符当图标。

## Unity 交接

- [ ] FairyGUI 已发布到 Unity。
- [ ] 生成代码纳入 Git。
- [ ] 自动生成文件未手改。
- [ ] `GameStartupFlow` 或对应启动流程注册 Binder。
- [ ] `.Logic.cs` 已同步新属性名。
- [ ] Open/Refresh/Close 不重复绑定事件。

## 验收

- [ ] 编辑器中包可加载。
- [ ] 目标界面能打开、刷新、关闭。
- [ ] AppFlow 返回链路正确。
- [ ] 真机点击区域正确。

