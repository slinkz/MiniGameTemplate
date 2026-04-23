using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// UnityEngine.Object 安全销毁工具。
    /// Play Mode 用 Destroy，Edit Mode 用 DestroyImmediate，避免编辑器预热/刷新流程报错。
    /// </summary>
    internal static class UnityObjectDestroyUtility
    {
        internal static void Destroy(Object obj)
        {
            if (obj == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif

            Object.Destroy(obj);
        }
    }
}
