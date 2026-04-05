using UnityEngine;

namespace MiniGameTemplate.Pool
{
    /// <summary>
    /// Configuration for an object pool, stored as a ScriptableObject asset.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Pool/Pool Definition", order = 0)]
    public class PoolDefinition : ScriptableObject
    {
        [Tooltip("Prefab to pool.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Number of objects to pre-instantiate.")]
        [SerializeField] private int _initialSize = 10;

        [Tooltip("Maximum pool size. 0 = unlimited.")]
        [SerializeField] private int _maxSize = 50;

        public GameObject Prefab => _prefab;
        public int InitialSize => _initialSize;
        public int MaxSize => _maxSize;
    }
}
