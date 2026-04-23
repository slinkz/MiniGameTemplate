using MiniGameTemplate.Danmaku;
using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// 障碍物注册器——挂在带 SpriteRenderer + Collider2D 的 GameObject 上，
    /// 自动将自身注册到 DanmakuSystem.ObstaclePool。
    /// 
    /// 碰撞形状自动识别：
    ///   - BoxCollider2D   → AddRect（OBB，支持任意 Z 轴旋转）
    ///   - CircleCollider2D → AddCircle（正方形 OBB 近似圆，旋转无意义）
    /// 两种 Collider 只能挂其中一种，同时存在时优先 CircleCollider2D。
    /// 
    /// Scene View 中 Collider 的绿色线框即为碰撞区域所见即所得。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
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

        // Collider 识别——二选一
        private BoxCollider2D _boxCollider;
        private CircleCollider2D _circleCollider;
        private bool _isCircle;

        private float _lastRotZ;

        /// <summary>当前在 ObstaclePool 中的索引（-1=未注册）</summary>
        public int PoolIndex => _poolIndex;

        /// <summary>当前障碍物是否为圆形</summary>
        public bool IsCircle => _isCircle;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            DetectColliderType();
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

                Vector2 pos = (Vector2)transform.position;

                if (_isCircle)
                {
                    // 圆形障碍物：只需同步位置，旋转无意义
                    Vector2 curCenter = (_circleCollider.offset == Vector2.zero)
                        ? pos
                        : pos + (Vector2)(transform.localToWorldMatrix.MultiplyVector(_circleCollider.offset));
                    _pool.UpdatePosition(_poolIndex, curCenter);
                }
                else
                {
                    // OBB 障碍物：同步位置 + 旋转
                    float curRotZ = transform.eulerAngles.z;
                    Vector2 curCenter = (_boxCollider.offset == Vector2.zero)
                        ? pos
                        : pos + RotateVector(_boxCollider.offset, curRotZ);

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
        }

        // ──── Collider 类型检测 ────

        /// <summary>
        /// 识别当前 GameObject 上的 Collider 类型。
        /// CircleCollider2D 优先于 BoxCollider2D。
        /// </summary>
        private void DetectColliderType()
        {
            _circleCollider = GetComponent<CircleCollider2D>();
            if (_circleCollider != null)
            {
                _isCircle = true;
                _boxCollider = null;
                return;
            }

            _boxCollider = GetComponent<BoxCollider2D>();
            if (_boxCollider != null)
            {
                _isCircle = false;
                return;
            }

            Debug.LogError($"[ObstacleRegistrar] {name} 没有 BoxCollider2D 也没有 CircleCollider2D！请添加其中一种。");
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

            if (_isCircle)
                RegisterCircle();
            else
                RegisterBox();
        }

        private void RegisterCircle()
        {
            Vector2 pos = (Vector2)transform.position;
            Vector2 worldCenter = (_circleCollider.offset == Vector2.zero)
                ? pos
                : pos + (Vector2)(transform.localToWorldMatrix.MultiplyVector(_circleCollider.offset));

            // 取 X/Y 轴最大缩放值作为半径缩放因子
            float maxScale = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y));
            float worldRadius = _circleCollider.radius * maxScale;

            _poolIndex = _pool.AddCircle(worldCenter, worldRadius, _hitPoints, _faction);

            if (_poolIndex < 0)
                Debug.LogWarning($"[ObstacleRegistrar] {name} 注册失败——ObstaclePool 已满 (max={ObstaclePool.MAX_OBSTACLES})");
        }

        private void RegisterBox()
        {
            float rotZ = transform.eulerAngles.z;
            Vector2 worldCenter = (Vector2)transform.position
                                + RotateVector(_boxCollider.offset, rotZ);
            // lossyScale 取 Abs 防负 scale（R6 缓解）
            // 注意：不支持有旋转的父级 Transform，lossyScale 在此场景下不准确。
            Vector2 size = new Vector2(
                _boxCollider.size.x * Mathf.Abs(transform.lossyScale.x),
                _boxCollider.size.y * Mathf.Abs(transform.lossyScale.y));
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
        /// Reset() 自动配置 Collider。
        /// 编辑器中添加此组件时自动调用。
        /// 如果已有 CircleCollider2D 则配置圆形；否则自动添加并配置 BoxCollider2D。
        /// </summary>
        private void Reset()
        {
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                circle.isTrigger = true;
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    // 取 sprite 最大半边长度作为半径
                    var bounds = sr.sprite.bounds;
                    circle.radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
                }
                return;
            }

            var box = GetComponent<BoxCollider2D>();
            if (box == null)
            {
                // 默认添加 BoxCollider2D（最常用情况）
                box = gameObject.AddComponent<BoxCollider2D>();
            }
            box.isTrigger = true;
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                box.size = spriteRenderer.sprite.bounds.size;
        }

        private void OnDrawGizmosSelected()
        {
            // 编辑器中选中时显示碰撞区域
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                DrawCircleGizmo(circle);
                return;
            }

            var box = GetComponent<BoxCollider2D>();
            if (box != null)
                DrawBoxGizmo(box);
        }

        private void DrawBoxGizmo(BoxCollider2D col)
        {
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

        private void DrawCircleGizmo(CircleCollider2D col)
        {
            Gizmos.color = _hitPoints > 0
                ? new Color(1f, 0.6f, 0.2f, 0.4f)
                : new Color(0.4f, 0.8f, 1f, 0.4f);

            float maxScale = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y));
            float worldRadius = col.radius * maxScale;
            Vector3 center = transform.position
                + (Vector3)(Vector2)(transform.localToWorldMatrix.MultiplyVector(col.offset));

            // 绘制实心圆盘 + 线框圆
            Gizmos.DrawSphere(center, worldRadius);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
            Gizmos.DrawWireSphere(center, worldRadius);
        }
#endif
    }
}
