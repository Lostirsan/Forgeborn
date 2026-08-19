using ForgeGame.Inventory;
using ForgeGame.Smithy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>Persistent HUD: money, current objective and quick-access buttons.</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private SmithyController controller;
        [SerializeField] private InventoryService inventory;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button journalButton;
        [SerializeField] private Button dungeonButton;

        private void OnEnable()
        {
            if (inventory != null) inventory.InventoryChanged += Refresh;
            if (inventoryButton != null) inventoryButton.onClick.AddListener(OpenInventory);
            if (journalButton != null) journalButton.onClick.AddListener(OpenJournal);
            if (dungeonButton != null) dungeonButton.onClick.AddListener(OpenDungeonPrep);
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            if (inventoryButton != null) inventoryButton.onClick.RemoveListener(OpenInventory);
            if (journalButton != null) journalButton.onClick.RemoveListener(OpenJournal);
            if (dungeonButton != null) dungeonButton.onClick.RemoveListener(OpenDungeonPrep);
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }

        private void Refresh()
        {
            if (moneyText != null && inventory != null)
                moneyText.text = $"Золото: {inventory.Money}";
        }

        private void OpenInventory() => controller?.OpenPanel(PanelId.Inventory);
        private void OpenJournal() => controller?.OpenPanel(PanelId.Journal);
        private void OpenDungeonPrep() => controller?.OpenPanel(PanelId.DungeonPrep);
    }
}
