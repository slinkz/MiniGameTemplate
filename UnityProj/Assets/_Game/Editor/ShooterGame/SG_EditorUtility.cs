using UnityEngine;
using UnityEditor;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// ShooterGame 编辑器公共工具方法。
    /// PT-003 假设边界：项目 SO 规模 &lt; 100 时 FindAssets 性能无忧。
    /// 注意 FindAssets 是子串匹配——"SG_BaseHP" 也会匹配 "SG_BaseHP_v2"。
    /// V1 靠命名唯一性保证（21 个 SO 无重名前缀），V2 改用 GUID 查找。
    /// TDD: SG_TOOLS_TDD_02 §2.1
    /// </summary>
    public static class SG_EditorUtility
    {
        /// <summary>
        /// 按名称查找指定类型的 ScriptableObject 资产。
        /// </summary>
        public static T FindSOByName<T>(string name) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name} {name}");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
