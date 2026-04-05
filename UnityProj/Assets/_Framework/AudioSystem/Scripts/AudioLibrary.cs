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
            EnsureLookup();
            _lookup.TryGetValue(key, out var clip);
            return clip;
        }

        private void EnsureLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, AudioClipSO>();
            if (_entries == null) return;
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                    _lookup[entry.key] = entry.clip;
            }
        }

        private void OnEnable()
        {
            _lookup = null; // Force rebuild on load
        }
    }
}
