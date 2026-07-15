using System;
using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 失败结算面板 Controller（V2 TDD_05 S5.5）。
    /// 职责：
    ///   - "基地沦陷" + 存活至 Wave N/M + 击杀 X/Y
    ///   - 火力提示（下一个可解锁技能）
    ///   - 暗红色淡入动效（0.4s）
    ///   - 重新挑战（CTA）+ 返回选关按钮
    ///   - 不触发解锁弹窗
    /// </summary>
    public class DefeatPanelController : MonoBehaviour, IDefeatPanelController
    {
        // ── 常量 ──

        private const float FADE_IN_DURATION = 0.4f;

        // ── FairyGUI ──

        private SG_Popup.DefeatPanel _view;

        // ── 回调 ──

        private Action _onRetry;
        private Action _onQuit;

        // ══════════════════════════════════════════════
        // 接口实现
        // ══════════════════════════════════════════════

        public void BindEvents(Action onRetry, Action onQuit)
        {
            _onRetry = onRetry;
            _onQuit = onQuit;
        }

        public void Show(BattleResultData result)
        {
            EnsureView();
            PopulateData(result);
            StartShowAnimation();
        }

        // ══════════════════════════════════════════════
        // 内部
        // ══════════════════════════════════════════════

        private void EnsureView()
        {
            if (_view != null) return;

            _view = SG_Popup.DefeatPanel.CreateInstance();
            GRoot.inst.AddChild(_view);
            _view.MakeFullScreen();
            _view.sortingOrder = 100;

            // 按钮绑定
            if (_view.btn_retry != null)
                _view.btn_retry.onClick.Add(OnRetryClicked);
            else
                Debug.LogError("[DefeatPanelController] Required button missing: btn_retry");

            if (_view.btn_quit != null)
                _view.btn_quit.onClick.Add(OnReturnClicked);
            else
                Debug.LogError("[DefeatPanelController] Required button missing: btn_quit");
        }

        private void PopulateData(BattleResultData result)
        {
            SetTextSafe("text_title", "基地沦陷");
            SetTextSafe("text_wave", $"存活至 Wave {result.CurrentWave}/{result.TotalWaves}");
            SetTextSafe("text_kills", $"击杀：{result.TotalKills}");
        }

        private void StartShowAnimation()
        {
            _view.visible = true;
            _view.alpha = 0f;

            // 暗红淡入动效（Defeat 状态下 Time.timeScale=0，必须忽略引擎时间缩放）
            _view.TweenFade(1f, FADE_IN_DURATION).SetIgnoreEngineTimeScale(true);

            // 如果有红色背景蒙版，单独做渐变
            var mask = _view.mask;
            if (mask != null)
            {
                mask.alpha = 0f;
                mask.TweenFade(0.6f, FADE_IN_DURATION).SetIgnoreEngineTimeScale(true);
            }
        }

        // ══════════════════════════════════════════════
        // 按钮回调
        // ══════════════════════════════════════════════

        private void OnRetryClicked()
        {
            _view.visible = false;
            _onRetry?.Invoke();
        }

        private void OnReturnClicked()
        {
            _view.visible = false;
            _onQuit?.Invoke();
        }

        // ══════════════════════════════════════════════
        // 辅助
        // ══════════════════════════════════════════════

        private void SetTextSafe(string childName, string text)
        {
            if (childName == "text_title")
            {
                if (_view.text_defeat != null)
                    _view.text_defeat.text = text;
                return;
            }

            if (childName == "text_wave")
            {
                if (_view.text_progress != null)
                    _view.text_progress.text = text;
                return;
            }

            if (childName == "text_kills")
            {
                if (_view.text_encourage != null)
                    _view.text_encourage.text = text;
                return;
            }

            Debug.LogWarning($"[DefeatPanelController] Unsupported text binding: {childName}");
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }
    }
}
