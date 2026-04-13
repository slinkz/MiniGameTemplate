using UnityEngine;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// VFX 位置解析接口——VFX 系统只依赖此契约，不反向依赖 Danmaku。
    /// 实现方负责将 attachSourceId 解析为世界坐标。（ADR-021）
    /// </summary>
    public interface IVFXPositionResolver
    {
        /// <summary>
        /// 尝试解析附着源的世界位置。
        /// </summary>
        /// <param name="attachSourceId">附着源 ID</param>
        /// <param name="worldPosition">解析出的世界位置</param>
        /// <returns>true=解析成功，false=源已失效</returns>
        bool TryResolvePosition(byte attachSourceId, out Vector3 worldPosition);
    }
}
