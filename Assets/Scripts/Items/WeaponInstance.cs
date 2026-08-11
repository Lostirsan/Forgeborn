using System;
using System.Collections.Generic;

namespace ForgeGame.Items
{
    /// <summary>
    /// A finished weapon. Plain serializable record (string ids) so it round-trips
    /// through the save file. Quality inputs describe the new bronze-casting path;
    /// final stats are produced by WeaponStatCalculator.
    /// </summary>
    [Serializable]
    public class WeaponInstance
    {
        public string uniqueId;
        public string blueprintId;
        public string materialId;
        public string customName;

        // Quality inputs (0..1) from the new production path.
        public float meltQuality;
        public float pourQuality;
        public float edgeForgeQuality;
        public float edgeThinness;   // 0 thick/durable .. 1 razor/fragile
        public float straightness;
        public float symmetry;
        public float workHardening;
        public float overworkDamage;
        public float assemblyQuality;

        // Chosen component ids (may be empty for defaults).
        public string guardId;
        public string handleId;
        public string pommelId;

        // The player's actual committed assembly placement (offset from ideal centre +
        // rotation). Lets an inventory/preview reconstruct the real, imperfect weapon.
        public float guardOffsetX;
        public float guardRotation;
        public float handleOffsetX;
        public float handleRotation;
        public float pommelOffsetX;
        public float pommelRotation;

        // Serializable snapshot of the exact crafted look (blade geometry + component
        // variants + committed placement). Null for legacy weapons — renderers fall back.
        public WeaponVisualSnapshot visual;

        // Final computed stats.
        public float damage;
        public float attackSpeed;
        public float durability;
        public float maxDurability;
        public float armorPenetration;
        public float weight;
        public float balance;
        public int value;

        public List<string> defectIds = new List<string>();
        public List<string> traits = new List<string>();

        public bool playerCrafted = true;
        public long createdAtUnix;

        public static WeaponInstance CreateEmpty(string blueprintId, string materialId)
        {
            return new WeaponInstance
            {
                uniqueId = Guid.NewGuid().ToString("N"),
                blueprintId = blueprintId,
                materialId = materialId,
                defectIds = new List<string>(),
                traits = new List<string>(),
                playerCrafted = true,
                createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}
