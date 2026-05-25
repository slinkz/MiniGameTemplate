using UnityEngine;
using UnityEngine.Serialization;
using MiniGameTemplate.Data;

namespace Game.ShooterGame
{
    /// <summary>
    /// 编辑器键盘输入驱动——将 WASD/方向键映射到 SG_InputDirection。
    /// 输出模拟的像素 delta（与 JoystickController 1:1 跟手模式对齐）。
    /// 与 FairyGUI 摇杆并存，用于 Editor 快速测试（不依赖鼠标拖拽摇杆）。
    /// 微信小游戏真机发布时此组件不生效（无物理键盘），不需要移除。
    /// </summary>
    public class SG_DebugKeyboardInput : MonoBehaviour
    {
        [SerializeField] private Vector2Variable _inputDirection;

        /// <summary>键盘模拟的像素移动速度（像素/秒），帧率无关</summary>
        [FormerlySerializedAs("_pixelsPerFrame")]
        [SerializeField] private float _pixelsPerSecond = 480f;

        private bool _hadInputLastFrame;

        private void Update()
        {
            if (_inputDirection == null) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            bool hasInput = !Mathf.Approximately(h, 0f) || !Mathf.Approximately(v, 0f);

            if (!hasInput)
            {
                // 键盘松开那一帧清零，之后不再覆盖（让摇杆可接管）
                if (_hadInputLastFrame)
                {
                    _inputDirection.SetValue(Vector2.zero);
                    _hadInputLastFrame = false;
                }
                return;
            }

            _hadInputLastFrame = true;

            var dir = new Vector2(h, v);

            // 归一化（防止对角线速度超过 1）
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();

            // 输出模拟的像素 delta（Bridge 会翻转 Y 轴）
            // 键盘 Y 轴是世界方向（上=正），取反 Y 让 Bridge 翻转后恢复正确
            Vector2 pixelDelta = new Vector2(dir.x, -dir.y) * (_pixelsPerSecond * Time.deltaTime);
            _inputDirection.SetValue(pixelDelta);
        }

        private void OnDisable()
        {
            if (_inputDirection != null)
                _inputDirection.SetValue(Vector2.zero);
        }
    }
}
