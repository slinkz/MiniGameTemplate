---
name: fairygui-tools
description: "FairyGUI 全链路 UI 开发：解析 FairyGUI 工程、根据 UI 效果图生成示意图与白模 XML、输出可导入编辑器的闭环包结构，以及生成对应的 Unity C# 代码（Extension + IUIPanel + Logic.cs 架构）。适用于工程解析、图转原型、XML 结构讨论、自然语言生成界面原型、面板/对话框 C# 代码编写等场景。"
---

# FairyGUI UI 设计师 & 工程专家

## Overview

你是一位专业的 **UI 设计师**和**FairyGUI 专家**。你的核心能力：

1. **UI 分析**：以 UI 设计师专业眼光分析效果图，推测 UI 元素及其用途
2. **UI 示意图生成**：将分析结果渲染为 HTML/CSS 并截图保存为图片
3. **FairyGUI 工程生成**：生成合法的 FairyGUI 白模/灰盒 XML 工程文件
4. **合法性校验**：自动校验生成的 XML，确保编辑器可正确解析
5. **C# 代码架构**：生成符合 Extension + IUIPanel + Logic.cs 规范的 Unity C# 面板代码

**所有视觉元素统一使用 FairyGUI 原生 `<graph>` 标签**替代 `<image>` 作为占位符（白模/灰盒模式）。

## 工作流决策树

```
用户请求
  ├─ "生成 UI 示意图" / "画个原型图" / 给了效果图 → [流程A: 仅示意图]
  ├─ "生成 XML" / "制作白模文件" / "导出工程" → [流程B: 生成 FairyGUI 工程]
  ├─ "分析这个 UI" / "解析工程结构"           → [流程C: 分析与讨论]
  └─ "写面板代码" / "新建面板" / "Logic.cs"   → [流程D: C# 代码架构]
      （流程 B 完成后若需要 C# 代码，自动衔接流程 D）
```

---

## 流程 A：生成 UI 示意图（仅图片）

### 步骤

1. **分析用户输入**（效果图或文字描述）
   - 识别所有 UI 元素：按钮、文本、图标、列表、滚动区域等
   - 推断元素用途和交互逻辑
   - 确定布局层级和层叠关系

2. **用 HTML/CSS 渲染示意图**
   - 创建一个独立的 HTML 文件
   - 使用 CSS 盒模型精确还原布局
   - 白模风格：深色背景 + 浅色/彩色色块表示不同元素类型
   - 添加文字标注说明各区域用途

3. **截图保存**
   - 使用 Puppeteer 将 HTML 页面截图保存为 JPG/PNG
   - 返回图片给用户确认

4. **按需生成原则**
   - ⚠️ **此流程不生成任何 FairyGUI XML 文件**
   - 仅输出示意图图片

### HTML 示意图模板

```html
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { background: #1a1a2e; font-family: Arial, sans-serif; }
  .container { width: [宽]px; height: [高]px; position: relative; overflow: hidden; }
  /* 用不同颜色区分元素类型 */
  .btn { background: #4a90d9; border-radius: 8px; }
  .text { color: #ffffff; }
  .icon { background: #666; border-radius: 50%; }
  .panel { background: #2d2d44; border-radius: 4px; }
  .input { background: #222; border: 1px solid #999; }
</style>
</head>
<body>
  <div class="container">
    <!-- 按层级排列 UI 元素 -->
  </div>
</body>
</html>
```

---

## 流程 B：生成 FairyGUI 工程文件

### 步骤

1. **确认 UI 结构**（若已有示意图则跳过）
   - 明确所有组件及其层级关系
   - 确定哪些需要独立为子组件

2. **生成工程文件**
   - `package.xml` — 包描述文件
   - 主界面组件 XML — 如 `Main.xml`
   - 所有被引用的子组件 XML

3. **执行校验**
   ```
   python scripts/validate_fui.py <输出目录>
   ```
   - 校验通过 → 交付给用户
   - 校验失败 → 修复后重新校验

4. **输出文件结构**
   ```
   输出目录/
   ├── package.xml          # 包描述
   ├── Main.xml             # 主界面（或用户指定名称）
   ├── components/          # 子组件目录
   │   ├── Button1.xml
   │   ├── ListItem.xml
   │   └── ...
   └── images/              # 空目录（预留给美术替换）
   ```

