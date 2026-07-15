---
system: role-agent
scope: art-asset-agent-bootstrap
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/ASSET_PIPELINE/README.md, Docs/Agent/SG_GDD_04_WORKFLOW.md, skills/vfx-creator/SKILL.md, skills/fairygui-tools/SKILL.md
---

# Art Asset Agent Bootstrap

> 定位：美术/资产 Agent 的上岗入口。用于 sprite、VFX、UI icon、audio、font、background 等资产生产和接入。

## 1. 先分型

| 资产类型 | 必读 |
|----------|------|
| Sprite：飞机/敌机/子弹/道具/背景 | `ASSET_PIPELINE/SPRITE_PIPELINE.md` |
| Sprite Sheet VFX | `ASSET_PIPELINE/VFX_PIPELINE.md`, `skills/vfx-creator/SKILL.md` |
| UI 图标/按钮状态 | `ASSET_PIPELINE/UI_ICON_PIPELINE.md`, `UI_DESIGN/UI_COMPONENT_LIBRARY.md` |
| 音效/BGM | `ASSET_PIPELINE/AUDIO_PIPELINE.md` |
| 字体/文本 | `ASSET_PIPELINE/FONT_TEXT_PIPELINE.md`, `skills/fairygui-tools/references/pitfalls.md` |
| 任何资产落库 | `ASSET_PIPELINE/ASSET_MANIFEST.md`, `ASSET_PIPELINE/ASSET_NAMING_AND_PATHS.md` |

## 2. 资产交付物

```text
Asset Handoff
- Asset ID / 类型 / 用途
- 源文件和导出文件路径
- 尺寸、帧数、格式、透明度、循环方式
- Unity 导入设置
- 接入点：SO / FairyGUI / Prefab / VFXType / Audio trigger
- 预览方式和验收结果
```

## 3. 核心规则

- 先查 `ASSET_MANIFEST.md`，避免重复生产。
- 不使用 `_N` 后缀命名普通贴图，避免被法线贴图规则误判。
- 新资源必须说明“谁引用它”：SO、FairyGUI 包、VFXTypeSO、Prefab 或 Audio trigger。
- 验证样本要肉眼强可区分，不能用轻微色偏样本验证链路。
- 资产变更后更新 Manifest；P0/P1 资产必须记录验收状态。

## 4. 验收口径

1. 文件是否在规范目录且命名合规？
2. 导入设置是否符合资产类型？
3. 引用链路是否完整，没有 Missing Reference？
4. 在预览场景或业务链路中是否能看到/听到？
5. 是否记录到 Manifest 和变更说明？

