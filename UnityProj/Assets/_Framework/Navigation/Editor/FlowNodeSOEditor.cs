using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Navigation;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// FlowNodeSO 自定义 Inspector：PanelTypeName 下拉 + Build Settings 校验。
    /// （PK ET-002/008）
    /// </summary>
    [CustomEditor(typeof(FlowNodeSO))]
    public class FlowNodeSOEditor : Editor
    {
        private string[] _panelKeys;
        private bool _panelKeysScanned;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all default properties
            DrawPropertiesExcluding(serializedObject, "m_Script");

            // Validation HelpBoxes
            var node = (FlowNodeSO)target;

            if (node.RequiredScene == null && node.UnloadSceneOnExit)
            {
                EditorGUILayout.HelpBox(
                    "UnloadSceneOnExit=true 但 RequiredScene 为空，此配置无意义。",
                    MessageType.Warning);
            }

            if (node.RequiredScene != null)
            {
                bool inBuildSettings = IsSceneInBuildSettings(node.RequiredScene.SceneName);
                if (!inBuildSettings)
                {
                    EditorGUILayout.HelpBox(
                        $"场景 '{node.RequiredScene.SceneName}' 不在 Build Settings 中！",
                        MessageType.Error);
                    if (GUILayout.Button("添加到 Build Settings"))
                        AddSceneToBuildSettings(node.RequiredScene.SceneName);
                }
            }

            if (string.IsNullOrEmpty(node.NodeId))
            {
                EditorGUILayout.HelpBox(
                    "NodeId 为空！栈序列化将无法识别此节点。点击下方按钮生成。",
                    MessageType.Error);
                if (GUILayout.Button("生成 NodeId"))
                {
                    var prop = serializedObject.FindProperty(FlowNodeSO.PROP_NODE_ID);
                    prop.stringValue = System.Guid.NewGuid().ToString("N");
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            return EditorBuildSettings.scenes.Any(s =>
                s.enabled && s.path.Contains(sceneName));
        }

        private static void AddSceneToBuildSettings(string sceneName)
        {
            var guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            if (guids.Length == 0)
            {
                Debug.LogError($"[FlowNodeSOEditor] Cannot find scene asset named '{sceneName}'.");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[FlowNodeSOEditor] Added '{path}' to Build Settings.");
        }
    }
}
