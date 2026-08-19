using ForgeGame.Smithy;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>Pause menu: resume, save, settings, return to the main menu.</summary>
    public class PausePanelController : SmithyPanel
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button slotsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (saveButton != null) saveButton.onClick.AddListener(() => Controller?.SaveGame());
            if (slotsButton != null) slotsButton.onClick.AddListener(() => Controller?.OpenPanel(PanelId.SaveSlots));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => Controller?.OpenPanel(PanelId.Settings));
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMenu);
        }

        private void ReturnToMenu()
        {
            // Auto-save so no progress is lost, then transition.
            Controller?.SaveGame(true);
            Controller?.ReturnToMainMenu();
        }
    }
}
