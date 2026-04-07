using System;
using MiniGameTemplate.Core;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;

namespace Game.UI
{
    public class ClickCounterPanelData
    {
        public IWeChatBridge WeChatBridge;
        public Action OnBackToMenu;
    }

    /// <summary>
    /// Standalone ClickCounter gameplay panel.
    /// Requires FairyGUI component Example/ClickCounterPanel to be exported.
    /// </summary>
    public partial class ClickCounterPanel
    {
        private const string HighScoreKey = "example_high_score";
        private const float RoundDuration = 10f;
        private const float TickInterval = 0.1f;

        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_NORMAL + 10;

        private IWeChatBridge _weChatBridge;

        private Action _onBackToMenu;

        private int _score;
        private int _highScore;
        private float _remaining;
        private bool _isRoundRunning;
        private bool _isWaitingRewardedAd;
        private int _lifecycleVersion;
        private TimerHandle _timer = TimerHandle.Invalid;

        protected void AddEvents()
        {
            if (_btnTap != null) _btnTap.onClick.Add(OnTapClicked);
            if (_btnBack != null) _btnBack.onClick.Add(OnBackClicked);
            if (_btnRestart != null) _btnRestart.onClick.Add(OnRestartClicked);
            if (_btnShare != null) _btnShare.onClick.Add(OnShareClicked);
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            _lifecycleVersion++;

            var panelData = data as ClickCounterPanelData;
            _weChatBridge = panelData?.WeChatBridge;
            _onBackToMenu = panelData?.OnBackToMenu;

            _highScore = GameBootstrapper.SaveSystem?.LoadInt(HighScoreKey, 0) ?? 0;
            StartRound();
        }

        protected override void OnClose()
        {
            _lifecycleVersion++;
            CancelTimer();
            _isWaitingRewardedAd = false;
            _weChatBridge = null;
            _onBackToMenu = null;
            base.OnClose();
        }

        private void StartRound(int startBonusScore = 0)
        {
            CancelTimer();

            _isWaitingRewardedAd = false;
            _score = startBonusScore;
            _remaining = RoundDuration;
            _isRoundRunning = true;

            if (_txtTitle != null) _txtTitle.text = "ClickCounter";
            if (_txtHint != null) _txtHint.text = startBonusScore > 0 ? "奖励生效：开局加分" : "疯狂点击中央按钮";

            RefreshHud();
            _timer = TimerService.Instance.Repeat(TickInterval, OnTick);
        }

        private void OnTick()
        {
            if (!_isRoundRunning)
                return;

            _remaining -= TickInterval;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                EndRound();
                return;
            }

            RefreshHud();
        }

        private void EndRound()
        {
            CancelTimer();
            _isRoundRunning = false;

            if (_score > _highScore)
            {
                _highScore = _score;
                if (GameBootstrapper.SaveSystem != null)
                {
                    GameBootstrapper.SaveSystem.SaveInt(HighScoreKey, _highScore);
                    GameBootstrapper.SaveSystem.Save();
                }
            }

            _weChatBridge?.ShowInterstitialAd();

            if (_txtTitle != null) _txtTitle.text = $"本局得分：{_score}";
            if (_txtHint != null) _txtHint.text = "可看激励广告开局加分";
            RefreshHud();
        }

        private void OnTapClicked()
        {
            if (!_isRoundRunning)
                return;

            _score += 1;
            RefreshHud();
        }

        private void OnBackClicked()
        {
            var onBackToMenu = _onBackToMenu;
            MiniGameTemplate.UI.UIManager.Instance.ClosePanel<ClickCounterPanel>();
            onBackToMenu?.Invoke();
        }

        private void OnRestartClicked()
        {
            StartRoundViaRewardedAd();
        }

        private void StartRoundViaRewardedAd()
        {
            if (_isWaitingRewardedAd)
                return;

            if (_weChatBridge == null)
            {
                StartRound();
                return;
            }

            _isWaitingRewardedAd = true;
            int requestVersion = _lifecycleVersion;
            if (_txtHint != null)
                _txtHint.text = "正在加载激励广告...";

            _weChatBridge.ShowRewardedAd(success =>
            {
                if (requestVersion != _lifecycleVersion)
                    return;

                int bonusScore = success ? 3 : 0;
                StartRound(bonusScore);
            });
        }

        private void OnShareClicked()
        {
            _weChatBridge?.Share($"我在 ClickCounter 得了 {_score} 分，来挑战我！", "", $"score={_score}");
        }

        private void RefreshHud()
        {
            if (_txtScore != null) _txtScore.text = _score.ToString();
            if (_txtTimer != null) _txtTimer.text = _remaining.ToString("F1");
            if (_txtHighScore != null) _txtHighScore.text = $"最高分：{_highScore}";
        }

        private void CancelTimer()
        {
            TimerService.Instance.Cancel(_timer);
            _timer = TimerHandle.Invalid;
        }
    }
}
