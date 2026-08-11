using System;
using System.Collections.Generic;
using ForgeGame.Inventory;
using ForgeGame.Research;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Everything the smithy needs to persist, bundled into the shared save file.
    /// Uses only ids and plain instances so it round-trips without asset references.
    /// </summary>
    [Serializable]
    public class SmithySaveData
    {
        public bool hasData;
        public bool bronzeSeeded; // one-time bronze grant (also migrates pre-bronze saves)
        public InventoryModel inventory = new InventoryModel();
        public List<MaterialResearchProgress> research = new List<MaterialResearchProgress>();
        public ForgeSession activeSession;
        public string equippedWeaponId = string.Empty;
        public float playerX;
    }
}
