using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Central UI panel manager. Handles opening, closing, and tracking active panels.
    /// Uses Singleton pattern (framework-internal only).
    ///
    /// ALL panel opening is async — no synchronous Resources.Load path.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private readonly Dictionary<Type, UIBase> _activePanels = new Dictionary<Type, UIBase>();
        private List<UIBase> _closeBuffer; // Reusable buffer for CloseAllPanels — avoids GC

        /// <summary>
        /// Open a panel of type T asynchronously via YooAsset.
        /// Creates it if not already open, refreshes if already open.
        /// </summary>
        public async System.Threading.Tasks.Task<T> OpenPanelAsync<T>(object data = null) where T : UIBase, new()
        {
            var type = typeof(T);

            if (_activePanels.TryGetValue(type, out var existing))
            {
                await existing.OpenAsync(data); // Will call OnRefresh
                return (T)existing;
            }

            var panel = new T();
            _activePanels[type] = panel;
            await panel.OpenAsync(data);
            return panel;
        }

        /// <summary>
        /// Close a panel of type T.
        /// </summary>
        public void ClosePanel<T>() where T : UIBase
        {
            var type = typeof(T);
            if (_activePanels.TryGetValue(type, out var panel))
            {
                panel.Close();
                _activePanels.Remove(type);
            }
        }

        /// <summary>
        /// Check if a panel type is currently open.
        /// </summary>
        public bool IsPanelOpen<T>() where T : UIBase
        {
            return _activePanels.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get an active panel instance, or null if not open.
        /// </summary>
        public T GetPanel<T>() where T : UIBase
        {
            _activePanels.TryGetValue(typeof(T), out var panel);
            return panel as T;
        }

        /// <summary>
        /// Close all open panels. Call on scene transition.
        /// Snapshot-then-clear pattern avoids InvalidOperationException
        /// if panel.Close() triggers further dictionary modifications.
        /// </summary>
        public void CloseAllPanels()
        {
            if (_activePanels.Count == 0) return;

            // Snapshot values before clearing to avoid iterator invalidation
            if (_closeBuffer == null)
                _closeBuffer = new List<UIBase>(_activePanels.Count);
            else
                _closeBuffer.Clear();

            foreach (var panel in _activePanels.Values)
                _closeBuffer.Add(panel);

            _activePanels.Clear();

            foreach (var panel in _closeBuffer)
                panel.Close();

            _closeBuffer.Clear();
        }
    }
}
