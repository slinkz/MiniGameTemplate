using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// Attach to any GameObject to auto-register/unregister with a TransformRuntimeSet.
    /// </summary>
    public class RuntimeSetRegistrar : MonoBehaviour
    {
        [SerializeField] private TransformRuntimeSet _runtimeSet;

        private void OnEnable()
        {
            if (_runtimeSet != null)
                _runtimeSet.Add(transform);
        }

        private void OnDisable()
        {
            if (_runtimeSet != null)
                _runtimeSet.Remove(transform);
        }
    }
}
