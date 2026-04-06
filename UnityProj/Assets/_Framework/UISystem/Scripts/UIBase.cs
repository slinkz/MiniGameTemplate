using FairyGUI;
using UnityEngine;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Base class for all UI panels.
    /// Subclass this for each game panel. Override lifecycle methods as needed.
    ///
    /// ALL panel loading is async via YooAsset — no Resources.Load fallback.
    /// Use OpenAsync() to open panels.
    /// </summary>
    public abstract class UIBase
    {
        public GComponent ContentPane { get; private set; }
        public bool IsVisible => ContentPane != null && ContentPane.visible;

        /// <summary>
        /// The FairyGUI package name this panel belongs to.
        /// </summary>
        protected abstract string PackageName { get; }

        /// <summary>
        /// The FairyGUI component name within the package.
        /// </summary>
        protected abstract string ComponentName { get; }

        /// <summary>
        /// Sort order layer for this panel.
        /// </summary>
        protected virtual int SortOrder => UIConstants.LAYER_NORMAL;

        /// <summary>
        /// [Async] Create and display the panel via YooAsset.
        /// This is the ONLY open path — no synchronous Resources.Load fallback.
        /// </summary>
        public async System.Threading.Tasks.Task OpenAsync(object data = null)
        {
            if (ContentPane != null)
            {
                OnRefresh(data);
                return;
            }

            await UIPackageLoader.AddPackageAsync(PackageName);
            CreateAndShow(data);
        }

        /// <summary>
        /// Shared creation logic after package is loaded.
        /// </summary>
        private void CreateAndShow(object data)
        {
            ContentPane = UIPackage.CreateObject(PackageName, ComponentName).asCom;

            if (ContentPane == null)
            {
                Debug.LogError($"[UIBase] Failed to create: {PackageName}/{ComponentName}");
                return;
            }

            ContentPane.sortingOrder = SortOrder;
            ContentPane.MakeFullScreen();
            GRoot.inst.AddChild(ContentPane);

            OnInit();
            OnOpen(data);
        }

        /// <summary>
        /// Hide and destroy the panel.
        /// </summary>
        public void Close()
        {
            if (ContentPane == null) return;

            OnClose();
            ContentPane.Dispose();
            ContentPane = null;
            UIPackageLoader.RemovePackage(PackageName);
        }

        /// <summary>
        /// Called once when the panel is first created.
        /// Use for binding UI elements to fields.
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// Called each time the panel is opened/shown.
        /// </summary>
        protected virtual void OnOpen(object data) { }

        /// <summary>
        /// Called when the panel is about to close.
        /// </summary>
        protected virtual void OnClose() { }

        /// <summary>
        /// Called when the panel is already open and OpenAsync() is called again.
        /// Use for refreshing data without recreating the panel.
        /// </summary>
        protected virtual void OnRefresh(object data) { }
    }
}
