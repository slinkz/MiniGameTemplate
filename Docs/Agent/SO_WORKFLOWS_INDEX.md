---
system: so-config
scope: workflow-index
last_verified: 2026-05-21
related_code: Assets/_Framework/**/*SO.cs, Assets/_Framework/**/Scripts/Config/*.cs
---

# SO 配置流程指南 — 索引

> 最后更新：2026-05-21 | 36 个 SO 类型 × 6 个系统分组

## 子文件索引

| 文件 | 内容 | SO 数量 |
|------|------|---------|
| [SO_WORKFLOWS_01_CORE](SO_WORKFLOWS_01_CORE.md) | 核心配置（GameConfig/SceneDefinition/AssetConfig） | 3 |
| [SO_WORKFLOWS_02_ENTITY](SO_WORKFLOWS_02_ENTITY.md) | Entity 战斗系统 SO | 8 |
| [SO_WORKFLOWS_03_DANMAKU](SO_WORKFLOWS_03_DANMAKU.md) | 弹幕系统 SO | 12 |
| [SO_WORKFLOWS_04_VFX_RENDER](SO_WORKFLOWS_04_VFX_RENDER.md) | VFX + 渲染 SO | 5 |
| [SO_WORKFLOWS_05_INFRA](SO_WORKFLOWS_05_INFRA.md) | 基础设施（Variables/Events/Pool/FSM/Audio） | 8 |

## 端到端工作流速查

| 我要做什么 | 需要创建的 SO | 参考子文件 |
|-----------|-------------|-----------|
| 新建一种敌人 | `EntityConfigSO` + `AIBehaviorSO`（可选） | 02_ENTITY §EntityConfigSO |
| 新建一个技能 | `SkillConfigSO`（含 ISkillEffect[] 配置） | 02_ENTITY §SkillConfigSO |
| 新建一个 Buff | `BuffConfigSO` | 02_ENTITY §BuffConfigSO |
| 新建一个 DOT | `DotConfigSO` | 02_ENTITY §DotConfigSO |
| 新建一个被动 | `PassiveAbilitySO` | 02_ENTITY §PassiveAbilitySO |
| 新建弹幕花样 | `BulletTypeSO` + `BulletPatternSO` | 03_DANMAKU §BulletType/Pattern |
| 编排 Boss 弹幕 | `PatternGroupSO` + `SpawnerProfileSO` | 03_DANMAKU §PatternGroup/Spawner |
| 配置难度梯度 | `DifficultyProfileSO` | 03_DANMAKU §DifficultyProfile |
| 新建 VFX 特效 | `VFXTypeSO` | 04_VFX §VFXTypeSO |
| 配置关卡波次 | `EntitySpawnWaveSO` | 02_ENTITY §EntitySpawnWaveSO |
| 新建场景定义 | `SceneDefinition` | 01_CORE §SceneDefinition |
| 配置 CDN/资源 | `AssetConfig` | 01_CORE §AssetConfig |
| 新建音效 | `AudioClipSO` → `AudioLibrary` | 05_INFRA §Audio |
| 新建运行时变量 | `FloatVariable`/`IntVariable`/... | 05_INFRA §Variables |
| 配置对象池 | `PoolDefinition` | 05_INFRA §Pool |

## Agent 通用创建模式（MCP）

```csharp
// 通过 unity_execute_code 创建任意 SO
var so = ScriptableObject.CreateInstance<XxxSO>();
so.Field1 = value1;
so.Field2 = value2;
AssetDatabase.CreateAsset(so, "Assets/_Game/Configs/XXX/NewAsset.asset");
AssetDatabase.SaveAssets();
```

**也可用 SOCreationWizard**：`Tools → MiniGame → SO Creation Wizard`（支持模板选择 + 批量创建）。

## 实例目录约定

| 系统 | 实例存放路径 |
|------|------------|
| Core | `Assets/_Game/Configs/Core/` |
| Entity | `Assets/_Game/Configs/Entity/` |
| AI | `Assets/_Game/Configs/AI/` |
| Skill | `Assets/_Game/Configs/_Template/Skill/` |
| Buff | `Assets/_Game/Configs/_Template/Buff/` |
| DOT | `Assets/_Game/Configs/ShooterGame/Dots/` |
| Passive | `Assets/_Game/Configs/ShooterGame/Passives/` |
| SpawnWave | `Assets/_Game/Configs/SpawnWave/` |
| Danmaku | `Assets/_Game/Configs/Danmaku/` |
| VFX | `Assets/_Game/Configs/VFX/` |
| Rendering | `Assets/_Game/Configs/Rendering/` |
| Audio | `Assets/_Game/Configs/Audio/` |
| Variables | `Assets/_Game/Configs/Variables/` |
| Events | `Assets/_Game/Configs/Events/` |
| Pool | `Assets/_Game/Configs/Pool/` |
| FSM | `Assets/_Game/Configs/FSM/` |

## 命名约定

- **模板资产**：`Template_<Type>_<Name>.asset`（如 `Template_Slime_Basic.asset`）
- **正式资产**：`<Type>_<Name>.asset`（如 `Slime_Basic.asset`）
- SO 类名后缀：`SO`（EntityConfigSO）或 `Config`/`Definition`（GameConfig/PoolDefinition）
