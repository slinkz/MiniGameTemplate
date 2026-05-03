using System.Collections.Generic;
using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 进度管理——封装 ISaveSystem 读写。
    /// 纯 C# 类（无 MonoBehaviour）。
    /// TDD_03 §2.2
    /// 
    /// 生命周期（TDD_03 §2.0）：
    ///   创建者：Boot 场景 GameStartupFlow.Awake()
    ///   跨场景共享：GameStartupFlow.Progress 静态属性
    ///   ISaveSystem 来源：GameBootstrapper.SaveSystem
    /// </summary>
    public class SG_ProgressManager
    {
        private const string SAVE_KEY = "sg_progress";
        private const int CURRENT_VERSION = 1;

        private readonly MiniGameTemplate.Data.ISaveSystem _saveSystem;
        private ProgressData _data;

        [System.Serializable]
        private class ProgressData
        {
            public int version = CURRENT_VERSION;
            public List<int> clearedLevels = new List<int>();
        }

        public SG_ProgressManager(MiniGameTemplate.Data.ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
            Load();
        }

        // ── 查询 ──

        /// <summary>指定关卡是否已通关（1-based）</summary>
        public bool IsLevelCleared(int levelIndex)
        {
            return _data.clearedLevels.Contains(levelIndex);
        }

        /// <summary>指定关卡是否可进入</summary>
        public bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex <= 1) return true;
            return IsLevelCleared(levelIndex - 1);
        }

        /// <summary>最大已解锁关卡（1-based）</summary>
        public int MaxUnlockedLevel(int totalLevels)
        {
            int max = 1;
            for (int i = 1; i <= totalLevels; i++)
            {
                if (IsLevelUnlocked(i)) max = i;
            }
            return max;
        }

        // ── 写入 ──

        /// <summary>标记关卡通关并持久化</summary>
        public void MarkLevelCleared(int levelIndex)
        {
            if (!_data.clearedLevels.Contains(levelIndex))
            {
                _data.clearedLevels.Add(levelIndex);
                Save();
            }
        }

        // ── 内部方法 ──

        private void Load()
        {
            string json = _saveSystem.LoadString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                _data = new ProgressData();
                return;
            }

            try
            {
                _data = JsonUtility.FromJson<ProgressData>(json);
                if (_data.version < CURRENT_VERSION)
                    MigrateData(_data);
            }
            catch
            {
                Debug.LogWarning("[SG_ProgressManager] 存档数据损坏，重置");
                _data = new ProgressData();
            }
        }

        private void Save()
        {
            string json = JsonUtility.ToJson(_data);
            _saveSystem.SaveString(SAVE_KEY, json);
            _saveSystem.FlushIfDirty();
        }

        private void MigrateData(ProgressData data)
        {
            data.version = CURRENT_VERSION;
        }

        /// <summary>清除所有进度（调试用）</summary>
        public void ResetAll()
        {
            _data = new ProgressData();
            Save();
        }
    }
}