### 核心规则

#### 规则 1：统一使用 graph 替代 image

所有视觉元素用 `<graph>` 构建白模，**绝不使用 `<image>`**。

```xml
<!-- ✅ 正确：用 graph 做按钮背景 -->
<graph id="bg" name="bg" xy="0,0" size="200,50" type="rect"
       fillColor="#ff4a90d9" corner="8"/>

<!-- ❌ 错误：不要用 image -->
<image id="bg" name="bg" src="xxx" .../>
```

**颜色约定（白模风格）：**
| UI 元素 | fillColor |
|---------|-----------|
| 深色背景 | `#ff1a1a2e` |
| 面板/卡片 | `#ff2d2d44` |
| 按钮 | `#ff4a90d9` |
| 输入框 | `#ff222222` + `lineSize="1" lineColor="#ff999999"` |
| 头像/图标占位 | `#ff666666`（圆形用 `type="eclipse"`） |
| 进度条背景 | `#ff444444` |
| 进度条填充 | `#ff4a90d9` |
| 分隔线 | `#ff555555` |
| 高亮/选中 | `#ffffc107` |
| 危险/删除 | `#ffe74c3c` |
| 成功/确认 | `#ff2ecc71` |

#### 规则 2：组件闭环原则

生成包含子组件引用的 XML 时，**必须**：
1. 在 `package.xml` 的 `<resources>` 中声明所有组件
2. 提供每个被引用组件的完整 XML 文件
3. `<component>` 实例的 `fileName` 必须匹配 `package.xml` 中的 `path + name`
4. `<list>` 的 `defaultItem` 必须使用 `ui://` URL 格式（**绝不能用文件路径**）

```xml
<!-- package.xml 中声明 -->
<component id="gen_btn1" name="MyButton.xml" path="/components/"/>

<!-- component 引用（使用 src + fileName） -->
<component id="n1" name="n1" src="gen_btn1"
           fileName="components/MyButton.xml" xy="10,10"/>

<!-- list 引用（使用 defaultItem = ui://包ID+资源ID） -->
<list id="n2" name="myList" defaultItem="ui://ab12cd34gen_btn1" .../>

<!-- MyButton.xml 必须存在 -->
```

#### 规则 3：ID 生成规范

- 包 ID：8 字符随机字母小写 + 数字，如 `"ab12cd34"`
- 资源/元件 ID：`"gen_" + 两位递增编号`，如 `gen_01`, `gen_02`, ...
- 包内 ID 不可重复

#### 规则 3.5：默认开启代码导出

生成 `package.xml` 时，`<publish>` 标签**必须**包含 `genCode="true"` 属性，使 FairyGUI 编辑器对该包默认开启 C# 代码导出。这确保每个包都能生成对应的 Binder 和组件扩展类。

```xml
<publish name="PackageName" genCode="true">
  <atlas name="Default" index="0"/>
</publish>
```

#### 规则 4：绝对禁止

- ❌ XML 中使用 HTML 注释 `<!-- -->`
- ❌ 使用 `<image>` 标签（白模模式下）
- ❌ 引用不存在的资源 ID
- ❌ `<controller>` 出现在 `<displayList>` 之后
- ❌ list 的 `defaultItem` 使用文件路径（如 `components/Foo.xml`），必须用 `ui://包ID资源ID` 格式
- ❌ `<transition>` 出现在 `<displayList>` 之前
- ❌ 遗漏被引用子组件的 XML 文件

#### 规则 5：扩展机制命名约定

FairyGUI 的扩展（Button/Label/ProgressBar 等）通过**名称约定**工作：

| 扩展类型 | 约定名称 |
|---------|---------|
| Button | 控制器 `button`（pages: up/down/over/selectedOver），文本 `title`，装载器 `icon` |
| Label | 文本 `title`，装载器 `icon` |
| ProgressBar | 图片/graph `bar`，文本 `title` |
| Slider | graph `bar`，按钮 `grip`，文本 `title` |
| ScrollBar | 按钮 `grip`，graph `bar`，按钮 `arrow1`/`arrow2` |

