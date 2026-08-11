using System;
using UnityEngine;
using UnityEngine.Audio;

namespace ForgeGame.Settings
{
    /// <summary>
    /// Owns the live <see cref="GameSettings"/>, applies them to the engine
    /// (audio mixer, screen, frame-rate, UI scale) and persists them through an
    /// <see cref="ISettingsStore"/>. There is exactly one of these in the menu
    /// scene; other components receive it through a serialized reference rather
    /// than a global singleton.
    /// </summary>
    public class SettingsService : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Optional. When assigned, volumes are applied to these exposed parameters in dB.")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        [Header("Interface")]
        [Tooltip("Optional. UI scale is applied to this transform's local scale.")]
        [SerializeField] private RectTransform uiScaleRoot;

        private readonly ISettingsStore _store = new PlayerPrefsSettingsStore();
        private GameSettings _current;

        /// <summary>The live settings model. Never null after Awake.</summary>
        public GameSettings Current => _current;

        /// <summary>Raised after settings are applied so listeners can react (e.g. gameplay VFX).</summary>
        public event Action<GameSettings> Changed;

        private void Awake()
        {
            _current = _store.Load();
        }

        private void Start()
        {
            ApplyAll();
        }

        /// <summary>Replaces the live model with a copy of <paramref name="settings"/> and applies it.</summary>
        public void SetAndApply(GameSettings settings)
        {
            if (settings == null) return;
            _current.CopyFrom(settings);
            ApplyAll();
        }

        /// <summary>Persists the current model.</summary>
        public void Save()
        {
            _store.Save(_current);
        }

        /// <summary>Applies every settings category to the engine and notifies listeners.</summary>
        public void ApplyAll()
        {
            ApplyAudio();
            ApplyGraphics();
            ApplyInterface();
            Changed?.Invoke(_current);
        }

        public void ApplyAudio()
        {
            if (audioMixer == null) return;
            bool muted = _current.muteAll;
            SetMixerVolume(masterVolumeParam, muted ? 0f : _current.masterVolume);
            SetMixerVolume(musicVolumeParam, muted ? 0f : _current.musicVolume);
            SetMixerVolume(sfxVolumeParam, muted ? 0f : _current.sfxVolume);
        }

        public void ApplyGraphics()
        {
            // Frame pacing: VSync takes priority; only cap FPS when VSync is off.
            QualitySettings.vSyncCount = _current.vSync ? 1 : 0;
            Application.targetFrameRate = _current.vSync ? -1 : GameSettings.FpsLimitToValue(_current.fpsLimit);

            int w = Mathf.Max(640, _current.resolutionWidth);
            int h = Mathf.Max(480, _current.resolutionHeight);
            FullScreenMode mode = ToFullScreenMode(_current.windowMode);

#if !UNITY_EDITOR
            // Avoid resizing the editor game view; only touch the real player window.
            if (Screen.width != w || Screen.height != h || Screen.fullScreenMode != mode)
                Screen.SetResolution(w, h, mode);
#else
            Screen.fullScreenMode = mode;
#endif
        }

        public void ApplyInterface()
        {
            if (uiScaleRoot != null)
                uiScaleRoot.localScale = Vector3.one * Mathf.Clamp(_current.uiScale, 0.5f, 2f);
        }

        private void SetMixerVolume(string param, float linear01)
        {
            if (string.IsNullOrEmpty(param) || audioMixer == null) return;
            audioMixer.SetFloat(param, LinearToDecibels(linear01));
        }

        /// <summary>Converts a 0..1 linear volume to a mixer-friendly decibel value.</summary>
        public static float LinearToDecibels(float linear01)
        {
            float clamped = Mathf.Clamp(linear01, 0.0001f, 1f);
            return Mathf.Log10(clamped) * 20f;
        }

        private static FullScreenMode ToFullScreenMode(WindowMode mode)
        {
            switch (mode)
            {
                case WindowMode.Fullscreen: return FullScreenMode.ExclusiveFullScreen;
                case WindowMode.Borderless: return FullScreenMode.FullScreenWindow;
                default: return FullScreenMode.Windowed;
            }
        }
    }
}
