using System.Collections.Generic;
using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Navigation;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using Game.ShooterGame;

namespace SG_Sortie
{
    /// <summary>
    /// 出战准备 Bottom Sheet 面板数据。
    /// 由 LevelSelectScreen 点击关卡时传入。
    /// </summary>
    public class SortieData
    {
        /// <summary>关卡索引（1-based，与选关界面一致）。</summary>
        public int LevelIndex;
    }

    /// <summary>
    /// 出战准备 Bottom Sheet — 半屏面板，展示已解锁技能/被动，供玩家装备后出击。
    /// TDD_02 S2.3 / V2 Sprint 5 实现。
    ///
    /// 交互：
    /// - 点击技能/被动卡片 → Toggle 装备（主动≤6，被动≤3）
    /// - 出击按钮 → 写入 BattleLevelData → Push Battle 节点
    /// - 点蒙版/下滑 → 关闭面板
    ///
    /// 注意：这不是导航节点面板（无 FlowNodeSO），而是由 LevelSelectScreen 直接
    /// 通过 UIManager.OpenPanelAsync 弹出的半屏 overlay。
    /// </summary>
    public partial class SortieBottomSheet : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL + 10;
        public bool IsFullScreen => false;
        public string PanelPackageName => "SG_Sortie";

        private const int MAX_SKILLS = 6;
        private const int MAX_PASSIVES = 3;

        private int _levelIndex;
        private SkillUnlockManager _unlockManager;

        // 选中状态跟踪
        private readonly List<SkillConfigSO> _selectedSkills = new List<SkillConfigSO>(MAX_SKILLS);
        private readonly List<PassiveAbilitySO> _selectedPassives = new List<PassiveAbilitySO>(MAX_PASSIVES);

        // 数据缓存（避免内部 buffer 问题，拷贝一份）
        private readonly List<SkillConfigSO> _unlockedSkills = new List<SkillConfigSO>(8);
        private readonly List<PassiveAbilitySO> _unlockedPassives = new List<PassiveAbilitySO>(4);

        public void OnOpen(object data)
        {
            var sortieData = data as SortieData;
            if (sortieData == null)
            {
                Debug.LogError("[SortieBottomSheet] OnOpen 收到 null data");
                return;
            }

            _levelIndex = sortieData.LevelIndex;
            _unlockManager = SG_Boot.UnlockManager;

            // 绑定按钮
            if (btn_sortie != null) btn_sortie.onClick.Add(OnSortieClicked);
            if (mask != null) mask.onClick.Add(OnMaskClicked);

            // 初始化数据
            PopulateSkillList();
            PopulatePassiveList();
            UpdateTitle();
        }

        public void OnClose()
        {
            // 解绑按钮事件（与 OnOpen 对称）
            if (btn_sortie != null) btn_sortie.onClick.Remove(OnSortieClicked);
            if (mask != null) mask.onClick.Remove(OnMaskClicked);

            _selectedSkills.Clear();
            _selectedPassives.Clear();
            _unlockedSkills.Clear();
            _unlockedPassives.Clear();
            _unlockManager = null;
        }

        public void OnRefresh(object data)
        {
            var sortieData = data as SortieData;
            if (sortieData != null)
            {
                _levelIndex = sortieData.LevelIndex;
            }

            PopulateSkillList();
            PopulatePassiveList();
            UpdateTitle();
        }

        // ── 技能列表 ──

        private void PopulateSkillList()
        {
            _selectedSkills.Clear();
            _unlockedSkills.Clear();

            if (_unlockManager != null)
            {
                var skills = _unlockManager.GetUnlockedSkills();
                // 拷贝（GetUnlockedSkills 返回内部 buffer）
                for (int i = 0; i < skills.Count; i++)
                    _unlockedSkills.Add(skills[i]);
            }

            // V2：自动全选已解锁技能（≤6 个）
            for (int i = 0; i < _unlockedSkills.Count && i < MAX_SKILLS; i++)
            {
                _selectedSkills.Add(_unlockedSkills[i]);
            }

            // 更新 GList
            if (list_skills != null)
            {
                list_skills.itemRenderer = RenderSkillItem;
                list_skills.numItems = _unlockedSkills.Count;
            }
        }

        private void RenderSkillItem(int index, GObject obj)
        {
            var item = obj as SkillCard;
            if (item == null || index >= _unlockedSkills.Count) return;

            var skill = _unlockedSkills[index];
            bool isSelected = _selectedSkills.Contains(skill);

            // 设置名称
            if (item.text_name != null) item.text_name.text = skill.DisplayName;

            // 选中态（controller "selected" page 0=未选 1=已选）
            var ctrl = item.selected;
            if (ctrl != null) ctrl.selectedIndex = isSelected ? 1 : 0;

            // 绑定点击（利用 data 属性避免闭包）
            item.data = index;
            item.onClick.Clear();
            item.onClick.Add(() => OnSkillCardClicked(index));
        }