### 生成示例

以下是一个简单弹窗的完整工程文件：

**package.xml:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<packageDescription id="ab12cd34">
  <resources>
    <component id="gen_01" name="SimpleDialog.xml" path="/" exported="true"/>
    <component id="gen_02" name="ConfirmButton.xml" path="/components/"/>
  </resources>
  <publish name="MyUI" genCode="true">
    <atlas name="Default" index="0"/>
  </publish>
</packageDescription>
```

**SimpleDialog.xml:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<component size="400,300">
  <displayList>
    <graph id="gen_03" name="bg" xy="0,0" size="400,300"
           type="rect" fillColor="#ff2d2d44" corner="12"/>
    <text id="gen_04" name="title" xy="20,15" size="360,30"
          fontSize="24" color="#ffffff" bold="true"
          align="center" autoSize="none" text="Dialog Title"/>
    <graph id="gen_05" name="divider" xy="20,55" size="360,1"
           type="rect" fillColor="#ff555555"/>
    <text id="gen_06" name="content" xy="20,70" size="360,150"
          fontSize="18" color="#cccccc" autoSize="height"
          text="Dialog content goes here."/>
    <component id="gen_07" name="confirmBtn" src="gen_02"
               fileName="components/ConfirmButton.xml"
               xy="140,240" size="120,40">
      <Button title="OK"/>
    </component>
  </displayList>
</component>
```

**components/ConfirmButton.xml:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<component size="120,40" extention="Button">
  <controller name="button" pages="0,up,1,down,2,over,3,selectedOver" selected="0"/>
  <displayList>
    <graph id="gen_08" name="bg_up" xy="0,0" size="120,40"
           type="rect" fillColor="#ff4a90d9" corner="8">
      <gearDisplay controller="button" pages="0"/>
      <relation target="" sidePair="width-width,height-height"/>
    </graph>
    <graph id="gen_09" name="bg_down" xy="0,0" size="120,40"
           type="rect" fillColor="#ff3a7bc8" corner="8">
      <gearDisplay controller="button" pages="1,3"/>
      <relation target="" sidePair="width-width,height-height"/>
    </graph>
    <graph id="gen_10" name="bg_over" xy="0,0" size="120,40"
           type="rect" fillColor="#ff5aa0e9" corner="8">
      <gearDisplay controller="button" pages="2"/>
      <relation target="" sidePair="width-width,height-height"/>
    </graph>
    <text id="gen_11" name="title" xy="0,0" size="120,40"
          fontSize="18" color="#ffffff" align="center" vAlign="middle"
          autoSize="none" singleLine="true" text="">
      <relation target="" sidePair="width-width,height-height"/>
    </text>
  </displayList>
  <Button/>
</component>
```

---

## 流程 C：分析与讨论

直接回答用户关于 FairyGUI 结构的问题，引用 `references/fairygui-xml-spec.md` 中的规范。

---

## 流程 D：C# 代码架构规范（Extension + IUIPanel + Logic.cs）

当生成 FairyGUI XML 白模后需要编写对应的 Unity C# 代码时，或者新建面板/对话框时，必须遵循本流程。

### 架构总览

```
FairyGUI 编辑器（genCode=true）
  │
  ├─ 导出 XXXPanel.cs       ← GComponent 子类 + ConstructFromXML + static URL
  ├─ 导出 XXXBinder.cs      ← UIObjectFactory.SetPackageItemExtension 注册
  │
  └─ 手写 XXXPanel.Logic.cs ← partial class + IUIPanel（业务逻辑）
```

UIManager 利用 FairyGUI 原生 Extension 机制（Binder + `CreateObjectFromURL`）创建面板实例，通过 `IUIPanel` 接口管理生命周期。

### 目录结构（强制）

按 FairyGUI 包名分子目录，导出代码和业务逻辑放在同一目录：

```
Assets/_Game/Scripts/UI/
├── Common/
│   ├── CommonBinder.cs           ← FairyGUI 导出（禁止手改）
│   ├── LoadingPanel.cs           ← FairyGUI 导出（禁止手改）
│   ├── LoadingPanel.Logic.cs     ← 手写业务逻辑
│   ├── ConfirmDialog.cs          ← FairyGUI 导出（禁止手改）
│   └── ConfirmDialog.Logic.cs    ← 手写业务逻辑
├── MainMenu/
│   ├── MainMenuBinder.cs
│   ├── MainMenuPanel.cs
│   └── MainMenuPanel.Logic.cs
└── <NewPackage>/
    ├── <NewPackage>Binder.cs
    ├── XXXPanel.cs
    └── XXXPanel.Logic.cs
