using UnityEngine;
using MiniGameTemplate.Data;

namespace Game.ShooterGame
{
    /// <summary>
    /// 编辑器键盘输入驱动——将 WASD/方向键映射到 SG_InputDirection。
    /// 与 FairyGUI 摇杆并存，用于 Editor 快速测试（不依赖鼠标拖拽摇杆）。
    /// 微信小游戏真机发布时此组件不生效（无物理键盘），不需要移除。
    /// </summary>
    public class SG_DebugKeyboardInput : MonoBehaviour
    {
        [SerializeField] private Vector2Variable _inputDirection;

        private void Update()
        {
            if (_inputDirection == null) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // 没有键盘输入时不覆盖摇杆值——两者可共存
            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
                return;

            var dir = new Vector2(h, v);

            // 归一化（防止对角线速度超过 1）
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();

            // SG_PlayerInputBridge 会翻转 Y 轴（FairyGUI 坐标系），
            // 键盘 Y 轴已经是世界方向（上=正），
            // 写入时取反 Y，让 Bridge 翻转后恢复正确方向。
            _inputDirection.SetValue(new Vector2(dir.x, -dir.y));
        }

        private void OnDisable()
        {
            if (_inputDirection != null)
                _inputDirection.SetValue(Vector2.zero);
        }
    }
}
