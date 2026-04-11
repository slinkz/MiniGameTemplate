# Known Pitfalls — 活跃层（强制读取）

> **容量上限**：30 条（+ 不限数量的 `[经典]` 条目）。超过时触发蒸馏，详见 SKILL.md「经验库维护规范」。
> **当前条目数**：18 条（PIT-007, PIT-014 ~ PIT-031）
> **归档层**：`known-pitfalls-archive.md`（13 条，PIT-001~006, PIT-008~013）

---

> 已归档 → PIT-001~004（已被 CL-1 覆盖）
> 已归档 → PIT-005~006（已被 CL-2 覆盖）

## PIT-007: 命名空间就近解析歧义 — Example.xxx `[经典]`
- **分类**: CL-2 命名空间安全
- **日期**: 2026-04-10
- **现象**: `ClickGameSceneEntry.cs` 中 `Example.xxx` 被解析为当前命名空间子空间
- **根因**: 文件位于 `MiniGameTemplate.Example` 命名空间，引用其他 `Example` 开头的类型产生歧义
- **修复**: 使用 `global::Example.xxx` 显式限定
- **严重度**: 🟡 需要 global:: 前缀
- **标记 [经典] 原因**: 此模式在跨 asmdef 重构时极易反复出现

---

> 已归档 → PIT-008（已被 CL-8 覆盖）
> 已归档 → PIT-009~013（已被 CL-3 覆盖）

## PIT-014: FairyGUI 包加载路径错误
- **分类**: CL-4 字符串级引用验证
- **日期**: 2026-04-06
- **现象**: `UIPackageLoader` 默认路径 `Assets/FairyGUI_Export/` 应为 `Assets/_Game/FairyGUI_Export/`
- **根因**: 代码中硬编码的路径与项目实际目录结构不一致
- **验证方法**: 路径常量与磁盘实际结构对照
- **严重度**: 🟡 运行时加载失败

---

## PIT-015: FairyGUI 包加载路径拼接多层
- **分类**: CL-4 字符串级引用验证
- **日期**: 2026-04-06
- **现象**: FairyGUI 加载路径拼接多了子目录层级（实际是平铺结构）
- **根因**: 假设 FairyGUI 导出有子目录，实际是扁平结构
- **验证方法**: 查看 `FairyGUI_Export/` 目录下的实际文件布局
- **严重度**: 🟡 运行时加载失败

---

## PIT-016: 场景名与文件名不一致
- **分类**: CL-4 字符串级引用验证
- **日期**: 2026-04-10
- **现象**: `MainMenuPanel.Logic.cs` 加载不存在的场景名 `ClickGame`，实际场景文件名为 `Game`
- **根因**: 代码中硬编码的场景名与 `EditorBuildSettings` / 磁盘文件不一致
- **验证方法**: `SceneManager.LoadScene("X")` → 磁盘确认 X.unity 存在
- **严重度**: 🟡 运行时崩溃

---

## PIT-017: PanelPackageName 指向旧包名
- **分类**: CL-4 字符串级引用验证
- **日期**: 2026-04-10
- **现象**: `ClickCounterPanel.Logic.cs` 的 `PanelPackageName` 指向旧包名
- **根因**: 包名变更后遗漏更新引用
- **验证方法**: `PanelPackageName` 与 FairyGUI `package.xml` 中的 `name` 属性对照
- **严重度**: 🟡 运行时 UI 加载失败

---

## PIT-018: EditorBuildSettings 场景路径不一致
- **分类**: CL-4 字符串级引用验证
- **日期**: 2026-04-10
- **现象**: `EditorBuildSettings.asset` 中场景路径与实际文件名不一致
- **根因**: 重命名/移动场景后未更新 Build Settings
- **验证方法**: `EditorBuildSettings` 中的路径与磁盘文件对照
- **严重度**: 🟡 构建时场景丢失

---

## PIT-019: Luban groups 空数组导致零输出 `[经典]`
- **分类**: CL-4 字符串级引用验证
- **日期**: 2026-04-06
- **现象**: `luban.conf` groups 空数组导致 Luban 导出零个表
- **根因**: Luban v4.x 必须定义至少一个 `default: true` 的 group
- **验证方法**: `luban.conf` 中确认 groups 配置正确
- **严重度**: 🟡 配置表完全丢失
- **标记 [经典] 原因**: Luban 配置易被新手/新项目忽略，反复发生

---

## PIT-020: YooAsset 收集配置为空
- **分类**: CL-4 字符串级引用验证
- **日期**: 2026-04-05
- **现象**: `AssetBundleCollectorSetting.asset` Packages 列表为空
- **根因**: 忘记配置 YooAsset 的资源收集路径
- **验证方法**: 确认 YooAsset 收集配置覆盖了需要加载的资产目录
- **严重度**: 🟡 运行时资源加载失败

