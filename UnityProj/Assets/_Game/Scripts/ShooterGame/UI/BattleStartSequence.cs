using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 战斗开始过渡动效（TDD_05 S5.4 / PK-R3 UID-010）。
    /// 暗幕淡入→"Wave 1"文字弹跳→暗幕淡出→回调 BattleController。
    /// 使用 GTween ignoreTimeScale=true 模式，不受 Time.timeScale=0 影响。
    /// </summary>
    public class BattleStartSequence
    {
        private readonly GComponent _hudView;
        private GGraph _darkOverlay;
        private GTextField _waveAnnounce;
        private System.Action _onComplete;

        public BattleStartSequence(GComponent hudView)
        {
            _hudView = hudView;
        }

        /// <summary>
        /// 播放战斗开始动效。完成后回调 onComplete。
        /// 总时长 1.0s。
        /// </summary>
        public void Play(int waveNumber, System.Action onComplete)
        {
            _onComplete = onComplete;

            // 获取或创建暗幕
            _darkOverlay = _hudView.GetChild("dark_overlay") as GGraph;
            if (_darkOverlay == null)
            {
                _darkOverlay = new GGraph();
                _darkOverlay.SetSize(GRoot.inst.width, GRoot.inst.height);
                _darkOverlay.DrawRect(_darkOverlay.width, _darkOverlay.height, 0, Color.clear, Color.black);
                _darkOverlay.name = "dark_overlay";
                _hudView.AddChild(_darkOverlay);
            }
            _darkOverlay.visible = true;
            _darkOverlay.alpha = 0f;

            // 获取或创建波次宣告文字
            _waveAnnounce = _hudView.GetChild("text_wave_announce") as GTextField;
            if (_waveAnnounce == null)
            {
                _waveAnnounce = new GTextField();
                _waveAnnounce.name = "text_wave_announce";
                _waveAnnounce.SetSize(400, 60);
                var tf = _waveAnnounce.textFormat;
                tf.size = 36;
                tf.color = Color.white;
                _waveAnnounce.textFormat = tf;
                _waveAnnounce.align = AlignType.Center;
                _waveAnnounce.verticalAlign = VertAlignType.Middle;
                _hudView.AddChild(_waveAnnounce);
            }
            _waveAnnounce.text = $"Wave {waveNumber}";
            _waveAnnounce.SetXY(
                (GRoot.inst.width - _waveAnnounce.width) * 0.5f,
                (GRoot.inst.height - _waveAnnounce.height) * 0.5f);
            _waveAnnounce.visible = true;
            _waveAnnounce.alpha = 0f;
            _waveAnnounce.SetScale(2f, 2f);

            // T+0.0s：暗幕淡入 (0→0.5, 0.2s)
            _darkOverlay.TweenFade(0.5f, 0.2f)
                .SetIgnoreEngineTimeScale(true)
                .OnComplete(PlayWaveText);
        }

        private void PlayWaveText()
        {
            // T+0.2s：文字放大缩小（scale 2.0→1.0→1.2→1.0, 0.5s, EaseOutBack）
            _waveAnnounce.alpha = 1f;
            _waveAnnounce.TweenScale(new Vector2(1f, 1f), 0.25f)
                .SetIgnoreEngineTimeScale(true)
                .SetEase(EaseType.BackOut)
                .OnComplete(PlayBounce);
        }

        private void PlayBounce()
        {
            _waveAnnounce.TweenScale(new Vector2(1.2f, 1.2f), 0.1f)
                .SetIgnoreEngineTimeScale(true)
                .OnComplete(() =>
                {
                    _waveAnnounce.TweenScale(new Vector2(1f, 1f), 0.15f)
                        .SetIgnoreEngineTimeScale(true)
                        .OnComplete(FadeOutOverlay);
                });
        }

        private void FadeOutOverlay()
        {
            // T+0.7s：暗幕淡出（alpha→0, 0.3s）
            _darkOverlay.TweenFade(0f, 0.3f)
                .SetIgnoreEngineTimeScale(true);
            _waveAnnounce.TweenFade(0f, 0.3f)
                .SetIgnoreEngineTimeScale(true)
                .OnComplete(SequenceComplete);
        }

        private void SequenceComplete()
        {
            _darkOverlay.visible = false;
            _waveAnnounce.visible = false;
            _onComplete?.Invoke();
        }
    }
}
