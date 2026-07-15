---
system: navigation
scope: appflow-tdd-editor-tools
parent: APPFLOW_TDD_INDEX
last_verified: 2026-05-07
---

# AppFlow TDD — §3.5 编辑器工具规格

> 父文档：[SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md](SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md)  
> PK 来源：ET-001/002/003/006/008/009

---

## 3.5.1 AppFlowNavigatorEditor（CustomEditor + EditorWindow）

| 项目 | 决策 |
|------|------|
| 类型 | `[CustomEditor(typeof(AppFlowNavigator))]` Inspector 内嵌 + `MenuItem("Tools/AppFlow/Navigator")` 独立 EditorWindow 入口 |
| 渲染 | IMGUI（与项目其他 Editor 工具一致，轻量） |
| 显示内容 | 栈列表表格：Index / Node.DisplayName / Data?.ToString() / 进入时间戳 |
| 操作按钮 | Pop（弹出栈顶）/ PopAll（回根）/ Push 预设下拉（从项目 FlowNodeSO 资产列表动态获取） |
| 刷新机制 | 事件驱动：订阅 `EditorOnNavigated` + `EditorApplication.update` 仅 PlayMode 启用 `Repaint()` |
| 错误高亮 | `_isTransitioning == true` 持续 > 3s 时红色 HelpBox 警告 |
| 非 PlayMode | 显示 "请进入播放模式查看导航栈" |

## 3.5.2 FlowNodeSOEditor（CustomEditor）

```csharp
// Assets/_Framework/Navigation/Editor/FlowNodeSOEditor.cs
[CustomEditor(typeof(FlowNodeSO))]
public class FlowNodeSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // _panelTypeName：下拉列表（数据源=项目中所有 public const string PanelKey）
        // fallback：手动输入模式（如果扫描不到 PanelKey 定义）
        DrawPanelTypeDropdown();

        // _requiredScene：标准 ObjectField + Build Settings 校验
        DrawSceneField();

        // 一致性校验 HelpBox
        ValidateConfiguration();

        serializedObject.ApplyModifiedProperties();
    }

    private void ValidateConfiguration()
    {
        var node = (FlowNodeSO)target;

        // 无意义配置
        if (node.RequiredScene == null && node.UnloadSceneOnExit)
            EditorGUILayout.HelpBox("UnloadSceneOnExit=true 但 RequiredScene 为空，此配置无意义。", MessageType.Warning);

        // Build Settings 校验
        if (node.RequiredScene != null)
        {
            bool inBuildSettings = /* EditorBuildSettings.scenes 遍历 */;
            if (!inBuildSettings)
            {
                EditorGUILayout.HelpBox($"场景 '{node.RequiredScene.SceneName}' 不在 Build Settings 中！", MessageType.Error);
                if (GUILayout.Button("添加到 Build Settings"))
                    AddSceneToBuildSettings(node.RequiredScene);
            }
        }
    }
}
```

## 3.5.3 FlowNodeSO.OnValidate（编辑期即时校验）

```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    // 1. PanelTypeName 格式校验
    if (!string.IsNullOrEmpty(_panelTypeName) && _panelTypeName.Contains(' '))
        Debug.LogWarning($"[FlowNodeSO] '{name}': PanelTypeName 不应包含空格，请使用 PascalCase。");

    // 2. 无意义配置
    if (_requiredScene == null && _unloadSceneOnExit)
        Debug.LogWarning($"[FlowNodeSO] '{name}': UnloadSceneOnExit=true 但 RequiredScene 为空。");

    // 3. DisplayName 自动填充
    if (string.IsNullOrEmpty(_displayName))
        _displayName = name;
}
#endif
```

## 3.5.4 面板注册验证工具 + 构建守护

```csharp
// Assets/_Framework/Navigation/Editor/AppFlowBuildValidator.cs

// === MenuItem 手动验证 ===
[MenuItem("Tools/AppFlow/Validate Panel Registration")]
private static void ValidatePanelRegistration()
{
    // 1. 收集所有 FlowNodeSO 的 _panelTypeName
    // 2. 正则扫描项目源文件中 RegisterPanelOpener("xxx" 调用
    // 3. 交叉对比 → 输出未匹配列表到 Console
}

// === 构建守护 ===
public class AppFlowBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 10;

    public void OnPreprocessBuild(BuildReport report)
    {
        var errors = new List<string>();

        // 1. 所有 FlowNodeSO._panelTypeName 有对应注册代码
        // 2. 所有 FlowNodeSO._requiredScene 在 Build Settings 中已启用
        // 3. GameStartupFlow 引用的 root 节点 SO 存在

        if (errors.Count > 0)
            throw new BuildFailedException($"[AppFlow] 构建验证失败：\n" + string.Join("\n", errors));

        Debug.Log("[AppFlow] 构建验证通过。");
    }
}
```

## 3.5.5 Hierarchy Icon + Scene Gizmo

```csharp
// Assets/_Framework/Navigation/Editor/AppFlowHierarchyIcon.cs
[InitializeOnLoad]
static class AppFlowHierarchyIcon
{
    static AppFlowHierarchyIcon()
    {
        EditorApplication.hierarchyWindowItemOnGUI += DrawIcon;
    }

    private static void DrawIcon(int instanceID, Rect selectionRect)
    {
        // Navigator GO：绿色（idle）/ 黄色（transitioning）/ 红色（超时）
        var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;
        var nav = go.GetComponent<AppFlowNavigator>();
        if (nav == null) return;

        var color = nav.IsTransitioning ? Color.yellow : Color.green;
        var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 16, 16);
        EditorGUI.DrawRect(iconRect, color);
    }
}

// IFlowHandler 实现者 Gizmo（在实现类中）
private void OnDrawGizmos()
{
    #if UNITY_EDITOR
    UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
        $"[FlowNode: {AppFlowNavigator.Instance?.CurrentNode?.DisplayName}]");
    #endif
}
```
