using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 将 Entity 桥接到弹幕碰撞系统。
    /// 实现 ICollisionTarget，注册到 TargetRegistry。
    /// 
    /// BC-05 契约实现：
    /// - BC-05.1: 实现 ICollisionTarget，桥接 TargetRegistry
    /// - BC-05.2: Init 时注册 → TargetRegistry.Register(this)
    /// - BC-05.3: Reset 时注销 → TargetRegistry.Unregister(this)
    /// - BC-05.4: OnBulletHit/OnLaserHit/OnSprayHit → EntityEventBus 发布 OnCollisionHit
    /// - BC-05.5: Hitbox 每帧从 Entity.Position + ConfigSO.CollisionRadius 计算
    /// 
    /// v2.4：碰撞回调构造 DamageContext 发布到 EntityEventBus。
    /// OwnerEntityId → 查找发射者 Entity → 暴击 Roll + AttackPower 覆盖。
    /// </summary>
    public class CollisionComponent : IEntityComponent, ICollisionTarget
    {
        // ──────────────── 内部状态 ────────────────

        private Entity _owner;
        private float _radius;
        private float _halfWidth;
        private float _halfHeight;
        private HitboxShape _hitboxShape;
        private int _targetSlot = -1;
        private bool _isActive = true;
        private bool _isCollisionEnabled = true;

        // ──────────────── IEntityComponent 实现 ────────────────

        public bool IsActive => _isActive;
        public ComponentType Type => ComponentType.Collision;

        public void Init(Entity owner)
        {
            _owner = owner;
            _radius = owner.ConfigSO.CollisionRadius;
            _hitboxShape = owner.ConfigSO.HitboxType;
            _halfWidth = owner.ConfigSO.CollisionHalfWidth;
            _halfHeight = owner.ConfigSO.CollisionHalfHeight;
            _isActive = true;
            _isCollisionEnabled = true;

            // 注册到弹幕碰撞系统
            var ds = DanmakuSystem.Instance;
            if (ds != null)
            {
                _targetSlot = ds.TargetRegistry.Register(this);
                if (_targetSlot < 0)
                {
                    Debug.LogError(
                        $"[CollisionComponent] Entity {_owner.Id} 注册碰撞目标失败：" +
                        $"TargetRegistry 已满（{TargetRegistry.MAX_TARGETS}/{TargetRegistry.MAX_TARGETS}），需扩容");
                    _isCollisionEnabled = false;
                }
            }
            else
            {
                // DanmakuSystem 未初始化（纯逻辑测试场景）
                _targetSlot = -1;
                _isCollisionEnabled = false;
            }
        }

        public void Reset()
        {
            // 注销碰撞目标
            if (_targetSlot >= 0)
            {
                var ds = DanmakuSystem.Instance;
                if (ds != null)
                {
                    ds.TargetRegistry.Unregister(this);
                }
            }
            _targetSlot = -1;
            _isCollisionEnabled = true;
            _isActive = true;
            _owner = null;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        // ──────────────── ICollisionTarget 实现 ────────────────

        /// <summary>
        /// 碰撞体：根据配置返回圆形或矩形 Hitbox。
        /// CollisionSolver 每帧读取此属性进行碰撞检测（BC-05.5）。
        /// </summary>
        public Hitbox Hitbox
        {
            get
            {
                if (_owner == null) return default;
                if (_hitboxShape == HitboxShape.Rect)
                    return new Hitbox(_owner.Position, _halfWidth, _halfHeight);
                return new Hitbox(_owner.Position, _radius);
            }
        }

        /// <summary>阵营过滤（Player 弹丸命中 Enemy 目标，反之亦然）</summary>
        public EnumCamp Faction => _owner != null ? _owner.Camp : EnumCamp.Neutral;

        /// <summary>
        /// 弹丸命中回调（BC-05.4）。
        /// 构造 DamageContext 发布 OnCollisionHit 到 EntityEventBus。
        /// SourcePosition = 子弹当前位置（从 BulletWorld 读取），用于精确击退方向。
        /// OwnerEntityId → 查找发射者 Entity → 暴击 Roll + AttackPower 覆盖。
        /// </summary>
        public void OnBulletHit(int damage, int bulletIndex)
        {
            if (!_isActive || !_isCollisionEnabled || _owner == null) return;

            var ctx = new DamageContext
            {
                BaseDamage = damage,
                AttackerId = EntityId.Invalid,
                HitType = CollisionEventType.BulletHit
            };

            var ds = DanmakuSystem.Instance;
            if (ds != null && bulletIndex >= 0 && bulletIndex < ds.BulletWorld.Capacity)
            {
                ref var core = ref ds.BulletWorld.Cores[bulletIndex];
                ctx.SourcePosition = core.Position;
                ctx.HasSourcePosition = true;

                // Sprint 4: 伤害来源标记（弹丸溯源 → damageStats 累加）
                ctx.SourceId = ds.BulletWorld.SourceTags[bulletIndex];

                // 从 OwnerEntityId 查找发射者 Entity，读取战斗属性
                if (core.OwnerEntityId != 0)
                {
                    ctx.AttackerId = new EntityId(core.OwnerEntityId);
                    var ownerEntity = FindEntityById(core.OwnerEntityId);
                    if (ownerEntity != null)
                    {
                        var ownerConfig = ownerEntity.ConfigSO;
                        // AttackPower 覆盖伤害（0 = 使用弹幕配置的固定 Damage）
                        if (ownerConfig.AttackPower > 0)
                            ctx.BaseDamage = ownerConfig.AttackPower;

                        // 暴击 Roll
                        if (ownerConfig.CritRate > 0f && Random.value < ownerConfig.CritRate)
                        {
                            ctx.IsCritical = true;
                            ctx.CritMultiplier = ownerConfig.CritDamageMultiplier;
                        }
                    }
                }
            }

            _owner.EventBus.Publish(new OnCollisionHit { Context = ctx });
        }

        /// <summary>激光命中回调（BC-05.4）。SourcePosition = 激光 Origin。</summary>
        public void OnLaserHit(int damage, int laserIndex)
        {
            if (!_isActive || !_isCollisionEnabled || _owner == null) return;

            var ctx = new DamageContext
            {
                BaseDamage = damage,
                AttackerId = EntityId.Invalid,
                HitType = CollisionEventType.LaserHit
            };

            var ds = DanmakuSystem.Instance;
            if (ds != null && laserIndex >= 0 && laserIndex < ds.LaserPool.Capacity)
            {
                ctx.SourcePosition = ds.LaserPool.Data[laserIndex].Origin;
                ctx.HasSourcePosition = true;
                ctx.SourceId = ds.LaserPool.Data[laserIndex].SourceTag;
            }

            _owner.EventBus.Publish(new OnCollisionHit { Context = ctx });
        }

        /// <summary>喷雾命中回调（BC-05.4）。SourcePosition = 喷雾 Origin。</summary>
        public void OnSprayHit(int damage, int sprayIndex)
        {
            if (!_isActive || !_isCollisionEnabled || _owner == null) return;

            var ctx = new DamageContext
            {
                BaseDamage = damage,
                AttackerId = EntityId.Invalid,
                HitType = CollisionEventType.SprayHit
            };

            var ds = DanmakuSystem.Instance;
            if (ds != null && sprayIndex >= 0 && sprayIndex < ds.SprayPool.Capacity)
            {
                ctx.SourcePosition = ds.SprayPool.Data[sprayIndex].Origin;
                ctx.HasSourcePosition = true;
            }

            _owner.EventBus.Publish(new OnCollisionHit { Context = ctx });
        }

        // ──────────────── 公开查询 ────────────────

        /// <summary>碰撞是否启用（注册失败时为 false）</summary>
        public bool IsCollisionEnabled => _isCollisionEnabled;

        /// <summary>在 TargetRegistry 中的槽位索引（-1 = 未注册）</summary>
        public int TargetSlot => _targetSlot;

        /// <summary>
        /// 池化安全检查（EC-010/EC-017）：
        /// CollisionSolver 可在遍历时检查此属性确认目标有效性。
        /// </summary>
        public bool IsAlive => _owner != null && _owner.IsAlive && !_owner.IsPendingDespawn;

        // ──────────────── 测试支持（internal） ────────────────

        /// <summary>
        /// 强制启用碰撞（仅测试用，绕过 DanmakuSystem.Instance 依赖）。
        /// </summary>
        internal void ForceEnableCollision() => _isCollisionEnabled = true;

        // ──────────────── 内部工具 ────────────────

        /// <summary>
        /// 通过 EntityId 值查找活跃 Entity（线性扫描，碰撞回调频率下可接受）。
        /// </summary>
        private static Entity FindEntityById(uint entityIdValue)
        {
            var mgr = EntityManagerAccessor.Instance;
            if (mgr == null) return null;

            var entities = mgr.ActiveEntities;
            for (int i = 0, count = entities.Count; i < count; i++)
            {
                if (entities[i].Id.Value == entityIdValue)
                    return entities[i];
            }
            return null;
        }
    }
}
