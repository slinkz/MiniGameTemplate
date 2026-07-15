---
system: editor-tools
scope: inspectors-drawers-postprocessors
last_verified: 2026-05-02
related_code: Assets/_Framework/Editor/Entity/*SOEditor.cs, Assets/_Framework/Editor/Danmaku/*Editor.cs, Assets/_Framework/EntitySystem/Editor/*.cs, Assets/_Framework/Editor/AssetImportEnforcer.cs, Assets/_Framework/Editor/PropertyDrawers/*.cs
---

# 编辑器工具手册 — 自定义 Inspector / Drawer / 自动处理器

## 自定义 Inspector 总览

以下 SO 类型有自定义 Inspector，选中资产时自动加载——无需菜单操作。

| SO 类型 | Inspector 类 | 核心功能 |
|---------|-------------|---------|
| EntityConfigSO | `EntityConfigSOEditor` | Checkbox Grid + 条件显示段落 + 校验 HelpBox |
| AIBehaviorSO | `AIBehaviorSOEditor` | ReorderableList + 可读摘要标题 + Always 兜底检查 |
| EntitySpawnWaveSO | `EntitySpawnWaveSOEditor` | 只读波次摘要面板 + Loop 可视化 |
| SkillConfigSO | `SkillConfigSOEditor` | ISkillEffect 类型发现 + 动态列表添加/删除 |
| BulletTypeSO | `BulletTypeSOEditor`（2 份） | 弹幕外观预览 + 参数分组 |
| LaserTypeSO | `LaserTypeSOEditor` | 激光参数编辑 |
| AtlasMappingSO | `AtlasMappingSOEditor` | Atlas 可视化预览 + 子图 UV 信息 |

---

## EntityConfigSOEditor

**源码**：`Assets/_Framework/Editor/Entity/EntityConfigSOEditor.cs`
**目标类型**：`EntityConfigSO`

### 功能
1. **Checkbox Grid**（ET-001）：ComponentType 枚举值以复选框网格显示，直观勾选
2. **条件显示**（ET-002）：只有勾选了对应组件才显示相关参数段
   - Health → MaxHp, 受击参数
   - Movement → MoveSpeed, TurnSpeed
   - Collision → CollisionRadius, Entity 碰撞参数
   - Attack → AttackPower, CritRate, AttackInterval, BulletPattern, FireOffset
   - AI → AIBehavior 引用
   - View → ViewPrefab, ViewPoolDef, SpriteAnimData, DebugColor, 受击视觉反馈
3. **校验 HelpBox**：
   - WF-006：Components 为空时红色 Error HelpBox
   - Control+AI 同时存在时 Error HelpBox
   - WF-002：Play Mode 下黄色 HelpBox 提醒"修改不会持久化"
4. **分段标题**（WF-011）：每个条件段之前有加粗标题分隔

### Agent 注意
- 修改 EntityConfigSO 字段后，Inspector 自动刷新——无需手动 Repaint
- 新增 ComponentType 枚举值后，无需修改 Inspector（自动遍历枚举）

---

## AIBehaviorSOEditor

**源码**：`Assets/_Framework/Editor/Entity/AIBehaviorSOEditor.cs`
**目标类型**：`AIBehaviorSO`

### 功能
1. **ReorderableList**（ET-005）：条目可拖拽排序
2. **可读标题**：每条显示 `If [Condition] → [Action]` 摘要
3. **Always 兜底校验**（WF-005）：若最后一条 Condition 不是 `Always`，底部显示红色 HelpBox

---

## EntitySpawnWaveSOEditor

**源码**：`Assets/_Framework/Editor/Entity/EntitySpawnWaveSOEditor.cs`
**目标类型**：`EntitySpawnWaveSO`

### 功能
1. **只读摘要面板**（ET-007）：Waves 上方显示
   - 每个 Wave 的触发条件 + 各 Group 概要（EntityConfig 名称 × 数量）
   - Loop 标记和 LoopStartWave 可视化
2. 摘要面板下方保留默认 Inspector 用于实际编辑

---

## SkillConfigSOEditor

**源码**：`Assets/_Framework/EntitySystem/Editor/SkillConfigSOEditor.cs`
**目标类型**：`SkillConfigSO`

### 功能
1. **TypeCache 类型发现**（ATK-001）：自动扫描所有程序集中的 `ISkillEffect` 实现
2. **动态效果列表**：
   - 「+」按钮：从下拉菜单选择 ISkillEffect 类型并添加为子资产
   - 「-」按钮：删除选中的效果
3. **零第三方依赖**——不使用 Odin
4. **跨 asmdef 发现**：`_Game/` 下扩展的 ISkillEffect 实现自动出现在下拉菜单

---

## Variable Property Drawers

**源码**：`Assets/_Framework/Editor/PropertyDrawers/`
**覆盖类型**：IntVariable / FloatVariable / BoolVariable / StringVariable

### 功能
- 在 MonoBehaviour Inspector 中，当字段类型为 Variable SO 时，右侧内联显示当前运行时值
- Play Mode 下值随帧刷新

---

## 自动处理器（AssetPostprocessor）

以下处理器在资源导入时自动运行——无需手动触发，无菜单项。

### TextureImportEnforcer

**源码**：`Assets/_Framework/Editor/AssetImportEnforcer.cs`
**触发时机**：`OnPreprocessTexture`（每次贴图导入/重导入）
**跳过**：`ThirdParty/`、`Assets/FairyGUI/`

| 规则 | 行为 |
|------|------|
| maxTextureSize > 1024 | 强制降至 1024 |
| 文件名以 `_N` 结尾 | 自动设为 NormalMap 类型 |
| mipmapEnabled = true | 强制关闭（小游戏无 LOD） |
| isReadable = true | 强制关闭（节省内存）¹ |
| 缺少 WebGL override | 自动添加 ASTC_6x6（Normal Map 用 ASTC_4x4） |

> ¹ Atlas Packer 打包期间自动跳过 isReadable 检查（`IsPackingInProgress` 标志）

### AudioImportEnforcer

**源码**：同上文件
**触发时机**：`OnPreprocessAudio` + `OnPostprocessAudio`
**跳过**：`ThirdParty/`、`Assets/FairyGUI/`

| 规则 | 行为 |
|------|------|
| 缺少 WebGL override | 自动添加：CompressedInMemory, Vorbis, 50% quality, OptimizeSampleRate |
| 短音频 < 3s + 非 Mono | 强制转 Mono（节省内存），延迟 reimport 防递归 |

### 反递归机制
AudioImportEnforcer 使用 `_reimportingPaths` HashSet + `EditorApplication.delayCall` 防止 `OnPostprocessAudio` 触发的 reimport 导致无限循环。

---

## DanmakuEditorRefreshCoordinator

**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Editor/DanmakuEditorRefreshCoordinator.cs`
**触发时机**：Domain Reload / ScriptReloaded / SO 修改
**用途**：弹幕 SO 修改后自动刷新编辑器预览——开发者无需手动刷新

---

## Agent 最佳实践

1. **修改 SO 后无需手动刷新 Inspector**——Unity 的 `SerializedObject.Update()` + `ApplyModifiedProperties()` 自动处理
2. **新建贴图后无需手动设置压缩**——TextureImportEnforcer 自动执行
3. **修改音频后无需手动设置 WebGL 参数**——AudioImportEnforcer 自动执行
4. **扩展新 ISkillEffect 类型**——直接在 `_Game/` 下新建类实现接口，SkillConfigSOEditor 自动发现
5. **新增 ComponentType 枚举值**——EntityConfigSOEditor 自动遍历，无需修改编辑器代码
