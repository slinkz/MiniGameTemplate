using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 技能效果策略接口。通过 [SerializeReference] 序列化到 SkillConfigSO。
    /// 
    /// ⚠️ 实现约束（SA-002）：
    /// - ISkillEffect 实现必须是【无状态】的——不得持有随 Execute 调用变化的字段
    /// - 原因：SkillConfigSO 是共享资产，多个 Entity 引用同一 SO = 共享同一 Effect 实例
    /// - 如需有状态行为（充能/蓄力），使用 SkillComponent 内部状态或 Phase 4 扩展
    /// - 所有序列化字段应为【配置参数】（只读），不应在 Execute 中修改
    /// 
    /// ⚠️ 命名约束（SA-012）：
    /// - ISkillEffect 实现类一经发布（有 SkillConfigSO 引用），不得重命名类名或移动命名空间
    /// - Unity [SerializeReference] 使用全限定类型名做序列化键
    /// - 如必须重命名，使用 [MovedFrom] 属性做兼容映射
    /// </summary>
    public interface ISkillEffect
    {
        /// <summary>
        /// 技能触发时执行。返回 true 表示效果成功执行，false 表示未执行。
        /// Phase 3A 中 SkillComponent 不消费返回值；Phase 4 可用于"失败不进 CD"逻辑。
        /// (v0.4 ATK-008)
        /// </summary>
        bool Execute(SkillContext ctx);
    }

    /// <summary>
    /// 技能执行上下文（struct，零 GC）。
    /// 
    /// 设计说明（SA-007）：
    /// SkillContext 是值类型（struct），但包含引用类型字段（Caster、SkillConfig）。
    /// 值拷贝后，引用字段仍指向同一对象实例。
    /// 这是 by-design：允许 DealAreaDamage 等场景中对 baseContext 做值拷贝，
    /// 每个目标获得独立 context 但共享 Caster 引用。
    /// </summary>
    public struct SkillContext
    {
        /// <summary>施法者</summary>
        public Entity Caster;
        /// <summary>施法位置</summary>
        public Vector2 CastPosition;
        /// <summary>瞄准方向</summary>
        public Vector2 AimDirection;
        /// <summary>当前帧 dt（供扩展使用）</summary>
        public float DeltaTime;
        /// <summary>技能配置引用（v0.4 GD-017）</summary>
        public SkillConfigSO SkillConfig;
    }
}
