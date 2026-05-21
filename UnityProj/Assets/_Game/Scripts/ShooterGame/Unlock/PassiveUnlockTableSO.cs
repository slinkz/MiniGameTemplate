using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 被动技能解锁表 SO。策划配置所有被动技能的解锁条件。
    /// V2 Sprint 3：被动走 PassiveAbilitySO（CD 驱动 + Buff 桥接）。
    /// TDD_02 S2.1 → TDD_03 S3.5
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/PassiveUnlockTable")]
    public class PassiveUnlockTableSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("被动技能配置（PassiveAbilitySO）")]
            public PassiveAbilitySO PassiveConfig;
            public UnlockConditionType ConditionType;
            [Tooltip("ClearLevel=关卡编号(1-based), Achievement=成就ID")]
            public int ConditionParam;
            [Tooltip("显示名（UI 用）")]
            public string DisplayName;
            [Tooltip("解锁描述")]
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
                if (_entries[i].PassiveConfig == null)
                {
                    Debug.LogError($"[PassiveUnlockTable] Entry[{i}] PassiveConfig 引用为空", this);
                    continue;
                }

                int id = _entries[i].PassiveConfig.GetInstanceID();
                if (!seen.Add(id))
                {
                    Debug.LogError($"[PassiveUnlockTable] 重复被动: {_entries[i].PassiveConfig.name}", this);
                }

                if (_entries[i].ConditionType == UnlockConditionType.ClearLevel)
                {
                    if (_entries[i].ConditionParam < 1 || _entries[i].ConditionParam > 30)
                    {
                        Debug.LogWarning($"[PassiveUnlockTable] Entry[{i}] 关卡编号越界: {_entries[i].ConditionParam}", this);
                    }
                }
            }
        }
#endif
    }
}
