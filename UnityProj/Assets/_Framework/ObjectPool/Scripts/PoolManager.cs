using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Pool
{
    /// <summary>
    /// Manages multiple object pools, indexed by PoolDefinition.
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        private readonly Dictionary<PoolDefinition, ObjectPool> _pools = new Dictionary<PoolDefinition, ObjectPool>();

        /// <summary>
        /// Get an object from the pool defined by the given definition.
        /// Creates the pool on first access.
        /// </summary>
        public GameObject Get(PoolDefinition definition)
        {
            var pool = GetOrCreatePool(definition);
            return pool.Get();
        }

        /// <summary>
        /// Return an object to its pool.
        /// </summary>
        public void Return(PoolDefinition definition, GameObject obj)
        {
            if (!_pools.TryGetValue(definition, out var pool))
            {
                Debug.LogWarning($"[PoolManager] No pool found for definition: {definition.name}");
                return;
            }

            pool.Return(obj);
        }

        /// <summary>
        /// Pre-create a pool for the given definition.
        /// Useful for preloading during scene initialization.
        /// </summary>
        public void PrewarmPool(PoolDefinition definition)
        {
            GetOrCreatePool(definition);
        }

        /// <summary>
        /// Return all in-use objects across all pools.
        /// </summary>
        public void ReturnAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.ReturnAll();
            }
        }

        private ObjectPool GetOrCreatePool(PoolDefinition definition)
        {
            if (_pools.TryGetValue(definition, out var pool))
                return pool;

            var parent = new GameObject($"[Pool] {definition.Prefab.name}").transform;
            parent.SetParent(transform);
            pool = new ObjectPool(definition, parent);
            _pools[definition] = pool;
            return pool;
        }
    }
}
