using System.Collections;
using UnityEngine;

namespace MiniGameTemplate.Utils
{
    /// <summary>
    /// Provides coroutine execution for non-MonoBehaviour classes.
    /// Framework internal — game logic should prefer Timer module or async/await.
    /// </summary>
    public class CoroutineRunner : Singleton<CoroutineRunner>
    {
        /// <summary>
        /// Start a coroutine from anywhere (non-MonoBehaviour context).
        /// </summary>
        public static Coroutine Run(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        /// <summary>
        /// Stop a coroutine started via Run().
        /// </summary>
        public static void Stop(Coroutine coroutine)
        {
            if (coroutine != null && Instance != null)
            {
                Instance.StopCoroutine(coroutine);
            }
        }
    }
}
