---
system: role-agent
scope: asset-pipeline-index
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/ART_ASSET_AGENT_BOOTSTRAP.md, Docs/Agent/SG_GDD_04_WORKFLOW.md
---

# Asset Pipeline Index

> 定位：资产生产、导入、接入和验收的总入口。

## 路由

| 资产类型 | 文档 |
|----------|------|
| 资产清单 | `ASSET_MANIFEST.md` |
| 命名和路径 | `ASSET_NAMING_AND_PATHS.md` |
| 飞机/敌机/子弹/道具/背景 | `SPRITE_PIPELINE.md` |
| Sprite Sheet VFX | `VFX_PIPELINE.md` |
| UI 图标/按钮状态 | `UI_ICON_PIPELINE.md` |
| 音效/BGM | `AUDIO_PIPELINE.md` |
| 字体/文本 | `FONT_TEXT_PIPELINE.md` |
| Unity 导入设置 | `IMPORT_SETTINGS.md` |
| 预览和验收 | `PREVIEW_AND_ACCEPTANCE.md` |
| AI 生成提示词 | `GENERATIVE_ASSET_PROMPTS.md` |

## 资产任务闭环

```text
查 Manifest -> 生产/生成 -> 放入规范目录 -> 设置导入 -> 接入 SO/FairyGUI/VFX/Audio -> 预览验收 -> 更新 Manifest
```

