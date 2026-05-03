# Luban v4.6.0 格式规范与踩坑记录

## luban.conf 配置格式

Luban v4.x 使用 JSON 配置文件（非 v3 的 YAML）。

### 最小可用配置

```json
{
    "groups": [
        {"names": ["c"], "default": true}
    ],
    "schemaFiles": [
        {"fileName": "Defs", "type": ""}
    ],
    "dataDir": "Datas",
    "targets": [
        {
            "name": "all",
            "manager": "Tables",
            "groups": ["c"],
            "topModule": "cfg"
        }
    ],
    "xargs": []
}
```

### 关键字段说明

| 字段 | 说明 | 注意事项 |
|------|------|----------|
| `groups` | 分组定义 | **必须有至少一个 `"default": true`**，否则所有表被认为不属于任何组，导出为空 |
| `schemaFiles` | Schema 文件/目录 | `"type": ""` 表示自动探测；目录会递归扫描所有 `.xml` |
| `dataDir` | 数据源根目录 | 相对于 luban.conf 所在目录 |
| `targets[].name` | 目标名称 | CLI 的 `-t` 参数引用此名称 |
| `targets[].manager` | 总表类名 | 生成的 `Tables` 类名 |
| `targets[].groups` | 目标包含的分组 | 必须引用 `groups` 中定义的名称 |
| `targets[].topModule` | 顶层命名空间 | 生成代码放在此命名空间下（如 `cfg`） |

### ⚠️ 致命陷阱：groups 为空

```json
// ❌ 导致生成空代码！
"groups": []

// ✅ 正确
"groups": [{"names": ["c"], "default": true}]
```

如果 `groups` 为空数组且 `table` 没有显式指定 `group`，Luban 会认为没有表需要导出。

## tables.xml Schema 格式

### Bean 定义

```xml
<bean name="Item">
    <var name="id" type="int"/>
    <var name="name" type="string"/>
    <var name="desc" type="string"/>
    <var name="tags" type="list,string"/>       <!-- 列表类型 -->
    <var name="extra" type="string" default=""/>  <!-- 带默认值 -->
</bean>
```

### Table 定义

```xml
<table name="TbItem" value="Item" input="item.xlsx" mode="map" index="id"/>
```

| 属性 | 说明 |
|------|------|
| `name` | 表类名（生成 C# 类用） |
| `value` | 数据 Bean 名（引用上面的 `<bean>`） |
| `input` | 数据源文件名（相对于 `dataDir`） |
| `mode` | `map`（字典，需 index）或 `list`（列表） |
| `index` | map 模式的索引字段名 |
| `group` | 可选，指定属于哪个分组。不指定则仅被 `default: true` 的组包含 |

### 枚举定义

```xml
<enum name="QualityType">
    <var name="Normal" alias="普通" value="0"/>
    <var name="Rare" alias="稀有" value="1"/>
    <var name="Epic" alias="史诗" value="2"/>
</enum>
```

### 嵌套 Bean / 多态

```xml
<bean name="Reward">
    <var name="type" type="string"/>
    <var name="amount" type="int"/>
</bean>

<bean name="Quest">
    <var name="id" type="int"/>
    <var name="rewards" type="list,Reward"/>  <!-- Bean 列表 -->
</bean>
```

## xlsx 数据源格式

### 行结构

| 行号 | A列标记 | 含义 |
|------|---------|------|
| 1 | `##var` | 字段名行（必须与 Bean 的 var name 匹配） |
| 2 | `##type` | 字段类型行（必须与 Bean 的 var type 匹配） |
| 3 | `##` | 注释行（中文表头，Luban 忽略此行内容） |
| 4+ | _(空)_ | 数据行 |

### A列的作用

- 第1~3行的 A 列是标记列（`##var`、`##type`、`##`）
- 数据行的 A 列**留空**即可（Luban 不读取 A 列的数据）
- 如果数据行 A 列写了 `##`，该行会被跳过（可用于注释掉某行数据）

### 注释/跳过行

- 数据行 A 列写 `##` → 整行被跳过
- 字段名以 `##` 开头 → 整列被跳过（可用于策划备注列）

### 多Sheet

- 一个 xlsx 可以有多个 Sheet
- Luban 默认读取**所有 Sheet**
- Sheet 名以 `#` 开头 → 该 Sheet 被跳过

### 列表/字典类型在 xlsx 中的写法

| 类型 | xlsx 单元格写法 | 说明 |
|------|-----------------|------|
| `list,int` | `1,2,3` | 逗号分隔 |
| `list,string` | `a,b,c` | 逗号分隔 |
| `vector2` | `1.0,2.0` | 逗号分隔 |
| `vector3` | `1.0,2.0,3.0` | 逗号分隔 |
| `bool` | `true` / `false` / `1` / `0` | 多种写法 |

### ⚠️ 常见数据错误

1. **int 列有空单元格** → Luban 报错 "cannot parse"。必须填数字或删除该行
2. **string 列不需要引号** → 直接写文本，不要加 `""`
3. **bool 列** → `true`/`false`（不区分大小写）或 `1`/`0`
4. **首行不是 ##var** → Luban 不知道如何映射字段，静默跳过数据

## CLI 参数速查

```bash
dotnet Luban.dll \
    --conf <luban.conf路径> \
    -t <target名称>          \  # 对应 targets[].name
    -c <代码生成器>           \  # cs-bin / cs-simple-json / cs-newtonsoft-json
    -d <数据格式>             \  # bin / json / bson / yaml
    -x outputCodeDir=<路径>   \  # 生成代码输出
    -x outputDataDir=<路径>      # 生成数据输出
```

### 本项目使用的参数组合

**Step 1 - Binary 代码 + 数据**：
```bash
-t all -c cs-bin -d bin -x outputCodeDir=... -x outputDataDir=...
```

**Step 2 - JSON 预览数据（不生成代码）**：
```bash
-t all -d json -x outputDataDir=...
```

## Schema 扫描行为

- `"fileName": "Defs"` + `"type": ""` → 递归扫描 `Defs/` 目录下所有 `.xml` 文件
- `_` 开头的文件**不会被忽略**（与数据文件不同）
- `.bak` 文件也会被尝试加载 → 确保 `Defs/` 下没有垃圾文件

## 生成文件命名规则

| 输入 | 生成 C# 文件 | 生成数据文件 |
|------|-------------|-------------|
| `<bean name="Item">` | `Item.cs` | — |
| `<table name="TbItem">` | `TbItem.cs` | `tbitem.bytes` / `tbitem.json` |
| `<table name="TbGlobalConst">` | `TbGlobalConst.cs` | `tbglobalconst.bytes` / `tbglobalconst.json` |

**规律**：数据文件名 = Table name 全小写（`TbItem` → `tbitem`）

`TablesExtension.cs` 中的 `GetTableNames()` 必须返回这些小写名称。

## 版本兼容性备注

- 本项目锁定 Luban **v4.6.0**，CLI 已预编译到 `Tools/Luban/`
- v4.x 与 v3.x **不兼容**：配置文件格式从 YAML 改为 JSON，schema 语法也有变化
- 如果参考网上的 Luban 教程，注意区分 v3 和 v4 的写法
