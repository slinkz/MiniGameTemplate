using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// T4 DPS 计算面板 — 裸 DPS + 被动期望 DPS + HP 预算对比。
    /// 菜单路径：Tools/ShooterGame/面板/DPS Calculator
    /// </summary>
    public class SG_DPSCalculatorWindow : EditorWindow
    {
        // ─── 输入 ───
        private EntityConfigSO _playerConfig;
        private SkillConfigSO[] _skillSlots = new SkillConfigSO[6];

        // ─── 被动模拟 Toggle ───
        private bool _simulateCrit;    // 暴击被动 PA-01
        private bool _simulatePierce;  // 穿透被动 PA-02
        private bool _simulateMagnet;  // 磁吸被动 PA-03（不影响 DPS）
        private bool _simulateTailgun; // 尾翼反击 PA-04

        // ─── 被动参数（来自 TDD） ───
        private const float CRIT_UPTIME = 0.33f;
        private const float CRIT_RATE = 0.15f;
        private const float CRIT_MULT = 2f;
        private const float PIERCE_UPTIME = 0.375f;
        private const float PIERCE_BONUS = 0.5f; // 穿透时平均多打 50% 目标
        private const float TAILGUN_DPS = 6f;     // PA-04 估算 DPS

        // ─── UI 状态 ───
        private Vector2 _scrollPos;
        private List<DPSEntry> _results = new List<DPSEntry>();
        private float _totalRawDPS;
        private float _totalExpectedDPS;

        [MenuItem("Tools/ShooterGame/面板/DPS Calculator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SG_DPSCalculatorWindow>("DPS Calculator");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("T4 DPS 计算面板", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // ─── 输入区 ───
            _playerConfig = (EntityConfigSO)EditorGUILayout.ObjectField(
                "玩家 EntityConfig", _playerConfig, typeof(EntityConfigSO), false);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("技能槽位", EditorStyles.miniLabel);
            for (int i = 0; i < _skillSlots.Length; i++)
            {
                _skillSlots[i] = (SkillConfigSO)EditorGUILayout.ObjectField(
                    $"  Slot {i + 1}", _skillSlots[i], typeof(SkillConfigSO), false);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("被动模拟", EditorStyles.miniLabel);
            _simulateCrit = EditorGUILayout.Toggle("  暴击被动 (PA-01)", _simulateCrit);
            _simulatePierce = EditorGUILayout.Toggle("  穿透被动 (PA-02)", _simulatePierce);
            _simulateMagnet = EditorGUILayout.Toggle("  磁吸被动 (PA-03, 不影响 DPS)", _simulateMagnet);
            _simulateTailgun = EditorGUILayout.Toggle("  尾翼反击 (PA-04)", _simulateTailgun);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("计算 DPS", GUILayout.Height(28)))
            {
                Calculate();
            }

            // ─── 结果区 ───
            if (_results.Count == 0) return;

            EditorGUILayout.Space(8);
            DrawResultsTable();
            DrawHPBudgetComparison();
        }

        private void Calculate()
        {
            _results.Clear();
            _totalRawDPS = 0f;
            _totalExpectedDPS = 0f;

            // 基础攻击 DPS
            if (_playerConfig != null)
            {
                float baseDPS = CalcBasicAttackDPS(_playerConfig);
                if (baseDPS > 0f)
                {
                    float expected = ApplyPassiveMultiplier(baseDPS);
                    _results.Add(new DPSEntry("基础攻击", baseDPS, expected, "10 dmg × 4/s"));
                    _totalRawDPS += baseDPS;
                    _totalExpectedDPS += expected;
                }
            }

            // 技能 DPS
            for (int i = 0; i < _skillSlots.Length; i++)
            {
                var skill = _skillSlots[i];
                if (skill == null) continue;

                float rawDPS = CalcSkillDPS(skill, out string detail);
                float expected = ApplyPassiveMultiplier(rawDPS);
                _results.Add(new DPSEntry(skill.DisplayName ?? skill.name, rawDPS, expected, detail));
                _totalRawDPS += rawDPS;
                _totalExpectedDPS += expected;
            }

            // 尾翼反击 DPS
            if (_simulateTailgun)
            {
                _results.Add(new DPSEntry("PA-04 尾翼反击", TAILGUN_DPS, TAILGUN_DPS, "被动触发"));
                _totalRawDPS += TAILGUN_DPS;
                _totalExpectedDPS += TAILGUN_DPS;
            }
        }

        private float CalcBasicAttackDPS(EntityConfigSO config)
        {
            float interval = config.AttackInterval;
            if (interval <= 0f) return 0f;

            var pattern = config.AttackBulletPattern;
            if (pattern == null) return 0f;

            var bulletType = pattern.BulletType;
            if (bulletType == null) return 0f;

            int damage = bulletType.Damage;
            int count = pattern.Count;
            float dps = (damage * count) / interval;
            return dps;
        }

        private float CalcSkillDPS(SkillConfigSO skill, out string detail)
        {
            detail = "";
            float cycleTime = skill.CooldownTime + skill.CastTime + skill.RecoveryTime;
            if (cycleTime <= 0f) cycleTime = 0.01f; // 防除零

            float totalDamage = 0f;

            if (skill.Effects == null) return 0f;

            foreach (var effect in skill.Effects)
            {
                if (effect is FireBulletsEffect bullets)
                {
                    var pattern = bullets.Pattern;
                    if (pattern != null)
                    {
                        var bt = pattern.BulletType;
                        if (bt != null)
                        {
                            int dmg = bt.Damage * pattern.Count * pattern.BurstCount;
                            totalDamage += dmg;
                            detail += $"{pattern.Count}×{bt.Damage}dmg ";
                        }
                    }
                }
                else if (effect is AreaDamageEffect area)
                {
                    totalDamage += area.BaseDamage;
                    detail += $"AOE {area.BaseDamage}dmg ";
                }
                else if (effect is FireLaserEffect laser)
                {
                    var lt = laser.LaserType;
                    if (lt != null)
                    {
                        float ticksPerFire = lt.FiringDuration / lt.TickInterval;
                        float laserDmg = lt.DamagePerTick * ticksPerFire;
                        totalDamage += laserDmg;
                        detail += $"Laser {lt.DamagePerTick}/tick×{ticksPerFire:F0}ticks ";
                    }
                }
            }

            float dps = totalDamage / cycleTime;
            detail += $"| cycle={cycleTime:F1}s";
            return dps;
        }

        private float ApplyPassiveMultiplier(float rawDPS)
        {
            float mult = 1f;
            if (_simulateCrit)
                mult += CRIT_UPTIME * CRIT_RATE * (CRIT_MULT - 1f); // ~+5%
            if (_simulatePierce)
                mult += PIERCE_UPTIME * PIERCE_BONUS; // ~+19%
            return rawDPS * mult;
        }

        private void DrawResultsTable()
        {
            EditorGUILayout.LabelField("─── DPS 计算结果 ───", EditorStyles.boldLabel);

            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("来源", EditorStyles.miniLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("裸 DPS", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("期望 DPS", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("详情", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 数据行
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(200));
            foreach (var entry in _results)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.Name, GUILayout.Width(140));
                EditorGUILayout.LabelField($"{entry.RawDPS:F1}/s", GUILayout.Width(70));
                EditorGUILayout.LabelField($"{entry.ExpectedDPS:F1}/s", GUILayout.Width(70));
                EditorGUILayout.LabelField(entry.Detail, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            // 汇总
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总计", EditorStyles.boldLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField($"{_totalRawDPS:F1}/s", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField($"{_totalExpectedDPS:F1}/s", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHPBudgetComparison()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("─── HP 预算 vs 理论清场时间 ───", EditorStyles.boldLabel);

            if (_totalExpectedDPS <= 0f)
            {
                EditorGUILayout.HelpBox("DPS 为零，无法计算 HP 预算。", MessageType.Warning);
                return;
            }

            // 5 关的敌机 HP 预算（估算）
            var levelData = new (string Name, int TotalHP, string ExpectedTime)[]
            {
                ("关卡 1 (18 普通)", (int)(18 * 20 * 1f), "45-60s"),
                ("关卡 2 (18 混合)", (int)(14 * 20 * 1f + 4 * 20 * 1f), "60-75s"),
                ("关卡 3 (28 含射手)", (int)(11 * 20 * 1.2f + 5 * 20 * 1.2f + 3 * 40 * 1.2f), "75-90s"),
                ("关卡 4 (33 含散射)", (int)(11 * 20 * 1.5f + 5 * 20 * 1.5f + 7 * 40 * 1.5f + 3 * 60 * 1.5f), "90-120s"),
                ("关卡 5 (52 含精英)", (int)(3 * 20 * 2f + 10 * 20 * 2f + 8 * 40 * 2f + 6 * 60 * 2f + 3 * 120 * 2f), "120-150s"),
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("关卡", EditorStyles.miniLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField("总 HP", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("理论清场", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("设计预期", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("差异", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            foreach (var (name, totalHP, expectedTime) in levelData)
            {
                float theoreticalTime = totalHP / _totalExpectedDPS;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, GUILayout.Width(180));
                EditorGUILayout.LabelField($"{totalHP}", GUILayout.Width(70));
                EditorGUILayout.LabelField($"{theoreticalTime:F1}s", GUILayout.Width(70));
                EditorGUILayout.LabelField(expectedTime, GUILayout.Width(80));

                // 简单差异指示
                string diff = theoreticalTime < 45f ? "⚡ 过快" :
                              theoreticalTime > 180f ? "⚠ 过慢" : "✓";
                EditorGUILayout.LabelField(diff, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }
        }

        private struct DPSEntry
        {
            public string Name;
            public float RawDPS;
            public float ExpectedDPS;
            public string Detail;

            public DPSEntry(string name, float rawDPS, float expectedDPS, string detail)
            {
                Name = name;
                RawDPS = rawDPS;
                ExpectedDPS = expectedDPS;
                Detail = detail;
            }
        }
    }
}
