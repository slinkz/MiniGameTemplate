namespace MiniGameTemplate.Danmaku
{
    /// <summary>弹丸生命阶段（严格单向：Active → Exploding → Dead → Free）</summary>
    public enum BulletPhase : byte
    {
        /// <summary>正常飞行中（参与碰撞 + 运动 + 渲染）</summary>
        Active = 0,

        /// <summary>播放爆炸帧动画（碰撞关闭，不运动，渲染爆炸帧）</summary>
        Exploding = 1,

        /// <summary>等待回收（同帧 Free）</summary>
        Dead = 2,
    }

    /// <summary>弹丸碰撞到某类目标时的响应行为</summary>
    public enum CollisionResponse : byte
    {
        /// <summary>立即死亡（HitPoints 归零）</summary>
        Die = 0,

        /// <summary>削减生命值（扣减量可配置）</summary>
        ReduceHP = 1,

        /// <summary>穿透（不消耗生命值，继续飞行）</summary>
        Pierce = 2,

        /// <summary>原路反弹（速度完全取反）</summary>
        BounceBack = 3,

        /// <summary>镜像反弹（速度沿碰撞法线镜像）</summary>
        Reflect = 4,

        /// <summary>超出距离后回收（仅屏幕边缘有效）</summary>
        RecycleOnDistance = 5,
    }

    /// <summary>弹丸阵营</summary>
    public enum BulletFaction : byte
    {
        /// <summary>敌方弹丸，与玩家阵营碰撞</summary>
        Enemy = 0,

        /// <summary>玩家弹丸，与敌方阵营碰撞</summary>
        Player = 1,

        /// <summary>中立弹丸，与所有对象碰撞</summary>
        Neutral = 2,
    }

    /// <summary>弹丸碰撞的目标类型（决定读取 BulletTypeSO 的哪组碰撞响应配置）</summary>
    public enum CollisionTarget : byte
    {
        /// <summary>玩家/敌人等碰撞对象</summary>
        Target = 0,

        /// <summary>场景障碍物（AABB）</summary>
        Obstacle = 1,

        /// <summary>屏幕边缘</summary>
        ScreenEdge = 2,
    }

    /// <summary>拖尾模式</summary>
    public enum TrailMode : byte
    {
        /// <summary>无拖尾</summary>
        None = 0,

        /// <summary>Mesh 内残影（轻量）</summary>
        Ghost = 1,

        /// <summary>独立 TrailPool 曲线（重量）</summary>
        Trail = 2,

        /// <summary>同时使用残影 + 曲线</summary>
        Both = 3,
    }

    /// <summary>爆炸模式</summary>
    public enum ExplosionMode : byte
    {
        /// <summary>无爆炸效果</summary>
        None = 0,

        /// <summary>Mesh 内帧动画（轻量，弹丸 Mesh 合批内播放）</summary>
        MeshFrame = 1,

        /// <summary>对象池预制件（重特效，从 PoolManager 取）</summary>
        PooledPrefab = 2,
    }

    /// <summary>渲染层</summary>
    public enum RenderLayer : byte
    {
        /// <summary>普通混合（Alpha Blend）</summary>
        Normal = 0,

        /// <summary>叠加发光（Additive Blend）</summary>
        Additive = 1,
    }
}
