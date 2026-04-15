using MiniGameTemplate.VFX;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕特效桥接配置——挂在 DanmakuSystem 同 GameObject 上。
    /// 持有 VFX 引用，DanmakuSystem 通过此组件获取桥接参数。
    /// DEV-002：将 VFX 序列化字段从 Facade 迁移到此组件，Facade 不再直接 using VFX。
    /// </summary>
    public class DanmakuEffectsBridgeConfig : MonoBehaviour
    {
        [Header("命中特效")]
        [Tooltip("命中时播放的 Sprite Sheet VFX 系统")]
        [SerializeField] private SpriteSheetVFXSystem _hitVfxSystem;

        [Tooltip("命中时播放的特效类型")]
        [SerializeField] private VFXTypeSO _hitVfxType;

        [Tooltip("命中特效的统一缩放")]
        [SerializeField, Min(0.01f)] private float _hitVfxScale = 1f;

        public SpriteSheetVFXSystem HitVfxSystem => _hitVfxSystem;
        public VFXTypeSO HitVfxType => _hitVfxType;
        public float HitVfxScale => _hitVfxScale;

        /// <summary>
        /// Creates the runtime bridge used by DanmakuSystem.
        /// </summary>
        public IDanmakuVFXRuntime CreateRuntimeBridge(AttachSourceRegistry attachRegistry)
        {
            if (_hitVfxSystem == null)
                return null;

            return new DanmakuVFXRuntimeBridge(_hitVfxSystem, new DanmakuAttachSourceResolver(attachRegistry));
        }
    }
}

