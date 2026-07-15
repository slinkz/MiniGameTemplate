---
name: asset-pipeline
description: "MiniGameTemplate 美术资产生产与接入工作流。用于新增、生成、导入、命名、登记、接线和验收 sprite、敌机/子弹/道具图、UI 图标、Sprite Sheet VFX、音效、BGM、字体、背景等资产；当任务需要 Asset Handoff、Manifest 更新、Unity 导入设置、SO/FairyGUI/VFX/Audio 接入或预览验收时触发。"
---

# Asset Pipeline

## 使用流程

1. 先读 `Docs/Agent/ART_ASSET_AGENT_BOOTSTRAP.md`。
2. 查 `Docs/Agent/ASSET_PIPELINE/ASSET_MANIFEST.md`，避免重复生产。
3. 按类型读 `Docs/Agent/ASSET_PIPELINE/README.md` 路由的专题文档。
4. VFX 任务继续读 `skills/vfx-creator/SKILL.md`。
5. UI 图标/FairyGUI 任务继续读 `skills/fairygui-tools/SKILL.md`。
6. 输出 Asset Handoff，并更新 Manifest。

## 任务路由

| 任务 | 必读 |
|------|------|
| 命名/路径 | `ASSET_PIPELINE/ASSET_NAMING_AND_PATHS.md` |
| sprite | `ASSET_PIPELINE/SPRITE_PIPELINE.md` |
| VFX | `ASSET_PIPELINE/VFX_PIPELINE.md`, `skills/vfx-creator/SKILL.md` |
| UI icon | `ASSET_PIPELINE/UI_ICON_PIPELINE.md` |
| audio | `ASSET_PIPELINE/AUDIO_PIPELINE.md` |
| font/text | `ASSET_PIPELINE/FONT_TEXT_PIPELINE.md` |
| 导入设置 | `ASSET_PIPELINE/IMPORT_SETTINGS.md` |
| 验收 | `ASSET_PIPELINE/PREVIEW_AND_ACCEPTANCE.md` |

## Asset Handoff 模板

```text
Asset Handoff
- Asset ID / 类型 / 用途：
- 源文件 / 导出文件：
- 尺寸、帧数、格式、透明度：
- Unity/FairyGUI 导入设置：
- 接入点：SO / FairyGUI / Prefab / VFXType / Audio trigger：
- 预览方式：
- 验收结果：
- Manifest 更新：
```

## 必须检查

- 命名和路径是否合规，普通贴图不得使用 `_N` 后缀。
- 是否有明确接入点。
- 是否无 Missing Reference。
- 是否在预览场景或业务链路中验证。
- P0/P1 资产是否登记到 Manifest。

