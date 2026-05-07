using System.Threading.Tasks;
using MiniGameTemplate.Core;
using MiniGameTemplate.Events;
using MiniGameTemplate.Navigation;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    /// <summary>
    /// Data passed to MainMenuPanel when opening.
    /// Implements IFlowData for AppFlowNavigator integration.
    /// </summary>
    [System.Serializable]
    public class MainMenuPanelData : IFlowData
    {
        [System.NonSerialized] public GameEvent StartGameEvent;
        [System.NonSerialized] public IWeChatBridge WeChatBridge;
        public bool EnableBannerAd = true;

        public override string ToString() => "MainMenuPanelData";
    }

    /// <summary>
    /// Main menu / lobby panel — the player's hub after loading completes.
    /// Serves as the central navigation hub for all game modules:
    ///   - "弹幕射击" → opens SG_LevelSelect
    ///   - Demo entries → load demo scenes
    ///   - Future: 养成、商店等模块入口
    /// </summary>
    public partial class MainMenuPanel : IUIPanel, IPanelSuspendable
    {
        /// <summary>面板注册表 Key（FlowNodeSO._panelTypeName 配这个值）。</summary>
        public const string PanelKey = "MainMenuPanel";

        public int PanelSortOrder => UIConstants.LAYER_NORMAL;
        public bool IsFullScreen => true;
        public string PanelPackageName => "MainMenu";

        /// <summary>
        /// 面板自注册（PK UA-003：分散注册消除编译耦合）。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSelf()
        {
            AppFlowNavigator.Instance.RegisterPanelOpener(PanelKey, async (data) =>
            {
                await UIManager.Instance.OpenPanelAsync<MainMenuPanel>(data);
            });
        }

        private IWeChatBridge _weChatBridge;
        private bool _enableBannerAd = true;

        public void OnOpen(object data)
        {
            // Bind button events (only in OnOpen — never re-bind)
            if (btnShooterGame != null) btnShooterGame.onClick.Add(OnShooterGameClicked);
            if (btnClickGame != null) btnClickGame.onClick.Add(OnClickGameClicked);
            if (btnDanmakuDemo != null) btnDanmakuDemo.onClick.Add(OnDanmukuDemoClicked);
            if (btnVFXDemo != null) btnVFXDemo.onClick.Add(OnVFXDemoClicked);
            if (btnSettings != null) btnSettings.onClick.Add(OnSettingsClicked);
            if (btnRanking != null) btnRanking.onClick.Add(OnRankingClicked);
            if (btnShare != null) btnShare.onClick.Add(OnShareClicked);

            ApplyData(data);
        }

        public void OnClose()
        {
            if (btnShooterGame != null) btnShooterGame.onClick.Remove(OnShooterGameClicked);
            if (btnClickGame != null) btnClickGame.onClick.Remove(OnClickGameClicked);
            if (btnDanmakuDemo != null) btnDanmakuDemo.onClick.Remove(OnDanmukuDemoClicked);
            if (btnVFXDemo != null) btnVFXDemo.onClick.Remove(OnVFXDemoClicked);
            if (btnSettings != null) btnSettings.onClick.Remove(OnSettingsClicked);
            if (btnRanking != null) btnRanking.onClick.Remove(OnRankingClicked);
            if (btnShare != null) btnShare.onClick.Remove(OnShareClicked);

            if (_enableBannerAd)
                _weChatBridge?.HideBannerAd();

            _weChatBridge = null;
        }


        public void OnRefresh(object data)
        {
            // Only update data — do NOT re-bind events
            ApplyData(data);
        }

        public void OnSuspend()
        {
            // Hide banner ad when another node covers us
            if (_enableBannerAd)
                _weChatBridge?.HideBannerAd();
        }

        public void OnResume(object data)
        {
            // Restore banner ad + refresh data when returning from sub-flow
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            var menuData = data as MainMenuPanelData;

            if (menuData != null)
            {
                _weChatBridge = menuData.WeChatBridge;
                _enableBannerAd = menuData.EnableBannerAd;
            }
            else
            {
                _enableBannerAd = true;
            }

            EnterMenuState();
        }

        private async void OnShooterGameClicked()
        {
            try
            {
                // 初始化 ShooterGame 进度管理器（幂等调用）
                Game.ShooterGame.SG_Boot.InitProgress();

                // 通过 Navigator Push 到选关界面
                var levelSelectNode = Game.ShooterGame.SG_FlowNodes.NodeLevelSelect;
                if (levelSelectNode != null)
                {
                    await AppFlowNavigator.Instance.PushAsync(levelSelectNode);
                }
                else
                {
                    // Fallback（旧路径）
                    UIManager.Instance.ClosePanel<MainMenuPanel>();
                    await UIManager.Instance.OpenPanelAsync<SG_LevelSelect.LevelSelectScreen>();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnClickGameClicked()
        {
            UIManager.Instance.ClosePanel<MainMenuPanel>();
            SceneManager.LoadScene("ClickGame");
        }

        private void OnDanmukuDemoClicked()
        {
            UIManager.Instance.ClosePanel<MainMenuPanel>();
            SceneManager.LoadScene("DanmakuDemo");
        }

        private void OnVFXDemoClicked()
        {
            UIManager.Instance.ClosePanel<MainMenuPanel>();
            SceneManager.LoadScene("VFXDemo");
        }

        private void OnSettingsClicked()
        {
            GameLog.Log("[MainMenuPanel] Settings button clicked (not yet implemented).");
        }

        private void OnRankingClicked()
        {
            _weChatBridge?.ShowRankingPanel();
        }

        private void OnShareClicked()
        {
            _weChatBridge?.Share("来和我一起玩吧！", "", "");
        }

        private void EnterMenuState()
        {
            if (_enableBannerAd)
                _weChatBridge?.ShowBannerAd();
        }
    }
}
