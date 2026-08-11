using System;
using System.Collections.Generic;
using ForgeGame.Smithy;
using ForgeGame.Smithy.Casting;

namespace ForgeGame.Items
{
    /// <summary>
    /// A cooled cast blade blank sitting in the inventory, waiting to be edge-forged on
    /// the anvil. Plain serializable data (own deep-copied geometry + string ids) so the
    /// player can cast several blanks without finishing a whole sword each time.
    /// </summary>
    [Serializable]
    public class CastBlankInstance
    {
        public string uniqueId;
        public string materialId;
        public string blueprintId;
        public float meltQuality;
        public float pourQuality;
        public CastBladeState blade;
        public List<string> defects = new List<string>();
        public long createdAtUnix;

        public static CastBlankInstance FromSession(ForgeSession s)
        {
            if (s == null) return null;
            return new CastBlankInstance
            {
                uniqueId = Guid.NewGuid().ToString("N"),
                materialId = s.selectedMaterialId,
                blueprintId = s.blueprintId,
                meltQuality = s.meltQuality,
                pourQuality = s.pourQuality,
                blade = s.castBlade != null ? s.castBlade.DeepCopy() : null,
                defects = new List<string>(s.accumulatedDefects),
                createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}
