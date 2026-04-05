using System.Text;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// PlayerPrefs-based implementation of ISaveSystem.
    /// Simple and reliable for small games.
    ///
    /// Security: All values are stored alongside an HMAC-SHA256 signature to detect
    /// client-side tampering. The HMAC key is device-specific — not a secret from a
    /// determined attacker, but raises the bar significantly above plain-text editing.
    ///
    /// Performance: Save() is throttled — multiple calls within the throttle window
    /// are batched into a single PlayerPrefs.Save(). Direct Save operations are
    /// deferred by marking dirty; call FlushIfDirty() from a periodic tick or scene transition.
    /// </summary>
    public class PlayerPrefsSaveSystem : ISaveSystem
    {
        private bool _dirty;
        private float _lastSaveTime;

        /// <summary>
        /// Minimum interval between actual disk writes (seconds).
        /// Default 1 second — prevents rapid-fire save storms.
        /// </summary>
        public float SaveThrottleSeconds = 1f;

        // --- HMAC Integrity Protection ---
        // The key is derived from device-specific info. Not unbreakable, but prevents
        // casual vConsole / localStorage edits. For competitive games, validate server-side.
        private static byte[] _hmacKey;
        private const string HMAC_SUFFIX = "__hmac";

        private static byte[] GetHmacKey()
        {
            if (_hmacKey != null) return _hmacKey;

            // Combine device identifier + application identifier for a per-game, per-device key.
            // On WebGL/WeChat, SystemInfo.deviceUniqueIdentifier returns a stable browser fingerprint.
            string seed = SystemInfo.deviceUniqueIdentifier + "|" + Application.identifier;
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                _hmacKey = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
            }
            return _hmacKey;
        }

        private static string ComputeHmac(string key, string value)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256(GetHmacKey()))
            {
                byte[] data = Encoding.UTF8.GetBytes(key + ":" + value);
                byte[] hash = hmac.ComputeHash(data);
                // Use a compact hex string (32 chars)
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static bool VerifyHmac(string key, string value)
        {
            string storedHmac = PlayerPrefs.GetString(key + HMAC_SUFFIX, "");
            if (string.IsNullOrEmpty(storedHmac)) return false;
            string expected = ComputeHmac(key, value);
            // Constant-time comparison to prevent timing attacks
            if (storedHmac.Length != expected.Length) return false;
            int diff = 0;
            for (int i = 0; i < storedHmac.Length; i++)
                diff |= storedHmac[i] ^ expected[i];
            return diff == 0;
        }

        private static void StoreWithHmac(string key, string valueForHmac)
        {
            PlayerPrefs.SetString(key + HMAC_SUFFIX, ComputeHmac(key, valueForHmac));
        }

        // --- Save Methods (with HMAC) ---

        public void SaveInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            StoreWithHmac(key, value.ToString());
            MarkDirty();
        }

        public int LoadInt(string key, int defaultValue = 0)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue;
            int value = PlayerPrefs.GetInt(key, defaultValue);
            if (!VerifyHmac(key, value.ToString()))
            {
                Utils.GameLog.LogWarning($"[SaveSystem] HMAC mismatch for key '{key}' — possible tampering. Returning default.");
                return defaultValue;
            }
            return value;
        }

        public void SaveFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            StoreWithHmac(key, value.ToString("R")); // Round-trip format for exact float repr
            MarkDirty();
        }

        public float LoadFloat(string key, float defaultValue = 0f)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue;
            float value = PlayerPrefs.GetFloat(key, defaultValue);
            if (!VerifyHmac(key, value.ToString("R")))
            {
                Utils.GameLog.LogWarning($"[SaveSystem] HMAC mismatch for key '{key}' — possible tampering. Returning default.");
                return defaultValue;
            }
            return value;
        }

        public void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            StoreWithHmac(key, value ?? "");
            MarkDirty();
        }

        public string LoadString(string key, string defaultValue = "")
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue;
            string value = PlayerPrefs.GetString(key, defaultValue);
            if (!VerifyHmac(key, value ?? ""))
            {
                Utils.GameLog.LogWarning($"[SaveSystem] HMAC mismatch for key '{key}' — possible tampering. Returning default.");
                return defaultValue;
            }
            return value;
        }

        public void SaveBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            StoreWithHmac(key, value ? "1" : "0");
            MarkDirty();
        }

        public bool LoadBool(string key, bool defaultValue = false)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue;
            int raw = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0);
            if (!VerifyHmac(key, raw.ToString()))
            {
                Utils.GameLog.LogWarning($"[SaveSystem] HMAC mismatch for key '{key}' — possible tampering. Returning default.");
                return defaultValue;
            }
            return raw == 1;
        }

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + HMAC_SUFFIX);
        }

        public void DeleteAll() => PlayerPrefs.DeleteAll();

        /// <summary>
        /// Request a disk save. Throttled: if called too frequently, the actual
        /// PlayerPrefs.Save() is deferred until the throttle window elapses.
        /// Call FlushIfDirty() periodically (e.g., each frame or scene transition) to ensure writes complete.
        /// </summary>
        public void Save()
        {
            float now = Time.realtimeSinceStartup;
            if (now - _lastSaveTime >= SaveThrottleSeconds)
            {
                PlayerPrefs.Save();
                _lastSaveTime = now;
                _dirty = false;
            }
            else
            {
                _dirty = true;
            }
        }

        /// <summary>
        /// Force flush if there are pending dirty writes. Call on scene transitions, pause, or quit.
        /// </summary>
        public void FlushIfDirty()
        {
            if (!_dirty) return;
            PlayerPrefs.Save();
            _lastSaveTime = Time.realtimeSinceStartup;
            _dirty = false;
        }

        private void MarkDirty()
        {
            _dirty = true;
        }
    }
}
