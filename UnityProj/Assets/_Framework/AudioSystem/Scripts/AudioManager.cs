using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Audio
{
    /// <summary>
    /// Central audio manager. Handles BGM and SFX playback.
    /// Volume levels are driven by FloatVariable SOs for easy UI binding.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Volume Controls (FloatVariable SOs)")]
        [SerializeField] private FloatVariable _masterVolume;
        [SerializeField] private FloatVariable _bgmVolume;
        [SerializeField] private FloatVariable _sfxVolume;

        [Header("Audio Library")]
        [SerializeField] private AudioLibrary _audioLibrary;

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;

        protected override void Awake()
        {
            base.Awake();

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
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
            float sfx = _sfxVolume != null ? _sfxVolume.Value : 1f;

            _bgmSource.volume = master * bgm;
            _sfxSource.volume = master * sfx;
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
        /// Play a sound effect (fire-and-forget).
        /// </summary>
        public void PlaySFX(AudioClipSO clipSO)
        {
            if (clipSO == null || clipSO.Clip == null) return;

            float master = _masterVolume != null ? _masterVolume.Value : 1f;
            float sfx = _sfxVolume != null ? _sfxVolume.Value : 1f;

            _sfxSource.PlayOneShot(clipSO.Clip, clipSO.Volume * master * sfx);
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
    }
}
