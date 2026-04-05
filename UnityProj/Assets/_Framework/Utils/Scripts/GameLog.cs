using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace MiniGameTemplate.Utils
{
    /// <summary>
    /// Conditional-compiled logging wrapper. In Release builds (non-DEVELOPMENT_BUILD,
    /// non-UNITY_EDITOR), ALL calls to GameLog.Log / LogWarning are stripped by the
    /// compiler — including their argument expressions (string interpolations, allocations).
    ///
    /// Usage: Replace Debug.Log("msg") with GameLog.Log("msg").
    /// Debug.LogError and Debug.LogException are NOT wrapped — errors should always log.
    ///
    /// NOTE: [Conditional] causes the ENTIRE call site to be removed at compile time,
    /// so $"string {interpolation}" never allocates in release builds.
    /// </summary>
    public static class GameLog
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }
    }
}
