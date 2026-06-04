using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Warblade.Data;

namespace Warblade.Managers
{
    /// <summary>
    /// Global audio service for one-shot SFX/UI sounds, looping music, and mixer volume control.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        private class AudioCueConfig
        {
            [SerializeField] private AudioCue _cue;
            [SerializeField] private AudioBus _bus = AudioBus.Sfx;
            [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
            [SerializeField, Range(0f, 1f)] private float _volume = 1f;
            [SerializeField, Range(-0.5f, 0.5f)] private float _pitchJitter = 0f;
            [SerializeField] private bool _loop;

            public AudioCue Cue => _cue;
            public AudioBus Bus => _bus;
            public AudioClip[] Clips => _clips;
            public float Volume => _volume;
            public float PitchJitter => _pitchJitter;
            public bool Loop => _loop;
        }

        private const float MinDecibels = -80f;
        private const float MaxDecibels = 0f;
        private const string MasterVolumePreferenceKey = "Audio.MasterVolume";
        private const string MusicVolumePreferenceKey = "Audio.MusicVolume";
        private const string SfxVolumePreferenceKey = "Audio.SfxVolume";
        private const string UiVolumePreferenceKey = "Audio.UiVolume";

        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private string _masterVolumeParameter = "MasterVolume";
        [SerializeField] private string _musicVolumeParameter = "MusicVolume";
        [SerializeField] private string _sfxVolumeParameter = "SfxVolume";
        [SerializeField] private string _uiVolumeParameter = "UiVolume";

        [Header("Sources")]
        [SerializeField, Min(1)] private int _sfxVoiceCount = 8;
        [SerializeField, Min(1)] private int _uiVoiceCount = 4;

        [Header("Cues")]
        [SerializeField] private AudioCueConfig[] _cues = Array.Empty<AudioCueConfig>();

        private readonly Dictionary<AudioCue, AudioCueConfig> _cueLookup = new Dictionary<AudioCue, AudioCueConfig>();
        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private readonly List<AudioSource> _uiSources = new List<AudioSource>();

        private AudioSource _musicSource;
        private int _sfxSourceIndex;
        private int _uiSourceIndex;
        private AudioCue _currentMusicCue = AudioCue.None;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public float UiVolume { get; private set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            BuildCueLookup();
            BuildSources();
            LoadVolumes();
            ApplyAllVolumes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Returns true when a cue has an assigned configuration in this manager.
        /// </summary>
        public bool HasCue(AudioCue cue)
        {
            return cue != AudioCue.None && _cueLookup.ContainsKey(cue);
        }

        /// <summary>
        /// Plays a configured one-shot cue through its assigned SFX or UI bus.
        /// </summary>
        public void PlayOneShot(AudioCue cue)
        {
            if (!TryGetCue(cue, out AudioCueConfig config))
            {
                return;
            }

            if (config.Bus == AudioBus.Music)
            {
                PlayMusic(cue);
                return;
            }

            AudioClip clip = SelectClip(config);
            if (clip == null)
            {
                return;
            }

            AudioSource source = config.Bus == AudioBus.Ui
                ? GetNextSource(_uiSources, ref _uiSourceIndex)
                : GetNextSource(_sfxSources, ref _sfxSourceIndex);

            if (source == null)
            {
                return;
            }

            source.pitch = GetPitch(config);
            source.PlayOneShot(clip, config.Volume);
        }

        /// <summary>
        /// Starts a music cue, replacing any currently playing music.
        /// </summary>
        public void PlayMusic(AudioCue cue)
        {
            if (!TryGetCue(cue, out AudioCueConfig config))
            {
                return;
            }

            AudioClip clip = SelectClip(config);
            if (clip == null || _musicSource == null)
            {
                return;
            }

            if (_currentMusicCue == cue && _musicSource.clip == clip && _musicSource.isPlaying)
            {
                return;
            }

            _currentMusicCue = cue;
            _musicSource.Stop();
            _musicSource.clip = clip;
            _musicSource.volume = config.Volume;
            _musicSource.pitch = GetPitch(config);
            _musicSource.loop = config.Loop;
            _musicSource.Play();
        }

        /// <summary>
        /// Stops the active music cue if one is playing.
        /// </summary>
        public void StopMusic()
        {
            _currentMusicCue = AudioCue.None;

            if (_musicSource == null)
            {
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        /// <summary>
        /// Sets the normalized master volume and stores it for future sessions.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            SetMixerVolume(_masterVolumeParameter, MasterVolume);
            PlayerPrefs.SetFloat(MasterVolumePreferenceKey, MasterVolume);
        }

        /// <summary>
        /// Sets the normalized music volume and stores it for future sessions.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            SetMixerVolume(_musicVolumeParameter, MusicVolume);
            PlayerPrefs.SetFloat(MusicVolumePreferenceKey, MusicVolume);
        }

        /// <summary>
        /// Sets the normalized SFX volume and stores it for future sessions.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            SetMixerVolume(_sfxVolumeParameter, SfxVolume);
            PlayerPrefs.SetFloat(SfxVolumePreferenceKey, SfxVolume);
        }

        /// <summary>
        /// Sets the normalized UI volume and stores it for future sessions.
        /// </summary>
        public void SetUiVolume(float volume)
        {
            UiVolume = Mathf.Clamp01(volume);
            SetMixerVolume(_uiVolumeParameter, UiVolume);
            PlayerPrefs.SetFloat(UiVolumePreferenceKey, UiVolume);
        }

        private void BuildCueLookup()
        {
            _cueLookup.Clear();

            if (_cues == null)
            {
                return;
            }

            for (int i = 0; i < _cues.Length; i++)
            {
                AudioCueConfig config = _cues[i];
                if (config == null || config.Cue == AudioCue.None)
                {
                    continue;
                }

                if (_cueLookup.ContainsKey(config.Cue))
                {
                    Debug.LogWarning($"[{nameof(AudioManager)}] Duplicate audio cue '{config.Cue}' on '{name}'. The first entry will be used.", this);
                    continue;
                }

                _cueLookup.Add(config.Cue, config);
            }
        }

        private void BuildSources()
        {
            _sfxSources.Clear();
            _uiSources.Clear();

            _musicSource = CreateSource("Music Source", _musicGroup, true);

            for (int i = 0; i < _sfxVoiceCount; i++)
            {
                _sfxSources.Add(CreateSource($"SFX Source {i + 1}", _sfxGroup, false));
            }

            for (int i = 0; i < _uiVoiceCount; i++)
            {
                _uiSources.Add(CreateSource($"UI Source {i + 1}", _uiGroup, false));
            }
        }

        private AudioSource CreateSource(string sourceName, AudioMixerGroup outputGroup, bool loop)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = outputGroup;
            return source;
        }

