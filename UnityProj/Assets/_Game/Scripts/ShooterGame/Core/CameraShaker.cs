using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// Camera 震动——Perlin Noise 位移偏移，零 GC。
    /// 挂载在 Main Camera 所在 GO 上。
    /// TDD_02 §3.2：新震动覆盖旧震动（不叠加，V1 简化）。
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        private Vector3 _originalPos;
        private float _duration;
        private float _intensity;
        private float _elapsed;
        private AnimationCurve _decayCurve;
        private bool _isShaking;

        private void Awake()
        {
            _originalPos = transform.localPosition;
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
    }
}
