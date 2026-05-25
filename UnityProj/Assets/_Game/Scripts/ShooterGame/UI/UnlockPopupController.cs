using System;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 解锁弹窗（TDD_05 S5.5 / PK-R3 UID-009 方案 A）。
    /// 多解锁=逐个弹出（间隔 0.3s 或玩家关闭后弹下一个）。
    /// sortOrder=200。
    /// </summary>
    public class UnlockPopupController
    {
        private const string FGUI_PKG = "SG_Popup";
        private const string FGUI_COMPONENT = "UnlockPopup";
        private const int SORT_ORDER = 200;
        private const float POPUP_INTERVAL = 0.3f;

        private GComponent _view;
        private readonly Queue<UnlockData> _pendingUnlocks = new();
        private Action<UnlockData> _onEquipNow;
        private Action _onDismissAll;
        private bool _isShowing;

        public struct UnlockData
        {
            public string DisplayName;
            public string TypeLabel; // "主动技能" / "被动技能"
            public string Description;
            public string IconKey;
        }

        /// <summary>
        /// 绑定回调。
        /// </summary>
        public void BindEvents(Action<UnlockData> onEquipNow, Action onDismissAll)
        {
            _onEquipNow = onEquipNow;
            _onDismissAll = onDismissAll;
        }

        /// <summary>
        /// 将解锁数据加入队列。调用 ShowNext() 开始弹出。
        /// </summary>
        public void Enqueue(UnlockData data)
        {
            _pendingUnlocks.Enqueue(data);
        }

        /// <summary>
        /// 开始弹出队列中的下一个。
        /// </summary>
        public void ShowNext()
        {
            if (_pendingUnlocks.Count == 0)
            {
                _isShowing = false;
                _onDismissAll?.Invoke();
                return;
            }

            _isShowing = true;
            var data = _pendingUnlocks.Dequeue();
            ShowSingle(data);
        }

        private void ShowSingle(UnlockData data)
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject(FGUI_PKG, FGUI_COMPONENT)?.asCom;
                if (_view == null)
                {
                    // Fallback：创建简易视图
                    _view = new GComponent();
                    _view.SetSize(GRoot.inst.width, GRoot.inst.height);
                }
                GRoot.inst.AddChild(_view);
                _view.sortingOrder = SORT_ORDER;
                _view.MakeFullScreen();

                // 绑定按钮事件
                var btnEquip = _view.GetChild("btn_equip")?.asButton;
                if (btnEquip != null)
                    btnEquip.onClick.Add(() => OnEquipClicked(data));

                var btnLater = _view.GetChild("btn_later")?.asButton;
                if (btnLater != null)
                    btnLater.onClick.Add(OnLaterClicked);
            }

            // 填充内容
            var txtName = _view.GetChild("text_name") as GTextField;
            if (txtName != null) txtName.text = data.DisplayName;

            var txtType = _view.GetChild("text_type") as GTextField;
            if (txtType != null) txtType.text = data.TypeLabel;

            var txtDesc = _view.GetChild("text_desc") as GTextField;
            if (txtDesc != null) txtDesc.text = data.Description;

            // 弹出动效：中心缩放
            _view.visible = true;
            _view.SetScale(0.5f, 0.5f);
            _view.alpha = 0f;
            _view.TweenScale(new Vector2(1f, 1f), 0.3f).SetEase(EaseType.BackOut);
            _view.TweenFade(1f, 0.2f);
        }

        private void OnEquipClicked(UnlockData data)
        {
            HideAndContinue();
            _onEquipNow?.Invoke(data);
        }

        private void OnLaterClicked()
        {
            HideAndContinue();
        }

        private void HideAndContinue()
        {
            if (_view != null)
            {
                _view.TweenFade(0f, 0.2f).OnComplete(() =>
                {
                    _view.visible = false;
                    // 延迟后弹出下一个
                    if (_pendingUnlocks.Count > 0)
                    {
                        Timers.inst.Add(POPUP_INTERVAL, 1, (obj) => ShowNext());
                    }
                    else
                    {
                        _isShowing = false;
                        _onDismissAll?.Invoke();
                    }
                });
            }
        }

        public bool IsShowing => _isShowing;
        public int PendingCount => _pendingUnlocks.Count;

        public void Dispose()
        {
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
            _pendingUnlocks.Clear();
        }
    }
}
