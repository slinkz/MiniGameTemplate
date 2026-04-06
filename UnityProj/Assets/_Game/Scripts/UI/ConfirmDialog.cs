using System;
using FairyGUI;

namespace Game.UI
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
    public class ConfirmDialog : MiniGameTemplate.UI.UIDialogBase
    {
        protected override string PackageName => MiniGameTemplate.UI.UIConstants.PKG_COMMON;
        protected override string ComponentName => MiniGameTemplate.UI.UIConstants.COMP_CONFIRM_DIALOG;
        protected override bool CloseOnClickOutside => false;

        // Must be above LAYER_LOADING (600) so the dialog is visible when invoked during startup.
        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_LOADING + 100;

        private GTextField _txtTitle;
        private GTextField _txtContent;
        private GButton _btnConfirm;
        private GButton _btnCancel;

        private Action _onConfirm;
        private Action _onCancel;

        protected override void OnInit()
        {
            base.OnInit();
            _txtTitle = ContentPane.GetChild("txtTitle") as GTextField;
            _txtContent = ContentPane.GetChild("txtContent") as GTextField;
            _btnConfirm = ContentPane.GetChild("btnConfirm") as GButton;
            _btnCancel = ContentPane.GetChild("btnCancel") as GButton;

            if (_btnConfirm != null)
                _btnConfirm.onClick.Add(OnConfirmClicked);
            if (_btnCancel != null)
                _btnCancel.onClick.Add(OnCancelClicked);
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            var dialogData = data as ConfirmDialogData;
            if (dialogData == null)
            {
                UnityEngine.Debug.LogError("[ConfirmDialog] OnOpen called with null or invalid data. Closing dialog.");
                Close();
                return;
            }

            _onConfirm = dialogData.OnConfirm;
            _onCancel = dialogData.OnCancel;

            if (_txtTitle != null) _txtTitle.text = dialogData.Title;
            if (_txtContent != null) _txtContent.text = dialogData.Content;
            if (_btnConfirm != null) _btnConfirm.title = dialogData.ConfirmText;
            if (_btnCancel != null)
            {
                _btnCancel.title = dialogData.CancelText;
                _btnCancel.visible = dialogData.ShowCancel;
            }

            // Center confirm button when cancel is hidden
            if (!dialogData.ShowCancel && _btnConfirm != null)
            {
                _btnConfirm.x = (ContentPane.width - _btnConfirm.width) / 2;
            }
        }

        protected override void OnClose()
        {
            _onConfirm = null;
            _onCancel = null;
            base.OnClose();
        }

        private void OnConfirmClicked()
        {
            var callback = _onConfirm;
            Close();
            callback?.Invoke();
        }

        private void OnCancelClicked()
        {
            var callback = _onCancel;
            Close();
            callback?.Invoke();
        }
    }
}
