using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 虚拟摇杆控制器——基于 FairyGUI 触摸事件。
    /// 每帧将方向向量写入 SG_InputDirection Vector2Variable。
    /// TDD_05 §3.2
    /// </summary>
    public class JoystickController : MonoBehaviour, IJoystickController
    {
        private const string FGUI_PKG = "SG_Battle";
        private const string FGUI_JOYSTICK = "Joystick";

        [SerializeField] private JoystickConfigSO _config;
        [SerializeField] private Vector2Variable _inputDirection;

        // FairyGUI 元素
        private GGraph _touchArea;
        private SG_Battle.Joystick _joystickBase;
        private GObject _joystickStick;

        // 状态
        private bool _isActive;
        private Vector2 _touchOrigin;   // 按下瞬间的初始触摸位置（用于摇杆视觉定位）
        private Vector2 _lastTouchPos;  // 上一帧触摸位置（用于计算 delta）
        private bool _inputEnabled = true;

        public void Init(GComponent battleHUD)
        {
            if (battleHUD == null) return;

            // 创建全屏 GGraph 作为触摸区
            // 必须 DrawRect 才有命中区域——Shape.HitTest 在 meshFactory==null 时返回 null
            _touchArea = new GGraph();
            _touchArea.SetSize(GRoot.inst.width, GRoot.inst.height);
            _touchArea.DrawRect(GRoot.inst.width, GRoot.inst.height, 0, Color.clear, Color.clear);
            _touchArea.touchable = true;
            battleHUD.AddChildAt(_touchArea, 1); // 位于背景之上、HUD 按钮之下

            // 创建底座和摇杆头
            _joystickBase = SG_Battle.Joystick.CreateInstance();
            _joystickBase.visible = false;
            battleHUD.AddChildAt(_joystickBase, 2); // 位于触摸层之上、HUD 按钮之下

            _joystickStick = _joystickBase.stick;

            // 绑定事件
            _touchArea.onTouchBegin.Add(OnTouchBegin);
            _touchArea.onTouchMove.Add(OnTouchMove);
            _touchArea.onTouchEnd.Add(OnTouchEnd);
        }

        public void SetEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled && _isActive)
            {
                Deactivate();
            }
        }

        // ── 触摸处理 ──

        private void OnTouchBegin(EventContext context)
        {
            if (!_inputEnabled) return;

            // 关键：捕获触摸，后续 Move/End 才会路由到此对象
            context.CaptureTouch();

            var touch = context.inputEvent;
            _touchOrigin = new Vector2(touch.x, touch.y);
            _lastTouchPos = _touchOrigin;
            _isActive = true;

            // 按下瞬间不产生移动输入，等待手指开始滑动
            _inputDirection.SetValue(Vector2.zero);
            ShowJoystick(_touchOrigin);
        }

        private void OnTouchMove(EventContext context)
        {
            if (!_isActive) return;

            var touch = context.inputEvent;
            Vector2 currentPos = new Vector2(touch.x, touch.y);

            // ── 1:1 跟手模式：直接输出原始像素 delta ──
            Vector2 frameDelta = currentPos - _lastTouchPos;
            _lastTouchPos = currentPos;

            // 极小抖动过滤（1px 以内视为静止）
            if (frameDelta.sqrMagnitude < 1f)
            {
                _inputDirection.SetValue(Vector2.zero);
            }
            else
            {
                // 直接输出像素级 delta，由 InputBridge 层做坐标换算
                _inputDirection.SetValue(frameDelta);
            }

            // ── 摇杆视觉：仍然按相对于触摸原点的偏移来显示 ──
            Vector2 visualDelta = currentPos - _touchOrigin;
            float clampedDistance = Mathf.Min(visualDelta.magnitude, _config.MaxRadius);
            Vector2 stickOffset = visualDelta.magnitude > 0.001f
                ? visualDelta.normalized * clampedDistance
                : Vector2.zero;
            UpdateStickVisual(stickOffset);
        }

        private void OnTouchEnd(EventContext context)
        {
            if (!_isActive) return;
            Deactivate();
        }

        private void Deactivate()
        {
            _isActive = false;
            _inputDirection.SetValue(Vector2.zero);
            HideJoystick();
        }

        // ── 视觉更新 ──

        private void ShowJoystick(Vector2 center)
        {
            if (_joystickBase == null) return;
            _joystickBase.SetPosition(
                center.x - _config.BaseDiameter * 0.5f,
                center.y - _config.BaseDiameter * 0.5f, 0);
            _joystickBase.alpha = _config.Alpha_Base;
            _joystickBase.visible = true;
        }

        private void HideJoystick()
        {
            if (_joystickBase == null) return;
            _joystickBase.TweenFade(0f, _config.DisappearDuration)
                .OnComplete(() => { _joystickBase.visible = false; });
        }

        private void UpdateStickVisual(Vector2 offset)
        {
            if (_joystickStick == null) return;
            float centerX = _config.BaseDiameter * 0.5f;
            float centerY = _config.BaseDiameter * 0.5f;
            _joystickStick.SetPosition(
                centerX + offset.x - _config.StickDiameter * 0.5f,
                centerY + offset.y - _config.StickDiameter * 0.5f, 0);
        }

        private void OnDestroy()
        {
            // 防御性清零——实际由 HUD 的 Dispose 递归清理
            if (_inputDirection != null)
                _inputDirection.SetValue(Vector2.zero);
            _touchArea = null;
            _joystickBase = null;
            _joystickStick = null;
        }
    }
}
