#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Danmaku.Editor
{
    /// <summary>
    /// Custom inspector for DanmakuSystem Phase 4 workflow tools.
    /// </summary>
    [CustomEditor(typeof(DanmakuSystem))]
    public class DanmakuSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Phase 4 Tools", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(Application.isPlaying == false && EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Run Controlled Refresh"))
                {
                    DanmakuEditorRefreshCoordinator.RunControlledRefreshMenu();
                }
            }

            var report = DanmakuEditorRefreshCoordinator.GetLastReport();
            if (report.LastRefreshTime != default)
            {
                EditorGUILayout.HelpBox(
                    $"Last Refresh: {report.LastRefreshTime:HH:mm:ss}\nDirty: {report.DirtyAssetCount}\nRegistries: {report.RebuiltRegistryCount}\nBatches: {report.WarmedBatchCount}\nSuccess: {report.Success}\nReason: {report.Reason}",
                    report.Success ? MessageType.Info : MessageType.Error);
            }
        }
    }
}
#endif
