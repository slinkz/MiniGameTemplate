# Audio 目录

存放游戏音频资源（BGM、SFX）。

## 目录约定
- `BGM/` — 背景音乐
- `SFX/` — 音效

## 导入规范
- WebGL 平台使用 Vorbis 压缩，质量 50%
- 短音效（< 3 秒）强制单声道
- 大 WAV 文件建议转为 OGG 格式
- 使用 AudioClipSO / AudioLibrary SO 管理引用
