using System.Threading.Tasks;
using FairyGUI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Loading panel — the first UI shown during game boot.
    /// Displays a progress bar driven by the loading pipeline.
    ///
    /// Belongs to Common package so it can be loaded with minimal overhead.
    /// </summary>
    public class LoadingPanel : MiniGameTemplate.UI.UIBase
    {
        protected override string PackageName => MiniGameTemplate.UI.UIConstants.PKG_COMMON;
        protected override string ComponentName => MiniGameTemplate.UI.UIConstants.COMP_LOADING_PANEL;
        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_LOADING;

        private GProgressBar _progressBar;
        private GTextField _txtPercent;
        private GTextField _txtHint;
        private GTextField _txtTitle;

        protected override void OnInit()
        {
            base.OnInit();
            _progressBar = ContentPane.GetChild("progressBar") as GProgressBar;
            _txtPercent = _progressBar?.GetChild("title")?.asTextField;
            _txtHint = ContentPane.GetChild("txtHint") as GTextField;
            _txtTitle = ContentPane.GetChild("txtTitle") as GTextField;
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            // Reset alpha in case a previous FadeOutAndCloseAsync set it to 0
            if (ContentPane != null)
                ContentPane.alpha = 1f;
            UpdateProgress(0f);
        }

        /// <summary>
        /// Set loading progress [0..1]. Drives the progress bar and percentage text.
        /// </summary>
        public void UpdateProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (_progressBar != null)
                _progressBar.value = progress * 100;
            if (_txtPercent != null)
                _txtPercent.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        /// <summary>
        /// Set the hint text below the progress bar.
        /// </summary>
        public void SetHintText(string hint)
        {
            if (_txtHint != null)
                _txtHint.text = hint;
        }

        /// <summary>
        /// Fade out and close the panel. Returns a Task that completes when the fade is done.
        /// Uses FairyGUI GTween for WebGL-safe animation.
        /// Includes timeout protection to prevent async deadlock if tween fails.
        /// </summary>
        public async Task FadeOutAndCloseAsync(float duration = 0.3f)
        {
            if (ContentPane == null)
                return;

            var tcs = new TaskCompletionSource<bool>();

            ContentPane.TweenFade(0f, duration).OnComplete(() =>
            {
                tcs.TrySetResult(true);
            });

            // Timeout protection: if tween doesn't complete within duration + 1s, force proceed.
            // This prevents async flow from hanging if the tween engine fails silently.
            var timeoutMs = (int)((duration + 1f) * 1000);
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

            if (completed != tcs.Task)
            {
                Debug.LogWarning("[LoadingPanel] FadeOut tween timed out — forcing close.");
                tcs.TrySetResult(false);
            }

            Close();
        }
    }
}
