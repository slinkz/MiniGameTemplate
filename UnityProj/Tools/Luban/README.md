# Luban 配置表工具

## 简介
[Luban](https://github.com/focus-creative-games/luban) v4.6.0 是本项目使用的配置表解决方案。

> 💡 **AI Agent**：处理 Luban 配置表相关任务时，请先加载 `luban-config` Skill（`.codebuddy/skills/luban-config/`），里面有完整的 SOP、格式规范和自动化脚本。

## 安装
Luban CLI 已编译到 `UnityProj/Tools/Luban/` 目录下（通过 `dotnet publish` 从 v4.6.0 源码构建），无需全局安装。

前置依赖：.NET 8.0+ SDK（运行 `dotnet --version` 确认）。

## 双格式架构

本项目采用 **Binary 运行时 + JSON 编辑器预览** 的双格式方案：

| 格式 | 位置 | 用途 | 是否打包 |
|------|------|------|----------|
| Binary (`.bytes`) | `Assets/_Game/ConfigData/` | YooAsset 运行时加载 | ✅ |
| JSON (`.json`) | `Assets/_Framework/Editor/ConfigPreview/` | 编辑器人工查看明文 | ❌ |

- **运行时代码**使用 `cs-bin` 生成（ByteBuf 反序列化），性能高且数据不可人读
- **打包后不含任何明文** JSON 数据（JSON 在 Editor/ 目录下，Unity 不打包 Editor 内容）

## 使用
1. 在 `DataTables/Defs/tables.xml` 中定义 Bean 和 Table（XML）
2. 在 `DataTables/Datas/` 中编写数据（**xlsx 格式**，策划可用 Excel 编辑）
3. 运行生成脚本：
   - Windows: `Tools/gen_config.bat`
   - macOS/Linux: `Tools/gen_config.sh`
4. 生成结果：
   - C# 代码 → `Assets/_Framework/DataSystem/Scripts/Config/Generated/`
   - Binary 数据 → `Assets/_Game/ConfigData/*.bytes`
   - JSON 预览 → `Assets/_Framework/Editor/ConfigPreview/*.json`

## 新增配置表流程

1. 在 `DataTables/Defs/tables.xml` 中新增 `<bean>` 和 `<table>` 定义
2. 创建 xlsx 数据文件（可用 Skill 脚本自动化）：
   ```bash
   python .codebuddy/skills/luban-config/scripts/create_xlsx.py \
     -o DataTables/Datas/xxx.xlsx -s TbXxx -f "id:int:ID,name:string:名称"
   ```
3. 自动更新 `TablesExtension.cs`（可用 Skill 脚本）：
   ```bash
   python .codebuddy/skills/luban-config/scripts/update_tables_extension.py --project-root .
   ```
4. 运行生成脚本：`Tools/gen_config.bat`

## xlsx 数据源格式

| 行 | A列 | B列 | C列 | ... |
|---|------|------|------|-----|
| 1 | `##var` | 字段名1 | 字段名2 | ... |
| 2 | `##type` | int | string | ... |
| 3 | `##` | 中文注释 | 中文注释 | ... |
| 4+ | (空) | 数据 | 数据 | ... |

```xml
<table name="TbItem" value="Item" input="item.xlsx" mode="map" index="id"/>
```

## 配置文件说明

### luban.conf (v4.6.0 格式)
```json
{
    "groups": [{"names": ["c"], "default": true}],
    "schemaFiles": [{"fileName": "Defs", "type": ""}],
    "dataDir": "Datas",
    "targets": [{
        "name": "all",
        "manager": "Tables",
        "groups": ["c"],
        "topModule": "cfg"
    }]
}
```

⚠️ `groups` 必须有至少一个 `"default": true` 的组，否则导出为空！

## 生成代码命名空间
生成的 C# 代码在 `cfg` 命名空间下（由 `topModule` 控制）。
