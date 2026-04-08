using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕时间缩放——独立于 Time.timeScale 的时间源（子弹时间效果）。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Time Scale")]
    public class DanmakuTimeScaleSO : ScriptableObject
    {
        [Range(0f, 2f)]
        public float TimeScale = 1f;

        /// <summary>弹幕系统的 deltaTime，所有时间计算用这个。</summary>
        public float DeltaTime => Time.deltaTime * TimeScale;

        /// <summary>设置慢动作</summary>
        public void SetSlowMotion(float scale) => TimeScale = scale;

        /// <summary>恢复正常速度</summary>
        public void ResetSpeed() => TimeScale = 1f;
    }
}
