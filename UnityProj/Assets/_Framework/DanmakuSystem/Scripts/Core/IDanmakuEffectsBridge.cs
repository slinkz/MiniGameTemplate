using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕特效桥接接口——将弹幕系统的碰撞事件和弹丸清除事件
    /// 桥接到 VFX/飘字/统计等表现系统，实现 DanmakuSystem 与 VFX 的解耦。
    /// </summary>
    public interface IDanmakuEffectsBridge
    {
        /// <summary>
        /// 当碰撞事件就绪时调用（每帧碰撞检测后、Buffer Reset 前）。
        /// 实现方消费 Buffer 中的事件触发 VFX、飘字等。
        /// </summary>
        void OnCollisionEventsReady(CollisionEventBuffer buffer);

        /// <summary>
        /// 当弹丸被清屏 API 清除时调用（每颗弹丸一次）。
        /// 实现方可将弹丸转化为得分特效、消除动画等。
        /// </summary>
        /// <param name="bulletIndex">弹丸在 BulletWorld 中的索引</param>
        /// <param name="position">弹丸当前位置</param>
        /// <param name="type">弹丸类型 SO</param>
        void OnBulletCleared(int bulletIndex, Vector2 position, BulletTypeSO type);
    }
}