---

## PIT-021: Timer 回调内取消被后续覆盖（僵尸计时器） `[经典]`
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-04-07
- **现象**: TimerService 回调内 Cancel 被后续写回覆盖，导致僵尸计时器
- **根因**: 回调中修改状态后，回调外的代码又写回了旧值
- **验证方法**: 回调中的状态修改是否会被调用端后续代码覆盖
- **严重度**: 🔴 逻辑 bug
- **标记 [经典] 原因**: 回调与调用方的状态竞争是常见陷阱

---

## PIT-022: 先推进后检查 — 激光/喷雾 TickTimer
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-04-09
- **现象**: 激光/喷雾 TickTimer 先推进再检查 → 永远跳过首帧伤害
- **根因**: `elapsed += dt; if (elapsed >= interval)` 而不是先检查后推进
- **验证方法**: 定时器/计数器的推进与检查顺序是否正确
- **严重度**: 🟡 逻辑 bug

---

## PIT-023: Pierce 冷却单 byte 多目标覆写
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-04-09
- **现象**: Pierce 冷却用单 byte `LastHitId`，多目标场景覆写丢失
- **根因**: 单一变量无法追踪多个目标的碰撞历史
- **验证方法**: 多目标交互场景下，单值状态是否满足需求
- **严重度**: 🟡 逻辑 bug

---

## PIT-024: 事件双绑定 — OnRefresh→OnOpen `[经典]`
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-04-08
- **现象**: `OnRefresh` 调用 `OnOpen` 导致 `onClick.Add()` 被重复执行，4 个面板均受影响
- **根因**: `OnRefresh` 应该调 `ApplyData(data)`，而不是 `OnOpen(data)`
- **验证方法**: `OnRefresh` 内部是否只做数据更新，不做事件绑定
- **严重度**: 🔴 逻辑 bug（点击触发多次）
- **标记 [经典] 原因**: FairyGUI 面板生命周期核心规则，新面板必检

---

## PIT-025: Dialog SortOrder 被遮挡
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-04-06
- **现象**: Dialog 层级 SortOrder 低于 Loading 面板导致 UI 92% 卡死
- **根因**: SortOrder 配置不当，Loading 遮挡了 Dialog
- **验证方法**: 新增面板时确认 SortOrder 层级关系正确
- **严重度**: 🔴 交互完全卡死

---

## PIT-026: ClosePanel 后回调被清空
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-04-07
- **现象**: `ClickCounterPanel.OnBackClicked()` 关闭面板后回调被清空
- **根因**: ClosePanel 触发 OnClose 清空所有引用，后续回调调用时已为 null
- **修复**: 先保存回调到局部变量，再 ClosePanel
- **验证方法**: ClosePanel 后是否有代码依赖面板的成员变量
- **严重度**: 🟡 运行时 NRE

---

## PIT-027: AssetService 返回 null 导致 NRE
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-04-05
- **现象**: `AssetService.UnloadUnusedAssetsAsync` 返回 null 时下游 NRE
- **根因**: 未处理异步方法返回 null 的情况
- **验证方法**: 异步方法的返回值是否可能为 null
- **严重度**: 🟡 运行时 NRE

---

## PIT-028: 顶点属性字段顺序错位 — DanmakuVertex `[经典]`
- **分类**: CL-6 渲染管线与 Mesh API
- **日期**: 2026-04-10
- **现象**: `DanmakuVertex` 字段顺序 Position→UV→Color，但 Unity 标准顺序是 Position→Color→UV
- **根因**: Unity Mesh API 会按 VertexAttributeDescriptor 的声明顺序重新排列数据
- **修复**: 调整字段顺序为 Position→Color→UV
- **验证方法**: 自定义顶点结构与 descriptor 声明顺序逐字段对照
- **严重度**: 🔴 渲染完全错乱
- **标记 [经典] 原因**: Mesh API 隐式规则，IDE 无法检测，必须人工审查

---

## PIT-029: 材质纹理未绑定 — BulletRenderer
- **分类**: CL-6 渲染管线与 Mesh API
- **日期**: 2026-04-10
- **现象**: `BulletRenderer.Initialize()` 从未把 `BulletAtlas` 绑定到材质 `_MainTex`
- **根因**: 创建材质时遗漏了纹理赋值步骤
- **修复**: 在 Initialize 中添加 `material.mainTexture = bulletAtlas`
- **验证方法**: 新建 Renderer 时确认所有材质属性已正确赋值
- **严重度**: 🔴 渲染全白/全黑

---

