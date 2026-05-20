using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 技能解锁表 SO。策划配置所有主动技能的解锁条件。
    /// TDD_02 S2.1
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/SkillUnlockTable")]
    public class SkillUnlockTableSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SkillConfigSO Skill;
            public UnlockConditionType ConditionType;
            [Tooltip("ClearLevel=关卡编号(1-based), Achievement=成就ID")]
            public int ConditionParam;
            [Tooltip("解锁描述（UI 显示用）")]
            public string Description;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public ReadOnlySpan<Entry> Entries => _entries;
        public int Count => _entries.Length;

        public Entry GetEntry(int index) => _entries[index];

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_entries == null) return;

            var seen = new HashSet<int>();
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Skill == null)
                {
                    Debug.LogError($"[SkillUnlockTable] Entry[{i}] 技能引用为空", this);
                    continue;
                }

                int id = _entries[i].Skill.GetInstanceID();
                if (!seen.Add(id))
                {
                    Debug.LogError($"[SkillUnlockTable] 重复技能: {_entries[i].Skill.name}", this);
                }

                if (_entries[i].ConditionType == UnlockConditionType.ClearLevel)
                {
                    if (_entries[i].ConditionParam < 1 || _entries[i].ConditionParam > 30)
                    {
                        Debug.LogWarning($"[SkillUnlockTable] Entry[{i}] 关卡编号越界: {_entries[i].ConditionParam}", this);
                    }
                }
            }
        }
#endif
    }
}
