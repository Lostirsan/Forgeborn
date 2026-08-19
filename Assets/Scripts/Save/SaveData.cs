using System;

namespace ForgeGame.Save
{
    /// <summary>
    /// Minimal serializable save payload. It only needs to carry enough to let
    /// the menu resume the correct scene for now; gameplay systems can extend it
    /// later without changing the menu.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Scene the player should return to. Empty means "use the default start scene".</summary>
        public string sceneName = string.Empty;

        /// <summary>Unix timestamp (seconds) of the last save, for "last played" display.</summary>
        public long savedAtUnix;

        /// <summary>Player-facing name of this save slot (shown in the save/load list).</summary>
        public string slotName = string.Empty;

        /// <summary>Save-format version, so future changes can migrate old files.</summary>
        public int version = 1;

        /// <summary>Smithy gameplay state. Null/empty on a brand-new game.</summary>
        public ForgeGame.Smithy.SmithySaveData smithy = new ForgeGame.Smithy.SmithySaveData();

        public static SaveData CreateNew()
        {
            return new SaveData
            {
                sceneName = string.Empty,
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}
