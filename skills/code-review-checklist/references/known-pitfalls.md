# Known Pitfalls — 活跃层（强制读取）

> **容量上限**：30 条（+ 不限数量的 `[经典]` 条目）。超过时触发蒸馏，详见 SKILL.md「经验库维护规范」。
> **当前条目数**：34 条（PIT-007, PIT-014 ~ PIT-031, PIT-034 ~ PIT-049）
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

## PIT-034: `??` / `?.` 运算符对 Unity Object 无效（fake-null 陷阱） `[经典]`
- **分类**: CL-1 跨文件引用完整性 / CL-6 渲染管线与 Mesh API
- **日期**: 2026-04-13
- **现象**: `BulletRenderer.Rebuild()` 中 `bulletType.SourceTexture ?? _fallbackAtlas` 未触发 fallback，所有弹丸的贴图被当作 null 跳过，弹幕完全不显示
- **根因**: Unity 的 `UnityEngine.Object` 重载了 `== null`（未赋值的序列化字段是 "fake null" 壳对象，C# 引用非 null）。`??` 和 `?.` 运算符使用 C# 原生 null 检查，不走 Unity 重载，所以 `??` 不会触发 fallback，`?.` 不会短路
- **修复**: 改用显式三元表达式 `(srcTex != null) ? srcTex : _fallbackAtlas`
- **验证方法**: 全局搜索 `?? ` 和 `?.`，检查左侧操作数是否为 `UnityEngine.Object` 派生类型；凡是 Unity Object 一律使用 `!= null` / `== null` 显式判断
- **严重度**: 🔴 运行时静默失败，无报错但功能完全不工作
- **标记 [经典] 原因**: C# 语法糖在 Unity 中的最大陷阱，极易在编码时无意识使用；AI 和人类程序员同样容易犯

---

## PIT-035: 同一 VFX 系统多入口失败语义不一致 — Play / PlayAttached
- **分类**: CL-6 渲染管线与 Mesh API
- **日期**: 2026-04-14
- **现象**: `SpriteSheetVFXSystem` 在 `VFXTypeSO` 未注册到 `VFXTypeRegistrySO` 时，`Play()` 会输出 `[SpriteSheetVFXSystem] Type not found in registry: ...`，但 `PlayAttached()` 直接 `return -1`，导致按 `K` 有 error、按 `J` 无 error，表面看像两条路径规则不同
- **根因**: 同一运行时系统的多条播放入口没有复用统一失败处理逻辑，出现 API 语义漂移；一条路径显式报错，另一条路径静默失败
- **修复**: 让 `PlayAttached()` 与 `Play()` 对齐：registry 未命中时统一 `Debug.LogError(...)`，并在日志中写明当前 registry 名称与修复步骤（把对应 `VFXTypeSO` 加入当前 `SpriteSheetVFXSystem` 使用的 `VFXTypeRegistrySO._types` 列表，或改回已注册的 VFXTypeSO）
- **验证方法**: 审查所有并行入口（如 `Play` / `PlayOneShot` / `PlayAttached`）的失败分支，确认日志级别、返回值语义、修复指引完全一致；对白名单/Registry 型系统，未命中必须是 **Error**，不能是 Warning 或静默失败
- **严重度**: 🔴 高误导性运行时问题，容易把同一根因误判成两套规则

---

## PIT-036: 数据层有 AttachMode，执行层却没消费 — Spray VFX 假开关
- **分类**: CL-6 渲染管线与 Mesh API
- **日期**: 2026-04-14
- **现象**: `VFXTypeSO.AttachMode` 在 Inspector 中可选 `World / FollowTarget`，文档和验收也要求区分两种模式；但 `SprayUpdater` 启动 VFX 时只判断 `spray.AttachId != 0`，导致按 `J` 时即使 `AttachMode=World` 也持续跟随 Boss，肉眼完全体验不出 `FollowTarget` 的作用
- **根因**: 数据层配置字段已经暴露，但运行时执行层没有真正消费该字段，实际行为仍由旧条件（`AttachId != 0`）硬编码决定，形成“配置存在但行为不分叉”的假开关
- **修复**: 在 `SprayUpdater` 中按 `sprayType.SprayVFXType.AttachMode` 分支：`FollowTarget` 才走 `PlayAttached(...)`；`World` 走 `Play(...)`，仅在生成瞬间取一次 `spray.Origin` / `spray.Direction`
- **验证方法**: 对每个暴露给设计师的枚举/布尔配置，必须做“行为分叉验收”——至少验证两个取值在运行时有肉眼可见或日志可证的差异；如果改配置前后表现完全一致，优先怀疑执行层没接线，而不是继续调参
- **严重度**: 🔴 架构语义失真，设计师可配置项沦为摆设，验收容易被假通过

