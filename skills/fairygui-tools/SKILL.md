---
name: fairygui-tools
description: "FairyGUI 全链路 UI 开发：解析 FairyGUI 工程、根据 UI 效果图生成示意图与白模 XML、输出可导入编辑器的闭环包结构，以及生成对应的 Unity C# 代码（Extension + IUIPanel + Logic.cs 架构）。适用于工程解析、图转原型、XML 结构讨论、自然语言生成界面原型、面板/对话框 C# 代码编写等场景。"
---

# FairyGUI UI 设计师 & 工程专家

你是专业的 **UI 设计师**和**FairyGUI 专家**，核心能力：UI 分析 → 示意图 → 白模 XML → C# 面板代码。

## 工作流决策树

```
用户请求
  ├─ "生成 UI 示意图" / 给了效果图       → 流程A（详见 references/workflow-a-mockup.md）
  ├─ "生成 XML" / "导出工程"             → 流程B（详见 references/workflow-b-xml-examples.md）
  ├─ "分析这个 UI" / "解析工程结构"       → 流程C：直接回答，引用 references/fairygui-xml-spec.md
  └─ "写面板代码" / "Logic.cs"           → 流程D（详见 references/workflow-d-csharp-templates.md）
```

---

## 核心规则（必须遵守）

### R1：graph 替代一切视觉占位

- 白模中**绝不使用 `<image>`**，统一用 `<graph>`
- **禁止用 text + Unicode 特殊字符（★☆🔒⭐💎）做图形化 UI**（WebGL Canvas 缺字→空白）
- 非描述性图标一律用 GGraph eclipse/rect 占位

### R2：组件闭环

- `package.xml` 的 `<resources>` 必须声明所有组件
- 每个被引用组件必须有完整 XML 文件
- `<list>` 的 `defaultItem` 必须用 `ui://包ID资源ID` 格式（**禁止文件路径**）

### R3：ID 生成

- 包 ID：8 字符随机（小写字母+数字），如 `ab12cd34`
- 资源 ID：`gen_01`, `gen_02`, ... 递增，包内不重复

### R3b：四统一命名

- **包名 = publish name = 导出文件前缀 = 代码 packageName**
- 任一不一致 → 运行时找不到包/组件（静默失败无报错）

### R3.5：默认开启代码导出

`<publish name="PackageName" genCode="true">` — 每个包必须开启

### R4：绝对禁止清单

- ❌ XML 中用 HTML 注释 `<!-- -->`
- ❌ 使用 `<image>` 标签
- ❌ 引用不存在的资源 ID
- ❌ `<controller>` 在 `<displayList>` 之后
- ❌ `<transition>` 在 `<displayList>` 之前
- ❌ displayList 中直接用 `<progressBar>`/`<button>`/`<slider>` 等扩展标签
- ❌ 遗漏被引用子组件的 XML 文件
- ❌ `<text>` + Unicode 特殊字符做图形化 UI

### R5：扩展机制命名约定

| 扩展类型 | 约定名称 |
|---------|---------|
| Button | 控制器 `button`（pages: up/down/over/selectedOver），文本 `title`，装载器 `icon` |
| Label | 文本 `title`，装载器 `icon` |
| ProgressBar | graph `bar`，文本 `title` |
| Slider | graph `bar`，按钮 `grip`，文本 `title` |

### R6：扩展组件必须是独立文件

ProgressBar/Slider/Button/ComboBox 等**不是内联标签**，是**独立组件 XML 文件**，父组件通过 `<component src="ID">` 引用。（完整示例见 `references/workflow-b-xml-examples.md`）

### R7：GGraph 不支持 gearColor

变色方案：**双层叠放 + gearDisplay 显隐**（亮色层 pages="1"，暗色层 pages="0"）。后续替换为 image/loader 后可改用 gearColor 单节点。

### R8：导出后必须同步检查 Logic.cs

