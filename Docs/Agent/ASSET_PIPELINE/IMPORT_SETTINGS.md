---
system: role-agent
scope: import-settings
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/ASSET_PIPELINE/SPRITE_PIPELINE.md, Docs/Agent/ASSET_PIPELINE/AUDIO_PIPELINE.md
---

# Import Settings

> 定位：Unity 导入设置检查表。

## Texture / Sprite

| 项 | 建议 |
|----|------|
| Texture Type | Sprite |
| Alpha | 保留透明 |
| PPU | 跟项目基准一致 |
| Max Size | 不超过实际需求 |
| Compression | 微信真机检查质量 |
| Read/Write | 默认关闭，除非运行时需要 |
| Mipmap | UI/2D sprite 默认关闭 |

## VFX Sheet

- 按帧网格切分。
- 材质/Blend 明确。
- 循环和一次性播放分开记录。

## Audio

| 类型 | 建议 |
|------|------|
| 短 SFX | Decompress on Load 或合适压缩，低延迟优先 |
| BGM | Streaming/Compressed，循环测试 |
| 高频音效 | 控制并发和音量 |

## 验收

- 没有 Missing Reference。
- 压缩后视觉/听觉质量可接受。
- 真机表现与 Editor 不冲突。

