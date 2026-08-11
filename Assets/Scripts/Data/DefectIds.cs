namespace ForgeGame.Data
{
    /// <summary>
    /// Stable string ids for the built-in defects. Evaluators reference these so
    /// they stay in sync with the <see cref="DefectData"/> assets the generator
    /// creates. New defects only need a new constant plus an asset.
    /// </summary>
    public static class DefectIds
    {
        public const string PorousIngot = "porous_ingot";
        public const string OverheatedMetal = "overheated_metal";
        public const string HairlineCrack = "hairline_crack";
        public const string UnevenEdge = "uneven_edge";
        public const string HeavyTip = "heavy_tip";
        public const string WeakTang = "weak_tang";
        public const string BrittleStructure = "brittle_structure";
        public const string SoftMetal = "soft_metal";
        public const string BentBlade = "bent_blade";
    }
}
