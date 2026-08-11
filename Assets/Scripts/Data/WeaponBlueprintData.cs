using System.Collections.Generic;
using UnityEngine;

namespace ForgeGame.Data
{
    /// <summary>
    /// A craftable weapon recipe. The first version only ships a sword, but the
    /// zone count and target shape are data-driven so more blueprints can be added.
    /// </summary>
    [CreateAssetMenu(menuName = "Forge Game/Weapon Blueprint", fileName = "Blueprint")]
    public class WeaponBlueprintData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite preview;

        [Tooltip("Metal mass required to forge this weapon.")]
        [SerializeField] private float requiredMetalMass = 3f;
        [Tooltip("Number of independent blade zones the player hammers.")]
        [SerializeField] private int bladeZoneCount = 5;

        [Header("Target shape per zone (0..1 thickness/width profile)")]
        [SerializeField] private List<float> targetShape = new List<float> { 0.15f, 0.45f, 0.7f, 0.85f, 1f };

        [Header("Base stats (before material & quality)")]
        [SerializeField] private float baseDamage = 20f;
        [SerializeField] private float baseAttackSpeed = 1f;
        [SerializeField] private float baseDurability = 100f;
        [SerializeField] private float baseArmorPenetration = 5f;

        [SerializeField] private List<string> allowedMaterialIds = new List<string>();

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public Sprite Preview => preview;
        public float RequiredMetalMass => requiredMetalMass;
        public int BladeZoneCount => Mathf.Max(1, bladeZoneCount);
        public float BaseDamage => baseDamage;
        public float BaseAttackSpeed => baseAttackSpeed;
        public float BaseDurability => baseDurability;
        public float BaseArmorPenetration => baseArmorPenetration;
        public IReadOnlyList<string> AllowedMaterialIds => allowedMaterialIds;

        public float GetTargetShape(int zoneIndex)
        {
            if (targetShape == null || targetShape.Count == 0) return 0.5f;
            zoneIndex = Mathf.Clamp(zoneIndex, 0, targetShape.Count - 1);
            return targetShape[zoneIndex];
        }

        public bool AllowsMaterial(string materialId)
        {
            if (allowedMaterialIds == null || allowedMaterialIds.Count == 0) return true;
            return allowedMaterialIds.Contains(materialId);
        }

        public void Configure(string newId, string name, float metalMass, int zones, List<float> shape,
            float dmg, float speed, float durability, float pen, List<string> allowed)
        {
            id = newId; displayName = name; requiredMetalMass = metalMass; bladeZoneCount = zones;
            targetShape = shape ?? new List<float>();
            baseDamage = dmg; baseAttackSpeed = speed; baseDurability = durability; baseArmorPenetration = pen;
            allowedMaterialIds = allowed ?? new List<string>();
        }
    }
}