```

### 命名空间规则（强约束）

- 命名空间 = FairyGUI 包名（如 `namespace Common`、`namespace MainMenu`）
- UIManager 运行时通过 `type.Namespace` 推导包名，因此**命名空间必须与 FairyGUI 包名完全一致**
- 同包内不同组件共享同一命名空间

### IUIPanel 接口

所有面板必须实现 `MiniGameTemplate.UI.IUIPanel` 接口：

```csharp
public interface IUIPanel
{
    int PanelSortOrder { get; }      // 层级排序，使用 UIConstants.LAYER_* 常量
    bool IsFullScreen { get; }       // true = 全屏铺满, false = 居中保持原尺寸
    string PanelPackageName { get; } // FairyGUI 包名（与命名空间一致）
    void OnOpen(object data);        // 创建后调用：绑定事件 + 初始化数据
    void OnClose();                  // 销毁前调用：清理资源、取消定时器
    void OnRefresh(object data);     // 已打开时再次 Open 触发：仅刷新数据
}
```

对话框额外实现 `IModalDialog`，UIManager 自动创建半透明遮罩：

```csharp
public interface IModalDialog
{
    bool CloseOnClickOutside { get; } // 点击遮罩是否关闭
}
```

### 层级常量

```csharp
UIConstants.LAYER_BACKGROUND = 0
UIConstants.LAYER_NORMAL     = 100
UIConstants.LAYER_POPUP      = 200
UIConstants.LAYER_DIALOG     = 300
UIConstants.LAYER_TOAST      = 400
UIConstants.LAYER_GUIDE      = 500
UIConstants.LAYER_LOADING    = 600
```

### Logic.cs 模板（普通面板）

```csharp
using MiniGameTemplate.UI;

namespace <PackageName>  // 必须与 FairyGUI 包名一致
{
    // Data class（可选，面板需要外部数据时使用）
    public class XXXPanelData
    {
        public string SomeField;
    }

    public partial class XXXPanel : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL;
        public bool IsFullScreen => true;
        public string PanelPackageName => "<PackageName>";

        public void OnOpen(object data)
        {
            // 绑定按钮事件（仅在 OnOpen 中做一次，绝不在 OnRefresh 中重复绑定）
            if (btnXxx != null) btnXxx.onClick.Add(OnXxxClicked);
            ApplyData(data);
        }

        public void OnClose()
        {
            // 清理资源、取消定时器、释放引用
        }

        public void OnRefresh(object data)
        {
            // ⚠️ 仅刷新数据，绝不调用 OnOpen(data)，避免事件双绑定
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            var d = data as XXXPanelData;
            if (d == null) return;
            // 应用数据到 UI 元素
        }

        private void OnXxxClicked()
        {
            // 业务逻辑
        }
    }
}
```

### Logic.cs 模板（对话框）

```csharp
using System;
using MiniGameTemplate.UI;

namespace <PackageName>
{
    public class XXXDialogData
    {
        public string Title = "提示";
        public string Content = "";
        public Action OnConfirm;
        public Action OnCancel;
    }

    public partial class XXXDialog : IUIPanel, IModalDialog
    {
        public int PanelSortOrder => UIConstants.LAYER_DIALOG;
        public bool IsFullScreen => false;  // 对话框不全屏
        public string PanelPackageName => "<PackageName>";
        public bool CloseOnClickOutside => false;

        private Action _onConfirm;
        private Action _onCancel;

        public void OnOpen(object data)
        {
            if (btnConfirm != null) btnConfirm.onClick.Add(OnConfirmClicked);
            if (btnCancel != null) btnCancel.onClick.Add(OnCancelClicked);
            ApplyData(data);
        }

