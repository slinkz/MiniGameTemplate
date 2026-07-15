---
system: editor-tools
scope: entity-danmaku-spine-debug
last_verified: 2026-05-02
related_code: Assets/_Framework/Editor/Entity/*.cs, Assets/_Framework/Editor/Rendering/*.cs, Assets/_Framework/Editor/SpineIntegrationTools.cs, Assets/_Framework/Editor/SORuntimeViewer.cs
---

# 编辑器工具手册 — Entity / 弹幕 / Spine / 调试

## Entity Debug Overview（Entity 调试总览）

**菜单路径**：`Window/Entity/Debug Overview`
**源码**：`Assets/_Framework/Editor/Entity/EntityDebugWindow.cs`
**用途**：Play Mode 下实时查看 Entity 系统状态——活跃 Entity 总数、Pool 使用率、Entity 列表

### 前置条件
- 必须处于 Play Mode
- 场景中需有 `EntitySystemBootstrap` 组件

### 界面说明

| 区域 | 内容 |
|------|------|
| 概览 | 活跃 Entity 总数、活跃 View 数量、刷怪点数量、全波次完成状态 |
| Pool 使用率 | 按 EntityConfigSO 分组，显示 `ActiveCount/Capacity` |
| Entity 列表 | ID / ConfigName / HP / Position / AI Action（支持按 ConfigName 筛选） |
| 🔄 Restart All Waves | 一键 DespawnAll + Spawner.RestartAll()（WF-002） |

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Window/Entity/Debug Overview");
```

### 常见问题
| 情况 | 原因 | 解决 |
|------|------|------|
| 显示"Entity System 未初始化" | 场景缺少 EntitySystemBootstrap | 添加 Bootstrap 到场景 |
| 列表为空 | 无活跃 Entity / 未触发波次 | 检查 EntitySpawnPoint 配置 |

---

## Validate All Configs（Entity 配置批量校验）

**菜单路径**：`Tools/Entity/Validate All Configs`
**源码**：`Assets/_Framework/Editor/Entity/EntityConfigValidator.cs`
**用途**：批量校验所有 EntityConfigSO / AIBehaviorSO / EntitySpawnWaveSO 资产

### 校验规则

**EntityConfigSO**：

| 检查项 | 级别 |
|--------|------|
| Components 列表为空 | Error |
| Components 重复项 | Warning |
| Control + AI 同时存在 | Error |
| AI 组件但 AIBehavior 为空 | Warning |
| Attack 组件但 BulletPattern 为空（且 AttackInterval > 0） | Warning |
| Collision 组件但 CollisionRadius ≤ 0 | Warning |
| PoolMax ≤ 0 | Error |

**AIBehaviorSO**：

| 检查项 | 级别 |
|--------|------|
| Entries 为空 | Error |
| 缺少 `Always` 兜底条目 | Error |

**EntitySpawnWaveSO**：

| 检查项 | 级别 |
|--------|------|
| Waves 数组为空 | Error |
| Wave.Groups 为空 | Warning |
| Group.EntityConfig 为 null | Error |
| Group.Count ≤ 0 | Warning |
| Loop=true 但 LoopStartWave ≥ Waves.Length | Error |

### 附加输出
- **AIBehaviorSO 反向引用摘要**：显示每个 AI 行为被哪些 EntityConfig 引用

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/Entity/Validate All Configs");
// 或直接：
MiniGameTemplate.EditorTools.EntityConfigValidator.ValidateAll();
```

---

## Create P1.11 Template SOs（模板 SO 创建器）

**菜单路径**：`MiniGameTemplate/Entity/Create P1.11 Template SOs`
**源码**：`Assets/_Framework/Editor/Entity/EntityTemplateSO_Creator.cs`
**用途**：一键创建全套 Demo 用模板 SO 资产（WF-009）

### 创建的资产

| 资产 | 路径 | 说明 |
|------|------|------|
| Template_Player | `_Template/Entity/` | 玩家配置（State+Health+Movement+Collision+Control+Attack） |
| Template_Slime | `_Template/Entity/` | 敌人配置（State+Health+Movement+Collision+AI+Attack） |
| Template_SlimeAI | `_Template/AI/` | Slime AI 行为（InRange→Attack, Always→MoveToTarget） |
| Template_EnemyWave | `_Template/SpawnWave/` | 两波次刷怪（3只 → 5只 Slime，循环） |
| Template_DebugViewPool | `_Template/Pool/` | 占位——需手动指定 Prefab |
| Template_DamageNumberPool | `_Template/Pool/` | 占位——需手动指定 Prefab |

### 注意事项
- 已存在的同名资产**不会被覆盖**（幂等安全）
- 目录不存在时自动创建
- Pool 资产为占位，需配合 Prefab 创建器使用

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("MiniGameTemplate/Entity/Create P1.11 Template SOs");
```

---

## Create Debug View Prefab / Damage Number Prefab

**菜单路径**：
- `MiniGameTemplate/Entity/Create Debug View Prefab`
- `MiniGameTemplate/Entity/Create Damage Number Prefab`

**源码**：`Assets/_Framework/Editor/Entity/EntityDebugViewPrefabCreator.cs`
**用途**：代码生成调试用 Prefab，无需手工搭建

### Debug View Prefab
- 输出路径：`Assets/_Game/Prefabs/Debug/EntityDebugView.prefab`
- 结构：根 GO → Sprite 子物体（32x32 圆形 SpriteRenderer）+ HPText 子物体（TextMesh）
- 附带生成 `DebugCircle.png`（32x32 白色圆形纹理）

### Damage Number Prefab
- 输出路径：`Assets/_Game/Prefabs/Debug/DamageNumber.prefab`
- 结构：根 GO + TextMesh（金色，48 号字，缩放 0.12）

### Agent MCP 调用
```csharp
// 创建 Debug View
MiniGameTemplate.Editor.Entity.EntityDebugViewPrefabCreator.CreateDebugViewPrefab();
// 创建伤害数字
MiniGameTemplate.Editor.Entity.EntityDebugViewPrefabCreator.CreateDamageNumberPrefab();
```

---

## Entity Gizmo Drawer（Entity 碰撞圈 Gizmo）

**源码**：`Assets/_Framework/Editor/Entity/EntityGizmoDrawer.cs`
**用途**：Play Mode 下在 Scene View 绘制 Entity 碰撞圈和 HP 标签
**触发方式**：自动（`[InitializeOnLoad]` + `SceneView.duringSceneGui`，无需手动触发）

### 可视化内容
| 元素 | 说明 |
|------|------|
| 碰撞圈线框 | 按阵营着色：Player=绿、Enemy=红、Neutral=灰 |
| HP 标签 | 碰撞圈上方显示 `HP: current/max` |
| 未初始化提示 | EntitySystem 未初始化时在 Scene View 显示 HelpBox |

### 注意
- 仅 Play Mode 生效
- 零运行时开销——全部代码在 Editor asmdef

---

## Atlas Packer（弹幕 Atlas 打包工具）

**菜单路径**：`Tools/MiniGame Template/Danmaku/Atlas Packer`
**源码**：`Assets/_Framework/Editor/Rendering/DanmakuAtlasPackerWindow.cs`
**用途**：将多张独立贴图打包成一张 Atlas + 生成 AtlasMappingSO

### 操作步骤
1. 打开窗口
2. 选择域（Bullet / VFX）——不允许混打
3. 配置 Atlas 最大尺寸（512~4096）、Padding、输出目录
4. 添加源贴图（拖拽 / 点击 / 从文件夹批量添加）
5. 点击「🔨 打包 Atlas」
6. 查看打包报告和预览

### 排列策略
| 条件 | 排列方式 |
|------|---------|
| 所有子图尺寸相同 | 网格排列（左上角起，支持序列帧） |
| 混合尺寸 | `PackTextures` 自动布局 |

### 重新打包
- 将已有 `AtlasMappingSO` 拖入"已有 AtlasMappingSO"字段
- 自动还原源贴图列表、padding、输出目录
- 打包后覆盖同一 Mapping

### 输出
- Atlas PNG：`{域}Atlas_{W}x{H}.png`
- AtlasMappingSO：包含每张子图的 SourceTexture / GUID / UVRect / PixelRect

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Danmaku/Atlas Packer");
```

> **注意**：这是 EditorWindow，打包操作需用户交互。

---

## Spine Integration（Spine 集成管理）

**菜单路径**：`Tools/MiniGame Template/Integrations/Spine/`
**源码**：`Assets/_Framework/Editor/SpineIntegrationTools.cs`
**用途**：通过 Scripting Define Symbols 控制 Spine 集成的启用/禁用

### 菜单项

| 菜单项 | 功能 |
|--------|------|
| Enable Spine (Current Target) | 添加 `FAIRYGUI_SPINE` + `ENABLE_SPINE` |
| Disable Spine (Current Target) | 移除上述两个 Define |
| Validate Integration | 检查 Define / 源码链接 / asmdef / 运行时程序集 一致性 |

### 前置条件
- 启用前需先运行 `UnityProj/Tools/setup_spine.bat`（或 .sh）创建源码符号链接
- 两个 Define 必须同步启用/禁用

### Agent MCP 调用
```csharp
// 启用
MiniGameTemplate.EditorTools.SpineIntegrationTools.EnableSpine();
// 禁用——通过菜单
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Integrations/Spine/Disable Spine (Current Target)");
// 校验
MiniGameTemplate.EditorTools.SpineIntegrationTools.ValidateIntegration();
```

---

## SO Runtime Viewer（SO 运行时查看器）

**菜单路径**：`Tools/MiniGame Template/Debug/SO Runtime Viewer`
**源码**：`Assets/_Framework/Editor/SORuntimeViewer.cs`
**用途**：Play Mode 下实时查看所有 SO 变量、事件、RuntimeSet 的值

### 界面说明

| Tab | 显示内容 |
|-----|---------|
| Variables | IntVariable / FloatVariable / BoolVariable / StringVariable 的 name=value |
| Events | GameEvent 名称 + ListenerCount + 「Raise」测试按钮 |
| RuntimeSets | RuntimeSet 子类 + Items Count |

### 操作
- 支持按名称筛选
- 点击「Select」跳转到对应 SO 资产
- Events Tab 可通过「Raise」手动触发事件（调试用）

### Agent MCP 调用
```csharp
EditorApplication.ExecuteMenuItem("Tools/MiniGame Template/Debug/SO Runtime Viewer");
```

> **注意**：仅 Play Mode 可用。
