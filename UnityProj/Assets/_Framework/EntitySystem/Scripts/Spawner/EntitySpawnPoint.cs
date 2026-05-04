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

        [Tooltip("场景加载后自动开始刷怪（无 TriggerZone 时）")]
        public bool AutoStartOnEnable = true;

        [Header("触发区域（P2.5 新增）")]
        [Tooltip("关联触发区域。为空=按 AutoStartOnEnable 自动开始；不为空=等玩家进入区域后才开始刷怪")]
        public EntityTriggerZone TriggerZone;

        [Header("生成区域")]
        [Tooltip("阵型/随机散布半径（控制 Circle/Grid/Random 的展开范围）")]
        [Min(0.1f)]
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

        // v2.5（ET-009）：选中时高亮显示 + 完整波次信息 + 阵型预览
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

                // ── 阵型预览 Gizmo ──
                DrawFormationPreview();
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在 Scene View 中绘制各波次各组的阵型预览点位。
        /// 不同波次用不同颜色区分。
        /// </summary>
        private void DrawFormationPreview()
        {
            if (WaveConfig.Waves == null) return;

            Color[] waveColors = { Color.cyan, Color.green, Color.magenta, Color.red, new Color(1f, 0.5f, 0f) };
            Vector2 center = (Vector2)transform.position;

            for (int w = 0; w < WaveConfig.Waves.Length; w++)
            {
                var wave = WaveConfig.Waves[w];
                if (wave.Groups == null) continue;

                Color baseColor = waveColors[w % waveColors.Length];

                for (int g = 0; g < wave.Groups.Length; g++)
                {
                    var group = wave.Groups[g];
                    if (group.Count <= 0) continue;

                    // 组内用稍偏的色调区分
                    Gizmos.color = Color.Lerp(baseColor, Color.white, g * 0.15f);

                    for (int i = 0; i < group.Count; i++)
                    {
                        Vector2 pos = CalcFormationPosition(center, AreaRadius,
                            group.Formation, ref group.FormationParams, i, group.Count);

                        // 绘制实心小球表示生成位置
                        Gizmos.DrawSphere((Vector3)pos, 0.15f);
                    }

                    // 绘制阵型轮廓辅助线
                    DrawFormationOutline(center, AreaRadius, group.Formation, ref group.FormationParams, group.Count, Gizmos.color);
                }
            }

            // 波次颜色图例
            float labelY = AreaRadius + 1.2f;
            for (int w = 0; w < Mathf.Min(WaveConfig.Waves.Length, waveColors.Length); w++)
            {
                UnityEditor.Handles.color = waveColors[w % waveColors.Length];
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (labelY + w * 0.4f),
                    $"● 波次 {w + 1}");
            }
        }

        /// <summary>计算阵型中第 index 个单位的位置（纯数学，与 EntitySpawner 逻辑一致）</summary>
        private static Vector2 CalcFormationPosition(Vector2 center, float areaRadius,
            SpawnFormation formation, ref FormationConfig cfg, int index, int total)
        {
            switch (formation)
            {
                case SpawnFormation.Line:
                {
                    float spacing = cfg.Spacing > 0f ? cfg.Spacing : (total > 1 ? areaRadius * 2f / (total - 1) : 0f);
                    float totalSpan = spacing * (total - 1);
                    float t = total > 1 ? (float)index / (total - 1) : 0.5f;
                    float offset = -totalSpan * 0.5f + t * totalSpan;
                    float rad = cfg.Angle * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    return center + dir * offset;
                }

                case SpawnFormation.Circle:
                {
                    float radius = cfg.Radius > 0f ? cfg.Radius : areaRadius;
                    float angleStep = total > 0 ? 360f / total : 0f;
                    float angle = cfg.Angle + angleStep * index;
                    float rad = angle * Mathf.Deg2Rad;
                    // 底边对齐：整体上移 radius，使阵型最低点 = center.y
                    Vector2 offset = new Vector2(0f, radius);
                    return center + offset + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                }

                case SpawnFormation.Grid:
                {
                    int cols = cfg.Columns > 0 ? cfg.Columns : Mathf.CeilToInt(Mathf.Sqrt(total));
                    if (cols < 1) cols = 1;
                    int rows = Mathf.CeilToInt((float)total / cols);
                    int col = index % cols;
                    int row = index / cols;
                    float spacing = cfg.Spacing > 0f ? cfg.Spacing : (cols > 1 ? areaRadius * 2f / (cols - 1) : 0f);
                    float gridW = spacing * (cols - 1);
                    float x = cols > 1 ? -gridW * 0.5f + col * spacing : 0f;
                    // 底边对齐：row=0 在 center.y，向上递增
                    float y = row * spacing;
                    return center + new Vector2(x, y);
                }

                case SpawnFormation.Random:
                default:
                {
                    // Random 无法确定性预览，用均匀圆周代替示意（底边对齐）
                    float rActualRadius = cfg.Radius > 0f ? cfg.Radius : areaRadius;
                    float rAngle = 360f / Mathf.Max(total, 1) * index;
                    float rRad = rAngle * Mathf.Deg2Rad;
                    float rRadius = rActualRadius * 0.7f; // 稍小于边界，示意散布范围
                    Vector2 rOffset = new Vector2(0f, rActualRadius);
                    return center + rOffset + new Vector2(Mathf.Cos(rRad), Mathf.Sin(rRad)) * rRadius;
                }
            }
        }

        /// <summary>绘制阵型轮廓辅助线</summary>
        private static void DrawFormationOutline(Vector2 center, float areaRadius,
            SpawnFormation formation, ref FormationConfig cfg, int total, Color color)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);

            switch (formation)
            {
                case SpawnFormation.Line:
                {
                    // 画一条线段连接首尾
                    if (total < 2) break;
                    Vector2 first = CalcFormationPosition(center, areaRadius, formation, ref cfg, 0, total);
                    Vector2 last = CalcFormationPosition(center, areaRadius, formation, ref cfg, total - 1, total);
                    Gizmos.DrawLine((Vector3)first, (Vector3)last);
                    break;
                }

                case SpawnFormation.Circle:
                {
                    // 画圆（底边对齐：圆心上移 radius）
                    float radius = cfg.Radius > 0f ? cfg.Radius : areaRadius;
                    Vector2 circleCenter = center + new Vector2(0f, radius);
                    int segments = 32;
                    Vector3 prev = (Vector3)(circleCenter + new Vector2(radius, 0f));
                    for (int i = 1; i <= segments; i++)
                    {
                        float a = (float)i / segments * Mathf.PI * 2f;
                        Vector3 next = (Vector3)(circleCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                        Gizmos.DrawLine(prev, next);
                        prev = next;
                    }
                    break;
                }

                case SpawnFormation.Grid:
                {
                    // 画网格外框（底边对齐：底边在 center.y，顶边在 center.y + gridH）
                    if (total < 2) break;
                    int cols = cfg.Columns > 0 ? cfg.Columns : Mathf.CeilToInt(Mathf.Sqrt(total));
                    if (cols < 1) cols = 1;
                    int rows = Mathf.CeilToInt((float)total / cols);
                    float spacing = cfg.Spacing > 0f ? cfg.Spacing : (cols > 1 ? areaRadius * 2f / (cols - 1) : 0f);
                    float gridW = spacing * (cols - 1);
                    float gridH = spacing * (rows - 1);
                    Vector3 bl = (Vector3)(center + new Vector2(-gridW * 0.5f, 0f));
                    Vector3 br = (Vector3)(center + new Vector2(gridW * 0.5f, 0f));
                    Vector3 tl = (Vector3)(center + new Vector2(-gridW * 0.5f, gridH));
                    Vector3 tr = (Vector3)(center + new Vector2(gridW * 0.5f, gridH));
                    Gizmos.DrawLine(tl, tr);
                    Gizmos.DrawLine(tr, br);
                    Gizmos.DrawLine(br, bl);
                    Gizmos.DrawLine(bl, tl);
                    break;
                }

                case SpawnFormation.Random:
                default:
                {
                    // Random 画虚线圆示意散布范围（底边对齐）
                    float rActualRadius = cfg.Radius > 0f ? cfg.Radius : areaRadius;
                    float rRadius = rActualRadius * 0.7f;
                    Vector2 rCenter = center + new Vector2(0f, rActualRadius);
                    int rSeg = 16;
                    for (int i = 0; i < rSeg; i += 2) // 间隔画 = 虚线效果
                    {
                        float a1 = (float)i / rSeg * Mathf.PI * 2f;
                        float a2 = (float)(i + 1) / rSeg * Mathf.PI * 2f;
                        Vector3 p1 = (Vector3)(rCenter + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * rRadius);
                        Vector3 p2 = (Vector3)(rCenter + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * rRadius);
                        Gizmos.DrawLine(p1, p2);
                    }
                    break;
                }
            }
        }
#endif
    }
}
