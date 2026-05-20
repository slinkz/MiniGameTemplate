using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Data;

namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 进度管理——封装 ISaveSystem 读写。
    /// 纯 C# 类（无 MonoBehaviour）。
    /// TDD_03 §2.2 / TDD_06 §8.2（V2 新增 Reload）/ TDD_02 S2.2（V2 Sprint 2 成就/解锁）
    /// 
    /// 生命周期（TDD_03 §2.0）：
    ///   创建者：Boot 场景 GameStartupFlow.Awake()
    ///   跨场景共享：GameStartupFlow.Progress 静态属性
    ///   ISaveSystem 来源：GameBootstrapper.SaveSystem
    /// </summary>
    public class SG_ProgressManager
    {
        private const string SAVE_KEY = "sg_progress";
        private const int CURRENT_VERSION = 3; // V3: Sprint 2 achievements + unlocks

        private readonly ISaveSystem _saveSystem;
        private SharedProgressData _data;

        public SG_ProgressManager(ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
            Load();
        }

        // ── 关卡查询 ──

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

        // ── 关卡写入 ──

        /// <summary>标记关卡通关并持久化</summary>
        public void MarkLevelCleared(int levelIndex)
        {
            if (!_data.clearedLevels.Contains(levelIndex))
            {
                _data.clearedLevels.Add(levelIndex);
                Save();
            }
        }

        // ── V2 Sprint 2: 成就查询 ──

        /// <summary>
        /// 检查指定成就 ID 是否达成。
        /// Achievement ID 定义：1=累计死亡5次, 2=单关击杀50, 3=累计被命中30次
        /// </summary>
        public bool IsAchievementMet(int achievementId)
        {
            switch (achievementId)
            {
                case 1: return _data.totalDeaths >= 5;
                case 2: return _data.maxKillsInOneLevel >= 50;
                case 3: return _data.totalHitsTaken >= 30;
                default: return false;
            }
        }

        /// <summary>累计死亡次数</summary>
        public int TotalDeaths => _data.totalDeaths;
        /// <summary>单关最高击杀</summary>
        public int MaxKillsInOneLevel => _data.maxKillsInOneLevel;
        /// <summary>累计被命中次数</summary>
        public int TotalHitsTaken => _data.totalHitsTaken;

        // ── V2 Sprint 2: 成就计数器 ──

        /// <summary>记录一次死亡（Defeat 时调用）</summary>
        public void RecordDeath()
        {
            _data.totalDeaths++;
            Save();
        }

        /// <summary>更新单关击杀记录（Victory 时调用）</summary>
        public void UpdateMaxKills(int killsThisLevel)
        {
            if (killsThisLevel > _data.maxKillsInOneLevel)
            {
                _data.maxKillsInOneLevel = killsThisLevel;
                Save();
            }
        }

        /// <summary>记录被命中次数（伤害转发时调用）</summary>
        public void RecordHit()
        {
            _data.totalHitsTaken++;
            // 不立即 Save，等关卡结束时统一保存（避免每次命中都 flush）
        }

        /// <summary>将当前挂起的计数器变更持久化（关卡结束时调用）</summary>
        public void FlushCounters()
        {
            Save();
        }

        // ── V2 Sprint 2: 关卡星级 ──

        /// <summary>获取指定关卡最高星级（无记录返回 0）</summary>
        public int GetLevelStars(int levelIndex)
        {
            for (int i = 0; i < _data.levelStars.Count; i++)
            {
                if (_data.levelStars[i].levelIndex == levelIndex)
                    return _data.levelStars[i].stars;
            }
            return 0;
        }

        /// <summary>更新关卡星级（仅在高于已有记录时更新）</summary>
        public void UpdateLevelStars(int levelIndex, int stars)
        {
            for (int i = 0; i < _data.levelStars.Count; i++)
            {
                if (_data.levelStars[i].levelIndex == levelIndex)
                {
                    if (stars > _data.levelStars[i].stars)
                    {
                        _data.levelStars[i] = new LevelStarEntry
                        {
                            levelIndex = levelIndex,
                            stars = stars,
                        };
                        Save();
                    }
                    return;
                }
            }
            // 新关卡
            _data.levelStars.Add(new LevelStarEntry { levelIndex = levelIndex, stars = stars });
            Save();
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
            // V1/V2 → V3: 新字段有默认值（0/空列表），直接升版本号
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
