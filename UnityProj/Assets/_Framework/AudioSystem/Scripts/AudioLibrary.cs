using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Audio
{
    /// <summary>
    /// A library of audio clips, indexed by name.
    /// Allows playing sounds by string key without direct SO references.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Audio/Audio Library", order = 1)]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct AudioEntry
        {
            public string key;
            public AudioClipSO clip;
        }

        [SerializeField] private AudioEntry[] _entries;

        private Dictionary<string, AudioClipSO> _lookup;

        public AudioClipSO GetClip(string key)
        {
            if (_lookup == null) BuildLookup(); // Safety fallback (should not happen)
            _lookup.TryGetValue(key, out var clip);
            return clip;
        }

        private void BuildLookup()
        {
            int capacity = _entries != null ? _entries.Length : 0;
            _lookup = new Dictionary<string, AudioClipSO>(capacity);
            if (_entries == null) return;
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                    _lookup[entry.key] = entry.clip;
            }
        }

        private void OnEnable()
        {
            // Pre-build lookup eagerly to avoid first-frame allocation spike
            BuildLookup();
        }
    }
}
