using System;
using FairyGUI;
using MiniGameTemplate.UI;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// Data passed to ConfirmDialog when opening.
    /// </summary>
    public class ConfirmDialogData
    {
        public string Title = "提示";
        public string Content = "";
        public string ConfirmText = "确认";
        public string CancelText = "取消";
        public Action OnConfirm;
        public Action OnCancel;
        public bool ShowCancel = true;
    }

    /// <summary>
    /// Generic confirm dialog — modal popup with title, content, and one or two buttons.
    /// Reusable across all game features.
    /// </summary>
    public partial class ConfirmDialog : IUIPanel, IModalDialog
    {
        public int PanelSortOrder => UIConstants.LAYER_LOADING + 100;
        public bool IsFullScreen => false;
        public string PanelPackageName => "Common";
        public bool CloseOnClickOutside => false;

        private Action _onConfirm;
        private Action _onCancel;

        public void OnOpen(object data)
        {
            // Bind button events (only in OnOpen — never re-bind)
            if (btnConfirm != null) btnConfirm.onClick.Add(OnConfirmClicked);
            if (btnCancel != null) btnCancel.onClick.Add(OnCancelClicked);

            ApplyData(data);
        }

        public void OnClose()
        {
            // Safety net: if dialog closes by external path (e.g. CloseAllPanels),
            // treat it as cancel so awaiting logic can always complete.
            var pendingCancel = _onCancel;
            _onConfirm = null;
            _onCancel = null;
            pendingCancel?.Invoke();
        }

        public void OnRefresh(object data)
        {
            // Only update data — do NOT re-bind events
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            var dialogData = data as ConfirmDialogData;
            if (dialogData == null)
            {
                Debug.LogError("[ConfirmDialog] OnOpen/OnRefresh called with null or invalid data. Dialog will be non-functional.");
                return;
            }

            _onConfirm = dialogData.OnConfirm;
            _onCancel = dialogData.OnCancel;

            if (txtTitle != null) txtTitle.text = dialogData.Title;
            if (txtContent != null) txtContent.text = dialogData.Content;
            if (btnConfirm != null) btnConfirm.title = dialogData.ConfirmText;
            if (btnCancel != null)
            {
                btnCancel.title = dialogData.CancelText;
                btnCancel.visible = dialogData.ShowCancel;
            }

            // Center confirm button when cancel is hidden
            if (!dialogData.ShowCancel && btnConfirm != null)
            {
                btnConfirm.x = (this.width - btnConfirm.width) / 2;
            }
        }

        private void OnConfirmClicked()
        {
            var callback = _onConfirm;
            _onConfirm = null;
            _onCancel = null;
            UIManager.Instance.ClosePanel<ConfirmDialog>();
            callback?.Invoke();
        }

        private void OnCancelClicked()
        {
            var callback = _onCancel;
            _onConfirm = null;
            _onCancel = null;
            UIManager.Instance.ClosePanel<ConfirmDialog>();
            callback?.Invoke();
        }
    }
}
