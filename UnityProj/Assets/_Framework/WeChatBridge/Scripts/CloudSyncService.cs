using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Cloud progress sync service (V2 — SG_TDD_06 §3.5).
    /// Responsibility: async upload/download progress to WeChat Cloud DB.
    /// Design: write-after-local + startup pull-merge.
    /// </summary>
    public class CloudSyncService
    {
        private readonly WxAuthService _auth;
        private readonly IWeChatBridge _bridge;

        private bool _isSyncing;
        private bool _hasPendingUpload;
        private int _retryCount;
        private const int MAX_RETRY = 3;
        private const float RETRY_BASE_DELAY = 2f; // exponential backoff: 2s, 4s, 8s

        public enum SyncState { Idle, Syncing, Failed }
        public SyncState State { get; private set; } = SyncState.Idle;

        /// <summary>Last successful sync timestamp (local realtimeSinceStartup).</summary>
        public float LastSyncTime { get; private set; } = -1f;

        public CloudSyncService(WxAuthService auth, IWeChatBridge bridge)
        {
            _auth = auth;
            _bridge = bridge;
        }

        /// <summary>
        /// Pull cloud progress on startup and merge with local.
        /// </summary>
        /// <param name="localData">Current local save JSON.</param>
        /// <param name="onComplete">(mergeSuccess, mergedJson)</param>
        public void PullAndMerge(string localData, Action<bool, string> onComplete)
        {
            if (!_auth.IsLoggedIn)
            {
                onComplete?.Invoke(false, localData);
                return;
            }

            State = SyncState.Syncing;
            _bridge.CallCloudFunction("getProgress", "{}", (success, result) =>
            {
                if (!success)
                {
                    State = SyncState.Failed;
                    GameLog.LogWarning($"[CloudSync] PullAndMerge failed: {result}");
                    onComplete?.Invoke(false, localData);
                    return;
                }

                // result = JSON.stringify(cloud function return value)
                // Cloud function returns { success: true, data: { version, clearedLevels, ... } }
                var cloudResult = JsonUtility.FromJson<GetProgressResult>(result);
                if (!cloudResult.success || cloudResult.data == null
                    || cloudResult.data.clearedLevels == null
                    || cloudResult.data.clearedLevels.Count == 0)
                {
                    // Cloud has no data — first time, upload local
                    if (!string.IsNullOrEmpty(localData))
                    {
                        EnqueueUpload(localData);
                    }
                    State = SyncState.Idle;
                    onComplete?.Invoke(true, localData);
                    return;
                }

                // Union merge
                string cloudJson = JsonUtility.ToJson(cloudResult.data);
                string merged = MergeProgress(localData, cloudJson);
                State = SyncState.Idle;
                LastSyncTime = Time.realtimeSinceStartup;
                onComplete?.Invoke(true, merged);
            });
        }

        /// <summary>
        /// Enqueue upload after level clear. Non-blocking.
        /// Uses "latest snapshot" mode — always uploads the most recent data.
        /// </summary>
        public void EnqueueUpload(string progressJson)
        {
            _latestProgressJson = progressJson;
            _hasPendingUpload = true;

            if (!_auth.IsLoggedIn)
            {
                // Login then retry
                _auth.Login((success, _) =>
                {
                    if (success) DoUpload();
                });
                return;
            }

            DoUpload();
        }

        private string _latestProgressJson; // Latest pending snapshot

        private void DoUpload()
        {
            if (_isSyncing) return;
            _isSyncing = true;
            State = SyncState.Syncing;

            string dataToUpload = _latestProgressJson;

            _bridge.CallCloudFunction("saveProgress", dataToUpload, (success, result) =>
            {
                _isSyncing = false;

                if (success)
                {
                    _retryCount = 0;
                    State = SyncState.Idle;
                    LastSyncTime = Time.realtimeSinceStartup;

                    // Check if new data arrived during upload
                    if (_latestProgressJson != dataToUpload)
                    {
                        DoUpload();
                    }
                    else
                    {
                        _hasPendingUpload = false;
                    }
                }
                else
                {
                    _retryCount++;
                    if (_retryCount < MAX_RETRY)
                    {
                        float delay = RETRY_BASE_DELAY * Mathf.Pow(2, _retryCount - 1);
                        GameLog.LogWarning($"[CloudSync] Upload failed, retry {_retryCount}/{MAX_RETRY} in {delay}s");
                        TimerService.Instance.Delay(delay, () => DoUpload(), true);
                    }
                    else
                    {
                        State = SyncState.Failed;
                        GameLog.LogWarning("[CloudSync] Upload max retries exceeded. Giving up for this session.");
                    }
                }
            });
        }

        /// <summary>
        /// Union merge: take the union of clearedLevels from both sources.
        /// GC note: called at startup (1x) + rare hot-reload. Frequency too low to worry.
        /// </summary>
        private static string MergeProgress(string localJson, string cloudJson)
        {
            SharedProgressData localData = null;
            SharedProgressData cloudData = null;

            if (!string.IsNullOrEmpty(localJson))
            {
                try { localData = JsonUtility.FromJson<SharedProgressData>(localJson); }
                catch { /* corrupt local, treat as empty */ }
            }

            if (!string.IsNullOrEmpty(cloudJson))
            {
                try { cloudData = JsonUtility.FromJson<SharedProgressData>(cloudJson); }
                catch { /* corrupt cloud, treat as empty */ }
            }

            if (localData == null) localData = new SharedProgressData();
            if (cloudData == null) cloudData = new SharedProgressData();

            // Union
            var merged = new HashSet<int>(localData.clearedLevels);
            if (cloudData.clearedLevels != null)
            {
                for (int i = 0; i < cloudData.clearedLevels.Count; i++)
                    merged.Add(cloudData.clearedLevels[i]);
            }

            localData.clearedLevels = new List<int>(merged);
            localData.clearedLevels.Sort();
            localData.version = 2; // Upgrade version

            return JsonUtility.ToJson(localData);
        }

        /// <summary>
        /// Deserialization target for getProgress cloud function return value.
        /// JSON shape: { "success": true, "data": { "version": 2, "clearedLevels": [...] } }
        /// </summary>
        [Serializable]
        private class GetProgressResult
        {
            public bool success;
            public SharedProgressData data;
        }
    }
}