        private void OnSkillCardClicked(int index)
        {
            if (index >= _unlockedSkills.Count) return;
            var skill = _unlockedSkills[index];

            if (_selectedSkills.Contains(skill))
            {
                // 取消选中
                _selectedSkills.Remove(skill);
            }
            else
            {
                // 选中（检查上限）
                if (_selectedSkills.Count >= MAX_SKILLS)
                {
                    ShakeItem(list_skills, index);
                    return;
                }
                _selectedSkills.Add(skill);
            }

            // 刷新单个卡片的选中态
            RefreshSkillItemState(index);
        }

        private void RefreshSkillItemState(int index)
        {
            if (list_skills == null) return;
            var item = list_skills.GetChildAt(index) as SkillCard;
            if (item == null) return;

            bool isSelected = _selectedSkills.Contains(_unlockedSkills[index]);
            var ctrl = item.selected;
            if (ctrl != null) ctrl.selectedIndex = isSelected ? 1 : 0;
        }

        // ── 被动列表 ──

        private void PopulatePassiveList()
        {
            _selectedPassives.Clear();
            _unlockedPassives.Clear();

            if (_unlockManager != null)
            {
                var passives = _unlockManager.GetUnlockedPassives();
                for (int i = 0; i < passives.Count; i++)
                    _unlockedPassives.Add(passives[i]);
            }

            // V2：自动全选已解锁被动（≤3 个）
            for (int i = 0; i < _unlockedPassives.Count && i < MAX_PASSIVES; i++)
            {
                _selectedPassives.Add(_unlockedPassives[i]);
            }

            if (list_passives != null)
            {
                list_passives.itemRenderer = RenderPassiveItem;
                list_passives.numItems = _unlockedPassives.Count;
            }
        }

        private void RenderPassiveItem(int index, GObject obj)
        {
            var item = obj as PassiveCard;
            if (item == null || index >= _unlockedPassives.Count) return;

            var passive = _unlockedPassives[index];
            bool isSelected = _selectedPassives.Contains(passive);

            if (item.text_name != null) item.text_name.text = passive.DisplayName;

            var ctrl = item.selected;
            if (ctrl != null) ctrl.selectedIndex = isSelected ? 1 : 0;

            item.data = index;
            item.onClick.Clear();
            item.onClick.Add(() => OnPassiveCardClicked(index));
        }

        private void OnPassiveCardClicked(int index)
        {
            if (index >= _unlockedPassives.Count) return;
            var passive = _unlockedPassives[index];

            if (_selectedPassives.Contains(passive))
            {
                _selectedPassives.Remove(passive);
            }
            else
            {
                if (_selectedPassives.Count >= MAX_PASSIVES)
                {
                    ShakeItem(list_passives, index);
                    return;
                }
                _selectedPassives.Add(passive);
            }

            RefreshPassiveItemState(index);
        }

        private void RefreshPassiveItemState(int index)
        {
            if (list_passives == null) return;
            var item = list_passives.GetChildAt(index) as PassiveCard;
            if (item == null) return;

            bool isSelected = _selectedPassives.Contains(_unlockedPassives[index]);
            var ctrl = item.selected;
            if (ctrl != null) ctrl.selectedIndex = isSelected ? 1 : 0;
        }

        // ── 标题/敌机预告 ──

        private void UpdateTitle()
        {
            if (text_level != null)
                text_level.text = $"第 {_levelIndex} 关";
        }

        // ── 出击按钮 ──

        private async void OnSortieClicked()
        {
            // 至少选一个主动技能
            if (_selectedSkills.Count == 0)
            {
                GameLog.LogWarning("[SortieBottomSheet] 未选择任何主动技能");
                if (btn_sortie != null)
                    btn_sortie.TweenMoveX(btn_sortie.x + 5f, 0.05f).SetRepeat(3, true);
                return;
            }

            GameLog.Log($"[SortieBottomSheet] 出击! Level={_levelIndex}, Skills={_selectedSkills.Count}, Passives={_selectedPassives.Count}");

            // 组装 BattleLevelData
            var battleData = new BattleLevelData
            {
                LevelIndex = _levelIndex - 1, // 1-based → 0-based
                EquippedSkills = _selectedSkills.ToArray(),
                EquippedPassives = _selectedPassives.ToArray()
            };

            // 关闭自己，然后 Push Battle
            UIManager.Instance.ClosePanel<SortieBottomSheet>();

            var battleNode = SG_FlowNodes.NodeBattle;
            if (battleNode != null)
            {
                await AppFlowNavigator.Instance.PushAsync(battleNode, battleData);
            }
            else
            {
                Debug.LogError("[SortieBottomSheet] Node_Battle SO is null!");
            }
        }

        // ── 关闭 ──

        private void OnMaskClicked()
        {
            UIManager.Instance.ClosePanel<SortieBottomSheet>();
        }

        // ── 工具 ──

        private void ShakeItem(GList list, int index)
        {
            if (list == null) return;
            var item = list.GetChildAt(index);
            if (item != null)
            {
                item.TweenMoveX(item.x + 4f, 0.04f).SetRepeat(3, true);
            }
        }
    }
}
