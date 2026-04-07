using System.Threading.Tasks;
using FairyGUI;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// Loading panel — the first UI shown during game boot.
    /// Displays a progress bar driven by the loading pipeline.
    /// Belongs to Common package so it can be loaded with minimal overhead.
    /// </summary>
    public partial class LoadingPanel : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_LOADING;
        public bool IsFullScreen => true;
        public string PanelPackageName => "Common";

        public void OnOpen(object data)
        {
            // Reset alpha in case a previous FadeOutAndCloseAsync set it to 0
            alpha = 1f;
            UpdateProgress(0f);
        }

        public void OnClose() { }

        public void OnRefresh(object data)
        {
            alpha = 1f;
        }

        /// <summary>
        /// Set loading progress [0..1]. Drives the progress bar and percentage text.
        /// </summary>
        public void UpdateProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (progressBar != null)
            {
                progressBar.value = progress * 100;
                var percentText = progressBar.GetChild("title")?.asTextField;
                if (percentText != null)
                    percentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
        }

        /// <summary>
        /// Set the hint text below the progress bar.
        /// </summary>
        public void SetHintText(string hint)
        {
            if (txtHint != null)
                txtHint.text = hint;
        }

        /// <summary>
        /// Fade out and close the panel. Returns a Task that completes when the fade is done.
        /// Uses FairyGUI GTween for WebGL-safe animation.
        /// Includes timeout protection to prevent async deadlock if tween fails.
        /// </summary>
        public async Task FadeOutAndCloseAsync(float duration = 0.3f)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.TweenFade(0f, duration).OnComplete(() =>
            {
                tcs.TrySetResult(true);
            });

            // Timeout protection: if tween doesn't complete within duration + 1s, force proceed.
            var timeoutMs = (int)((duration + 1f) * 1000);
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

            if (completed != tcs.Task)
            {
                Debug.LogWarning("[LoadingPanel] FadeOut tween timed out — forcing close.");
                tcs.TrySetResult(false);
            }

            UIManager.Instance.ClosePanel<LoadingPanel>();
        }
    }
}