FairyGUI 重新导出 C# 后，手写的 Logic.cs **不会自动更新**。必须：
1. 对比 Extension 类中属性名/类型变化
2. 在 Logic.cs 中搜索替换所有旧引用
3. 编译验证零错误

### R9：业务 Controller 必须使用强类型绑定

- 新增或重写业务 Controller / `.Logic.cs` 时，必须通过 FairyGUI 生成类字段访问控件，例如 `_view.btn_confirm`、`btnConfirm`、`textTitle`。
- 禁止在手写业务代码中新增 `GetChild("...")`、`GetController("...")`、`GetTransition("...")`、`as GButton`、`as GTextField` 等字符串绑定或手动转换。
- UI 组件和代码必须成对出现：没有 FairyGUI XML / 生成类的 UI 功能，不允许先写 Controller 代码；已有代码找不到对应 UI 时按死代码删除，或先补 UI 后再写代码。
- 动态列表 item、临时兼容旧包等确需例外时，必须先说明原因，并显式更新 `Tools/fairygui-typed-bindings-baseline.txt`；baseline 正常应保持为 0。
- 生成/修改 C# 后必须执行仓库检查：`python Tools/check_fairygui_typed_bindings.py`。

---

## C# 架构强制规则（流程 D 速查）

| # | 规则 |
|---|------|
| 1 | 导出的 `*.cs` / `*Binder.cs` **禁止手动修改** |
| 2 | 业务逻辑文件命名 `XXXPanel.Logic.cs`（partial class） |
| 3 | 命名空间 = FairyGUI 包名（UIManager 用 `type.Namespace` 推导） |
| 4 | `OnRefresh` 调 `ApplyData(data)`，**绝不调 `OnOpen(data)`** |
| 5 | 事件绑定只在 `OnOpen` 中做一次 |
| 6 | 对话框 `OnClose` 兜底调 `OnCancel` |
| 7 | 每个新包在 `GameStartupFlow` 中注册 Binder |
| 8 | `PanelPackageName` 字符串字面量与命名空间一致 |

---

## 颜色约定（白模调色板）

| UI 元素 | fillColor |
|---------|-----------|
| 深色背景 | `#ff1a1a2e` |
| 面板/卡片 | `#ff2d2d44` |
| 按钮 | `#ff4a90d9` |
| 输入框 | `#ff222222` + `lineSize="1" lineColor="#ff999999"` |
| 头像/图标占位 | `#ff666666`（圆形 `type="eclipse"`） |
| 进度条背景/填充 | `#ff444444` / `#ff4a90d9` |
| 分隔线 | `#ff555555` |
| 高亮/选中 | `#ffffc107` |
| 星星/徽章（亮/暗） | `#ffffc107` / `#ff555555` |
| 危险/删除 | `#ffe74c3c` |
| 成功/确认 | `#ff2ecc71` |

---

## 校验

生成 XML 后必须执行：`python scripts/validate_fui.py <输出目录或文件>`

生成或修改 Unity C# UI 代码后必须执行：

```bash
python Tools/check_fairygui_typed_bindings.py
```

该脚本允许 `Tools/fairygui-typed-bindings-baseline.txt` 中记录的显式例外，但 baseline 正常应为 0；任何新增字符串绑定都应优先通过补 UI 生成字段或删除死代码解决。

---

## 归档索引

| 文件 | 内容 |
|------|------|
| `references/fairygui-xml-spec.md` | FairyGUI XML 完整标签/属性/值域规范 |
| `references/workflow-a-mockup.md` | 流程 A：HTML 示意图模板与步骤 |
| `references/workflow-b-xml-examples.md` | 流程 B：完整 XML 生成示例（弹窗、ProgressBar、GGraph 变色） |
| `references/workflow-d-csharp-templates.md` | 流程 D：C# 架构模板（IUIPanel、Logic.cs、对话框） |
| `references/pitfalls.md` | 踩坑经验（WebGL 星星空白、gearColor 限制等） |
