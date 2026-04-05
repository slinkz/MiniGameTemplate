namespace MiniGameTemplate.Data
{
    /// <summary>
    /// Interface for local data persistence.
    /// Swap implementations without touching game logic.
    /// </summary>
    public interface ISaveSystem
    {
        void SaveInt(string key, int value);
        int LoadInt(string key, int defaultValue = 0);

        void SaveFloat(string key, float value);
        float LoadFloat(string key, float defaultValue = 0f);

        void SaveString(string key, string value);
        string LoadString(string key, string defaultValue = "");

        void SaveBool(string key, bool value);
        bool LoadBool(string key, bool defaultValue = false);

        bool HasKey(string key);
        void DeleteKey(string key);
        void DeleteAll();

        /// <summary>
        /// Flush pending writes to disk.
        /// </summary>
        void Save();
    }
}
