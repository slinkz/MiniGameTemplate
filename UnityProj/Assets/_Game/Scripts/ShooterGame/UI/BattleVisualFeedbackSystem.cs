using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 战斗视觉反馈系统（TDD_05 S5.6）。
    /// 统一管理：红闪 / PA-04 反击时序链 / 技能释放闪白 / 道具吸入。
    /// 单一实例，由 BattleController 持有和驱动。
    /// </summary>
    public class BattleVisualFeedbackSystem
    {
        // ── 红闪（屏幕边缘）──
        private GGraph _redFlashOverlay;
        private float _redFlashTimer;
        private float _redFlashDuration;
        private float _redFlashMaxAlpha;

        // 优先级排队（P0 = 红闪，P1 = 其他）
        private bool _isP0Active;
        private const float P0_SUPPRESS_DELAY = 0.3f;

        // ── 反击时序链 ──
        private float _counterAttackTimer = -1f;
        private System.Action _counterAttackBulletCallback;

        /// <summary>
        /// 初始化。传入 HUD View 用于创建红闪 overlay。
        /// </summary>
        public void Init(GComponent hudView)
        {
            // 创建红闪 overlay（仅边缘 100pt 区域，中央透明）
            _redFlashOverlay = hudView.GetChild("red_flash") as GGraph;
            if (_redFlashOverlay == null)
            {
                _redFlashOverlay = new GGraph();
                _redFlashOverlay.SetSize(GRoot.inst.width, GRoot.inst.height);
                _redFlashOverlay.DrawRect(_redFlashOverlay.width, _redFlashOverlay.height,
                    0, Color.clear, new Color(1f, 0f, 0f, 0.6f));
                _redFlashOverlay.name = "red_flash";
                // sortOrder 在 Z-order 层级 50（高于 HUD 层 10）
                hudView.AddChild(_redFlashOverlay);
            }
            _redFlashOverlay.alpha = 0f;
            _redFlashOverlay.visible = false;
        }

        /// <summary>
        /// 每帧更新。
        /// </summary>
        public void Tick(float dt)
        {
            // 红闪淡出
            if (_redFlashTimer > 0f)
            {
                _redFlashTimer -= dt;
                float t = Mathf.Clamp01(_redFlashTimer / _redFlashDuration);
                _redFlashOverlay.alpha = t * _redFlashMaxAlpha;

                if (_redFlashTimer <= 0f)
                {
                    _redFlashOverlay.visible = false;
                    _isP0Active = false;
                }
            }

            // PA-04 反击时序链
            if (_counterAttackTimer >= 0f)
            {
                _counterAttackTimer += dt;

                if (_counterAttackTimer >= 0.20f && _counterAttackBulletCallback != null)
                {
                    _counterAttackBulletCallback.Invoke();
                    _counterAttackBulletCallback = null;
                    _counterAttackTimer = -1f; // 链完成
                }
            }
        }

        // ──────────────────── 公共接口 ────────────────────

        /// <summary>
        /// 触发红闪（基地受伤 / 敌机碰飞机）。
        /// </summary>
        /// <param name="intensity">强度：0=轻微（基地被弹），1=加强（碰撞）</param>
        public void TriggerRedFlash(int intensity = 0)
        {
            _redFlashDuration = intensity == 0 ? 0.3f : 0.4f;
            _redFlashMaxAlpha = intensity == 0 ? 0.4f : 0.6f;
            _redFlashTimer = _redFlashDuration;
            _redFlashOverlay.alpha = _redFlashMaxAlpha;
            _redFlashOverlay.visible = true;
            _isP0Active = true;
        }

        /// <summary>
        /// 触发 PA-04 反击时序链。
        /// T+0.00s: 红闪 → T+0.15s: 图标闪白+"反击！" → T+0.20s: 8发弹幕（回调）
        /// </summary>
        public void TriggerCounterAttackSequence(System.Action onFireBullets)
        {
            // T+0.00s: 红闪
            TriggerRedFlash(0);

            // 启动时序链
            _counterAttackTimer = 0f;
            _counterAttackBulletCallback = onFireBullets;

            // T+0.15s: 图标闪白（由外部订阅者处理，这里只触发回调延迟）
            // 这里简化——实际闪白效果在 PassiveIndicatorPanel 中处理
        }

        /// <summary>
        /// 触发道具拾取吸入效果。
        /// worldPos → 飞向 HUD 指定位置 + 光芒爆发。
        /// </summary>
        public void TriggerPickupCollectEffect(Vector3 worldPos)
        {
            // 简化实现：光芒爆发可通过 VFX Pool 实现
            // UI 通知由 PickupNotificationQueue 处理
            // 此处预留 Sprite 飞入动画（需 Camera 引用，由 BattleController 注入）
        }

        /// <summary>
        /// P0 是否正在激活（用于延迟其他通知）。
        /// </summary>
        public bool IsP0Active => _isP0Active;

        public void Dispose()
        {
            if (_redFlashOverlay != null)
            {
                _redFlashOverlay.Dispose();
                _redFlashOverlay = null;
            }
        }
    }
}
