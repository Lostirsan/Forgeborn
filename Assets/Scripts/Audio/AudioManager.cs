using UnityEngine;
using UnityEngine.Audio;

namespace ForgeGame.Audio
{
    /// <summary>
    /// Plays menu music and one-shot UI feedback sounds. Everything is optional:
    /// if a clip or source is missing the call is silently ignored, so the menu
    /// works before any audio assets exist. Volume is handled by the mixer, so
    /// this class only deals with routing and playback.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Mixer routing (optional)")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("Clips (optional)")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip closeClip;

        private void Awake()
        {
            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                if (musicGroup != null) musicSource.outputAudioMixerGroup = musicGroup;
            }

            if (sfxSource != null)
            {
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
            }
        }

        private void Start()
        {
            PlayMenuMusic();
        }

        public void PlayMenuMusic()
        {
            if (musicSource == null || menuMusic == null) return;
            if (musicSource.isPlaying && musicSource.clip == menuMusic) return;
            musicSource.clip = menuMusic;
            musicSource.Play();
        }

        public void PlayHover() => PlayOneShot(hoverClip);
        public void PlayClick() => PlayOneShot(clickClip);
        public void PlayClose() => PlayOneShot(closeClip);

        private void PlayOneShot(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}
