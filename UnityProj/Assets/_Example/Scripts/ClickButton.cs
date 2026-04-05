using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// Handles click/tap input and forwards to ClickGameManager.
    /// Demonstrates single-responsibility: this component ONLY handles input.
    /// </summary>
    public class ClickButton : MonoBehaviour
    {
        [SerializeField] private ClickGameManager _gameManager;

        /// <summary>
        /// Called by UI button or EventSystem click.
        /// In FairyGUI, wire this to a button's onClick event.
        /// </summary>
        public void OnButtonClicked()
        {
            _gameManager.OnClick();
        }
    }
}
