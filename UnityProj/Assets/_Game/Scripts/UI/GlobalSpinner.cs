using System.Threading.Tasks;
using FairyGUI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Global spinner / loading indicator — shows during network requests or scene transitions.
    /// Displays at LAYER_TOAST to stay above normal UI but below dialogs.
    /// Auto-hides after a configurable timeout to prevent stuck spinners.
    /// </summary>
    public class GlobalSpinner : MiniGameTemplate.UI.UIBase
    {
        protected override string PackageName => MiniGameTemplate.UI.UIConstants.PKG_COMMON;
        protected override string ComponentName => MiniGameTemplate.UI.UIConstants.COMP_GLOBAL_SPINNER;
        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_TOAST;

        private GTextField _txtHint;
        private float _timeout;
        private float _openTime;

        protected override void OnInit()
        {
            base.OnInit();
            _txtHint = ContentPane.GetChild("txtHint") as GTextField;
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            if (ContentPane != null)
                ContentPane.alpha = 1f;

            var spinnerData = data as GlobalSpinnerData;
            if (spinnerData != null)
            {
                if (_txtHint != null)
                    _txtHint.text = spinnerData.Message;
                _timeout = spinnerData.Timeout;
            }
            else
            {
                if (_txtHint != null)
                    _txtHint.text = "请稍候...";
                _timeout = DEFAULT_TIMEOUT;
            }

            _openTime = Time.unscaledTime;

            // Start timeout check via FairyGUI timer (WebGL-safe, no coroutine needed)
            Timers.inst.Add(0.5f, 0, CheckTimeout);
        }

        protected override void OnClose()
        {
            Timers.inst.Remove(CheckTimeout);
            base.OnClose();
        }

        private void CheckTimeout(object param)
        {
            if (_timeout > 0 && Time.unscaledTime - _openTime >= _timeout)
            {
                Debug.LogWarning($"[GlobalSpinner] Timed out after {_timeout}s — auto-hiding.");
                Hide();
            }
        }

        private const float DEFAULT_TIMEOUT = 15f;

        // === Static convenience API ===

        /// <summary>
        /// Show the global spinner with an optional message and timeout.
        /// </summary>
        public static async Task Show(string message = "请稍候...", float timeout = DEFAULT_TIMEOUT)
        {
            var data = new GlobalSpinnerData { Message = message, Timeout = timeout };
            await MiniGameTemplate.UI.UIManager.Instance.OpenPanelAsync<GlobalSpinner>(data);
        }

        /// <summary>
        /// Hide the global spinner if it's currently shown.
        /// </summary>
        public static void Hide()
        {
            MiniGameTemplate.UI.UIManager.Instance.ClosePanel<GlobalSpinner>();
        }
    }

    /// <summary>
    /// Data for configuring the GlobalSpinner.
    /// </summary>
    public class GlobalSpinnerData
    {
        public string Message = "请稍候...";
        public float Timeout = 15f;
    }
}
