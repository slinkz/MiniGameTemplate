using MiniGameTemplate.VFX;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 默认特效桥接实现——消费 CollisionEventBuffer 触发命中 VFX。
    /// 持有 VFX 系统引用，DanmakuSystem 不再直接引用 SpriteSheetVFXSystem。
    /// </summary>
    public class DefaultDanmakuEffectsBridge : IDanmakuEffectsBridge
    {
        private readonly SpriteSheetVFXSystem _hitVfxSystem;
        private readonly VFXTypeSO _hitVfxType;
        private readonly float _hitVfxScale;

        public DefaultDanmakuEffectsBridge(
            SpriteSheetVFXSystem hitVfxSystem,
            VFXTypeSO hitVfxType,
            float hitVfxScale)
        {
            _hitVfxSystem = hitVfxSystem;
            _hitVfxType = hitVfxType;
            _hitVfxScale = hitVfxScale;
        }

        public void OnCollisionEventsReady(CollisionEventBuffer buffer)
        {
            if (_hitVfxSystem == null || _hitVfxType == null)
                return;

            if (!_hitVfxSystem.CanPlay(_hitVfxType))
                return;

            var span = buffer.AsReadOnlySpan();
            for (int i = 0; i < span.Length; i++)
            {
                ref readonly var evt = ref span[i];
                PlayHitVFX(evt.Position);
            }
        }

        public void OnBulletCleared(int bulletIndex, Vector2 position, BulletTypeSO type)
        {
            PlayHitVFX(position);
        }

        private void PlayHitVFX(Vector2 position)
        {
            if (_hitVfxSystem == null || _hitVfxType == null)
                return;

            if (!_hitVfxSystem.CanPlay(_hitVfxType))
                return;

            _hitVfxSystem.PlayOneShot(_hitVfxType, new Vector3(position.x, position.y, 0f), _hitVfxScale);
        }
    }
}
