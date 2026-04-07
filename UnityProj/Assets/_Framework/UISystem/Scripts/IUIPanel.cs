using FairyGUI;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Interface for UI panels managed by UIManager.
    /// Implement this on FairyGUI-exported partial classes to enable lifecycle management.
    ///
    /// Usage: Your FairyGUI-exported class (e.g. LoadingPanel : GComponent) already has
    /// field bindings and ConstructFromXML. Add a partial class implementing IUIPanel
    /// to provide lifecycle hooks and panel configuration.
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// Layer sort order. Use UIConstants.LAYER_* values.
        /// </summary>
        int PanelSortOrder { get; }

        /// <summary>
        /// Whether this panel is made full-screen when opened.
        /// Return false for dialogs/popups that keep their original size and are centered.
        /// </summary>
        bool IsFullScreen { get; }

        /// <summary>
        /// The FairyGUI package name this panel belongs to (e.g. "Common", "MainMenu").
        /// Used by UIManager for async package loading and ref-count management.
        /// </summary>
        string PanelPackageName { get; }

        /// <summary>
        /// Called after the panel is created and added to GRoot.
        /// Use for initializing state and binding events.
        /// </summary>
        void OnOpen(object data);

        /// <summary>
        /// Called before the panel is disposed.
        /// Use for cleanup, unsubscribing events, cancelling timers.
        /// </summary>
        void OnClose();

        /// <summary>
        /// Called when panel is already open and OpenPanelAsync is called again.
        /// Use for refreshing data without recreating the panel.
        /// </summary>
        void OnRefresh(object data);
    }

    /// <summary>
    /// Extension interface for dialog/popup panels that need a modal overlay.
    /// UIManager automatically creates a semi-transparent overlay behind the dialog
    /// and optionally closes the dialog when the overlay is clicked.
    /// </summary>
    public interface IModalDialog
    {
        /// <summary>
        /// Whether clicking the modal overlay closes this dialog.
        /// </summary>
        bool CloseOnClickOutside { get; }
    }
}
