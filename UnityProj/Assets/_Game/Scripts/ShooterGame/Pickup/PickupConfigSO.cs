using UnityEngine;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Pool;

namespace Game.ShooterGame
{
    /// <summary>
    /// 道具配置 ScriptableObject（TDD_02 S2.4）。
    /// 策划在 Inspector 中创建，路径：Assets/_Game/Configs/Pickup/
    /// </summary>
    [CreateAssetMenu(fileName = "NewPickupConfig", menuName = "Configs/ShooterGame/PickupConfig")]
    public class PickupConfigSO : ScriptableObject
    {
        [Header("基础")]
        public string DisplayName;

        [Tooltip("道具类型")]
        public PickupType Type;

        [Header("Buff 类型（Type=Buff 时填写）")]
        [Tooltip("施加的 Buff 配置")]
        public BuffConfigSO BuffConfig;

        [Header("修复类型（Type=Repair 时填写）")]
        [Tooltip("基地 HP 修复量")]
        [Min(1)]
        public int RepairAmount = 10;

        [Header("弹药类型（Type=Ammo 时填写）")]
        [Tooltip("弹药强化 Buff（走 Buff 桥接，如攻速加倍 3s）")]
        public BuffConfigSO AmmoBuffConfig;

        [Header("金币类型（Type=Coin 时填写）")]
        [Tooltip("金币数量")]
        [Min(1)]
        public int CoinAmount = 10;

        [Header("表现")]
        [Tooltip("道具图标（用于批量 Mesh 渲染，优先于 ViewPrefab）")]
        public Sprite Icon;

        [Tooltip("图标显示尺寸（世界空间）")]
        public Vector2 IconSize = new Vector2(0.6f, 0.6f);

        [Tooltip("道具在场景中的 View Prefab（保留，Icon 优先）")]
        public GameObject ViewPrefab;

        [Tooltip("拾取特效")]
        public PoolDefinition PickupVfx;

        [Header("漂浮 & 生命周期")]
        [Tooltip("漂浮速度（单位/秒，向下）")]
        [Min(0.1f)]
        public float FloatSpeed = 0.8f;

        [Tooltip("存在时限（秒，最后 2s 闪烁提示）")]
        [Min(1f)]
        public float Lifetime = 8f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            switch (Type)
            {
                case PickupType.Buff:
                    if (BuffConfig == null)
                        Debug.LogError($"[PickupConfigSO] {name}: Buff 类型必须设置 BuffConfig", this);
                    break;
                case PickupType.Ammo:
                    if (AmmoBuffConfig == null)
                        Debug.LogError($"[PickupConfigSO] {name}: Ammo 类型必须设置 AmmoBuffConfig", this);
                    break;
            }
        }
#endif
    }
}
