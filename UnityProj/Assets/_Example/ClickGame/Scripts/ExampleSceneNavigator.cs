using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Platform;
using MiniGameTemplate.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniGameTemplate.Example
{
    [System.Obsolete("Use AppFlowNavigator.Pop() instead. This class will be removed in a future version.")]
    public static class ExampleSceneNavigator
    {
        private const string BootSceneName = "Boot";
        private static bool _pendingOpenMainMenu;

        public static void ReturnToMainMenu()
        {
            _pendingOpenMainMenu = true;

            var danmakuSystem = Object.FindObjectOfType<DanmakuSystem>();
            if (danmakuSystem != null)
            {
                danmakuSystem.ClearAll();
                Object.Destroy(danmakuSystem.gameObject);
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(BootSceneName, LoadSceneMode.Single);
        }

        private static async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_pendingOpenMainMenu || scene.name != BootSceneName)
                return;

            _pendingOpenMainMenu = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            UIManager.RegisterBinder("MainMenu", MainMenu.MainMenuBinder.BindAll);

            var menuData = new MainMenu.MainMenuPanelData
            {
                StartGameEvent = null,
                WeChatBridge = WeChatBridgeFactory.Create(),
                EnableBannerAd = true
            };

            await UIManager.Instance.OpenPanelAsync<MainMenu.MainMenuPanel>(menuData);
        }
    }
}
