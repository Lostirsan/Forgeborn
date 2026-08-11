using System;
using System.Collections.Generic;

namespace ForgeGame.Research
{
    /// <summary>
    /// The player's accumulated knowledge about one material. This is player
    /// progress, kept entirely separate from the material's reference data, and is
    /// what gets serialized into the save file.
    /// </summary>
    [Serializable]
    public class MaterialResearchProgress
    {
        public string materialId;

        public List<ResearchRangeSample> meltingSamples = new List<ResearchRangeSample>();
        public List<ResearchRangeSample> forgeHeatSamples = new List<ResearchRangeSample>();
        public List<QuenchResearchEntry> quenchEntries = new List<QuenchResearchEntry>();

        /// <summary>Names of properties (hardness, toughness, ...) the player has revealed.</summary>
        public List<string> revealedProperties = new List<string>();

        public int experimentCount;
        public bool mastered;

        public List<ResearchRangeSample> SamplesFor(ResearchStageType stage)
        {
            switch (stage)
            {
                case ResearchStageType.Melting: return meltingSamples;
                case ResearchStageType.ForgeHeat: return forgeHeatSamples;
                default: return meltingSamples;
            }
        }

        public QuenchResearchEntry GetQuench(int medium)
        {
            foreach (var e in quenchEntries)
                if (e.medium == medium) return e;
            return null;
        }

        public bool HasProperty(string propertyName) =>
            revealedProperties != null && revealedProperties.Contains(propertyName);

        public void RevealProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return;
            if (revealedProperties == null) revealedProperties = new List<string>();
            if (!revealedProperties.Contains(propertyName)) revealedProperties.Add(propertyName);
        }
    }
}
