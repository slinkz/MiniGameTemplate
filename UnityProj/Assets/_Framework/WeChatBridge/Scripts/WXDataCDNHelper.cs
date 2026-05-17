using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Reads the DATA_CDN value set by the WeChat Mini Game conversion panel at runtime.
    /// This is the Single Source of Truth for CDN base URL — no need to duplicate
    /// the value in AssetConfig SO or any other config.
    ///
    /// JS layer: GameGlobal.unityNamespace.DATA_CDN (set in game.js by the conversion tool).
    /// C# layer: WXDataCDNHelper.GetDataCDN() returns it as a trimmed string.
    ///
    /// In Editor or non-WeChat builds, returns empty string.
    /// </summary>
    public static class WXDataCDNHelper
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int WXBridge_GetDataCDN(byte[] buffer, int bufferSize);
#endif

        /// <summary>
        /// Get the DATA_CDN value from the WeChat conversion panel config (game.js).
        /// Returns empty string in Editor or non-WeChat environments.
        /// The returned URL has trailing slashes removed.
        /// </summary>
        public static string GetDataCDN()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var buffer = new byte[512];
            int len = WXBridge_GetDataCDN(buffer, buffer.Length);
            if (len > 0)
            {
                return System.Text.Encoding.UTF8.GetString(buffer, 0, len).TrimEnd('/');
            }
#endif
            return string.Empty;
        }
    }
}
