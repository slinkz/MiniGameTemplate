# FairyGUI SDK 集成

## 概述

FairyGUI 是本模板的 UI 框架。SDK 通过 git submodule + 目录连接（junction/symlink）方式集成。

## 集成方式

- SDK 源码位于 `ThirdParty/FairyGUI-unity/`（git submodule）
- 通过目录连接映射到 `Assets/FairyGUI/` 让 Unity 识别：
  - `Assets/FairyGUI/Scripts/` → SDK 运行时代码
  - `Assets/FairyGUI/Editor/` → SDK 编辑器扩展
  - `Assets/FairyGUI/Resources/` → SDK 内置资源（Shader 等）

## 首次设置

克隆项目后运行：

```bash
# Windows
Tools\setup_fairygui.bat

# macOS/Linux
chmod +x Tools/setup_fairygui.sh && Tools/setup_fairygui.sh
```

## 更新 SDK 版本

```bash
cd ThirdParty/FairyGUI-unity
git fetch --tags
git checkout v5.2.0   # 或其他版本标签
cd ../..
git add ThirdParty/FairyGUI-unity
git commit -m "Update FairyGUI SDK to v5.2.0"
```

## 当前版本

- SDK: v5.2.0 (2025-05-11)
- 仓库: https://github.com/fairygui/FairyGUI-unity

## 与框架的关系

- `_Framework/UISystem/` 中的 `UIBase`、`UIManager`、`UIPackageLoader` 等类直接依赖 FairyGUI API
- 所有 UI 面板通过 FairyGUI 编辑器设计，导出后放到 `_Game/FairyGUI/` 目录
- 参见 `_Framework/UISystem/MODULE_README.md` 了解 UI 框架的使用方式