---

## PIT-037: Pool.FreeAll 只重置 free list 不清 Data — 幽灵实体复活 VFX
- **分类**: CL-6 渲染管线与 Mesh API
- **日期**: 2026-04-15
- **现象**: 按 R 清场后子弹消失了，但 spray VFX（Loop 模式）仍在屏幕上播放。看起来像 VFX 系统没有响应清场，实际是喷雾系统的"幽灵"数据在下一帧把 VFX 重新启动了
- **根因**: `SprayPool.FreeAll()` 只重置了 free list 和 ActiveCount，**没有清零 `Data[]`**。`ClearAll()` 先遍历 spray 停 VFX（`StopAttached`），再调 `FreeAll()`。但因为 Data 没清，`SprayUpdater.UpdateAll()` 在同一帧或下一帧看到 `Phase != 0` 的幽灵 spray，且 `VfxSlot == -1`（被 ClearAll 设为 -1），于是重新走"首帧 VFX 启动"逻辑，把刚 Stop 的 VFX 又播了一遍
- **修复**: `SprayPool.FreeAll()` 循环内加 `Data[i] = default;`。同步排查 `ObstaclePool.FreeAll()` 也有相同问题，一并修复。对比 `BulletWorld.FreeAll()`（清 `Cores[i].Flags = 0`）和 `LaserPool.FreeAll()`（清 `Data[i] = default`），它们没有此问题
- **验证方法**: 所有自定义对象池的 `FreeAll()` 必须让遍历逻辑（检查 Phase / Flags / IsActive）无法再命中已释放的槽位。最简单的规则：**`Free(index)` 怎么清数据，`FreeAll()` 就怎么清**——不能偷懒只重置 free list
- **严重度**: 🔴 用户可见的运行时 bug，清场功能失效

---

## PIT-038: FairyGUI onTouchBegin 缺少 CaptureTouch — 后续 Move/End 丢失 `[经典]`
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-05-06
- **现象**: `JoystickController.OnTouchBegin` 触发正常（有日志），但 `OnTouchMove` / `OnTouchEnd` 从未触发，摇杆拖动完全无反应
- **根因**: FairyGUI 触摸事件系统要求在 `onTouchBegin` 回调中调用 `context.CaptureTouch()` 才会将后续 Move/End 事件路由到该对象。不调用 = Stage 不知道谁"拥有"这次触摸，Move/End 被丢弃
- **修复**: 在 `OnTouchBegin` 的 `_inputEnabled` 检查通过后添加 `context.CaptureTouch()`
- **验证方法**: 凡是注册了 `onTouchBegin` + `onTouchMove` / `onTouchEnd` 的 FairyGUI 对象，必须确认 Begin 回调中有 `context.CaptureTouch()`。无此调用 = Move/End 100% 丢失
- **严重度**: 🔴 功能完全不工作
- **标记 [经典] 原因**: FairyGUI 触摸三件套铁律，Begin 必须 CaptureTouch，否则 Move/End 丢失。IDE 不报错、编译通过、Begin 有回调，极具迷惑性

---

## PIT-039: 方法重载 + 裸 null 参数 → CS0121 歧义 `[经典]`
- **分类**: CL-10 方法重载安全
- **日期**: 2026-05-19
- **现象**: `BattleFlowHandler` 调用 `SetLaunchContext(null)` 编译报 CS0121——`int?` 和 `BattleLevelData` 两个重载对 `null` 同等匹配
- **根因**: Sprint 2 新增 `SetLaunchContext(BattleLevelData)` 重载后，已有调用点的裸 `null` 既能隐式匹配 nullable value type (`int?`) 又能匹配 reference type (`BattleLevelData`)，编译器无法决策
- **修复**: 显式转型 `SetLaunchContext((int?)null)` 消除歧义
- **验证方法**: 新增方法重载时，全局搜索已有调用点中传递 `null` / `default` 的地方；如果目标参数类型中有 nullable value type 与 reference type 并存，必须显式转型
- **严重度**: 🔴 编译失败
- **标记 [经典] 原因**: 新增重载是常见操作，裸 null 是 C# 中极易忽略的歧义源；AI 生成重载时几乎不会检查已有调用点的 null 传参

