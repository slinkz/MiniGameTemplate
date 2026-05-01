using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 触发区域组件（P2.5）——放置在场景中的区域检测器。
    /// 
    /// 使用方式：
    /// 1. 在 GameObject 上挂 BoxCollider2D 或 CircleCollider2D（IsTrigger=true）
    /// 2. 挂本脚本，配置 TargetCamp / OneShot
    /// 3. 在 EntitySpawnPoint 的 TriggerZone 字段中引用此 GO
    ///    → SpawnPoint 关联了 TriggerZone = 等玩家进入区域后才开始刷怪
    ///    → SpawnPoint 未关联 TriggerZone = 按 AutoStartOnEnable 自动开始
    /// 
    /// 设计决策：
    /// - TriggerZone 是 SpawnPoint 级开关，不是波次级——简洁、直觉
    /// - 区域形状由 Collider2D 定义（策划在 Inspector 中拖拽编辑大小）
    /// - 检测逻辑仍为主动轮询 EntityManager（Entity 纯逻辑对象无 GO Collider，不走 Physics2D）
    /// - 通过 Collider2D.OverlapPoint 判断 Entity.Position 是否在区域内——支持任意 Collider2D 形状
    /// - 零 GC：无事件、无回调、纯状态查询
    /// - Collider2D 自带 Gizmo 可视化，选中时额外显示触发状态标签
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EntityTriggerZone : MonoBehaviour
    {
        [Header("触发条件")]
        [Tooltip("检测的目标阵营（默认 Player）")]
        public Danmaku.EnumCamp TargetCamp = Danmaku.EnumCamp.Player;

        [Tooltip("是否只触发一次（true=进入后永久激活；false=离开后重置）")]
        public bool OneShot = true;

        /// <summary>是否已被触发</summary>
        public bool IsTriggered { get; private set; }

        /// <summary>手动重置触发状态（Loop 模式下 Spawner 重启时调用）</summary>
        public void ResetTrigger() => IsTriggered = false;

        private Collider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true; // 确保不参与物理碰撞
        }

        /// <summary>
        /// 每帧由 EntitySpawner 调用检测。
        /// 利用 Collider2D.OverlapPoint 判断 Entity.Position 是否在触发区域内。
        /// 返回 true 表示本帧检测到有目标在区域内。
        /// </summary>
        public bool CheckTrigger(EntityManager entityManager)
        {
            if (IsTriggered && OneShot) return true; // 已触发且一次性，直接返回

            var entities = entityManager.ActiveEntities;
            bool found = false;

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.IsPendingDespawn) continue;
                if (entity.Camp != TargetCamp) continue;

                // Collider2D.OverlapPoint：支持 Box/Circle/Polygon 任意形状
                if (_collider.OverlapPoint(entity.Position))
                {
                    IsTriggered = true;
                    return true;
                }
            }

            // 非 OneShot 模式下，没有目标在区域内时重置
            if (!OneShot && !found) IsTriggered = false;
            return IsTriggered;
        }

        // ──────────── 自动配置 ────────────

        private void Reset()
        {
            // 新挂脚本时：如果没有 Collider2D，自动添加 BoxCollider2D
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
            }
            col.isTrigger = true;
        }

        private void OnValidate()
        {
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[EntityTriggerZone] {gameObject.name}: Collider2D.isTrigger 应为 true，已自动修正。", this);
                col.isTrigger = true;
            }
        }

        // ──────────── Gizmo 可视化 ────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Collider2D 自带绿色线框 Gizmo，这里只画状态标签
            UnityEditor.Handles.color = IsTriggered ? Color.red : Color.green;
            UnityEditor.Handles.Label(
                transform.position + Vector3.down * 0.5f,
                IsTriggered ? $"[TRIGGERED] {gameObject.name}" : gameObject.name);
        }
#endif
    }
}
