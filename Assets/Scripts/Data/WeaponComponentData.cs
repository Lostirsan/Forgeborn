using UnityEngine;

namespace ForgeGame.Data
{
    /// <summary>
    /// A non-blade weapon part (guard, handle, pommel). Components tweak the final
    /// weapon's weight, balance, speed and value, and provide a sprite layer.
    /// </summary>
    [CreateAssetMenu(menuName = "Forge Game/Weapon Component", fileName = "Component")]
    public class WeaponComponentData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private ComponentSlot slot = ComponentSlot.Guard;

        [SerializeField] private float weightDelta = 0.1f;
        [Tooltip("Positive shifts balance toward the hilt (better handling).")]
        [SerializeField, Range(-0.3f, 0.3f)] private float balanceDelta = 0f;
        [SerializeField, Range(-0.2f, 0.2f)] private float speedMultiplierDelta = 0f;
        [SerializeField] private int valueDelta = 2;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public Sprite Icon => icon;
        public ComponentSlot Slot => slot;
        public float WeightDelta => weightDelta;
        public float BalanceDelta => balanceDelta;
        public float SpeedMultiplierDelta => speedMultiplierDelta;
        public int ValueDelta => valueDelta;

        public void Configure(string newId, string name, ComponentSlot componentSlot,
            float weight, float balance, float speed, int value)
        {
            id = newId; displayName = name; slot = componentSlot;
            weightDelta = weight; balanceDelta = balance; speedMultiplierDelta = speed; valueDelta = value;
        }

        public void SetIcon(Sprite sprite) => icon = sprite;
    }
}
