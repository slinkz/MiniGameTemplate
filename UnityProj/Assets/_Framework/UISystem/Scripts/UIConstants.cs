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
        public const string PKG_COMMON = "Common";
        public const string PKG_MAIN_MENU = "MainMenu";

        // === Component names (match FairyGUI component names) ===
        public const string COMP_LOADING_PANEL = "LoadingPanel";
        public const string COMP_CONFIRM_DIALOG = "ConfirmDialog";
        public const string COMP_PRIVACY_DIALOG = "PrivacyDialog";
        public const string COMP_GLOBAL_SPINNER = "GlobalSpinner";
        public const string COMP_MAIN_MENU_PANEL = "MainMenuPanel";
        public const string COMP_CLICK_COUNTER_PANEL = "ClickCounterPanel";

    }
}
