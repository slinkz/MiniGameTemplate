using System;
using FairyGUI;
using MiniGameTemplate.Core;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Utils;

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

    public class ClickCounterPanel : MiniGameTemplate.UI.UIBase
    {
        private const string HighScoreKey = "example_high_score";
        private const float RoundDuration = 10f;
        private const float TickInterval = 0.1f;

        protected override string PackageName => MiniGameTemplate.UI.UIConstants.PKG_EXAMPLE;

        protected override string ComponentName => MiniGameTemplate.UI.UIConstants.COMP_CLICK_COUNTER_PANEL;
        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_NORMAL + 10;

        private GTextField _txtTitle;
        private GTextField _txtHighScore;
        private GTextField _txtScore;
        private GTextField _txtTimer;
        private GTextField _txtHint;

        private GButton _btnTap;
        private GButton _btnBack;
        private GButton _btnRestart;
        private GButton _btnShare;

        private IWeChatBridge _weChatBridge;
        private Action _onBackToMenu;

        private int _score;
        private int _highScore;
        private float _remaining;
        private bool _isRoundRunning;
        private TimerHandle _timer = TimerHandle.Invalid;

        protected override void OnInit()
        {
            base.OnInit();

            _txtTitle = ContentPane.GetChild("txtTitle") as GTextField;
            _txtHighScore = ContentPane.GetChild("txtHighScore") as GTextField;
            _txtScore = ContentPane.GetChild("txtScore") as GTextField;
            _txtTimer = ContentPane.GetChild("txtTimer") as GTextField;
            _txtHint = ContentPane.GetChild("txtHint") as GTextField;

            _btnTap = ContentPane.GetChild("btnTap") as GButton;
            _btnBack = ContentPane.GetChild("btnBack") as GButton;
            _btnRestart = ContentPane.GetChild("btnRestart") as GButton;
            _btnShare = ContentPane.GetChild("btnShare") as GButton;

            if (_btnTap != null) _btnTap.onClick.Add(OnTapClicked);
            if (_btnBack != null) _btnBack.onClick.Add(OnBackClicked);
            if (_btnRestart != null) _btnRestart.onClick.Add(OnRestartClicked);
            if (_btnShare != null) _btnShare.onClick.Add(OnShareClicked);
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            var panelData = data as ClickCounterPanelData;
            _weChatBridge = panelData?.WeChatBridge;
            _onBackToMenu = panelData?.OnBackToMenu;

            _highScore = GameBootstrapper.SaveSystem?.LoadInt(HighScoreKey, 0) ?? 0;
            StartRound();
        }

        protected override void OnClose()
        {
            CancelTimer();
            _weChatBridge = null;
            _onBackToMenu = null;
            base.OnClose();
        }

        private void StartRound()
        {
            CancelTimer();

            _score = 0;
            _remaining = RoundDuration;
            _isRoundRunning = true;

            if (_txtTitle != null) _txtTitle.text = "ClickCounter";
            if (_txtHint != null) _txtHint.text = "疯狂点击中央按钮";

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

            if (_txtTitle != null) _txtTitle.text = $"本局得分：{_score}";
            if (_txtHint != null) _txtHint.text = "点击重开继续挑战";
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
            StartRound();
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
