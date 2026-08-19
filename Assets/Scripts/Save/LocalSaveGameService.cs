using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ForgeGame.Save
{
    /// <summary>
    /// Stores several named save slots as JSON files under
    /// <see cref="Application.persistentDataPath"/> (slot_0.json … slot_N.json). The active
    /// slot is remembered in PlayerPrefs so autosave and the main menu's Continue target the
    /// right file across launches. A legacy single-file save is migrated into slot 0 on first
    /// run. All file access is guarded so a corrupt file reads as "empty" instead of throwing.
    /// </summary>
    public sealed class LocalSaveGameService : ISaveGameService
    {
        private const int Slots = 6;
        private const string LegacyFile = "savegame.json";
        private const string ActiveSlotPref = "forge.activeSlot";

        public int SlotCount => Slots;

        private static string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"slot_{slot}.json");
        private static string LegacyPath => Path.Combine(Application.persistentDataPath, LegacyFile);

        public LocalSaveGameService()
        {
            MigrateLegacy();
        }

        // ---- Active slot ----

        public int ActiveSlot
        {
            get { int s = PlayerPrefs.GetInt(ActiveSlotPref, 0); return (s >= 0 && s < Slots) ? s : 0; }
        }

        public void SetActiveSlot(int slot)
        {
            if (slot < 0 || slot >= Slots) return;
            PlayerPrefs.SetInt(ActiveSlotPref, slot);
            PlayerPrefs.Save();
        }

        // ---- Single-slot facade (acts on the active slot) ----

        public bool HasSave => MostRecentSlot() >= 0;

        public SaveData Load()
        {
            if (SlotUsed(ActiveSlot)) return Load(ActiveSlot);
            int recent = MostRecentSlot();
            if (recent >= 0) { SetActiveSlot(recent); return Load(recent); }
            return null;
        }

        public SaveData CreateNewGame()
        {
            int slot = FirstFreeSlot();
            if (slot < 0) slot = ActiveSlot; // all full → reuse the active one
            var data = SaveData.CreateNew();
            data.slotName = "Новая игра";
            Save(slot, data);
            SetActiveSlot(slot);
            return data;
        }

        public void Save(SaveData data) => Save(ActiveSlot, data);

        public void Delete() => Delete(ActiveSlot);

        // ---- Slot operations ----

        public SaveData Load(int slot)
        {
            string path = SlotPath(slot);
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ForgeGame] Could not read slot {slot}: {e.Message}");
                return null;
            }
        }

        public void Save(int slot, SaveData data)
        {
            if (data == null || slot < 0 || slot >= Slots) return;
            try
            {
                // Preserve an existing slot name when the caller didn't supply one.
                if (string.IsNullOrEmpty(data.slotName))
                {
                    var existing = Load(slot);
                    data.slotName = existing != null && !string.IsNullOrEmpty(existing.slotName)
                        ? existing.slotName : $"Слот {slot + 1}";
                }
                data.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[ForgeGame] Could not write slot {slot}: {e.Message}");
            }
        }

        public void Delete(int slot)
        {
            try { if (File.Exists(SlotPath(slot))) File.Delete(SlotPath(slot)); }
            catch (Exception e) { Debug.LogError($"[ForgeGame] Could not delete slot {slot}: {e.Message}"); }
        }

        public IReadOnlyList<SaveSlotInfo> ListSlots()
        {
            var list = new List<SaveSlotInfo>(Slots);
            for (int i = 0; i < Slots; i++)
            {
                var data = Load(i);
                list.Add(new SaveSlotInfo
                {
                    index = i,
                    used = data != null,
                    name = data != null && !string.IsNullOrEmpty(data.slotName) ? data.slotName : $"Слот {i + 1}",
                    savedAtUnix = data?.savedAtUnix ?? 0,
                });
            }
            return list;
        }

        // ---- Helpers ----

        private bool SlotUsed(int slot) => File.Exists(SlotPath(slot));

        private int FirstFreeSlot()
        {
            for (int i = 0; i < Slots; i++) if (!SlotUsed(i)) return i;
            return -1;
        }

        private int MostRecentSlot()
        {
            int best = -1; long bestTime = -1;
            for (int i = 0; i < Slots; i++)
            {
                var d = Load(i);
                if (d != null && d.savedAtUnix >= bestTime) { bestTime = d.savedAtUnix; best = i; }
            }
            return best;
        }

        private void MigrateLegacy()
        {
            try
            {
                if (!File.Exists(LegacyPath) || File.Exists(SlotPath(0))) return;
                string json = File.ReadAllText(LegacyPath);
                var data = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);
                if (data == null) return;
                if (string.IsNullOrEmpty(data.slotName)) data.slotName = "Сохранение";
                File.WriteAllText(SlotPath(0), JsonUtility.ToJson(data, true));
                SetActiveSlot(0);
            }
            catch (Exception e) { Debug.LogWarning($"[ForgeGame] Legacy save migration skipped: {e.Message}"); }
        }
    }
}
