namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 组件类型枚举——Entity 内部以此为数组索引实现 O(1) GetComponent。
    /// 新增组件类型时在此枚举追加（最大 16 种，预留扩展）。
    /// </summary>
    public enum ComponentType : byte
    {
        State = 0,
        Health = 1,
        Animation = 2,
        Movement = 3,
        Collision = 4,
        AutoAim = 5,
        Skill = 6,
        Control = 7,
        AI = 8,
        Attack = 9,    // [已退役] 保留数值兼容序列化。运行时 EntityPool 返回 null
        Buff = 10,     // Phase 3A (P3.4)
        EnemyShoot = 11, // V2 Sprint 1: 敌机射击组件
        Passive = 12,   // V2 Sprint 3: 被动技能组件
        // 预留 13~15
        MAX = 16
    }
}
