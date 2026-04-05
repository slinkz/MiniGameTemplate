namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Constants for FairyGUI package names and component names.
    /// Eliminates magic strings throughout UI code.
    /// Update these when FairyGUI packages are renamed.
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

        // === Package names (match FairyGUI export names) ===
        // Example:
        // public const string PKG_COMMON = "Common";
        // public const string PKG_MAIN_MENU = "MainMenu";

        // === Component names ===
        // Example:
        // public const string COMP_MAIN_PANEL = "MainPanel";
        // public const string COMP_CONFIRM_DIALOG = "ConfirmDialog";
    }
}
