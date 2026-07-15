# 流程 D：C# 代码架构模板

> 归档自 SKILL.md，按需加载。

## 架构总览

```
FairyGUI 编辑器（genCode=true）
  │
  ├─ 导出 XXXPanel.cs       ← GComponent 子类 + ConstructFromXML + static URL
  ├─ 导出 XXXBinder.cs      ← UIObjectFactory.SetPackageItemExtension 注册
  │
  └─ 手写 XXXPanel.Logic.cs ← partial class + IUIPanel（业务逻辑）
```

## 目录结构（强制）

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

## IUIPanel 接口

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

对话框额外实现 `IModalDialog`：

```csharp
public interface IModalDialog
{
    bool CloseOnClickOutside { get; } // 点击遮罩是否关闭
}
```

## 层级常量

```csharp
UIConstants.LAYER_BACKGROUND = 0
UIConstants.LAYER_NORMAL     = 100
UIConstants.LAYER_POPUP      = 200
UIConstants.LAYER_DIALOG     = 300
UIConstants.LAYER_TOAST      = 400
UIConstants.LAYER_GUIDE      = 500
UIConstants.LAYER_LOADING    = 600
```

## Logic.cs 模板（普通面板）

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
            // ⚠️ 仅刷新数据，绝不调 OnOpen(data)，避免事件双绑定
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

## 强类型绑定要求

手写业务代码只访问 FairyGUI 生成字段，不新增字符串查找。

UI 和代码必须成对落地：新增面板、弹窗、列表 item、按钮或动效入口时，先在 `UIProject/assets/<Package>/` 创建 XML 并发布生成类，再写 Controller 逻辑。没有对应 XML / 生成字段的 Controller 逻辑视为死代码，应删除。

允许：

```csharp
if (btnConfirm != null)
    btnConfirm.onClick.Add(OnConfirmClicked);

if (txtTitle != null)
    txtTitle.text = d.Title;
```

禁止：

```csharp
GetChild("btn_confirm").asButton.onClick.Add(OnConfirmClicked);
var title = GetChild("txt_title") as GTextField;
var state = GetController("state");
var intro = GetTransition("t_intro");
```

生成/修改 `XXXPanel.Logic.cs`、Controller 或手写 UI 代码后，必须在仓库根目录执行：

```bash
python Tools/check_fairygui_typed_bindings.py
```

如果脚本报出新增违规，优先回到 FairyGUI 导出设置补生成字段，或删除没有 UI 承载的代码；只有经过确认的兼容例外才更新 `Tools/fairygui-typed-bindings-baseline.txt`，且 baseline 正常应保持为 0。

## Logic.cs 模板（对话框）

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

## Binder 注册（启动时）

在 `GameStartupFlow.RunAsync` 中为每个 FairyGUI 包注册 Binder：

```csharp
UIManager.RegisterBinder("Common", Common.CommonBinder.BindAll);
UIManager.RegisterBinder("MainMenu", MainMenu.MainMenuBinder.BindAll);
// 新增包时在此追加注册
```

Binder 采用**懒激活**机制：注册时仅记录，首次 `OpenPanelAsync` 使用该包时才执行 `BindAll()`。

## 打开/关闭面板

```csharp
// 打开面板（异步，T 必须同时是 GComponent 和 IUIPanel）
await UIManager.Instance.OpenPanelAsync<Common.LoadingPanel>();
await UIManager.Instance.OpenPanelAsync<MainMenu.MainMenuPanel>(menuData);

// 关闭面板
UIManager.Instance.ClosePanel<Common.LoadingPanel>();

// 关闭所有面板（场景切换时）
UIManager.Instance.CloseAllPanels();
```

## 新建面板完整流程

1. FairyGUI 编辑器中创建组件，设置 `genCode="true"`
2. 导出 C# 代码到 `_Game/Scripts/UI/<PackageName>/`
3. 创建 `XXXPanel.Logic.cs`，实现 `IUIPanel`（参考上方模板）
4. 在 `GameStartupFlow.RunAsync` 中追加 `UIManager.RegisterBinder(...)` 注册
5. 使用 `await UIManager.Instance.OpenPanelAsync<PackageName.XXXPanel>(data)` 打开
