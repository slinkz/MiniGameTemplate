using MiniGameTemplate.Audio;
using MiniGameTemplate.Pool;
using MiniGameTemplate.Rendering;
using UnityEngine;
using UnityEngine.Serialization;


namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸视觉类型配置——外观、碰撞、伤害、拖尾、爆炸、混合模式全部在 Inspector 配置。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Type")]
    public class BulletTypeSO : ScriptableObject
    {
        // ──── 统一资源描述（Phase 1.4） ────

        [Header("资源描述（统一）")]
        [Tooltip("源贴图——每个弹丸类型可引用独立贴图")]
        public Texture2D SourceTexture;

        [Tooltip("静态弹丸的 UV 区域（归一化到 SourceTexture）")]
        [FormerlySerializedAs("AtlasUV")]
        public Rect UVRect = new Rect(0, 0, 1, 1);

        [Header("采样模式")]
        public BulletSamplingMode SamplingMode = BulletSamplingMode.Static;

        [Header("序列帧配置（SamplingMode = SpriteSheet 时有效）")]
        [Min(1)] public int SheetColumns = 1;
        [Min(1)] public int SheetRows = 1;
        [Min(1)] public int SheetTotalFrames = 1;
        public BulletPlaybackMode PlaybackMode = BulletPlaybackMode.StretchToLifetime;
        [Min(0.001f)] public float FixedFps = 12f;

        [Header("Atlas 绑定（可选优化）")]
        [Tooltip("绑定的 Atlas 映射。null = 使用 SourceTexture 独立模式。ADR-017：Atlas 为可逆派生产物。")]
        public AtlasMappingSO AtlasBinding;

        /// <summary>资源描述版本号，用于迁移器</summary>
        [HideInInspector] public int SchemaVersion = 1;

        // ──── 旧字段（迁移兼容） ────

        /// <summary>已弃用——旧图集 UV，已重命名为 UVRect + FormerlySerializedAs</summary>
        [HideInInspector, SerializeField]
        private Rect _legacyAtlasUV;

        // ──── 视觉 ────

        [Header("视觉")]

        [Tooltip("颜色叠加")]
        public Color Tint = Color.white;

        [Tooltip("弹丸尺寸（世界单位）")]
        public Vector2 Size = new(0.2f, 0.2f);

        [Tooltip("朝飞行方向旋转（米粒弹等非圆弹丸）")]
        public bool RotateToDirection;

        [Header("碰撞")]
        public float CollisionRadius = 0.1f;

        [Header("运动")]
        [Tooltip("运动策略类型（Default=标准运动, SineWave=正弦波, Spiral=螺旋）")]
        public MotionType MotionType = MotionType.Default;

        [Tooltip("速度随生命周期的曲线（横轴 0-1 = 生命百分比，纵轴 = 速度倍率）")]
        public AnimationCurve SpeedOverLifetime = AnimationCurve.Constant(0, 1, 1);

        [Header("视觉动画（DEC-005=C）")]
        [Tooltip("勾选后 Mover 才会每帧采样动画曲线写入 BulletCore（不勾=跳过采样，性能更好）")]
        public bool UseVisualAnimation;

        [Tooltip("缩放随生命周期的曲线（横轴 0-1 = 生命百分比，纵轴 = 缩放倍率，默认常量 1）")]
        public AnimationCurve ScaleOverLifetime = AnimationCurve.Constant(0, 1, 1);

        [Tooltip("透明度随生命周期的曲线（横轴 0-1 = 生命百分比，纵轴 = Alpha 倍率 0-1，默认常量 1）")]
        public AnimationCurve AlphaOverLifetime = AnimationCurve.Constant(0, 1, 1);

        [Tooltip("颜色随生命周期的渐变（默认白色→白色=无变化）")]
        public Gradient ColorOverLifetime;

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

        [Tooltip("爆炸帧序列第一帧的 UV 矩形（图集中水平排列，每帧等宽）")]
        public Rect ExplosionAtlasUV;

        [Tooltip("重特效预制件（PooledPrefab 模式）")]
        public PoolDefinition HeavyExplosionPrefab;

        // ──── 子弹幕 ────

        [Header("子弹幕（消亡时触发）")]
        [Tooltip("弹丸消亡时触发的子弹幕模式。null = 无子弹幕")]
        public BulletPatternSO ChildPattern;

        // ──── 运行时 ────

        /// <summary>DanmakuTypeRegistry 分配的运行时索引</summary>
        [HideInInspector]
        public ushort RuntimeIndex;

        // ──── 序列帧辅助方法 ────

        /// <summary>
        /// 序列帧总有效帧数。
        /// </summary>
        public int MaxFrameCount => Mathf.Max(1, Mathf.Min(SheetTotalFrames, SheetColumns * SheetRows));

        /// <summary>
        /// 解析实际使用的贴图。优先级：AtlasBinding.AtlasTexture > SourceTexture。
        /// 不含 fallback 全局 Atlas，那层由 Renderer 负责。
        /// </summary>
        public Texture2D GetResolvedTexture()
        {
            if (AtlasBinding != null && AtlasBinding.AtlasTexture != null)
                return AtlasBinding.AtlasTexture;
            return SourceTexture;
        }

        /// <summary>
        /// 解析基础 UV 区域。
        /// Atlas 模式下优先从映射表查找 SourceTexture 的子区域；
        /// 若 SourceTexture 为空或映射中找不到，则 fallback 到手动配置的 UVRect。
        /// </summary>
        public Rect GetResolvedBaseUV()
        {
            if (AtlasBinding != null && AtlasBinding.AtlasTexture != null
                && SourceTexture != null
                && AtlasBinding.TryFindEntry(SourceTexture, out var entry))
            {
                return entry.UVRect;
            }
            return UVRect;
        }

        /// <summary>
        /// 根据帧索引计算该帧在 baseUV 内的子区域。
        /// Static 模式直接返回 baseUV。
        /// SpriteSheet 模式按列行切分。
        /// </summary>
        public Rect GetFrameUV(int frameIndex, Rect baseUV)
        {
            if (SamplingMode == BulletSamplingMode.Static)
                return baseUV;

            int cols = Mathf.Max(1, SheetColumns);
            int rows = Mathf.Max(1, SheetRows);
            int clampedFrame = Mathf.Clamp(frameIndex, 0, MaxFrameCount - 1);
            int x = clampedFrame % cols;
            int y = clampedFrame / cols;
            float fw = baseUV.width / cols;
            float fh = baseUV.height / rows;
            return new Rect(baseUV.x + x * fw, baseUV.y + y * fh, fw, fh);
        }

        /// <summary>
        /// 根据帧索引计算该帧在 UVRect 内的子区域（无 Atlas 绑定的兼容方法）。
        /// Static 模式直接返回 UVRect。
        /// SpriteSheet 模式按列行切分。
        /// </summary>
        public Rect GetFrameUV(int frameIndex)
        {
            return GetFrameUV(frameIndex, UVRect);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (SheetColumns < 1) SheetColumns = 1;
            if (SheetRows < 1) SheetRows = 1;
            if (SheetTotalFrames < 1) SheetTotalFrames = 1;
            if (FixedFps < 0.001f) FixedFps = 0.001f;
            Editor.DanmakuEditorRefreshCoordinator.MarkDirty(this);
        }
#endif
    }
}

