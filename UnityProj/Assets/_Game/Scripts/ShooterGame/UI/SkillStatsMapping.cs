using System.Collections.Generic;
using UnityEngine;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 技能伤害统计映射（TDD_05 S5.5 / PK-R4 DE-008）。
    /// sourceTag → (displayName, iconKey, barColor) 硬编码映射表。
    /// V2 技能数量固定，V3 可改为 SO 驱动。
    /// </summary>
    public static class SkillStatsMapping
    {
        public struct SkillDisplayInfo
        {
            public string Name;
            public string IconKey;
            public Color BarColor;
        }

        private static readonly Dictionary<int, SkillDisplayInfo> s_Mapping = new()
        {
            { 0, new SkillDisplayInfo { Name = "基础攻击", IconKey = "icon_basic_atk", BarColor = new Color(0.47f, 0.56f, 0.61f) } },       // #78909C
            { 1, new SkillDisplayInfo { Name = "散射弹幕", IconKey = "icon_sk_p01", BarColor = new Color(0.31f, 0.76f, 0.97f) } },            // #4FC3F7
            { 2, new SkillDisplayInfo { Name = "追踪导弹", IconKey = "icon_sk_p02", BarColor = new Color(1f, 0.44f, 0.26f) } },               // #FF7043
            { 3, new SkillDisplayInfo { Name = "护盾", IconKey = "icon_sk_p03", BarColor = new Color(0.67f, 0.28f, 0.74f) } },                // #AB47BC
            { 4, new SkillDisplayInfo { Name = "激光", IconKey = "icon_sk_p04", BarColor = new Color(1f, 0.65f, 0.15f) } },                   // #FFA726
            { 5, new SkillDisplayInfo { Name = "火力全开", IconKey = "icon_sk_p05", BarColor = new Color(0.98f, 0.93f, 0.35f) } },            // #F9EE59
            { 6, new SkillDisplayInfo { Name = "反击弹幕", IconKey = "icon_sk_p06", BarColor = new Color(0.4f, 0.73f, 0.42f) } },             // #66BB6A
            { 7, new SkillDisplayInfo { Name = "反击弹幕", IconKey = "icon_pa04", BarColor = new Color(0.94f, 0.33f, 0.31f) } },              // #EF5350 (PA-04)
        };

        // DOT 范围 4001~4003
        private static readonly SkillDisplayInfo s_DotDefault = new()
        {
            Name = "持续伤害",
            IconKey = "icon_dot",
            BarColor = new Color(0.6f, 0.2f, 0.8f) // 紫色
        };

        private static readonly SkillDisplayInfo s_OtherDefault = new()
        {
            Name = "其他",
            IconKey = "icon_other",
            BarColor = new Color(0.62f, 0.62f, 0.62f) // #9E9E9E
        };

        /// <summary>
        /// 根据 sourceTagId 获取显示信息。
        /// </summary>
        public static SkillDisplayInfo GetDisplayInfo(int sourceTag)
        {
            if (s_Mapping.TryGetValue(sourceTag, out var info))
                return info;

            // DOT 范围
            if (sourceTag >= 4000 && sourceTag <= 4999)
                return s_DotDefault;

            return s_OtherDefault;
        }

        /// <summary>
        /// "其他"合并条的显示信息。
        /// </summary>
        public static SkillDisplayInfo OtherInfo => s_OtherDefault;
    }
}
