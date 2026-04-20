using MiniGameTemplate.VFX;
using UnityEngine;

namespace MiniGameTemplate.Example.VFXDemo
{
    /// <summary>
    /// 最小 VFX Demo：定时在一排位置上轮播多个特效类型。
    /// 只负责播放节奏与类型选择，不负责输入与返回逻辑。
    /// </summary>
    public class VFXDemoSpawner : MonoBehaviour
    {
        private enum TypeSelectionMode
        {
            Sequential = 0,
            Random = 1,
            Fixed = 2,
        }

        [SerializeField] private SpriteSheetVFXSystem _vfxSystem;
        [SerializeField] private VFXTypeSO[] _types;
        [SerializeField] private TypeSelectionMode _selectionMode = TypeSelectionMode.Sequential;
        [SerializeField, Min(0)] private int _fixedTypeIndex;
        [SerializeField, Min(0.05f)] private float _interval = 0.2f;
        [SerializeField, Min(1)] private int _spawnCount = 5;
        [SerializeField] private Vector2 _start = new Vector2(-3f, 0f);
        [SerializeField] private Vector2 _step = new Vector2(1.5f, 0f);
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _autoPlayOnStart = true;

        private float _timer;
        private int _cursor;
        private int _typeCursor;
        private bool _isPlaying = true;

        public int TypeCount => _types == null ? 0 : _types.Length;

        private void OnEnable()
        {
            _isPlaying = _autoPlayOnStart;
            _timer = 0f;
            _cursor = 0;
            _typeCursor = 0;
        }

        private void Update()
        {
            if (!_isPlaying || _vfxSystem == null || !HasValidTypes())
                return;

            _timer += Time.deltaTime;
            if (_timer < _interval)
                return;

            _timer = 0f;
            SpawnOne();
        }

        [ContextMenu("Restart Loop")]
        public void RestartLoop()
        {
            _cursor = 0;
            _typeCursor = 0;
            _timer = _interval;
            _isPlaying = true;
        }

        [ContextMenu("Stop Loop")]
        public void StopLoop()
        {
            _isPlaying = false;
            _timer = 0f;
        }

        [ContextMenu("Spawn Once")]
        public void SpawnOne()
        {
            if (_vfxSystem == null || !TrySelectType(out var type))
                return;

            Vector2 pos2D = _start + _step * _cursor;
            _vfxSystem.Play(type, new Vector3(pos2D.x, pos2D.y, 0f));

            AdvanceSpawnCursor();
        }

        public void SetSelectionModeSequential()
        {
            _selectionMode = TypeSelectionMode.Sequential;
            _typeCursor = 0;
            Debug.Log("[VFXDemoSpawner] Selection mode -> Sequential");
        }

        public void SetSelectionModeRandom()
        {
            _selectionMode = TypeSelectionMode.Random;
            Debug.Log("[VFXDemoSpawner] Selection mode -> Random");
        }

        public void SetFixedTypeIndex(int index)
        {
            _selectionMode = TypeSelectionMode.Fixed;
            _fixedTypeIndex = Mathf.Max(0, index);

            if (_types != null && _fixedTypeIndex < _types.Length)
            {
                var type = _types[_fixedTypeIndex];
                Debug.Log($"[VFXDemoSpawner] Selection mode -> Fixed index={_fixedTypeIndex} type={(type == null ? "<null>" : type.name)} tint={(type == null ? "<null>" : type.Tint.ToString())}");
                return;
            }

            Debug.Log($"[VFXDemoSpawner] Selection mode -> Fixed index={_fixedTypeIndex} type=<out-of-range>");
        }

        public void SpawnOneManual()
        {
            if (_vfxSystem == null || !TrySelectType(out var type))
                return;

            Vector2 pos2D = _start + _step * _cursor;
            Debug.Log($"[VFXDemoSpawner] ManualSpawn mode={_selectionMode} fixedIndex={_fixedTypeIndex} cursor={_cursor} selectedType={(type == null ? "<null>" : type.name)} tint={(type == null ? "<null>" : type.Tint.ToString())} pos={pos2D}");
            _vfxSystem.Play(type, new Vector3(pos2D.x, pos2D.y, 0f));
            AdvanceSpawnCursor();
        }

        private bool HasValidTypes()
        {
            if (_types == null || _types.Length == 0)
                return false;

            for (int i = 0; i < _types.Length; i++)
            {
                if (_types[i] != null)
                    return true;
            }

            return false;
        }

        private bool TrySelectType(out VFXTypeSO type)
        {
            type = null;
            if (!HasValidTypes())
                return false;

            switch (_selectionMode)
            {
                case TypeSelectionMode.Random:
                    return TrySelectRandomType(out type);
                case TypeSelectionMode.Fixed:
                    return TrySelectFixedType(out type);
                default:
                    return TrySelectSequentialType(out type);
            }
        }

        private bool TrySelectSequentialType(out VFXTypeSO type)
        {
            for (int i = 0; i < _types.Length; i++)
            {
                int index = (_typeCursor + i) % _types.Length;
                if (_types[index] == null)
                    continue;

                type = _types[index];
                _typeCursor = (index + 1) % _types.Length;
                return true;
            }

            type = null;
            return false;
        }

        private bool TrySelectRandomType(out VFXTypeSO type)
        {
            int validCount = 0;
            for (int i = 0; i < _types.Length; i++)
            {
                if (_types[i] != null)
                    validCount++;
            }

            if (validCount == 0)
            {
                type = null;
                return false;
            }

            int target = Random.Range(0, validCount);
            for (int i = 0; i < _types.Length; i++)
            {
                if (_types[i] == null)
                    continue;

                if (target == 0)
                {
                    type = _types[i];
                    return true;
                }

                target--;
            }

            type = null;
            return false;
        }

        private bool TrySelectFixedType(out VFXTypeSO type)
        {
            if (_types == null || _types.Length == 0)
            {
                type = null;
                return false;
            }

            int clampedIndex = Mathf.Clamp(_fixedTypeIndex, 0, _types.Length - 1);
            type = _types[clampedIndex];
            if (type != null)
                return true;

            return TrySelectSequentialType(out type);
        }

        private void AdvanceSpawnCursor()
        {
            _cursor++;
            if (_cursor >= _spawnCount)
            {
                if (_loop)
                {
                    _cursor = 0;
                }
                else
                {
                    _cursor = _spawnCount - 1;
                    _isPlaying = false;
                }
            }
        }
    }
}
