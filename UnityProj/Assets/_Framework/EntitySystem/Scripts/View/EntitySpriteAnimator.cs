using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 通用 Entity 序列帧动画驱动器（MonoBehaviour）。
    /// 挂载在正式 View Prefab 上，实现 IEntityView 接口。
    /// 
    /// 设计：
    /// - 序列帧数据存储在 SpriteAnimDataSO（ScriptableObject）
    /// - 动画切换由 AnimationComponent.CurrentAnimId 驱动
    /// - 闪白通过 MaterialPropertyBlock 零材质实例化实现
    /// - 支持面朝方向翻转（FlipX）
    /// 
    /// Phase 2 交付（P2.1）。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EntitySpriteAnimator : MonoBehaviour, IEntityView
    {
        [Header("动画数据")]
        [Tooltip("序列帧动画配置资产")]
        [SerializeField] private SpriteAnimDataSO _animData;

        // ──────────── 缓存 ────────────
        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;
        private static readonly int s_FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int s_FlashAmountId = Shader.PropertyToID("_FlashAmount");

        // ──────────── 运行时状态 ────────────
        private int _currentAnimId;
        private int _currentFrame;
        private float _frameTimer;
        private bool _isFlashing;
        private float _flashTimer;
        private float _flashDuration;
        private Color _flashColor;
        private Color _originalColor;
        private bool _initialized;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            _originalColor = _sr.color;
        }

        // ──────────── IEntityView 实现 ────────────

        public void OnViewInit(EntityViewContext ctx)
        {
            _currentAnimId = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _isFlashing = false;
            _flashTimer = 0f;
            _initialized = true;

            // 如果 EntityConfigSO 有动画数据引用，覆盖 Inspector 配置
            if (ctx.Config.SpriteAnimData != null)
                _animData = ctx.Config.SpriteAnimData;

            // 初始帧
            UpdateSprite();
        }

        public void OnViewSync(EntityViewSyncData data)
        {
            if (!_initialized || _animData == null) return;

            // 动画切换
            if (data.CurrentAnimId != _currentAnimId)
            {
                _currentAnimId = data.CurrentAnimId;
                _currentFrame = 0;
                _frameTimer = 0f;
            }

            // 帧推进
            var clip = _animData.GetClip(_currentAnimId);
            if (clip != null && clip.Frames != null && clip.Frames.Length > 0)
            {
                float fps = clip.FramesPerSecond > 0f ? clip.FramesPerSecond : 10f;
                _frameTimer += Time.deltaTime;
                float frameDuration = 1f / fps;

                while (_frameTimer >= frameDuration)
                {
                    _frameTimer -= frameDuration;
                    _currentFrame++;

                    if (_currentFrame >= clip.Frames.Length)
                    {
                        _currentFrame = clip.Loop ? 0 : clip.Frames.Length - 1;
                    }
                }

                UpdateSprite();
            }

            // 面朝方向翻转
            if (data.Position.x != 0f || data.Rotation != 0f)
            {
                // 简易翻转：根据移动方向
                // Phase 2 简化版：由 Rotation 决定翻转
                bool flipX = Mathf.Abs(data.Rotation) > 90f && Mathf.Abs(data.Rotation) < 270f;
                _sr.flipX = flipX;
            }

            // 闪白淡出
            if (_isFlashing)
            {
                _flashTimer += Time.deltaTime;
                float progress = _flashDuration > 0f ? _flashTimer / _flashDuration : 1f;

                if (progress >= 1f)
                {
                    _isFlashing = false;
                    _sr.color = _originalColor;
                    ClearFlashMPB();
                }
                else
                {
                    // 使用 MaterialPropertyBlock 实现无材质克隆闪白
                    float flashAmount = 1f - progress;
                    _sr.GetPropertyBlock(_mpb);
                    _mpb.SetColor(s_FlashColorId, _flashColor);
                    _mpb.SetFloat(s_FlashAmountId, flashAmount);
                    _sr.SetPropertyBlock(_mpb);
                }
            }
        }

        public void OnViewHitFlash(Color flashColor, float duration)
        {
            _isFlashing = true;
            _flashTimer = 0f;
            _flashDuration = duration;
            _flashColor = flashColor;
        }

        public void OnViewHitFlashEnd()
        {
            _isFlashing = false;
            _sr.color = _originalColor;
            ClearFlashMPB();
        }

        public void OnViewReset()
        {
            _currentAnimId = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _isFlashing = false;
            _flashTimer = 0f;
            _initialized = false;
            _sr.color = _originalColor;
            ClearFlashMPB();
        }

        // ──────────── 内部 ────────────

        private void UpdateSprite()
        {
            if (_animData == null) return;

            var clip = _animData.GetClip(_currentAnimId);
            if (clip == null || clip.Frames == null || clip.Frames.Length == 0) return;

            int frameIdx = Mathf.Clamp(_currentFrame, 0, clip.Frames.Length - 1);
            var sprite = clip.Frames[frameIdx];
            if (sprite != null)
                _sr.sprite = sprite;
        }

        private void ClearFlashMPB()
        {
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(s_FlashAmountId, 0f);
            _sr.SetPropertyBlock(_mpb);
        }
    }
}
