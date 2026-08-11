namespace ForgeGame.Data
{
    /// <summary>Broad grouping used for storage and filtering.</summary>
    public enum MaterialCategory
    {
        Metal = 0,
        Fuel = 1,
        Flux = 2,
        Gem = 3,
        MonsterPart = 4,
        Other = 5
    }

    /// <summary>What an <see cref="ItemData"/> represents in the inventory.</summary>
    public enum ItemType
    {
        Ore = 0,
        Fuel = 1,
        Flux = 2,
        Ingot = 3,
        Workpiece = 4,
        Weapon = 5,
        Component = 6,
        Consumable = 7,
        MonsterPart = 8
    }

    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>Kind of weapon part for the layered visual + assembly system.</summary>
    public enum ComponentSlot
    {
        Blade = 0,
        Guard = 1,
        Handle = 2,
        Pommel = 3
    }

    /// <summary>Liquid used at the quenching station. Extensible.</summary>
    public enum QuenchMedium
    {
        Water = 0,
        Oil = 1,
        MonsterBlood = 2,
        Brine = 3,
        Alchemical = 4,
        IceEssence = 5
    }
}
