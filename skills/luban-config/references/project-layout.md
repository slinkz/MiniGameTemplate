# 项目配置表架构布局

## 目录结构

```
MiniGameTemplate/
├── UnityProj/
│   ├── DataTables/                          ← Luban 工作目录
│   │   ├── luban.conf                       ← Luban v4.6.0 主配置
│   │   ├── Defs/
│   │   │   └── tables.xml                   ← Bean + Table schema 定义
│   │   └── Datas/
│   │       ├── item.xlsx                    ← 道具表数据（策划编辑）
│   │       └── globalconst.xlsx             ← 全局常量表数据
│   │
│   ├── Tools/
│   │   ├── Luban/
│   │   │   └── Luban.dll                    ← 预编译的 Luban CLI (v4.6.0)
│   │   ├── gen_config.bat                   ← Windows 生成脚本
│   │   └── gen_config.sh                    ← macOS/Linux 生成脚本
│   │
│   └── Assets/
│       ├── _Framework/
│       │   ├── DataSystem/
│       │   │   ├── Scripts/Config/
│       │   │   │   ├── ConfigManager.cs          ← 运行时加载入口（静态类）
│       │   │   │   ├── TablesExtension.cs        ← 手写 partial（表名注册）
│       │   │   │   └── Generated/                ← 🔒 Luban 自动生成，勿手改
│       │   │   │       ├── Tables.cs             ← 总表入口（partial class）
│       │   │   │       ├── Item.cs               ← Bean 数据类
│       │   │   │       ├── TbItem.cs             ← 表容器（Dictionary索引）
│       │   │   │       ├── GlobalConst.cs
│       │   │   │       └── TbGlobalConst.cs
│       │   │   └── Resources/ConfigData/         ← .bytes Resources fallback
│       │   │       ├── tbitem.bytes
│       │   │       └── tbglobalconst.bytes
│       │   └── Editor/ConfigPreview/             ← .json 编辑器预览（不打包）
│       │       ├── tbitem.json
│       │       └── tbglobalconst.json
│       │
│       └── _Game/
│           └── ConfigData/                       ← .bytes YooAsset 运行时加载
│               ├── tbitem.bytes
│               └── tbglobalconst.bytes
```

## 数据流

```
                    ┌─────────────────────────────────────────────┐
                    │  DataTables/Datas/*.xlsx（策划用 Excel 编辑）  │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  DataTables/Defs/tables.xml（Schema 定义）    │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  gen_config.bat / .sh（调用 Luban CLI）       │
                    │                                              │
                    │  Step 1: -c cs-bin -d bin → C# 代码 + .bytes │
                    │  Step 2: -d json → .json 预览                │
                    └───┬──────────┬────────────────────────────────┘
                        │          │
              ┌─────────▼───┐  ┌──▼──────────┐  ┌▼──────────────────┐
              │ Generated/  │  │ ConfigData/ │  │ ConfigPreview/    │
              │ *.cs        │  │ *.bytes     │  │ *.json            │
              │ (C# 代码)    │  │ (运行时数据) │  │ (编辑器预览)       │
              └──────┬──────┘  └──────┬──────┘  └───────────────────┘
                     │                │
              ┌──────▼────────────────▼──────┐
              │ ConfigManager.InitializeAsync │
              │  1. GetTableNames()           │
              │  2. LoadConfigBytesAsync()    │
              │  3. new Tables(ByteBuf loader)│
              └──────────────────────────────┘
```

## ConfigManager 架构

`ConfigManager` 是静态类，提供两种初始化方式：

- `InitializeAsync()` — 异步加载（推荐，WebGL 安全）
  1. 从 `TablesExtension.GetTableNames()` 获取所有表名
  2. 异步加载每个 `.bytes`（仅 YooAsset，编辑器使用 EditorSimulate 模式）
  3. 构造 `cfg.Tables(Func<string, ByteBuf>)`

- `Initialize()` — 同步加载（编辑器/测试用）
  1. 直接用 `Resources.Load<TextAsset>()` 同步加载
  2. 构造 `cfg.Tables`

### 安全特性

- `IsValidConfigFileName()` — 防路径遍历攻击
- `IntegrityVerifier` — 可选的完整性校验委托
- `ResetStatics()` — Domain Reload 兼容

## TablesExtension.cs 的作用

`cfg.Tables` 是 Luban 自动生成的 partial class。`TablesExtension.cs` 是手写的 partial 扩展，提供 `GetTableNames()` 静态方法。

**为什么需要这个文件**：`ConfigManager` 需要在构造 `Tables` 之前知道所有表名以预加载 `.bytes` 数据。Luban 生成的 `Tables.cs` 不包含表名列表，所以通过 partial class 补充。

**同步规则**：每次新增/删除表后，`GetTableNames()` 返回的数组必须更新。可用 `update_tables_extension.py` 脚本自动完成。

## .gitattributes 配置

```
DataTables/**/*.xlsx binary
DataTables/**/*.xls binary
```

xlsx 是二进制格式，必须在 `.gitattributes` 中标记为 binary，防止 Git 行尾转换损坏文件。
