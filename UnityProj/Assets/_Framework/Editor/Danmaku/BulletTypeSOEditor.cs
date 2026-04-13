using UnityEditor;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Editor.Danmaku
{
    [CustomEditor(typeof(BulletTypeSO))]
    public class BulletTypeSOEditor : UnityEditor.Editor
    {
        // 仅在 UseVisualAnimation 勾选时显示的字段名
        private static readonly System.Collections.Generic.HashSet<string> _animFields =
            new System.Collections.Generic.HashSet<string>
            {
                "ScaleOverLifetime",
                "AlphaOverLifetime",
                "ColorOverLifetime",
            };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var useAnim = serializedObject.FindProperty("UseVisualAnimation");
            bool animEnabled = useAnim != null && useAnim.boolValue;

            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // 跳过默认的 m_Script 字段以外的所有属性按需绘制
                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator, true);
                    continue;
                }

                // 动画字段：仅在启用时显示
                if (_animFields.Contains(iterator.name))
                {
                    if (animEnabled)
                        EditorGUILayout.PropertyField(iterator, true);
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
