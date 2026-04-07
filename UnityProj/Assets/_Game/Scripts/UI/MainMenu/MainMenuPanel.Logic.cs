using MiniGameTemplate.Core;
using MiniGameTemplate.Events;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;

namespace MainMenu
{
    /// <summary>
    /// Data passed to MainMenuPanel when opening.
    /// </summary>
    public class MainMenuPanelData
    {
        public GameEvent StartGameEvent;
        public IWeChatBridge WeChatBridge;
        public bool EnableBannerAd = true;
    }

    /// <summary>
    /// Main menu / lobby panel — the player's hub after loading completes.
    /// Displays player info, start button, and utility shortcuts.
    ///
    /// If StartGameEvent is not configured in Boot scene, this panel runs a built-in
    /// ClickCounter fallback mode so the template remains playable out of the box.
    /// </summary>
    public partial class MainMenuPanel : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL;
        public bool IsFullScreen => true;
        public string PanelPackageName => "MainMenu";

        private const string HighScoreKey = "example_high_score";
        private const float DefaultRoundDuration = 10f;
        private const float TickInterval = 0.1f;

        private GameEvent _startGameEvent;
        private IWeChatBridge _weChatBridge;
        private bool _enableBannerAd = true;

        private bool _isLocalRoundRunning;
        private bool _isResultState;
        private bool _isWaitingRewardedAd;
        private int _lifecycleVersion;

        private int _score;
        private int _highScore;
        private float _remainingTime;
        private TimerHandle _countdownTimer = TimerHandle.Invalid;

        public void OnOpen(object data)
        {
            _lifecycleVersion++;

            // Bind button events (only in OnOpen — never re-bind)
            if (btnStart != null) btnStart.onClick.Add(OnStartClicked);
            if (btnSettings != null) btnSettings.onClick.Add(OnSettingsClicked);
            if (btnRanking != null) btnRanking.onClick.Add(OnRankingClicked);
            if (btnShare != null) btnShare.onClick.Add(OnShareClicked);

            ApplyData(data);
        }

        public void OnClose()
        {
            _lifecycleVersion++;
            CancelRoundTimer();
            if (_enableBannerAd)
                _weChatBridge?.HideBannerAd();

            _isWaitingRewardedAd = false;
            _startGameEvent = null;
            _weChatBridge = null;
        }

        public void OnRefresh(object data)
        {
            _lifecycleVersion++;
            // Only update data — do NOT re-bind events
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            var menuData = data as MainMenuPanelData;

            if (menuData != null)
            {
                _startGameEvent = menuData.StartGameEvent;
                _weChatBridge = menuData.WeChatBridge;
                _enableBannerAd = menuData.EnableBannerAd;
            }
            else
            {
                _enableBannerAd = true;
            }

            LoadHighScore();
            EnterMenuState();
        }

        private void LoadHighScore()
        {
            if (GameBootstrapper.SaveSystem == null)
            {
                _highScore = 0;
                return;
            }

            _highScore = GameBootstrapper.SaveSystem.LoadInt(HighScoreKey, 0);
        }

        private void RefreshPlayerInfo()
        {
            if (txtNickname == null)
                return;

            if (_weChatBridge == null)
            {
                txtNickname.text = "点击开始游戏";
                return;
            }

            var userInfo = _weChatBridge.GetUserInfo();
            txtNickname.text = userInfo != null ? userInfo.Nickname : "点击开始游戏";
        }

        private void OnStartClicked()
        {
            if (_startGameEvent != null)
            {
                _startGameEvent.Raise();
                return;
            }

            if (_isLocalRoundRunning)
            {
                _score += 1;
                RefreshLocalHud();
                return;
            }

            StartLocalRound();
        }

        private void OnSettingsClicked()
        {
            if (_isLocalRoundRunning || _isResultState)
            {
                EnterMenuState();
                return;
            }

            GameLog.Log("[MainMenuPanel] Settings button clicked (not yet implemented).");
        }

        private void OnRankingClicked()
        {
            if (_isLocalRoundRunning)
            {
                StartLocalRound();
                return;
            }

            if (_isResultState)
            {
                StartRoundViaRewardedAd();
                return;
            }

            _weChatBridge?.ShowRankingPanel();
        }

