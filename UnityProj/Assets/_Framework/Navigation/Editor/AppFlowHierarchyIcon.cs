using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Navigation;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Hierarchy 窗口中 AppFlowNavigator 的状态图标。（PK ET-006）
    /// 绿色 = idle, 黄色 = transitioning
    /// </summary>
    [InitializeOnLoad]
    internal static class AppFlowHierarchyIcon
    {
        static AppFlowHierarchyIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIcon;
        }

        private static void DrawIcon(int instanceID, Rect selectionRect)
        {
            if (!Application.isPlaying) return;

            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            var nav = go.GetComponent<AppFlowNavigator>();
            if (nav == null) return;

            var color = nav.IsTransitioning ? Color.yellow : Color.green;
            var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 14, 14);

            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(iconRect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
