using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Editor.Danmaku
{
    [CustomEditor(typeof(LaserTypeSO))]
    public class LaserTypeSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawOverview();
            EditorGUILayout.Space(6f);

            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            DrawFieldNotes();

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawOverview()
        {
            EditorGUILayout.HelpBox(
                "LaserTypeSO 用来配置激光的贴图、颜色、宽度曲线、阶段时长、伤害与碰撞响应。\n\n" +
                "重点：CoreColor = 激光主体主色，并且当前也会作为 Charging 阶段预警线颜色。\n" +
                "WidthProfile = 沿激光长度的宽度分布；WidthOverLifetime = 沿生命周期时间的宽度变化。",
                MessageType.Info);
        }

        private static void DrawFieldNotes()
        {
            EditorGUILayout.LabelField("字段说明", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "LaserTexture：激光贴图，沿长度方向滚动 UV。\n" +
                "UVScrollSpeed：贴图滚动速度，值越大流动越快。\n" +
                "CoreColor：激光主体主色；当前版本也用于预警线颜色。\n" +
                "EdgeColor：设计意图是边缘色，但当前版本尚未完整接入激光本体的可见渐变，先视为保留字段。\n" +
                "WidthProfile：沿激光长度采样，控制中段粗/两端细等形状。\n" +
                "WidthOverLifetime：沿时间采样，控制激光从蓄力到消散的整体宽度变化。\n" +
                "MaxWidth：激光最大世界宽度，最终宽度会乘上 WidthOverLifetime。\n" +
                "ChargeDuration：蓄力阶段，仅显示预警线，不造成伤害。\n" +
                "FiringDuration：发射阶段，激光造成伤害。\n" +
                "FadeDuration：消散阶段，视觉渐隐。\n" +
                "DamagePerTick / TickInterval：持续伤害数值与判定间隔。\n" +
                "OnHitObstacle / OnHitScreenEdge / MaxReflections：控制激光遇到障碍物、屏幕边缘后的行为。",
                MessageType.None);

            EditorGUILayout.HelpBox(
                "当前实现说明：\n" +
                "1. CoreColor 不只是“核心区域颜色”，它还是预警线颜色来源。\n" +
                "2. EdgeColor 当前不是完全无效，但还没有形成设计师直觉中的“明显边缘渐变控制”，不要把它理解成一定会直接改出清晰边框。",
                MessageType.Warning);
        }
    }
}
