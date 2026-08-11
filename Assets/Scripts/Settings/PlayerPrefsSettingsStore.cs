using UnityEngine;

namespace ForgeGame.Settings
{
    /// <summary>
    /// Stores the whole <see cref="GameSettings"/> model as a single JSON blob in
    /// PlayerPrefs. All PlayerPrefs access is confined to this class so it is the
    /// only file that needs to change if the storage backend is replaced later.
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        private const string Key = "ForgeGame.Settings.v1";

        public bool HasSaved => PlayerPrefs.HasKey(Key);

        public GameSettings Load()
        {
            if (!PlayerPrefs.HasKey(Key))
                return new GameSettings();

            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new GameSettings();

            try
            {
                var settings = JsonUtility.FromJson<GameSettings>(json);
                return settings ?? new GameSettings();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ForgeGame] Failed to parse saved settings, using defaults. {e.Message}");
                return new GameSettings();
            }
        }

        public void Save(GameSettings settings)
        {
            if (settings == null) return;
            string json = JsonUtility.ToJson(settings);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    }
}
