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
    /// Optional interface for panels that need Suspend/Resume lifecycle hooks.
    /// When a Navigator Push hides this panel, OnSuspend() is called.
    /// When a Navigator Pop restores this panel, OnResume() is called.
    ///
    /// Panels that don't implement this interface will simply be hidden/shown
    /// without any lifecycle callback — which is fine for most static panels.
    ///
    /// Implement this when you need to:
    /// - Pause/resume animations or timers
    /// - Hide/show platform ads (banner, interstitial)
    /// - Unsubscribe/resubscribe from per-frame events
    /// - Refresh data when returning from a sub-flow
    /// </summary>
    public interface IPanelSuspendable
    {
        /// <summary>
        /// Called when the panel is being suspended (hidden but kept alive).
        /// Triggered by Navigator Push — a new node covers this panel.
        /// </summary>
        void OnSuspend();

        /// <summary>
        /// Called when the panel is being resumed after suspension.
        /// Triggered by Navigator Pop — the covering node is removed and this panel reappears.
        /// </summary>
        void OnResume(object data);
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
