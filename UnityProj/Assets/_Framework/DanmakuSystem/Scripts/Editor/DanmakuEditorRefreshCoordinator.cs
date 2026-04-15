#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MiniGameTemplate.Rendering;
using MiniGameTemplate.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniGameTemplate.Danmaku.Editor
{
    /// <summary>
    /// Coordinates the fixed editor refresh workflow: dirty -> registry rebuild -> batch warmup -> result report.
    /// </summary>
    [InitializeOnLoad]
    public static class DanmakuEditorRefreshCoordinator
    {
        private const string MENU_ROOT = "Tools/MiniGameTemplate/Danmaku/";
        private const string LOG_PREFIX = "[DanmakuEditorRefresh]";

        private static readonly HashSet<UnityEngine.Object> DirtyAssets = new();
        private static RefreshReport _lastReport;
        private static bool _refreshQueued;
        private static bool _isRefreshing;

        static DanmakuEditorRefreshCoordinator()
        {
            EditorApplication.delayCall += FlushQueuedRefresh;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Marks an asset dirty for the controlled refresh workflow.
        /// </summary>
        public static void MarkDirty(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            DirtyAssets.Add(asset);
            QueueRefresh();
        }

        /// <summary>
        /// Returns the latest refresh report snapshot.
        /// </summary>
        public static RefreshReport GetLastReport()
        {
            return _lastReport;
        }

        [MenuItem(MENU_ROOT + "Run Controlled Refresh")]
        public static void RunControlledRefreshMenu()
        {
            RunControlledRefresh(force: true, reason: "manual menu");
        }

        [MenuItem(MENU_ROOT + "Clear Refresh Report")]
        public static void ClearReportMenu()
        {
            _lastReport = default;
            Debug.Log(LOG_PREFIX + " cleared last report.");
        }

        private static void QueueRefresh()
        {
            if (_refreshQueued)
                return;

            _refreshQueued = true;
            EditorApplication.delayCall += FlushQueuedRefresh;
        }

        private static void FlushQueuedRefresh()
        {
            _refreshQueued = false;

            if (_isRefreshing || DirtyAssets.Count == 0)
                return;

            if (Application.isPlaying)
                return;

            RunControlledRefresh(force: false, reason: "queued dirty assets");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (DirtyAssets.Count == 0)
                return;

            RunControlledRefresh(force: true, reason: "before entering play mode");
        }

        private static void RunControlledRefresh(bool force, string reason)
        {
            if (_isRefreshing)
                return;

            if (!force && DirtyAssets.Count == 0)
                return;

            _isRefreshing = true;
            try
            {
                var report = new RefreshReport
                {
                    DirtyAssetCount = DirtyAssets.Count,
                    LastRefreshTime = DateTime.Now,
                    Reason = reason,
                };

                bool registryOk = RebuildRegistries(ref report);
                if (registryOk)
                {
                    bool batchOk = WarmupBatches(ref report);
                    report.Success = batchOk;
                }
                else
                {
                    report.Success = false;
                }

                if (report.Success)
                {
                    DirtyAssets.Clear();
                }

                _lastReport = report;
                LogReport(report);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private static bool RebuildRegistries(ref RefreshReport report)
        {
            try
            {
                string[] danmakuGuids = AssetDatabase.FindAssets("t:DanmakuTypeRegistry");
                foreach (string guid in danmakuGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var registry = AssetDatabase.LoadAssetAtPath<DanmakuTypeRegistry>(path);
                    if (registry == null)
                        continue;

                    registry.AssignRuntimeIndices();
                    EditorUtility.SetDirty(registry);
                    report.RebuiltRegistryCount++;
                }

                string[] vfxGuids = AssetDatabase.FindAssets("t:VFXTypeRegistrySO");
                foreach (string guid in vfxGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var registry = AssetDatabase.LoadAssetAtPath<VFXTypeRegistrySO>(path);
                    if (registry == null)
                        continue;

                    registry.RebuildRuntimeIndices();
                    EditorUtility.SetDirty(registry);
                    report.RebuiltRegistryCount++;
                }

                AssetDatabase.SaveAssets();
                return true;
            }
            catch (Exception ex)
            {
                report.Failures.Add("Registry rebuild failed: " + ex.Message);
                return false;
            }
        }

        private static bool WarmupBatches(ref RefreshReport report)
        {
            try
            {
                string[] danmakuSystemGuids = AssetDatabase.FindAssets("t:GameObject DanmakuSystem");
                foreach (string guid in danmakuSystemGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                        continue;

                    var system = prefab.GetComponent<DanmakuSystem>();
                    if (system == null)
                        continue;

                    if (TryWarmupDanmakuPrefab(system, ref report))
                        report.WarmedBatchCount++;
                }

                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                foreach (string guid in sceneGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try
                    {
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            var systems = root.GetComponentsInChildren<DanmakuSystem>(true);
                            foreach (var system in systems)
                            {
                                if (TryWarmupDanmakuPrefab(system, ref report))
                                    report.WarmedBatchCount++;
                            }
                        }
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }

                return report.Failures.Count == 0;
            }
            catch (Exception ex)
            {
                report.Failures.Add("Batch warmup failed: " + ex.Message);
                return false;
            }
        }

        private static bool TryWarmupDanmakuPrefab(DanmakuSystem system, ref RefreshReport report)
        {
            if (system == null)
                return false;

            try
            {
                system.EditorWarmupBatches();
                return true;
            }
            catch (Exception ex)
            {
                report.Failures.Add(system.name + " warmup failed: " + ex.Message);
                return false;
            }
        }

        private static void LogReport(RefreshReport report)
        {
            string summary = $"{LOG_PREFIX} dirty={report.DirtyAssetCount}, registries={report.RebuiltRegistryCount}, batches={report.WarmedBatchCount}, success={report.Success}, reason={report.Reason}";
            if (report.Failures.Count == 0)
            {
                Debug.Log(summary);
                return;
            }

            Debug.LogError(summary + "\n - " + string.Join("\n - ", report.Failures));
        }

        /// <summary>
        /// Snapshot of the latest controlled refresh execution.
        /// </summary>
        public struct RefreshReport
        {
            public int DirtyAssetCount;
            public int RebuiltRegistryCount;
            public int WarmedBatchCount;
            public bool Success;
            public DateTime LastRefreshTime;
            public string Reason;
            public List<string> Failures => _failures ??= new List<string>();

            private List<string> _failures;
        }
    }
}
#endif
