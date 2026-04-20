using MiniGameTemplate.Rendering;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    // DanmakuSystem.API.cs — Fire/Register/Clear 等公开 API
    public partial class DanmakuSystem
    {
        // ──── 属性访问器 ────

        /// <summary>弹丸世界容器（外部发射用）</summary>
        public BulletWorld BulletWorld => _bulletWorld;

        /// <summary>激光池</summary>
        public LaserPool LaserPool => _laserPool;

        /// <summary>喷雾池</summary>
        public SprayPool SprayPool => _sprayPool;

        /// <summary>障碍物池</summary>
        public ObstaclePool ObstaclePool => _obstaclePool;

        /// <summary>挂载源注册表（激光/喷雾跟随旋转物体）</summary>
        public AttachSourceRegistry AttachRegistry => _attachRegistry;

        /// <summary>碰撞目标注册表</summary>
        public TargetRegistry TargetRegistry => _targetRegistry;

        /// <summary>调度器（外部 Schedule 用）</summary>
        public PatternScheduler Scheduler => _scheduler;

        /// <summary>发射器驱动器</summary>
        public SpawnerDriver SpawnerDriver => _spawnerDriver;

        /// <summary>类型注册表（框架内部使用）</summary>
        internal DanmakuTypeRegistry TypeRegistry => _typeRegistry;

        /// <summary>当前难度配置</summary>
        public DifficultyProfileSO Difficulty
        {
            get => _difficulty;
            set => _difficulty = value;
        }

        // ──── RuntimeAtlas 统计（R4.3：Debug HUD 用） ────

        /// <summary>
        /// 收集所有子系统的 RuntimeAtlas 统计快照。
        /// 返回数组：[0]=Bullet, [1]=VFX, [2]=DamageNumber。
        /// 对应 Atlas 未启用的条目为 null。
        /// </summary>
        public (string Label, RuntimeAtlasStats? Stats)[] GetAllAtlasStats()
        {
            return new (string, RuntimeAtlasStats?)[]
            {
                ("Bullet", _bulletRenderer?.GetAtlasStats()),
                ("VFX", _vfxRuntime?.GetAtlasStats()),
                ("DmgNum", _damageNumbers?.GetAtlasStats()),
            };
        }

        // ──── 公开 API ────

        /// <summary>
        /// 设置玩家碰撞体信息（向后兼容便捷方法）。
        /// 内部通过 PlayerCollisionTarget 适配器注册到 TargetRegistry。
        /// </summary>
        public void SetPlayer(Transform playerTransform, float radius)
        {
            if (_builtinPlayerTarget != null)
            {
                _targetRegistry.Unregister(_builtinPlayerTarget);
            }

            if (playerTransform != null)
            {
                _builtinPlayerTarget = new PlayerCollisionTarget(playerTransform, radius, _onPlayerHit, _onDamageDealt);
                _targetRegistry.Register(_builtinPlayerTarget);
            }
            else
            {
                _builtinPlayerTarget = null;
            }
        }

        /// <summary>注册一个碰撞目标到弹幕系统。</summary>
        public bool RegisterTarget(ICollisionTarget target)
        {
            return _targetRegistry.Register(target) >= 0;
        }

        /// <summary>注销一个碰撞目标。</summary>
        public void UnregisterTarget(ICollisionTarget target)
        {
            _targetRegistry.Unregister(target);
        }

        /// <summary>发射弹幕组合。</summary>
        public void FireGroup(PatternGroupSO group, Vector2 origin, float baseAngle)
        {
            group = ResolvePatternOverride(group);

            Vector2 playerPos = _builtinPlayerTarget != null
                ? _builtinPlayerTarget.Hitbox.Center
                : Vector2.zero;
            _scheduler.Schedule(group, origin, baseAngle, playerPos);
        }

        /// <summary>发射单个弹幕。</summary>
        public void FireBullets(BulletPatternSO pattern, Vector2 origin, float baseAngle)
        {
            _scheduler.ScheduleSingle(pattern, origin, baseAngle);
        }

        /// <summary>
        /// 发射激光（Detached 模式——发射后固定不动）。
        /// </summary>
        public int FireLaser(LaserTypeSO type, Vector2 origin, float angle,
            float length, float lifetime = 0f)
        {
            return FireLaserInternal(type, origin, angle, length, lifetime, 0);
        }

        /// <summary>
        /// 发射激光（Attached 模式——每帧跟随挂载 Transform 的位置和朝向）。
        /// </summary>
        public int FireLaser(LaserTypeSO type, Transform source, float length,
            float lifetime = 0f, Vector2 localOffset = default, float angleOffset = 0f)
        {
            if (type == null || source == null)
                return -1;

            byte attachId = _attachRegistry.Register(source, localOffset, angleOffset);
            if (attachId == 0) return -1;

            Vector2 origin = _attachRegistry.GetWorldPosition(attachId, (Vector2)source.position);
            float angle = _attachRegistry.GetWorldAngle(attachId, source.eulerAngles.z * Mathf.Deg2Rad);

            return FireLaserInternal(type, origin, angle, length, lifetime, attachId);
        }

        /// <summary>
        /// 发射喷雾（Detached 模式——发射后固定不动）。
        /// </summary>
        public int FireSpray(SprayTypeSO type, Vector2 origin, float direction,
            float coneAngle, float range, float lifetime)
        {
            return FireSprayInternal(type, origin, direction, coneAngle, range, lifetime, 0);
        }

        /// <summary>
        /// 发射喷雾（Attached 模式——每帧跟随挂载 Transform）。
        /// </summary>
        public int FireSpray(SprayTypeSO type, Transform source,
            float coneAngle, float range, float lifetime,
            Vector2 localOffset = default, float angleOffset = 0f)
        {
            if (type == null || source == null)
                return -1;

            byte attachId = _attachRegistry.Register(source, localOffset, angleOffset);
            if (attachId == 0) return -1;

            Vector2 origin = _attachRegistry.GetWorldPosition(attachId, (Vector2)source.position);
            float direction = _attachRegistry.GetWorldAngle(attachId, source.eulerAngles.z * Mathf.Deg2Rad);

            return FireSprayInternal(type, origin, direction, coneAngle, range, lifetime, attachId);
        }

        /// <summary>
        /// 清场——回收所有弹丸/激光/喷雾/障碍物/挂载源/调度任务。
        /// 注意：不清除 TargetRegistry 的注册（目标对象的生命周期由外部管理）。
        /// </summary>
        public void ClearAll()
        {
            // 先停止所有喷雾附着 VFX
            if (_vfxRuntime != null)
            {
                for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
                {
                    ref var spray = ref _sprayPool.Data[i];
                    if (spray.Phase != 0 && spray.VfxSlot >= 0)
                    {
                        _vfxRuntime.StopAttached(spray.VfxSlot);
                        spray.VfxSlot = -1;
                    }
                }
            }

            _bulletWorld.FreeAll();
            _laserPool.FreeAll();
            _sprayPool.FreeAll();
            _obstaclePool.FreeAll();
            _attachRegistry.FreeAll();
            _scheduler.ClearAll();
            _spawnerDriver.ClearAll();
            _trailPool.FreeAll();
        }

        /// <summary>
        /// 清屏 API——清除所有活跃弹丸并通知特效桥接层。
        /// 桥接层可将弹丸转化为特效或得分。
        /// </summary>
        public void ClearAllBulletsWithEffect()
        {
            var cores = _bulletWorld.Cores;
            int capacity = _bulletWorld.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref var core = ref cores[i];
                if ((core.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;

                var bulletType = _typeRegistry.GetBulletType(core.TypeIndex);
                _effectsBridge?.OnBulletCleared(i, core.Position, bulletType);
            }

            _bulletWorld.FreeAll();
        }

        // ──── 内部 ────

        private PatternGroupSO ResolvePatternOverride(PatternGroupSO group)
        {
            if (_difficulty == null || _difficulty.PatternOverrides == null) return group;

            var overrides = _difficulty.PatternOverrides;
            for (int i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Original == group && overrides[i].Replacement != null)
                    return overrides[i].Replacement;
            }
            return group;
        }

        private int FireLaserInternal(LaserTypeSO type, Vector2 origin, float angle,
            float length, float lifetime, byte attachId)
        {
            if (type == null)
            {
                if (attachId != 0) _attachRegistry.Release(attachId);
                return -1;
            }

            int index = _laserPool.Allocate();
            if (index < 0)
            {
                if (attachId != 0) _attachRegistry.Release(attachId);
                return -1;
            }

            byte typeIndex = _typeRegistry.GetOrRegisterLaser(type);
            ref var laser = ref _laserPool.Data[index];
            laser.Origin = origin;
            laser.Angle = angle;
            laser.Length = length;
            laser.MaxWidth = type.MaxWidth;
            laser.Width = type.MaxWidth * 0.05f;
            laser.Lifetime = lifetime > 0f ? lifetime : type.TotalDuration;
            laser.TickInterval = type.TickInterval;
            laser.DamagePerTick = type.DamagePerTick;
            laser.Phase = 1;
            laser.LaserTypeIndex = typeIndex;
            laser.MaxReflections = type.MaxReflections;
            laser.AttachId = attachId;
            laser.Faction = (byte)type.Faction;
            laser.Elapsed = 0f;
            laser.TickTimer = 0f;
            laser.SegmentCount = 0;
            laser.VisualLength = 0f;

            return index;
        }

        private int FireSprayInternal(SprayTypeSO type, Vector2 origin, float direction,
            float coneAngle, float range, float lifetime, byte attachId)
        {
            if (type == null)
            {
                if (attachId != 0) _attachRegistry.Release(attachId);
                return -1;
            }

            int index = _sprayPool.Allocate();
            if (index < 0)
            {
                if (attachId != 0) _attachRegistry.Release(attachId);
                return -1;
            }

            byte typeIndex = _typeRegistry.GetOrRegisterSpray(type);
            ref var spray = ref _sprayPool.Data[index];
            spray.Origin = origin;
            spray.Direction = direction;
            spray.ConeAngle = coneAngle;
            spray.Range = range;
            spray.Lifetime = lifetime;
            spray.TickInterval = type.TickInterval;
            spray.DamagePerTick = type.DamagePerTick;
            spray.Phase = 1;
            spray.SprayTypeIndex = typeIndex;
            spray.AttachId = attachId;
            spray.Faction = (byte)type.Faction;
            spray.Elapsed = 0f;
            spray.TickTimer = 0f;
            spray.VfxSlot = -1;  // Phase 3：VFX 附着将在 SprayUpdater 首帧启动

            return index;
        }
    }
}
