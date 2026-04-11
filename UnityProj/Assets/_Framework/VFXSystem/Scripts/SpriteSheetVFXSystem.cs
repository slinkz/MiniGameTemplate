using UnityEngine;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// Sprite Sheet VFX 唯一 MonoBehaviour 入口。
    /// 阶段 1 只负责初始化、播放、更新、渲染与清理。
    /// </summary>
    public class SpriteSheetVFXSystem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private VFXRenderConfig _renderConfig;
        [SerializeField] private VFXTypeRegistrySO _typeRegistry;
        [SerializeField, Min(1)] private int _capacity = VFXPool.DEFAULT_CAPACITY;

        private VFXPool _pool;
        private VFXBatchRenderer _renderer;

        public int ActiveCount => _pool?.ActiveCount ?? 0;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            _renderer?.Rebuild(_pool, _typeRegistry);
        }

        public void Initialize()
        {
            if (_pool != null)
                return;

            RebuildRegistryRuntimeIndices();
            _pool = new VFXPool(_capacity);
            _renderer = new VFXBatchRenderer();
            _renderer.Initialize(_renderConfig, _pool.Capacity);
        }

        public void Dispose()
        {
            _renderer?.Dispose();
            _renderer = null;
            _pool = null;
        }

        public bool CanPlay(VFXTypeSO type)
        {
            return type != null
                && _typeRegistry != null
                && _typeRegistry.Contains(type);
        }

        public void PlayOneShot(VFXTypeSO type, Vector3 position, float scale = 1f, float rotationDegrees = 0f, Color? colorOverride = null)
        {
            Play(type, position, scale, rotationDegrees, colorOverride);
        }

        public int Play(VFXTypeSO type, Vector3 position, float scale = 1f, float rotationDegrees = 0f, Color? colorOverride = null)
        {
            if (type == null)
                return -1;

            Initialize();
            RebuildRegistryRuntimeIndices();

            if (!CanPlay(type))
            {
                Debug.LogWarning($"[SpriteSheetVFXSystem] Type not found in registry: {type.name}");
                return -1;
            }

            int slot = _pool.Allocate();
            if (slot < 0)
                return -1;

            Color finalColor = colorOverride ?? type.Tint;
            _pool.Instances[slot] = new VFXInstance
            {
                Position = position,
                Color = finalColor,
                RotationDegrees = rotationDegrees,
                Scale = Mathf.Max(0.01f, scale),
                Elapsed = 0f,
                TypeIndex = type.RuntimeIndex,
                CurrentFrame = 0,
                Flags = (byte)(VFXInstance.FLAG_ACTIVE | (type.Loop ? 0 : VFXInstance.FLAG_PLAY_ONCE)),
            };

            return slot;
        }

        public void Stop(int slot)
        {
            if (_pool == null || slot < 0 || slot >= _pool.Capacity)
                return;

            if (_pool.Instances[slot].IsActive)
                _pool.Free(slot);
        }

        public void Tick(float deltaTime)
        {
            if (_pool == null || _typeRegistry == null)
                return;

            var instances = _pool.Instances;
            for (int i = 0; i < _pool.Capacity; i++)
            {
                ref var instance = ref instances[i];
                if (!instance.IsActive)
                    continue;

                if (!_typeRegistry.TryGet(instance.TypeIndex, out var type))
                {
                    _pool.Free(i);
                    continue;
                }

                instance.Elapsed += deltaTime;
                int frame = Mathf.FloorToInt(instance.Elapsed * Mathf.Max(1f, type.FramesPerSecond));

                if (type.Loop)
                {
                    instance.CurrentFrame = (byte)(frame % type.MaxFrameCount);
                    continue;
                }

                if (frame >= type.MaxFrameCount)
                {
                    _pool.Free(i);
                    continue;
                }

                instance.CurrentFrame = (byte)frame;
            }
        }

        private void RebuildRegistryRuntimeIndices()
        {
            _typeRegistry?.RebuildRuntimeIndices();
        }
    }
}
