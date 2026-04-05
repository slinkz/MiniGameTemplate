using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Pool
{
    /// <summary>
    /// A single object pool that manages instances of a specific prefab.
    /// </summary>
    public class ObjectPool
    {
        private readonly PoolDefinition _definition;
        private readonly Queue<GameObject> _available = new Queue<GameObject>();
        private readonly HashSet<GameObject> _inUse = new HashSet<GameObject>();
        private readonly Transform _parent;
        private int _totalCreated;

        public int AvailableCount => _available.Count;
        public int InUseCount => _inUse.Count;

        public ObjectPool(PoolDefinition definition, Transform parent)
        {
            _definition = definition;
            _parent = parent;
            Prewarm();
        }

        private void Prewarm()
        {
            for (int i = 0; i < _definition.InitialSize; i++)
            {
                CreateNewInstance();
            }
        }

        private GameObject CreateNewInstance()
        {
            var obj = Object.Instantiate(_definition.Prefab, _parent);
            obj.SetActive(false);
            _available.Enqueue(obj);
            _totalCreated++;
            return obj;
        }

        /// <summary>
        /// Get an object from the pool. Returns null if max size reached.
        /// </summary>
        public GameObject Get()
        {
            GameObject obj;

            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else if (_definition.MaxSize <= 0 || _totalCreated < _definition.MaxSize)
            {
                obj = CreateNewInstance();
                _available.Dequeue(); // Remove from available since we'll use it immediately
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool for {_definition.Prefab.name} is exhausted (max={_definition.MaxSize}).");
                return null;
            }

            obj.SetActive(true);
            _inUse.Add(obj);
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;
            if (!_inUse.Remove(obj))
            {
                Debug.LogWarning($"[ObjectPool] Trying to return an object not from this pool: {obj.name}");
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(_parent);
            _available.Enqueue(obj);
        }

        /// <summary>
        /// Return all in-use objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            var list = new List<GameObject>(_inUse);
            foreach (var obj in list)
            {
                Return(obj);
            }
        }
    }
}