---

## PIT-040: MCP 编译检查返回旧缓存 — AssetDatabase.Refresh 铁律
- **分类**: CL-10 方法重载安全 / 工作流
- **日期**: 2026-05-19
- **现象**: 外部编辑器修改 .cs 文件后，MCP 调用 `unity_get_compilation_errors` 返回 0E/0W（假阳性），实际上 Unity 尚未重新编译
- **根因**: Unity Editor 只在获得窗口焦点或显式调用 `AssetDatabase.Refresh()` 时才触发增量编译。MCP 远程查询的是"上一次编译周期"的结果，不是文件当前状态的编译结果
- **修复**: 每次外部修改 .cs 后，必须先 `unity_execute_code("AssetDatabase.Refresh()")` 等编译完成，再查 `unity_get_compilation_errors`
- **验证方法**: 任何"MCP 说 0 错误"的结果，检查时间戳是否在文件修改之后；若不确定，一律先 Refresh 再查
- **严重度**: 🔴 工作流假阳性，会导致带编译错误的代码被标记为"审查通过"

---

## PIT-041: 云函数未同步客户端数据结构 — 新字段静默丢弃 `[经典]`
- **分类**: CL-9 架构一致性
- **日期**: 2026-05-24
- **现象**: 关卡通关后星级（`levelStars`）在本地存档正常，但云端始终为空。换设备/清缓存后星级数据丢失
- **根因**: `CloudFunctions/saveProgress/index.js` 是 V1/V2 时期写的，只解构了 `clearedLevels` + `version`。客户端 V3 新增的 `levelStars`、`unlockedSkillIds`、`unlockedPassiveIds`、成就计数器被 JavaScript 解构语法**静默丢弃**——无任何报错
- **修复**: 在云函数中完整解构所有 `SharedProgressData` 字段并写入数据库；或更好的做法是直接存储整个 event 对象（透传式存储）
- **验证方法**:
  1. 每次客户端持久化数据结构（`SharedProgressData` 等）增删字段时，全局搜索所有相关云函数（`saveProgress`、`getProgress`）
  2. 确认云函数解构/写入了所有新字段
  3. 确认 `getProgress` 返回不会过滤新字段
  4. 部署后真机验证：写入 → 清缓存 → 重新拉取，确认新字段完整
- **为什么危险**: 客户端 `JSON.stringify` 自动包含所有字段 → 上传"成功"；云函数解构 `const { a, b } = event` 丢弃未列出字段 → JavaScript 不报错；`getProgress` 读回来的就是写进去的 → 看不出差异。唯一暴露时机是换设备/清缓存
- **严重度**: 🔴 数据丢失，用户无感知直到换设备
- **标记 [经典] 原因**: 客户端与云端数据结构版本漂移是长期维护中的高频陷阱；JavaScript 解构的静默丢弃特性让问题极难发现

---

## PIT-042: DamageRedirect 转发基地时必须清除暴击
- **分类**: CL-6 渲染管线与 Mesh API / 战斗系统
- **日期**: 2026-05-23
- **现象**: 伤害转发到基地后基地扣血异常放大
- **根因**: DamageRedirect 转发伤害时携带了原始暴击标记，基地不应承受暴击
- **修复**: 转发时 `IsCritical=false, CritMultiplier=1f`
- **验证方法**: 伤害转发链路中检查暴击标记是否被正确清除
- **严重度**: 🔴 数值 bug

---

## PIT-043: HealthComponent 无敌帧必须在 modifier 链之前
- **分类**: CL-5 生命周期与时序 / 战斗系统
- **日期**: 2026-05-23
- **现象**: 无敌状态下仍受到伤害
- **根因**: `_iFrameRemaining > 0` 判断放在 modifier 链之后执行，modifier 已经修改了 HP
- **修复**: 无敌帧检查在 modifier 链之前执行，命中则直接 return
- **验证方法**: HealthComponent 的伤害处理流程中，无敌判断是否为最先执行
- **严重度**: 🔴 逻辑 bug

---

