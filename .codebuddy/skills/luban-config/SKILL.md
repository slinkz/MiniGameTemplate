---
name: luban-config
description: >
  Luban v4.6.0 配置表管理技能。当需要新增、修改、删除 Luban 配置表，
  创建 xlsx 数据文件，运行生成脚本，或排查 Luban 相关问题时，加载此技能。
  触发关键词：配置表、Luban、luban、xlsx 数据表、新增表、添加配置、
  gen_config、DataTables、tables.xml、luban.conf、TablesExtension。
---

# Luban Config Skill

管理本项目的 Luban v4.6.0 配置表系统——从定义 Schema 到生成代码到运行时加载的全流程。

## 核心概念

本项目采用 **Binary 运行时 + JSON 编辑器预览** 双格式方案：
- 运行时：`cs-bin` 生成的 C# 代码 + `.bytes` 二进制数据（YooAsset 加载，Resources fallback）
- 编辑器：`Editor/ConfigPreview/*.json` 供人工查看（不打包）
- 数据源：**xlsx 格式**，策划可用 Excel 直接编辑

## 关键文件路径

| 文件 | 路径 | 说明 |
|------|------|------|
| Luban CLI | `UnityProj/Tools/Luban/Luban.dll` | dotnet 运行 |
| 配置入口 | `UnityProj/DataTables/luban.conf` | v4.6.0 格式 |
| Schema 定义 | `UnityProj/DataTables/Defs/tables.xml` | Bean + Table XML |
| 数据源目录 | `UnityProj/DataTables/Datas/` | xlsx 文件 |
| 生成脚本 | `UnityProj/Tools/gen_config.bat` / `.sh` | 3 步：cs-bin+bin → json → copy |
| 生成代码输出 | `Assets/_Framework/DataSystem/Scripts/Config/Generated/` | 自动生成，勿手改 |
| TablesExtension | `Assets/_Framework/DataSystem/Scripts/Config/TablesExtension.cs` | 手写 partial，**必须同步** |
| ConfigManager | `Assets/_Framework/DataSystem/Scripts/Config/ConfigManager.cs` | 运行时加载入口 |
| Binary 输出 | `Assets/_Game/ConfigData/*.bytes` | YooAsset 收集 |
| Resources 副本 | `Assets/_Framework/DataSystem/Resources/ConfigData/*.bytes` | fallback |
| JSON 预览 | `Assets/_Framework/Editor/ConfigPreview/*.json` | 编辑器查看 |

## SOP：新增一张配置表

按以下步骤**严格按顺序**执行，不可跳步：

### Step 1: 定义 Schema

在 `DataTables/Defs/tables.xml` 的 `<module>` 内新增 `<bean>` 和 `<table>`：

```xml
<bean name="MyData">
    <var name="id" type="int"/>
    <var name="name" type="string"/>
    <!-- 更多字段 -->
</bean>

<table name="TbMyData" value="MyData" input="mydata.xlsx" mode="map" index="id"/>
```

**命名规范**：
- Bean 名：PascalCase（如 `MyData`）
- Table 名：`Tb` + Bean 名（如 `TbMyData`）
- xlsx 文件名：全小写（如 `mydata.xlsx`）
- `mode="map"` 用字典索引，`mode="list"` 用列表

### Step 2: 创建 xlsx 数据文件

运行 Skill 脚本自动创建：

```bash
python .workbuddy/skills/luban-config/scripts/create_xlsx.py \
  --output UnityProj/DataTables/Datas/mydata.xlsx \
  --sheet TbMyData \
  --fields "id:int:ID,name:string:名称"
```

或手动创建，**xlsx 格式必须严格遵循**：

| 行号 | A列 | B列 | C列 | ... |
|------|------|------|------|-----|
| 1 | `##var` | id | name | ... |
| 2 | `##type` | int | string | ... |
| 3 | `##` | ID | 名称 | ... |
| 4+ | _(空)_ | 1001 | 测试道具 | ... |

⚠️ **A列**第1行必须是 `##var`，第2行 `##type`，第3行 `##`（注释行），第4行起 A列留空。

