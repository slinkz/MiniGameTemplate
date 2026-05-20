using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// 道具掉落系统（TDD_02 S2.5）。
    /// 监听敌机死亡事件，按 DropTable 概率生成道具。
    /// 纯 C# 服务，由 BattleController 创建和管理。
    /// </summary>
    public sealed class ItemDropSystem
    {
        private DropTableSO _normalDropTable;
        private DropTableSO _eliteDropTable;
        private PickupSystem _pickupSystem;

        /// <summary>距离上次出修复道具的击杀计数（保底用）</summary>
        private int _killsSinceLastRepair;

        /// <summary>保底阈值：连续 N 次击杀不出修复 → 强制出一个</summary>
        private const int REPAIR_GUARANTEE_KILLS = 15;

        /// <summary>
        /// 初始化（BattleController 创建后调用）。
        /// </summary>
        public void Init(DropTableSO normalTable, DropTableSO eliteTable, PickupSystem pickupSystem)
        {
            _normalDropTable = normalTable;
            _eliteDropTable = eliteTable;
            _pickupSystem = pickupSystem;
            _killsSinceLastRepair = 0;
        }

        /// <summary>
        /// 重置状态（关卡 Retry 时调用）。
        /// </summary>
        public void Reset()
        {
            _killsSinceLastRepair = 0;
        }

        /// <summary>
        /// 敌机被击杀时调用（由 BattleController 订阅 OnDeath 事件后转发）。
        /// </summary>
        /// <param name="position">敌机死亡位置</param>
        /// <param name="isElite">是否精英敌机</param>
        public void OnEnemyKilled(Vector2 position, bool isElite = false)
        {
            var table = isElite ? _eliteDropTable : _normalDropTable;
            if (table == null || table.Entries == null || table.Entries.Length == 0)
                return;

            _killsSinceLastRepair++;

            // 保底检查：连续 N 次击杀没出修复 → 强制
            if (_killsSinceLastRepair >= REPAIR_GUARANTEE_KILLS)
            {
                SpawnRepairGuarantee(position, table);
                return;
            }

            // 概率判定
            float roll = Random.value;
            if (roll >= table.BaseDropRate)
                return;

            var pickupConfig = table.Roll();
            if (pickupConfig == null) return;

            _pickupSystem.SpawnPickup(position, pickupConfig);

            // 如果掉了修复道具，重置保底计数
            if (pickupConfig.Type == PickupType.Repair)
            {
                _killsSinceLastRepair = 0;
            }
        }

        private void SpawnRepairGuarantee(Vector2 position, DropTableSO table)
        {
            // 从掉落表中找修复道具
            for (int i = 0; i < table.Entries.Length; i++)
            {
                if (table.Entries[i].Pickup != null && table.Entries[i].Pickup.Type == PickupType.Repair)
                {
                    _pickupSystem.SpawnPickup(position, table.Entries[i].Pickup);
                    _killsSinceLastRepair = 0;
                    return;
                }
            }

            // 掉落表无修复道具 → 降级为普通 Roll。
            // 设计意图：保底机制核心目标是"给玩家东西"以维持正反馈。
            // 当策划配表漏配修复道具时，不应直接吞掉这次保底机会。
            var fallback = table.Roll();
            if (fallback != null)
            {
                _pickupSystem.SpawnPickup(position, fallback);
            }
            _killsSinceLastRepair = 0;
        }
    }
}
