#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// AIBehaviorSO 自定义 Inspector——Phase 1 最小版。
    /// ET-005: 每个 AIBehaviorEntry 列表元素标题显示可读摘要。
    /// WF-005: 缺少 Always 兜底条目时底部红色 HelpBox。
    /// </summary>
    [CustomEditor(typeof(AIBehaviorSO))]
    public class AIBehaviorSOEditor : UnityEditor.Editor
    {
        private ReorderableList _list;
        private SerializedProperty _entries;

        private void OnEnable()
        {
            _entries = serializedObject.FindProperty("Entries");
            _list = new ReorderableList(serializedObject, _entries, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                elementHeightCallback = ElementHeight
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _list.DoLayoutList();

            // WF-005: 检查最后一条是否为 Always
            if (_entries.arraySize > 0)
            {
                var lastEntry = _entries.GetArrayElementAtIndex(_entries.arraySize - 1);
                var condProp = lastEntry.FindPropertyRelative("Condition");
                if (condProp != null && (AIConditionType)condProp.enumValueIndex != AIConditionType.Always)
                {
                    EditorGUILayout.HelpBox(
                        "警告：条件表缺少 Always 兜底条目。运行时将默认 Idle，建议显式配置。",
                        MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "条件-动作表为空！请至少添加一条 Always → Idle 兜底条目。",
                    MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "条件-动作表（优先级从上到下）");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _entries.GetArrayElementAtIndex(index);
            rect.y += 2;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // 第一行：可读摘要标题
            var summaryRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
            EditorGUI.LabelField(summaryRect, FormatSummary(element, index), EditorStyles.boldLabel);

            // 第二行起：展开编辑
            rect.y += lineHeight + spacing;

            var condProp = element.FindPropertyRelative("Condition");
            var paramProp = element.FindPropertyRelative("ConditionParam");
            var actionProp = element.FindPropertyRelative("Action");
            var actionParamProp = element.FindPropertyRelative("ActionParam");

            float fieldWidth = (rect.width - 12) / 4f;
            var r = new Rect(rect.x, rect.y, fieldWidth, lineHeight);
            EditorGUI.PropertyField(r, condProp, GUIContent.none);
            r.x += fieldWidth + 4;
            EditorGUI.PropertyField(r, paramProp, GUIContent.none);
            r.x += fieldWidth + 4;
            EditorGUI.PropertyField(r, actionProp, GUIContent.none);
            r.x += fieldWidth + 4;
            EditorGUI.PropertyField(r, actionParamProp, GUIContent.none);
        }

        private float ElementHeight(int index)
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + 6;
        }

        private string FormatSummary(SerializedProperty element, int index)
        {
            var cond = (AIConditionType)element.FindPropertyRelative("Condition").enumValueIndex;
            float condParam = element.FindPropertyRelative("ConditionParam").floatValue;
            var action = (AIActionType)element.FindPropertyRelative("Action").enumValueIndex;
            float actionParam = element.FindPropertyRelative("ActionParam").floatValue;

            string condStr = cond switch
            {
                AIConditionType.Always => "Always",
                AIConditionType.HpBelow => $"HP < {condParam:P0}",
                AIConditionType.TargetInRange => $"TargetInRange ({condParam:F1})",
                AIConditionType.TargetLost => "TargetLost",
                _ => cond.ToString()
            };

            string actionStr = actionParam > 0 ? $"{action} ({actionParam:F1})" : action.ToString();
            return $"[{index}] {condStr} → {actionStr}";
        }
    }
}
#endif
