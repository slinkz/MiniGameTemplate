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
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed on quit. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Auto-create if not yet registered via Awake
                        var go = new GameObject($"[{typeof(T).Name}]");
                        _instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} detected on '{gameObject.name}' — destroying.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
