using System;
using UnityEngine;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V3 cloud-backed save system (cloud-authoritative).
    /// Strategy:
    ///   - Write: instantly to local + async upload to cloud.
    ///   - Read: always from local (ms-level).
    ///   - Startup: pull cloud → overwrite local (no merge).
    ///   - Upload failure: block player with retry dialog (handled by UI via event).
    ///
    /// Replacement condition: WeChat Mini Game environment + cloud-dev configured.
    /// Non-WeChat environments automatically degrade to PlayerPrefsSaveSystem.
    /// </summary>
    public class CloudSaveSystem : ISaveSystem
    {
        private readonly PlayerPrefsSaveSystem _local;
        private readonly CloudSyncService _syncService;
        private readonly WxAuthService _authService;

        private const string PROGRESS_KEY = "sg_progress";

        public CloudSaveSystem(WxAuthService authService, IWeChatBridge bridge)
        {
            _local = new PlayerPrefsSaveSystem();
            _authService = authService;
            _syncService = new CloudSyncService(authService, bridge);
        }

        /// <summary>
        /// Expose the sync service so upper layers can subscribe to
        /// OnUploadFailedNeedRetry and show a blocking retry dialog.
        /// </summary>
        public CloudSyncService SyncService => _syncService;

        /// <summary>
        /// Start async login + cloud pull. Non-blocking.
        /// Cloud data overwrites local on success (cloud-authoritative).
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

                // Pull cloud → overwrite local (cloud is authoritative, no merge)
                string localProgress = _local.LoadString(PROGRESS_KEY, "");
                _syncService.PullCloudProgress(localProgress, (pulled, cloudJson) =>
                {
                    if (pulled)
                    {
                        // Cloud is authoritative — always overwrite local, even with empty.
                        // This ensures admin-reset / cloud wipe propagates to local cache.
                        _local.SaveString(PROGRESS_KEY, cloudJson ?? "");
                        _local.FlushIfDirty();

                        if (!string.IsNullOrEmpty(cloudJson))
                            GameLog.Log("[CloudSave] Cloud progress pulled → local overwritten.");
                        else
                            GameLog.Log("[CloudSave] Cloud is empty → local cache cleared.");
                    }
                    OnCloudPullCompleted?.Invoke(pulled ? (cloudJson ?? "") : localProgress);
                });
            });
        }

        // === Cloud pull notification ===

        /// <summary>
        /// Fires when cloud pull completes. UI layer listens to refresh display.
        /// Parameter: progress JSON from cloud (or local fallback).
        /// </summary>
        public event Action<string> OnCloudPullCompleted;

        // === ISaveSystem implementation ===

        public void SaveString(string key, string value)
        {
            _local.SaveString(key, value);

            // Only progress data triggers cloud sync
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
