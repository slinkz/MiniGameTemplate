using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// PlayerPrefs-based implementation of ISaveSystem.
    /// Simple and reliable for small games.
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

        public void SaveInt(string key, int value) { PlayerPrefs.SetInt(key, value); MarkDirty(); }
        public int LoadInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        public void SaveFloat(string key, float value) { PlayerPrefs.SetFloat(key, value); MarkDirty(); }
        public float LoadFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SaveString(string key, string value) { PlayerPrefs.SetString(key, value); MarkDirty(); }
        public string LoadString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        public void SaveBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); MarkDirty(); }
        public bool LoadBool(string key, bool defaultValue = false) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
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
