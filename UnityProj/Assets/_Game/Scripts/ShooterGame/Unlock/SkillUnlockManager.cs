using System.Collections.Generic;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 技能解锁管理器——纯 C# 服务，不继承 MonoBehaviour。
    /// 查询 SkillUnlockTableSO / PassiveUnlockTableSO + SG_ProgressManager 计算解锁状态。
    /// TDD_02 S2.1
    /// </summary>
    public class SkillUnlockManager
    {
        private readonly SkillUnlockTableSO _skillTable;
        private readonly PassiveUnlockTableSO _passiveTable;
        private readonly SG_ProgressManager _progress;

        // 缓存上次已解锁列表（用于 CheckNewUnlocks 对比）
        private readonly List<SkillConfigSO> _lastUnlockedSkills = new List<SkillConfigSO>(8);
        private readonly List<BuffConfigSO> _lastUnlockedPassives = new List<BuffConfigSO>(4);

        // 复用 buffer —— 避免每次调用都 new List（Y-3 优化）
        private readonly List<SkillConfigSO> _skillBuffer = new List<SkillConfigSO>(8);
        private readonly List<BuffConfigSO> _passiveBuffer = new List<BuffConfigSO>(4);
        private readonly List<SkillConfigSO> _newSkillBuffer = new List<SkillConfigSO>(4);
        private readonly List<BuffConfigSO> _newPassiveBuffer = new List<BuffConfigSO>(4);

        public SkillUnlockManager(
            SkillUnlockTableSO skillTable,
            PassiveUnlockTableSO passiveTable,
            SG_ProgressManager progress)
        {
            _skillTable = skillTable;
            _passiveTable = passiveTable;
            _progress = progress;

            // 初始化缓存
            RefreshCache();
        }

        /// <summary>
        /// 获取当前已解锁的主动技能列表。
        /// 注意：返回的是内部 buffer 引用，调用方不应缓存此引用（下次调用会覆盖）。
        /// </summary>
        public List<SkillConfigSO> GetUnlockedSkills()
        {
            _skillBuffer.Clear();
            for (int i = 0; i < _skillTable.Count; i++)
            {
                var entry = _skillTable.GetEntry(i);
                if (IsConditionMet(entry.ConditionType, entry.ConditionParam))
                {
                    _skillBuffer.Add(entry.Skill);
                }
            }
            return _skillBuffer;
        }

        /// <summary>
        /// 获取当前已解锁的被动技能列表（返回 BuffConfigSO）。
        /// 注意：返回的是内部 buffer 引用，调用方不应缓存此引用。
        /// </summary>
        public List<BuffConfigSO> GetUnlockedPassives()
        {
            _passiveBuffer.Clear();
            for (int i = 0; i < _passiveTable.Count; i++)
            {
                var entry = _passiveTable.GetEntry(i);
                if (IsConditionMet(entry.ConditionType, entry.ConditionParam))
                {
                    _passiveBuffer.Add(entry.BuffConfig);
                }
            }
            return _passiveBuffer;
        }

        /// <summary>
        /// 检查是否有新解锁内容（对比上次调用时的缓存）。
        /// 返回 true = 有新解锁，newSkills/newPassives 包含新增项。
        /// 注意：out 参数为内部 buffer 引用，调用方应立即消费。
        /// </summary>
        public bool CheckNewUnlocks(
            out List<SkillConfigSO> newSkills,
            out List<BuffConfigSO> newPassives)
        {
            _newSkillBuffer.Clear();
            _newPassiveBuffer.Clear();

            var currentSkills = GetUnlockedSkills();
            var currentPassives = GetUnlockedPassives();

            // 找出新增的技能
            for (int i = 0; i < currentSkills.Count; i++)
            {
                if (!_lastUnlockedSkills.Contains(currentSkills[i]))
                {
                    _newSkillBuffer.Add(currentSkills[i]);
                }
            }

            // 找出新增的被动
            for (int i = 0; i < currentPassives.Count; i++)
            {
                if (!_lastUnlockedPassives.Contains(currentPassives[i]))
                {
                    _newPassiveBuffer.Add(currentPassives[i]);
                }
            }

            // 更新缓存
            _lastUnlockedSkills.Clear();
            _lastUnlockedSkills.AddRange(currentSkills);
            _lastUnlockedPassives.Clear();
            _lastUnlockedPassives.AddRange(currentPassives);

            newSkills = _newSkillBuffer;
            newPassives = _newPassiveBuffer;
            return _newSkillBuffer.Count > 0 || _newPassiveBuffer.Count > 0;
        }

        // ── 内部 ──

        private bool IsConditionMet(UnlockConditionType condType, int param)
        {
            switch (condType)
            {
                case UnlockConditionType.Default:
                    return true;

                case UnlockConditionType.ClearLevel:
                    return _progress.IsLevelCleared(param);

                case UnlockConditionType.Achievement:
                    return _progress.IsAchievementMet(param);

                default:
                    return false;
            }
        }

        private void RefreshCache()
        {
            _lastUnlockedSkills.Clear();
            _lastUnlockedSkills.AddRange(GetUnlockedSkills());
            _lastUnlockedPassives.Clear();
            _lastUnlockedPassives.AddRange(GetUnlockedPassives());
        }
    }
}
