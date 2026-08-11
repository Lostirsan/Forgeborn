using System;
using System.Collections.Generic;
using ForgeGame.Items;

namespace ForgeGame.Inventory
{
    /// <summary>A stack of a stackable item type, referenced by id.</summary>
    [Serializable]
    public class InventoryItemInstance
    {
        public string itemId;
        public int count;

        public InventoryItemInstance() { }
        public InventoryItemInstance(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }

    /// <summary>
    /// Pure serializable inventory state. Stackable goods live as id+count stacks;
    /// unique crafted objects (ingots, weapons) live as their own instance lists.
    /// No scene or asset references, so it saves and loads cleanly.
    /// </summary>
    [Serializable]
    public class InventoryModel
    {
        public List<InventoryItemInstance> stacks = new List<InventoryItemInstance>();
        public List<IngotInstance> ingots = new List<IngotInstance>();
        public List<CastBlankInstance> castBlanks = new List<CastBlankInstance>();
        public List<WeaponInstance> weapons = new List<WeaponInstance>();
        public int money;
    }
}
