using System;

namespace ForgeGame.Research
{
    /// <summary>Grade returned by every range evaluation. The UI, not this enum, decides colours.</summary>
    public enum ResultGrade
    {
        CriticalFailure = 0,
        Bad = 1,
        Acceptable = 2,
        Good = 3,
        Perfect = 4
    }

    /// <summary>Which processing scale a sample belongs to.</summary>
    public enum ResearchStageType
    {
        Melting = 0,
        ForgeHeat = 1,
        Quench = 2
    }

    /// <summary>How well the player knows a material overall.</summary>
    public enum KnowledgeLevel
    {
        Unknown = 0,
        Observed = 1,
        Tested = 2,
        Studied = 3,
        Mastered = 4
    }

    /// <summary>One recorded experiment on a temperature scale.</summary>
    [Serializable]
    public class ResearchRangeSample
    {
        public ResearchStageType stageType;
        public float testedValue;
        public float testedRangeMin;
        public float testedRangeMax;
        public ResultGrade resultGrade;
        public int experimentNumber;
    }

    /// <summary>Discovered outcome for a single quench medium of a material.</summary>
    [Serializable]
    public class QuenchResearchEntry
    {
        public int medium; // stored as int for painless serialization
        public bool tested;
        public ResultGrade bestGrade;
        public int experiments;
    }
}
