using UnityEngine;

namespace MiniGameTemplate.Audio
{
    /// <summary>
    /// Configuration for a single audio clip.
    /// Create via: Create → MiniGameTemplate → Audio → Audio Clip.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Audio/Audio Clip", order = 0)]
    public class AudioClipSO : ScriptableObject
    {
        [SerializeField] private AudioClip _clip;
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 1f;
        [Range(0.5f, 2f)]
        [SerializeField] private float _pitch = 1f;
        [SerializeField] private bool _loop;

        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public float Pitch => _pitch;
        public bool Loop => _loop;
    }
}
