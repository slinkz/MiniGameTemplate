using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// T2 Buff 速览面板（TDD_05 S5.2）。
    /// 自动扫描所有 BuffConfigSO / DotConfigSO，列表展示 + 排序 + 筛选 + Ping。
    /// </summary>
    public class SG_BuffOverviewWindow : EditorWindow
    {
        // ── 持久化 Key ──
        private const string PREF_SORT_COL = "SG_BuffOverview_SortColumn";
        private const string PREF_SORT_ASC = "SG_BuffOverview_SortAscending";

        private enum Tab { Buff, DOT }

        private Tab _currentTab = Tab.Buff;
        private int _sortColumn;
        private bool _sortAscending = true;
        private int _tagFilter = -1; // -1 = All, 0+ = (int)BuffTag
        private string _searchText = "";
        private Vector2 _scrollPos;

        // ── 数据缓存 ──
        private List<BuffConfigSO> _allBuffs = new();
        private List<DotConfigSO> _allDots = new();

        [MenuItem("Tools/ShooterGame/工具/Buff Overview")]
        public static void ShowWindow()
        {
            var window = GetWindow<SG_BuffOverviewWindow>("Buff Overview");
            window.minSize = new Vector2(600, 300);
        }

        private void OnEnable()
        {
            _sortColumn = EditorPrefs.GetInt(PREF_SORT_COL, 0);
            _sortAscending = EditorPrefs.GetBool(PREF_SORT_ASC, true);
            Refresh();
        }

        private void OnDisable()
        {
            EditorPrefs.SetInt(PREF_SORT_COL, _sortColumn);
            EditorPrefs.SetBool(PREF_SORT_ASC, _sortAscending);
        }

        private void Refresh()
        {
            _allBuffs.Clear();
            _allDots.Clear();

            foreach (var guid in AssetDatabase.FindAssets("t:BuffConfigSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<BuffConfigSO>(path);
                if (so != null) _allBuffs.Add(so);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:DotConfigSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<DotConfigSO>(path);
                if (so != null) _allDots.Add(so);
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            if (_currentTab == Tab.Buff)
                DrawBuffTable();
            else
                DrawDotTable();
            EditorGUILayout.EndScrollView();
        }

        // ──────────────────── Toolbar ────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Tab 切换
            if (GUILayout.Toggle(_currentTab == Tab.Buff, "Buff", EditorStyles.toolbarButton))
                _currentTab = Tab.Buff;
            if (GUILayout.Toggle(_currentTab == Tab.DOT, "DOT", EditorStyles.toolbarButton))
                _currentTab = Tab.DOT;

            GUILayout.FlexibleSpace();

            // Tag 筛选（仅 Buff 页）
            if (_currentTab == Tab.Buff)
            {
                EditorGUILayout.LabelField("Tag:", GUILayout.Width(30));
                var tagOptions = new[] { "All", "Positive", "Negative", "Status" };
                int currentTagIdx = _tagFilter == -1 ? 0 : _tagFilter + 1;
                int newTagIdx = EditorGUILayout.Popup(currentTagIdx, tagOptions, EditorStyles.toolbarPopup, GUILayout.Width(80));
                _tagFilter = newTagIdx == 0 ? -1 : (newTagIdx - 1);
            }

            // 搜索
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(150));

            // 刷新
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(24)))
                Refresh();

            EditorGUILayout.EndHorizontal();
        }

        // ──────────────────── Buff Table ────────────────────

        private void DrawBuffTable()
        {
            var headers = new[] { "BuffId", "名称", "Tag", "Duration", "MoveSpd", "AtkIntv", "DmgTaken", "BulletCnt", "Stack" };
            DrawSortableHeader(headers);

            var filtered = _allBuffs.AsEnumerable();

            // Tag 过滤
            if (_tagFilter != -1)
                filtered = filtered.Where(b => (int)b.Tag == _tagFilter);

            // 名称搜索
            if (!string.IsNullOrEmpty(_searchText))
                filtered = filtered.Where(b => b.DisplayName != null && b.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                                            || b.name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            // 排序
            var sorted = SortBuffs(filtered, _sortColumn, _sortAscending).ToList();

            foreach (var buff in sorted)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(buff.BuffId.ToString(), EditorStyles.label, GUILayout.Width(50)))
                    EditorGUIUtility.PingObject(buff);
                GUILayout.Label(buff.DisplayName ?? buff.name, GUILayout.Width(120));
                GUILayout.Label(buff.Tag.ToString(), GUILayout.Width(70));
                GUILayout.Label(buff.Duration == 0 ? "∞" : $"{buff.Duration:F1}s", GUILayout.Width(60));
                GUILayout.Label(FormatMod(buff.MoveSpeedModifier), GUILayout.Width(60));
                GUILayout.Label(FormatMod(buff.AttackIntervalModifier), GUILayout.Width(60));
                GUILayout.Label(FormatMod(buff.DamageTakenModifier), GUILayout.Width(60));
                GUILayout.Label(FormatMod(buff.BulletCountModifier), GUILayout.Width(60));
                GUILayout.Label($"{buff.StackMode}({buff.MaxStacks})", GUILayout.Width(80));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField($"共 {sorted.Count} 条", EditorStyles.centeredGreyMiniLabel);
        }

        // ──────────────────── DOT Table ────────────────────

        private void DrawDotTable()
        {
            var headers = new[] { "DotId", "名称", "Damage", "Interval", "Duration", "DPS" };
            DrawSortableHeader(headers);

            var filtered = _allDots.AsEnumerable();
            if (!string.IsNullOrEmpty(_searchText))
                filtered = filtered.Where(d => d.DisplayName != null && d.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                                            || d.name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            var sorted = SortDots(filtered, _sortColumn, _sortAscending).ToList();

            foreach (var dot in sorted)
            {
                float dps = dot.Interval > 0 ? dot.DamagePerTick / dot.Interval : 0f;

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(dot.DotId.ToString(), EditorStyles.label, GUILayout.Width(50)))
                    EditorGUIUtility.PingObject(dot);
                GUILayout.Label(dot.DisplayName ?? dot.name, GUILayout.Width(120));
                GUILayout.Label(dot.DamagePerTick.ToString(), GUILayout.Width(60));
                GUILayout.Label($"{dot.Interval:F2}s", GUILayout.Width(60));
                GUILayout.Label($"{dot.Duration:F1}s", GUILayout.Width(60));
                GUILayout.Label($"{dps:F1}", GUILayout.Width(60));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField($"共 {sorted.Count} 条", EditorStyles.centeredGreyMiniLabel);
        }

        // ──────────────────── Helpers ────────────────────

        private void DrawSortableHeader(string[] headers)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < headers.Length; i++)
            {
                string label = headers[i];
                if (i == _sortColumn)
                    label += _sortAscending ? " ▲" : " ▼";

                if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(i < 2 ? (i == 0 ? 50 : 120) : 60)))
                {
                    if (_sortColumn == i)
                        _sortAscending = !_sortAscending;
                    else
                    {
                        _sortColumn = i;
                        _sortAscending = true;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string FormatMod(float mod)
        {
            if (Mathf.Approximately(mod, 1f)) return "-";
            return $"×{mod:F2}";
        }

        private static IEnumerable<BuffConfigSO> SortBuffs(IEnumerable<BuffConfigSO> source, int col, bool asc)
        {
            Func<BuffConfigSO, object> key = col switch
            {
                0 => b => b.BuffId,
                1 => b => b.DisplayName ?? b.name,
                2 => b => (int)b.Tag,
                3 => b => b.Duration,
                4 => b => b.MoveSpeedModifier,
                5 => b => b.AttackIntervalModifier,
                6 => b => b.DamageTakenModifier,
                7 => b => b.BulletCountModifier,
                8 => b => b.MaxStacks,
                _ => b => b.BuffId,
            };
            return asc ? source.OrderBy(key) : source.OrderByDescending(key);
        }

        private static IEnumerable<DotConfigSO> SortDots(IEnumerable<DotConfigSO> source, int col, bool asc)
        {
            Func<DotConfigSO, object> key = col switch
            {
                0 => d => d.DotId,
                1 => d => d.DisplayName ?? d.name,
                2 => d => d.DamagePerTick,
                3 => d => d.Interval,
                4 => d => d.Duration,
                5 => d => d.Interval > 0 ? d.DamagePerTick / d.Interval : 0f,
                _ => d => d.DotId,
            };
            return asc ? source.OrderBy(key) : source.OrderByDescending(key);
        }
    }
}
