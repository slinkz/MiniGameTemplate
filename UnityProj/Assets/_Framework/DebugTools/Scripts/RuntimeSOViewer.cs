#if UNITY_EDITOR
using UnityEngine;
using MiniGameTemplate.Data;

namespace MiniGameTemplate.DebugTools
{
    /// <summary>
    /// Editor-only component that displays SO variable values in the scene view.
    /// Attach to any GameObject and assign SO variables to monitor.
    /// </summary>
    [ExecuteInEditMode]
    public class RuntimeSOViewer : MonoBehaviour
    {
        [Header("Variables to Monitor")]
        [SerializeField] private FloatVariable[] _floatVariables;
        [SerializeField] private IntVariable[] _intVariables;
        [SerializeField] private BoolVariable[] _boolVariables;

        private void OnDrawGizmos()
        {
            // Values are visible in the Inspector at runtime
            // This component primarily serves as a convenient grouping mechanism
        }
    }
}
#endif
