using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕类型注册表——集中管理所有弹丸/激光/喷雾类型 SO，Awake 时分配运行时索引。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/Type Registry")]
    public class DanmakuTypeRegistry : ScriptableObject
    {
        [Header("弹丸类型")]
        public BulletTypeSO[] BulletTypes;

        [Header("激光类型")]
        public LaserTypeSO[] LaserTypes;

        [Header("喷雾类型")]
        public SprayTypeSO[] SprayTypes;

        /// <summary>
        /// 给每个 TypeSO 分配运行时索引。必须在 DanmakuSystem.Awake 中调用。
        /// </summary>
        public void AssignRuntimeIndices()
        {
            if (BulletTypes != null)
            {
                for (ushort i = 0; i < BulletTypes.Length; i++)
                {
                    if (BulletTypes[i] != null)
                        BulletTypes[i].RuntimeIndex = i;
                }
            }

            if (LaserTypes != null)
            {
                for (byte i = 0; i < LaserTypes.Length; i++)
                {
                    if (LaserTypes[i] != null)
                        LaserTypes[i].RuntimeIndex = i;
                }
            }

            if (SprayTypes != null)
            {
                for (byte i = 0; i < SprayTypes.Length; i++)
                {
                    if (SprayTypes[i] != null)
                        SprayTypes[i].RuntimeIndex = i;
                }
            }
        }
    }
}
