# Phase 3 视觉增强 — 用户验收操作手册

> 生成日期：2026-04-13 | 角色：项目经理
> 前置条件：Phase 2 已验收通过（2026-04-12）

---

## 准备工作

1. 打开 Unity 编辑器，加载 `MiniGameTemplate/UnityProj` 工程
2. 等待编译完成

---

## C-02 — Unity 编辑器无编译错误

**操作**：看 Console 窗口（`Window → General → Console`）
**通过条件**：无红色错误条目（Warning 可忽略）

- [x] 通过

---

## C-03 — Demo 场景可运行

**操作**：打开 DanmakuDemo 场景 → 点击 Play
**通过条件**：进入播放模式无崩溃，弹丸正常飞行

- [x] 通过

---

## G-02 — DanmakuEffectsBridgeConfig 组件可配置

**操作**：

1. 在 Hierarchy 中选中挂有 `DanmakuSystem` 组件的 GameObject
2. Inspector 底部 → `Add Component` → 搜索 `DanmakuEffectsBridgeConfig` → 添加
3. 将场景中的 `SpriteSheetVFXSystem` 拖入 `Hit Vfx System` 字段
4. 将一个 `VFXTypeSO` 资产拖入 `Hit Vfx Type` 字段
5. `Hit Vfx Scale` 设为 `1`

**通过条件**：三个字段都能正常赋值，Inspector 显示无报错

- [x] 通过

---

## G-03 — 命中特效正常

**操作**：Play → 让弹丸命中碰撞目标
**通过条件**：命中时播放 VFX 特效，与 Phase 2 表现一致

- [x] 通过

---

## A-03 — BulletTypeSO 视觉动画配置

**操作**：

1. 在 Project 中选中任意 `BulletTypeSO` 资产
2. Inspector 中找到 `Use Visual Animation` 勾选框 → 勾选
3. 观察下方出现三个配置项：`Scale Over Lifetime`（曲线）、`Alpha Over Lifetime`（曲线）、`Color Over Lifetime`（渐变）

**通过条件**：勾选后三个配置项可编辑，取消勾选后隐藏或不影响运行

- [x] 通过

---

## A-04 — 缩放动画可见

**操作**：

1. 选中 BulletTypeSO → 勾选 `Use Visual Animation`
2. `Scale Over Lifetime` 曲线设为：起点 (0, 0.2)，终点 (1, 2)（小→大）
3. Play → 观察弹丸

**通过条件**：弹丸从小变大，生命周期末尾约为初始的 2 倍大小

- [x] 通过

---

## A-05 — 淡出动画可见

**操作**：

1. `Alpha Over Lifetime` 曲线设为：起点 (0, 1)，终点 (1, 0)
2. Play → 观察弹丸

**通过条件**：弹丸飞行过程中逐渐变透明，末尾接近完全消失

- [x] 通过

---

## A-06 — 颜色动画可见

**操作**：

1. `Color Over Lifetime` 渐变设为：左端白色，右端红色
2. Play → 观察弹丸

**通过条件**：弹丸从白色渐变为红色

- [x] 通过

---

## A-07 — 关闭动画无副作用

**操作**：

1. 取消勾选 `Use Visual Animation`
2. Play → 观察弹丸

**通过条件**：弹丸大小/透明度/颜色与 Phase 2 完全一致，无异常

- [x] 通过

---

## B-01 — 溶解参数 Inspector 可配

**操作**：

1. 在 Project 中找到弹丸使用的 Material（使用 `DanmakuBullet` 或 `DanmakuBulletAdditive` Shader）
2. 选中 Material，Inspector 中查看属性列表
3. 注意：`DanmakuBulletAdditive` 属于加色混合材质，Inspector 预览球的颜色观感可能比原贴图更亮或偏粉；验收以场景运行效果和参数可编辑性为准，不以预览球色感为准

**通过条件**：可见以下四个属性并可编辑：

- `Dissolve Tex`（噪声贴图槽位）

