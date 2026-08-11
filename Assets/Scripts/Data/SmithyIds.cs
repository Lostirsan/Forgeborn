namespace ForgeGame.Data
{
    /// <summary>Stable ids for the built-in test items, ores and materials.</summary>
    public static class SmithyIds
    {
        // Bronze-casting slice (the only active production material).
        public const string Bronze = "bronze";
        public const string BronzeSword = "bronze_sword";

        // Legacy materials (kept in the database, no longer part of production).
        public const string Iron = "iron";
        public const string Steel = "steel";
        public const string Silver = "silver";

        // Ores (inventory items that also carry OreData)
        public const string IronOre = "iron_ore";
        public const string SteelOre = "steel_ore";
        public const string SilverOre = "silver_ore";

        // Consumable inputs
        public const string Coal = "coal";
        public const string Flux = "flux";

        // Default free weapon components
        public const string BasicGuard = "guard_basic";
        public const string BasicHandle = "handle_basic";
        public const string BasicPommel = "pommel_basic";

        // Blueprint
        public const string SwordBlueprint = "sword";
    }
}
