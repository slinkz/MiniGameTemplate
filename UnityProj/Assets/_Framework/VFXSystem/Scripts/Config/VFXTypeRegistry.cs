using System;
using System.Collections.Generic;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// VFX 类型注册表（ADR-030）——运行时类，懒注册。
    /// 不再持久化为 ScriptableObject 资产。
    /// </summary>
    internal sealed class VFXTypeRegistry
    {
        private readonly List<VFXTypeSO> _types = new();
        private readonly Dictionary<VFXTypeSO, ushort> _indices = new();

        public int Count => _types.Count;

        public ushort GetOrRegister(VFXTypeSO type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (_indices.TryGetValue(type, out ushort index))
                return index;

            if (_types.Count >= ushort.MaxValue + 1)
                throw new InvalidOperationException("[VFXTypeRegistry] VFXType 数量超出 ushort 上限。");

            index = (ushort)_types.Count;
            _types.Add(type);
            _indices.Add(type, index);
            return index;
        }

        public bool Contains(VFXTypeSO type)
        {
            return type != null && _indices.ContainsKey(type);
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

        public VFXTypeSO GetType(ushort index)
        {
            if (!TryGet(index, out var type))
                throw new IndexOutOfRangeException($"[VFXTypeRegistry] VFXType index 越界: {index}/{_types.Count}");
            return type;
        }

        public void WarmUp(IEnumerable<VFXTypeSO> types)
        {
            if (types == null)
                return;

            foreach (var type in types)
            {
                if (type != null)
                    GetOrRegister(type);
            }
        }
    }
}
