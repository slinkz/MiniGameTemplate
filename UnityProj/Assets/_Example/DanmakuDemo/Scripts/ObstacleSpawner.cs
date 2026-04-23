using MiniGameTemplate.Danmaku;
using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// 障碍物生成器——在 Inspector 中配置障碍物列表，Start 时自动注册到 DanmakuSystem.ObstaclePool。
    /// 支持运行时通过 API 动态增删，Scene View 中通过 Gizmo 可视化所有障碍物（含旋转）。
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct ObstacleDefinition
        {
            [Tooltip("障碍物中心位置（世界坐标）")]
            public Vector2 Center;

            [Tooltip("障碍物尺寸（宽, 高）")]
            public Vector2 Size;

            [Tooltip("旋转角度（度，逆时针为正）")]
            public float Rotation;

            [Tooltip("生命值。0=不可摧毁")]
            public int HitPoints;

            [Tooltip("阵营（同阵营弹丸穿透）")]
            public BulletFaction Faction;
        }

        [Header("预配置障碍物")]
        [Tooltip("启动时自动注册的障碍物列表")]
        [SerializeField] private ObstacleDefinition[] _obstacles = new[]
        {
            // 默认布局：3 个挡板，模拟竖版射击游戏中的掩体
            new ObstacleDefinition { Center = new Vector2(-2f, 0f), Size = new Vector2(1.5f, 0.4f), Rotation = 0f, HitPoints = 0, Faction = BulletFaction.Neutral },
            new ObstacleDefinition { Center = new Vector2(0f, -0.5f), Size = new Vector2(1f, 1f), Rotation = 0f, HitPoints = 50, Faction = BulletFaction.Neutral },
            new ObstacleDefinition { Center = new Vector2(2f, 0f), Size = new Vector2(1.5f, 0.4f), Rotation = 0f, HitPoints = 0, Faction = BulletFaction.Neutral },
        };

        [Header("可视化")]
        [Tooltip("不可摧毁障碍物的 Gizmo 颜色")]
        [SerializeField] private Color _indestructibleColor = new Color(0.4f, 0.8f, 1f, 0.6f);

        [Tooltip("可摧毁障碍物的 Gizmo 颜色")]
        [SerializeField] private Color _destructibleColor = new Color(1f, 0.6f, 0.2f, 0.6f);

        [Tooltip("已摧毁障碍物的 Gizmo 颜色")]
        [SerializeField] private Color _destroyedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        // 运行时：每个 Definition 对应的 Pool 索引（-1=未注册）
        private int[] _poolIndices;
        private ObstaclePool _pool;
        private bool _spawned;

        /// <summary>当前已注册的障碍物数量</summary>
        public int SpawnedCount
        {
            get
            {
                if (_poolIndices == null) return 0;
                int count = 0;
                for (int i = 0; i < _poolIndices.Length; i++)
                    if (_poolIndices[i] >= 0) count++;
                return count;
            }
        }

        private void Start()
        {
            var system = DanmakuSystem.Instance;
            if (system == null)
            {
                Debug.LogError("[ObstacleSpawner] DanmakuSystem 未就绪，无法注册障碍物。");
                enabled = false;
                return;
            }

            _pool = system.ObstaclePool;
            SpawnAll();
        }

        /// <summary>
        /// 注册所有预配置的障碍物到 Pool。
        /// </summary>
        public void SpawnAll()
        {
            if (_pool == null) return;

            _poolIndices = new int[_obstacles.Length];
            for (int i = 0; i < _obstacles.Length; i++)
            {
                ref var def = ref _obstacles[i];
                float rotRad = def.Rotation * Mathf.Deg2Rad;
                int slot = _pool.AddRect(def.Center, def.Size, def.HitPoints, def.Faction, rotRad);
                _poolIndices[i] = slot;

                if (slot < 0)
                    Debug.LogWarning($"[ObstacleSpawner] 障碍物 [{i}] 注册失败（Pool 已满？容量={ObstaclePool.MAX_OBSTACLES}）");
            }

            _spawned = true;
            Debug.Log($"[ObstacleSpawner] 已注册 {SpawnedCount}/{_obstacles.Length} 个障碍物");
        }

        /// <summary>
        /// 移除所有已注册的障碍物。
        /// </summary>
        public void RemoveAll()
        {
            if (_pool == null || _poolIndices == null) return;

            for (int i = 0; i < _poolIndices.Length; i++)
            {
                if (_poolIndices[i] >= 0)
                {
                    _pool.Remove(_poolIndices[i]);
                    _poolIndices[i] = -1;
                }
            }

            _spawned = false;
            Debug.Log("[ObstacleSpawner] 已移除所有障碍物");
        }

        /// <summary>
        /// 切换：如果已生成则清除，否则重新生成。
        /// </summary>
        public void Toggle()
        {
            if (_spawned)
                RemoveAll();
            else
                SpawnAll();
        }

        // ──── Gizmo 可视化（含旋转） ────

        private void OnDrawGizmos()
        {
            if (_obstacles == null) return;

            for (int i = 0; i < _obstacles.Length; i++)
            {
                ref var def = ref _obstacles[i];

                // 运行时：检查是否被摧毁
                bool destroyed = false;
                if (Application.isPlaying && _poolIndices != null && i < _poolIndices.Length)
                {
                    int idx = _poolIndices[i];
                    if (idx >= 0 && _pool != null && _pool.Data[idx].Phase == (byte)ObstaclePhase.Destroyed)
                        destroyed = true;
                    if (idx < 0)
                        destroyed = true; // 注册失败也视为无效
                }

                if (destroyed)
                    Gizmos.color = _destroyedColor;
                else if (def.HitPoints > 0)
                    Gizmos.color = _destructibleColor;
                else
                    Gizmos.color = _indestructibleColor;

                Vector3 center = new Vector3(def.Center.x, def.Center.y, 0f);
                Vector3 size = new Vector3(def.Size.x, def.Size.y, 0.01f);

                // 旋转 Gizmo
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, def.Rotation), Vector3.one);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;

                // 显示 HP 标签（编辑器模式下）
#if UNITY_EDITOR
                string label = def.HitPoints > 0 ? $"HP:{def.HitPoints}" : "∞";
                UnityEditor.Handles.Label(center + Vector3.up * (def.Size.y * 0.5f + 0.15f), label);
#endif
            }
        }
    }
}
