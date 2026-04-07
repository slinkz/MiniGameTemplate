using System;
using System.Threading.Tasks;

namespace Game.UI
{
    /// <summary>
    /// Privacy authorization dialog — required by WeChat for first launch or policy updates.
    /// Modal popup that prompts the user to agree or reject the privacy policy.
    /// </summary>
    public partial class PrivacyDialog
    {
        protected override bool CloseOnClickOutside => false;

        // Must be above LAYER_LOADING (600) so the dialog is visible over the loading screen.
        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_LOADING + 100;

        private Action<bool> _onResult;

        protected void AddEvents()

        {
            if (_btnAgree != null) _btnAgree.onClick.Add(OnAgreeClicked);
            if (_btnReject != null) _btnReject.onClick.Add(OnRejectClicked);
            if (_txtPrivacyLink != null) _txtPrivacyLink.onClick.Add(OnPrivacyLinkClicked);
        }


        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            _onResult = data as Action<bool>;

            if (_onResult == null)
            {
                UnityEngine.Debug.LogWarning("[PrivacyDialog] OnOpen called without Action<bool> callback.");
            }
        }

        protected override void OnClose()
        {
            // Safety net: if dialog is closed without button press (e.g. CloseAllPanels),
            // invoke callback with false so any awaiting Task completes instead of hanging.
            var pendingCallback = _onResult;
            _onResult = null;
            pendingCallback?.Invoke(false);

            base.OnClose();
        }

        private void OnAgreeClicked()
        {
            var callback = _onResult;
            _onResult = null; // Clear before Close to prevent double-invoke from OnClose
            Close();
            callback?.Invoke(true);
        }

        private void OnRejectClicked()
        {
            var callback = _onResult;
            _onResult = null;
            Close();
            callback?.Invoke(false);
        }

        private void OnPrivacyLinkClicked()
        {
            // In real WeChat environment, this would call wx.openPrivacyContract()
            MiniGameTemplate.Utils.GameLog.Log("[PrivacyDialog] Privacy link clicked — would open privacy contract.");
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
                await MiniGameTemplate.UI.UIManager.Instance.OpenPanelAsync<PrivacyDialog>(callback);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[PrivacyDialog] Failed to open: {ex.Message}");
                tcs.TrySetResult(false);
            }

            return await tcs.Task;
        }
    }
}
