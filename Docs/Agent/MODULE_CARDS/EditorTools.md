---
system: knowledge-engineering
scope: module-card-editor-tools
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/SO_Config_Workflow.md, Docs/Agent/CONTEXT_PACKS/WeChat_Build_Cloud.md
---

# Module Card: EditorTools

## 1. 模块职责

EditorTools 提供 Unity Editor 内的构建、校验、资源审计、SO 创建、引用查找、自定义 Inspector、Atlas 打包、Entity 调试、Dev Server 与第三方集成辅助工具。它的目标是把重复、易错、平台相关的人工流程固化成菜单、窗口、校验器和导入规则。

## 2. 不负责什么

- 不承载运行时业务逻辑，Editor 代码不得被 Player 构建依赖。
- 不绕过 SO / Asset / Build 的事实源，只做创建、校验、预览和自动化。
- 不替代微信开发者工具或真机验收。
- 不自动迁移旧资产语义；涉及批量改资产时必须保留 Undo、日志和可回滚路径。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `MiniGameBuildPipeline` | WebGL Development/Release 构建与微信设置校验 |
| `BuildModeSwitch` | EditorSimulate / WebGL 导出模式切换与 YooAsset Bundle 构建 |
| `LocalHttpServerWindow` | 本地 Dev Server、端口检测、CDN 一致性检查 |
| `ArchitectureValidator` | 架构规则扫描 |
| `AssetAuditWindow` | 资源预算审计 |
| `AssetReferenceFinder` | Project 资源引用查找 |
| `SOCreationWizard` | SO 创建向导 |
| `SORuntimeViewer` | 运行时 SO 值查看 |
| `EntityConfigValidator` | Entity 配置批量校验 |
| `DanmakuAtlasPackerWindow` | Danmaku/VFX Atlas 打包 |
| `TextureImportEnforcer`, `AudioImportEnforcer` | 导入规则自动处理 |
| ShooterGame Editor 工具 | SG SO 校验、战斗状态、DPS、技能预览、进度重置等 |

## 4. 数据流

```text
菜单 / Inspector / AssetPostprocessor
  -> EditorTools API
  -> AssetDatabase / PlayerSettings / BuildPipeline / YooAsset / http-server
  -> 生成资产、构建产物、校验报告或调试窗口
  -> Console / Dialog / Project 资源反馈
```

## 5. 生命周期

```text
打开 Unity Editor -> 菜单/导入/Inspector 触发 -> 执行工具 -> 写资产或产物 -> SaveAssets/Refresh -> 人工或自动验证
```

导入处理器会自动触发；构建、Dev Server、批量创建工具通常由菜单或 MCP 显式触发。

## 6. 依赖关系

EditorTools 只存在于 Editor 程序集，依赖 UnityEditor API、项目运行时类型和第三方编辑器包。运行时代码可以被 EditorTools 读取和校验，但运行时代码不能依赖 EditorTools。

## 7. 关键配置 / 资产路径

```text
UnityProj/Assets/_Framework/Editor/
UnityProj/Assets/_Framework/Editor/Build/
UnityProj/Assets/_Framework/Editor/Entity/
UnityProj/Assets/_Framework/Editor/Rendering/
UnityProj/Assets/_Framework/Editor/Danmaku/
UnityProj/Assets/_Framework/Editor/PropertyDrawers/
UnityProj/Assets/_Game/Editor/ShooterGame/
UnityProj/Tools/
UnityProj/DataTables/
```

## 8. 关键 ADR / 约束

- Editor 工具不能泄漏到运行时 asmdef 或 Player 构建。
- 批量资产修改必须可审计：明确目标路径、日志、Undo 或可重建策略。
- WebGL/微信构建工具必须遵守 `SYSTEMS/CONV/CONV_03_PLATFORM.md` 的平台约束。
- 自动导入规则要跳过 ThirdParty / FairyGUI 等外部资产路径，避免破坏供应商资源。

## 9. 热路径 / 平台约束

- Editor 扫描可慢，但不能在 AssetPostprocessor 中递归触发导入风暴。
- 构建工具会修改 PlayerSettings、BuildTarget、AssetConfig.PlayMode，运行前要确认当前工作意图。
- Dev Server 使用本地进程和端口，Unity 重编译后要重新确认窗口状态。
- `UnityProj/Tools/**` 中的 setup/gen_config 脚本属于工程工具链，改动后要同时验证 Windows 与非 Windows 脚本是否仍一致。

## 10. 常见错误

- 在运行时 asmdef 中引用 EditorTools 命名空间。
- MCP 直接执行带确认弹窗的菜单，导致流程阻塞。
- 改 SO 结构后忘记更新 Inspector、Validator 和创建向导。
- 改微信构建/CDN 后只跑 Editor 菜单，未验证开发者工具和真机。
- 批量资源导入规则误处理 ThirdParty/FairyGUI 目录。

## 11. 修改前必读

- `SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_INDEX.md`
- `SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_01_BUILD.md`
- `SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_02_VALIDATE.md`
- `SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_03_ENTITY.md`
- `SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_04_INSPECTORS.md`
- `CONTEXT_PACKS/SO_Config_Workflow.md`
- `CONTEXT_PACKS/WeChat_Build_Cloud.md`
- `TOOLS/MCP_INTEGRATION.md`

## 12. 修改后必验

- Unity Editor 编译无错误。
- 新增/修改菜单可打开，Validate 方法不误禁用。
- 批量资产工具有日志、目标路径明确，必要时保留 Undo 或可重建路径。
- 自定义 Inspector 不破坏已有序列化字段。
- 构建/Dev Server/微信相关工具至少验证到本次改动涉及的菜单路径。
- AssetPostprocessor 改动需验证导入不会递归、不会处理 ThirdParty/FairyGUI 外部资产。