### Step 3: 更新 TablesExtension.cs

运行 Skill 脚本自动更新：

```bash
python .codebuddy/skills/luban-config/scripts/update_tables_extension.py
```

此脚本会读取 `tables.xml` 中所有 `<table>` 定义，自动重新生成 `TablesExtension.cs` 中的 `GetTableNames()` 数组。

或手动编辑 `TablesExtension.cs`，在 `GetTableNames()` 的返回数组中添加新表名（**全小写**，如 `"tbmydata"`）。

### Step 4: 运行生成脚本

```bash
# Windows
UnityProj/Tools/gen_config.bat

# macOS/Linux
bash UnityProj/Tools/gen_config.sh
```

生成成功后检查：
- `Generated/` 下出现新的 `MyData.cs` + `TbMyData.cs` + 更新后的 `Tables.cs`
- `Assets/_Game/ConfigData/` 下出现 `tbmydata.bytes`
- `Resources/ConfigData/` 下出现副本

### Step 5: 代码中访问

```csharp
// 异步初始化（游戏启动时调用一次）
await ConfigManager.InitializeAsync();

// 访问数据
var item = ConfigManager.Tables.TbMyData.Get(1001);
Debug.Log(item.Name);

// 遍历
foreach (var kv in ConfigManager.Tables.TbMyData.DataMap)
{
    Debug.Log($"{kv.Key}: {kv.Value.Name}");
}
```

## SOP：修改现有表字段

1. 修改 `tables.xml` 中对应 `<bean>` 的 `<var>` 定义
2. 修改对应 xlsx 文件的 `##var` 行和 `##type` 行（增删列）
3. 运行 `gen_config.bat` / `.sh` 重新生成
4. 编译检查 C# 代码中是否有因字段变更导致的编译错误

## SOP：删除配置表

1. 从 `tables.xml` 中删除对应 `<bean>` 和 `<table>`
2. 删除 `DataTables/Datas/` 中对应的 xlsx 文件
3. 运行 `update_tables_extension.py` 或手动从 `TablesExtension.cs` 移除表名
4. 运行 `gen_config.bat` / `.sh` 重新生成（会自动清理旧生成文件）
5. 删除残留的 `.bytes` 和 `.json` 文件（如果生成脚本没有自动清理）
6. 清理 C# 代码中对已删除表的引用

## 常见陷阱与排错

详见 `references/luban-v4-format.md`，以下是最高频的坑：

1. **生成空代码** → `luban.conf` 中 `groups` 必须有至少一个 `"default": true` 的组
2. **xlsx 数据不被读取** → A 列必须有 `##var` / `##type` / `##` 标记行
3. **表名不匹配** → `TablesExtension.cs` 中的表名必须全小写且与 `tables.xml` 的 `name` 属性对应（去掉前缀后小写）
4. **YooAsset 加载失败** → 确认 `.bytes` 文件在 `Assets/_Game/ConfigData/` 下且已被 YooAsset Collector 收集
5. **运行时 ByteBuf 反序列化崩溃** → xlsx 中的数据类型必须与 `##type` 行声明一致（int 列不能有空字符串）

## Luban 支持的字段类型速查

| 类型 | 说明 | 示例值 |
|------|------|--------|
| `int` | 32位整数 | `100` |
| `long` | 64位整数 | `100000000` |
| `float` | 单精度浮点 | `3.14` |
| `double` | 双精度浮点 | `3.14159` |
| `bool` | 布尔 | `true` / `false` |
| `string` | 字符串 | `hello` |
| `text` | 本地化文本（key+text） | `key|text` |
| `datetime` | 日期时间 | `2025-01-01 00:00:00` |
| `vector2` | 二维向量 | `1.0,2.0` |
| `vector3` | 三维向量 | `1.0,2.0,3.0` |
| `list,int` | 整数列表 | `1,2,3` |
| `map,int,string` | 字典 | — |
| `枚举名` | 枚举引用 | `EnumValue` |
| `Bean名?` | 可空 Bean 引用 | — |