- `Dissolve Amount`（0~1 滑条）

- `Dissolve Edge Width`（浮点）

- `Dissolve Edge Color`（颜色选择器）

- [x] 通过

---

## B-02 — 溶解效果运行时可见

**操作**：

1. 给 Material 的 `Dissolve Tex` 指定一张噪声贴图（任意灰度噪声图）
2. `Dissolve Amount` 设为 `0.4`，`Dissolve Edge Width` 设为 `0.05`，`Dissolve Edge Color` 设为橙色
3. Play → 观察弹丸

**通过条件**：弹丸表面出现溶解镂空效果，边缘有橙色过渡带

- [x] 通过

---

## B-03 — 发光参数 Inspector 可配

**操作**：同 B-01 的 Material
**通过条件**：可见 `Glow Intensity`（浮点）、`Glow Color`（颜色选择器）和 `Glow Width`（浮点）

- [x] 通过

---

## B-04 — 发光效果运行时可见

**操作**：

1. `Glow Intensity` 设为 `0.5`，`Glow Color` 设为青色，`Glow Width` 设为 `0.2`
2. Play → 观察弹丸

**通过条件**：弹丸边缘出现明显的青色发光描边，主体仍保持原有贴图主色，不应整体被染成其他颜色；调大 `Glow Width` 时描边变宽，调小时描边变窄

- [x] 通过

---

## B-05 — 默认参数下无视觉变化

**操作**：

1. `Dissolve Amount` 设为 `0`，`Glow Intensity` 设为 `0`
2. Play → 观察弹丸

**通过条件**：弹丸表现与 Phase 2 完全一致

- [x] 通过

---

## D-01 — 预警线仅在 Charging 阶段显示

**操作**：发射一条激光 → 观察 Charging（蓄力）阶段
**通过条件**：Charging 时出现一条细线，进入 Firing（发射）后细线消失

- [x] 通过

---

## D-02 — 预警线闪烁

**操作**：同上，关注 Charging 阶段细线
**通过条件**：细线 alpha 有明显的脉冲闪烁效果（不是静态不动的）

- [x] 通过

---

## D-03 — 预警线颜色与激光类型一致

**操作**：对比 LaserTypeSO 的 `Core Color` 与预警线颜色
**通过条件**：颜色一致

- [x] 通过

---

## D-04 — 预警线排序低于激光本体

**操作**：观察 Charging → Firing 过渡瞬间
**通过条件**：激光体绘制在预警线之上，不被预警线遮挡

- [x] 通过

---

## E-01 — SprayTypeSO 可配 VFX

**前置说明**：当前 `DanmakuDemo` 已补喷雾触发入口：`K` = Detached Spray，`J` = Attached Spray（跟随 Boss）；Demo 默认已注册 `SprayType_BasicCone`，若无效果再检查 `TypeRegistry` 引用是否丢失

**操作**：选中任意 `SprayTypeSO` 资产 → Inspector 查看

**通过条件**：可见 `Spray VFX Type` 字段，可引用一个 `VFXTypeSO` 资产

- [x] 通过

---

## E-02 — AttachMode 下拉可选

**操作**：选中 `VFXTypeSO` 资产 → Inspector 查看 `Attach Mode` 字段
**通过条件**：下拉菜单可选 `World` / `FollowTarget`（以及 `Socket` 占位）

- [x] 通过

---

## E-03 — FollowTarget 跟随

**操作**：

1. 给 SprayTypeSO 配置一个 AttachMode=FollowTarget 的 VFXTypeSO
2. Play → 等待 Boss 自动横向往返移动 → 按 `J` 发射 Attached Spray
3. 再把同一个 VFXTypeSO 的 AttachMode 改回 `World`，重复按 `J` 对比

**通过条件**：

- `FollowTarget`：Boss 移动时，喷雾附着的 VFX 持续跟随喷雾源位置移动

- `World`：VFX 仅在生成瞬间取一次喷雾源位置，后续不再跟随 Boss 移动

