using System;
using System.Threading.Tasks;
using FairyGUI;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// Privacy authorization dialog — required by WeChat for first launch or policy updates.
    /// Modal popup that prompts the user to agree or reject the privacy policy.
    /// </summary>
    public partial class PrivacyDialog : IUIPanel, IModalDialog
    {
        public int PanelSortOrder => UIConstants.LAYER_LOADING + 100;
        public bool IsFullScreen => false;
        public string PanelPackageName => "Common";
        public bool CloseOnClickOutside => false;

        private Action<bool> _onResult;

        public void OnOpen(object data)
        {
            // Bind button events (only in OnOpen — never re-bind)
            if (btnAgree != null) btnAgree.onClick.Add(OnAgreeClicked);
            if (btnReject != null) btnReject.onClick.Add(OnRejectClicked);
            if (txtPrivacyLink != null) txtPrivacyLink.onClick.Add(OnPrivacyLinkClicked);

            ApplyData(data);
        }

        public void OnClose()
        {
            // Safety net: if dialog is closed without button press (e.g. CloseAllPanels),
            // invoke callback with false so any awaiting Task completes instead of hanging.
            var pendingCallback = _onResult;
            _onResult = null;
            pendingCallback?.Invoke(false);
        }

        public void OnRefresh(object data)
        {
            // Only update data — do NOT re-bind events
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            _onResult = data as Action<bool>;

            if (_onResult == null)
            {
                Debug.LogWarning("[PrivacyDialog] OnOpen/OnRefresh called without Action<bool> callback.");
            }
        }

        private void OnAgreeClicked()
        {
            var callback = _onResult;
            _onResult = null; // Clear before Close to prevent double-invoke from OnClose
            UIManager.Instance.ClosePanel<PrivacyDialog>();
            callback?.Invoke(true);
        }

        private void OnRejectClicked()
        {
            var callback = _onResult;
            _onResult = null;
            UIManager.Instance.ClosePanel<PrivacyDialog>();
            callback?.Invoke(false);
        }

        private void OnPrivacyLinkClicked()
        {
            GameLog.Log("[PrivacyDialog] Privacy link clicked — would open privacy contract.");
        }

        /// <summary>
        /// Convenience method: open the dialog and return a Task that resolves with the user's choice.
        /// true = agreed, false = rejected.
        /// Safe against load failures and unexpected close paths.
        /// </summary>
        public static async Task<bool> ShowAndWaitAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            Action<bool> callback = result => tcs.TrySetResult(result);

            try
            {
                await UIManager.Instance.OpenPanelAsync<PrivacyDialog>(callback);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PrivacyDialog] Failed to open: {ex.Message}");
                tcs.TrySetResult(false);
            }

            return await tcs.Task;
        }
    }
}
