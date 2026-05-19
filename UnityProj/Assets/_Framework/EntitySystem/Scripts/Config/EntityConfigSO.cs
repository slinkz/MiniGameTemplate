using UnityEngine;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Pool;

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
        public int PoolMax = 128;

        [Header("属性")]
        public int MaxHp = 100;
        public float MoveSpeed = 3f;
        public float TurnSpeed = 360f;
        public float CollisionRadius = 0.5f;

        [Tooltip("击退距离（v2.4 GD-R4-004）")]
        public float KnockbackDistance = 0.5f;

        [Tooltip("击退持续时间（秒）")]
        public float KnockbackDuration = 0.2f;

        [Header("受击参数（P2.4 新增）")]
        [Tooltip("无敌帧数（受伤后 N 帧内不可再受伤，0=不启用）")]
        [Min(0)]
        public int IFrameCount = 0;

        [Tooltip("HitStop 顿帧数（受伤后冻结 N 帧，营造打击感，0=不启用）")]
        [Min(0)]
        public int HitStopFrames = 0;

        [Tooltip("击退速度曲线（时间 0~1 → 速度衰减，可选。为空时使用线性衰减）")]
        public AnimationCurve KnockbackCurve;

        [Header("Entity vs Entity 碰撞（P2.2）")]
        [Tooltip("是否参与 Entity vs Entity 碰撞检测")]
        public bool EnableEntityCollision = true;

        [Tooltip("碰撞层（0=默认，与所有层碰撞。相同非零层才碰撞。）")]
        public int CollisionLayer = 0;

        [Tooltip("接触伤害值（0=不造成接触伤害）")]
        public int ContactDamage = 0;

        [Tooltip("接触伤害间隔（秒）。防止每帧重复伤害。")]
        public float ContactDamageInterval = 0.5f;

        [Header("战斗属性")]
        [Tooltip("攻击力（覆盖弹幕 BulletTypeSO.Damage，0 = 使用弹幕配置的固定伤害）")]
        [Min(0)]
        public int AttackPower = 0;

        [Tooltip("暴击率（0~1，0.3 = 30% 暴击率）")]
        [Range(0f, 1f)]
        public float CritRate = 0f;

        [Tooltip("暴击伤害倍率（如 2.0 = 200% 伤害）")]
        [Min(1f)]
        public float CritDamageMultiplier = 2f;

        [Header("自动瞄准（P3.1）")]
        [Tooltip("搜索半径（0=不启用 AutoAim）")]
        public float AutoAimRadius = 0f;

        [Tooltip("搜索间隔（秒）[占位符]——需 gameplay 测试调整")]
        [Min(0.05f)]
        public float AutoAimSearchInterval = 0.2f;

        [Header("攻击（v2.4 新增）")]
        [Tooltip("攻击间隔（秒），0 = 不攻击")]
        public float AttackInterval = 1f;

        [Tooltip("首次开火延迟（秒）。每只敌机独立计时，从 Spawn 时刻开始。V2 Sprint 1 新增。")]
        [Min(0f)]
        public float FirstAttackDelay = 1.0f;

        [Tooltip("攻击弹幕 Pattern（BulletPatternSO，发射配置）")]
        public Danmaku.BulletPatternSO AttackBulletPattern;

        [Tooltip("发射点偏移（相对 Entity 位置）")]
        public Vector2 AttackFireOffset;

        [Header("技能（P3.3）")]
        [Tooltip("技能配置（null=不启用 Skill 组件）")]
        public SkillConfigSO SkillConfig;

        [Header("AI 行为（v2.4 新增）")]
        [Tooltip("AI 行为配置资产（条件-动作表）")]
        public AIBehaviorSO AIBehavior;

        // ──────────────── View 桥接（Phase 1.9）────────────────

        [Header("视觉 View")]
        [Tooltip("正式 View Prefab（Phase 2 使用）。为空时 ViewBridge 使用内置 Debug Prefab。")]
        public GameObject ViewPrefab;

        [Tooltip("正式 View 对象池定义（Phase 2 使用）。与 ViewPrefab 配套。")]
        public Pool.PoolDefinition ViewPoolDef;

        [Tooltip("序列帧动画数据（Phase 2 使用）。可选——为空时 View Prefab 自身配置的 AnimData 生效。")]
        public SpriteAnimDataSO SpriteAnimData;

        [Tooltip("Debug View 颜色（Phase 1 Debug Prefab 的 SpriteRenderer 色调）")]
        public Color DebugColor = Color.white;

        // ──────────────── 受击反馈 + 视觉特效（P1.11 启用）────────────────

        [Header("受击反馈")]
        [Tooltip("受击闪白持续时间")]
        public float HitFlashDuration = 0.1f;

        [Tooltip("受击闪白颜色")]
        public Color HitFlashColor = Color.white;

        [Tooltip("是否显示伤害数字")]
        public bool ShowDamageNumber = true;

        [Header("视觉特效")]
        [Tooltip("生成特效（走 PoolManager，可选）")]
        public Pool.PoolDefinition SpawnEffect;

        [Tooltip("受击特效（走 PoolManager，可选）")]
        public Pool.PoolDefinition HitEffect;

        [Tooltip("死亡特效（走 PoolManager，可选）")]
        public Pool.PoolDefinition DeathEffect;

        [Tooltip("死亡延迟（播完表现再回收，秒）")]
        public float DeathDelay = 0.3f;
    }
}
