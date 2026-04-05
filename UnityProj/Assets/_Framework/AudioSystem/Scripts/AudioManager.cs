using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Audio
{
    /// <summary>
    /// Central audio manager. Handles BGM and SFX playback.
    /// Volume levels are driven by FloatVariable SOs for easy UI binding.
    ///
    /// SFX uses a pool of AudioSources to support concurrent sound effects.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Volume Controls (FloatVariable SOs)")]
        [SerializeField] private FloatVariable _masterVolume;
        [SerializeField] private FloatVariable _bgmVolume;
        [SerializeField] private FloatVariable _sfxVolume;

        [Header("Audio Library")]
        [SerializeField] private AudioLibrary _audioLibrary;

        [Header("SFX Pool")]
        [Tooltip("Maximum number of concurrent SFX channels. More channels = more simultaneous sounds.")]
        [SerializeField] private int _sfxPoolSize = 4;

        private AudioSource _bgmSource;
        private List<AudioSource> _sfxPool;
        private int _nextSfxIndex;

        protected override void Awake()
        {
            base.Awake();

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;

            // Create SFX audio source pool
            _sfxPoolSize = Mathf.Max(1, _sfxPoolSize);
            _sfxPool = new List<AudioSource>(_sfxPoolSize);
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                _sfxPool.Add(sfxSource);
            }
            _nextSfxIndex = 0;
        }

        private void OnEnable()
        {
            if (_masterVolume != null) _masterVolume.OnValueChanged += OnVolumeChanged;
            if (_bgmVolume != null) _bgmVolume.OnValueChanged += OnVolumeChanged;
            if (_sfxVolume != null) _sfxVolume.OnValueChanged += OnVolumeChanged;
        }

        private void OnDisable()
        {
            if (_masterVolume != null) _masterVolume.OnValueChanged -= OnVolumeChanged;
            if (_bgmVolume != null) _bgmVolume.OnValueChanged -= OnVolumeChanged;
            if (_sfxVolume != null) _sfxVolume.OnValueChanged -= OnVolumeChanged;
        }

        private void OnVolumeChanged(float _)
        {
            float master = _masterVolume != null ? _masterVolume.Value : 1f;
            float bgm = _bgmVolume != null ? _bgmVolume.Value : 1f;

            _bgmSource.volume = master * bgm;
            // SFX volume is applied per-PlayOneShot call, no need to update pool sources
        }

        /// <summary>
        /// Play background music. Stops current BGM if playing.
        /// </summary>
        public void PlayBGM(AudioClipSO clipSO)
        {
            if (clipSO == null || clipSO.Clip == null) return;

            _bgmSource.clip = clipSO.Clip;
            _bgmSource.pitch = clipSO.Pitch;
            _bgmSource.loop = true;
            OnVolumeChanged(0); // Refresh volume
            _bgmSource.Play();
        }

        /// <summary>
        /// Stop current background music.
        /// </summary>
        public void StopBGM()
        {
            _bgmSource.Stop();
        }

        /// <summary>
        /// Play a sound effect using the SFX pool (fire-and-forget).
        /// Round-robins through available AudioSources for concurrent playback.
        /// </summary>
        public void PlaySFX(AudioClipSO clipSO)
        {
            if (clipSO == null || clipSO.Clip == null) return;

            float master = _masterVolume != null ? _masterVolume.Value : 1f;
            float sfx = _sfxVolume != null ? _sfxVolume.Value : 1f;

            var source = GetNextSfxSource();
            source.PlayOneShot(clipSO.Clip, clipSO.Volume * master * sfx);
        }

        /// <summary>
        /// Play a sound effect by key from the AudioLibrary.
        /// </summary>
        public void PlaySFX(string key)
        {
            if (_audioLibrary == null) return;
            var clip = _audioLibrary.GetClip(key);
            if (clip != null) PlaySFX(clip);
        }

        /// <summary>
        /// Stop all currently playing SFX.
        /// </summary>
        public void StopAllSFX()
        {
            foreach (var source in _sfxPool)
            {
                source.Stop();
            }
        }

        private AudioSource GetNextSfxSource()
        {
            var source = _sfxPool[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxPool.Count;
            return source;
        }
    }
}
