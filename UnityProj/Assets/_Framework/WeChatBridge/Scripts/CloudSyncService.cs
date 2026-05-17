using System;
using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Cloud progress sync service (V3 — cloud-authoritative).
    /// Design: cloud is the single source of truth.
    ///   - Startup: pull cloud → overwrite local (no merge).
    ///   - Save: upload to cloud; on failure retry 3×, then block player
    ///     with a modal dialog until they tap "Retry" or kill the process.
    ///   - If player kills the process, the failed write is lost.
    ///     Next launch reads the last successful cloud state.
    /// </summary>
    public class CloudSyncService
    {
        private readonly WxAuthService _auth;
        private readonly IWeChatBridge _bridge;

        private bool _isSyncing;
        private int _retryCount;
        private const int MAX_RETRY = 3;
        private const float RETRY_BASE_DELAY = 2f; // exponential backoff: 2s, 4s, 8s

        public enum SyncState { Idle, Syncing, Failed }
        public SyncState State { get; private set; } = SyncState.Idle;

        /// <summary>Last successful sync timestamp (local realtimeSinceStartup).</summary>
        public float LastSyncTime { get; private set; } = -1f;

        /// <summary>
        /// Fires when upload fails after MAX_RETRY attempts.
        /// Upper layer should show a blocking "network error, tap to retry" dialog.
        /// The Action parameter is the retry callback — call it when user taps "Retry".
        /// </summary>
        public event Action<Action> OnUploadFailedNeedRetry;

        public CloudSyncService(WxAuthService auth, IWeChatBridge bridge)
        {
            _auth = auth;
            _bridge = bridge;
        }

        // ═══════════════════════════════════════════════════
        //  Pull — startup: cloud → local (no merge)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Pull cloud progress on startup. Cloud data overwrites local.
        /// If cloud has no data, returns empty string (new player / admin-reset).
        /// </summary>
        /// <param name="localData">Unused (kept for API compat). Cloud is authoritative — empty cloud = new player.</param>
        /// <param name="onComplete">(success, cloudJson). Empty string when cloud has no data.</param>
        public void PullCloudProgress(string localData, Action<bool, string> onComplete)
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
                    GameLog.LogWarning($"[CloudSync] Pull failed: {result}");
                    onComplete?.Invoke(false, localData);
                    return;
                }

                var cloudResult = JsonUtility.FromJson<GetProgressResult>(result);
                if (!cloudResult.success || cloudResult.data == null
                    || cloudResult.data.clearedLevels == null
                    || cloudResult.data.clearedLevels.Count == 0)
                {
                    // Cloud is authoritative. Empty cloud = new player (or admin-reset).
                    // Do NOT seed from local — that would undo intentional cloud wipes.
                    State = SyncState.Idle;
                    onComplete?.Invoke(true, "");
                    return;
                }

                // Cloud has data → it is authoritative. Return it to overwrite local.
                string cloudJson = JsonUtility.ToJson(cloudResult.data);
                State = SyncState.Idle;
                LastSyncTime = Time.realtimeSinceStartup;
                onComplete?.Invoke(true, cloudJson);
            });
        }

        // ═══════════════════════════════════════════════════
        //  Push — upload progress (blocking on failure)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Enqueue upload after level clear. Non-blocking on success.
        /// On failure: retries MAX_RETRY times, then fires OnUploadFailedNeedRetry
        /// to block player until they choose to retry.
        /// Uses "latest snapshot" mode — always uploads the most recent data.
        /// </summary>
        public void EnqueueUpload(string progressJson)
        {
            _latestProgressJson = progressJson;

            // Mark state IMMEDIATELY so WaitForIdleAsync() won't see a false "Idle"
            // before the async login/upload actually starts.
            State = SyncState.Syncing;

            if (!_auth.IsLoggedIn)
            {
                _auth.Login((success, _) =>
                {
                    if (success) DoUpload();
                    else NotifyUploadFailed();
                });
                return;
            }

            DoUpload();
        }

        private string _latestProgressJson;

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
                    OnUploadCompleted?.Invoke(LastSyncTime);

                    // Check if new data arrived during upload
                    if (_latestProgressJson != dataToUpload)
                    {
                        DoUpload();
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
                        NotifyUploadFailed();
                    }
                }
            });
        }

        /// <summary>
        /// Called when all automatic retries are exhausted.
        /// Fires OnUploadFailedNeedRetry so the UI layer can show a blocking dialog.
        /// The retry callback resets the counter and tries again.
        /// </summary>
        private void NotifyUploadFailed()
        {
            State = SyncState.Failed;
            GameLog.LogWarning("[CloudSync] Upload max retries exceeded — requesting user retry.");

            if (OnUploadFailedNeedRetry != null)
            {
                OnUploadFailedNeedRetry.Invoke(() =>
                {
                    // User tapped "Retry" — reset counter and go again.
                    // Keep State = Syncing (not Idle) to prevent WaitForIdleAsync
                    // from completing during the gap before DoUpload sets _isSyncing.
                    _retryCount = 0;
                    State = SyncState.Syncing;
                    GameLog.Log("[CloudSync] User initiated retry.");
                    DoUpload();
                });
            }
            else
            {
                // No listener — log and give up (shouldn't happen in production)
                GameLog.LogWarning("[CloudSync] No retry listener registered. Upload abandoned.");
            }
        }

        // ═══════════════════════════════════════════════════
        //  Async bridge — allow callers to await upload completion
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Fires after each successful upload. Parameter: realtimeSinceStartup of completion.
        /// </summary>
        public event Action<float> OnUploadCompleted;

        /// <summary>
        /// Await this to block until the current pending upload succeeds.
        /// If State is already Idle and no upload is in progress, completes immediately.
        ///
        /// NOTE: This method does NOT participate in retry UI — the global
        /// OnUploadFailedNeedRetry → NetworkRetryService mechanism handles that.
        /// This merely waits until the outcome resolves to "success" (regardless
        /// of how many retry cycles it took).
        ///
        /// Typical usage:
        /// <code>
        ///   cloudSync.EnqueueUpload(json);
        ///   await cloudSync.WaitForIdleAsync();
        ///   // upload confirmed
        /// </code>
        /// </summary>
        public Task WaitForIdleAsync()
        {
            if (State == SyncState.Idle && !_isSyncing)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>();

            void handler(float _)
            {
                OnUploadCompleted -= handler;
                tcs.TrySetResult(true);
            }

            OnUploadCompleted += handler;

            // Edge case: state transitioned to Idle between the check above and subscribing
            if (State == SyncState.Idle && !_isSyncing)
            {
                OnUploadCompleted -= handler;
                tcs.TrySetResult(true);
            }

            return tcs.Task;
        }

        // ═══════════════════════════════════════════════════
        //  Deserialization helper
        // ═══════════════════════════════════════════════════

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
