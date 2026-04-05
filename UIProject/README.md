# UIProject — FairyGUI 编辑器工程

## 概述

这是 FairyGUI 编辑器的工程目录，UI 设计师在此创建和编辑游戏 UI 界面。

## 目录结构

```
UIProject/
├── MiniGameTemplate.fairy   ← 用 FairyGUI 编辑器打开此文件
├── assets/                  ← UI 包资源（每个包一个子目录）
│   └── <PackageName>/       # 包含 package.xml + 图片 + 组件 XML
└── settings/                ← 工程设置
    ├── Adaptation.json      # 适配设置（设计分辨率 720×1280）
    ├── Common.json          # 通用设置（字体、颜色方案等）
    ├── CustomProperties.json # 自定义属性
    ├── i18n.json            # 多语言配置
    └── Publish.json         # 发布设置（输出路径、图集选项等）
```

## 发布/导出

**导出目标目录**：`../UnityProj/Assets/_Game/FairyGUI_Export/`

在 FairyGUI 编辑器中点击「发布」后，生成的 `_fui.bytes` 描述文件和图集纹理会自动输出到 Unity 工程中。

### 导出路径配置

发布路径在 `settings/Publish.json` 中配置：
```json
{
  "path": "../UnityProj/Assets/_Game/FairyGUI_Export"
}
```

如果你 fork 后移动了目录结构，需要同步更新这个路径。

## 设计分辨率

当前配置为 **720×1280**（竖屏小游戏常见分辨率），适配模式为 `ScaleWithScreenSize`。

修改方式：
1. FairyGUI 编辑器 → 项目设置 → 适配
2. 或直接编辑 `settings/Adaptation.json`

## 工作流

1. 用 FairyGUI 编辑器打开 `MiniGameTemplate.fairy`
2. 在 `assets/` 下创建新的 UI 包
3. 设计 UI 组件（按钮、面板、列表等）
4. 发布 → 导出到 Unity 工程
5. 在 Unity 中通过 `UIPackageLoader` 加载对应包

## Agent 操作指南

当 Agent 需要创建 UI 时：

1. **结构生成**：在 `assets/<PackageName>/` 下创建 `package.xml` 和组件 XML 文件
2. **白模模式**：使用 FairyGUI 的 Graph 组件（矩形/圆形 + 颜色填充）作为图片占位符
3. **发布不需要手动操作**：Agent 可以直接将导出格式的文件写入 `UnityProj/Assets/_Game/FairyGUI_Export/`
4. 组件闭环原则：引用的子组件必须同时生成，不能留悬空引用
