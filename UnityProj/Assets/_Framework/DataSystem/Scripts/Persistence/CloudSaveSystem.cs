using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V4 cloud-authoritative save system (memory + cloud ONLY, no local storage).
    /// 
    /// Core Principles (2026-05-24 redesign):
    ///   - Cloud is the SINGLE source of truth. Local storage is NEVER used.
    ///   - Startup: pull cloud → load into memory. If cloud is empty → new player.
    ///   - Write: update memory → upload to cloud. NEVER write to local.
    ///   - Network failure: retry in-process. If process dies → data lost (acceptable).
    ///   - Local data is untrusted (can be tampered). We never read or write it.
    ///   - Startup pull is MANDATORY: game MUST NOT proceed until cloud data is loaded.
    ///     On failure → auto-retry 3× with exponential backoff → block player with retry dialog.
    ///
    /// Environment split:
    ///   - Editor: PlayerPrefsSaveSystem (debug only, no cloud).
    ///   - WeChat (DevTools + real device): this class (cloud-only).
    /// </summary>
    public class CloudSaveSystem : ISaveSystem
    {
        private readonly CloudSyncService _syncService;
        private readonly WxAuthService _authService;

        private const string PROGRESS_KEY = "sg_progress";
        private const int STARTUP_MAX_RETRY = 3;
        private const float STARTUP_RETRY_BASE_DELAY = 2f; // exponential backoff: 2s, 4s, 8s

        // In-memory store: the ONLY place runtime data lives.
        // Key → JSON string. Only PROGRESS_KEY is cloud-synced.
        private readonly Dictionary<string, string> _memoryStore = new Dictionary<string, string>();

        // Integer/Float/Bool stores (non-cloud, memory-only, for ISaveSystem compat)
        private readonly Dictionary<string, int> _intStore = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _floatStore = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> _boolStore = new Dictionary<string, bool>();

        private int _startupRetryCount;

        /// <summary>
        /// True after cloud pull completes successfully.
        /// Game code MUST wait for this before trusting any data.
        /// </summary>
        public bool IsCloudReady { get; private set; }

        public CloudSaveSystem(WxAuthService authService, IWeChatBridge bridge)
        {
            _authService = authService;
            _syncService = new CloudSyncService(authService, bridge);
        }

        /// <summary>
        /// Expose the sync service so upper layers can subscribe to
        /// OnUploadFailedNeedRetry and show a blocking retry dialog.
        /// </summary>
        public CloudSyncService SyncService => _syncService;

        // ═══════════════════════════════════════════════════
        //  Startup: Login + Pull cloud → memory (MANDATORY, blocking retry)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Start async login + cloud pull. Non-blocking call, but the game
        /// MUST wait for IsCloudReady before proceeding to gameplay.
        /// On failure: auto-retries with exponential backoff, then fires
        /// OnStartupPullFailedNeedRetry for UI layer to show a blocking dialog.
        /// Call once at startup after construction.
        /// </summary>
        public void InitCloudSync()
        {
            _startupRetryCount = 0;
            DoStartupLogin();
        }

        private void DoStartupLogin()
        {
            GameLog.Log("[CloudSave V4] InitCloudSync — calling _authService.Login()...");
            _authService.Login((success, openid) =>
            {
                if (!success)
                {
                    GameLog.LogWarning($"[CloudSave V4] Login failed (reason={openid}).");
                    HandleStartupFailure("login");
                    return;
                }

                DoStartupPull();
            });
        }

        private void DoStartupPull()
        {
            _syncService.PullCloudProgress("", (pulled, cloudJson) =>
            {
                if (pulled)
                {
                    // Cloud is authoritative. Whatever it says, we use.
                    if (!string.IsNullOrEmpty(cloudJson))
                    {
                        _memoryStore[PROGRESS_KEY] = cloudJson;
                        GameLog.Log("[CloudSave V4] Cloud data loaded into memory.");
                    }
                    else
                    {
                        // Cloud is empty → new player or admin-reset. Memory stays empty.
                        _memoryStore.Remove(PROGRESS_KEY);
                        GameLog.Log("[CloudSave V4] Cloud is empty → fresh start.");
                    }

                    IsCloudReady = true;
                    _startupRetryCount = 0;
                    OnCloudPullCompleted?.Invoke(cloudJson ?? "");
                }
                else
                {
                    GameLog.LogWarning("[CloudSave V4] Cloud pull failed.");
                    HandleStartupFailure("pull");
                }
            });
        }

        /// <summary>
        /// Handles startup login/pull failure: auto-retry with exponential backoff,
        /// then escalate to UI layer via OnStartupPullFailedNeedRetry.
        /// </summary>
        private void HandleStartupFailure(string phase)
        {
            _startupRetryCount++;

            if (_startupRetryCount <= STARTUP_MAX_RETRY)
            {
                float delay = STARTUP_RETRY_BASE_DELAY * Mathf.Pow(2, _startupRetryCount - 1);
                GameLog.LogWarning($"[CloudSave V4] Startup {phase} failed, auto-retry {_startupRetryCount}/{STARTUP_MAX_RETRY} in {delay}s");
                TimerService.Instance.Delay(delay, () => DoStartupLogin(), true);
            }
            else
            {
                // All automatic retries exhausted → ask UI layer to show blocking dialog
                GameLog.LogWarning("[CloudSave V4] Startup pull max retries exceeded — requesting user retry.");
                OnStartupPullFailedNeedRetry?.Invoke(() =>
                {
                    // User tapped "Retry" → reset counter and start from login again
                    _startupRetryCount = 0;
                    GameLog.Log("[CloudSave V4] User initiated startup retry.");
                    DoStartupLogin();
                });
            }
        }

        // ═══════════════════════════════════════════════════
        //  Cloud pull notification
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Fires when cloud pull completes successfully. UI layer listens to refresh display.
        /// Parameter: cloud progress JSON (empty string if cloud has no data — new player).
        /// This event ONLY fires on success. On failure, see OnStartupPullFailedNeedRetry.
        /// </summary>
        public event Action<string> OnCloudPullCompleted;

        /// <summary>
        /// Fires when startup pull fails after all automatic retries are exhausted.
        /// UI layer should show a blocking "network error, tap to retry" dialog.
        /// The Action parameter is the retry callback — call it when user taps "Retry".
        /// This resets the retry counter and restarts the entire Login → Pull sequence.
        /// </summary>
        public event Action<Action> OnStartupPullFailedNeedRetry;

        // ═══════════════════════════════════════════════════
        //  ISaveSystem implementation (memory + cloud upload)
        // ═══════════════════════════════════════════════════

        public void SaveString(string key, string value)
        {
            _memoryStore[key] = value;

            // Only progress data triggers cloud upload
            if (key == PROGRESS_KEY)
            {
                _syncService.EnqueueUpload(value);
            }
        }

        public string LoadString(string key, string defaultValue = "")
        {
            return _memoryStore.TryGetValue(key, out string val) ? val : defaultValue;
        }

        public void SaveInt(string key, int value) => _intStore[key] = value;
        public int LoadInt(string key, int defaultValue = 0)
            => _intStore.TryGetValue(key, out int val) ? val : defaultValue;

        public void SaveFloat(string key, float value) => _floatStore[key] = value;
        public float LoadFloat(string key, float defaultValue = 0f)
            => _floatStore.TryGetValue(key, out float val) ? val : defaultValue;

        public void SaveBool(string key, bool value) => _boolStore[key] = value;
        public bool LoadBool(string key, bool defaultValue = false)
            => _boolStore.TryGetValue(key, out bool val) ? val : defaultValue;

        public bool HasKey(string key)
        {
            return _memoryStore.ContainsKey(key)
                || _intStore.ContainsKey(key)
                || _floatStore.ContainsKey(key)
                || _boolStore.ContainsKey(key);
        }

        public void DeleteKey(string key)
        {
            _memoryStore.Remove(key);
            _intStore.Remove(key);
            _floatStore.Remove(key);
            _boolStore.Remove(key);
        }

        public void DeleteAll()
        {
            _memoryStore.Clear();
            _intStore.Clear();
            _floatStore.Clear();
            _boolStore.Clear();
            IsCloudReady = false;
        }

        // No-op: there is no local storage to flush.
        public void Save() { }
        public void FlushIfDirty() { }
    }
}
