using System;

namespace ForgeGame.Items
{
    /// <summary>
    /// A single smelted ingot. Plain serializable data — it references its material
    /// by string id so it survives saving/loading without holding asset references.
    /// </summary>
    [Serializable]
    public class IngotInstance
    {
        public string uniqueId;
        public string materialId;
        public float mass;
        [UnityEngine.Range(0f, 1f)] public float purity;
        [UnityEngine.Range(0f, 1f)] public float smeltingQuality;
        public bool porous;
        public bool overheated;
        public bool isScrap;
        public long createdAtUnix;

        public static IngotInstance Create(string materialId, float mass, float purity, float quality)
        {
            return new IngotInstance
            {
                uniqueId = Guid.NewGuid().ToString("N"),
                materialId = materialId,
                mass = mass,
                purity = purity,
                smeltingQuality = quality,
                createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}
