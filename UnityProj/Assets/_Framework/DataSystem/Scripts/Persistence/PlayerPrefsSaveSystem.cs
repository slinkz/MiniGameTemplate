using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// PlayerPrefs-based implementation of ISaveSystem.
    /// Simple and reliable for small games.
    /// </summary>
    public class PlayerPrefsSaveSystem : ISaveSystem
    {
        public void SaveInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public int LoadInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        public void SaveFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public float LoadFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SaveString(string key, string value) => PlayerPrefs.SetString(key, value);
        public string LoadString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        public void SaveBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
        public bool LoadBool(string key, bool defaultValue = false) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void DeleteAll() => PlayerPrefs.DeleteAll();

        public void Save() => PlayerPrefs.Save();
    }
}