- 两种模式必须能被肉眼明确区分；如果两者表现一致，说明 AttachMode 语义未真正接入运行时

- [x] 通过

---

## E-04 — 喷雾回收时 VFX 停止

**操作**：Play → 等喷雾生命周期结束或飞出边界
**通过条件**：喷雾消失时附着的 VFX 同步停止

- [x] 通过

---

## E-05 — ClearAll 停止喷雾 VFX

**操作**：Play → 有喷雾在场时调用 ClearAll（通过测试按钮或代码）
**通过条件**：所有喷雾的附着 VFX 立即停止

- [x] 通过

---

## E-06 — 目标失效冻结语义（ADR-021）

**操作**：Play → 喷雾附着源 GameObject 在播放中被销毁
**通过条件**：VFX 冻结在最后有效位置，播完后自然消失，不报错

- [x] 通过

---

## F-01 — TimeScale 联动

**操作**：

1. Play → 通过代码或 Inspector 将 DanmakuSystem 的 TimeScale 设为 `0.5`
2. 观察喷雾附着的 VFX 播放速度

**通过条件**：VFX 播放速度明显变慢（约半速）

- [x] 通过

---

## F-02 — TimeScale=0 暂停

**操作**：TimeScale 设为 `0`
**通过条件**：VFX 停在当前帧不推进

- [x] 通过

---

## H-01 — 默认弹丸回归

**操作**：所有新功能保持默认（动画关闭、溶解/发光=0）→ Play
**通过条件**：弹丸行为与 Phase 2 完全一致

- [x] 通过

---

## H-02 — 激光回归

**操作**：发射激光 → 观察完整生命周期
**通过条件**：Charging → Firing → 结束，行为正常

- [x] 通过

---

## H-03 — 喷雾碰撞回归

**操作**：Play → 喷雾与目标/障碍物碰撞
**通过条件**：碰撞检测正常触发

- [x] 通过

---

## H-04 — ClearAll 回归

**操作**：场景中有弹丸+激光+喷雾时调用 ClearAll
**通过条件**：全部三种弹幕类型正确清除，无残留

- [x] 通过

---

## 已完成项（Agent 已验证）

| ID   | 检查项                              | 状态  |
| ---- | -------------------------------- | --- |
| C-01 | IDE lint 0 错误                    | ✅   |
| A-01 | BulletCore 48B 对齐                | ✅   |
| A-02 | Allocate() 初始化动画默认值（DEV-005 已修复） | ✅   |
| G-01 | DanmakuSystem 主文件无 VFX using     | ✅   |

---

## 已知遗留项（不阻塞验收）

| ID      | 级别  | 描述                                    | 处置                                            |
| ------- | --- | ------------------------------------- | --------------------------------------------- |
| DEV-005 | 建议  | Renderer 零值检查→源头初始化                   | ✅ 已修复                                         |
| DEV-006 | 建议  | VFXAttachMode 缺 Socket 占位             | ✅ 已修复                                         |
| DEV-007 | 轻微  | PlayAttached 未实现同源同类型去重               | 📝 SprayUpdater VfxSlot guard 绕过，Phase 4 前文档化 |
| DEV-008 | 中等  | Runtime.cs 全限定引用 SpriteSheetVFXSystem | 📝 BridgeConfig 已缓解，完整桥接化留 Phase 4            |

---

## 建议验收顺序

1. **编译**：C-02
2. **加组件**：G-02 → G-03
3. **Demo 回归**：C-03 → H-01 / H-02 / H-03 / H-04
4. **动画功能**：A-03 → A-04 / A-05 / A-06 → A-07
5. **Shader**：B-01 / B-03 → B-02 / B-04 → B-05
6. **激光预警线**：D-01 / D-02 / D-03 / D-04
7. **喷雾 VFX**：E-01 / E-02 → E-03 / E-04 / E-05 → E-06
8. **时间缩放**：F-01 / F-02
