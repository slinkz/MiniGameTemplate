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
        private IVFXPositionResolver _positionResolver;
        private float _timeScale = 1f;

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
            Tick(Time.deltaTime * _timeScale);
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
            _renderer.Initialize(_renderConfig, _typeRegistry, _pool.Capacity);
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
                string registryName = _typeRegistry != null ? _typeRegistry.name : "<null>";
                Debug.LogError(
                    $"[SpriteSheetVFXSystem] Type not found in registry: {type.name}. " +
                    $"Registry={registryName}. 修复：把该 VFXTypeSO 加入当前 SpriteSheetVFXSystem 使用的 VFXTypeRegistrySO._types 列表，或改回已注册的 VFXTypeSO。");
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

        // ──── 附着式 VFX API（ADR-027 §8 三阶段语义） ────

        /// <summary>设置位置解析器（由 Danmaku 桥接层注入）</summary>
        public void SetPositionResolver(IVFXPositionResolver resolver)
        {
            _positionResolver = resolver;
        }

        /// <summary>设置 VFX 时间缩放（与弹幕 TimeScale 联动）</summary>
        public void SetTimeScale(float timeScale)
        {
            _timeScale = Mathf.Max(0f, timeScale);
        }

        /// <summary>
        /// 播放附着式 VFX（绑定到附着源，每帧自动同步位置）。
        /// 重复调用同一 slot 会先 Stop 旧的再创建新的（ADR-027 §8）。
        /// </summary>
        /// <param name="type">VFX 类型</param>
        /// <param name="attachSourceId">附着源 ID（AttachSourceRegistry）</param>
        /// <param name="scale">缩放</param>
        /// <param name="colorOverride">颜色覆盖</param>
        /// <returns>VFX slot index，-1=失败</returns>
        public int PlayAttached(VFXTypeSO type, byte attachSourceId, float scale = 1f, Color? colorOverride = null)
        {
            if (type == null || attachSourceId == 0) return -1;

            Initialize();
            RebuildRegistryRuntimeIndices();

            if (!CanPlay(type))
            {
                string registryName = _typeRegistry != null ? _typeRegistry.name : "<null>";
                Debug.LogError(
                    $"[SpriteSheetVFXSystem] Type not found in registry: {type.name}. " +
                    $"Registry={registryName}. 修复：把该 VFXTypeSO 加入当前 SpriteSheetVFXSystem 使用的 VFXTypeRegistrySO._types 列表，或改回已注册的 VFXTypeSO。");
                return -1;
            }

            int slot = _pool.Allocate();
            if (slot < 0) return -1;

            // 尝试解析初始位置
            Vector3 initPos = Vector3.zero;
            _positionResolver?.TryResolvePosition(attachSourceId, out initPos);

            Color finalColor = colorOverride ?? type.Tint;
            _pool.Instances[slot] = new VFXInstance
            {
                Position = initPos,
                Color = finalColor,
                RotationDegrees = 0f,
                Scale = Mathf.Max(0.01f, scale),
                Elapsed = 0f,
                TypeIndex = type.RuntimeIndex,
                CurrentFrame = 0,
                Flags = (byte)(VFXInstance.FLAG_ACTIVE | (type.Loop ? 0 : VFXInstance.FLAG_PLAY_ONCE)),
                AttachSourceId = attachSourceId,
            };

            return slot;
        }

        /// <summary>
        /// 停止附着式 VFX（幂等——重复调用、无效 handle 均安全）。
        /// </summary>
        public void StopAttached(int slot)
        {
            Stop(slot);  // 复用已有 Stop 逻辑
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

                // ── 附着位置同步（ADR-021） ──
                if (instance.AttachSourceId > 0 && (instance.Flags & VFXInstance.FLAG_FROZEN) == 0)
                {
                    if (_positionResolver != null && _positionResolver.TryResolvePosition(instance.AttachSourceId, out var resolvedPos))
                    {
                        instance.Position = resolvedPos;
                    }
                    else
                    {
                        // 源失效——冻结在最后有效位置，播放到结束
                        instance.Flags |= VFXInstance.FLAG_FROZEN;
                        instance.AttachSourceId = 0;  // 解除绑定，不再尝试解析
                    }
                }

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
