using System;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 胜利结算面板 Controller（V2 TDD_05 S5.5）。
    /// 职责：
    ///   - 关卡名 + 星级（⭐×N）
    ///   - 击杀/用时/金币
    ///   - 技能贡献横向条形图（前4名+其他合并）
    ///   - 条形生长动效（EaseOutCubic，间隔0.1s）
    ///   - 最高贡献条金色描边
    ///   - 下一关/返回选关按钮
    ///   - 关闭后触发解锁弹窗检查（通过回调，由 BattleController 管理时序）
    /// </summary>
    public class VictoryPanelController : MonoBehaviour, IVictoryPanelController
    {
        // ── 常量 ──

        private const int MAX_CONTRIBUTION_BARS = 5;
        private const float MERGE_THRESHOLD = 0.05f; // ≤5% 合并为"其他"
        private const float BAR_ANIM_DURATION = 0.3f;
        private const float BAR_ANIM_INTERVAL = 0.1f;
        private const float PANEL_SLIDE_DURATION = 0.3f;
        private const float STAR_POP_INTERVAL = 0.3f;

        // ── FairyGUI 组件引用 ──

        private SG_Popup.VictoryPanel _view;
        private GList _barList;

        // ── 回调 ──

        private Action _onNextLevel;
        private Action _onReturnToSelect;

        // ── 动效状态 ──

        private readonly List<BarAnimState> _barAnims = new List<BarAnimState>(MAX_CONTRIBUTION_BARS);
        private float _animTimer;
        private int _animIndex;
        private bool _isAnimating;

        // ── 缓存 ──

        private readonly List<ContributionEntry> _sortedEntries = new List<ContributionEntry>(8);

        // ── 结构 ──

        private struct ContributionEntry
        {
            public int SourceTag;
            public float Percentage;
        }

        private struct BarAnimState
        {
            public GComponent BarItem;
            public float TargetPercent;
            public float CurrentPercent;
            public float Elapsed;
            public bool Started;
            public bool Finished;
        }

        // ══════════════════════════════════════════════
        // 接口实现
        // ══════════════════════════════════════════════

        public void BindEvents(Action onNextLevel, Action onReturnToSelect)
        {
            _onNextLevel = onNextLevel;
            _onReturnToSelect = onReturnToSelect;
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

            _view = SG_Popup.VictoryPanel.CreateInstance();
            GRoot.inst.AddChild(_view);
            _view.MakeFullScreen();
            _view.sortingOrder = 100;

            // 按钮绑定
            if (_view.btn_confirm != null)
                _view.btn_confirm.onClick.Add(OnReturnClicked);
            else
                Debug.LogError("[VictoryPanelController] Required button missing: btn_confirm");

            _barList = _view.GetChild("list_contribution")?.asList;
        }

        private void PopulateData(BattleResultData result)
        {
            // 关卡名
            var txtLevel = _view.GetChild("text_level")?.asTextField;
            if (txtLevel != null)
                txtLevel.text = $"第 {result.LevelIndex + 1} 关";

            // 星级
            var starGroup = _view.star_group;
            if (starGroup != null)
            {
                var ctrl = starGroup.stars;
                if (ctrl != null)
                    ctrl.selectedIndex = Mathf.Clamp(result.Stars, 0, 3);
            }

            // 击杀/用时/金币
            SetTextSafe("text_kills", $"击杀：{result.TotalKills}");
            int minutes = Mathf.FloorToInt(result.BattleTime / 60f);
            int seconds = Mathf.FloorToInt(result.BattleTime % 60f);
            SetTextSafe("text_time", $"用时：{minutes:D2}:{seconds:D2}");
            SetTextSafe("text_coins", $"金币：+{result.CoinsEarned}");

            // 技能贡献条形图
            BuildContributionBars(result.DamageStats);
        }

        private void BuildContributionBars(Dictionary<int, int> damageStats)
        {
            _sortedEntries.Clear();
            _barAnims.Clear();
            _isAnimating = false;
            _animIndex = 0;
            _animTimer = 0f;

            if (_barList == null || damageStats == null || damageStats.Count == 0)
                return;

            _barList.RemoveChildrenToPool();

            // 计算总伤害
            long totalDamage = 0;
            foreach (var kvp in damageStats)
                totalDamage += kvp.Value;

            if (totalDamage <= 0) return;

            // 转换为百分比并排序
            foreach (var kvp in damageStats)
            {
                float pct = (float)kvp.Value / totalDamage;
                _sortedEntries.Add(new ContributionEntry { SourceTag = kvp.Key, Percentage = pct });
            }

            _sortedEntries.Sort((a, b) => b.Percentage.CompareTo(a.Percentage));

            // 取前4名 + 合并剩余为"其他"
            float othersTotal = 0f;
            int displayCount = 0;

            for (int i = 0; i < _sortedEntries.Count && displayCount < MAX_CONTRIBUTION_BARS - 1; i++)
            {
                if (_sortedEntries[i].Percentage <= MERGE_THRESHOLD)
                {
                    // 从这里开始全部合并
                    for (int j = i; j < _sortedEntries.Count; j++)
                        othersTotal += _sortedEntries[j].Percentage;
                    break;
                }

                AddBarItem(_sortedEntries[i].SourceTag, _sortedEntries[i].Percentage, displayCount == 0);
                displayCount++;
            }

            // "其他"条
            if (othersTotal > 0.001f)
            {
                AddBarItem(-1, othersTotal, false); // -1 = 其他
            }

            // 启动动效
            _isAnimating = _barAnims.Count > 0;
        }

        private void AddBarItem(int sourceTag, float percentage, bool isTop)
        {
            var item = _barList.AddItemFromPool()?.asCom;
            if (item == null) return;

            var info = sourceTag >= 0
                ? SkillStatsMapping.GetDisplayInfo(sourceTag)
                : SkillStatsMapping.OtherInfo;

            // 设置名称文字
            var txtName = item.GetChild("text_name")?.asTextField;
            if (txtName != null)
                txtName.text = info.Name;

            // 设置百分比文字（初始0%，动效后更新）
            var txtPct = item.GetChild("text_percent")?.asTextField;
            if (txtPct != null)
                txtPct.text = "0%";

            // 设置条形颜色
            var bar = item.GetChild("bar_fill")?.asGraph;
            if (bar != null)
            {
                bar.color = info.BarColor;
                bar.width = 0; // 动效起点
            }

            // 最高贡献条金色描边（2pt #FFD54F）
            if (isTop)
            {
                var border = item.GetChild("bar_border")?.asGraph;
                if (border != null)
                {
                    border.visible = true;
                    border.color = new Color(1f, 0.835f, 0.31f); // #FFD54F
                }
            }

            _barAnims.Add(new BarAnimState
            {
                BarItem = item,
                TargetPercent = percentage,
                CurrentPercent = 0f,
                Elapsed = 0f,
                Started = false,
                Finished = false,
            });
        }

        private void StartShowAnimation()
        {
            _view.visible = true;
            _view.alpha = 0f;
            _view.y = Screen.height * 0.1f;

            // 面板滑入动效（Victory 状态下 Time.timeScale=0，必须忽略引擎时间缩放）
            _view.TweenFade(1f, PANEL_SLIDE_DURATION).SetIgnoreEngineTimeScale(true);
            _view.TweenMoveY(0f, PANEL_SLIDE_DURATION).SetEase(EaseType.CubicOut).SetIgnoreEngineTimeScale(true);

            // 星星弹出延迟（由 FairyGUI 动效控制器驱动，此处仅触发）
            var starGroup = _view.star_group;
            if (starGroup != null)
            {
                var trans = starGroup.GetTransition("t_pop");
                if (trans != null)
                    trans.Play();
            }
        }

        // ══════════════════════════════════════════════
        // Update 驱动条形动效
        // ══════════════════════════════════════════════

        private void Update()
        {
            if (!_isAnimating) return;

            _animTimer += Time.unscaledDeltaTime;

            for (int i = 0; i < _barAnims.Count; i++)
            {
                var state = _barAnims[i];

                // 依次启动（间隔 BAR_ANIM_INTERVAL）
                float startTime = i * BAR_ANIM_INTERVAL;
                if (_animTimer < startTime) continue;

                if (!state.Started)
                {
                    state.Started = true;
                    _barAnims[i] = state;
                }

                if (state.Finished) continue;

                state.Elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(state.Elapsed / BAR_ANIM_DURATION);
                float eased = EaseOutCubic(t);
                state.CurrentPercent = state.TargetPercent * eased;

                // 更新条形宽度
                var bar = state.BarItem.GetChild("bar_fill")?.asGraph;
                if (bar != null)
                {
                    // 条形最大宽度 = 父容器宽度的 TargetPercent 比例
                    float maxWidth = state.BarItem.width * 0.6f; // 60%区域留给条形
                    bar.width = maxWidth * state.CurrentPercent / Mathf.Max(state.TargetPercent, 0.01f) * eased;
                }

                // 更新百分比数字
                var txtPct = state.BarItem.GetChild("text_percent")?.asTextField;
                if (txtPct != null)
                    txtPct.text = $"{Mathf.RoundToInt(state.CurrentPercent * 100)}%";

                if (t >= 1f)
                    state.Finished = true;

                _barAnims[i] = state;
            }

            // 检查全部完成
            bool allDone = true;
            for (int i = 0; i < _barAnims.Count; i++)
            {
                if (!_barAnims[i].Finished) { allDone = false; break; }
            }

            if (allDone)
                _isAnimating = false;
        }

        // ══════════════════════════════════════════════
        // 按钮回调
        // ══════════════════════════════════════════════

        private void OnNextLevelClicked()
        {
            HideAndDispose();
            _onNextLevel?.Invoke();
        }

        private void OnReturnClicked()
        {
            HideAndDispose();
            _onReturnToSelect?.Invoke();
        }

        private void HideAndDispose()
        {
            if (_view != null)
            {
                _view.visible = false;
            }
        }

        // ══════════════════════════════════════════════
        // 辅助
        // ══════════════════════════════════════════════

        private void SetTextSafe(string childName, string text)
        {
            if (childName == "text_kills")
            {
                if (_view.text_kills != null)
                    _view.text_kills.text = text;
                return;
            }

            if (childName == "text_hp")
            {
                if (_view.text_hp != null)
                    _view.text_hp.text = text;
                return;
            }

            var tf = _view.GetChild(childName)?.asTextField;
            if (tf != null)
                tf.text = text;
        }

        private static float EaseOutCubic(float t)
        {
            t -= 1f;
            return t * t * t + 1f;
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
