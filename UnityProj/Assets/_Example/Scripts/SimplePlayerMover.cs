using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// 极简玩家移动——键盘 WASD / 方向键 + 触屏拖拽。
    /// 挂到带 SpriteRenderer 的 GameObject 上就能用。
    /// </summary>
    public class SimplePlayerMover : MonoBehaviour
    {
        [Header("移动")]
        [Tooltip("移动速度（世界单位/秒）")]
        [SerializeField] private float _speed = 6f;

        [Header("世界边界")]
        [Tooltip("限制移动范围（与 DanmakuWorldConfig.WorldBounds 对齐）")]
        [SerializeField] private Rect _bounds = new(-5.5f, -9.5f, 11f, 19f);

        // 触屏拖拽状态
        private bool _dragging;
        private Vector2 _dragOffset;
        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            Vector2 pos = transform.position;

            // ── 键盘输入 ──
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (h != 0 || v != 0)
            {
                Vector2 dir = new Vector2(h, v).normalized;
                pos += dir * _speed * Time.deltaTime;
            }

            // ── 触屏拖拽（移动端） ──
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
            {
                Vector2 screenPos = Input.touchCount > 0
                    ? (Vector2)Input.GetTouch(0).position
                    : (Vector2)Input.mousePosition;

                Vector2 worldPos = _cam != null
                    ? (Vector2)_cam.ScreenToWorldPoint(screenPos)
                    : screenPos;

                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    _dragging = true;
                    _dragOffset = pos - worldPos;
                }

                if (_dragging)
                {
                    pos = worldPos + _dragOffset;
                }
            }
            else
            {
                _dragging = false;
            }

            // ── 边界约束 ──
            pos.x = Mathf.Clamp(pos.x, _bounds.xMin, _bounds.xMax);
            pos.y = Mathf.Clamp(pos.y, _bounds.yMin, _bounds.yMax);

            transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            var center = _bounds.center;
            var size = _bounds.size;
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0), new Vector3(size.x, size.y, 0));
        }
    }
}
