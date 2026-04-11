# AnkleBreaker Unity MCP 本地冻结说明

## 为什么要冻结到项目内
- 上游通过 git URL 安装时，代码位于 `Library/PackageCache/`，任何重新解析包或版本变化都会覆盖临时补丁
- 当前项目使用 Unity 2021.3.17f1，而上游 `2.26.0` 实际使用了多处 Unity 2022.2+ API，直接编译会失败
- 因此本项目将该包冻结为本地 package：`Packages/com.anklebreaker.unity-mcp/`

## 当前冻结方式
- `Packages/manifest.json` 中依赖已改为：
  - `"com.anklebreaker.unity-mcp": "file:Packages/com.anklebreaker.unity-mcp"`
- 后续不要再从 Package Manager 直接升级这个包，除非先评估兼容性

## 当前补丁内容
### 1. 新增兼容层
- 文件：`Editor/MCPUnityCompat.cs`
- 作用：
  - Unity 2022.2+：继续走 `FindObjectsByType` / `FindFirstObjectByType`
  - Unity 2021.3：回退到 `Resources.FindObjectsOfTypeAll`
  - 对 2021.3 结果做场景对象过滤，排除 persistent asset / 未加载场景对象 / HideAndDontSave 对象

### 2. 已替换的命令文件
- `MCPAudioCommands.cs`
- `MCPComponentCommands.cs`
- `MCPGameObjectCommands.cs`
- `MCPLightingCommands.cs`
- `MCPNavigationCommands.cs`
- `MCPPrefabCommands.cs`
- `MCPSearchCommands.cs`
- `MCPSelectionCommands.cs`
- `MCPUICommands.cs`
- `MCPGraphicsCommands.cs`
- `MCPProfilerCommands.cs`
- `MCPUMACommands.cs`

## 后续维护规则
1. 若需要升级上游版本，先在独立分支验证 Unity 2021.3 编译
2. 升级后必须重新执行：
   - `code-review-checklist`
   - Unity CLI batchmode 编译检查
3. 若上游已正式修复 2021.3 兼容问题，再考虑移除本地补丁

## 已知注意事项
- 如果 Unity 正在打开本项目，batchmode 编译检查会报：`Multiple Unity instances cannot open the same project`
- `-logFile` 不要写到 `.tasks/` 这类点前缀目录，Unity 会判定为非法目录
