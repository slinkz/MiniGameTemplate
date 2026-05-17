using System;
using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Result type for a server request executed through <see cref="ServerRequestGate"/>.
    /// </summary>
    public enum ServerRequestResult
    {
        /// <summary>Server confirmed success — proceed with happy path.</summary>
        Success,

        /// <summary>Server explicitly rejected (e.g. cheat detection, invalid data).
        /// Not retriable — show reason and bail.</summary>
        BusinessRejected,

        /// <summary>Network/timeout failure — retriable via blocking retry dialog.
        /// NOTE: Callers of <see cref="ServerRequestGate.RequestAsync{T}"/> will NEVER
        /// see this value because the Gate handles retry internally. It exists only for
        /// the request delegate to communicate failure type back to the Gate.</summary>
        NetworkFailed
    }

    /// <summary>
    /// Wrapper for server responses flowing through <see cref="ServerRequestGate"/>.
    /// </summary>
    /// <typeparam name="T">Payload type on success.</typeparam>
    public class ServerResponse<T>
    {
        public ServerRequestResult Result;

        /// <summary>Valid when Result == Success.</summary>
        public T Data;

        /// <summary>Valid when Result == BusinessRejected. Human-readable reason for the player.</summary>
        public string RejectReason;

        /// <summary>Valid when Result == NetworkFailed. For logging/debugging only.</summary>
        public Exception NetworkError;

        // ----- Factory helpers -----

        public static ServerResponse<T> Ok(T data) => new ServerResponse<T>
        {
            Result = ServerRequestResult.Success,
            Data = data
        };

        public static ServerResponse<T> Rejected(string reason) => new ServerResponse<T>
        {
            Result = ServerRequestResult.BusinessRejected,
            RejectReason = reason
        };

        public static ServerResponse<T> NetFailed(Exception ex = null) => new ServerResponse<T>
        {
            Result = ServerRequestResult.NetworkFailed,
            NetworkError = ex
        };
    }

    /// <summary>
    /// Orchestrates "send request → show loading mask → await result → dispatch" as
    /// a single atomic operation from the player's perspective.
    ///
    /// Flow:
    ///   1. Show loading mask (blocks all input)
    ///   2. Invoke the request delegate
    ///   3a. Success → hide mask → return response (caller shows success UI)
    ///   3b. BusinessRejected → hide mask → return response (caller shows failure UI)
    ///   3c. NetworkFailed → hide mask → show blocking retry dialog → on retry → goto 1
    ///
    /// The caller NEVER receives NetworkFailed — it is handled internally via retry.
    /// The only ways to exit the retry loop are:
    ///   - A successful or business-rejected response arrives
    ///   - The player kills the process
    ///
    /// Dependencies:
    ///   - <see cref="LoadingMaskService"/> (loading mask display)
    ///   - <see cref="NetworkRetryService"/> (blocking retry dialog)
    /// Both must have their providers injected before calling RequestAsync.
    ///
    /// Thread safety: NOT thread-safe. Must be called from main thread only.
    /// </summary>
    public static class ServerRequestGate
    {
        private const string DEFAULT_LOADING_MSG = "正在与服务器通讯...";
        private const float REQUEST_TIMEOUT_SECONDS = 15f;

        /// <summary>
        /// Execute a server request with full loading mask + retry orchestration.
        /// Returns only Success or BusinessRejected — never NetworkFailed.
        /// </summary>
        /// <typeparam name="T">Payload type on success.</typeparam>
        /// <param name="request">
        /// Async delegate that performs the actual server call.
        /// Must return <see cref="ServerResponse{T}"/> indicating the result.
        /// If the delegate throws an exception, it is treated as NetworkFailed.
        /// </param>
        /// <param name="loadingMessage">Text to display on the loading mask.</param>
        /// <param name="timeoutSeconds">
        /// Max seconds to wait for the request before treating as NetworkFailed.
        /// Pass 0 or negative to disable timeout (not recommended).
        /// </param>
        public static async Task<ServerResponse<T>> RequestAsync<T>(
            Func<Task<ServerResponse<T>>> request,
            string loadingMessage = null,
            float timeoutSeconds = REQUEST_TIMEOUT_SECONDS)
        {
            string msg = loadingMessage ?? DEFAULT_LOADING_MSG;

            while (true)
            {
                // 1. Show loading mask
                LoadingMaskService.Show(msg);

                ServerResponse<T> response;
                try
                {
                    if (timeoutSeconds > 0)
                    {
                        response = await ExecuteWithTimeout(request, timeoutSeconds);
                    }
                    else
                    {
                        response = await request();
                    }
                }
                catch (Exception ex)
                {
                    // Any unhandled exception from the request → treat as network failure
                    GameLog.LogWarning($"[ServerRequestGate] Request threw exception: {ex.Message}");
                    response = ServerResponse<T>.NetFailed(ex);
                }

                // 2. Evaluate result
                if (response.Result != ServerRequestResult.NetworkFailed)
                {
                    // Success or BusinessRejected → hide mask and return
                    LoadingMaskService.Hide();
                    return response;
                }

                // 3. Network failure → hide mask → show blocking retry → loop
                LoadingMaskService.Hide();

                GameLog.LogWarning("[ServerRequestGate] Network failure detected. Showing retry dialog...");

                // Wait for player to tap "Retry" via a TaskCompletionSource bridge
                var retryTcs = new TaskCompletionSource<bool>();
                NetworkRetryService.ShowBlockingRetry(
                    retryAction: () => retryTcs.TrySetResult(true));

                // This awaits until the player taps Retry (or kills the process)
                await retryTcs.Task;

                // Player tapped retry — loop back to show mask + re-invoke request
                GameLog.Log("[ServerRequestGate] Player retried. Re-sending request...");
            }
        }

        /// <summary>
        /// Execute request with a timeout. If the timeout elapses first,
        /// returns NetworkFailed with a TimeoutException.
        /// </summary>
        private static async Task<ServerResponse<T>> ExecuteWithTimeout<T>(
            Func<Task<ServerResponse<T>>> request,
            float timeoutSeconds)
        {
            var requestTask = request();
            var delayMs = Mathf.RoundToInt(timeoutSeconds * 1000f);
            // NOTE: Task.Delay relies on UnitySynchronizationContext in WebGL (single-threaded).
            // It works as long as the PlayerLoop is ticking. If called while Time.timeScale == 0
            // and no unscaled tick is active, the delay may stall. Current callers ensure
            // timeScale is restored before reaching here, but keep this in mind for future use.
            var timeoutTask = Task.Delay(delayMs);

            var completedTask = await Task.WhenAny(requestTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timed out
                GameLog.LogWarning($"[ServerRequestGate] Request timed out after {timeoutSeconds}s.");
                return ServerResponse<T>.NetFailed(
                    new TimeoutException($"Server request timed out after {timeoutSeconds} seconds."));
            }

            // Request completed (may have faulted)
            return await requestTask;
        }

        /// <summary>Reset on domain reload.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // ServerRequestGate is stateless (no static fields to reset),
            // but we keep this method as a placeholder for future additions.
        }
    }
}
