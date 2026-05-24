using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V3 cloud-backed save system (cloud-authoritative with progressive merge).
    /// Strategy:
    ///   - Write: instantly to local + async upload to cloud.
    ///   - Read: always from local (ms-level).
    ///   - Startup: pull cloud → progressive merge with local (stars=max, levels=union).
    ///   - Upload failure: block player with retry dialog (handled by UI via event).
    ///
    /// Progressive merge rationale: "cloud-authoritative" still applies as the baseline,
    /// but incremental data (star ratings, cleared levels) uses max/union to prevent
    /// data loss from the upload race window (local saved 3★, but upload hadn't completed
    /// before process was killed → next launch cloud still has 2★).
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
        /// Cloud data merged with local on success (progressive merge).
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

                // Pull cloud → progressive merge with local
                string localProgress = _local.LoadString(PROGRESS_KEY, "");
                _syncService.PullCloudProgress(localProgress, (pulled, cloudJson) =>
                {
                    if (pulled)
                    {
                        string mergedJson = MergeProgressData(localProgress, cloudJson);
                        _local.SaveString(PROGRESS_KEY, mergedJson);
                        _local.FlushIfDirty();

                        if (!string.IsNullOrEmpty(mergedJson))
                            GameLog.Log("[CloudSave] Cloud progress pulled → merged with local.");
                        else
                            GameLog.Log("[CloudSave] Cloud is empty, local is empty → fresh start.");

                        // If merge produced different data than cloud, re-upload so cloud catches up
                        if (!string.IsNullOrEmpty(mergedJson) && mergedJson != cloudJson)
                        {
                            GameLog.Log("[CloudSave] Local had newer data → re-uploading merged result.");
                            _syncService.EnqueueUpload(mergedJson);
                        }

                        OnCloudPullCompleted?.Invoke(mergedJson);
                    }
                    else
                    {
                        OnCloudPullCompleted?.Invoke(localProgress);
                    }
                });
            });
        }

        // === Progressive Merge ===

        /// <summary>
        /// Merge local and cloud progress data using progressive rules:
        ///   - clearedLevels: union (never un-clear a level)
        ///   - levelStars: max per level (never downgrade stars)
        ///   - counters (totalDeaths, maxKillsInOneLevel, totalHitsTaken): max
        ///   - unlocked IDs: union
        ///   - version: max
        /// Cloud is still "authoritative" in the sense that cloud provides the baseline,
        /// but local increments that haven't been uploaded yet are preserved.
        /// </summary>
        private static string MergeProgressData(string localJson, string cloudJson)
        {
            // Both empty → empty
            if (string.IsNullOrEmpty(localJson) && string.IsNullOrEmpty(cloudJson))
                return "";

            // One side empty → use the other
            if (string.IsNullOrEmpty(cloudJson)) return localJson;
            if (string.IsNullOrEmpty(localJson)) return cloudJson;

            SharedProgressData local = null;
            SharedProgressData cloud = null;

            try { local = JsonUtility.FromJson<SharedProgressData>(localJson); } catch { }
            try { cloud = JsonUtility.FromJson<SharedProgressData>(cloudJson); } catch { }

            if (local == null && cloud == null) return "";
            if (local == null) return cloudJson;
            if (cloud == null) return localJson;

            // Start from cloud as baseline (cloud-authoritative)
            var merged = cloud;

            // clearedLevels: union
            if (local.clearedLevels != null)
            {
                foreach (int level in local.clearedLevels)
                {
                    if (!merged.clearedLevels.Contains(level))
                        merged.clearedLevels.Add(level);
                }
            }

            // levelStars: max per level
            if (local.levelStars != null)
            {
                foreach (var localEntry in local.levelStars)
                {
                    bool found = false;
                    for (int i = 0; i < merged.levelStars.Count; i++)
                    {
                        if (merged.levelStars[i].levelIndex == localEntry.levelIndex)
                        {
                            if (localEntry.stars > merged.levelStars[i].stars)
                            {
                                merged.levelStars[i] = new LevelStarEntry
                                {
                                    levelIndex = localEntry.levelIndex,
                                    stars = localEntry.stars,
                                };
                            }
                            found = true;
                            break;
                        }
                    }
                    if (!found && localEntry.stars > 0)
                    {
                        merged.levelStars.Add(localEntry);
                    }
                }
            }

            // Counters: max
            merged.totalDeaths = Math.Max(merged.totalDeaths, local.totalDeaths);
            merged.maxKillsInOneLevel = Math.Max(merged.maxKillsInOneLevel, local.maxKillsInOneLevel);
            merged.totalHitsTaken = Math.Max(merged.totalHitsTaken, local.totalHitsTaken);

            // Unlocked IDs: union
            if (local.unlockedSkillIds != null)
            {
                foreach (string id in local.unlockedSkillIds)
                {
                    if (merged.unlockedSkillIds == null)
                        merged.unlockedSkillIds = new List<string>();
                    if (!merged.unlockedSkillIds.Contains(id))
                        merged.unlockedSkillIds.Add(id);
                }
            }
            if (local.unlockedPassiveIds != null)
            {
                foreach (string id in local.unlockedPassiveIds)
                {
                    if (merged.unlockedPassiveIds == null)
                        merged.unlockedPassiveIds = new List<string>();
                    if (!merged.unlockedPassiveIds.Contains(id))
                        merged.unlockedPassiveIds.Add(id);
                }
            }

            // Version: max
            merged.version = Math.Max(merged.version, local.version);

            return JsonUtility.ToJson(merged);
        }

        // === Cloud pull notification ===

        /// <summary>
        /// Fires when cloud pull completes. UI layer listens to refresh display.
        /// Parameter: merged progress JSON.
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
