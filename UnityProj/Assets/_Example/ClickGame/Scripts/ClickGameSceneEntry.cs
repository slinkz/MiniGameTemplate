using MiniGameTemplate.Platform;
using MiniGameTemplate.UI;
using UnityEngine;

namespace MiniGameTemplate.Example
{
    public class ClickGameSceneEntry : MonoBehaviour
    {
        private bool _opened;

        private async void Start()
        {
            UIManager.RegisterBinder("ClickGame", global::ClickGame.ClickGameBinder.BindAll);


            if (_opened)
                return;

            _opened = true;
            var data = new global::ClickGame.ClickCounterPanelData

            {
                WeChatBridge = WeChatBridgeFactory.Create(),
                OnBackToMenu = ExampleSceneNavigator.ReturnToMainMenu
            };

            await UIManager.Instance.OpenPanelAsync<global::ClickGame.ClickCounterPanel>(data);

        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ExampleSceneNavigator.ReturnToMainMenu();
        }
    }
}
