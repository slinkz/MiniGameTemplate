# Luban 配置表工具

## 简介
[Luban](https://github.com/focus-creative-games/luban) 是本项目使用的配置表解决方案。

## 安装
```bash
# 需要 .NET 8.0+ SDK
dotnet tool install -g luban.client
```

## 使用
1. 在 `UnityProj/DataTables/Defs/` 中定义表结构
2. 在 `UnityProj/DataTables/Datas/` 中编写数据（推荐 JSON 格式，Agent友好）
3. 运行生成脚本：
   - Windows: `UnityProj/Tools/gen_config.bat`
   - macOS/Linux: `UnityProj/Tools/gen_config.sh`
4. 生成的 C# 代码 → `UnityProj/Assets/_Framework/DataSystem/Scripts/Config/Generated/`
5. 生成的数据文件 → `UnityProj/Assets/_Framework/DataSystem/Resources/ConfigData/`

## 新增配置表流程
1. 在 `Defs/` 中新增 `.xml` 定义文件
2. 在 `__tables__.xml` 中注册该表
3. 在 `Datas/` 中新增对应数据文件
4. 运行生成脚本
