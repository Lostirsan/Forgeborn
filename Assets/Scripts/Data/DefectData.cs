using UnityEngine;

namespace ForgeGame.Data
{
    public enum DefectSeverity
    {
        Minor = 0,
        Moderate = 1,
        Major = 2,
        Critical = 3
    }

    /// <summary>
    /// A named flaw (or trade-off) a weapon can carry. Effects are multiplicative
    /// stat modifiers applied by the stat calculator, so defects are pure data and
    /// designers can add new ones without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Forge Game/Defect", fileName = "Defect")]
    public class DefectData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private DefectSeverity severity = DefectSeverity.Minor;
        [Tooltip("Hidden defects are not shown until the weapon has been used.")]
        [SerializeField] private bool hiddenUntilUsed = false;
        [SerializeField] private bool repairable = true;

        [Header("Stat multipliers (1 = no change)")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float attackSpeedMultiplier = 1f;
        [SerializeField] private float durabilityMultiplier = 1f;
        [SerializeField] private float armorPenetrationMultiplier = 1f;
        [SerializeField] private float balanceDelta = 0f;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public string Description => description;
        public DefectSeverity Severity => severity;
        public bool HiddenUntilUsed => hiddenUntilUsed;
        public bool Repairable => repairable;
        public float DamageMultiplier => damageMultiplier;
        public float AttackSpeedMultiplier => attackSpeedMultiplier;
        public float DurabilityMultiplier => durabilityMultiplier;
        public float ArmorPenetrationMultiplier => armorPenetrationMultiplier;
        public float BalanceDelta => balanceDelta;

        public void Configure(string newId, string name, string desc, DefectSeverity sev,
            bool hidden, bool canRepair, float dmg, float speed, float durability, float pen, float balance)
        {
            id = newId; displayName = name; description = desc; severity = sev;
            hiddenUntilUsed = hidden; repairable = canRepair;
            damageMultiplier = dmg; attackSpeedMultiplier = speed; durabilityMultiplier = durability;
            armorPenetrationMultiplier = pen; balanceDelta = balance;
        }
    }
}
