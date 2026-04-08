using MiniGameTemplate.Audio;
using MiniGameTemplate.Pool;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸视觉类型配置——外观、碰撞、伤害、拖尾、爆炸、混合模式全部在 Inspector 配置。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Type")]
    public class BulletTypeSO : ScriptableObject
    {
        [Header("视觉")]
        [Tooltip("在弹丸图集中的 UV 矩形")]
        public Rect AtlasUV;

        [Tooltip("颜色叠加")]
        public Color Tint = Color.white;

        [Tooltip("弹丸尺寸（世界单位）")]
        public Vector2 Size = new(0.2f, 0.2f);

        [Tooltip("朝飞行方向旋转（米粒弹等非圆弹丸）")]
        public bool RotateToDirection;

        [Header("碰撞")]
        public float CollisionRadius = 0.1f;

        [Header("运动")]
        [Tooltip("速度随生命周期的曲线（横轴 0-1 = 生命百分比，纵轴 = 速度倍率）")]
        public AnimationCurve SpeedOverLifetime = AnimationCurve.Constant(0, 1, 1);

        [Header("伤害")]
        [Tooltip("弹丸命中目标时造成的基础伤害值")]
        [Min(0)]
        public int Damage = 1;

        [Header("生命值")]
        [Tooltip("初始生命值。1=单次碰撞即死（默认），255=几乎不可摧毁")]
        [Range(1, 255)]
        public byte InitialHitPoints = 1;

        [Header("阵营")]
        public BulletFaction Faction = BulletFaction.Enemy;

        // ──── 碰撞响应 ────

        [Header("碰撞响应 — 碰到对象（玩家/敌人）")]
        public CollisionResponse OnHitTarget = CollisionResponse.Die;

        [Tooltip("OnHitTarget=ReduceHP 时每次扣减的生命值")]
        [Range(1, 255)]
        public byte HitTargetHPCost = 1;

        [Header("碰撞响应 — 碰到障碍物")]
        public CollisionResponse OnHitObstacle = CollisionResponse.Die;

        [Tooltip("OnHitObstacle=ReduceHP 时每次扣减的生命值")]
        [Range(1, 255)]
        public byte HitObstacleHPCost = 1;

        [Header("碰撞响应 — 碰到屏幕边缘")]
        public CollisionResponse OnHitScreenEdge = CollisionResponse.Die;

        [Tooltip("OnHitScreenEdge=ReduceHP 时每次扣减的生命值")]
        [Range(1, 255)]
        public byte HitScreenEdgeHPCost = 1;

        [Tooltip("OnHitScreenEdge=RecycleOnDistance 时，超出屏幕边缘多远后回收（世界单位）")]
        public float ScreenEdgeRecycleDistance = 1f;

        // ──── 碰撞反馈 ────

        [Header("碰撞反馈（视觉 + 音频）")]
        [Tooltip("反弹/反射时播放的特效（从 EffectPool 取，null=无特效）")]
        public PoolDefinition BounceEffect;

        [Tooltip("反弹/反射时播放的音效")]
        public AudioClipSO BounceSFX;

        [Tooltip("穿透目标时播放的音效")]
        public AudioClipSO PierceSFX;

        [Tooltip("HP 扣减（但未死亡）时的闪烁色调")]
        public Color DamageFlashTint = new Color(1, 0.3f, 0.3f, 1);

        [Tooltip("HP 扣减时闪烁的帧数（在渲染时叠加色调）")]
        public byte DamageFlashFrames = 3;

        // ──── 拖尾 ────

        [Header("拖尾")]
        public TrailMode Trail = TrailMode.None;

        [Tooltip("Mesh 残影数量（Ghost 模式）")]
        public byte GhostCount = 3;

        [Tooltip("轨迹点数（Trail 模式）")]
        public int TrailPointCount = 20;

        public float TrailWidth = 0.3f;
        public AnimationCurve TrailWidthCurve;
        public Gradient TrailColor;

        // ──── 爆炸 ────

        [Header("爆炸")]
        public ExplosionMode Explosion = ExplosionMode.MeshFrame;

        [Tooltip("Mesh 内爆炸帧数")]
        public int ExplosionFrameCount = 4;

        [Tooltip("重特效预制件（PooledPrefab 模式）")]
        public PoolDefinition HeavyExplosionPrefab;

        // ──── 子弹幕 ────

        [Header("子弹幕（消亡时触发）")]
        [Tooltip("弹丸消亡时触发的子弹幕模式。null = 无子弹幕")]
        public BulletPatternSO ChildPattern;

        // ──── 渲染层 ────

        [Header("渲染层")]
        public RenderLayer Layer = RenderLayer.Normal;

        /// <summary>DanmakuTypeRegistry 分配的运行时索引</summary>
        [HideInInspector]
        public ushort RuntimeIndex;
    }
}
