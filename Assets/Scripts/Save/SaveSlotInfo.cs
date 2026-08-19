namespace ForgeGame.Save
{
    /// <summary>Lightweight metadata about one save slot, for listing without loading full data.</summary>
    public struct SaveSlotInfo
    {
        public int index;
        public bool used;
        public string name;
        public long savedAtUnix;
    }
}
