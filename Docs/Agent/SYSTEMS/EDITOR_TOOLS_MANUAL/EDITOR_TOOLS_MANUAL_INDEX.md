---
system: editor-tools
scope: manual-index
last_verified: 2026-05-02
related_code: Assets/_Framework/Editor/**/*.cs, Assets/_Framework/EntitySystem/Editor/*.cs
---

# 编辑器工具使用手册 — 索引

> Agent 可通过本索引快速定位任何编辑器工具的操作步骤、MCP 调用方式和常见错误处理。

## 子文件

| # | 文件 | 覆盖范围 | 行数 |
|---|------|---------|------|
| 1 | [SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_01_BUILD.md](SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_01_BUILD.md) | 构建/导出/Dev Server（7 个菜单项） | ~160 |
| 2 | [SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_02_VALIDATE.md](SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_02_VALIDATE.md) | 校验/审计/引用查找（4 个菜单项） | ~130 |
| 3 | [SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_03_ENTITY.md](SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_03_ENTITY.md) | Entity 系统工具（6 个菜单项） | ~140 |
| 4 | [SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_04_INSPECTORS.md](SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_04_INSPECTORS.md) | 自定义 Inspector + 自动处理器 | ~130 |

## 菜单速查表

| 我想做什么 | 菜单路径 | 子文件 |
|-----------|---------|-------|
| 构建 WebGL（开发） | `Tools/MiniGame Template/Build/Build WebGL (Development)` | 01 |
| 构建 WebGL（发布） | `Tools/MiniGame Template/Build/Build WebGL (Release)` | 01 |
| 检查微信设置 | `Tools/MiniGame Template/Build/Validate WeChat Settings` | 01 |
| 一键导出模式 | `Tools/MiniGame/切换到导出模式 (Build Bundle + WebGL)` | 01 |
| 切回编辑器模式 | `Tools/MiniGame/切换到编辑器模式 (EditorSimulate)` | 01 |
| 导出后处理 | `Tools/MiniGame/导出后处理 (Post-Export)` | 01 |
| 启动本地 Dev Server | `Tools/MiniGame Template/Dev Server` | 01 |
| 检查架构违规 | `Tools/MiniGame Template/Validate/Architecture Check` | 02 |
| 资源预算审计 | `Tools/MiniGame Template/Validate/Asset Audit` | 02 |
| 查找资源引用 | `Tools/MiniGame Template/Find References Of Selected Asset` | 02 |
| 创建 SO 资产 | `Tools/MiniGame Template/SO Creation Wizard` | 02 |
| Entity 调试总览 | `Window/Entity/Debug Overview` | 03 |
| 校验 Entity 配置 | `Tools/Entity/Validate All Configs` | 03 |
| 创建模板 SO | `MiniGameTemplate/Entity/Create P1.11 Template SOs` | 03 |
| 创建 Debug View Prefab | `MiniGameTemplate/Entity/Create Debug View Prefab` | 03 |
| 创建 Damage Number Prefab | `MiniGameTemplate/Entity/Create Damage Number Prefab` | 03 |
| 打包弹幕 Atlas | `Tools/MiniGame Template/Danmaku/Atlas Packer` | 03 |
| 启用/禁用 Spine | `Tools/MiniGame Template/Integrations/Spine/...` | 03 |
| 运行时查看 SO 值 | `Tools/MiniGame Template/Debug/SO Runtime Viewer` | 03 |
| 配置 EntityConfigSO | Inspector 自动加载 | 04 |
| 配置 SkillConfigSO | Inspector 自动加载 | 04 |
| 配置 AIBehaviorSO | Inspector 自动加载 | 04 |
| 配置 EntitySpawnWaveSO | Inspector 自动加载 | 04 |

## 自动处理器（无需手动触发）

| 处理器 | 触发时机 | 详见 |
|-------|---------|------|
| TextureImportEnforcer | 贴图导入时 | 04 §纹理规则 |
| AudioImportEnforcer | 音频导入时 | 04 §音频规则 |

## 相关代码目录

```
Assets/_Framework/Editor/                 ← 通用编辑器工具
Assets/_Framework/Editor/Build/           ← 构建模式切换
Assets/_Framework/Editor/Entity/          ← Entity 系统 Inspector/Validator
Assets/_Framework/Editor/Rendering/       ← Atlas 打包
Assets/_Framework/Editor/Danmaku/         ← 弹幕 SO Inspector
Assets/_Framework/Editor/PropertyDrawers/ ← Variable Drawer
Assets/_Framework/DanmakuSystem/Scripts/Editor/ ← 弹幕编辑器刷新
Assets/_Framework/EntitySystem/Editor/    ← SkillConfigSO Inspector
```