## PIT-044: HealthComponent.OnHpChanged 必须是 HP 同步的单一数据源
- **分类**: CL-9 架构一致性 / 战斗系统
- **日期**: 2026-05-23
- **现象**: HP 显示与实际不一致，某些地方没有更新
- **根因**: 散落的 `floatVariable.SetValue()` 手动同步，遗漏某些路径
- **修复**: 所有 HP 同步统一走 `OnHpChanged` 事件自动推送到 FloatVariable SO
- **验证方法**: 全局搜索 HP 相关的 FloatVariable.SetValue，确认只存在于 OnHpChanged 订阅者中
- **严重度**: 🔴 数据不一致

---

## PIT-045: LaserTypeSO 必须设置 LaserTexture
- **分类**: CL-6 渲染管线与 Mesh API
- **日期**: 2026-05-20
- **现象**: 激光配置完成但运行时完全不显示
- **根因**: `LaserRenderer.Rebuild()` 在 `LaserTexture==null` 时静默跳过渲染，无 Error 日志
- **修复**: 确保所有 LaserTypeSO 的 LaserTexture 字段已赋值；或在 Rebuild 中加 null 检查 Warning
- **验证方法**: 新建 LaserTypeSO 后检查 Inspector 中 LaserTexture 是否已赋值
- **严重度**: 🟡 静默失败无报错

---

## PIT-046: AttachSourceRegistry localOffset 是 Transform 局部坐标
- **分类**: CL-6 渲染管线与 Mesh API / 战斗系统
- **日期**: 2026-05-20
- **现象**: 子弹/特效生成位置偏移到错误方向（如向下而非向前）
- **根因**: Spawner 统一设 `entity.Rotation=270°`，世界空间偏移（如 FireOffset）直传到 localOffset 会被旋转 270°。localOffset 是 Transform 局部坐标
- **修复**: 世界空间偏移需先 `InverseTransformVector` 转换为局部坐标后再赋值
- **验证方法**: 使用 AttachSource 时，确认偏移量是局部坐标还是世界坐标；如果是世界坐标需转换
- **严重度**: 🟡 位置偏移 bug

---

## PIT-047: 纯视觉效果必须用 unscaledDeltaTime `[经典]`
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-05-22
- **现象**: 暂停时飘字/粒子/UI 动效冻住，取消暂停后残留在屏幕上
- **根因**: 视觉效果生命周期用了 `Time.deltaTime`，暂停时 `timeScale=0` 导致 dt=0，效果永远不会结束
- **修复**: 飘字/粒子/UI 动效等不受游戏逻辑控制的纯表现，生命周期必须走 `Time.unscaledDeltaTime`
- **验证方法**: 暂停游戏，观察所有视觉效果是否正常消散/完成动画
- **严重度**: 🔴 视觉残留 bug
- **标记 [经典] 原因**: 凡是"看起来"效果和游戏逻辑无关的 Tick，都容易误用 deltaTime

---

## PIT-048: DontDestroyOnLoad 退场清理三件套 `[经典]`
- **分类**: CL-5 生命周期与时序
- **日期**: 2026-05-22
- **现象**: 切场景后旧场景的子弹/碰撞/对象池残留，导致空引用或僵尸碰撞
- **根因**: DontDestroyOnLoad 对象跨场景存活，但其管理的运行时数据（池、碰撞注册、子弹）未清理
- **修复**: 三件套缺一不可 ①`PoolManager.ClearAll` 回收借出对象 ②`EntitySystemBootstrap.OnDestroy→DespawnAll` 注销 CollisionComponent ③`DanmakuSystem.ClearAll` 立即清子弹
- **验证方法**: 切场景前后检查：对象池借出数归零、碰撞系统无残留注册、弹幕系统无活跃子弹
- **严重度**: 🔴 跨场景泄漏
- **标记 [经典] 原因**: 多系统耦合的清理时序，缺任一步就泄漏，且表现不确定（有时不崩只是静默异常）

---

## PIT-049: 定长数组 swap-remove 必须 default 清尾 `[经典]`
- **分类**: CL-5 生命周期与时序 / 内存管理
- **日期**: 2026-05-22
- **现象**: 对象池释放后 GC 不回收，内存持续增长
- **根因**: `array[i] = array[--count]` 后尾部 `array[count]` 仍持有旧引用，阻止 GC
- **修复**: swap-remove 后必须 `array[count] = default`
- **验证方法**: 所有使用 swap-remove 模式的数组/列表，检查 remove 后是否清尾
- **严重度**: 🟡 内存泄漏
- **标记 [经典] 原因**: 手写数据结构中极易遗漏的一步，编译器和 IDE 均无法检测

---

_（新的踩坑记录追加在此处）_
