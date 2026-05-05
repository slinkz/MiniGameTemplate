using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Navigation;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// AppFlowNavigator 自定义 Inspector + 独立 EditorWindow。
    /// PlayMode 栈可视化 + Pop/PopAll/Push 快速操作。（PK UA-009/ET-001）
    /// </summary>
    [CustomEditor(typeof(AppFlowNavigator))]
    public class AppFlowNavigatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var nav = (AppFlowNavigator)target;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请进入播放模式查看导航栈", MessageType.Info);
                DrawDefaultInspector();
                return;
            }

            // 状态指示器
            if (nav.IsTransitioning)
            {
                EditorGUILayout.HelpBox("⚠️ Transition 进行中...", MessageType.Warning);
            }

            // 栈表格
            EditorGUILayout.LabelField("Navigation Stack", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            var stack = nav.Stack;
            if (stack.Count == 0)
            {
                EditorGUILayout.LabelField("(空栈)");
            }
            else
            {
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    var entry = stack[i];
                    string prefix = i == stack.Count - 1 ? "▶ " : "  ";
                    string nodeName = entry.Node != null ? entry.Node.DisplayName : "(null)";
                    string dataStr = entry.Data?.ToString() ?? "—";
                    EditorGUILayout.LabelField($"{prefix}[{i}] {nodeName}", dataStr);
                }
            }

            EditorGUILayout.EndVertical();

            // 快速操作按钮
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(nav.IsTransitioning);

            if (GUILayout.Button("Pop (返回上一级)") && nav.StackDepth > 1)
            {
                nav.Pop();
            }

            if (GUILayout.Button("PopAll (回到根节点)") && nav.StackDepth > 1)
            {
                _ = nav.PopAllAsync();
            }

            EditorGUI.EndDisabledGroup();

            // 强制刷新
            if (Application.isPlaying)
                Repaint();
        }
    }

    /// <summary>
    /// 独立 EditorWindow 入口。（PK ET-001）
    /// </summary>
    public class AppFlowNavigatorWindow : EditorWindow
    {
        [MenuItem("Tools/AppFlow/Navigator")]
        public static void ShowWindow()
        {
            GetWindow<AppFlowNavigatorWindow>("AppFlow Navigator");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请进入播放模式查看导航栈", MessageType.Info);
                return;
            }

            var nav = AppFlowNavigator.Instance;
            if (nav == null)
            {
                EditorGUILayout.HelpBox("AppFlowNavigator 未初始化", MessageType.Warning);
                return;
            }

            // 状态
            EditorGUILayout.LabelField("Current Node", nav.CurrentNode?.DisplayName ?? "(none)");
            EditorGUILayout.LabelField("Stack Depth", nav.StackDepth.ToString());
            EditorGUILayout.LabelField("Transitioning", nav.IsTransitioning.ToString());

            EditorGUILayout.Space();

            // 栈
            var stack = nav.Stack;
            EditorGUILayout.LabelField("Stack (top → bottom)", EditorStyles.boldLabel);
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                var entry = stack[i];
                string nodeName = entry.Node != null ? entry.Node.DisplayName : "(null)";
                EditorGUILayout.LabelField($"  [{i}] {nodeName}");
            }

            EditorGUILayout.Space();

            // 操作
            EditorGUI.BeginDisabledGroup(nav.IsTransitioning);
            if (GUILayout.Button("Pop") && nav.StackDepth > 1)
                nav.Pop();
            if (GUILayout.Button("PopAll") && nav.StackDepth > 1)
                _ = nav.PopAllAsync();
            EditorGUI.EndDisabledGroup();

            if (Application.isPlaying)
                Repaint();
        }
    }
}
