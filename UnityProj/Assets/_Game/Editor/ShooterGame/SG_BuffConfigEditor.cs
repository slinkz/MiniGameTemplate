using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// BuffConfigSO CustomEditor（TDD_05 S5.8b / PK-R2 ET-005）。
    /// - 根据 Tag 智能显隐字段
    /// - HelpBox 即时反馈（Duration/ID 范围/ID 冲突）
    /// - 折叠式 StatModifiers 预览区
    /// </summary>
    [CustomEditor(typeof(BuffConfigSO))]
    public class SG_BuffConfigEditor : UnityEditor.Editor
    {
        private const int BUFF_ID_MIN = 1000;
        private const int BUFF_ID_MAX = 3999;

        private bool _showModPreview = true;

        // 序列化属性缓存
        private SerializedProperty _displayName;
        private SerializedProperty _buffId;
        private SerializedProperty _tag;
        private SerializedProperty _stackMode;
        private SerializedProperty _maxStacks;
        private SerializedProperty _duration;
        private SerializedProperty _moveSpeedMod;
        private SerializedProperty _atkIntervalMod;
        private SerializedProperty _dmgTakenMod;
        private SerializedProperty _bulletCountMod;
        private SerializedProperty _grantsPierce;
        private SerializedProperty _critRateBonus;
        private SerializedProperty _critMultiplierOverride;
        private SerializedProperty _pickupRadiusMod;
        private SerializedProperty _vfxPrefab;

        private void OnEnable()
        {
            _displayName = serializedObject.FindProperty("DisplayName");
            _buffId = serializedObject.FindProperty("BuffId");
            _tag = serializedObject.FindProperty("Tag");
            _stackMode = serializedObject.FindProperty("StackMode");
            _maxStacks = serializedObject.FindProperty("MaxStacks");
            _duration = serializedObject.FindProperty("Duration");
            _moveSpeedMod = serializedObject.FindProperty("MoveSpeedModifier");
            _atkIntervalMod = serializedObject.FindProperty("AttackIntervalModifier");
            _dmgTakenMod = serializedObject.FindProperty("DamageTakenModifier");
            _bulletCountMod = serializedObject.FindProperty("BulletCountModifier");
            _grantsPierce = serializedObject.FindProperty("GrantsPierce");
            _critRateBonus = serializedObject.FindProperty("CritRateBonus");
            _critMultiplierOverride = serializedObject.FindProperty("CritMultiplierOverride");
            _pickupRadiusMod = serializedObject.FindProperty("PickupRadiusModifier");
            _vfxPrefab = serializedObject.FindProperty("VfxPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var buff = (BuffConfigSO)target;

            // ── 基础 ──
            EditorGUILayout.LabelField("基础", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_buffId);

            // HelpBox: ID 范围
            if (buff.BuffId > 0 && (buff.BuffId < BUFF_ID_MIN || buff.BuffId > BUFF_ID_MAX))
            {
                EditorGUILayout.HelpBox($"BuffId={buff.BuffId} 超出范围 [{BUFF_ID_MIN},{BUFF_ID_MAX}]", MessageType.Error);
            }

            // HelpBox: ID 冲突检查
            CheckIdConflict(buff);

            EditorGUILayout.Space(4);

            // ── 分类与叠加 ──
            EditorGUILayout.LabelField("分类与叠加", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tag);
            EditorGUILayout.PropertyField(_stackMode);
            if (buff.StackMode == StackMode.Stack)
                EditorGUILayout.PropertyField(_maxStacks);

            EditorGUILayout.Space(4);

            // ── 持续时间 ──
            EditorGUILayout.LabelField("持续时间", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_duration);
            if (buff.Duration < 0f)
                EditorGUILayout.HelpBox("Duration 必须 ≥ 0", MessageType.Error);
            if (buff.Duration == 0f)
                EditorGUILayout.HelpBox("Duration=0 → 永久 Buff（不会自动过期）", MessageType.Info);

            EditorGUILayout.Space(4);

            // ── 属性修正（所有 Tag 共有）──
            EditorGUILayout.LabelField("属性修正（乘法）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_moveSpeedMod);
            EditorGUILayout.PropertyField(_atkIntervalMod);
            EditorGUILayout.PropertyField(_dmgTakenMod);
            EditorGUILayout.PropertyField(_bulletCountMod);

            EditorGUILayout.Space(4);

            // ── 被动/特殊效果（仅 Positive 或明确需要时）──
            bool isPositive = buff.Tag == BuffTag.Positive;
            if (isPositive)
            {
                EditorGUILayout.LabelField("被动/特殊效果", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_grantsPierce);
                EditorGUILayout.PropertyField(_critRateBonus);
                EditorGUILayout.PropertyField(_critMultiplierOverride);
                EditorGUILayout.PropertyField(_pickupRadiusMod);
            }

            EditorGUILayout.Space(4);

            // ── VFX ──
            EditorGUILayout.LabelField("视觉效果", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_vfxPrefab);

            EditorGUILayout.Space(8);

            // ── 折叠式预览区 ──
            _showModPreview = EditorGUILayout.Foldout(_showModPreview, "数值效果预览（只读）", true);
            if (_showModPreview)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = false;

                DrawModLine("移速", buff.MoveSpeedModifier);
                DrawModLine("攻击间隔", buff.AttackIntervalModifier);
                DrawModLine("受伤倍率", buff.DamageTakenModifier);
                DrawModLine("弹丸数", buff.BulletCountModifier);

                if (isPositive)
                {
                    if (buff.GrantsPierce) EditorGUILayout.LabelField("🔷 穿透弹");
                    if (buff.CritRateBonus > 0) EditorGUILayout.LabelField($"🔷 暴击率 +{buff.CritRateBonus * 100:F0}%");
                    if (buff.CritMultiplierOverride > 0) EditorGUILayout.LabelField($"🔷 暴击倍率 → {buff.CritMultiplierOverride:F1}x");
                    if (buff.PickupRadiusModifier != 1f) EditorGUILayout.LabelField($"🔷 拾取范围 ×{buff.PickupRadiusModifier:F1}");
                }

                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawModLine(string label, float mod)
        {
            if (Mathf.Approximately(mod, 1f))
                EditorGUILayout.LabelField(label, "—（无变化）");
            else
            {
                float pct = (mod - 1f) * 100f;
                string sign = pct >= 0 ? "+" : "";
                EditorGUILayout.LabelField(label, $"×{mod:F2}（{sign}{pct:F0}%）");
            }
        }

        private void CheckIdConflict(BuffConfigSO current)
        {
            if (current.BuffId <= 0) return;

            var guids = AssetDatabase.FindAssets("t:BuffConfigSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var other = AssetDatabase.LoadAssetAtPath<BuffConfigSO>(path);
                if (other == null || other == current) continue;
                if (other.BuffId == current.BuffId)
                {
                    EditorGUILayout.HelpBox($"BuffId={current.BuffId} 与 '{other.name}' 冲突！", MessageType.Warning);
                    return;
                }
            }
        }
    }
}
