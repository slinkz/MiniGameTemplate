using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 技能 CD 指示器面板（TDD_05 S5.4 / PK-R3 UID-002）。
    /// 2行×3列，48×48pt 圆形。
    /// 四态：Cooldown(扇形遮罩) → Ready(全亮) → Casting(金色边框) → Release(闪白)。
    /// 未装备=灰色空圈(α0.3)。
    /// 
    /// TDD-06 改动：Slot[0] 是普攻（不显示 CD），UI 从 Slot[1] 开始映射。
    /// 调用方应使用 SKILL_SLOT_START_INDEX 作为 SkillComponent 的起始读取索引。
    /// </summary>
    public class SkillCDPanel
    {
        private const int MAX_SKILLS = 6;
        private const int COLUMNS = 3;
        private const float SLOT_SIZE = 48f;
        private const float GAP = 4f;

        /// <summary>
        /// SkillComponent 槽位到 UI 槽位的起始偏移（TDD-06）。
        /// UI Panel index 0 对应 SkillComponent.GetSlot(SKILL_SLOT_START_INDEX)。
        /// Slot[0] = 普攻（不在 CD 面板显示）。
        /// </summary>
        public const int SKILL_SLOT_START_INDEX = 1;

        private readonly GComponent _container;
        private readonly GComponent[] _slots = new GComponent[MAX_SKILLS];
        private readonly GProgressBar[] _cdBars = new GProgressBar[MAX_SKILLS];
        private readonly SkillSlotState[] _lastStates = new SkillSlotState[MAX_SKILLS];

        public enum SkillSlotState : byte
        {
            Empty = 0,
            Cooldown,
            Ready,
            Casting,
            Release,
        }

        public SkillCDPanel(GComponent parent, float bottomY)
        {
            float totalWidth = COLUMNS * SLOT_SIZE + (COLUMNS - 1) * GAP;
            float totalHeight = 2 * SLOT_SIZE + GAP;
            float startX = (GRoot.inst.width - totalWidth) * 0.5f;
            float startY = bottomY - totalHeight - 8f; // 8pt padding above base HP bar

            _container = new GComponent();
            _container.SetSize(totalWidth, totalHeight);
            _container.SetXY(startX, startY);
            parent.AddChild(_container);

            for (int i = 0; i < MAX_SKILLS; i++)
            {
                int row = i / COLUMNS;
                int col = i % COLUMNS;
                float x = col * (SLOT_SIZE + GAP);
                float y = row * (SLOT_SIZE + GAP);

                // 使用 FairyGUI 编辑器中预制的 slot 组件（如果存在）
                var slot = UIPackage.CreateObject("SG_Battle", "SkillSlot")?.asCom;
                if (slot == null)
                {
                    // Fallback：代码创建
                    slot = new GComponent();
                    slot.SetSize(SLOT_SIZE, SLOT_SIZE);
                    var bg = new GGraph();
                    bg.SetSize(SLOT_SIZE, SLOT_SIZE);
                    bg.DrawEllipse(SLOT_SIZE, SLOT_SIZE, Color.gray);
                    bg.name = "bg";
                    slot.AddChild(bg);
                }

                slot.SetXY(x, y);
                _container.AddChild(slot);
                _slots[i] = slot;
                _cdBars[i] = slot.GetChild("cd_bar") as GProgressBar;
                _lastStates[i] = SkillSlotState.Empty;

                // 初始：灰色空圈
                SetSlotEmpty(i);
            }
        }

        /// <summary>
        /// 每帧更新各技能槽位状态。
        /// </summary>
        public void UpdateSlot(int index, SkillSlotState state, float cdProgress = 0f)
        {
            if (index < 0 || index >= MAX_SKILLS) return;
            var slot = _slots[index];
            if (slot == null) return;

            if (state == _lastStates[index] && state == SkillSlotState.Cooldown)
            {
                // 仅更新进度
                if (_cdBars[index] != null)
                    _cdBars[index].value = cdProgress * 100f;
                return;
            }

            _lastStates[index] = state;

            switch (state)
            {
                case SkillSlotState.Empty:
                    SetSlotEmpty(index);
                    break;
                case SkillSlotState.Cooldown:
                    slot.alpha = 1f;
                    slot.grayed = false;
                    if (_cdBars[index] != null)
                        _cdBars[index].value = cdProgress * 100f;
                    break;
                case SkillSlotState.Ready:
                    slot.alpha = 1f;
                    slot.grayed = false;
                    if (_cdBars[index] != null)
                        _cdBars[index].value = 100f;
                    break;
                case SkillSlotState.Casting:
                    slot.alpha = 1f;
                    slot.grayed = false;
                    // 金色边框可通过 controller 切换
                    var ctrl = slot.GetController("state");
                    if (ctrl != null) ctrl.selectedPage = "casting";
                    break;
                case SkillSlotState.Release:
                    // 闪白 0.2s
                    slot.alpha = 1f;
                    slot.TweenFade(0.5f, 0.1f).OnComplete(() => slot.TweenFade(1f, 0.1f));
                    break;
            }
        }

        private void SetSlotEmpty(int index)
        {
            var slot = _slots[index];
            slot.alpha = 0.3f;
            slot.grayed = true;
        }

        public void Dispose()
        {
            if (_container != null)
                _container.Dispose();
        }
    }
}
