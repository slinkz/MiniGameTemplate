---
system: role-agent
scope: audio-pipeline
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/DESIGN/PLAYER_JOURNEY.md, Docs/Agent/WECHAT_INTEGRATION.md
---

# Audio Pipeline

> 定位：音效和 BGM 资产生产与接入。当前项目音频资料较少，本文件先建立最低可用规范。

## 类型

| 类型 | 格式建议 | 用途 |
|------|----------|------|
| SFX | wav 源文件，运行可压缩 | 射击、爆炸、拾取、按钮、胜负 |
| BGM | ogg loop | 选关、战斗 |
| UI SFX | wav | 点击、弹窗、解锁、星级 |

## 命名

```text
sfx_shoot_player_01.wav
sfx_enemy_explode_01.wav
sfx_pickup_repair_01.wav
sfx_ui_confirm_01.wav
bgm_battle_loop.ogg
```

## 流程

1. 从设计/UI 卡片确认触发事件。
2. 生成或制作源文件。
3. 统一响度，避免爆音。
4. 导入 `UnityProj/Assets/_Game/Audio/`。
5. 配置 AudioClip import setting。
6. 接入 Audio trigger 或后续 AudioConfig。
7. 验收：触发时机、重复播放、暂停/重试/退出。

## 验收

- 高频 SFX 不刺耳、不堆叠爆音。
- BGM loop 无明显断点。
- 暂停时策略明确：暂停或降低音量。
- 微信真机有声且延迟可接受。

