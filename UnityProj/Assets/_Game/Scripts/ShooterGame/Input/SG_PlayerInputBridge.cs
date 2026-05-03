using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Entity;
using EntityClass = MiniGameTemplate.Entity.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 摇杆→Entity 移动桥接。
    /// 每帧读取 SG_InputDirection，写入玩家 Entity 的 MovementComponent。
    /// 挂载在 Battle 场景中。
    /// TDD_05 §4.2
    /// </summary>
    public class SG_PlayerInputBridge : MonoBehaviour
    {
        [SerializeField] private Vector2Variable _inputDirection;

        private EntityClass _playerEntity;
        private MovementComponent _movement;

        /// <summary>由 BattleController 调用，传入玩家 Entity</summary>
        public void Init(EntityClass playerEntity)
        {
            _playerEntity = playerEntity;
            _movement = playerEntity.GetComponent(ComponentType.Movement) as MovementComponent;

            if (_movement == null)
                Debug.LogError("[SG_PlayerInputBridge] Player Entity 缺少 MovementComponent!");
        }

        private void Update()
        {
            if (_movement == null) return;

            // FairyGUI 触摸坐标 y 轴向下，需翻转为世界坐标（TDD_05 §4.3）
            Vector2 input = _inputDirection.Value;
            Vector2 worldDir = new Vector2(input.x, -input.y);

            _movement.SetMoveDirection(worldDir);
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
