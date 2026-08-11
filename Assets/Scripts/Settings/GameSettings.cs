using System;
using UnityEngine;

namespace ForgeGame.Settings
{
    /// <summary>Preferred window presentation mode.</summary>
    public enum WindowMode
    {
        Fullscreen = 0,
        Borderless = 1,
        Windowed = 2
    }

    /// <summary>Frame-rate cap options exposed to the player.</summary>
    public enum FpsLimit
    {
        Fps30 = 0,
        Fps60 = 1,
        Fps120 = 2,
        Fps144 = 3,
        Unlimited = 4
    }

    /// <summary>
    /// Plain serializable data model holding every player-facing option.
    /// It contains data only; applying and persisting it is done by
    /// <see cref="SettingsService"/> so the model stays storage-agnostic.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        // ---- Audio (linear 0..1, converted to dB when applied) ----
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;
        public bool muteAll = false;

        // ---- Graphics ----
        public WindowMode windowMode = WindowMode.Fullscreen;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public bool vSync = true;
        public FpsLimit fpsLimit = FpsLimit.Fps60;

        // ---- Interface ----
        [Range(0.75f, 1.25f)] public float uiScale = 1f;
        public bool screenShake = true;
        [Range(0f, 1f)] public float effectsIntensity = 1f;
        public string language = "ru";

        /// <summary>Creates a fresh copy so panels can edit without touching the live model.</summary>
        public GameSettings Clone()
        {
            return (GameSettings)MemberwiseClone();
        }

        /// <summary>Copies every value from <paramref name="other"/> into this instance.</summary>
        public void CopyFrom(GameSettings other)
        {
            if (other == null) return;
            masterVolume = other.masterVolume;
            musicVolume = other.musicVolume;
            sfxVolume = other.sfxVolume;
            muteAll = other.muteAll;
            windowMode = other.windowMode;
            resolutionWidth = other.resolutionWidth;
            resolutionHeight = other.resolutionHeight;
            vSync = other.vSync;
            fpsLimit = other.fpsLimit;
            uiScale = other.uiScale;
            screenShake = other.screenShake;
            effectsIntensity = other.effectsIntensity;
            language = other.language;
        }

        /// <summary>Numeric target for <see cref="FpsLimit"/>; -1 means "no cap".</summary>
        public static int FpsLimitToValue(FpsLimit limit)
        {
            switch (limit)
            {
                case FpsLimit.Fps30: return 30;
                case FpsLimit.Fps60: return 60;
                case FpsLimit.Fps120: return 120;
                case FpsLimit.Fps144: return 144;
                default: return -1;
            }
        }
    }
}