        public void OnClose()
        {
            var pendingCancel = _onCancel;
            _onConfirm = null;
            _onCancel = null;
            pendingCancel?.Invoke(); // 安全兜底：外部关闭时视为取消
        }

        public void OnRefresh(object data)
        {
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            var d = data as XXXDialogData;
            if (d == null) return;
            _onConfirm = d.OnConfirm;
            _onCancel = d.OnCancel;
            if (txtTitle != null) txtTitle.text = d.Title;
            if (txtContent != null) txtContent.text = d.Content;
        }

        private void OnConfirmClicked()
        {
            var cb = _onConfirm;
            _onConfirm = null;
            _onCancel = null;
            UIManager.Instance.ClosePanel<XXXDialog>();
            cb?.Invoke();
        }

        private void OnCancelClicked()
        {
            var cb = _onCancel;
            _onConfirm = null;
            _onCancel = null;
            UIManager.Instance.ClosePanel<XXXDialog>();
            cb?.Invoke();
        }
    }
}
```

### Binder 注册（启动时）

在 `GameStartupFlow.RunAsync` 中为每个 FairyGUI 包注册 Binder：

```csharp
UIManager.RegisterBinder("Common", Common.CommonBinder.BindAll);
UIManager.RegisterBinder("MainMenu", MainMenu.MainMenuBinder.BindAll);
// 新增包时在此追加注册
```

Binder 采用**懒激活**机制：注册时仅记录，首次 `OpenPanelAsync` 使用该包时才执行 `BindAll()`。

### 打开/关闭面板

```csharp
// 打开面板（异步，T 必须同时是 GComponent 和 IUIPanel）
await UIManager.Instance.OpenPanelAsync<Common.LoadingPanel>();
await UIManager.Instance.OpenPanelAsync<MainMenu.MainMenuPanel>(menuData);

// 关闭面板
UIManager.Instance.ClosePanel<Common.LoadingPanel>();

// 关闭所有面板（场景切换时）
UIManager.Instance.CloseAllPanels();
```

### 强制规则清单

| # | 规则 | 原因 |
|---|------|------|
| 1 | FairyGUI 导出的 `*.cs` 和 `*Binder.cs` **禁止手动修改** | 编辑器重新导出会覆盖 |
| 2 | 业务逻辑文件命名为 `XXXPanel.Logic.cs` | 与导出文件区分，`partial class` 连接 |
| 3 | 命名空间 = FairyGUI 包名 | UIManager 用 `type.Namespace` 推导包名 |
| 4 | `OnRefresh` 必须调 `ApplyData(data)`，**绝不调 `OnOpen(data)`** | 防止事件双绑定（P0 级别缺陷） |
| 5 | 事件绑定（`onClick.Add` 等）只在 `OnOpen` 中做一次 | `OnRefresh` 重复绑定会导致回调多次触发 |
| 6 | 对话框 `OnClose` 要兜底调 `OnCancel` | 防止外部关闭时 `TaskCompletionSource` 永久阻塞 |
| 7 | 每个新包在 `GameStartupFlow` 中注册 Binder | 否则 `CreateObjectFromURL` 返回 null |
| 8 | `PanelPackageName` 用字符串字面量，与命名空间保持一致 | 运行时包加载依赖此值 |

### 新建面板完整流程

1. FairyGUI 编辑器中创建组件，设置 `genCode="true"`
2. 导出 C# 代码到 `_Game/Scripts/UI/<PackageName>/`
3. 创建 `XXXPanel.Logic.cs`，实现 `IUIPanel`（参考上方模板）
4. 在 `GameStartupFlow.RunAsync` 中追加 `UIManager.RegisterBinder(...)` 注册
5. 使用 `await UIManager.Instance.OpenPanelAsync<PackageName.XXXPanel>(data)` 打开

---

## 参考资料

### XML 规范手册
完整的 FairyGUI XML 标签、属性、值域规范见：
`references/fairygui-xml-spec.md`

### 校验脚本
生成 XML 后必须执行校验：
```bash
python scripts/validate_fui.py <输出目录或文件>
```

### 知识来源
- FairyGUI 官方设计文档（24 篇）
- FairyGUI 官方示例工程（16 个包，176 个 XML 文件）
- FairyGUI 编辑器源码分析


