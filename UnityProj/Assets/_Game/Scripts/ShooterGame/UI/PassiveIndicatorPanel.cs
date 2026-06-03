using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 被动技能指示器面板（TDD_05 S5.4 / PK-R3 UID-003）。
    /// 左上角 3 个 40×40pt 方形圆角。
    /// 三态：Cooldown(暗色+充能边框顺时针) → Ready(亮色+呼吸缩放) → Active(绿色环形进度消耗)。
    /// </summary>
    public class PassiveIndicatorPanel
    {
        private const int MAX_PASSIVES = 3;
        private const float SLOT_SIZE = 40f;
        private const float GAP = 6f;
        private const float BREATH_MIN = 1.0f;
        private const float BREATH_MAX = 1.05f;
        private const float BREATH_HZ = 2f;

        private readonly GComponent _container;
        private readonly GComponent[] _slots = new GComponent[MAX_PASSIVES];
        private readonly PassiveSlotState[] _lastStates = new PassiveSlotState[MAX_PASSIVES];
        private readonly float[] _breathTimers = new float[MAX_PASSIVES];

        public enum PassiveSlotState : byte
        {
            Empty = 0,
            Cooldown,
            Ready,
            Active,
        }

        public PassiveIndicatorPanel(GComponent parent)
        {
            // 左上角，PauseButton 右侧
            float startX = 56f; // 留出暂停按钮空间
            float startY = 12f;

            _container = new GComponent();
            _container.SetSize(MAX_PASSIVES * (SLOT_SIZE + GAP), SLOT_SIZE);
            _container.SetXY(startX, startY);
            parent.AddChild(_container);

            for (int i = 0; i < MAX_PASSIVES; i++)
            {
                float x = i * (SLOT_SIZE + GAP);

                // 纯代码创建（40×40 方形圆角白模）
                var slot = new GComponent();
                slot.SetSize(SLOT_SIZE, SLOT_SIZE);

                var bg = new GGraph();
                bg.SetSize(SLOT_SIZE, SLOT_SIZE);
                bg.DrawRect(SLOT_SIZE, SLOT_SIZE, 0, Color.clear, new Color(0.3f, 0.3f, 0.3f));
                bg.name = "bg";
                slot.AddChild(bg);

                // cd_progress：充能进度条
                var cdProgress = new GProgressBar();
                cdProgress.SetSize(SLOT_SIZE, SLOT_SIZE);
                cdProgress.name = "cd_progress";
                cdProgress.value = 0;
                slot.AddChild(cdProgress);

                // active_progress：激活状态进度条
                var activeProgress = new GProgressBar();
                activeProgress.SetSize(SLOT_SIZE, SLOT_SIZE);
                activeProgress.name = "active_progress";
                activeProgress.value = 0;
                slot.AddChild(activeProgress);

                slot.SetXY(x, 0);
                _container.AddChild(slot);
                _slots[i] = slot;
                _lastStates[i] = PassiveSlotState.Empty;
                SetSlotEmpty(i);
            }
        }

        /// <summary>
        /// 每帧更新。
        /// </summary>
        public void UpdateSlot(int index, PassiveSlotState state, float progress = 0f)
        {
            if (index < 0 || index >= MAX_PASSIVES) return;
            var slot = _slots[index];
            if (slot == null) return;

            _lastStates[index] = state;

            switch (state)
            {
                case PassiveSlotState.Empty:
                    SetSlotEmpty(index);
                    break;
                case PassiveSlotState.Cooldown:
                    slot.alpha = 0.6f;
                    slot.grayed = true;
                    slot.SetScale(1f, 1f);
                    // 充能进度（如果有 progress bar 子对象）
                    var cdBar = slot.GetChild("cd_progress") as GProgressBar;
                    if (cdBar != null) cdBar.value = progress * 100f;
                    break;
                case PassiveSlotState.Ready:
                    slot.alpha = 1f;
                    slot.grayed = false;
                    // 呼吸缩放由 Tick 驱动
                    break;
                case PassiveSlotState.Active:
                    slot.alpha = 1f;
                    slot.grayed = false;
                    slot.SetScale(1f, 1f);
                    // 环形进度消耗
                    var activeBar = slot.GetChild("active_progress") as GProgressBar;
                    if (activeBar != null) activeBar.value = progress * 100f;
                    break;
            }
        }

        /// <summary>
        /// 每帧调用驱动呼吸动画。
        /// </summary>
        public void Tick(float dt)
        {
            for (int i = 0; i < MAX_PASSIVES; i++)
            {
                if (_lastStates[i] == PassiveSlotState.Ready)
                {
                    _breathTimers[i] += dt * BREATH_HZ * 2f * Mathf.PI;
                    float scale = Mathf.Lerp(BREATH_MIN, BREATH_MAX, (Mathf.Sin(_breathTimers[i]) + 1f) * 0.5f);
                    _slots[i]?.SetScale(scale, scale);
                }
            }
        }

        private void SetSlotEmpty(int index)
        {
            var slot = _slots[index];
            slot.alpha = 0.3f;
            slot.grayed = true;
            slot.SetScale(1f, 1f);
        }

        public void Dispose()
        {
            _container?.Dispose();
        }
    }
}
