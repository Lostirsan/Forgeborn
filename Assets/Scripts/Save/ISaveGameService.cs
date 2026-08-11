namespace ForgeGame.Save
{
    /// <summary>
    /// Contract the main menu talks to for save-game state. The menu never
    /// depends on how or where saves are stored, so the real implementation can
    /// evolve independently (files, cloud, multiple slots, ...).
    /// </summary>
    public interface ISaveGameService
    {
        /// <summary>True when a resumable save exists.</summary>
        bool HasSave { get; }

        /// <summary>Loads the current save, or null when there is none.</summary>
        SaveData Load();

        /// <summary>Creates and persists a brand new save, overwriting any existing one.</summary>
        SaveData CreateNewGame();

        /// <summary>Persists the given save data.</summary>
        void Save(SaveData data);

        /// <summary>Permanently removes the save (used by tests / debug tooling).</summary>
        void Delete();
    }
}
