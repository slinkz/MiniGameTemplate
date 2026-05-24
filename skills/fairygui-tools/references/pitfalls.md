# 踩坑经验（实战积累）

> 归档自 SKILL.md，按需加载。

## 案例 1：星星 UI 在微信小游戏 WebGL 中空白（2026-05-24）

**场景**：选关界面和胜利面板用 `<text>` 显示 `★★★` / `★☆☆` 表示星级。

**现象**：云端数据确认 3 星通关，但模拟器中星星区域完全空白。

**根因**：
1. Unicode 特殊字符 `★☆` 在微信小游戏 WebGL Canvas 离屏文本渲染中，因字体缺字显示为空白
2. 这不是代码逻辑问题，而是渲染层面的平台限制

**解决方案**：
- 将 `<text name="text_star" text="★★★"/>` 替换为 `<component name="star_group">`
- `StarDisplay` 组件内用 3 对 GGraph eclipse（金色亮 + 灰色暗）叠放
- 通过 controller `stars`（pages 0-3）+ `gearDisplay` 控制不同星级下的显隐
- C# 代码通过 `star_group.stars.selectedIndex` 设置星级

**二次踩坑**：
- GGraph **不支持 gearColor**（只有 image/text/richtext/loader 支持），最初尝试用 gearColor 切换颜色失败
- FairyGUI 导出后 Extension 类属性名从 `text_star` → `star_group`，但手写的 **Logic.cs 没有同步更新**，导致编译报错 CS1061

**教训**：
- 非描述性图形化 UI → 优先 GGraph，禁止 Unicode 特殊字符
- GGraph 变色 → 双层叠放 + gearDisplay，不要尝试 gearColor
- FairyGUI XML 改名后 → 必须检查 Logic.cs 中所有旧属性引用
