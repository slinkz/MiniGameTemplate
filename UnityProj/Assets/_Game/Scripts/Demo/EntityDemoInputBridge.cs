using UnityEngine;
using MiniGameTemplate.Entity;
using EntityClass = MiniGameTemplate.Entity.Entity;

namespace MiniGameTemplate.Game.Demo
{
    /// <summary>
    /// P1.11 Demo 场景：将键盘输入桥接到玩家 Entity 的 ControlComponent。
    /// 放在场景根 GO 上，由 Bootstrap 生成玩家 Entity 后通过 PlayerEntityId 查找。
    /// 
    /// 操作：
    /// - WASD / 方向键：移动
    /// - Space / J：射击
    /// - 鼠标方向 / 右摇杆（Phase 2）：瞄准
    /// 
    /// Phase 1 简化：瞄准方向 = 移动方向（无独立瞄准）。
    /// </summary>
    public class EntityDemoInputBridge : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("玩家 Entity 配置（用于在 EntityManager 中查找玩家 Entity）")]
        public EntityConfigSO PlayerConfig;

        [Tooltip("是否自动生成玩家（Demo 用，正式项目由 Spawner 管理）")]
        public bool AutoSpawnPlayer = true;

        [Tooltip("玩家初始位置")]
        public Vector2 PlayerSpawnPosition = new Vector2(0f, -3f);

        private EntityClass _playerEntity;
        private ControlComponent _controlComponent;

        private void Start()
        {
            if (AutoSpawnPlayer && PlayerConfig != null)
            {
                var mgr = EntityManagerAccessor.Instance;
                if (mgr != null)
                {
                    _playerEntity = mgr.Spawn(PlayerConfig, PlayerSpawnPosition, 90f); // 朝上
                    if (_playerEntity != null)
                    {
                        _controlComponent = _playerEntity.GetComponent(ComponentType.Control) as ControlComponent;
                        if (_controlComponent == null)
                        {
                            Debug.LogWarning("[DemoInputBridge] 玩家 EntityConfig 缺少 ControlComponent！");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[DemoInputBridge] EntityManagerAccessor.Instance 为空！请确保场景中有 EntitySystemBootstrap。");
                }
            }
        }

        private void Update()
        {
            if (_controlComponent == null) return;

            // 移动输入
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 moveDir = new Vector2(h, v);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
            _controlComponent.SetMoveInput(moveDir);

            // 攻击输入
            bool attack = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J);
            _controlComponent.SetAttackInput(attack);

            // 瞄准方向：Phase 1 简化——移动方向 = 瞄准方向（无独立瞄准时朝上）
            Vector2 aimDir = moveDir.sqrMagnitude > 0.01f ? moveDir : Vector2.up;
            _controlComponent.SetAimInput(aimDir);
        }

        /// <summary>获取当前玩家 Entity（外部查询用）</summary>
        public EntityClass PlayerEntity => _playerEntity;
    }
}
