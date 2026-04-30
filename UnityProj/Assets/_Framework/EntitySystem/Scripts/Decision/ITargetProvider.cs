using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 目标提供者接口——AutoAimComponent 实现此接口，
    /// AI Action 通过它获取当前锁定目标的位置信息。
    /// </summary>
    public interface ITargetProvider
    {
        /// <summary>是否有有效目标</summary>
        bool HasTarget { get; }

        /// <summary>目标位置</summary>
        Vector2 TargetPosition { get; }

        /// <summary>到目标的距离（缓存值，避免重复计算）</summary>
        float DistanceToTarget { get; }
    }
}
