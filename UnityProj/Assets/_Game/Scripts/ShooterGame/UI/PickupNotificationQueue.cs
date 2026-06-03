using System.Collections.Generic;
using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 拾取通知队列（TDD_05 S5.4）。
    /// "获得：{名}！"弹入0.2s→显示1.5s→淡出0.3s。
    /// 队列最大2（超出快速替换最旧）。
    /// </summary>
    public class PickupNotificationQueue
    {
        private const int MAX_QUEUE = 2;
        private const float SHOW_DURATION = 1.5f;
        private const float FADE_IN_DURATION = 0.2f;
        private const float FADE_OUT_DURATION = 0.3f;

        private readonly GComponent _parent;
        private readonly Queue<NotificationSlot> _activeSlots = new();
        private readonly float _posX;
        private readonly float _posY;

        private struct NotificationSlot
        {
            public GComponent View;
            public float RemainingTime;
        }

        public PickupNotificationQueue(GComponent parent, float centerX, float bottomY)
        {
            _parent = parent;
            _posX = centerX;
            _posY = bottomY - 60f; // 中下偏上
        }

        /// <summary>
        /// 显示一条拾取通知。
        /// </summary>
        public void ShowNotification(string pickupName)
        {
            // 超出队列上限：替换最旧
            if (_activeSlots.Count >= MAX_QUEUE)
            {
                var oldest = _activeSlots.Dequeue();
                if (oldest.View != null)
                {
                    GTween.Kill(oldest.View);
                    oldest.View.Dispose();
                }
            }

            // 创建通知 UI
            // 纯代码创建通知横幅
            var notif = new GComponent();
            notif.SetSize(300, 40);

            var notifBg = new GGraph();
            notifBg.SetSize(300, 40);
            notifBg.DrawRect(300, 40, 0, Color.clear, new Color(0.1f, 0.1f, 0.18f, 0.8f));
            notifBg.name = "notif_bg";
            notif.AddChild(notifBg);

            var txt = new GTextField();
            txt.SetSize(300, 40);
            var tf = txt.textFormat;
            tf.size = 20;
            tf.color = Color.white;
            txt.textFormat = tf;
            txt.align = AlignType.Center;
            txt.name = "text";
            notif.AddChild(txt);

            var textField = notif.GetChild("text") as GTextField;
            if (textField != null)
                textField.text = $"获得：{pickupName}！";

            // 位置：根据当前队列数偏移
            float offsetY = _activeSlots.Count * 44f;
            notif.SetXY(_posX - notif.width * 0.5f, _posY - offsetY);
            notif.alpha = 0f;
            _parent.AddChild(notif);

            // 弹入动效
            notif.TweenFade(1f, FADE_IN_DURATION);
            notif.TweenMoveY(notif.y - 10f, FADE_IN_DURATION).SetEase(EaseType.QuadOut);

            _activeSlots.Enqueue(new NotificationSlot { View = notif, RemainingTime = SHOW_DURATION });
        }

        /// <summary>
        /// 每帧调用。驱动超时淡出。
        /// </summary>
        public void Tick(float dt)
        {
            int count = _activeSlots.Count;
            for (int i = 0; i < count; i++)
            {
                var slot = _activeSlots.Dequeue();
                slot.RemainingTime -= dt;

                if (slot.RemainingTime <= 0f)
                {
                    // 淡出
                    if (slot.View != null)
                    {
                        var view = slot.View;
                        view.TweenFade(0f, FADE_OUT_DURATION).OnComplete(() => view.Dispose());
                    }
                    // 不重新入队（已过期）
                }
                else
                {
                    _activeSlots.Enqueue(slot);
                }
            }
        }

        public void Clear()
        {
            while (_activeSlots.Count > 0)
            {
                var slot = _activeSlots.Dequeue();
                if (slot.View != null)
                {
                    GTween.Kill(slot.View);
                    slot.View.Dispose();
                }
            }
        }
    }
}
