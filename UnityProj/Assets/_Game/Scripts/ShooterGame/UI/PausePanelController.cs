using System;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 暂停菜单 V2（TDD_05 S5.5）。
    /// 
    /// 内容区域：
    /// - "当前 Build"：已装备主动(6格)+被动(3格)图标
    /// - "当前 Buff"：实时显示活跃 Buff 列表（GList 虚拟列表，固定高度240pt）
    /// - "本局统计"：击杀数/当前波次/用时
    /// - 继续按钮（最大最醒目，品牌色）→ 恢复 timeScale
    /// - 重试按钮 → 直接重开（无二次确认）
    /// - 退出按钮 → 二次确认弹窗 → 返回选关
    /// 
    /// sortOrder=800，蒙版 alpha=0.7，点击蒙版=继续。
    /// Buff 倒计时冻结（timeScale=0 时不走）。
    /// </summary>
    public class PausePanelController : MonoBehaviour, IPausePanelController
    {
        private const string FGUI_PKG = "SG_Popup";
        private const string FGUI_COMPONENT = "PausePanel";
        private const int SORT_ORDER = 800;

        [Header("SO 数据源（暂停面板统计用）")]
        [SerializeField] private IntVariable _killCount;
        [SerializeField] private IntVariable _currentWaveIndex;
        [SerializeField] private IntVariable _totalWaveCount;

        private SG_Popup.PausePanel _view;
        private Action _onResume;
        private Action _onRetry;
        private Action _onQuit;

        // 数据注入
        private Entity _playerEntity;
        private float _battleTimer;
        private BattleLevelData _battleLevelData;

        public void BindEvents(Action onResume, Action onRetry, Action onQuit)
        {
            _onResume = onResume;
            _onRetry = onRetry;
            _onQuit = onQuit;
        }


        /// <summary>注入运行时数据源（由 BattleController 调用）。</summary>
        public void SetRuntimeData(Entity playerEntity, float battleTimer, BattleLevelData levelData)
        {
            _playerEntity = playerEntity;
            _battleTimer = battleTimer;
            _battleLevelData = levelData;
        }

        public void Show()
        {
            if (_view == null)
            {
                CreateView();
            }

            // 刷新数据
            RefreshContent();

            Time.timeScale = 0f;
            _view.visible = true;
        }

        public void Hide()
        {
            Time.timeScale = 1f;
            if (_view != null)
                _view.visible = false;
        }

        private void CreateView()
        {
            _view = SG_Popup.PausePanel.CreateInstance();
            if (_view == null)
            {
                // Fallback：代码创建简易布局
                Debug.LogError("[PausePanelController] Required component missing: PausePanel");
                return;
            }

            GRoot.inst.AddChild(_view);
            _view.sortingOrder = SORT_ORDER;
            _view.MakeFullScreen();

            // 按钮绑定
            if (_view.btn_resume != null)
                _view.btn_resume.onClick.Add(OnResumeClicked);
            else
                Debug.LogError("[PausePanelController] Required button missing: btn_resume");

            var btnRetry = _view.GetChild("btn_retry")?.asButton;
            if (btnRetry != null)
                btnRetry.onClick.Add(OnRetryClicked);

            if (_view.btn_quit != null)
                _view.btn_quit.onClick.Add(OnQuitClicked);
            else
                Debug.LogError("[PausePanelController] Required button missing: btn_quit");

            // 蒙版点击=继续
            var mask = _view.mask;
            if (mask != null)
                mask.onClick.Add(OnResumeClicked);
        }

        private void RefreshContent()
        {
            // 本局统计
            var txtKills = _view.GetChild("text_kills") as GTextField;
            if (txtKills != null)
                txtKills.text = $"击杀：{_killCount.Value}";

            var txtWave = _view.GetChild("text_wave") as GTextField;
            if (txtWave != null)
                txtWave.text = $"波次：{_currentWaveIndex.Value}/{_totalWaveCount.Value}";

            var txtTime = _view.GetChild("text_time") as GTextField;
            if (txtTime != null)
            {
                int minutes = Mathf.FloorToInt(_battleTimer / 60f);
                int seconds = Mathf.FloorToInt(_battleTimer % 60f);
                txtTime.text = $"用时：{minutes:D2}:{seconds:D2}";
            }

            // 当前 Buff 列表
            RefreshBuffList();

            // 当前 Build（装备展示）
            RefreshBuildDisplay();
        }

        private void RefreshBuffList()
        {
            var buffList = _view.GetChild("list_buffs") as GList;
            if (buffList == null || _playerEntity == null) return;

            var buffComp = _playerEntity.GetComponent(ComponentType.Buff) as BuffComponent;
            if (buffComp == null || buffComp.ActiveBuffCount == 0)
            {
                buffList.numItems = 0;
                // 显示"无"
                var txtNoBuff = _view.GetChild("text_no_buff") as GTextField;
                if (txtNoBuff != null) txtNoBuff.visible = true;
                return;
            }

            var txtNoBuffHide = _view.GetChild("text_no_buff") as GTextField;
            if (txtNoBuffHide != null) txtNoBuffHide.visible = false;

            int count = buffComp.ActiveBuffCount;
            buffList.numItems = count;
            for (int i = 0; i < count; i++)
            {
                var data = buffComp.GetBuffDisplayData(i);
                var item = buffList.GetChildAt(i)?.asCom;
                if (item == null) continue;

                var txtName = item.GetChild("text_name") as GTextField;
                if (txtName != null) txtName.text = GetBuffDisplayName(data.BuffId);

                var txtTimer = item.GetChild("text_timer") as GTextField;
                if (txtTimer != null)
                {
                    txtTimer.text = data.Duration > 0
                        ? $"{data.RemainingTime:F1}s"
                        : "永久";
                }

                // 增益=蓝色边框，减益=红色边框
                var border = item.GetChild("border") as GGraph;
                if (border != null)
                {
                    Color borderColor = data.Tag == BuffTag.Positive
                        ? new Color(0.31f, 0.76f, 0.97f) // #4FC3F7
                        : new Color(0.94f, 0.33f, 0.31f); // #EF5350
                    border.color = borderColor;
                }
            }
        }

        private void RefreshBuildDisplay()
        {
            if (_battleLevelData == null) return;

            // 主动技能图标（6 格）
            for (int i = 0; i < 6; i++)
            {
                var slot = _view.GetChild($"skill_slot_{i}") as GComponent;
                if (slot == null) continue;

                bool hasSkill = _battleLevelData.EquippedSkills != null
                    && i < _battleLevelData.EquippedSkills.Length
                    && _battleLevelData.EquippedSkills[i] != null;

                slot.grayed = !hasSkill;
                slot.alpha = hasSkill ? 1f : 0.3f;
            }

            // 被动技能图标（3 格）
            for (int i = 0; i < 3; i++)
            {
                var slot = _view.GetChild($"passive_slot_{i}") as GComponent;
                if (slot == null) continue;

                bool hasPassive = _battleLevelData.EquippedPassives != null
                    && i < _battleLevelData.EquippedPassives.Length
                    && _battleLevelData.EquippedPassives[i] != null;

                slot.grayed = !hasPassive;
                slot.alpha = hasPassive ? 1f : 0.3f;
            }
        }

        /// <summary>根据 BuffId 获取显示名（从 SO 缓存）</summary>
        private string GetBuffDisplayName(int buffId)
        {
            // 简化实现：遍历 BuffConfigSO 查找
            // V3 可改为 Dictionary 缓存
            var allBuffs = Resources.FindObjectsOfTypeAll<BuffConfigSO>();
            for (int i = 0; i < allBuffs.Length; i++)
            {
                if (allBuffs[i].BuffId == buffId)
                    return allBuffs[i].DisplayName;
            }
            return $"Buff #{buffId}";
        }

        private void OnResumeClicked()
        {
            Hide();
            _onResume?.Invoke();
        }

        private void OnRetryClicked()
        {
            Time.timeScale = 1f;
            if (_view != null) _view.visible = false;
            _onRetry?.Invoke();
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;
            _onQuit?.Invoke();
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
