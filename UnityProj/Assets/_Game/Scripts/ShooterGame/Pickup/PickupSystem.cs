using System;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 道具拾取系统（TDD_02 S2.5）。
    /// 纯 C# 服务，由 BattleController 每帧驱动。
    /// 
    /// 职责：
    /// - 管理场景中活跃的道具实例
    /// - 检测玩家距离 → 自动拾取
    /// - 磁吸范围内掉落物飞向玩家（视觉反馈）
    /// - 生命周期：超时/底线消失
    /// - 拾取效果：Buff/修复/弹药/金币
    /// </summary>
    public sealed class PickupSystem
    {
        /// <summary>同屏最大道具数量</summary>
        private const int MAX_PICKUPS = 16;

        /// <summary>底线 Y 坐标（到此处消失）</summary>
        private const float BOTTOM_LINE_Y = -6.0f;

        /// <summary>闪烁开始前的剩余时间（最后 2s 闪烁）</summary>
        private const float BLINK_THRESHOLD = 2.0f;

        /// <summary>磁吸飞行基础速度（单位/秒）</summary>
        private const float ATTRACT_BASE_SPEED = 8f;

        /// <summary>磁吸飞行最大速度（单位/秒）</summary>
        private const float ATTRACT_MAX_SPEED = 25f;

        /// <summary>磁吸加速度（每秒增加的速度）</summary>
        private const float ATTRACT_ACCEL = 20f;

        /// <summary>飞到此距离内即拾取（世界单位）</summary>
        private const float COLLECT_DIST = 0.3f;
        private const float COLLECT_DIST_SQR = COLLECT_DIST * COLLECT_DIST;

        private readonly PickupInstance[] _pickups = new PickupInstance[MAX_PICKUPS];
        private int _activeCount;

        private Entity _playerEntity;
        private Entity _baseEntity;
        private SG_ProgressManager _progress;

        /// <summary>基础拾取半径（策划可通过 BattleController Inspector 调整）</summary>
        private float _basePickupRadius = 1.0f;

        /// <summary>当前活跃道具数量</summary>
        public int ActiveCount => _activeCount;

        /// <summary>
        /// 道具被拾取时的通知回调（UI 通知条用）。
        /// 参数：道具显示名称。
        /// </summary>
        public event Action<string> OnPickupCollected;

        /// <summary>
        /// 初始化系统引用。
        /// </summary>
        /// <param name="basePickupRadius">基础拾取半径（被动 Buff 会在此基础上乘以倍率）</param>
        public void Init(Entity playerEntity, Entity baseEntity, SG_ProgressManager progress, float basePickupRadius = 1.0f)
        {
            _playerEntity = playerEntity;
            _baseEntity = baseEntity;
            _progress = progress;
            _basePickupRadius = basePickupRadius;
            _activeCount = 0;
        }

        /// <summary>
        /// 清空所有道具（关卡重置时调用）。
        /// 必须清零数组防止幽灵数据残留（PIT-037 同类型）。
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _activeCount; i++)
                _pickups[i] = default;
            _activeCount = 0;
        }

        /// <summary>
        /// 在指定位置生成一个道具。
        /// </summary>
        public bool SpawnPickup(Vector2 position, PickupConfigSO config)
        {
            if (config == null || _activeCount >= MAX_PICKUPS) return false;

            ref var pickup = ref _pickups[_activeCount];
            pickup.Config = config;
            pickup.Position = position;
            pickup.RemainingTime = config.Lifetime;
            pickup.IsActive = true;
            pickup.IsAttracting = false;
            pickup.AttractSpeed = 0f;
            _activeCount++;
            return true;
        }

        /// <summary>
        /// 每帧驱动（由 BattleController 调用）。
        /// </summary>
        public void Tick(float dt)
        {
            if (_activeCount == 0 || _playerEntity == null) return;

            Vector2 playerPos = _playerEntity.Position;

            // 查询当前拾取半径（基础半径 × 被动 Buff 倍率）
            float radiusMod = 1f;
            var buffComp = _playerEntity.GetComponent(ComponentType.Buff) as BuffComponent;
            if (buffComp != null)
                radiusMod = buffComp.PickupRadiusModifier;
            float effectiveRadius = _basePickupRadius * radiusMod;
            float effectiveRadiusSqr = effectiveRadius * effectiveRadius;
            bool hasMagnet = radiusMod > 1.01f;

            for (int i = _activeCount - 1; i >= 0; i--)
            {
                ref var pickup = ref _pickups[i];
                if (!pickup.IsActive) continue;

                float dx = pickup.Position.x - playerPos.x;
                float dy = pickup.Position.y - playerPos.y;
                float distSqr = dx * dx + dy * dy;

                // ── 磁吸飞行状态 ──
                if (pickup.IsAttracting)
                {
                    // 已到达 → 拾取
                    if (distSqr < COLLECT_DIST_SQR)
                    {
                        CollectPickup(ref pickup);
                        RemoveAt(i);
                        continue;
                    }

                    // 加速飞向玩家
                    pickup.AttractSpeed += ATTRACT_ACCEL * dt;
                    if (pickup.AttractSpeed > ATTRACT_MAX_SPEED)
                        pickup.AttractSpeed = ATTRACT_MAX_SPEED;

                    float dist = Mathf.Sqrt(distSqr);
                    float step = pickup.AttractSpeed * dt;
                    if (step >= dist)
                    {
                        // 本帧直接到达
                        CollectPickup(ref pickup);
                        RemoveAt(i);
                        continue;
                    }

                    // 归一化方向 × 步长（飞向玩家）
                    float invDist = 1f / dist;
                    pickup.Position.x -= dx * invDist * step;
                    pickup.Position.y -= dy * invDist * step;
                    continue;
                }

                // ── 正常漂浮状态 ──

                // 向下漂浮
                pickup.Position.y -= pickup.Config.FloatSpeed * dt;

                // 生命周期检查
                pickup.RemainingTime -= dt;
                if (pickup.RemainingTime <= 0 || pickup.Position.y <= BOTTOM_LINE_Y)
                {
                    RemoveAt(i);
                    continue;
                }

                // 无磁吸时的普通拾取（基础半径判定）
                float baseRadiusSqr = _basePickupRadius * _basePickupRadius;
                if (distSqr < baseRadiusSqr)
                {
                    CollectPickup(ref pickup);
                    RemoveAt(i);
                    continue;
                }

                // 磁吸范围判定 → 进入飞行状态
                if (hasMagnet && distSqr < effectiveRadiusSqr)
                {
                    pickup.IsAttracting = true;
                    pickup.AttractSpeed = ATTRACT_BASE_SPEED;
                }
            }
        }

        /// <summary>
        /// 获取道具实例（UI 渲染用）。
        /// </summary>
        public ref readonly PickupInstance GetPickup(int index) => ref _pickups[index];

        /// <summary>
        /// 检查道具是否应该闪烁（剩余时间 < 2s）。
        /// </summary>
        public static bool ShouldBlink(float remainingTime)
        {
            return remainingTime > 0 && remainingTime <= BLINK_THRESHOLD;
        }

        /// <summary>
        /// 获取闪烁 Alpha（sin 波动 [0.3, 1.0]）。
        /// 前 1s = 2Hz，后 1s = 4Hz（加速暗示即将消失）。
        /// </summary>
        public static float GetBlinkAlpha(float remainingTime)
        {
            float elapsed = BLINK_THRESHOLD - remainingTime;
            float freq = elapsed < 1f ? 2f : 4f;
            float sin = Mathf.Sin(elapsed * freq * Mathf.PI * 2f);
            // 映射 [-1,1] → [0.3, 1.0]
            return 0.65f + 0.35f * sin;
        }

        // ── 拾取效果 ──

        private void CollectPickup(ref PickupInstance pickup)
        {
            var config = pickup.Config;

            switch (config.Type)
            {
                case PickupType.Buff:
                    if (config.BuffConfig != null)
                    {
                        var buff = _playerEntity.GetComponent(ComponentType.Buff) as BuffComponent;
                        buff?.ApplyBuff(config.BuffConfig);
                    }
                    break;

                case PickupType.Repair:
                    if (_baseEntity != null)
                    {
                        var health = _baseEntity.GetComponent(ComponentType.Health) as HealthComponent;
                        health?.Heal(config.RepairAmount);
                    }
                    break;

                case PickupType.Ammo:
                    if (config.AmmoBuffConfig != null)
                    {
                        var buff = _playerEntity.GetComponent(ComponentType.Buff) as BuffComponent;
                        buff?.ApplyBuff(config.AmmoBuffConfig);
                    }
                    break;

                case PickupType.Coin:
                    // V2: 金币暂存计数，等关卡结算时统一入账
                    // 后续 Sprint 补充金币系统
                    break;
            }

            // V2 S5.4: 通知 UI
            OnPickupCollected?.Invoke(config.DisplayName);
        }

        // ── 数组操作 ──

        private void RemoveAt(int index)
        {
            _pickups[index] = _pickups[_activeCount - 1];
            _pickups[_activeCount - 1] = default;
            _activeCount--;
        }
    }

    /// <summary>
    /// 道具实例数据（值类型，零 GC）。
    /// </summary>
    public struct PickupInstance
    {
        public PickupConfigSO Config;
        public Vector2 Position;
        public float RemainingTime;
        public bool IsActive;

        /// <summary>是否正在被磁吸飞向玩家</summary>
        public bool IsAttracting;

        /// <summary>当前磁吸飞行速度（加速曲线）</summary>
        public float AttractSpeed;
    }
}
