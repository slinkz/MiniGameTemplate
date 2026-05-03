using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// 虚拟摇杆全参数 SO 化——Play Mode 中 Inspector 修改即时生效。
    /// TDD_05 §2.1
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/JoystickConfig")]
    public class JoystickConfigSO : ScriptableObject
    {
        [Header("操控参数")]
        [Tooltip("死区半径（pt）——偏移 < 此值时不响应移动")]
        public float DeadZone = 8f;

        [Tooltip("最大偏移半径（pt）——超过时摇杆头钳制在边缘")]
        public float MaxRadius = 60f;

        [Header("视觉参数")]
        [Tooltip("底座透明度")]
        [Range(0f, 1f)]
        public float Alpha_Base = 0.3f;

        [Tooltip("摇杆头透明度")]
        [Range(0f, 1f)]
        public float Alpha_Stick = 0.6f;

        [Tooltip("底座直径（pt）")]
        public float BaseDiameter = 120f;

        [Tooltip("摇杆头直径（pt）")]
        public float StickDiameter = 50f;

        [Header("动效")]
        [Tooltip("出现动效时长（秒）")]
        public float AppearDuration = 0.1f;

        [Tooltip("消失动效时长（秒）")]
        public float DisappearDuration = 0.15f;
    }
}
