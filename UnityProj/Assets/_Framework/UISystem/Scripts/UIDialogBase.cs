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
                _modalOverlay.Dispose();
                _modalOverlay = null;
            }
            base.OnClose();
        }

        private void CreateModalOverlay()
        {
            _modalOverlay = new GGraph();
            _modalOverlay.MakeFullScreen();
            _modalOverlay.DrawRect(
                GRoot.inst.width, GRoot.inst.height,
                0, UnityEngine.Color.clear, new UnityEngine.Color(0, 0, 0, 0.6f));
            _modalOverlay.sortingOrder = SortOrder - 1;
            GRoot.inst.AddChild(_modalOverlay);

            if (CloseOnClickOutside)
            {
                _modalOverlay.onClick.Add(() => Close());
            }
        }
    }
}
