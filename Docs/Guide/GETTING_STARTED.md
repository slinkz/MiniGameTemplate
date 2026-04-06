# 环境搭建与首次运行

> **预计时间**：15 分钟 &nbsp;|&nbsp; **目标**：从 clone 仓库到在 Unity 编辑器中运行示例游戏

## 你需要准备的东西

在开始之前，确认你已安装以下工具：

| 工具 | 版本要求 | 下载地址 |
|------|----------|----------|
| Unity | **2022.3 LTS**（任何 2022.3.x 小版本均可） | [Unity 下载](https://unity.com/releases/editor/qa/lts-releases) |
| Git | 2.30+ | [Git 下载](https://git-scm.com/) |
| FairyGUI Editor | 最新版 | [FairyGUI 官网](https://www.fairygui.com/) |

**可选但推荐：**

| 工具 | 用途 |
|------|------|
| [微信开发者工具](https://developers.weixin.qq.com/miniprogram/dev/devtools/download.html) | 微信小游戏真机测试 |
| [.NET SDK 8.0+](https://dotnet.microsoft.com/download) | 运行 Luban 配置表生成工具 |
| VS Code / Rider | C# 代码编辑（Unity 自带的 Visual Studio 也行） |

## Step 1：克隆项目

```bash
git clone --recursive <仓库地址>
cd MiniGameTemplate
```

> ⚠️ **必须加 `--recursive`**。项目使用 Git submodule 引入 FairyGUI SDK。如果你忘了加，运行：
> ```bash
> git submodule update --init --recursive
> ```

## Step 2：设置 FairyGUI SDK

FairyGUI 的运行时代码通过 Git submodule 存放在 `UnityProj/ThirdParty/FairyGUI-unity/`，需要建立目录链接让 Unity 识别：

**Windows（在项目根目录执行）：**
```bash
cd UnityProj
Tools\setup_fairygui.bat
```

**macOS / Linux：**
```bash
cd UnityProj
bash Tools/setup_fairygui.sh
```

脚本做了两件事：
1. 初始化 Git submodule（如果尚未初始化）
2. 创建 `Assets/FairyGUI` → `ThirdParty/FairyGUI-unity/Assets` 的目录 Junction（Windows）或符号链接（macOS/Linux），让 Unity 能识别 FairyGUI 源码

执行成功后你应该看到：
```
Done! FairyGUI SDK is ready.
```

## Step 3：打开 Unity 工程

1. 启动 Unity Hub
2. 点击 **Open** → 选择 `MiniGameTemplate/UnityProj/` 目录
3. Unity 版本选择 **2022.3.x LTS**
4. 等待 Unity 导入所有资源（首次约 2-5 分钟）

> 💡 如果 Unity 版本不匹配，Hub 会提示你安装对应版本。建议始终使用 2022.3 LTS 系列。

### 首次打开可能看到的编译错误

| 错误 | 原因 | 解决方案 |
|------|------|----------|
| `The type or namespace name 'FairyGUI' could not be found` | FairyGUI 目录链接未建立 | 回到 Step 2 执行 setup 脚本 |
| `The type or namespace name 'YooAsset' could not be found` | YooAsset 本地包路径无效 | 确认 `ThirdParty/YooAsset/` 目录存在且含 `package.json` |

## Step 4：运行示例游戏

1. 在 Project 面板中找到 `Assets/_Framework/GameLifecycle/Scenes/Boot.unity`
2. **双击打开** Boot 场景
3. 按下 **Play ▶**

你应该看到控制台输出类似以下日志：

```
[Bootstrapper] Starting MiniGameTemplate v0.1.0
[Bootstrapper] AssetService initialized.
[Bootstrapper] ConfigManager initialized.
[Bootstrapper] TimerService initialized.
[Bootstrapper] AudioManager ready.
[Bootstrapper] UIManager initialized.
[Bootstrapper] PoolManager initialized.
[Bootstrapper] All systems initialized.
```

> ⚠️ **必须从 Boot 场景启动**。Boot 场景包含 `GameBootstrapper`，它负责按正确顺序初始化所有框架系统。直接打开游戏场景运行会导致空引用错误。

## Step 5：了解项目目录结构

打开成功后，花一分钟熟悉 Assets 目录结构：

```
Assets/
├── FairyGUI/ → Junction     ← ThirdParty/FairyGUI-unity/Assets/（setup 脚本自动创建）
├── _Framework/              ← 框架代码（一般不改）
│   ├── AssetSystem/         ← YooAsset 资源管理封装
│   ├── AudioSystem/         ← 音频管理（BGM + SFX）
│   ├── DataSystem/          ← SO 变量 + 运行时集合 + 存储 + 配置表
│   ├── DebugTools/          ← 帧率显示、运行时调试
│   ├── Editor/              ← 编辑器扩展工具
│   ├── EventSystem/         ← SO 事件通道
│   ├── FSM/                 ← 有限状态机
│   ├── GameLifecycle/       ← 启动流程 + 场景管理
│   ├── ObjectPool/          ← 对象池
│   ├── Timer/               ← 计时器服务
│   ├── UISystem/            ← FairyGUI 集成
│   ├── Utils/               ← 通用工具（Singleton、CoroutineRunner 等）
│   └── WeChatBridge/        ← 微信 SDK 桥接层
├── _Example/                ← 示例游戏（点击计数器）
├── _Game/                   ← 你的游戏代码放这里
│   ├── FairyGUI_Export/     ← FairyGUI 导出的 UI 资源
│   ├── Scenes/              ← 游戏场景
│   └── ScriptableObjects/   ← SO 资产文件
└── ScriptTemplates/         ← C# 脚本模板
```

每个框架模块目录下都有一个 `MODULE_README.md`，想了解某个模块就读它。

## Step 6（可选）：生成 Luban 配置表

如果你需要使用配置表系统：

1. 确保已安装 [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
2. 运行生成脚本：

**Windows：**
```bash
cd UnityProj
Tools\gen_config.bat
```

**macOS / Linux：**
```bash
cd UnityProj
bash Tools/gen_config.sh
```

生成产物说明：

| 输出 | 路径 | 用途 |
|------|------|------|
| C# 代码 | `Assets/_Framework/DataSystem/Scripts/Config/Generated/` | ByteBuf 反序列化代码 |
| 二进制数据 | `Assets/_Game/ConfigData/*.bytes` | YooAsset 运行时加载 |
| JSON 数据 | `Assets/_Framework/Editor/ConfigPreview/*.json` | 编辑器预览（不打包） |

> 💡 **AI 开发者提示**：项目中提供了 `luban-config` Skill（位于 `.codebuddy/skills/luban-config/`），可自动化新增/修改/删除配置表的完整流程。详见下方 [AI Skills 章节](#ai-skills-工具链)。

## Step 7（可选）：打开 FairyGUI 工程

1. 启动 FairyGUI Editor
2. 打开 `MiniGameTemplate/UIProject/MiniGameTemplate.fairy`
3. UI 编辑器的设计分辨率为 **720×1280**（竖屏小游戏）
4. 发布时资源自动输出到 `UnityProj/Assets/_Game/FairyGUI_Export/`

## 接下来做什么

环境搭建完成，你有几个方向可以深入：

- 📖 **[示例游戏代码解读](EXAMPLE_WALKTHROUGH.md)** — 理解框架各模块是如何配合工作的
- 🏗 **[架构设计解读](ARCHITECTURE_OVERVIEW.md)** — 理解 SO 驱动架构的设计理念
- 📚 **[框架模块使用手册](FRAMEWORK_MODULES.md)** — 开始在 `_Game/` 目录下开发你的游戏

## 常见问题

### Q: Unity 编译很慢怎么办？

项目使用了 Assembly Definition（`.asmdef`）分离编译域。框架代码、游戏代码、示例代码、编辑器代码各自独立编译。修改 `_Game/` 下的代码时，不会重新编译 `_Framework/`。

### Q: 我需要修改框架代码吗？

一般不需要。框架设计为通过 SO 资产（变量、事件、配置）与游戏代码交互。如果你发现必须修改框架，可能说明缺少了某个扩展点——先看看 `MODULE_README.md` 有没有现成的解决方案。

### Q: 可以删掉不需要的模块吗？

可以，但需要注意模块依赖关系。详见 [架构设计解读](ARCHITECTURE_OVERVIEW.md) 中的模块依赖图。删除模块前运行 `Tools → MiniGame Template → Validate → Architecture Check` 检查是否有依赖断裂。

---

## AI Skills 工具链

本模板内置了供 AI Agent（如 CodeBuddy / WorkBuddy）使用的 **Skills**，可以大幅提升 AI 协作开发效率。Skills 存放在 `.codebuddy/skills/` 目录（CodeBuddy 官方标准路径），会随 Git 仓库一起分发。

### 当前可用 Skills

| Skill | 路径 | 功能 |
|-------|------|------|
| `luban-config` | `.codebuddy/skills/luban-config/` | Luban 配置表自动化：新增/修改/删除表、生成 xlsx、同步 TablesExtension.cs |
| `fairygui-tools` | `.codebuddy/skills/fairygui-tools/` | FairyGUI UI 开发自动化：从效果图/自然语言生成白模 XML、解析 FairyGUI 工程结构、UI 结构分析 |

### 如何使用

1. **你不需要手动做任何事**——当你和 AI 助手协作时，AI 会自动识别并加载相关 Skill
2. 例如，当你对 AI 说"新增一张 TbShop 配置表"，AI 会自动加载 `luban-config` Skill，按照标准流程完成：
   - 在 `DataTables/Defs/tables.xml` 中添加表定义
   - 自动创建 xlsx 数据文件（带正确的表头格式）
   - 同步 `TablesExtension.cs` 扩展代码
   - 运行 `gen_config.bat` 验证生成结果
3. 当你对 AI 说"帮我做一个设置面板的 UI"，AI 会自动加载 `fairygui-tools` Skill，按照标准流程完成：
   - 分析 UI 需求，生成白模示意图
   - 生成符合 FairyGUI 规范的 XML 文件（含 package.xml 和组件 XML）
   - 遵循"组件闭环原则"确保所有子组件引用完整
4. Skill 内含踩坑记录和最佳实践，确保 AI 不会犯已知错误

### 想了解更多？

- 查看 `.codebuddy/skills/luban-config/SKILL.md` 了解 Luban 配置表 Skill 的完整 SOP 和技术细节
- 查看 `.codebuddy/skills/fairygui-tools/SKILL.md` 了解 FairyGUI UI 开发 Skill 的工作流和白模规范
- Skill 的格式遵循 CodeBuddy Skill 标准，你也可以为项目创建自定义 Skill
