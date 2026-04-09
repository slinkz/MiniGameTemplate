namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕碰撞目标接口。
    /// 任何可被弹丸命中的实体（玩家、Boss、小怪等）均需实现此接口并通过
    /// <see cref="TargetRegistry.Register"/> 注册到碰撞系统中。
    /// </summary>
    public interface ICollisionTarget
    {
        /// <summary>当前碰撞体（每帧更新位置）</summary>
        CircleHitbox Hitbox { get; }

        /// <summary>
        /// 目标自身阵营。碰撞系统用此做阵营过滤：
        /// - Player 阵营目标只被 Enemy/Neutral 弹丸命中
        /// - Enemy 阵营目标只被 Player/Neutral 弹丸命中
        /// - Neutral 阵营目标被所有弹丸命中
        /// </summary>
        BulletFaction Faction { get; }

        /// <summary>
        /// 被弹丸命中时回调。由 CollisionSolver 在碰撞检测阶段调用。
        /// </summary>
        /// <param name="damage">本次命中的伤害值</param>
        /// <param name="bulletIndex">命中的弹丸在 BulletWorld 中的索引</param>
        void OnBulletHit(int damage, int bulletIndex);

        /// <summary>
        /// 被激光命中时回调（每 Tick 一次）。
        /// </summary>
        /// <param name="damage">本 Tick 伤害值</param>
        /// <param name="laserIndex">激光在 LaserPool 中的索引</param>
        void OnLaserHit(int damage, int laserIndex);

        /// <summary>
        /// 被喷雾命中时回调（每 Tick 一次）。
        /// </summary>
        /// <param name="damage">本 Tick 伤害值</param>
        /// <param name="sprayIndex">喷雾在 SprayPool 中的索引</param>
        void OnSprayHit(int damage, int sprayIndex);
    }
}
