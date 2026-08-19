using ForgeGame.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ForgeGame.Dungeon
{
    /// <summary>
    /// The dungeon's ESC menu: a pause panel (Resume / Settings / Exit to menu) plus a compact
    /// settings sub-panel (language, master volume, fullscreen). Opening it freezes the run and
    /// the fight via <see cref="Time.timeScale"/>; resuming restores it. Self-contained — it does
    /// not depend on the smithy's panel stack, only the shared <see cref="Loc"/> facade.
    /// </summary>
    public class DungeonPauseController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseRoot;
        [SerializeField] private GameObject settingsRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button smithyButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private string smithySceneName = "Smithy";

        public bool IsOpen => (pauseRoot != null && pauseRoot.activeSelf) || SettingsOpen;
        public bool SettingsOpen => settingsRoot != null && settingsRoot.activeSelf;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (smithyButton != null) smithyButton.onClick.AddListener(ReturnToSmithy);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (exitButton != null) exitButton.onClick.AddListener(ExitToMenu);
            if (backButton != null) backButton.onClick.AddListener(CloseSettings);
            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(AudioListener.volume);
                masterSlider.onValueChanged.AddListener(v => AudioListener.volume = v);
            }
            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
                fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
            }
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (settingsRoot != null) settingsRoot.SetActive(false);
        }

        public void Open()
        {
            if (pauseRoot != null) pauseRoot.SetActive(true);
            if (settingsRoot != null) settingsRoot.SetActive(false);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (settingsRoot != null) settingsRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        public void OpenSettings()
        {
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (settingsRoot != null) settingsRoot.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsRoot != null) settingsRoot.SetActive(false);
            if (pauseRoot != null) pauseRoot.SetActive(true);
        }

        // Wired to the four language buttons (via a persistent string listener).
        public void SetLanguage(string code) => Loc.SetLocale(code);

        // Leave the run and return to the smithy — the loot gathered so far is banked into the
        // saved inventory when the smithy loads (via ExpeditionResult).
        private void ReturnToSmithy()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(smithySceneName);
        }

        private void ExitToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
        }

        // Safety: never leave the game frozen if this object is torn down while paused.
        private void OnDisable() => Time.timeScale = 1f;
    }
}
