using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 自动瞄准组件——定频搜索敌对阵营最近 Entity，暴露锁定目标信息。
    /// 实现 ITargetProvider 接口，供 AI Action / AttackComponent 读取。
    ///
    /// ComponentType.AutoAim = 5
    /// TickOrder = 120（Attack 之前，Decision 之后）
    ///
    /// 设计决策：
    /// - 定频搜索（默认 0.2s [占位符]），不是每帧——省 CPU
    /// - 只锁定最近目标（最近优先策略），不做优先级/仇恨表
    /// - 目标失效时立即重搜（v0.4 GD-002，默认行为）
    /// - Init 时立即执行一次 SearchTarget
    /// </summary>
    public sealed class AutoAimComponent : IEntityComponent, ITickable, ITargetProvider
    {
        // ── IEntityComponent ──
        public ComponentType Type => ComponentType.AutoAim;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ── ITickable ──
        public int TickOrder => TickOrders.AutoAim; // 120

        // ── ITargetProvider ──
        public bool HasTarget => _currentTarget != null
                              && _currentTarget.IsAlive
                              && !_currentTarget.IsPendingDespawn;
        public Vector2 TargetPosition => HasTarget ? _currentTarget.Position : _owner.Position;
        public float DistanceToTarget => HasTarget
            ? (_currentTarget.Position - _owner.Position).magnitude
            : float.MaxValue;

        // ── 公开状态 ──
        /// <summary>当前瞄准方向（归一化），AttackComponent 读取此值</summary>
        public Vector2 AimDirection { get; private set; }

        // ── 配置 ──
        private Entity _owner;
        private float _searchRadius;
        private float _searchInterval;
        private float _timer;
        private Entity _currentTarget;

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            _searchRadius = owner.ConfigSO.AutoAimRadius;
            _searchInterval = owner.ConfigSO.AutoAimSearchInterval;
            _timer = 0f;
            _currentTarget = null;
            AimDirection = Vector2.up;
            IsActive = _searchRadius > 0f;

            // v0.4（SA-011）：Init 时立即搜索。
            // 此时 Entity 自身尚未加入 active list（先 Init → 后 Register），
            // 因此不会瞄准自己。
            if (IsActive) SearchTarget();
            if (HasTarget)
            {
                Vector2 dir = _currentTarget.Position - _owner.Position;
                if (dir.sqrMagnitude > 0.001f)
                    AimDirection = dir.normalized;
            }
        }

        public void Reset()
        {
            _currentTarget = null;
            _owner = null;
            _timer = 0f;
            AimDirection = Vector2.up;
            IsActive = false;
        }

        // ── Tick ──

        public void Tick(float dt)
        {
            // v0.4（GD-002）：目标失效时立即重搜
            if (_currentTarget != null && (!_currentTarget.IsAlive || _currentTarget.IsPendingDespawn))
            {
                _currentTarget = null;
                SearchTarget();
            }

            // 定频搜索
            _timer += dt;
            if (_timer >= _searchInterval)
            {
                _timer -= _searchInterval;
                SearchTarget();
            }

            // 更新瞄准方向
            if (HasTarget)
            {
                Vector2 dir = _currentTarget.Position - _owner.Position;
                if (dir.sqrMagnitude > 0.001f)
                    AimDirection = dir.normalized;
            }
            else
            {
                float rad = _owner.Rotation * Mathf.Deg2Rad;
                AimDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }
        }

        private void SearchTarget()
        {
            var mgr = EntityManagerAccessor.Instance;
            Debug.Assert(mgr != null, "[AutoAimComponent] EntityManager not initialized!");
            if (mgr == null) return;

            var hostileCamp = CampUtility.GetHostileCamp(_owner.Camp);
            _currentTarget = mgr.FindNearestEntity(_owner.Position, _searchRadius, hostileCamp);
        }
    }
}
