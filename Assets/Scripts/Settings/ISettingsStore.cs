namespace ForgeGame.Settings
{
    /// <summary>
    /// Abstraction over where settings are persisted. Keeping this behind an
    /// interface means the PlayerPrefs implementation can later be swapped for a
    /// file or cloud store without touching the rest of the menu.
    /// </summary>
    public interface ISettingsStore
    {
        bool HasSaved { get; }

        /// <summary>Returns the persisted settings, or a fresh default set when none exist.</summary>
        GameSettings Load();

        void Save(GameSettings settings);
    }
}
