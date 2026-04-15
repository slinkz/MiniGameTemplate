using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Rendering;

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

            var bulletType = (BulletTypeSO)target;

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

                // UVRect 字段：添加"选择子图"按钮
                if (iterator.name == "UVRect")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    DrawSubSpriteButton(bulletType, iterator);
                    continue;
                }

                // ExplosionAtlasUV 字段：同样添加按钮
                if (iterator.name == "ExplosionAtlasUV")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    DrawExplosionSubSpriteButton(bulletType);
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSubSpriteButton(BulletTypeSO bulletType, SerializedProperty uvRectProp)
        {
            // 只有在有贴图时才显示按钮
            Texture2D displayTex = null;
            AtlasMappingSO atlas = null;
            int cols = 1, rows = 1;

            if (bulletType.AtlasBinding != null && bulletType.AtlasBinding.AtlasTexture != null)
            {
                displayTex = bulletType.AtlasBinding.AtlasTexture;
                atlas = bulletType.AtlasBinding;
            }
            else if (bulletType.SourceTexture != null)
            {
                displayTex = bulletType.SourceTexture;
            }

            if (displayTex == null) return;

            if (bulletType.SamplingMode == BulletSamplingMode.SpriteSheet && atlas == null)
            {
                cols = Mathf.Max(1, bulletType.SheetColumns);
                rows = Mathf.Max(1, bulletType.SheetRows);
            }

            if (GUILayout.Button("🔍 选择子图", GUILayout.Height(20)))
            {
                var currentUV = bulletType.UVRect;
                Rendering.AtlasSubSpritePopup.Show(displayTex, atlas, cols, rows, currentUV, (newUV) =>
                {
                    Undo.RecordObject(bulletType, "Select Sub-Sprite UV");
                    bulletType.UVRect = newUV;
                    EditorUtility.SetDirty(bulletType);
                });
            }
        }

        private void DrawExplosionSubSpriteButton(BulletTypeSO bulletType)
        {
            Texture2D displayTex = null;
            AtlasMappingSO atlas = null;

            if (bulletType.AtlasBinding != null && bulletType.AtlasBinding.AtlasTexture != null)
            {
                displayTex = bulletType.AtlasBinding.AtlasTexture;
                atlas = bulletType.AtlasBinding;
            }
            else if (bulletType.SourceTexture != null)
            {
                displayTex = bulletType.SourceTexture;
            }

            if (displayTex == null) return;

            if (GUILayout.Button("🔍 选择爆炸子图", GUILayout.Height(20)))
            {
                var currentUV = bulletType.ExplosionAtlasUV;
                Rendering.AtlasSubSpritePopup.Show(displayTex, atlas, 1, 1, currentUV, (newUV) =>
                {
                    Undo.RecordObject(bulletType, "Select Explosion Sub-Sprite UV");
                    bulletType.ExplosionAtlasUV = newUV;
                    EditorUtility.SetDirty(bulletType);
                });
            }
        }
    }
}
