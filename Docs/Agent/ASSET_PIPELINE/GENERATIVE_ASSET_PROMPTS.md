---
system: role-agent
scope: generative-asset-prompts
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: skills/vfx-creator/references/prompt-templates.md, Docs/Agent/ASSET_PIPELINE/SPRITE_PIPELINE.md
---

# Generative Asset Prompts

> 定位：AI 生成资产的提示词模板。生成后仍必须按资产管线导入和验收。

## Sprite Prompt

```text
Create a clean flat-vector vertical shoot-em-up game sprite, transparent background,
front-facing {asset}, high contrast silhouette, readable at {size}px,
modern sci-fi style, no text, no watermark, centered, PNG.
```

## UI Icon Prompt

```text
Create a compact mobile game UI icon for {skill_or_buff}, transparent background,
simple flat vector, strong silhouette, readable at 32px, limited color palette,
no text, no emoji, no watermark.
```

## VFX Prompt

```text
Create a {frames}-frame sprite sheet for {effect}, transparent background,
{cols} columns, consistent center point, high contrast, strong shape change per frame,
mobile game style, no text, no watermark.
```

## 反例

- 只说“做一个好看的爆炸”。
- 没写透明背景、帧数、列数、尺寸。
- 没写可读性和高对比。
- 生成后直接接入业务，不先做预览。

