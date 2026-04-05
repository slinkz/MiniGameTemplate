# Art 目录

存放游戏美术资源。

## 目录约定
- `Textures/` — 纹理、贴图
- `Materials/` — 材质
- `Models/` — 3D模型（如有）
- `Animations/` — 动画资源

## 导入规范
- 纹理最大 1024px（AssetPostprocessor 会自动限制）
- 压缩格式 ASTC 6x6（WebGL/移动端）
- 禁用 Read/Write（节省内存）
- UI 纹理禁用 Mipmap
