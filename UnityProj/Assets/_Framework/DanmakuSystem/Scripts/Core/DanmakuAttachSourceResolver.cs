using MiniGameTemplate.VFX;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// Danmaku → VFX 位置解析桥接——将 AttachSourceRegistry 的 Transform 查询
    /// 适配为 IVFXPositionResolver 接口，VFX 系统不需要知道 Danmaku 内部实现。（ADR-021）
    /// </summary>
    public class DanmakuAttachSourceResolver : IVFXPositionResolver
    {
        private readonly AttachSourceRegistry _registry;

        public DanmakuAttachSourceResolver(AttachSourceRegistry registry)
        {
            _registry = registry;
        }

        public bool TryResolvePosition(byte attachSourceId, out Vector3 worldPosition)
        {
            if (_registry == null || attachSourceId == 0 || attachSourceId >= AttachSourceRegistry.MAX_SOURCES)
            {
                worldPosition = Vector3.zero;
                return false;
            }

            var transform = _registry.Transforms[attachSourceId];
            if (transform == null)
            {
                worldPosition = Vector3.zero;
                return false;
            }

            Vector2 pos = _registry.GetWorldPosition(attachSourceId, Vector2.zero);
            worldPosition = new Vector3(pos.x, pos.y, 0f);
            return true;
        }
    }
}
