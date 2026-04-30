using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 角色配置资产。Phase 1 核心配置入口。
    /// 策划在 Inspector 中创建和编辑，路径：Assets/_Game/Configs/Entity/
    /// 
    /// 设计原则：
    /// - 单一数据源：所有 Entity 属性从此 SO 读取
    /// - 设计师友好：[CreateAssetMenu] + Tooltip + 分组 Header
    /// - Pool 驱动：PoolMax 决定预分配容量，Components[] 决定挂载哪些组件
    /// </summary>
    [CreateAssetMenu(fileName = "NewEntityConfig", menuName = "Entity/EntityConfig")]
    public class EntityConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("全局唯一配置 ID。Phase 1 可不填（用 SO 引用），Phase 2 迁移 Luban 时必填。")]
        public int ConfigId;

        [Tooltip("调试/UI 显示名")]
        public string DisplayName;

        [Tooltip("阵营（统一使用 EnumCamp）")]
        public EnumCamp Camp;

        [Header("组件列表")]
        [Tooltip("该 Entity 挂载的组件类型。EntityPool 根据此列表预创建组件。")]
        public ComponentType[] Components;

        [Header("对象池")]
        [Tooltip("对象池最大容量（预分配数量）")]
        [Min(1)]
        public int PoolMax = 16;

        [Header("属性")]
        public int MaxHp = 100;
        public float MoveSpeed = 3f;
        public float TurnSpeed = 360f;
        public float CollisionRadius = 0.5f;

        [Tooltip("击退距离（v2.4 GD-R4-004）")]
        public float KnockbackDistance = 0.5f;

        [Tooltip("击退持续时间（秒）")]
        public float KnockbackDuration = 0.2f;

        [Header("攻击（v2.4 新增）")]
        [Tooltip("攻击间隔（秒），0 = 不攻击")]
        public float AttackInterval = 1f;

        [Tooltip("攻击弹幕 Pattern（BulletPatternSO，发射配置）")]
        public Danmaku.BulletPatternSO AttackBulletPattern;

        [Tooltip("发射点偏移（相对 Entity 位置）")]
        public Vector2 AttackFireOffset;

        [Header("AI 行为（v2.4 新增）")]
        [Tooltip("AI 行为配置资产（条件-动作表）")]
        public AIBehaviorSO AIBehavior;

        // ──────────────── 视觉（Phase 1.10 填充）────────────────
        // public PoolDefinition SpawnEffect;
        // public PoolDefinition HitEffect;
        // public PoolDefinition DeathEffect;
        // public float HitFlashDuration = 0.1f;
        // public float DeathDelay = 0.5f;
        // public bool ShowDamageNumber = true;
    }
}
