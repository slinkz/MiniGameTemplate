using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 序列帧动画数据资产。
    /// 存储多个动画 Clip（按 AnimId 索引），每个 Clip 包含帧序列 + FPS + Loop。
    /// 
    /// 设计原则：
    /// - 纯数据 ScriptableObject，无运行时逻辑
    /// - AnimId 与 AnimationComponent.CurrentAnimId 对应
    /// - 设计师通过 Inspector 编辑帧数据
    /// 
    /// Phase 2 交付（P2.1）。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpriteAnimData", menuName = "Entity/SpriteAnimData")]
    public class SpriteAnimDataSO : ScriptableObject
    {
        [Tooltip("动画片段列表（按 AnimId 索引）")]
        public SpriteAnimClip[] Clips;

        /// <summary>
        /// 按 AnimId 获取动画片段。
        /// 超出范围返回 Clips[0]（Idle 兜底）；空数组返回 null。
        /// </summary>
        public SpriteAnimClip GetClip(int animId)
        {
            if (Clips == null || Clips.Length == 0) return null;
            if (animId < 0 || animId >= Clips.Length) return Clips[0]; // Idle 兜底
            return Clips[animId];
        }
    }

    /// <summary>
    /// 单个动画片段数据。
    /// </summary>
    [System.Serializable]
    public class SpriteAnimClip
    {
        [Tooltip("动画名称（调试用）")]
        public string Name;

        [Tooltip("帧序列（Sprite 数组）")]
        public Sprite[] Frames;

        [Tooltip("播放速度（帧/秒）")]
        [Min(1f)]
        public float FramesPerSecond = 10f;

        [Tooltip("是否循环")]
        public bool Loop = true;
    }
}
