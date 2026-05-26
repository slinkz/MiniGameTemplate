using MiniGameTemplate.Battle;
using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// Camera 震动——Perlin Noise 位移偏移，零 GC。
    /// 挂载在 Main Camera 所在 GO 上。
    /// TDD_02 §3.2：新震动覆盖旧震动（不叠加，V1 简化）。
    /// TDD-07 B6：实现 IBattleCleanup（退场时停止震动）。
    /// </summary>
    public class CameraShaker : MonoBehaviour, IBattleCleanup
    {
        private Vector3 _originalPos;
        private float _duration;
        private float _intensity;
        private float _elapsed;
        private AnimationCurve _decayCurve;
        private bool _isShaking;

        [Header("TDD-07: 退场事件通道")]
        [SerializeField] private BattleLifecycleEvent _onBattleEnd;

        private void Awake()
        {
            _originalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            // TDD-07 B6: 注册退场清理
            if (_onBattleEnd != null)
                _onBattleEnd.Register(this);
        }

        private void OnDisable()
        {
            // TDD-07 B6: 注销退场清理
            if (_onBattleEnd != null)
                _onBattleEnd.Unregister(this);
        }

        public void Shake(float duration, float intensity, AnimationCurve decay)
        {
            _duration = duration;
            _intensity = intensity;
            _decayCurve = decay;
            _elapsed = 0f;
            _isShaking = true;
        }

        private void LateUpdate()
        {
            if (!_isShaking) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                _isShaking = false;
                transform.localPosition = _originalPos;
                return;
            }

            float t = _elapsed / _duration;
            float strength = _intensity * (_decayCurve != null
                ? _decayCurve.Evaluate(t) : (1f - t));

            float offsetX = (Mathf.PerlinNoise(_elapsed * 25f, 0f) - 0.5f) * 2f * strength;
            float offsetY = (Mathf.PerlinNoise(0f, _elapsed * 25f) - 0.5f) * 2f * strength;

            transform.localPosition = _originalPos + new Vector3(offsetX, offsetY, 0f);
        }

        /// <summary>强制停止震动并复位（ET-004: 重试时调用）</summary>
        public void StopShake()
        {
            _isShaking = false;
            transform.localPosition = _originalPos;
        }

        // ── TDD-07: IBattleCleanup 实现 ──

        /// <summary>停止震动——在弹幕/碰撞清理之后。</summary>
        public int CleanupOrder => 50;

        /// <summary>退场清理回调。</summary>
        public void OnBattleCleanup() => StopShake();
    }
}
