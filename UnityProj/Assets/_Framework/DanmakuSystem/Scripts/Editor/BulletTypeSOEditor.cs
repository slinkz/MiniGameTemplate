using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Danmaku.Editor
{
    /// <summary>
    /// BulletTypeSO 自定义 Inspector——按 MotionType / TrailMode 条件显示相关字段，
    /// 减少认知噪音，设计师只看到当前模式下有用的参数。
    /// </summary>
    [CustomEditor(typeof(BulletTypeSO))]
    public class BulletTypeSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var prop = serializedObject.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                // 跳过脚本字段
                if (prop.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(prop);
                    continue;
                }

                // ── SineWave 参数：仅 MotionType == SineWave 时显示 ──
                if (prop.name == "SineAmplitude" || prop.name == "SineFrequency")
                {
                    var motionType = (MotionType)serializedObject.FindProperty("MotionType").intValue;
                    if (motionType != MotionType.SineWave) continue;
                }

                // ── Spiral 参数：仅 MotionType == Spiral 时显示 ──
                if (prop.name == "SpiralAngularVelocity")
                {
                    var motionType = (MotionType)serializedObject.FindProperty("MotionType").intValue;
                    if (motionType != MotionType.Spiral) continue;
                }

                // ── Ghost 拖尾参数：仅 Trail 包含 Ghost 时显示 ──
                if (prop.name == "GhostCount" || prop.name == "GhostInterval")
                {
                    var trailMode = (TrailMode)serializedObject.FindProperty("Trail").intValue;
                    if (trailMode != TrailMode.Ghost && trailMode != TrailMode.Both) continue;
                }

                // ── Trail 条带参数：仅 Trail 包含 Trail 时显示 ──
                if (prop.name == "TrailPointCount" || prop.name == "TrailWidth"
                    || prop.name == "TrailWidthCurve" || prop.name == "TrailColor")
                {
                    var trailMode = (TrailMode)serializedObject.FindProperty("Trail").intValue;
                    if (trailMode != TrailMode.Trail && trailMode != TrailMode.Both) continue;
                }

                EditorGUILayout.PropertyField(prop, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
