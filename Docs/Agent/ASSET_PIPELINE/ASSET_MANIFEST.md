---
system: role-agent
scope: asset-manifest
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/ASSET_PIPELINE/ASSET_NAMING_AND_PATHS.md
---

# Asset Manifest

> 定位：P0/P1 资产登记表。不是列出每个临时文件，而是追踪关键可交付资产。

## 状态

| 状态 | 含义 |
|------|------|
| planned | 已需要，未制作 |
| draft | 有占位或生成样本 |
| imported | 已导入 Unity/FairyGUI |
| wired | 已接入 SO/Prefab/UI/VFX/Audio |
| accepted | 已通过业务验收 |
| retired | 不再使用，保留历史 |

## 当前关键资产

| Asset ID | 类型 | 用途 | 路径/接入点 | 状态 |
|----------|------|------|-------------|------|
| SG_Player | sprite | 玩家飞机 | `UnityProj/Assets/_Game/Textures/TempSprites/SG_Player.png` | imported |
| SG_Enemy_Normal | sprite | 普通敌 | `UnityProj/Assets/_Game/Textures/TempSprites/SG_Enemy_Normal.png` | imported |
| SG_Enemy_Fast | sprite | 快速敌 | `UnityProj/Assets/_Game/Textures/TempSprites/SG_Enemy_Fast.png` | imported |
| SG_Enemy_Shooter | sprite | 射手敌 | `UnityProj/Assets/_Game/Textures/TempSprites/SG_Enemy_Shooter.png` | imported |
| SG_Enemy_Scatter | sprite | 散射敌 | `UnityProj/Assets/_Game/Textures/TempSprites/SG_Enemy_Scatter.png` | imported |
| SG_Enemy_Elite | sprite | 精英敌 | `UnityProj/Assets/_Game/Textures/TempSprites/SG_Enemy_Elite.png` | imported |
| SG_Base | sprite | 基地/防线 | `UnityProj/Assets/_Game/Textures/TempSprites/SG_Base.png` | imported |
| BattleHUD | ui | 战斗 HUD | `UIProject/assets/SG_Battle/BattleHUD.xml` | wired |
| LevelSelectScreen | ui | 选关 | `UIProject/assets/SG_LevelSelect/LevelSelectScreen.xml` | wired |
| SortieBottomSheet | ui | 出战准备 | `UIProject/assets/SG_Sortie/SortieBottomSheet.xml` | wired |

## 新增记录模板

```text
| Asset ID | 类型 | 用途 | 源文件 | 导出文件 | 接入点 | 状态 | 验收 |
```

