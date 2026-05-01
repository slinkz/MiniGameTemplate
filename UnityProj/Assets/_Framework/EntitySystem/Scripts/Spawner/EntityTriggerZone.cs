using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 触发区域组件（P2.5 新增）——放置在场景中的区域检测器。
    /// 
    /// 核心逻辑：
    /// - 每帧检查是否有指定阵营（默认 Player）的 Entity 进入区域
    /// - 进入后标记 IsTriggered = true（一次性，进入后不可逆）
    /// - EntitySpawner 在 WaveTriggerMode.OnEnterArea 模式下读取此标记
    /// 
    /// 设计决策：
    /// - 不使用 Unity Collider/Trigger（Entity 是纯逻辑对象无 GO Collider）
    /// - 直接遍历 EntityManager.ActiveEntities 做圆-圆/圆-矩形检测
    /// - 零 GC：无事件、无回调、纯状态查询
    /// - 策划在 Inspector 中配置区域形状和大小
    /// 
    /// Editor：绘制 Gizmo 可视化触发区域（绿色=未触发，红色=已触发）。
    /// </summary>
    public class EntityTriggerZone : MonoBehaviour
    {
        [Header("触发条件")]
        [Tooltip("检测的目标阵营（默认 Player）")]
        public Danmaku.EnumCamp TargetCamp = Danmaku.EnumCamp.Player;

        [Tooltip("触发区域半径")]
        public float TriggerRadius = 2f;

        [Tooltip("是否只触发一次（true=进入后永久激活；false=离开后重置）")]
        public bool OneShot = true;

        /// <summary>是否已被触发</summary>
        public bool IsTriggered { get; private set; }

        /// <summary>手动重置触发状态（Loop 模式下 Spawner 重启时调用）</summary>
        public void ResetTrigger() => IsTriggered = false;

        /// <summary>
        /// 每帧由 EntitySpawner 调用检测（而非 Update 自驱动，保持框架时序控制）。
        /// 返回 true 表示本帧检测到有目标在区域内。
        /// </summary>
        public bool CheckTrigger(EntityManager entityManager)
        {
            if (IsTriggered && OneShot) return true; // 已触发且一次性，直接返回

            var entities = entityManager.ActiveEntities;
            Vector2 center = (Vector2)transform.position;
            float radiusSq = TriggerRadius * TriggerRadius;

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.IsPendingDespawn) continue;
                if (entity.Camp != TargetCamp) continue;

                float distSq = (entity.Position - center).sqrMagnitude;
                if (distSq <= radiusSq)
                {
                    IsTriggered = true;
                    return true;
                }
            }

            // 非 OneShot 模式下，没有目标在区域内时重置
            if (!OneShot) IsTriggered = false;
            return false;
        }

        // ──────────── Gizmo 可视化 ────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = IsTriggered
                ? new Color(1f, 0.3f, 0.3f, 0.3f)  // 红色=已触发
                : new Color(0.3f, 1f, 0.3f, 0.3f);  // 绿色=未触发
            Gizmos.DrawWireSphere(transform.position, TriggerRadius);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.down * (TriggerRadius + 0.2f),
                IsTriggered ? $"[TRIGGERED] {gameObject.name}" : gameObject.name);
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsTriggered ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, TriggerRadius);
        }
    }
}
