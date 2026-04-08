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
        /// 取最新的世界角度（弧度）。Transform 已销毁时返回 fallback。
        /// </summary>
        public float GetWorldAngle(byte id, float fallback)
        {
            if (id == 0) return fallback;
            var t = Transforms[id];
            if (t == null) return fallback;
            return t.eulerAngles.z * Mathf.Deg2Rad + AngleOffsets[id];
        }

        /// <summary>清场——释放全部挂载源。</summary>
        public void FreeAll()
        {
            for (int i = 1; i < MAX_SOURCES; i++)
            {
                Transforms[i] = null;
                LocalOffsets[i] = default;
                AngleOffsets[i] = 0f;
                _refCounts[i] = 0;
            }
            _freeTop = 0;
            for (int i = MAX_SOURCES - 1; i >= 1; i--)
                _freeSlots[_freeTop++] = i;
        }
    }
}
