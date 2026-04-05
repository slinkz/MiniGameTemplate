# Scenes 目录

存放游戏场景文件。

## 约定
- `Boot.unity` — 启动场景（仅包含 GameBootstrapper）
- `Main.unity` — 游戏主场景
- 其他场景按功能命名

## 场景引用
- 通过 SceneDefinition SO 引用场景
- 通过 SceneLoader 加载场景
- 确保所有需要的场景已添加到 Build Settings
