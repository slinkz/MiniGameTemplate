using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// WeChat silent login service (V2 — SG_TDD_06 §2.3).
    /// Uses cloud function "login" which auto-injects OPENID via wx.cloud.getWXContext().
    /// Thread safety: WebGL is single-threaded, no locks needed.
    /// </summary>
    public class WxAuthService
    {
        public enum AuthState { NotLoggedIn, LoggingIn, LoggedIn, Failed }

        private AuthState _state = AuthState.NotLoggedIn;
        private string _openId;
        private string _token;
        private float _tokenExpireTime; // Time.realtimeSinceStartup based

        private readonly IWeChatBridge _bridge;
        private readonly List<Action<bool, string>> _pendingCallbacks = new List<Action<bool, string>>(4);

        // Max consecutive failures before giving up for this session
        private int _failCount;
        private const int MAX_FAIL_COUNT = 3;

        public WxAuthService(IWeChatBridge bridge)
        {
            _bridge = bridge;
        }

        /// <summary>Current login state.</summary>
        public AuthState State => _state;

        /// <summary>Is logged in and token not expired?</summary>
        public bool IsLoggedIn => _state == AuthState.LoggedIn
                                  && Time.realtimeSinceStartup < _tokenExpireTime;

        /// <summary>Returns openid when logged in, null otherwise.</summary>
        public string OpenId => IsLoggedIn ? _openId : null;

        /// <summary>
        /// Initiate silent login. Safe to call multiple times — internally debounced.
        /// Implementation: directly calls "login" cloud function (cloud-dev auto-injects openid).
        /// </summary>
        public void Login(Action<bool, string> onComplete)
        {
            // Already gave up this session
            if (_failCount >= MAX_FAIL_COUNT)
            {
                GameLog.LogWarning("[WxAuth] Max login failures reached. Skipping.");
                onComplete?.Invoke(false, "max_failures_reached");
                return;
            }

            // De-duplicate concurrent calls
            if (_state == AuthState.LoggingIn)
            {
                _pendingCallbacks.Add(onComplete);
                return;
            }

            // Already logged in and token valid
            if (IsLoggedIn)
            {
                onComplete?.Invoke(true, _openId);
                return;
            }

            _state = AuthState.LoggingIn;
            _pendingCallbacks.Clear();
            _pendingCallbacks.Add(onComplete);

            // Call "login" cloud function — cloud-dev auto-injects OPENID, no wx.login code needed
            _bridge.CallCloudFunction("login", "{}", (success, result) =>
            {
                if (!success)
                {
                    _failCount++;
                    CompleteLogin(false, result ?? "cloud function failed");
                    return;
                }

                try
                {
                    var loginResult = JsonUtility.FromJson<LoginResult>(result);
                    _openId = loginResult.openid;
                    _token = loginResult.token;
                    _tokenExpireTime = Time.realtimeSinceStartup + loginResult.expireIn;
                    _failCount = 0; // Reset on success
                    GameLog.Log($"[WxAuth] Login success, openid={_openId?.Substring(0, Mathf.Min(8, _openId?.Length ?? 0))}...");
                    CompleteLogin(true, _openId);
                }
                catch (Exception ex)
                {
                    _failCount++;
                    GameLog.LogWarning($"[WxAuth] Login result parse failed: {ex.Message}");
                    CompleteLogin(false, "parse_error");
                }
            });
        }

        /// <summary>Refresh login if token expired.</summary>
        public void RefreshIfNeeded(Action<bool> onComplete = null)
        {
            if (IsLoggedIn)
            {
                onComplete?.Invoke(true);
                return;
            }
            Login((success, _) => onComplete?.Invoke(success));
        }

        // --- Internal ---

        private void CompleteLogin(bool success, string result)
        {
            _state = success ? AuthState.LoggedIn : AuthState.Failed;
            for (int i = 0; i < _pendingCallbacks.Count; i++)
            {
                _pendingCallbacks[i]?.Invoke(success, result);
            }
            _pendingCallbacks.Clear();
        }

        [Serializable]
        private struct LoginResult
        {
            public string openid;
            public string token;
            public int expireIn; // seconds
        }
    }
}
