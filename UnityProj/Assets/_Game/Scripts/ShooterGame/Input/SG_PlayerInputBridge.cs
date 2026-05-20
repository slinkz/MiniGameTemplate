using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Entity;
using EntityClass = MiniGameTemplate.Entity.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 摇杆→Entity 移动桥接 + 全自动射击。
    /// 每帧读取 SG_InputDirection，写入玩家 Entity 的 MovementComponent。
    /// 同时持续写入 ControlComponent.WantsAttack=true（GDD v1.6: 全自动战斗）。
    /// 挂载在 Battle 场景中。
    /// TDD_05 §4.2
    /// </summary>
    public class SG_PlayerInputBridge : MonoBehaviour
    {
        [SerializeField] private Vector2Variable _inputDirection;

        private EntityClass _playerEntity;
        private MovementComponent _movement;
        private ControlComponent _control;

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
            }
        }

        private void Update()
        {
            // FairyGUI 触摸坐标 y 轴向下，需翻转为世界坐标（TDD_05 §4.3）
            Vector2 input = _inputDirection.Value;
            Vector2 worldDir = new Vector2(input.x, -input.y);

            // 有 ControlComponent 时走统一决策通道（避免 Tick 覆盖）
            if (_control != null)
            {
                _control.SetMoveInput(worldDir);
                _control.SetAimInput(Vector2.up);
            }
            else if (_movement != null)
            {
                // 无 ControlComponent 的降级路径（兼容旧配置）
                _movement.SetMoveDirection(worldDir);
            }
        }

        /// <summary>禁用输入（Intro/结算状态调用）</summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
                _movement?.SetMoveDirection(Vector2.zero);
        }
    }
}
