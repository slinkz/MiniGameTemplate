using MiniGameTemplate.Danmaku;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Editor.Danmaku
{
    [CustomEditor(typeof(BulletTypeSO))]
    public class BulletTypeSOEditor : UnityEditor.Editor
    {
        // 视觉动画字段名（需与 BulletTypeSO 字段名完全一致）
        private static readonly string[] AnimationFields =
        {
            "ScaleOverLifetime",
            "AlphaOverLifetime",
            "ColorOverLifetime"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // 跳过脚本引用（默认绘制）
                if (iterator.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator);
                    continue;
                }

                // 如果是动画字段，检查 UseVisualAnimation
                if (System.Array.IndexOf(AnimationFields, iterator.name) >= 0)
                {
                    SerializedProperty useAnim = serializedObject.FindProperty("UseVisualAnimation");
                    if (useAnim != null && !useAnim.boolValue)
                        continue; // 未勾选时跳过
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