        private void OnShareClicked()
        {
            if (_isLocalRoundRunning || _isResultState)
            {
                _weChatBridge?.Share($"我在 ClickCounter 得了 {_score} 分，来挑战我！", "", $"score={_score}");
                return;
            }

            _weChatBridge?.Share("来和我一起玩吧！", "", "");
        }

        private void EnterMenuState()
        {
            CancelRoundTimer();
            _isLocalRoundRunning = false;
            _isResultState = false;
            _isWaitingRewardedAd = false;

            RefreshPlayerInfo();

            if (txtGameTitle != null)
                txtGameTitle.text = "ClickCounter";
            if (txtVersion != null)
                txtVersion.text = $"最高分：{_highScore}";

            if (btnStart != null) btnStart.title = "开始游戏";
            if (btnSettings != null) btnSettings.title = "设置";
            if (btnRanking != null) btnRanking.title = "排行";
            if (btnShare != null) btnShare.title = "分享";

            if (_enableBannerAd)
                _weChatBridge?.ShowBannerAd();
        }

        private void StartLocalRound(int startBonusScore = 0)
        {
            CancelRoundTimer();

            if (_enableBannerAd)
                _weChatBridge?.HideBannerAd();

            _isWaitingRewardedAd = false;
            _isLocalRoundRunning = true;
            _isResultState = false;
            _score = startBonusScore;
            _remainingTime = DefaultRoundDuration;

            if (btnStart != null) btnStart.title = "点击 +1";
            if (btnSettings != null) btnSettings.title = "返回";
            if (btnRanking != null) btnRanking.title = "重开";
            if (btnShare != null) btnShare.title = "晒分";

            if (txtNickname != null)
                txtNickname.text = startBonusScore > 0 ? "奖励生效：开局加分" : "疯狂点击开始！";

            RefreshLocalHud();

            _countdownTimer = TimerService.Instance.Repeat(TickInterval, OnLocalRoundTick);
        }

        private void StartRoundViaRewardedAd()
        {
            if (_isWaitingRewardedAd)
                return;

            if (_weChatBridge == null)
            {
                StartLocalRound();
                return;
            }

            _isWaitingRewardedAd = true;
            int requestVersion = _lifecycleVersion;
            if (txtNickname != null)
                txtNickname.text = "正在加载激励广告...";

            _weChatBridge.ShowRewardedAd(success =>
            {
                if (requestVersion != _lifecycleVersion)
                    return;

                int bonusScore = success ? 3 : 0;
                StartLocalRound(bonusScore);
            });
        }

        private void OnLocalRoundTick()
        {
            if (!_isLocalRoundRunning)
                return;

            _remainingTime -= TickInterval;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                EndLocalRound();
                return;
            }

            RefreshLocalHud();
        }

        private void EndLocalRound()
        {
            CancelRoundTimer();
            _isLocalRoundRunning = false;
            _isResultState = true;

            if (_score > _highScore)
            {
                _highScore = _score;
                if (GameBootstrapper.SaveSystem != null)
                {
                    GameBootstrapper.SaveSystem.SaveInt(HighScoreKey, _highScore);
                    GameBootstrapper.SaveSystem.Save();
                }
            }

            if (txtNickname != null)
                txtNickname.text = "本局结束";
            if (txtGameTitle != null)
                txtGameTitle.text = $"本局得分：{_score}";
            if (txtVersion != null)
                txtVersion.text = $"最高分：{_highScore}";

            if (btnStart != null) btnStart.title = "再来一局";
            if (btnSettings != null) btnSettings.title = "返回";
            if (btnRanking != null) btnRanking.title = "激励重开";
            if (btnShare != null) btnShare.title = "晒分";

            _weChatBridge?.ShowInterstitialAd();
        }

        private void RefreshLocalHud()
        {
            if (txtGameTitle != null)
                txtGameTitle.text = $"得分：{_score}";

            if (txtVersion != null)
                txtVersion.text = $"剩余：{_remainingTime:F1}s | 最高：{_highScore}";
        }

        private void CancelRoundTimer()
        {
            TimerService.Instance.Cancel(_countdownTimer);
            _countdownTimer = TimerHandle.Invalid;
        }
    }
}
