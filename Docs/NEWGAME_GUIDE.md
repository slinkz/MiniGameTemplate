# 新游戏创建指南

基于 MiniGameTemplate 新建一个小游戏项目的完整流程。

## Step 1: 复制项目

```bash
# 方法 A: Fork（推荐）
# 在 Git 平台上 Fork MiniGameTemplate 仓库

# 方法 B: 本地复制
cp -r MiniGameTemplate/ MyNewGame/
cd MyNewGame
rm -rf .git
git init
```

## Step 2: 基础配置

### 2.1 修改项目名称
1. 打开 `UnityProj/Assets/_Framework/GameLifecycle/Presets/DefaultGameConfig.asset`
2. 修改 `Game Name` 和 `Version`

### 2.2 修改 Bundle ID
1. 用 Unity 2022 LTS 打开 `UnityProj/` 目录
2. `Edit → Project Settings → Player`
3. 修改 `Company Name`、`Product Name`、`Bundle Identifier`

### 2.3 修改命名空间（可选）
如果你想把 `MiniGameTemplate.Game` 改为项目专用命名空间（如 `MyNewGame`），在所有 `UnityProj/Assets/_Game/` 下的脚本中替换。

## Step 3: 清理示例

```
# 如果不需要示例游戏，删除 _Example 目录
UnityProj/Assets/_Example/  ← 删除整个目录
```

## Step 4: 创建游戏场景

1. 在 `UnityProj/Assets/_Game/Scenes/` 下创建你的游戏场景（如 `GameScene.unity`）
2. 创建 SceneDefinition SO：
   - 右键 `UnityProj/Assets/_Game/ScriptableObjects/` → Create → MiniGameTemplate → Core → Scene Definition
   - 填入场景名称
3. 将该 SceneDefinition 拖入 `DefaultGameConfig` 的 `Initial Scene` 字段
4. 在 Build Settings 中添加场景（Boot 排第一，GameScene 排第二）

## Step 5: 创建游戏 UI

1. 用 FairyGUI 编辑器打开 `UIProject/` 目录中的 `.fairy` 工程文件
2. 创建你的 UI 包
3. FairyGUI 导出目标已配置为 `UnityProj/Assets/_Game/FairyGUI_Export/`
4. 更新 `UIConstants.cs` 中的包名和组件名常量
5. 创建 Panel 类继承 `UIBase`：

```csharp
using MiniGameTemplate.UI;

public class MainMenuPanel : UIBase
{
    protected override string PackageName => UIConstants.PKG_MAIN_MENU;
    protected override string ComponentName => UIConstants.COMP_MAIN_PANEL;

    protected override void OnInit()
    {
        // 绑定 UI 元素
    }

    protected override void OnOpen(object data)
    {
        // 显示时的逻辑
    }
}
```

## Step 6: 创建游戏 SO 资产

根据游戏需求创建 ScriptableObject 资产：

```
通过菜单: Tools → MiniGame Template → SO Creation Wizard
或右键: Create → MiniGameTemplate → Variables/Events/...
```

常见 SO 资产：
- `PlayerScore` (IntVariable) — 玩家分数
- `PlayerHighScore` (IntVariable) — 最高分
- `OnGameStart` (GameEvent) — 游戏开始事件
- `OnGameOver` (GameEvent) — 游戏结束事件

## Step 7: 配置表（Luban）

如果游戏需要配置表：

1. 在 `UnityProj/DataTables/Defs/` 中定义表结构
2. 在 `UnityProj/DataTables/Defs/__tables__.xml` 中注册
3. 在 `UnityProj/DataTables/Datas/` 中编写数据（JSON 格式）
4. 运行 `UnityProj/Tools/gen_config.bat` 生成代码
5. 在 `ConfigManager` 中调用生成的 Tables 类

## Step 8: 微信 SDK（如需要）

详见 [WECHAT_INTEGRATION.md](WECHAT_INTEGRATION.md)

## Step 9: 验证

1. 运行 `Tools → MiniGame Template → Validate Architecture` 检查架构合规
2. 从 Boot 场景启动，确认初始化流程正常
3. 测试游戏核心循环

## 检查清单

- [ ] 修改了 GameConfig（名称、版本）
- [ ] 修改了 Player Settings（Bundle ID）
- [ ] 创建了游戏场景并配置为 Initial Scene
- [ ] Boot 场景在 Build Settings 中排第一
- [ ] 清理了 _Example（如不需要）
- [ ] 创建了需要的 SO 变量和事件
- [ ] FairyGUI 工程导出路径正确指向 UnityProj
- [ ] 架构验证通过
