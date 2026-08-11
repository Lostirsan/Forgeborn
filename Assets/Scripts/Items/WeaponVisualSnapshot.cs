using System;
using ForgeGame.Smithy;
using ForgeGame.Smithy.Casting;

namespace ForgeGame.Items
{
    /// <summary>
    /// A plain, serializable snapshot of exactly how a finished weapon looks — enough to
    /// rebuild the player's crafted sword identically anywhere (Assembly result, item
    /// result, inventory preview, dungeon) after UI close, save and load. No sprites,
    /// textures, GameObjects or asset references: only the forged blade geometry, the
    /// chosen component ids + visual variants, and each part's committed placement.
    ///
    /// Placement offsets are stored NORMALISED (offsetX ÷ the Assembly reference blade
    /// height) so any preview size reproduces the same relative crookedness; rotations
    /// are plain degrees. Round-trips through <see cref="WeaponInstance"/> via JsonUtility.
    /// </summary>
    [Serializable]
    public class WeaponVisualSnapshot
    {
        /// <summary>Assembly blade display height the raw pixel offsets were normalised against.</summary>
        public const float ReferenceBladeHeight = 720f;

        public CastBladeState blade;          // this weapon's OWN forged geometry (deep copy)
        public string materialId;             // for blade tint lookup at render time

        public string guardId;
        public string handleId;
        public string pommelId;

        public int guardVariant;
        public int handleVariant;
        public int pommelVariant;

        public float guardOffsetNorm;         // offsetX / ReferenceBladeHeight
        public float handleOffsetNorm;
        public float pommelOffsetNorm;

        public float guardRotation;           // degrees, as the player committed
        public float handleRotation;
        public float pommelRotation;

        public bool HasBlade => blade != null && blade.sections != null && blade.sections.Count >= 2;

        /// <summary>Snapshots the live crafting session into an independent visual record.</summary>
        public static WeaponVisualSnapshot FromSession(ForgeSession s)
        {
            if (s == null) return null;
            return new WeaponVisualSnapshot
            {
                blade = s.castBlade != null ? s.castBlade.DeepCopy() : null,
                materialId = s.selectedMaterialId,
                guardId = s.guardId,
                handleId = s.handleId,
                pommelId = s.pommelId,
                guardVariant = s.guardVariant,
                handleVariant = s.handleVariant,
                pommelVariant = s.pommelVariant,
                guardOffsetNorm = s.guardOffsetX / ReferenceBladeHeight,
                handleOffsetNorm = s.handleOffsetX / ReferenceBladeHeight,
                pommelOffsetNorm = s.pommelOffsetX / ReferenceBladeHeight,
                guardRotation = s.guardRotation,
                handleRotation = s.handleRotation,
                pommelRotation = s.pommelRotation
            };
        }
    }
}
