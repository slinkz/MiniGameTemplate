namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// MovementComponent SpeedModifier ID 注册表。(v0.4 SA-003/SA-010)
    /// ⚠️ ID 唯一性由开发者保证——如果两个系统用了相同 ID，后注册的会覆盖前一个。
    /// Phase 3A 只有 Buff 一个来源，冲突风险为零。
    /// Phase 4+ 如增加来源，考虑在 Debug 模式下添加 ID 冲突检测。
    /// </summary>
    public static class SpeedModifierIds
    {
        public const int Buff = 1;          // BuffComponent 速度修正
        public const int Terrain = 2;       // 预留：地形减速
        public const int Equipment = 3;     // 预留：装备加成
    }
}
