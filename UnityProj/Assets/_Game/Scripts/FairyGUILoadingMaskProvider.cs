using FairyGUI;
using UnityEngine;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;

namespace Game
{
    /// <summary>
    /// FairyGUI-based implementation of <see cref="ILoadingMaskProvider"/>.
    ///
    /// Creates a full-screen semi-transparent black overlay with a centered message label.
    /// Built entirely in code — no FairyGUI package asset required.
    ///
    /// Layer order:
    ///   - Added to GRoot at a high sortingOrder so it covers all normal panels.
    ///   - Lower than ConfirmDialog (retry dialog) so retry can appear on top.
    ///
    /// Usage: injected at startup via <c>LoadingMaskService.SetProvider(new FairyGUILoadingMaskProvider());</c>
    /// </summary>
    public class FairyGUILoadingMaskProvider : ILoadingMaskProvider
    {
        private const int SORT_ORDER = 9000; // Must NOT coexist with ConfirmDialog — NetworkRetryService handles mutual exclusion.

        private GComponent _mask;
        private GTextField _label;
        private GGraph _spinner;
        private GTweener _spinTween;

        public void Show(string message)
        {
            if (_mask != null)
            {
                // Already showing — just update text
                UpdateMessage(message);
                return;
            }

            BuildMask(message);
            GRoot.inst.AddChild(_mask);
            _mask.sortingOrder = SORT_ORDER;

            GameLog.Log("[LoadingMask] Shown: " + message);
        }

        public void UpdateMessage(string message)
        {
            if (_label != null)
            {
                _label.text = message ?? "";
            }
        }

        public void Hide()
        {
            if (_mask == null) return;

            StopSpinner();

            if (_mask.parent != null)
            {
                _mask.parent.RemoveChild(_mask);
            }
            _mask.Dispose();
            _mask = null;
            _label = null;
            _spinner = null;

            GameLog.Log("[LoadingMask] Hidden.");
        }

        private void BuildMask(string message)
        {
            _mask = new GComponent();
            _mask.gameObjectName = "__LoadingMask__";
            _mask.SetSize(GRoot.inst.width, GRoot.inst.height);
            _mask.AddRelation(GRoot.inst, RelationType.Size);

            // Opaque enough to signal "blocked" but still see the game behind
            _mask.opaque = true;  // Blocks all touch/click input

            // Semi-transparent black background
            var bg = new GGraph();
            bg.SetSize(_mask.width, _mask.height);
            bg.AddRelation(_mask, RelationType.Size);
            bg.DrawRect(bg.width, bg.height, 0, Color.clear, new Color(0f, 0f, 0f, 0.6f));
            _mask.AddChild(bg);

            // Spinner (simple rotating square — visual placeholder)
            _spinner = new GGraph();
            _spinner.SetSize(40, 40);
            _spinner.DrawRect(40, 40, 0, Color.clear, Color.white);
            _spinner.SetPivot(0.5f, 0.5f, true);
            _spinner.SetXY((_mask.width - 40) / 2f, _mask.height / 2f - 60);
            _spinner.AddRelation(_mask, RelationType.Center_Center);
            _mask.AddChild(_spinner);
            StartSpinner();

            // Message label
            _label = new GTextField();
            _label.SetSize(_mask.width, 40);
            _label.SetXY(0, _mask.height / 2f);
            _label.AddRelation(_mask, RelationType.Center_Center);
            _label.align = AlignType.Center;
            _label.verticalAlign = VertAlignType.Middle;

            var format = _label.textFormat;
            format.color = Color.white;
            format.size = 28;
            _label.textFormat = format;

            _label.text = message ?? "";
            _mask.AddChild(_label);
        }

        private void StartSpinner()
        {
            if (_spinner == null) return;
            _spinTween = _spinner.TweenRotate(360f, 1.2f).SetEase(EaseType.Linear);
            _spinTween.SetRepeat(-1); // Infinite loop
        }

        private void StopSpinner()
        {
            if (_spinTween != null)
            {
                _spinTween.Kill();
                _spinTween = null;
            }
        }
    }
}
