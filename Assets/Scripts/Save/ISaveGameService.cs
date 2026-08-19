using System.Collections.Generic;

namespace ForgeGame.Save
{
    /// <summary>
    /// Contract the game talks to for save-game state. The single-slot methods act on the
    /// <see cref="ActiveSlot"/> so existing callers (main-menu Continue/New Game, autosave)
    /// keep working, while the slot-aware methods drive the multi-slot save/load UI.
    /// </summary>
    public interface ISaveGameService
    {
        /// <summary>True when at least one slot holds a resumable save.</summary>
        bool HasSave { get; }

        /// <summary>Loads the active slot (falling back to the most recent), or null when there is none.</summary>
        SaveData Load();

        /// <summary>Creates and persists a brand new save in a fresh slot, made active.</summary>
        SaveData CreateNewGame();

        /// <summary>Persists the given data to the active slot.</summary>
        void Save(SaveData data);

        /// <summary>Permanently removes the active slot.</summary>
        void Delete();

        // ---- Multi-slot ----

        /// <summary>How many save slots exist.</summary>
        int SlotCount { get; }

        /// <summary>The slot single-slot operations and autosave target. Persisted across launches.</summary>
        int ActiveSlot { get; }

        void SetActiveSlot(int slot);

        /// <summary>Metadata for every slot (used flag, name, timestamp), ordered by index.</summary>
        IReadOnlyList<SaveSlotInfo> ListSlots();

        SaveData Load(int slot);
        void Save(int slot, SaveData data);
        void Delete(int slot);
    }
}
