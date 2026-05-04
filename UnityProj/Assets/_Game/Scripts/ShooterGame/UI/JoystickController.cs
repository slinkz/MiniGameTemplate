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
        private const string FGUI_PKG = "Battle";
        private const string FGUI_JOYSTICK = "Joystick";

        [SerializeField] private JoystickConfigSO _config;
        [SerializeField] private Vector2Variable _inputDirection;

        // FairyGUI 元素
        private GGraph _touchArea;
        private GComponent _joystickBase;
        private GObject _joystickStick;

        // 状态
        private bool _isActive;
        private Vector2 _touchOrigin;
        private bool _inputEnabled = true;

        public void Init(GComponent battleHUD)
        {
            if (battleHUD == null) return;

            // 创建全屏 GGraph 作为触摸区
            _touchArea = new GGraph();
            _touchArea.SetSize(GRoot.inst.width, GRoot.inst.height);
            _touchArea.touchable = true;
            _touchArea.sortingOrder = 50; // 低于 HUD 元素
            battleHUD.AddChild(_touchArea);

            // 创建底座和摇杆头
            _joystickBase = UIPackage.CreateObject(FGUI_PKG, FGUI_JOYSTICK).asCom;
            _joystickBase.visible = false;
            battleHUD.AddChild(_joystickBase);

            _joystickStick = _joystickBase.GetChild("stick");

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

            var touch = context.inputEvent;
            _touchOrigin = new Vector2(touch.x, touch.y);
            _isActive = true;

            ShowJoystick(_touchOrigin);
        }

        private void OnTouchMove(EventContext context)
        {
            if (!_isActive) return;

            var touch = context.inputEvent;
            Vector2 currentPos = new Vector2(touch.x, touch.y);
            Vector2 delta = currentPos - _touchOrigin;
            float distance = delta.magnitude;

            // 死区检测
            if (distance < _config.DeadZone)
            {
                _inputDirection.SetValue(Vector2.zero);
                UpdateStickVisual(Vector2.zero);
                return;
            }

            // 方向归一化
            Vector2 direction = delta.normalized;

            // 钳制摇杆头在最大半径内
            float clampedDistance = Mathf.Min(distance, _config.MaxRadius);
            Vector2 stickOffset = direction * clampedDistance;

            // 写入 SO
            _inputDirection.SetValue(direction);

            // 更新视觉
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
    }
}
