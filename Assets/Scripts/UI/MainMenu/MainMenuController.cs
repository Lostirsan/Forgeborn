using ForgeGame.Audio;
using ForgeGame.Save;
using ForgeGame.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ForgeGame.UI.MainMenu
{
    /// <summary>
    /// Top-level controller for the main menu. It owns the save service, wires the
    /// primary buttons, opens/closes the overlay panels, and routes the Escape /
    /// cancel input by priority (modal → panel → quit confirmation).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private ScreenFader fader;
        [SerializeField] private AudioManager audioManager;

        [Header("Scene flow")]
        [Tooltip("Scene loaded by New Game and by Continue when the save has no explicit scene.")]
        [SerializeField] private string forgeSceneName = "Forge";

        [Header("Main buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanelRoot;
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private GameObject creditsPanelRoot;
        [SerializeField] private Button creditsBackButton;
        [SerializeField] private ConfirmationModal confirmationModal;
        [SerializeField] private GameObject loadingBlocker;

        [Header("Footer")]
        [SerializeField] private TMP_Text versionText;

        [Header("Focus containment")]
        [Tooltip("Branding + main buttons group; disabled while an overlay is open so navigation stays inside it.")]
        [SerializeField] private CanvasGroup mainContentGroup;

        private ISaveGameService _saveService;
        private GameObject _selectionBeforeOverlay;
        private bool _isLoading;

        private bool SettingsOpen => settingsPanelRoot != null && settingsPanelRoot.activeSelf;
        private bool CreditsOpen => creditsPanelRoot != null && creditsPanelRoot.activeSelf;
        private bool ModalOpen => confirmationModal != null && confirmationModal.IsOpen;

        private void Awake()
        {
            _saveService = new LocalSaveGameService();

            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (creditsButton != null) creditsButton.onClick.AddListener(OnCredits);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnSettingsBack);
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(OnCreditsBack);
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
            if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGame);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCredits);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
            if (settingsBackButton != null) settingsBackButton.onClick.RemoveListener(OnSettingsBack);
            if (creditsBackButton != null) creditsBackButton.onClick.RemoveListener(OnCreditsBack);
        }

        private void Start()
        {
            if (versionText != null)
                versionText.text = $"v{Application.version}";

            if (settingsPanelRoot != null) settingsPanelRoot.SetActive(false);
            if (creditsPanelRoot != null) creditsPanelRoot.SetActive(false);
            if (loadingBlocker != null) loadingBlocker.SetActive(false);
            if (confirmationModal != null) confirmationModal.Hide();
            SetMainContentInteractable(true);

            bool hasSave = _saveService.HasSave;
            if (continueButton != null) continueButton.interactable = hasSave;

            fader?.FadeIn();

            GameObject first = (hasSave && continueButton != null)
                ? continueButton.gameObject
                : (newGameButton != null ? newGameButton.gameObject : null);
            SelectGameObject(first);
        }

        private void Update()
        {
            if (_isLoading) return;
            if (WasCancelPressed())
                HandleCancel();
        }

        // ---- Main button handlers ----

        public void OnContinue()
        {
            if (!_saveService.HasSave) return;
            audioManager?.PlayClick();

            SaveData data = _saveService.Load();
            string scene = (data != null && !string.IsNullOrEmpty(data.sceneName))
                ? data.sceneName
                : forgeSceneName;
            StartSceneLoad(scene);
        }

        public void OnNewGame()
        {
            string message = _saveService.HasSave
                ? "Начать новую игру? Существующее сохранение будет перезаписано."
                : "Начать новую игру?";

            OpenModal(message, "Начать", () =>
            {
                _saveService.CreateNewGame();
                StartSceneLoad(forgeSceneName);
            });
        }

        public void OnSettings()
        {
            if (settingsPanelRoot == null || settingsPanel == null) return;
            _selectionBeforeOverlay = CurrentSelection();
            audioManager?.PlayClick();
            SetMainContentInteractable(false);
            settingsPanelRoot.SetActive(true);
            settingsPanel.OnOpened();
        }

        public void OnCredits()
        {
            if (creditsPanelRoot == null) return;
            _selectionBeforeOverlay = CurrentSelection();
            audioManager?.PlayClick();
            SetMainContentInteractable(false);
            creditsPanelRoot.SetActive(true);
        }

        public void OnQuit()
        {
            OpenModal("Выйти из игры?", "Выйти", QuitGame);
        }

        // ---- Back buttons (wired by the generator) ----

        public void OnSettingsBack()
        {
            if (!SettingsOpen) return;
            settingsPanel?.RevertUnsaved();
            settingsPanelRoot.SetActive(false);
            audioManager?.PlayClose();
            SetMainContentInteractable(true);
            RestoreSelection();
        }

        public void OnCreditsBack()
        {
            if (!CreditsOpen) return;
            creditsPanelRoot.SetActive(false);
            audioManager?.PlayClose();
            SetMainContentInteractable(true);
            RestoreSelection();
        }

        // ---- Flow helpers ----

        private void OpenModal(string message, string confirmText, System.Action onConfirm)
        {
            if (confirmationModal == null)
            {
                // No modal wired: fail safe by performing the action directly.
                onConfirm?.Invoke();
                return;
            }

            _selectionBeforeOverlay = CurrentSelection();
            audioManager?.PlayClick();
            SetMainContentInteractable(false);
            confirmationModal.Show(
                message, confirmText, "Отмена",
                () => { SetMainContentInteractable(true); onConfirm?.Invoke(); },
                () => { SetMainContentInteractable(true); RestoreSelection(); });
        }

        private void SetMainContentInteractable(bool on)
        {
            if (mainContentGroup == null) return;
            mainContentGroup.interactable = on;
            mainContentGroup.blocksRaycasts = on;
        }

        private void StartSceneLoad(string sceneName)
        {
            if (_isLoading) return;

            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[ForgeGame] Cannot start game: scene '{sceneName}' is missing from Build Settings. " +
                               "Set a valid start scene on MainMenuController.forgeSceneName.");
                return;
            }

            _isLoading = true;
            if (loadingBlocker != null) loadingBlocker.SetActive(true);

            if (fader != null)
            {
                fader.LoadSceneWithFade(sceneName, OnSceneLoadFailed);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        private void OnSceneLoadFailed()
        {
            _isLoading = false;
            if (loadingBlocker != null) loadingBlocker.SetActive(false);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- Input / focus ----

        private void HandleCancel()
        {
            if (ModalOpen)
            {
                audioManager?.PlayClose();
                confirmationModal.CancelFromExternal();
            }
            else if (SettingsOpen)
            {
                OnSettingsBack();
            }
            else if (CreditsOpen)
            {
                OnCreditsBack();
            }
            else
            {
                OnQuit();
            }
        }

        private static bool WasCancelPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;

            Gamepad gp = Gamepad.current;
            if (gp != null && (gp.bButton.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)) return true;

            return false;
        }

        private static GameObject CurrentSelection()
        {
            return EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        }

        private static void SelectGameObject(GameObject go)
        {
            if (go != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(go);
        }

        private void RestoreSelection()
        {
            SelectGameObject(_selectionBeforeOverlay);
        }
    }
}
