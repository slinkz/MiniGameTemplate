---
system: role-agent
scope: asset-naming-and-paths
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SYSTEMS/CONV/CONV_INDEX.md, Docs/Agent/SHOOTER_GAME/GDD/SG_GDD_04_WORKFLOW.md
---

# Asset Naming And Paths

> 定位：资产命名和目录规范。

## 命名

| 类型 | 格式 | 示例 |
|------|------|------|
| 游戏 sprite | `sg_<category>_<name>.png` | `sg_enemy_shooter.png` |
| 子弹 | `bullet_<name>_01.png` | `bullet_scatter_01.png` |
| 道具 | `pickup_<name>_01.png` | `pickup_repair_01.png` |
| UI 图标 | `ui_icon_<name>.png` | `ui_icon_buff_haste.png` |
| VFX 图集 | `vfx_<name>_<frames>x<cols>.png` | `vfx_explosion_16x4.png` |
| 音效 | `sfx_<event>_<variant>.wav` | `sfx_enemy_shoot_01.wav` |
| BGM | `bgm_<scene>_loop.ogg` | `bgm_battle_loop.ogg` |

## 禁止

- 普通贴图不使用 `_N` 后缀，避免被法线贴图规则误判。
- 不用空格、中文、特殊字符。
- 不用 `new`、`final`、`test` 作为长期文件名。

## 建议路径

```text
UnityProj/Assets/_Game/Textures/ShooterGame/
  Characters/
  Bullets/
  Pickups/
  Backgrounds/
  VFX/
  UIIcons/

UnityProj/Assets/_Game/Audio/
  SFX/
  BGM/

UIProject/assets/<PackageName>/
```

当前临时 sprite 仍在 `Textures/TempSprites/`，迁移前不要假装已经在正式目录。