        private bool TryGetCue(AudioCue cue, out AudioCueConfig config)
        {
            if (cue == AudioCue.None)
            {
                config = null;
                return false;
            }

            if (_cueLookup.TryGetValue(cue, out config))
            {
                return true;
            }

            Debug.LogWarning($"[{nameof(AudioManager)}] Missing audio cue '{cue}' on '{name}'.", this);
            return false;
        }

        private static AudioClip SelectClip(AudioCueConfig config)
        {
            AudioClip[] clips = config.Clips;
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int index = clips.Length == 1 ? 0 : UnityEngine.Random.Range(0, clips.Length);
            return clips[index];
        }

        private static float GetPitch(AudioCueConfig config)
        {
            if (config.PitchJitter <= 0f)
            {
                return 1f;
            }

            return 1f + UnityEngine.Random.Range(-config.PitchJitter, config.PitchJitter);
        }

        private static AudioSource GetNextSource(IReadOnlyList<AudioSource> sources, ref int index)
        {
            if (sources == null || sources.Count == 0)
            {
                return null;
            }

            AudioSource source = sources[index];
            index = (index + 1) % sources.Count;
            return source;
        }

        private void LoadVolumes()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterVolumePreferenceKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumePreferenceKey, 1f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumePreferenceKey, 1f);
            UiVolume = PlayerPrefs.GetFloat(UiVolumePreferenceKey, 1f);
        }

        private void ApplyAllVolumes()
        {
            SetMixerVolume(_masterVolumeParameter, MasterVolume);
            SetMixerVolume(_musicVolumeParameter, MusicVolume);
            SetMixerVolume(_sfxVolumeParameter, SfxVolume);
            SetMixerVolume(_uiVolumeParameter, UiVolume);
        }

        private void SetMixerVolume(string parameterName, float normalizedVolume)
        {
            if (_audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            float decibels = NormalizedVolumeToDecibels(normalizedVolume);
            _audioMixer.SetFloat(parameterName, decibels);
        }

        private static float NormalizedVolumeToDecibels(float normalizedVolume)
        {
            if (normalizedVolume <= 0.0001f)
            {
                return MinDecibels;
            }

            return Mathf.Clamp(Mathf.Log10(normalizedVolume) * 20f, MinDecibels, MaxDecibels);
        }
    }
}
