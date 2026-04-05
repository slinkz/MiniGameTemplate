using UnityEngine;

namespace MiniGameTemplate.Pool
{
    /// <summary>
    /// Attach to pooled GameObjects for auto-return functionality.
    /// Only ticks Update when _autoReturnDelay > 0 to avoid wasted MonoBehaviour.Update overhead.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [Tooltip("If set, the object will auto-return to pool after this many seconds.")]
        [SerializeField] private float _autoReturnDelay = 0f;

        [Tooltip("The PoolDefinition this object belongs to. Set by PoolManager.")]
        [HideInInspector]
        public PoolDefinition PoolDef;

        private float _elapsedTime;

        private void OnEnable()
        {
            _elapsedTime = 0f;
            // Only enable Update ticking when auto-return is configured
            enabled = _autoReturnDelay > 0f;
        }

        private void Update()
        {
            // Update is only called when enabled (i.e., _autoReturnDelay > 0)
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= _autoReturnDelay)
            {
                ReturnToPool();
            }
        }

        public void ReturnToPool()
        {
            if (PoolDef != null)
            {
                PoolManager.Instance.Return(PoolDef, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
