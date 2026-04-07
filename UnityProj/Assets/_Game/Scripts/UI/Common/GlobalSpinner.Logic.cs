using System.Threading.Tasks;
using FairyGUI;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// Data for configuring the GlobalSpinner.
    /// </summary>
    public class GlobalSpinnerData
    {
        public string Message = "请稍候...";
        public float Timeout = 15f;
    }

    /// <summary>
    /// Global spinner / loading indicator — shows during network requests or scene transitions.
    /// Displays at LAYER_TOAST to stay above normal UI but below dialogs.
    /// Auto-hides after a configurable timeout to prevent stuck spinners.
    /// </summary>
    public partial class GlobalSpinner : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_TOAST;
        public bool IsFullScreen => true;
        public string PanelPackageName => "Common";

        private const float DEFAULT_TIMEOUT = 15f;

        private float _timeout;
        private float _openTime;

        public void OnOpen(object data)
        {
            ApplyData(data);
        }

        public void OnClose()
        {
            Timers.inst.Remove(CheckTimeout);
        }

        public void OnRefresh(object data)
        {
            // Only update data — do NOT re-setup timer from scratch without cleanup
            Timers.inst.Remove(CheckTimeout);
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            alpha = 1f;

            var spinnerData = data as GlobalSpinnerData;
            if (spinnerData != null)
            {
                if (txtHint != null)
                    txtHint.text = spinnerData.Message;
                _timeout = spinnerData.Timeout;
            }
            else
            {
                if (txtHint != null)
                    txtHint.text = "请稍候...";
                _timeout = DEFAULT_TIMEOUT;
            }

            _openTime = Time.unscaledTime;

            // Start timeout check via FairyGUI timer (WebGL-safe, no coroutine needed)
            Timers.inst.Add(0.5f, 0, CheckTimeout);
        }

        private void CheckTimeout(object param)
        {
            if (_timeout > 0 && Time.unscaledTime - _openTime >= _timeout)
            {
                Debug.LogWarning($"[GlobalSpinner] Timed out after {_timeout}s — auto-hiding.");
                Hide();
            }
        }

        // === Static convenience API ===

        /// <summary>
        /// Show the global spinner with an optional message and timeout.
        /// </summary>
        public static async Task Show(string message = "请稍候...", float timeout = DEFAULT_TIMEOUT)
        {
            var data = new GlobalSpinnerData { Message = message, Timeout = timeout };
            await UIManager.Instance.OpenPanelAsync<GlobalSpinner>(data);
        }

        /// <summary>
        /// Hide the global spinner if it's currently shown.
        /// </summary>
        public static void Hide()
        {
            UIManager.Instance.ClosePanel<GlobalSpinner>();
        }
    }
}