## PIT-030: 只改 C# 代码不改 FairyGUI 源文件 `[经典]`
- **分类**: CL-7 FairyGUI 改动完整性
- **日期**: 2026-04-10
- **现象**: 将主界面"开始游戏"按钮改为"ClickGame"和"弹幕Demo"两个按钮时，只修改了 C# 代码，未修改 FairyGUI 的 XML 源文件
- **根因**: 颠倒了 FairyGUI 改动的正确顺序。UI 结构变更必须先改 FairyGUI 源文件（XML），再由 FairyGUI 编辑器重新导出 C# 代码，最后才能修改业务逻辑
- **正确流程**:
  1. 修改 `UIProject/assets/MainMenu/MainMenuPanel.xml`，增加两个按钮组件
  2. 在 FairyGUI 编辑器中重新发布，或提醒用户重新导出
  3. 更新 `MainMenuPanel.Logic.cs` 中的业务逻辑代码
- **验证方法**: 任何 UI 结构变更（增删组件/按钮/面板），检查 FairyGUI XML 是否已同步修改
- **严重度**: 🔴 运行时找不到组件 / 编译通过但功能异常
- **标记 [经典] 原因**: AI 最容易犯的错误 — 直接改 C# 而忽略 UI 源文件

---

## PIT-031: 移动代码未同步移动 FairyGUI 导出代码和导出路径 `[经典]`
- **分类**: CL-7 FairyGUI 改动完整性
- **日期**: 2026-04-10
- **现象**: 将 ClickGame 从 `_Game` 移动到 `_Example` 时，只移动了手写脚本，未移动 FairyGUI 导出的 C# 代码（Binder/Panel/组件类），也未修改 `package.xml` 中的 `codePath`
- **根因**: 没有意识到 FairyGUI 的代码导出路径是在 `package.xml` 中配置的，移动代码时必须同步更新
- **正确流程**:
  1. 修改 `UIProject/assets/ClickGame/package.xml`，将 `codePath` 从 `../UnityProj/Assets/_Game/Scripts/UI` 改为 `../UnityProj/Assets/_Example/ClickGame/UI`
  2. 将 FairyGUI 自动生成的 C# 代码移动到新路径
  3. 将 `.Logic.cs` 手写代码移动到新路径
  4. 确认 namespace 仍正确（FairyGUI 生成代码的 namespace = 包名，不变）
  5. 确认调用端使用 `global::ClickGame.XXX` 引用
- **验证方法**: 移动 FairyGUI 相关代码时，检查 `package.xml` 的 `codePath` 是否已更新
- **严重度**: 🔴 编译失败 / 下次 FairyGUI 发布会覆盖到旧路径
- **标记 [经典] 原因**: AI 最容易犯的错误 — 只移动手写代码而忽略生成代码和配置

---

## PIT-032: 自定义类名与 Unity 内置组件重名 — VFXRenderer
- **分类**: CL-2 命名空间安全
- **日期**: 2026-04-11
- **现象**: 新增 `VFXRenderer` 类时与 Unity 内置组件同名，触发 warning：`AddComponent and GetComponent will not work with this script`
- **根因**: 新建运行时类时只检查了当前模块内命名冲突，未对 UnityEngine / Unity 内置组件名做全局避让
- **修复**: 重命名为更具体的 `VFXBatchRenderer`
- **验证方法**: 新增类型命名后，额外搜索 Unity 常见内置组件/类型名；避免使用过于泛化的 `Renderer` / `Manager` / `Debug` / `Animation` 等命名
- **严重度**: 🟡 编译 warning，但会导致组件 API 行为异常

---

## PIT-033: 先怀疑逻辑切换，后发现只是视觉区分度不足 — VFX 多类型播放
- **分类**: CL-6 渲染管线与 Mesh API
- **日期**: 2026-04-11
- **现象**: `VFXDemo` 中按 `4` 选择蓝色爆炸后，日志已显示 `selectedType=VFXType_Explosion_Blue` 且 `runtimeIndex=1`，但肉眼仍觉得看到的是黄色爆炸，导致排查长时间围绕输入链路、运行时索引和渲染映射打转
- **根因**: 占位爆炸图集本身偏黄白，高亮区域在 `Additive` 混合下会继续向白色漂移；蓝色变体最初仅靠较浅的蓝色 Tint 区分，视觉差异不足。问题本质是“可视化验证样本设计失败”，不是播放逻辑失效
- **修复**: 蓝色变体改为高对比验证配置（更深蓝 Tint、更大尺寸、切到 Normal 层），先确保肉眼可区分；同时把 `RuntimeIndex` 从 SO 序列化字段改为纯运行时字段，避免真实逻辑问题与视觉误判叠加
- **验证方法**: 新增多类型/多皮肤示例时，先做“肉眼可区分性检查”——至少保证轮廓、尺寸、颜色、混合层中有一项明显不同；若日志已证明类型切换正确，应优先怀疑渲染表现与验证素材，而不是继续在输入/状态机层兜圈子
- **严重度**: 🔴 高成本误判，会显著拉长排查链路

---

_（新的踩坑记录追加在此处）_
