using FairyGUI;
using MiniGameTemplate.Core;
using MiniGameTemplate.Events;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Utils;

namespace Game.UI
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
    public class MainMenuPanel : MiniGameTemplate.UI.UIBase
    {
        private const string HighScoreKey = "example_high_score";
        private const float DefaultRoundDuration = 10f;
        private const float TickInterval = 0.1f;

        protected override string PackageName => MiniGameTemplate.UI.UIConstants.PKG_MAIN_MENU;
        protected override string ComponentName => MiniGameTemplate.UI.UIConstants.COMP_MAIN_MENU_PANEL;
        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_NORMAL;

        private GTextField _txtNickname;
        private GTextField _txtGameTitle;
        private GTextField _txtVersion;
        private GButton _btnStart;
        private GButton _btnSettings;
        private GButton _btnRanking;
        private GButton _btnShare;

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

        protected override void OnInit()
        {
            base.OnInit();
            _txtNickname = ContentPane.GetChild("txtNickname") as GTextField;
            _txtGameTitle = ContentPane.GetChild("txtGameTitle") as GTextField;
            _txtVersion = ContentPane.GetChild("txtVersion") as GTextField;
            _btnStart = ContentPane.GetChild("btnStart") as GButton;
            _btnSettings = ContentPane.GetChild("btnSettings") as GButton;
            _btnRanking = ContentPane.GetChild("btnRanking") as GButton;
            _btnShare = ContentPane.GetChild("btnShare") as GButton;

            if (_btnStart != null) _btnStart.onClick.Add(OnStartClicked);
            if (_btnSettings != null) _btnSettings.onClick.Add(OnSettingsClicked);
            if (_btnRanking != null) _btnRanking.onClick.Add(OnRankingClicked);
            if (_btnShare != null) _btnShare.onClick.Add(OnShareClicked);
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            _lifecycleVersion++;

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

        protected override void OnClose()
        {
            _lifecycleVersion++;
            CancelRoundTimer();
            if (_enableBannerAd)
                _weChatBridge?.HideBannerAd();

            _isWaitingRewardedAd = false;
            _startGameEvent = null;
            _weChatBridge = null;
            base.OnClose();
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
            if (_txtNickname == null)
                return;

            if (_weChatBridge == null)
            {
                _txtNickname.text = "点击开始游戏";
                return;
            }

            var userInfo = _weChatBridge.GetUserInfo();
            _txtNickname.text = userInfo != null ? userInfo.Nickname : "点击开始游戏";
        }

        private void OnStartClicked()
        {
            // Preferred path: event-driven game flow configured in Boot scene.
            if (_startGameEvent != null)
            {
                _startGameEvent.Raise();
                return;
            }

            // Fallback path: local ClickCounter mode inside MainMenuPanel.
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
            // In fallback ClickCounter mode: acts as "Back to Menu".
            if (_isLocalRoundRunning || _isResultState)
            {
                EnterMenuState();
                return;
            }

            GameLog.Log("[MainMenuPanel] Settings button clicked (not yet implemented).");
        }

        private void OnRankingClicked()
        {
            // In fallback ClickCounter mode: acts as "Restart".
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

            if (_txtGameTitle != null)
                _txtGameTitle.text = "ClickCounter";
            if (_txtVersion != null)
                _txtVersion.text = $"最高分：{_highScore}";

            if (_btnStart != null) _btnStart.title = "开始游戏";
            if (_btnSettings != null) _btnSettings.title = "设置";
            if (_btnRanking != null) _btnRanking.title = "排行";
            if (_btnShare != null) _btnShare.title = "分享";

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

            if (_btnStart != null) _btnStart.title = "点击 +1";
            if (_btnSettings != null) _btnSettings.title = "返回";
            if (_btnRanking != null) _btnRanking.title = "重开";
            if (_btnShare != null) _btnShare.title = "晒分";

            if (_txtNickname != null)
                _txtNickname.text = startBonusScore > 0 ? "奖励生效：开局加分" : "疯狂点击开始！";

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
            if (_txtNickname != null)
                _txtNickname.text = "正在加载激励广告...";

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

            if (_txtNickname != null)
                _txtNickname.text = "本局结束";
            if (_txtGameTitle != null)
                _txtGameTitle.text = $"本局得分：{_score}";
            if (_txtVersion != null)
                _txtVersion.text = $"最高分：{_highScore}";

            if (_btnStart != null) _btnStart.title = "再来一局";
            if (_btnSettings != null) _btnSettings.title = "返回";
            if (_btnRanking != null) _btnRanking.title = "激励重开";
            if (_btnShare != null) _btnShare.title = "晒分";

            _weChatBridge?.ShowInterstitialAd();
        }


        private void RefreshLocalHud()
        {
            if (_txtGameTitle != null)
                _txtGameTitle.text = $"得分：{_score}";

            if (_txtVersion != null)
                _txtVersion.text = $"剩余：{_remainingTime:F1}s | 最高：{_highScore}";
        }

        private void CancelRoundTimer()
        {
            TimerService.Instance.Cancel(_countdownTimer);
            _countdownTimer = TimerHandle.Invalid;
        }
    }
}

