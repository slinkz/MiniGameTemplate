using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Entity.Editor
{
    /// <summary>
    /// SkillConfigSO 自定义编辑器（v0.4 ATK-001）。
    /// 使用 TypeCache.GetTypesDerivedFrom&lt;ISkillEffect&gt;() 实现类型发现。
    /// 提供"+"添加、"-"删除 ISkillEffect。
    /// 不依赖 Odin——零第三方 Editor 依赖。
    /// 
    /// 搜索所有已加载程序集（包含 _Game/ 下的 Assembly Definition），
    /// 框架外扩展的 ISkillEffect 实现会自动出现在下拉菜单中。
    /// </summary>
    [CustomEditor(typeof(SkillConfigSO))]
    public class SkillConfigSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _displayName;
        private SerializedProperty _triggerMode;
        private SerializedProperty _cooldownTime;
        private SerializedProperty _castTime;
        private SerializedProperty _recoveryTime;
        private SerializedProperty _effects;

        private static Type[] _cachedEffectTypes;
        private static string[] _cachedEffectNames;

        private void OnEnable()
        {
            _displayName = serializedObject.FindProperty("DisplayName");
            _triggerMode = serializedObject.FindProperty("TriggerMode");
            _cooldownTime = serializedObject.FindProperty("CooldownTime");
            _castTime = serializedObject.FindProperty("CastTime");
            _recoveryTime = serializedObject.FindProperty("RecoveryTime");
            _effects = serializedObject.FindProperty("Effects");

            RefreshEffectTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── 基础 ──
            EditorGUILayout.LabelField("基础", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_triggerMode);

            EditorGUILayout.Space(4);

            // ── 时间轴 ──
            EditorGUILayout.LabelField("时间轴", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cooldownTime);
            EditorGUILayout.PropertyField(_castTime);
            EditorGUILayout.PropertyField(_recoveryTime);

            EditorGUILayout.Space(8);

            // ── 效果列表 ──
            EditorGUILayout.LabelField("效果列表", EditorStyles.boldLabel);
            DrawEffectsList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEffectsList()
        {
            for (int i = 0; i < _effects.arraySize; i++)
            {
                var element = _effects.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");

                // 标题行：类型名 + 删除按钮
                EditorGUILayout.BeginHorizontal();
                string typeName = GetManagedReferenceTypeName(element);
                EditorGUILayout.LabelField($"[{i}] {typeName}", EditorStyles.boldLabel);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    _effects.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                // 展开属性
                EditorGUI.indentLevel++;
                DrawChildProperties(element);
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // 添加按钮
            EditorGUILayout.Space(4);
            if (_cachedEffectTypes != null && _cachedEffectTypes.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ 添加效果", GUILayout.Width(120)))
                {
                    ShowAddEffectMenu();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("未找到 ISkillEffect 实现类。", MessageType.Info);
            }
        }

        private void ShowAddEffectMenu()
        {
            var menu = new GenericMenu();
            for (int i = 0; i < _cachedEffectTypes.Length; i++)
            {
                var type = _cachedEffectTypes[i];
                menu.AddItem(new GUIContent(_cachedEffectNames[i]), false, () =>
                {
                    serializedObject.Update();
                    int newIndex = _effects.arraySize;
                    _effects.InsertArrayElementAtIndex(newIndex);
                    var newElement = _effects.GetArrayElementAtIndex(newIndex);
                    newElement.managedReferenceValue = Activator.CreateInstance(type);
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        private static void DrawChildProperties(SerializedProperty property)
        {
            var iter = property.Copy();
            int depth = iter.depth;
            if (!iter.NextVisible(true)) return;

            do
            {
                if (iter.depth <= depth) break;
                EditorGUILayout.PropertyField(iter, true);
            }
            while (iter.NextVisible(false));
        }

        private static string GetManagedReferenceTypeName(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
                return "(null)";

            // Format: "Assembly FullTypeName"
            var parts = property.managedReferenceFullTypename.Split(' ');
            if (parts.Length >= 2)
            {
                var fullName = parts[1];
                int lastDot = fullName.LastIndexOf('.');
                return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
            }
            return property.managedReferenceFullTypename;
        }

        private static void RefreshEffectTypes()
        {
            _cachedEffectTypes = GetEffectTypes();
            _cachedEffectNames = _cachedEffectTypes.Select(t => t.Name).ToArray();
        }

        /// <summary>
        /// 使用 TypeCache 发现所有 ISkillEffect 实现类型（ATK-001）。
        /// 搜索所有已加载程序集（包含 _Game/ 下的 Assembly Definition），
        /// 框架外扩展的 ISkillEffect 实现会自动出现在下拉菜单中。
        /// </summary>
        private static Type[] GetEffectTypes()
        {
            return TypeCache.GetTypesDerivedFrom<ISkillEffect>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToArray();
        }
    }
}
