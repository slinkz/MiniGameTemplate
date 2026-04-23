using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 障碍物数据容器。OBB 碰撞，容量 64。
    /// </summary>
    public class ObstaclePool
    {
        public const int MAX_OBSTACLES = 64;

        public readonly ObstacleData[] Data = new ObstacleData[MAX_OBSTACLES];

        /// <summary>精确活跃障碍物数</summary>
        public int ActiveCount { get; private set; }

        private readonly int[] _freeSlots = new int[MAX_OBSTACLES];
        private int _freeTop;

        public ObstaclePool()
        {
            for (int i = MAX_OBSTACLES - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
        }

        // ──── 便捷 API ────

        /// <summary>
        /// 添加矩形障碍物（OBB）。
        /// rotationRad 放末尾（默认 0f），避免与现有调用 AddRect(center, size, hp, faction) 产生隐式类型转换。
        /// </summary>
        /// <param name="center">中心点（世界坐标）</param>
        /// <param name="size">尺寸（宽, 高）</param>
        /// <param name="hitPoints">生命值。0=不可摧毁</param>
        /// <param name="faction">阵营（同阵营弹丸穿透）</param>
        /// <param name="rotationRad">旋转角度（弧度，逆时针为正）。默认 0 = 无旋转，等同于旧 AABB。</param>
        /// <returns>池索引，-1 表示池满</returns>
        public int AddRect(Vector2 center, Vector2 size, int hitPoints = 0,
            BulletFaction faction = BulletFaction.Neutral, float rotationRad = 0f)
        {
            int slot = Allocate();
            if (slot < 0) return -1;

            ref var obs = ref Data[slot];
            obs.Center = center;
            obs.HalfExtents = size * 0.5f;
            obs.RotationRad = rotationRad;
            obs.Sin = Mathf.Sin(rotationRad);
            obs.Cos = Mathf.Cos(rotationRad);
            obs.HitPoints = hitPoints;
            obs.Faction = (byte)faction;
            obs.Phase = (byte)ObstaclePhase.Active;
            return slot;
        }

        /// <summary>
        /// 添加圆形障碍物（近似为正方形 OBB，旋转固定 0°）。
        /// </summary>
        /// <param name="center">中心点（世界坐标）</param>
        /// <param name="radius">半径</param>
        /// <param name="hitPoints">生命值。0=不可摧毁</param>
        /// <param name="faction">阵营</param>
        /// <returns>池索引，-1 表示池满</returns>
        public int AddCircle(Vector2 center, float radius, int hitPoints = 0,
            BulletFaction faction = BulletFaction.Neutral)
        {
            return AddRect(center, new Vector2(radius * 2f, radius * 2f), hitPoints, faction);
        }

        /// <summary>
        /// 移除障碍物（等同于 Free）。
        /// </summary>
        public void Remove(int index)
        {
            if (index < 0 || index >= MAX_OBSTACLES) return;
            if (Data[index].Phase == (byte)ObstaclePhase.Inactive) return;
            Free(index);
        }

        /// <summary>
        /// 同时更新障碍物位置和旋转。
        /// </summary>
        public void UpdateTransform(int index, Vector2 center, float rotationRad)
        {
            if (index < 0 || index >= MAX_OBSTACLES) return;
            ref var obs = ref Data[index];
            if (obs.Phase == (byte)ObstaclePhase.Inactive) return;
            obs.Center = center;
            obs.RotationRad = rotationRad;
            obs.Sin = Mathf.Sin(rotationRad);
            obs.Cos = Mathf.Cos(rotationRad);
        }

        /// <summary>
        /// 更新障碍物位置（BC-06：只更新位置，旋转不变）。
        /// </summary>
        public void UpdatePosition(int index, Vector2 center)
        {
            if (index < 0 || index >= MAX_OBSTACLES) return;
            ref var obs = ref Data[index];
            if (obs.Phase == (byte)ObstaclePhase.Inactive) return;
            obs.Center = center;
        }

        // ──── 底层 API ────

        /// <summary>分配一个空闲槽位。返回索引，-1 表示池满。</summary>
        public int Allocate()
        {
            if (_freeTop == 0) return -1;
            ActiveCount++;
            return _freeSlots[--_freeTop];
        }

        /// <summary>归还槽位。</summary>
        public void Free(int index)
        {
            Data[index] = default;
            _freeSlots[_freeTop++] = index;
            ActiveCount--;
        }

        /// <summary>清场——回收所有障碍物。必须清零 Data，否则碰撞检测可能命中幽灵障碍物。</summary>
        public void FreeAll()
        {
            _freeTop = 0;
            for (int i = MAX_OBSTACLES - 1; i >= 0; i--)
            {
                Data[i] = default;
                _freeSlots[_freeTop++] = i;
            }
            ActiveCount = 0;
        }
    }
}
