using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.Dungeon
{
    /// <summary>
    /// A simple 9-slot dungeon inventory shown as a hotbar. Picked-up items stack into a slot with
    /// the same id, otherwise take the first empty slot. Display-only for now (no drag/use yet).
    /// </summary>
    public class DungeonHotbar : MonoBehaviour
    {
        [SerializeField] private Image[] slotIcons;   // 9
        [SerializeField] private TMP_Text[] slotCounts; // 9

        private readonly string[] _ids = new string[9];
        private readonly int[] _counts = new int[9];
        private readonly Sprite[] _sprites = new Sprite[9];

        /// <summary>Add items; returns false only if the hotbar is full.</summary>
        public bool Add(string id, Sprite icon, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return false;
            for (int i = 0; i < 9; i++)
                if (_ids[i] == id) { _counts[i] += amount; Refresh(i); return true; }
            for (int i = 0; i < 9; i++)
                if (string.IsNullOrEmpty(_ids[i])) { _ids[i] = id; _sprites[i] = icon; _counts[i] = amount; Refresh(i); return true; }
            return false;
        }

        private void Refresh(int i)
        {
            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotIcons[i].sprite = _sprites[i];
                slotIcons[i].enabled = _sprites[i] != null;
            }
            if (slotCounts != null && i < slotCounts.Length && slotCounts[i] != null)
                slotCounts[i].text = _counts[i] > 1 ? _counts[i].ToString() : "";
        }
    }
}
