using FairyGUI;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Utility helpers for controlling Spine content embedded in FairyGUI GLoader3D.
    ///
    /// Usage prerequisites:
    /// 1) Spine runtime source linked into Assets (setup_spine script)
    /// 2) Scripting Define Symbols contain FAIRYGUI_SPINE
    /// </summary>
    public static class FairySpineHelper
    {
        /// <summary>
        /// Compile-time flag: true only when FAIRYGUI_SPINE is enabled.
        /// </summary>
        public static bool IsSpineFeatureEnabled
        {
            get
            {
#if FAIRYGUI_SPINE
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Configure and play a Spine animation on a named GLoader3D child.
        /// </summary>
        public static bool TryPlaySpine(GComponent root, string loaderName, string animationName, bool loop = true, string skinName = null)
        {
            if (root == null)
            {
                GameLog.LogWarning("[FairySpineHelper] Root component is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(loaderName))
            {
                GameLog.LogWarning("[FairySpineHelper] loaderName is empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(animationName))
            {
                GameLog.LogWarning("[FairySpineHelper] animationName is empty.");
                return false;
            }

            var loader = root.GetChild(loaderName) as GLoader3D;
            if (loader == null)
            {
                GameLog.LogWarning($"[FairySpineHelper] Child '{loaderName}' is not a GLoader3D.");
                return false;
            }

            if (!IsSpineFeatureEnabled)
            {
                GameLog.LogWarning("[FairySpineHelper] FAIRYGUI_SPINE is disabled. Enable define before playing Spine.");
                return false;
            }

            loader.animationName = animationName;
            loader.loop = loop;
            if (!string.IsNullOrEmpty(skinName))
                loader.skinName = skinName;
            loader.playing = true;

            return true;
        }

        /// <summary>
        /// Stop playback on a named GLoader3D child.
        /// </summary>
        public static bool TryStopSpine(GComponent root, string loaderName)
        {
            if (root == null)
            {
                GameLog.LogWarning("[FairySpineHelper] Root component is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(loaderName))
            {
                GameLog.LogWarning("[FairySpineHelper] loaderName is empty.");
                return false;
            }

            var loader = root.GetChild(loaderName) as GLoader3D;
            if (loader == null)
            {
                GameLog.LogWarning($"[FairySpineHelper] Child '{loaderName}' is not a GLoader3D.");
                return false;
            }

            loader.playing = false;
            return true;
        }

    }
}
