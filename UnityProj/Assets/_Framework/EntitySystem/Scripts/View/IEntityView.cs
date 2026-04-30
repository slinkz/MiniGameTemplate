using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 正式 View 的标准接口。
    /// 挂载在正式 ViewPrefab 的根 GO 上，EntityViewBridge 在 Spawn 时检测并缓存。
    /// 
    /// 设计原则：
    /// - 单一职责：IEntityView 只负责接收 Entity 状态变化的通知
    /// - 零 GC：所有参数用 struct/值类型传递
    /// - 可选实现：不实现此接口的 Prefab 仍能正常 Spawn（退化为纯位置同步）
    /// 
    /// Phase 2 交付（P2.1）。
    /// </summary>
    public interface IEntityView
    {
        /// <summary>
        /// View 初始化（Spawn 时调用一次）。
        /// 传入 Entity 配置信息供 View 层初始化动画/颜色/缩放等。
        /// </summary>
        void OnViewInit(EntityViewContext ctx);

        /// <summary>
        /// 每帧同步（在 EntityViewBridge.SyncAll 中调用）。
        /// 传入 Entity 当前状态快照，View 层决定如何表现。
        /// </summary>
        void OnViewSync(EntityViewSyncData data);

        /// <summary>
        /// 受击闪白通知（闪白开始时调用）。
        /// </summary>
        void OnViewHitFlash(Color flashColor, float duration);

        /// <summary>
        /// 闪白结束通知（恢复原色）。
        /// </summary>
        void OnViewHitFlashEnd();

        /// <summary>
        /// View 重置（Despawn 归还池前调用）。
        /// 用于重置动画状态、颜色等。
        /// </summary>
        void OnViewReset();
    }

    /// <summary>
    /// View 初始化上下文（一次性传递，Spawn 时）。
    /// </summary>
    public struct EntityViewContext
    {
        public EntityConfigSO Config;
        public EntityId EntityId;
        public Vector2 Position;
        public float Rotation;
        public int MaxHp;
        public int CurrentHp;
    }

    /// <summary>
    /// 每帧同步数据包（值类型，零 GC）。
    /// </summary>
    public struct EntityViewSyncData
    {
        public Vector2 Position;
        public float Rotation;
        public int CurrentHp;
        public int MaxHp;
        public int CurrentAnimId;
        public bool IsAlive;
    }
}
