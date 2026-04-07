using System;
using MiniGameTemplate.Core;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;
using MiniGameTemplate.UI;

namespace Example
{
    /// <summary>
    /// Data passed to ClickCounterPanel when opening.
    /// </summary>
    public class ClickCounterPanelData
    {
        public IWeChatBridge WeChatBridge;
        public Action OnBackToMenu;
    }

    /// <summary>
    /// Standalone ClickCounter gameplay panel.
    /// Requires FairyGUI component Example/ClickCounterPanel to be exported.
    /// </summary>
    public partial class ClickCounterPanel : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL + 10;
        public bool IsFullScreen => true;
        public string PanelPackageName => "Example";

        private const string HighScoreKey = "example_high_score";
        private const float RoundDuration = 10f;
        private const float TickInterval = 0.1f;

        private IWeChatBridge _weChatBridge;
        private Action _onBackToMenu;

        private int _score;
        private int _highScore;
        private float _remaining;
        private bool _isRoundRunning;
        private bool _isWaitingRewardedAd;
        private int _lifecycleVersion;
        private TimerHandle _timer = TimerHandle.Invalid;

        public void OnOpen(object data)
        {
            _lifecycleVersion++;

            // Bind button events (only in OnOpen — never re-bind)
            if (btnTap != null) btnTap.onClick.Add(OnTapClicked);
            if (btnBack != null) btnBack.onClick.Add(OnBackClicked);
            if (btnRestart != null) btnRestart.onClick.Add(OnRestartClicked);
            if (btnShare != null) btnShare.onClick.Add(OnShareClicked);

            ApplyData(data);
        }

        public void OnClose()
        {
            _lifecycleVersion++;
            CancelTimer();
            _isWaitingRewardedAd = false;
            _weChatBridge = null;
            _onBackToMenu = null;
        }

        public void OnRefresh(object data)
        {
            _lifecycleVersion++;
            // Only update data — do NOT re-bind events
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            var panelData = data as ClickCounterPanelData;
            _weChatBridge = panelData?.WeChatBridge;
            _onBackToMenu = panelData?.OnBackToMenu;

            _highScore = GameBootstrapper.SaveSystem?.LoadInt(HighScoreKey, 0) ?? 0;
            StartRound();
        }

        private void StartRound(int startBonusScore = 0)
        {
            CancelTimer();

            _isWaitingRewardedAd = false;
            _score = startBonusScore;
            _remaining = RoundDuration;
            _isRoundRunning = true;

            if (txtTitle != null) txtTitle.text = "ClickCounter";
            if (txtHint != null) txtHint.text = startBonusScore > 0 ? "奖励生效：开局加分" : "疯狂点击中央按钮";

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

            if (txtTitle != null) txtTitle.text = $"本局得分：{_score}";
            if (txtHint != null) txtHint.text = "可看激励广告开局加分";
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
            UIManager.Instance.ClosePanel<ClickCounterPanel>();
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
            if (txtHint != null)
                txtHint.text = "正在加载激励广告...";

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
            if (txtScore != null) txtScore.text = _score.ToString();
            if (txtTimer != null) txtTimer.text = _remaining.ToString("F1");
            if (txtHighScore != null) txtHighScore.text = $"最高分：{_highScore}";
        }

        private void CancelTimer()
        {
            TimerService.Instance.Cancel(_timer);
            _timer = TimerHandle.Invalid;
        }
    }
}
