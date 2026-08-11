using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgeGame.Data
{
    /// <summary>Per-medium quenching suitability, 0 (bad) .. 1 (ideal).</summary>
    [Serializable]
    public struct QuenchAffinity
    {
        public QuenchMedium medium;
        [Range(0f, 1f)] public float compatibility;
    }

    /// <summary>
    /// The <b>true</b> physical properties of a smithing material. These values are
    /// the ground truth the evaluators grade against; the player only ever sees the
    /// portions they have discovered through the research system. Nothing here is
    /// mutated at runtime — it is read-only reference data.
    /// </summary>
    [CreateAssetMenu(menuName = "Forge Game/Material", fileName = "Material")]
    public class MaterialData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private MaterialCategory category = MaterialCategory.Metal;
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;
        [SerializeField] private Color visualColor = new Color(0.7f, 0.7f, 0.75f);

        [Header("Melting (true ranges, degrees)")]
        [SerializeField] private float meltingMin = 900f;
        [SerializeField] private float meltingIdealMin = 1100f;
        [SerializeField] private float meltingIdealMax = 1350f;
        [SerializeField] private float meltingMax = 1550f;

        [Header("Forging heat (true ranges, degrees)")]
        [SerializeField] private float forgingMin = 650f;
        [SerializeField] private float forgingIdealMin = 800f;
        [SerializeField] private float forgingIdealMax = 1100f;
        [SerializeField] private float forgingMax = 1250f;

        [Header("Physical properties (normalized 0..1 unless noted)")]
        [SerializeField] private float density = 7.8f;
        [SerializeField, Range(0f, 1f)] private float hardness = 0.5f;
        [SerializeField, Range(0f, 1f)] private float toughness = 0.6f;
        [SerializeField, Range(0f, 1f)] private float flexibility = 0.6f;
        [SerializeField, Range(0f, 1f)] private float corrosionResistance = 0.4f;
        [SerializeField] private float heatRateMultiplier = 1f;
        [SerializeField] private float coolingRateMultiplier = 1f;

        [Header("Economy / quench")]
        [SerializeField] private int baseValue = 5;
        [SerializeField] private List<QuenchAffinity> quenchAffinities = new List<QuenchAffinity>();
        [SerializeField] private List<string> traits = new List<string>();

        [Header("Info")]
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public Sprite Icon => icon;
        public MaterialCategory Category => category;
        public ItemRarity Rarity => rarity;
        public Color VisualColor => visualColor;

        public float MeltingMin => meltingMin;
        public float MeltingIdealMin => meltingIdealMin;
        public float MeltingIdealMax => meltingIdealMax;
        public float MeltingMax => meltingMax;

        public float ForgingMin => forgingMin;
        public float ForgingIdealMin => forgingIdealMin;
        public float ForgingIdealMax => forgingIdealMax;
        public float ForgingMax => forgingMax;

        public float Density => density;
        public float Hardness => hardness;
        public float Toughness => toughness;
        public float Flexibility => flexibility;
        public float CorrosionResistance => corrosionResistance;
        public float HeatRateMultiplier => heatRateMultiplier;
        public float CoolingRateMultiplier => coolingRateMultiplier;

        public int BaseValue => baseValue;
        public IReadOnlyList<string> Traits => traits;

        /// <summary>Overall span used to lay out the research scale for melting.</summary>
        public Vector2 MeltingScaleRange => new Vector2(meltingMin - 200f, meltingMax + 200f);
        public Vector2 ForgingScaleRange => new Vector2(forgingMin - 150f, forgingMax + 150f);

        public float GetQuenchCompatibility(QuenchMedium medium)
        {
            for (int i = 0; i < quenchAffinities.Count; i++)
                if (quenchAffinities[i].medium == medium)
                    return quenchAffinities[i].compatibility;
            return 0.25f; // unknown medium: poor but not zero
        }

        /// <summary>Generator helper to fill a freshly created asset.</summary>
        public void Configure(string newId, string name, MaterialCategory cat, ItemRarity rar, Color color,
            Vector4 melting, Vector4 forging, float dens, float hard, float tough, float flex, float corr,
            float heatMul, float coolMul, int value, List<QuenchAffinity> quench, List<string> traitList, string desc)
        {
            id = newId; displayName = name; category = cat; rarity = rar; visualColor = color;
            meltingMin = melting.x; meltingIdealMin = melting.y; meltingIdealMax = melting.z; meltingMax = melting.w;
            forgingMin = forging.x; forgingIdealMin = forging.y; forgingIdealMax = forging.z; forgingMax = forging.w;
            density = dens; hardness = hard; toughness = tough; flexibility = flex; corrosionResistance = corr;
            heatRateMultiplier = heatMul; coolingRateMultiplier = coolMul; baseValue = value;
            quenchAffinities = quench ?? new List<QuenchAffinity>();
            traits = traitList ?? new List<string>();
            description = desc;
        }

        public void SetIcon(Sprite sprite) => icon = sprite;
    }
}
