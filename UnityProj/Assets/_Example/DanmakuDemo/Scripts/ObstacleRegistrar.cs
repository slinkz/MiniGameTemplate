using MiniGameTemplate.Danmaku;
using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// 障碍物注册器——挂在带 SpriteRenderer + BoxCollider2D 的 GameObject 上，
    /// 自动将自身注册到 DanmakuSystem.ObstaclePool。
    /// 
    /// 使用方式：拖入预制体到场景中，调整 Transform 位置、旋转和 Scale 即可。
    /// 运行时会根据 BoxCollider2D 尺寸 + Transform.rotation.z 自动计算 OBB。
    /// Scene View 中 BoxCollider2D 的绿色线框即为碰撞区域所见即所得。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ObstacleRegistrar : MonoBehaviour
    {
        [Header("障碍物属性")]
        [Tooltip("生命值。0 = 不可摧毁")]
        [SerializeField] private int _hitPoints;

        [Tooltip("阵营（同阵营弹丸穿透）")]
        [SerializeField] private BulletFaction _faction = BulletFaction.Neutral;

        [Header("状态（只读）")]
        [SerializeField, HideInInspector] private int _poolIndex = -1;

        private ObstaclePool _pool;
        private SpriteRenderer _sr;
        private BoxCollider2D _collider;
        private float _lastRotZ;

        /// <summary>当前在 ObstaclePool 中的索引（-1=未注册）</summary>
        public int PoolIndex => _poolIndex;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
        }

        private void OnEnable()
        {
            // 延迟到第一帧注册，确保 DanmakuSystem 已初始化
            _poolIndex = -1;
        }

        private void Start()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Update()
        {
            // 同步 Transform 位置/旋转到障碍物数据（支持移动平台 + 旋转障碍物）
            if (_pool != null && _poolIndex >= 0)
            {
                ref var data = ref _pool.Data[_poolIndex];
                if (data.Phase == (byte)ObstaclePhase.Destroyed)
                {
                    // 被弹幕打碎了——视觉反馈
                    OnDestroyed();
                    return;
                }

                float curRotZ = transform.eulerAngles.z;
                Vector2 pos = (Vector2)transform.position;
                // worldCenter 计算：offset 非零时需旋转，为零时直接用 position（零三角函数）
                Vector2 curCenter = (_collider.offset == Vector2.zero)
                    ? pos
                    : pos + RotateVector(_collider.offset, curRotZ);

                if (Mathf.Approximately(curRotZ, _lastRotZ))
                {
                    // 旋转没变——只更新位置，避免 Pool 侧三角函数调用
                    _pool.UpdatePosition(_poolIndex, curCenter);
                }
                else
                {
                    _pool.UpdateTransform(_poolIndex, curCenter, curRotZ * Mathf.Deg2Rad);
                    _lastRotZ = curRotZ;
                }
            }
        }

        // ──── 注册 / 注销 ────

        private void Register()
        {
            var system = DanmakuSystem.Instance;
            if (system == null)
            {
                Debug.LogWarning($"[ObstacleRegistrar] DanmakuSystem 未就绪，{name} 无法注册。");
                return;
            }

            _pool = system.ObstaclePool;
            if (_pool == null) return;

            // 从 BoxCollider2D 尺寸 + Transform 计算 OBB 参数
            float rotZ = transform.eulerAngles.z;
            Vector2 worldCenter = (Vector2)transform.position
                                + RotateVector(_collider.offset, rotZ);
            // lossyScale 取 Abs 防负 scale（R6 缓解）
            // 注意：不支持有旋转的父级 Transform，lossyScale 在此场景下不准确。
            Vector2 size = new Vector2(
                _collider.size.x * Mathf.Abs(transform.lossyScale.x),
                _collider.size.y * Mathf.Abs(transform.lossyScale.y));
            float rotRad = rotZ * Mathf.Deg2Rad;

            _poolIndex = _pool.AddRect(worldCenter, size, _hitPoints, _faction, rotRad);
            _lastRotZ = rotZ;

            if (_poolIndex < 0)
                Debug.LogWarning($"[ObstacleRegistrar] {name} 注册失败——ObstaclePool 已满 (max={ObstaclePool.MAX_OBSTACLES})");
        }

        private void Unregister()
        {
            if (_pool != null && _poolIndex >= 0)
            {
                _pool.Remove(_poolIndex);
                _poolIndex = -1;
            }
        }

        // ──── 被摧毁时的视觉反馈 ────

        private void OnDestroyed()
        {
            // 简单处理：变暗 + 半透明
            if (_sr != null)
            {
                _sr.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
            }

            // 取消注册，停止 Update
            _poolIndex = -1;
            enabled = false;
        }

        // ──── 辅助方法 ──── 

        /// <summary>将 2D 向量绕原点旋转 angleDeg 度。</summary>
        private static Vector2 RotateVector(Vector2 v, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // ──── 编辑器辅助 ────

#if UNITY_EDITOR
        /// <summary>
        /// Reset() 自动配置 BoxCollider2D。
        /// 编辑器中添加此组件时自动调用。
        /// </summary>
        private void Reset()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                // 如果已有 SpriteRenderer，从其尺寸初始化 collider size
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    col.size = sr.sprite.bounds.size;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 编辑器中选中时显示 OBB 碰撞区域（旋转 Gizmo）
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            Gizmos.color = _hitPoints > 0
                ? new Color(1f, 0.6f, 0.2f, 0.4f)
                : new Color(0.4f, 0.8f, 1f, 0.4f);

            float rotZ = transform.eulerAngles.z;
            Vector3 center = transform.position + (Vector3)RotateVector(col.offset, rotZ);
            Vector3 size = new Vector3(
                col.size.x * Mathf.Abs(transform.lossyScale.x),
                col.size.y * Mathf.Abs(transform.lossyScale.y),
                0.01f);

            // 使用 Matrix4x4.TRS 旋转 Gizmo
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, rotZ), Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
