using MiniGameTemplate.Pool;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 障碍物类型配置——AABB 碰撞 + 阵营过滤 + 可摧毁。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Obstacle Type")]
    public class ObstacleTypeSO : ScriptableObject
    {
        [Header("碰撞")]
        [Tooltip("AABB 尺寸（世界单位）")]
        public Vector2 Size = new(1f, 1f);

        [Header("生命值")]
        [Tooltip("0 = 不可摧毁，>0 = 可被摧毁（受弹丸伤害扣减）")]
        public int HitPoints = 0;

        [Header("阵营")]
        [Tooltip("自己阵营的弹丸可穿透")]
        public BulletFaction Faction = BulletFaction.Enemy;

        [Header("视觉")]
        [Tooltip("被摧毁时播放的特效")]
        public PoolDefinition DestroyEffect;

        [Tooltip("Sprite/图片（如果用 SpriteRenderer 渲染）")]
        public Sprite Visual;
    }
}
