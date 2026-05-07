using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Data;

namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 进度管理——封装 ISaveSystem 读写。
    /// 纯 C# 类（无 MonoBehaviour）。
    /// TDD_03 §2.2 / TDD_06 §8.2（V2 新增 Reload）
    /// 
    /// 生命周期（TDD_03 §2.0）：
    ///   创建者：Boot 场景 GameStartupFlow.Awake()
    ///   跨场景共享：GameStartupFlow.Progress 静态属性
    ///   ISaveSystem 来源：GameBootstrapper.SaveSystem
    /// </summary>
    public class SG_ProgressManager
    {
        private const string SAVE_KEY = "sg_progress";
        private const int CURRENT_VERSION = 2; // V2: cloud sync

        private readonly ISaveSystem _saveSystem;
        private SharedProgressData _data;

        public SG_ProgressManager(ISaveSystem saveSystem)
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

        // ── V2 新增（TDD_06 §8.2）──

        /// <summary>
        /// 重新从 ISaveSystem 加载最新数据。
        /// 用于云端 merge 完成后刷新内存状态。
        /// </summary>
        public void Reload()
        {
            Load();
        }

        // ── 内部方法 ──

        private void Load()
        {
            string json = _saveSystem.LoadString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                _data = new SharedProgressData { version = CURRENT_VERSION };
                return;
            }

            try
            {
                _data = JsonUtility.FromJson<SharedProgressData>(json);
                if (_data == null)
                {
                    _data = new SharedProgressData { version = CURRENT_VERSION };
                }
                else if (_data.version < CURRENT_VERSION)
                {
                    MigrateData(_data);
                }
            }
            catch
            {
                Debug.LogWarning("[SG_ProgressManager] 存档数据损坏，重置");
                _data = new SharedProgressData { version = CURRENT_VERSION };
            }
        }

        private void Save()
        {
            string json = JsonUtility.ToJson(_data);
            _saveSystem.SaveString(SAVE_KEY, json);
            _saveSystem.FlushIfDirty();
        }

        private void MigrateData(SharedProgressData data)
        {
            // V1 → V2: just bump version, data format is the same
            data.version = CURRENT_VERSION;
        }

        /// <summary>清除所有进度（调试用）</summary>
        public void ResetAll()
        {
            _data = new SharedProgressData { version = CURRENT_VERSION };
            Save();
        }
    }
}
