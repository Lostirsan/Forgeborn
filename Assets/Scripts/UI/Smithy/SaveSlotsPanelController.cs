using System;
using ForgeGame.Localization;
using ForgeGame.Smithy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Multi-slot save/load. Each row is one slot: its name + last-played time, with Save
    /// (write the current game here), Load (resume that slot) and Delete. The name field at the
    /// top provides "save as" — type a name, press Save on any row. Rows are fixed (built by the
    /// scene generator); this controller just fills and wires them from the save service.
    /// </summary>
    public class SaveSlotsPanelController : SmithyPanel
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_Text[] nameLabels;
        [SerializeField] private TMP_Text[] dateLabels;
        [SerializeField] private Button[] saveButtons;
        [SerializeField] private Button[] loadButtons;
        [SerializeField] private Button[] deleteButtons;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                int slot = i; // capture per row
                if (saveButtons != null && i < saveButtons.Length && saveButtons[i] != null)
                    saveButtons[i].onClick.AddListener(() => OnSave(slot));
                if (loadButtons != null && i < loadButtons.Length && loadButtons[i] != null)
                    loadButtons[i].onClick.AddListener(() => OnLoad(slot));
                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                    deleteButtons[i].onClick.AddListener(() => OnDelete(slot));
            }
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.OpenPanel(PanelId.Pause));
        }

        private int SlotCount => nameLabels != null ? nameLabels.Length : 0;

        protected override void OnOpened() => Populate();

        private void Populate()
        {
            if (Controller == null) return;
            var slots = Controller.GetSlots();
            int active = Controller.ActiveSlot;
            for (int i = 0; i < SlotCount; i++)
            {
                bool has = i < slots.Count;
                var info = has ? slots[i] : default;
                bool used = has && info.used;

                if (nameLabels[i] != null)
                {
                    nameLabels[i].text = used ? info.name : Loc.Tr("common.empty");
                    nameLabels[i].color = i == active ? new Color(0.98f, 0.82f, 0.4f) : new Color(0.9f, 0.85f, 0.75f);
                }
                if (dateLabels != null && i < dateLabels.Length && dateLabels[i] != null)
                    dateLabels[i].text = used ? FormatTime(info.savedAtUnix) : "";
                if (loadButtons != null && i < loadButtons.Length && loadButtons[i] != null)
                    loadButtons[i].interactable = used;
                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                    deleteButtons[i].interactable = used;
            }
        }

        private static string FormatTime(long unix)
        {
            if (unix <= 0) return "";
            try { return DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime.ToString("dd.MM.yyyy HH:mm"); }
            catch { return ""; }
        }

        private void OnSave(int slot)
        {
            string name = nameInput != null ? nameInput.text : null;
            Controller?.SaveToSlot(slot, name);
            if (nameInput != null) nameInput.text = "";
            Populate();
        }

        private void OnLoad(int slot) => Controller?.LoadSlot(slot); // reloads the scene

        private void OnDelete(int slot)
        {
            Controller?.DeleteSlot(slot);
            Populate();
        }
    }
}
