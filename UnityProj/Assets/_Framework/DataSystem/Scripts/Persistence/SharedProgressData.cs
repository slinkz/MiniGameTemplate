using System;
using System.Collections.Generic;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V3 progress data DTO (shared type — SG_TDD_06 §3.3 + TDD_02 S2.2).
    /// Used by both CloudSyncService (merge) and SG_ProgressManager (Load/Save).
    /// 
    /// v0.4: Promoted from SG_ProgressManager's internal private class to shared type.
    /// v0.5: V2 Sprint 2 — 技能/被动解锁 + 成就计数器 + 关卡星级。
    /// Field names must match JSON keys exactly for JsonUtility.
    /// </summary>
    [Serializable]
    public class SharedProgressData
    {
        public int version = 1;
        public List<int> clearedLevels = new List<int>();

        // ── V2 Sprint 2 新增 ──

        /// <summary>已解锁的技能 ID 列表（SkillConfigSO 的 asset name）</summary>
        public List<string> unlockedSkillIds = new List<string>();

        /// <summary>已解锁的被动 ID 列表（BuffConfigSO 的 asset name）</summary>
        public List<string> unlockedPassiveIds = new List<string>();

        /// <summary>累计死亡次数（Achievement ID=1 用）</summary>
        public int totalDeaths;

        /// <summary>单关最高击杀（Achievement ID=2 用）</summary>
        public int maxKillsInOneLevel;

        /// <summary>累计被命中次数（Achievement ID=3 用）</summary>
        public int totalHitsTaken;

        /// <summary>关卡最高星级（key=关卡编号 1-based, value=星数 1~3）</summary>
        public List<LevelStarEntry> levelStars = new List<LevelStarEntry>();
    }

    /// <summary>
    /// 关卡星级条目（JsonUtility 不支持 Dictionary，用 List 替代）。
    /// </summary>
    [Serializable]
    public struct LevelStarEntry
    {
        public int levelIndex;
        public int stars;
    }
}
