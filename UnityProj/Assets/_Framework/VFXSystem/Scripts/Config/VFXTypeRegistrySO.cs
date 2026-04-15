using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.VFX
{

    /// <summary>
    /// VFX 类型注册表。
    /// 用于给运行时分配稳定索引，避免跨系统硬编码查找。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/VFX/Type Registry")]
    public class VFXTypeRegistrySO : ScriptableObject
    {
        [SerializeField] private List<VFXTypeSO> _types = new();

        public IReadOnlyList<VFXTypeSO> Types => _types;
        public int Count => _types.Count;

        public void RebuildRuntimeIndices()
        {
            for (ushort i = 0; i < _types.Count; i++)
            {
                if (_types[i] != null)
                    _types[i].RuntimeIndex = i;
            }
        }

        public bool Contains(VFXTypeSO type)
        {
            return type != null && _types.Contains(type);
        }

        public bool TryGet(ushort index, out VFXTypeSO type)
        {
            if (index < _types.Count)
            {
                type = _types[index];
                return type != null;
            }

            type = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            MiniGameTemplate.Danmaku.Editor.DanmakuEditorRefreshCoordinator.MarkDirty(this);
        }
#endif
    }
}

