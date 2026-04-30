using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 放置在场景中的刷怪点。策划通过 Inspector 配置波次 SO 和生成范围。
    /// Editor 模式下绘制 Gizmo 可视化生成区域。
    /// v2.5 变更（ET-009）：改为 Always 绘制 + Label，多刷怪点场景一目了然。
    /// </summary>
    public class EntitySpawnPoint : MonoBehaviour
    {
        [Header("波次配置")]
        [Tooltip("引用波次配置 SO")]
        public EntitySpawnWaveSO WaveConfig;

        [Tooltip("场景加载后自动开始刷怪")]
        public bool AutoStartOnEnable = true;

        [Header("生成区域")]
        [Tooltip("随机散布半径")]
        public float AreaRadius = 2f;

        // v2.5（ET-009）：始终绘制半透明圆圈 + 名称标签
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // 半透明黄色
            Gizmos.DrawWireSphere(transform.position, AreaRadius);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, gameObject.name);
#endif
        }

        // v2.5（ET-009）：选中时高亮显示 + 完整波次信息
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AreaRadius);
#if UNITY_EDITOR
            if (WaveConfig != null)
            {
                int totalWaves = WaveConfig.Waves?.Length ?? 0;
                int totalMonsters = 0;
                string firstEnemy = "N/A";
                if (totalWaves > 0 && WaveConfig.Waves[0].Groups?.Length > 0)
                {
                    firstEnemy = WaveConfig.Waves[0].Groups[0].EntityConfig?.DisplayName ?? "?";
                    foreach (var wave in WaveConfig.Waves)
                        if (wave.Groups != null)
                            foreach (var g in wave.Groups)
                                totalMonsters += g.Count;
                }
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (AreaRadius + 0.3f),
                    $"{gameObject.name}\n{totalWaves} 波 | {totalMonsters} 怪 | 首波: {firstEnemy}");
            }
#endif
        }
    }
}
