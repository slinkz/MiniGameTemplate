using System;
using UnityEngine;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V2 cloud-backed save system (SG_TDD_06 §4.2).
    /// Strategy: write instantly to local + async enqueue upload to cloud.
    /// Read: always from local (ms-level). Startup: one-time merge from cloud.
    /// 
    /// Replacement condition: WeChat Mini Game environment + cloud-dev configured.
    /// Non-WeChat environments automatically degrade to PlayerPrefsSaveSystem.
    /// </summary>
    public class CloudSaveSystem : ISaveSystem
    {
        private readonly PlayerPrefsSaveSystem _local;
        private readonly CloudSyncService _syncService;
        private readonly WxAuthService _authService;

        private bool _initialMergeDone;
        private const string PROGRESS_KEY = "sg_progress";

        public CloudSaveSystem(WxAuthService authService, IWeChatBridge bridge)
        {
            _local = new PlayerPrefsSaveSystem();
            _authService = authService;
            _syncService = new CloudSyncService(authService, bridge);
        }

        /// <summary>
        /// Start async login + cloud pull-merge. Non-blocking.
        /// Call once at startup after construction.
        /// </summary>
        public void InitCloudSync()
        {
            GameLog.Log("[CloudSave] InitCloudSync — calling _authService.Login()...");
            _authService.Login((success, openid) =>
            {
                if (!success)
                {
                    GameLog.LogWarning($"[CloudSave] Login failed (reason={openid}) — running in local-only mode.");
                    return;
                }

                // Pull cloud and merge
                string localProgress = _local.LoadString(PROGRESS_KEY, "");
                _syncService.PullAndMerge(localProgress, (merged, mergedJson) =>
                {
                    if (merged && mergedJson != localProgress)
                    {
                        // Cloud has newer data — write back to local
                        _local.SaveString(PROGRESS_KEY, mergedJson);
                        _local.FlushIfDirty();
                        GameLog.Log("[CloudSave] Cloud progress merged to local.");
                    }
                    _initialMergeDone = true;
                    // Notify upper layer to refresh
                    OnCloudMergeCompleted?.Invoke(mergedJson ?? localProgress);
                });
            });
        }

        // === Merge notification (TDD §4.2 / CS-002) ===

        /// <summary>
        /// Fires when cloud merge completes. UI layer listens to refresh display.
        /// Parameter: merged progress JSON.
        /// </summary>
        public event Action<string> OnCloudMergeCompleted;

        /// <summary>
        /// Hot-reload: re-pull cloud and merge. Call on wx.onShow (hot startup).
        /// </summary>
        public void Reload()
        {
            string localProgress = _local.LoadString(PROGRESS_KEY, "");

            if (!_authService.IsLoggedIn)
            {
                // Not logged in — just notify with local data
                OnCloudMergeCompleted?.Invoke(localProgress);
                return;
            }

            _syncService.PullAndMerge(localProgress, (merged, mergedJson) =>
            {
                if (merged && mergedJson != localProgress)
                {
                    _local.SaveString(PROGRESS_KEY, mergedJson);
                    _local.FlushIfDirty();
                }
                _initialMergeDone = true;
                OnCloudMergeCompleted?.Invoke(mergedJson ?? localProgress);
            });
        }

        // === ISaveSystem implementation (all delegate to _local + progress key triggers cloud) ===

        public void SaveString(string key, string value)
        {
            _local.SaveString(key, value);

            // Only progress data triggers cloud sync (intentional design — TDD §4.2 / CS-003)
            if (key == PROGRESS_KEY)
            {
                _syncService.EnqueueUpload(value);
            }
        }

        public string LoadString(string key, string defaultValue = "")
            => _local.LoadString(key, defaultValue);

        public void SaveInt(string key, int value) => _local.SaveInt(key, value);
        public int LoadInt(string key, int defaultValue = 0) => _local.LoadInt(key, defaultValue);
        public void SaveFloat(string key, float value) => _local.SaveFloat(key, value);
        public float LoadFloat(string key, float defaultValue = 0f) => _local.LoadFloat(key, defaultValue);
        public void SaveBool(string key, bool value) => _local.SaveBool(key, value);
        public bool LoadBool(string key, bool defaultValue = false) => _local.LoadBool(key, defaultValue);
        public bool HasKey(string key) => _local.HasKey(key);
        public void DeleteKey(string key) => _local.DeleteKey(key);
        public void DeleteAll() => _local.DeleteAll();
        public void Save() => _local.Save();
        public void FlushIfDirty() => _local.FlushIfDirty();
    }
}
