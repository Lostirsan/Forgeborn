using System;
using System.IO;
using UnityEngine;

namespace ForgeGame.Save
{
    /// <summary>
    /// Stores a single save slot as a JSON file under
    /// <see cref="Application.persistentDataPath"/>. All file access is guarded so
    /// a corrupt or unreadable file is treated as "no save" instead of throwing.
    /// </summary>
    public sealed class LocalSaveGameService : ISaveGameService
    {
        private const string FileName = "savegame.json";

        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool HasSave => File.Exists(FilePath);

        public SaveData Load()
        {
            if (!File.Exists(FilePath))
                return null;

            try
            {
                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrEmpty(json))
                    return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ForgeGame] Could not read save file: {e.Message}");
                return null;
            }
        }

        public SaveData CreateNewGame()
        {
            var data = SaveData.CreateNew();
            Save(data);
            return data;
        }

        public void Save(SaveData data)
        {
            if (data == null) return;
            try
            {
                data.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[ForgeGame] Could not write save file: {e.Message}");
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ForgeGame] Could not delete save file: {e.Message}");
            }
        }
    }
}
