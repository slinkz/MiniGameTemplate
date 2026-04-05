using UnityEngine;

namespace MiniGameTemplate.Utils
{
    /// <summary>
    /// Lightweight singleton base for MonoBehaviours.
    /// RESTRICTED: Only for framework-internal managers (AudioManager, UIManager, etc.).
    /// Game logic should NEVER use singletons — communicate via SO events/variables instead.
    ///
    /// Singleton lifecycle:
    /// - If accessed before Awake, auto-creates a new GameObject.
    /// - If placed in scene, self-registers in Awake.
    /// - Does NOT use FindObjectOfType (expensive and unreliable across scenes).
    ///
    /// Performance notes:
    /// - No lock: WebGL is single-threaded; Unity API is main-thread-only.
    /// - Uses ReferenceEquals to skip Unity's overloaded == (avoids native interop cost).
    /// - [RuntimeInitializeOnLoadMethod] resets statics for Domain Reload disabled workflows.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _applicationIsQuitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _applicationIsQuitting = false;
        }

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed on quit. Returning null.");
                    return null;
                }

                // ReferenceEquals avoids Unity's overloaded == which calls native Object.CompareBaseObjects
                if (ReferenceEquals(_instance, null) || !_instance)
                {
                    // Auto-create if not yet registered via Awake
                    var go = new GameObject($"[{typeof(T).Name}]");
                    _instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (ReferenceEquals(_instance, null) || !_instance)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (!ReferenceEquals(_instance, this))
            {
                Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} detected on '{gameObject.name}' — destroying.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
