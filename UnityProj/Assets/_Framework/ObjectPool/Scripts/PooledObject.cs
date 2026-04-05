using UnityEngine;

namespace MiniGameTemplate.Pool
{
    /// <summary>
    /// Attach to pooled GameObjects for auto-return functionality.
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
        }

        private void Update()
        {
            if (_autoReturnDelay <= 0f) return;

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
