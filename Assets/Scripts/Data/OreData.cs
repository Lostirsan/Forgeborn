using UnityEngine;

namespace ForgeGame.Data
{
    /// <summary>
    /// An ore is an inventory item that yields a metal <see cref="MaterialData"/>
    /// when smelted. It links to the material rather than duplicating its data.
    /// </summary>
    [CreateAssetMenu(menuName = "Forge Game/Ore", fileName = "Ore")]
    public class OreData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private MaterialData material;

        [Tooltip("Metal mass produced per unit of ore at perfect purity.")]
        [SerializeField] private float metalYieldPerUnit = 1f;
        [Tooltip("0 = pure, 1 = very impure. Raises the flux needed and lowers purity.")]
        [SerializeField, Range(0f, 1f)] private float impurity = 0.2f;
        [Tooltip("0 = trivial, 1 = very hard to melt. Shifts the effective ideal window.")]
        [SerializeField, Range(0f, 1f)] private float smeltingDifficulty = 0.3f;
        [SerializeField] private int baseValue = 3;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public Sprite Icon => icon;
        public MaterialData Material => material;
        public float MetalYieldPerUnit => metalYieldPerUnit;
        public float Impurity => impurity;
        public float SmeltingDifficulty => smeltingDifficulty;
        public int BaseValue => baseValue;

        public void Configure(string newId, string name, MaterialData mat, float yield, float imp, float difficulty, int value)
        {
            id = newId; displayName = name; material = mat;
            metalYieldPerUnit = yield; impurity = imp; smeltingDifficulty = difficulty; baseValue = value;
        }

        public void SetIcon(Sprite sprite) => icon = sprite;
    }
}
