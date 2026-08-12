using System.Collections.Generic;
using ForgeGame.Localization;
using ForgeGame.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeGame.UI.MainMenu
{
    /// <summary>
    /// Presents the settings screen. It edits a working copy of the settings,
    /// applies changes live for immediate feedback, and only persists them when
    /// the player presses Apply. Back (handled by the owning menu) reverts any
    /// unsaved changes to the snapshot taken when the panel opened.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("Service")]
        [SerializeField] private SettingsService settingsService;

        [Header("Sections / tabs")]
        [SerializeField] private GameObject soundSection;
        [SerializeField] private GameObject graphicsSection;
        [SerializeField] private GameObject interfaceSection;
        [SerializeField] private Button soundTabButton;
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button interfaceTabButton;

        [Header("Sound controls")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle muteToggle;

        [Header("Graphics controls")]
        [SerializeField] private TMP_Dropdown windowModeDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown fpsDropdown;

        [Header("Interface controls")]
        [SerializeField] private Slider uiScaleSlider;
        [SerializeField] private Toggle screenShakeToggle;
        [SerializeField] private Slider effectsIntensitySlider;
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Footer buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;

        [Header("Default focus")]
        [SerializeField] private GameObject firstSelected;

        private GameSettings _working;
        private GameSettings _snapshot;
        private bool _populating;
        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();

        // Language selector (Unity Localization). Order = dropdown option order.
        private static readonly string[] LangCodes = { "ru", "en", "es", "hi" };
        private static readonly string[] LangNames = { "Русский", "English", "Español", "हिन्दी" };

        private void Awake()
        {
            HookControls();
            HookTabs();
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
        }

        private void OnDestroy()
        {
            UnhookControls();
            UnhookTabs();
            if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetToDefaults);
        }

        /// <summary>Called by the owning menu right after the panel is activated.</summary>
        public void OnOpened()
        {
            if (settingsService == null)
            {
                Debug.LogError("[ForgeGame] SettingsPanelController has no SettingsService assigned.");
                return;
            }

            _snapshot = settingsService.Current.Clone();
            _working = settingsService.Current.Clone();

            BuildStaticDropdownOptions();
            BuildResolutionOptions();
            Populate();
            ShowSection(0);

            if (firstSelected != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstSelected);
        }

        /// <summary>Reverts any unsaved edits. Called by the menu when closing without Apply.</summary>
        public void RevertUnsaved()
        {
            if (settingsService != null && _snapshot != null)
                settingsService.SetAndApply(_snapshot);
        }

        // ---- Buttons (wired by the generator to these public methods) ----

        public void Apply()
        {
            if (settingsService == null) return;
            settingsService.SetAndApply(_working);
            settingsService.Save();
            _snapshot = settingsService.Current.Clone();
        }

        public void ResetToDefaults()
        {
            _working = new GameSettings();
            Populate();
            settingsService?.SetAndApply(_working);
        }

        // ---- Live editing ----

        private void ApplyLive()
        {
            if (_populating) return;
            settingsService?.SetAndApply(_working);
        }

        private void Populate()
        {
            _populating = true;

            if (masterSlider != null) masterSlider.SetValueWithoutNotify(_working.masterVolume);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(_working.musicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(_working.sfxVolume);
            if (muteToggle != null) muteToggle.SetIsOnWithoutNotify(_working.muteAll);

            if (windowModeDropdown != null) windowModeDropdown.SetValueWithoutNotify((int)_working.windowMode);
            if (resolutionDropdown != null) resolutionDropdown.SetValueWithoutNotify(CurrentResolutionIndex());
            if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(_working.vSync);
            if (fpsDropdown != null) fpsDropdown.SetValueWithoutNotify((int)_working.fpsLimit);

            if (uiScaleSlider != null) uiScaleSlider.SetValueWithoutNotify(_working.uiScale);
            if (screenShakeToggle != null) screenShakeToggle.SetIsOnWithoutNotify(_working.screenShake);
            if (effectsIntensitySlider != null) effectsIntensitySlider.SetValueWithoutNotify(_working.effectsIntensity);
            if (languageDropdown != null) languageDropdown.SetValueWithoutNotify(CurrentLanguageIndex());

            _populating = false;
        }

        // ---- Dropdown option builders (no manual Inspector setup needed) ----

        private void BuildStaticDropdownOptions()
        {
            SetOptions(windowModeDropdown, "Полноэкранный", "Без рамки", "Оконный");
            SetOptions(fpsDropdown, "30", "60", "120", "144", "Без ограничения");
            SetOptions(languageDropdown, LangNames);
        }

        private void BuildResolutionOptions()
        {
            _resolutions.Clear();
            var seen = new HashSet<long>();
            foreach (var r in Screen.resolutions)
            {
                long key = ((long)r.width << 32) | (uint)r.height;
                if (seen.Add(key))
                    _resolutions.Add(new Vector2Int(r.width, r.height));
            }

            var current = new Vector2Int(_working.resolutionWidth, _working.resolutionHeight);
            if (!_resolutions.Contains(current))
                _resolutions.Add(current);

            if (resolutionDropdown != null)
            {
                var options = new List<string>(_resolutions.Count);
                foreach (var res in _resolutions)
                    options.Add($"{res.x} x {res.y}");
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(options);
            }
        }

        private int CurrentResolutionIndex()
        {
            var current = new Vector2Int(_working.resolutionWidth, _working.resolutionHeight);
            int idx = _resolutions.IndexOf(current);
            return idx < 0 ? 0 : idx;
        }

        private static void SetOptions(TMP_Dropdown dropdown, params string[] options)
        {
            if (dropdown == null) return;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
        }

        // ---- Control wiring ----

        private void HookControls()
        {
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (muteToggle != null) muteToggle.onValueChanged.AddListener(OnMuteChanged);

            if (windowModeDropdown != null) windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (vsyncToggle != null) vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (fpsDropdown != null) fpsDropdown.onValueChanged.AddListener(OnFpsChanged);

            if (uiScaleSlider != null) uiScaleSlider.onValueChanged.AddListener(OnUiScaleChanged);
            if (screenShakeToggle != null) screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
            if (effectsIntensitySlider != null) effectsIntensitySlider.onValueChanged.AddListener(OnEffectsChanged);
            if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        private void UnhookControls()
        {
            if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (muteToggle != null) muteToggle.onValueChanged.RemoveListener(OnMuteChanged);

            if (windowModeDropdown != null) windowModeDropdown.onValueChanged.RemoveListener(OnWindowModeChanged);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (vsyncToggle != null) vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
            if (fpsDropdown != null) fpsDropdown.onValueChanged.RemoveListener(OnFpsChanged);

            if (uiScaleSlider != null) uiScaleSlider.onValueChanged.RemoveListener(OnUiScaleChanged);
            if (screenShakeToggle != null) screenShakeToggle.onValueChanged.RemoveListener(OnScreenShakeChanged);
            if (effectsIntensitySlider != null) effectsIntensitySlider.onValueChanged.RemoveListener(OnEffectsChanged);
            if (languageDropdown != null) languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }

        private int CurrentLanguageIndex()
        {
            int i = System.Array.IndexOf(LangCodes, Loc.CurrentCode);
            return i < 0 ? 0 : i;
        }

        private void OnLanguageChanged(int i)
        {
            if (_populating) return;
            if (i >= 0 && i < LangCodes.Length) Loc.SetLocale(LangCodes[i]);
        }

        private void OnMasterChanged(float v) { _working.masterVolume = v; ApplyLive(); }
        private void OnMusicChanged(float v) { _working.musicVolume = v; ApplyLive(); }
        private void OnSfxChanged(float v) { _working.sfxVolume = v; ApplyLive(); }
        private void OnMuteChanged(bool v) { _working.muteAll = v; ApplyLive(); }

        private void OnWindowModeChanged(int i) { _working.windowMode = (WindowMode)i; ApplyLive(); }
        private void OnResolutionChanged(int i)
        {
            if (i >= 0 && i < _resolutions.Count)
            {
                _working.resolutionWidth = _resolutions[i].x;
                _working.resolutionHeight = _resolutions[i].y;
            }
            ApplyLive();
        }
        private void OnVSyncChanged(bool v) { _working.vSync = v; ApplyLive(); }
        private void OnFpsChanged(int i) { _working.fpsLimit = (FpsLimit)i; ApplyLive(); }

        private void OnUiScaleChanged(float v) { _working.uiScale = v; ApplyLive(); }
        private void OnScreenShakeChanged(bool v) { _working.screenShake = v; ApplyLive(); }
        private void OnEffectsChanged(float v) { _working.effectsIntensity = v; ApplyLive(); }

        // ---- Tabs ----

        private void HookTabs()
        {
            if (soundTabButton != null) soundTabButton.onClick.AddListener(ShowSound);
            if (graphicsTabButton != null) graphicsTabButton.onClick.AddListener(ShowGraphics);
            if (interfaceTabButton != null) interfaceTabButton.onClick.AddListener(ShowInterface);
        }

        private void UnhookTabs()
        {
            if (soundTabButton != null) soundTabButton.onClick.RemoveListener(ShowSound);
            if (graphicsTabButton != null) graphicsTabButton.onClick.RemoveListener(ShowGraphics);
            if (interfaceTabButton != null) interfaceTabButton.onClick.RemoveListener(ShowInterface);
        }

        private void ShowSound() => ShowSection(0);
        private void ShowGraphics() => ShowSection(1);
        private void ShowInterface() => ShowSection(2);

        private void ShowSection(int index)
        {
            if (soundSection != null) soundSection.SetActive(index == 0);
            if (graphicsSection != null) graphicsSection.SetActive(index == 1);
            if (interfaceSection != null) interfaceSection.SetActive(index == 2);
        }
    }
}
