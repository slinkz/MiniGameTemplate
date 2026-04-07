namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Constants for UI layer sorting.
    /// Use these in IUIPanel.PanelSortOrder implementations.
    ///
    /// Package names and component names are no longer centralized here —
    /// they are encoded in FairyGUI-exported classes (URL, CreateInstance, namespace).
    /// </summary>
    public static class UIConstants
    {
        // === Layer sort orders ===
        public const int LAYER_BACKGROUND = 0;
        public const int LAYER_NORMAL = 100;
        public const int LAYER_POPUP = 200;
        public const int LAYER_DIALOG = 300;
        public const int LAYER_TOAST = 400;
        public const int LAYER_GUIDE = 500;
        public const int LAYER_LOADING = 600;
    }
}
