using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光/喷雾的挂载源注册表。
    /// <para>
    /// 设计目标：让激光/喷雾每帧自动同步挂载 Transform 的位置和朝向。
    /// 激光/喷雾数据层只存一个 <c>byte AttachId</c>（0 = 未挂载），
    /// Updater 通过本注册表查表取得最新 Transform 数据。
    /// </para>
    /// <para>容量 24（激光 16 + 喷雾 8 足够覆盖最坏情况）。</para>
    /// </summary>
    public class AttachSourceRegistry
    {
        /// <summary>最大挂载源数量</summary>
        public const int MAX_SOURCES = 24;

        /// <summary>
        /// 挂载源 Transform 数组。索引 0 保留（0 = 未挂载），有效范围 [1, MAX_SOURCES)。
        /// </summary>
        public readonly Transform[] Transforms = new Transform[MAX_SOURCES];

        /// <summary>
        /// 每个挂载源的角度偏移（弧度）。
        /// 最终角度 = Transform.rotation.eulerAngles.z * Deg2Rad + AngleOffsets[id]。
        /// </summary>
        public readonly float[] AngleOffsets = new float[MAX_SOURCES];

        /// <summary>
        /// 可选的追踪目标解析器。每帧调用获取最新目标 Transform。
        /// 相比固定 TargetTransforms，支持目标死亡后自动切换新目标。
        /// null = 不追踪（使用固定 AngleOffset）。
        /// </summary>
        public readonly System.Func<Transform>[] TargetResolvers = new System.Func<Transform>[MAX_SOURCES];

        /// <summary>
        /// 每个挂载源的局部位置偏移（Transform 局部空间）。
        /// 最终世界位置 = Transform.TransformPoint(LocalOffsets[id])。
        /// </summary>
        public readonly Vector2[] LocalOffsets = new Vector2[MAX_SOURCES];

        /// <summary>
        /// 引用计数——同一个 Transform 可以被多条激光/喷雾共享。
        /// 当引用计数归零时，槽位可被回收。
        /// </summary>
        private readonly int[] _refCounts = new int[MAX_SOURCES];

        /// <summary>空闲栈</summary>
        private readonly int[] _freeSlots = new int[MAX_SOURCES];
        private int _freeTop;

        public AttachSourceRegistry()
        {
            // 索引 0 保留，不加入空闲栈
            for (int i = MAX_SOURCES - 1; i >= 1; i--)
                _freeSlots[_freeTop++] = i;
        }

        /// <summary>
        /// 注册一个挂载源。返回 ID（1 ~ MAX_SOURCES-1），0 表示池满。
        /// 注册后引用计数初始为 1，无需额外调用 AddRef。
        /// </summary>
        /// <param name="source">挂载的 Transform</param>
        /// <param name="localOffset">局部位置偏移</param>
        /// <param name="angleOffset">角度偏移（弧度）</param>
        public byte Register(Transform source, Vector2 localOffset = default, float angleOffset = 0f)
        {
            if (_freeTop == 0) return 0; // 池满
            int id = _freeSlots[--_freeTop];
            Transforms[id] = source;
            LocalOffsets[id] = localOffset;
            AngleOffsets[id] = angleOffset;
            _refCounts[id] = 1;
            return (byte)id;
        }

        /// <summary>
        /// 为已注册的挂载源设置追踪目标解析器。
        /// 每帧调用 resolver 获取最新目标 Transform，支持目标死亡后自动切换新目标。
        /// </summary>
        /// <param name="id">AttachId（由 Register 返回）</param>
        /// <param name="resolver">目标 Transform 解析器（null = 取消追踪）</param>
        public void SetTarget(byte id, System.Func<Transform> resolver)
        {
            if (id == 0) return;
            TargetResolvers[id] = resolver;
        }

        /// <summary>
        /// 增加引用计数（每次将 AttachId 写入激光/喷雾时调用）。
        /// </summary>
        public void AddRef(byte id)
        {
            if (id == 0) return;
            _refCounts[id]++;
        }

        /// <summary>
        /// 减少引用计数。当计数归零且 Transform 已被销毁或不再需要时，自动回收槽位。
        /// </summary>
        public void Release(byte id)
        {
            if (id == 0) return;
            _refCounts[id]--;
            if (_refCounts[id] <= 0)
            {
                Transforms[id] = null;
                LocalOffsets[id] = default;
                AngleOffsets[id] = 0f;
                TargetResolvers[id] = null;
                _refCounts[id] = 0;
                _freeSlots[_freeTop++] = id;
            }
        }

        /// <summary>
        /// 取最新的世界位置。Transform 已销毁时返回 fallback。
        /// </summary>
        public Vector2 GetWorldPosition(byte id, Vector2 fallback)
        {
            if (id == 0) return fallback;
            var t = Transforms[id];
            if (t == null) return fallback;
            Vector2 local = LocalOffsets[id];
            if (local.x == 0f && local.y == 0f)
                return (Vector2)t.position;
            return (Vector2)t.TransformPoint(new Vector3(local.x, local.y, 0f));
        }

        /// <summary>
        /// 该挂载源是否配置了目标追踪（有 resolver）。
        /// 用于 LaserUpdater 区分"追踪模式目标丢失→应回收"和"非追踪模式→正常存活"。
        /// </summary>
        public bool HasTargetResolver(byte id)
        {
            return id != 0 && TargetResolvers[id] != null;
        }

        /// <summary>
        /// 获取发射口到追踪目标的距离。无目标/无 resolver 时返回 -1。
        /// 配合 LaserUpdater 使用，每帧动态更新 laser.Length 使激光末端精确到达目标。
        /// </summary>
        /// <param name="id">AttachId</param>
        /// <param name="cachedTarget">
        /// 由 GetWorldAngle 同帧 resolve 出的目标 Transform（避免重复调用 resolver）。
        /// 传 null 时会自行调用 resolver（兼容独立调用场景）。
        /// </param>
        public float GetDistanceToTarget(byte id, Transform cachedTarget = null)
        {
            if (id == 0) return -1f;
            var t = Transforms[id];
            if (t == null) return -1f;

            var target = cachedTarget;
            if (target == null)
            {
                var resolver = TargetResolvers[id];
                if (resolver == null) return -1f;
                target = resolver();
            }
            if (target == null) return -1f;

            Vector2 origin = GetWorldPosition(id, (Vector2)t.position);
            Vector2 targetPos = (Vector2)target.position;
            return Vector2.Distance(origin, targetPos);
        }

        /// <summary>
        /// 取最新的世界角度（弧度）。Transform 已销毁时返回 fallback。
        /// 如果设置了 TargetResolver 且返回有效 Transform，角度将朝向目标位置。
        /// resolvedTarget 输出本帧 resolve 的目标 Transform（可传入 GetDistanceToTarget 避免重复调用 resolver）。
        /// </summary>
        public float GetWorldAngle(byte id, float fallback, out Transform resolvedTarget)
        {
            resolvedTarget = null;
            if (id == 0) return fallback;
            var t = Transforms[id];
            if (t == null) return fallback;

            // 有追踪目标时：每帧通过 resolver 获取最新目标
            var resolver = TargetResolvers[id];
            if (resolver != null)
            {
                resolvedTarget = resolver();
                if (resolvedTarget != null)
                {
                    Vector2 origin = GetWorldPosition(id, (Vector2)t.position);
                    Vector2 targetPos = (Vector2)resolvedTarget.position;
                    Vector2 toTarget = targetPos - origin;
                    if (toTarget.sqrMagnitude > 0.0001f)
                        return Mathf.Atan2(toTarget.y, toTarget.x);
                }
            }

            // 无追踪目标：使用固定 angleOffset
            return t.eulerAngles.z * Mathf.Deg2Rad + AngleOffsets[id];
        }

        /// <summary>
        /// 取最新的世界角度（弧度）。不需要 resolvedTarget 时使用此重载。
        /// </summary>
        public float GetWorldAngle(byte id, float fallback)
        {
            return GetWorldAngle(id, fallback, out _);
        }

        /// <summary>清场——释放全部挂载源。</summary>
        public void FreeAll()
        {
            for (int i = 1; i < MAX_SOURCES; i++)
            {
                Transforms[i] = null;
                LocalOffsets[i] = default;
                AngleOffsets[i] = 0f;
                TargetResolvers[i] = null;
                _refCounts[i] = 0;
            }
            _freeTop = 0;
            for (int i = MAX_SOURCES - 1; i >= 1; i--)
                _freeSlots[_freeTop++] = i;
        }
    }
}
