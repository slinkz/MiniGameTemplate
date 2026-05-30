using UnityEngine;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.View
{
    public class SimpleEntityView : MonoBehaviour, IEntityView
    {
        public SpriteRenderer sr;
        private Color _baseColor = Color.white;
        private Color _flashColor;
        private float _flashTimer;
        private float _flashDuration;

        public void OnViewInit(EntityViewContext ctx)
        {
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
            _baseColor = ctx.Config.DebugColor;
            if (sr != null) sr.color = _baseColor;
            
            // 匹配碰撞体大小
            if (ctx.Config.HitboxType == Danmaku.HitboxShape.Rect)
            {
                float scaleX = ctx.Config.CollisionHalfWidth * 2f;
                float scaleY = ctx.Config.CollisionHalfHeight * 2f;
                transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
            else
            {
                float scale = ctx.Config.CollisionRadius * 2f;
                // 稍微放大一点点视觉，让碰撞体在视觉内部
                transform.localScale = new Vector3(scale * 1.2f, scale * 1.2f, 1f);
            }
        }

        public void OnViewSync(EntityViewSyncData data) { }

        public void OnViewHitFlash(Color flashColor, float duration)
        {
            _flashColor = flashColor;
            _flashDuration = duration;
            _flashTimer = duration;
        }

        public void OnViewHitFlashEnd()
        {
            _flashTimer = 0;
            if (sr != null) sr.color = _baseColor;
        }

        public void OnViewReset()
        {
            if (this == null) return; // 已被 Unity 销毁（场景卸载时）
            OnViewHitFlashEnd();
            // Scale 由 OnViewInit 设定，不在此重置（避免池复用时闪一帧 Vector3.one）
        }

        private void Update()
        {
            if (_flashTimer > 0 && sr != null)
            {
                if (_flashDuration <= 0f) { OnViewHitFlashEnd(); return; }
                _flashTimer -= Time.deltaTime;
                float t = 1f - (_flashTimer / _flashDuration);
                sr.color = Color.Lerp(_flashColor, _baseColor, t);
            }
        }
    }
}
