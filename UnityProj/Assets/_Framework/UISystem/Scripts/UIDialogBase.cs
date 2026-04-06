using FairyGUI;
using UnityEngine;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Base class for dialog/popup panels.
    /// Adds modal background overlay and close-on-click-outside support.
    /// </summary>
    public abstract class UIDialogBase : UIBase
    {
        protected override int SortOrder => UIConstants.LAYER_DIALOG;
        protected override bool IsFullScreen => false; // Dialogs keep their original size and are centered

        /// <summary>
        /// Whether clicking the modal overlay closes this dialog.
        /// </summary>
        protected virtual bool CloseOnClickOutside => true;

        private GGraph _modalOverlay;

        protected override void OnInit()
        {
            base.OnInit();
            CreateModalOverlay();
        }

        protected override void OnClose()
        {
            if (_modalOverlay != null)
            {
                // GObject.Dispose() internally calls RemoveFromParent(),
                // so the overlay is automatically removed from GRoot.
                _modalOverlay.Dispose();
                _modalOverlay = null;
            }
            base.OnClose();
        }

        private void CreateModalOverlay()
        {
            _modalOverlay = new GGraph();
            _modalOverlay.SetSize(GRoot.inst.width, GRoot.inst.height);
            _modalOverlay.DrawRect(
                _modalOverlay.width, _modalOverlay.height,
                0, UnityEngine.Color.clear, new UnityEngine.Color(0, 0, 0, 0.6f));

            // Bind overlay size to GRoot so it stays full-screen on resize/orientation change
            _modalOverlay.AddRelation(GRoot.inst, FairyGUI.RelationType.Size);

            _modalOverlay.sortingOrder = SortOrder - 1;
            GRoot.inst.AddChild(_modalOverlay);

            if (CloseOnClickOutside)
            {
                _modalOverlay.onClick.Add(() => Close());
            }
        }
    }
}
