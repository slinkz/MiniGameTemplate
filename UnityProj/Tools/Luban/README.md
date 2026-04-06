# Luban 配置表工具

## 简介
[Luban](https://github.com/focus-creative-games/luban) v4.6.0 是本项目使用的配置表解决方案。

## 安装
Luban CLI 已编译到 `UnityProj/Tools/Luban/` 目录下（通过 `dotnet publish` 从 v4.6.0 源码构建），无需全局安装。

前置依赖：.NET 8.0+ SDK（运行 `dotnet --version` 确认）。

## 双格式架构

本项目采用 **Binary 运行时 + JSON 编辑器预览** 的双格式方案：

| 格式 | 位置 | 用途 | 是否打包 |
|------|------|------|----------|
| Binary (`.bytes`) | `Assets/_Game/ConfigData/` | YooAsset 运行时加载 | ✅ |
| Binary (`.bytes`) | `Assets/_Framework/.../Resources/ConfigData/` | Resources fallback 加载 | ✅ |
| JSON (`.json`) | `Assets/_Framework/Editor/ConfigPreview/` | 编辑器人工查看明文 | ❌ |

- **运行时代码**使用 `cs-bin` 生成（ByteBuf 反序列化），性能高且数据不可人读
- **打包后不含任何明文** JSON 数据（JSON 在 Editor/ 目录下，Unity 不打包 Editor 内容）

## 使用
1. 在 `UnityProj/DataTables/Defs/tables.xml` 中定义 Bean 和 Table（XML）
2. 在 `UnityProj/DataTables/Datas/` 中编写数据（推荐 JSON 格式，Agent 友好）
3. 运行生成脚本：
   - Windows: `UnityProj/Tools/gen_config.bat`
   - macOS/Linux: `UnityProj/Tools/gen_config.sh`
4. 生成结果：
   - C# 代码 → `Assets/_Framework/DataSystem/Scripts/Config/Generated/`
   - Binary 数据 → `Assets/_Game/ConfigData/*.bytes` + `Resources/ConfigData/*.bytes`
   - JSON 预览 → `Assets/_Framework/Editor/ConfigPreview/*.json`

## 新增配置表流程
1. 在 `DataTables/Defs/tables.xml` 中新增 `<bean>` 和 `<table>` 定义
2. 在 `DataTables/Datas/` 中新增对应 JSON 数据文件
3. 更新 `TablesExtension.cs` 中的 `GetTableNames()` 方法，添加新表名
4. 运行生成脚本

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

### xlsx 数据源格式
Luban v4.x 推荐使用 xlsx 作为数据源，策划可以直接用 Excel 打开编辑：

| 行 | A列 | B列 | C列 | ... |
|---|------|------|------|-----|
| 1 | `##var` | 字段名1 | 字段名2 | ... |
| 2 | `##type` | int | string | ... |
| 3 | `##` | 中文注释 | 中文注释 | ... |
| 4+ | (空) | 数据 | 数据 | ... |

```xml
<table name="TbItem" value="Item" input="item.xlsx" mode="map" index="id"/>
```

## 生成代码命名空间
生成的 C# 代码在 `cfg` 命名空间下（由 `topModule` 控制）。
