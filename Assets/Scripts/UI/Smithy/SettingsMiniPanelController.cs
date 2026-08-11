using ForgeGame.Settings;
using ForgeGame.Smithy;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Compact in-scene settings reusing the shared <see cref="SettingsService"/>:
    /// volumes, effects intensity and screen shake. Applies and persists live.
    /// </summary>
    public class SettingsMiniPanelController : SmithyPanel
    {
        [SerializeField] private SettingsService settings;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider effectsSlider;
        [SerializeField] private Toggle shakeToggle;
        [SerializeField] private Button backButton;

        private bool _populating;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.OpenPanel(PanelId.Pause));
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(_ => Apply());
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(_ => Apply());
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(_ => Apply());
            if (effectsSlider != null) effectsSlider.onValueChanged.AddListener(_ => Apply());
            if (shakeToggle != null) shakeToggle.onValueChanged.AddListener(_ => Apply());
        }

        protected override void OnOpened()
        {
            if (settings == null) return;
            _populating = true;
            var c = settings.Current;
            masterSlider?.SetValueWithoutNotify(c.masterVolume);
            musicSlider?.SetValueWithoutNotify(c.musicVolume);
            sfxSlider?.SetValueWithoutNotify(c.sfxVolume);
            effectsSlider?.SetValueWithoutNotify(c.effectsIntensity);
            shakeToggle?.SetIsOnWithoutNotify(c.screenShake);
            _populating = false;
        }

        private void Apply()
        {
            if (_populating || settings == null) return;
            var c = settings.Current;
            if (masterSlider != null) c.masterVolume = masterSlider.value;
            if (musicSlider != null) c.musicVolume = musicSlider.value;
            if (sfxSlider != null) c.sfxVolume = sfxSlider.value;
            if (effectsSlider != null) c.effectsIntensity = effectsSlider.value;
            if (shakeToggle != null) c.screenShake = shakeToggle.isOn;
            settings.ApplyAll();
            settings.Save();
        }
    }
}
