using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 波次指示器增强动效（TDD_05 S5.4 / PK-R3 UID-010）。
    /// 常规波次: scale弹跳1.0→1.3→1.0(0.3s)+色高亮。
    /// FINAL WAVE: 红色+1.0→1.5→1.0(0.4s)。
    /// 首波也执行弹跳（取消首波无动效限制）。
    /// </summary>
    public class WaveIndicatorAnimator
    {
        private readonly GTextField _waveText;
        private readonly Color _normalColor = Color.white;
        private readonly Color _finalColor = new Color(1f, 0.3f, 0.3f);

        public WaveIndicatorAnimator(GTextField waveText)
        {
            _waveText = waveText;
        }

        /// <summary>
        /// 播放波次切换动效。
        /// </summary>
        /// <param name="currentWave">当前波次（1-based）</param>
        /// <param name="totalWaves">总波次数</param>
        public void PlayWaveTransition(int currentWave, int totalWaves)
        {
            if (_waveText == null) return;

            bool isFinal = currentWave >= totalWaves;
            float targetScale = isFinal ? 1.5f : 1.3f;
            float duration = isFinal ? 0.4f : 0.3f;
            Color highlightColor = isFinal ? _finalColor : _normalColor;

            _waveText.color = highlightColor;

            // 弹跳动效
            GTween.Kill(_waveText);
            _waveText.SetScale(1f, 1f);
            _waveText.TweenScale(new Vector2(targetScale, targetScale), duration * 0.4f)
                .SetEase(EaseType.BackOut)
                .OnComplete(() =>
                {
                    _waveText.TweenScale(new Vector2(1f, 1f), duration * 0.6f)
                        .SetEase(EaseType.BounceOut)
                        .OnComplete(() =>
                        {
                            // 恢复颜色
                            if (!isFinal) _waveText.color = _normalColor;
                        });
                });
        }
    }
}
