using System.Collections.Generic;

namespace ForgeGame.Dungeon
{
    /// <summary>
    /// Loot gathered during a dungeon run that has not yet been banked into the persistent
    /// smithy inventory. The dungeon adds to it (ore pickups, combat rewards); when the player
    /// returns to the smithy, <c>SmithyController</c> drains it into the real inventory and
    /// saves — so a run's gains become part of the saved progress instead of being lost with
    /// the scene. A plain static holder (single assembly) that survives the scene load.
    /// </summary>
    public static class ExpeditionResult
    {
        public struct Stack { public string itemId; public int amount; }

        private static readonly List<Stack> _loot = new List<Stack>();

        public static bool HasLoot => _loot.Count > 0;

        public static void Add(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
            int i = _loot.FindIndex(s => s.itemId == itemId);
            if (i >= 0) { var s = _loot[i]; s.amount += amount; _loot[i] = s; }
            else _loot.Add(new Stack { itemId = itemId, amount = amount });
        }

        /// <summary>Returns a copy of the pending loot and clears the buffer.</summary>
        public static List<Stack> Drain()
        {
            var copy = new List<Stack>(_loot);
            _loot.Clear();
            return copy;
        }

        public static void Clear() => _loot.Clear();
    }
}
