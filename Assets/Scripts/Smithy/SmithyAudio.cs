using UnityEngine;
using UnityEngine.Audio;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Local audio adapter for the smithy. All clip fields are optional; missing
    /// clips are silently ignored so the scene runs without any audio assets. A
    /// looping source covers ambience (fire/bellows) and a one-shot source covers
    /// events. Routed to the shared SFX mixer group when assigned.
    /// </summary>
    public class SmithyAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("Clips (all optional)")]
        [SerializeField] private AudioClip fireLoop;
        [SerializeField] private AudioClip bellows;
        [SerializeField] private AudioClip hammer;
        [SerializeField] private AudioClip water;
        [SerializeField] private AudioClip steam;
        [SerializeField] private AudioClip grind;
        [SerializeField] private AudioClip uiOpen;
        [SerializeField] private AudioClip itemGet;
        [SerializeField] private AudioClip weaponDone;

        [Header("Ambience (shop/forge cross-fade, all optional)")]
        [SerializeField] private AudioSource streetSource;
        [SerializeField] private AudioSource forgeSource;
        [SerializeField] private AudioClip streetAmbience;
        [SerializeField] private AudioClip forgeAmbience;
        [SerializeField] private AudioClip transitionCreak;
        [SerializeField] private float ambienceVolume = 0.6f;

        private void Awake()
        {
            if (loopSource != null)
            {
                loopSource.loop = true;
                loopSource.playOnAwake = false;
                if (sfxGroup != null) loopSource.outputAudioMixerGroup = sfxGroup;
            }
            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
                if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
            }
            ConfigureAmbience(streetSource, streetAmbience);
            ConfigureAmbience(forgeSource, forgeAmbience);
        }

        private void ConfigureAmbience(AudioSource src, AudioClip clip)
        {
            if (src == null) return;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
            if (clip != null) { src.clip = clip; src.Play(); }
        }

        private void Start()
        {
            if (loopSource != null && fireLoop != null)
            {
                loopSource.clip = fireLoop;
                loopSource.volume = 0.5f;
                loopSource.Play();
            }
        }

        /// <summary>Cross-fades street↔forge ambience. 0 = pure shop/street, 1 = pure forge.</summary>
        public void SetAmbienceBlend(float forge01)
        {
            forge01 = Mathf.Clamp01(forge01);
            if (streetSource != null) streetSource.volume = (1f - forge01) * ambienceVolume;
            if (forgeSource != null) forgeSource.volume = forge01 * ambienceVolume;
        }

        public void PlayTransitionCreak() => One(transitionCreak);

        private void One(AudioClip c) { if (sfxSource != null && c != null) sfxSource.PlayOneShot(c); }

        public void PlayBellows() => One(bellows);
        public void PlayHammer() => One(hammer);
        public void PlayWater() => One(water);
        public void PlaySteam() => One(steam);
        public void PlayGrind() => One(grind);
        public void PlayUiOpen() => One(uiOpen);
        public void PlayItemGet() => One(itemGet);
        public void PlayWeaponDone() => One(weaponDone);
    }
}
