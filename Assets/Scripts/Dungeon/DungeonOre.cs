using UnityEngine;

namespace ForgeGame.Dungeon
{
    /// <summary>A pickup of ore lying on a dungeon floor block. Click it → the hero walks over and collects it.</summary>
    public class DungeonOre : MonoBehaviour
    {
        public string itemId = "iron_ore";
        public int amount = 1;
    }
}
