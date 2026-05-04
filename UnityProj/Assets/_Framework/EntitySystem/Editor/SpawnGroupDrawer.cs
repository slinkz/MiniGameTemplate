// ============================================================
// SpawnGroupDrawer.cs — SpawnGroup 自定义 Inspector 绘制
// 功能：
//   1. 在 Formation 枚举下方显示阵型效果简介（HelpBox）
//   2. 根据所选阵型只显示相关的 FormationParams 字段
//   3. 每个字段附带中文描述 Label
// ============================================================
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.Entity.Editor
{
    [CustomPropertyDrawer(typeof(SpawnGroup))]
    public class SpawnGroupDrawer : PropertyDrawer
    {
        // 阵型效果简介
        private static readonly string[] FormationDescriptions = new string[]
        {
            // Random
            "【随机散布】在散布半径范围内随机生成（半径=0 则用 SpawnPoint.AreaRadius），适合小股杂兵。",
            // Line
            "【直线阵型】沿指定角度方向等间距排列，适合编队冲锋、弹幕墙。",
            // Circle
            "【环形阵型】沿圆周等角度分布，适合包围战术、Boss 召唤。",
            // Grid
            "【网格阵型】行列网格排列并居中，适合密集方阵、塔防编队。",
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight + 2f; // Foldout

            // 基本字段：EntityConfig, Camp, Count, SpawnInterval, Formation
            height += (EditorGUIUtility.singleLineHeight + 2f) * 5;

            // HelpBox for formation description (~2 lines)
            height += 38f;

            // FormationParams 字段（根据阵型类型决定数量）
            var formation = (SpawnFormation)property.FindPropertyRelative("Formation").enumValueIndex;
            height += GetFormationParamsHeight(formation);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            Rect rect = new Rect(position.x, position.y, position.width, lineH);

            // Foldout
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            rect.y += lineH + spacing;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // EntityConfig
                var entityConfigProp = property.FindPropertyRelative("EntityConfig");
                EditorGUI.PropertyField(rect, entityConfigProp, new GUIContent("怪种配置"));
                rect.y += lineH + spacing;

                // Camp
                var campProp = property.FindPropertyRelative("Camp");
                EditorGUI.PropertyField(rect, campProp, new GUIContent("阵营"));
                rect.y += lineH + spacing;

                // Count
                var countProp = property.FindPropertyRelative("Count");
                EditorGUI.PropertyField(rect, countProp, new GUIContent("数量"));
                rect.y += lineH + spacing;

                // SpawnInterval
                var intervalProp = property.FindPropertyRelative("SpawnInterval");
                EditorGUI.PropertyField(rect, intervalProp, new GUIContent("生成间隔（秒）"));
                rect.y += lineH + spacing;

                // Formation
                var formationProp = property.FindPropertyRelative("Formation");
                EditorGUI.PropertyField(rect, formationProp, new GUIContent("阵型"));
                rect.y += lineH + spacing;

                // Formation HelpBox
                var formation = (SpawnFormation)formationProp.enumValueIndex;
                int descIndex = Mathf.Clamp((int)formation, 0, FormationDescriptions.Length - 1);
                Rect helpRect = new Rect(rect.x, rect.y, rect.width, 36f);
                EditorGUI.HelpBox(helpRect, FormationDescriptions[descIndex], MessageType.Info);
                rect.y += 38f;

                // FormationParams — 只显示与当前阵型相关的字段
                var paramsProp = property.FindPropertyRelative("FormationParams");
                DrawFormationParams(rect, paramsProp, formation);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private void DrawFormationParams(Rect startRect, SerializedProperty paramsProp, SpawnFormation formation)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            Rect rect = startRect;

            if (formation == SpawnFormation.Random)
            {
                // Random：散布半径 + 噪声
                DrawField(ref rect, paramsProp, "Radius", "散布半径",
                    "随机散布范围半径（世界单位）。0 = 使用 SpawnPoint 的 AreaRadius", lineH, spacing);
                var jitterProp = paramsProp.FindPropertyRelative("Jitter");
                EditorGUI.PropertyField(rect, jitterProp,
                    new GUIContent("随机噪声", "每个单位附加的随机偏移量（世界单位），使位置不完全随机均匀"));
                return;
            }

            switch (formation)
            {
                case SpawnFormation.Line:
                    DrawField(ref rect, paramsProp, "Spacing", "间距",
                        "相邻单位之间的距离（世界单位）。0 = 自动用 AreaRadius*2/(数量-1) 计算", lineH, spacing);
                    DrawField(ref rect, paramsProp, "Angle", "排列角度",
                        "排列方向角度（度）。0=水平排列、90=垂直排列、45=斜线", lineH, spacing);
                    break;

                case SpawnFormation.Circle:
                    DrawField(ref rect, paramsProp, "Radius", "圆半径",
                        "圆环半径（世界单位）。0 = 使用 SpawnPoint 的 AreaRadius", lineH, spacing);
                    DrawField(ref rect, paramsProp, "Angle", "起始角度",
                        "第一个单位的角度偏移（度）。0=右侧开始、90=上方开始", lineH, spacing);
                    break;

                case SpawnFormation.Grid:
                    DrawField(ref rect, paramsProp, "Spacing", "格间距",
                        "网格中相邻单位的间距（世界单位）。0 = 自动用 AreaRadius 计算", lineH, spacing);
                    DrawField(ref rect, paramsProp, "Columns", "列数",
                        "网格列数。0 = 自动取 ceil(√数量)", lineH, spacing);
                    break;
            }

            // Jitter — 所有非 Random 阵型都可用
            DrawField(ref rect, paramsProp, "Jitter", "随机噪声",
                "每个单位附加的随机偏移量，让阵型不那么死板", lineH, spacing);
        }

        private static void DrawField(ref Rect rect, SerializedProperty parent,
            string fieldName, string displayName, string tooltip, float lineH, float spacing)
        {
            var prop = parent.FindPropertyRelative(fieldName);
            EditorGUI.PropertyField(rect, prop, new GUIContent(displayName, tooltip));
            rect.y += lineH + spacing;
        }

        private float GetFormationParamsHeight(SpawnFormation formation)
        {
            float lineH = EditorGUIUtility.singleLineHeight + 2f;

            switch (formation)
            {
                case SpawnFormation.Random:
                    return lineH * 2; // Radius + Jitter
                case SpawnFormation.Line:
                    return lineH * 3; // Spacing + Angle + Jitter
                case SpawnFormation.Circle:
                    return lineH * 3; // Radius + Angle + Jitter
                case SpawnFormation.Grid:
                    return lineH * 3; // Spacing + Columns + Jitter
                default:
                    return lineH;
            }
        }
    }
}
