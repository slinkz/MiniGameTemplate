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
5. **按强制分层规则创建面板代码（禁止单文件混写）**：
   - `XXXPanel.FUI.cs`：仅放 `PackageName` / `ComponentName`（字符串字面量）+ UI 绑定与初始化（可被 FairyGUI 重新导出覆盖）
   - `XXXPanel.cs`：放其余 override（如 `SortOrder`、`CloseOnClickOutside`）+ 业务逻辑（手写，禁止放导出绑定代码）


```csharp
// MainMenuPanel.FUI.cs
using FairyGUI;
using MiniGameTemplate.UI;

public partial class MainMenuPanel : UIBase
{
    protected override string PackageName => "MainMenu";
    protected override string ComponentName => "MainMenuPanel";

    private GButton _btnStart;

    protected override void OnInit()
    {
        base.OnInit();
        _btnStart = ContentPane.GetChild("btnStart") as GButton;
        AddEvents();
    }
}

// MainMenuPanel.cs
public partial class MainMenuPanel
{
    protected override int SortOrder => UIConstants.LAYER_NORMAL;

    protected void AddEvents()
    {
        if (_btnStart != null) _btnStart.onClick.Add(OnStartClicked);
    }

    protected override void OnOpen(object data)
    {
        base.OnOpen(data);
        // 显示时的业务逻辑
    }

    private void OnStartClicked() { }
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

## Step 7: 配置表（Luban v4.6.0）

如果游戏需要配置表，先加载 `luban-config` Skill，然后按 Skill 中的 SOP 操作：

1. 在 `UnityProj/DataTables/Defs/tables.xml` 中定义 Bean 和 Table
2. 用 `create_xlsx.py` 脚本创建 xlsx 数据文件，或手动创建（`##var`/`##type`/`##` 三行表头 + 数据行）
3. 用 `update_tables_extension.py` 脚本自动更新 `TablesExtension.cs`
4. 运行 `UnityProj/Tools/gen_config.bat` 生成代码和数据
5. 通过 `ConfigManager.Tables.TbXxx` 访问生成的表数据

> 💡 详细格式规范、踩坑记录见 `.codebuddy/skills/luban-config/` Skill 文档。

## Step 8: 微信 SDK（如需要）

详见 [WECHAT_INTEGRATION.md](WECHAT_INTEGRATION.md)

## Step 9: 验证

1. 运行 `Tools → MiniGame Template → Validate → Architecture Check` 检查架构合规
2. 运行 `Tools → MiniGame Template → Validate → Asset Audit` 检查资源预算
3. 从 Boot 场景启动，确认初始化流程正常
4. 测试游戏核心循环

## Step 10: 微信小游戏 PlayerSettings 配置

1. `Edit → Project Settings → Player → WebGL`：
   - **Color Space**: Gamma（微信小游戏推荐）
   - **Memory Size**: 256 MB
   - **Compression Format**: Release 用 Brotli，Dev 用 Disabled
   - **Exception Support**: Release 用 `Explicitly Thrown Exceptions Only`
   - **Linker Target**: Wasm
   - **Strip Engine Code**: 启用
   - **Managed Stripping Level**: Medium
2. 或直接使用 `Tools → MiniGame Template → Build → Build WebGL (Release)` — 会自动配置以上参数

## Step 11: YooAsset 包规则配置

1. 打开 `UnityProj/Assets/_Game/ScriptableObjects/` 中的 `AssetConfig` SO
2. 配置 `Default Package Name`（默认 "DefaultPackage"）
3. **编辑器开发**：Play Mode 选 `EditorSimulate`（无需构建 Bundle）
4. **真机测试**：Play Mode 选 `Offline`（需先构建 Bundle 到 StreamingAssets）
5. **在线更新**：Play Mode 选 `Host`，填入 CDN 服务器地址
6. YooAsset Bundle 构建：通过 `YooAsset → AssetBundle Builder` 窗口操作

## Step 12: UI 运行时源码链接设置（FairyGUI + 可选 Spine）

clone 新项目后先初始化子模块，再建立目录链接：

```bash
# 在仓库根目录执行
git submodule update --init --recursive

# FairyGUI（必做）
cd UnityProj
Tools\setup_fairygui.bat
# 或 bash Tools/setup_fairygui.sh

# Spine（可选，仅当项目需要 FairyGUI 显示 Spine）
Tools\setup_spine.bat
# 或 bash Tools/setup_spine.sh
```

如果启用 Spine，需要在 Unity 菜单执行：
- `Tools -> MiniGame Template -> Integrations -> Spine -> Enable Spine (Current Target)`

这会启用 `FAIRYGUI_SPINE`（以及模板级 `ENABLE_SPINE`）宏；未启用时不会编译/加载 Spine。

## Step 13: Luban 配置表新增表流程

> 💡 加载 `luban-config` Skill 后按其 SOP 操作，以下是快速参考：

1. 在 `DataTables/Defs/tables.xml` 中新增 `<bean>` 和 `<table>` 定义
2. 用 `create_xlsx.py` 创建 xlsx：`python .codebuddy/skills/luban-config/scripts/create_xlsx.py -o DataTables/Datas/xxx.xlsx -s TbXxx -f "id:int:ID,..."`
3. 用 `update_tables_extension.py` 自动同步表名：`python .codebuddy/skills/luban-config/scripts/update_tables_extension.py --project-root UnityProj`
4. 运行 `Tools/gen_config.bat`（Windows）或 `gen_config.sh`（macOS/Linux）
5. 生成输出：`_Game/ConfigData/*.bytes` + `Editor/ConfigPreview/*.json`
6. 通过 `ConfigManager.Tables.TbXxx` 访问（需在 `ConfigManager.InitializeAsync()` 完成后）

## Step 14: 构建与发布

```bash
# 方式一：Unity 菜单一键构建
Tools → MiniGame Template → Build → Build WebGL (Release)

# 方式二：打开构建输出目录
Tools → MiniGame Template → Build → Open Build Folder
```

构建完成后：
1. 使用微信小游戏 Unity 转换插件将 WebGL 输出转为微信小游戏格式
2. 在微信开发者工具中打开转换后的项目
3. 真机预览测试

## 检查清单

- [ ] 修改了 GameConfig（名称、版本）
- [ ] 修改了 Player Settings（Bundle ID、WebGL 配置）
- [ ] 初始化了 FairyGUI git submodule 并执行 setup 脚本
- [ ] （可选）若项目使用 Spine：执行 setup_spine 脚本并启用 FAIRYGUI_SPINE
- [ ] 创建了游戏场景并配置为 Initial Scene
- [ ] Boot 场景在 Build Settings 中排第一
- [ ] 清理了 _Example（如不需要）
- [ ] 创建了需要的 SO 变量和事件
- [ ] FairyGUI 工程导出路径正确指向 UnityProj
- [ ] YooAsset AssetConfig 已配置（Play Mode + Package Name）
- [ ] 配置表已生成并验证
- [ ] 架构验证通过
- [ ] 资源审计通过
- [ ] WebGL 构建成功
