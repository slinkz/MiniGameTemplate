using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// 道具掉落表 ScriptableObject（TDD_02 S2.4）。
    /// 包含加权随机抽取逻辑 + 基础掉率。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDropTable", menuName = "Configs/ShooterGame/DropTable")]
    public class DropTableSO : ScriptableObject
    {
        [System.Serializable]
        public struct DropEntry
        {
            public PickupConfigSO Pickup;
            [Min(1)] public int Weight;
        }

        [Header("掉落条目")]
        public DropEntry[] Entries;

        [Header("掉率")]
        [Tooltip("基础掉率（0~1，0.3 = 30% 概率掉落）")]
        [Range(0f, 1f)]
        public float BaseDropRate = 0.3f;

        [Tooltip("V2 保留：是否保底掉落（当前不启用）")]
        public bool GuaranteeDrop;

        /// <summary>
        /// 加权随机抽取一个道具。
        /// 调用前应已通过 BaseDropRate 判定是否掉落。
        /// </summary>
        public PickupConfigSO Roll()
        {
            if (Entries == null || Entries.Length == 0) return null;

            int totalWeight = 0;
            for (int i = 0; i < Entries.Length; i++)
                totalWeight += Entries[i].Weight;

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < Entries.Length; i++)
            {
                cumulative += Entries[i].Weight;
                if (roll < cumulative)
                    return Entries[i].Pickup;
            }

            // 防御性兜底
            return Entries[Entries.Length - 1].Pickup;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Entries == null || Entries.Length == 0)
            {
                Debug.LogError($"[DropTableSO] {name}: Entries 为空", this);
                return;
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].Pickup == null)
                    Debug.LogError($"[DropTableSO] {name}: Entries[{i}] Pickup 为空", this);
                if (Entries[i].Weight <= 0)
                    Debug.LogWarning($"[DropTableSO] {name}: Entries[{i}] Weight ≤ 0", this);
            }

            if (BaseDropRate <= 0f || BaseDropRate > 1f)
                Debug.LogWarning($"[DropTableSO] {name}: BaseDropRate={BaseDropRate} 不在 (0,1] 范围", this);
        }
#endif
    }
}
