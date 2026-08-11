namespace ForgeGame.Smithy
{
    /// <summary>Identifies each overlay panel for the controller's panel manager.</summary>
    public enum PanelId
    {
        None = 0,
        Inventory = 1,
        Foundry = 2,
        Anvil = 3,
        Assembly = 4,
        Journal = 5,
        ItemResult = 6,
        Pause = 7,
        Settings = 8,
        Debug = 9
    }

    /// <summary>
    /// The interactive workstations in the new bronze-casting smithy. The old
    /// furnace/forge/quench/grindstone stations no longer exist.
    /// </summary>
    public enum SmithyStation
    {
        Storage = 0,
        Foundry = 1,
        Anvil = 2,
        AssemblyTable = 3,
        DungeonDoor = 4,
        MainMenuDoor = 5
    }
}
