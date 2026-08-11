using UnityEngine;

namespace ForgeGame.Data
{
    /// <summary>
    /// Static definition of an inventory item type. Concrete stacks in the
    /// inventory reference this by <see cref="id"/>; per-item runtime state lives
    /// in plain serializable instance classes, never in the ScriptableObject.
    /// </summary>
    [CreateAssetMenu(menuName = "Forge Game/Item", fileName = "Item")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType itemType = ItemType.Ore;
        [SerializeField] private bool stackable = true;
        [SerializeField] private int maxStack = 99;
        [SerializeField] private int baseValue = 1;
        [SerializeField] private Color tint = Color.white;
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public Sprite Icon => icon;
        public ItemType ItemType => itemType;
        public bool Stackable => stackable;
        public int MaxStack => Mathf.Max(1, maxStack);
        public int BaseValue => baseValue;
        public Color Tint => tint;
        public string Description => description;

        /// <summary>Editor/generator helper to populate a freshly created asset.</summary>
        public void Configure(string newId, string name, ItemType type, bool isStackable,
            int maxStackSize, int value, Color color, string desc)
        {
            id = newId;
            displayName = name;
            itemType = type;
            stackable = isStackable;
            maxStack = maxStackSize;
            baseValue = value;
            tint = color;
            description = desc;
        }

        public void SetIcon(Sprite sprite) => icon = sprite;
    }
}
