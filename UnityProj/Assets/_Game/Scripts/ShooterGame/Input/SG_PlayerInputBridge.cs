using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;
using EntityClass = MiniGameTemplate.Entity.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 摇杆→Entity 移动桥接 + 全自动射击。
    /// 1:1 跟手模式：摇杆输出像素 delta → 此处换算为世界坐标偏移 → 直接设置 Entity 位置。
    /// 同时持续写入 ControlComponent.WantsAttack=true（GDD v1.6: 全自动战斗）。
    /// 挂载在 Battle 场景中。
    /// TDD_05 §4.2
    /// </summary>
    public class SG_PlayerInputBridge : MonoBehaviour
    {
        [SerializeField] private Vector2Variable _inputDirection;
        [SerializeField] private DanmakuWorldConfig _worldConfig;

        private EntityClass _playerEntity;
        private MovementComponent _movement;
        private ControlComponent _control;

        // 屏幕像素→世界坐标换算系数（运行时计算一次）
        private float _pixelToWorldX;
        private float _pixelToWorldY;

        /// <summary>由 BattleController 调用，传入玩家 Entity</summary>
        public void Init(EntityClass playerEntity)
        {
            _playerEntity = playerEntity;
            _movement = playerEntity.GetComponent(ComponentType.Movement) as MovementComponent;
            _control = playerEntity.GetComponent(ComponentType.Control) as ControlComponent;

            if (_movement == null && _control == null)
                Debug.LogError("[SG_PlayerInputBridge] Player Entity 缺少 MovementComponent 和 ControlComponent!");

            // GDD v1.6: 全自动射击——ControlComponent 存在时常驻开火 + 默认朝上瞄准
            if (_control != null)
            {
                _control.SetAttackInput(true);
                _control.SetAimInput(Vector2.up); // 纵版射击默认朝上（覆盖框架层 Vector2.right）
                _control.SuppressMovement = true; // 1:1 跟手模式：位置由 Bridge 直接设置，禁止速度系统干扰
            }

            // 计算像素→世界换算系数
            // worldWidth / screenWidth = 每像素对应多少世界单位
            float screenW = GRoot.inst.width;
            float screenH = GRoot.inst.height;
            float worldW = _worldConfig != null ? _worldConfig.WorldBounds.width : 12f;
            float worldH = _worldConfig != null ? _worldConfig.WorldBounds.height : 20f;
            _pixelToWorldX = worldW / screenW;
            _pixelToWorldY = worldH / screenH;
        }

        private void Update()
        {
            Vector2 input = _inputDirection.Value;
            if (input.sqrMagnitude < 0.01f) return;

            // 消费后立即清零
            _inputDirection.SetValue(Vector2.zero);

            if (_movement == null) return;

            // 像素 delta → 世界坐标偏移
            // FairyGUI y 轴向下，世界 y 轴向上 → 翻转 y
            float worldDx = input.x * _pixelToWorldX;
            float worldDy = -input.y * _pixelToWorldY;

            // 直接偏移位置（绕过速度系统，1:1 跟手）
            Vector2 newPos = _playerEntity.Position + new Vector2(worldDx, worldDy);

            // 边界钳制（不让飞机飞出世界）
            if (_worldConfig != null)
            {
                Rect bounds = _worldConfig.WorldBounds;
                newPos.x = Mathf.Clamp(newPos.x, bounds.xMin, bounds.xMax);
                newPos.y = Mathf.Clamp(newPos.y, bounds.yMin, bounds.yMax);
            }

            _movement.SetPosition(newPos);

            // 保持全自动射击瞄准方向
            if (_control != null)
                _control.SetAimInput(Vector2.up);
        }

        /// <summary>禁用输入（Intro/结算状态调用）</summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
    }
}
