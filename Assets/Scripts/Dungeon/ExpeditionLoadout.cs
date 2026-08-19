namespace ForgeGame.Dungeon
{
    /// <summary>
    /// The party composition and gear the player chose in the smithy before descending.
    /// A plain static holder that survives the Smithy → Dungeon scene load (single assembly,
    /// no serialization needed). The dungeon reads it once when it builds the party; if it was
    /// never configured (e.g. entering the scene directly), the dungeon falls back to the full
    /// default party with no bonuses.
    /// </summary>
    public static class ExpeditionLoadout
    {
        public static bool configured;
        public static bool takeLeader = true;      // the knight is now an optional roster pick too
        public static bool takeCompanion = true;

        public static string leaderWeaponName;     // empty = unarmed ("Кулаки")
        public static string companionWeaponName;
        public static int leaderWeaponBonus;       // flat damage added to that hero's strikes
        public static int companionWeaponBonus;

        public static void Reset()
        {
            configured = false;
            takeLeader = true;
            takeCompanion = true;
            leaderWeaponName = companionWeaponName = null;
            leaderWeaponBonus = companionWeaponBonus = 0;
        }
    }
}
