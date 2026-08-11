using System.Collections.Generic;
using ForgeGame.Data;
using ForgeGame.Items;
using UnityEngine;

namespace ForgeGame.Smithy.Assembly
{
    /// <summary>
    /// The single place final weapon stats are computed for the bronze-casting path.
    /// It combines the blueprint, material, and each stage's quality (melt, pour,
    /// edge forging, straightness, symmetry, work-hardening, overwork, assembly) plus
    /// components and defects. UI never does this maths — it only displays the result.
    /// </summary>
    public static class WeaponStatCalculator
    {
        public static void Calculate(WeaponInstance w, SmithyDatabase db)
        {
            if (w == null || db == null) return;
            var bp = db.GetBlueprint(w.blueprintId);
            var mat = db.GetMaterial(w.materialId);
            if (bp == null || mat == null) return;

            // Metal quality from the foundry (melt + pour).
            float metalQuality = Mathf.Lerp(0.55f, 1f, w.meltQuality) * Mathf.Lerp(0.6f, 1f, w.pourQuality);
            float edge = Mathf.Clamp01(w.edgeForgeQuality);
            float hardening = Mathf.Clamp01(w.workHardening);

            float hardness = mat.Hardness;
            float toughness = mat.Toughness;

            // --- Damage --- (harder metal, better/thinner edge, good work-hardening)
            float damage = bp.BaseDamage
                * Mathf.Lerp(0.75f, 1.30f, hardness)
                * Mathf.Lerp(0.70f, 1.20f, edge)
                * (1f + w.edgeThinness * 0.22f)
                * Mathf.Lerp(0.88f, 1.06f, metalQuality)
                * Mathf.Lerp(0.94f, 1.08f, hardening);

            // --- Weight & balance ---
            float weight = Mathf.Max(0.4f, bp.RequiredMetalMass * (mat.Density / 7.8f) * 0.5f);
            float balance = Mathf.Clamp01(w.straightness * 0.45f + w.symmetry * 0.30f + 0.20f);

            // --- Attack speed --- (heavier & crooked = slower)
            float attackSpeed = bp.BaseAttackSpeed
                * Mathf.Lerp(1.15f, 0.75f, Mathf.InverseLerp(0.4f, 1.6f, weight))
                * Mathf.Lerp(0.85f, 1.10f, balance);

            // --- Durability --- (tough metal, good pour, not too thin, low overwork)
            float durability = bp.BaseDurability
                * Mathf.Lerp(0.60f, 1.25f, toughness)
                * Mathf.Lerp(0.70f, 1.15f, w.pourQuality)
                * Mathf.Lerp(0.88f, 1.05f, metalQuality)
                * (1f - w.edgeThinness * 0.28f)
                * (1f - w.overworkDamage * 0.5f)
                * Mathf.Lerp(0.92f, 1.10f, hardening);

            // --- Armor penetration ---
            float armorPen = bp.BaseArmorPenetration
                * Mathf.Lerp(0.70f, 1.35f, hardness)
                * Mathf.Lerp(0.85f, 1.20f, edge);

            // Components.
            var guard = db.GetComponent(w.guardId);
            var handle = db.GetComponent(w.handleId);
            var pommel = db.GetComponent(w.pommelId);
            foreach (var c in new[] { guard, handle, pommel })
            {
                if (c == null) continue;
                weight += c.WeightDelta;
                balance += c.BalanceDelta;
                attackSpeed *= 1f + c.SpeedMultiplierDelta;
            }
            balance = Mathf.Clamp01(balance);

            // Defect multipliers.
            foreach (var id in w.defectIds)
            {
                var d = db.GetDefect(id);
                if (d == null) continue;
                damage *= d.DamageMultiplier;
                attackSpeed *= d.AttackSpeedMultiplier;
                durability *= d.DurabilityMultiplier;
                armorPen *= d.ArmorPenetrationMultiplier;
                balance = Mathf.Clamp01(balance + d.BalanceDelta);
            }

            // --- Manual assembly quality --- (how well the player fitted the hilt).
            // Crooked/misaligned components hurt handling and longevity but never make
            // the weapon useless (worst case keeps 65% balance / 85% speed / 90% dura).
            float assembly = Mathf.Clamp01(w.assemblyQuality);
            balance = Mathf.Clamp01(balance * Mathf.Lerp(0.65f, 1f, assembly));
            attackSpeed *= Mathf.Lerp(0.85f, 1f, assembly);
            durability *= Mathf.Lerp(0.90f, 1f, assembly);

            // --- Value --- (assembly quality is a clear factor in worth)
            float overall = Mathf.Clamp01(metalQuality * 0.32f +
                edge * 0.30f + assembly * 0.22f + w.straightness * 0.08f + w.symmetry * 0.08f);
            float rarityMul = 1f + (int)mat.Rarity * 0.6f;
            int value = Mathf.RoundToInt((bp.BaseDamage * 0.5f + mat.BaseValue * 3f)
                * rarityMul * Mathf.Lerp(0.5f, 1.6f, overall));
            foreach (var c in new[] { guard, handle, pommel })
                if (c != null) value += c.ValueDelta;

            // Commit.
            w.damage = Mathf.Round(Mathf.Max(1f, damage) * 10f) / 10f;
            w.attackSpeed = Mathf.Round(Mathf.Max(0.2f, attackSpeed) * 100f) / 100f;
            w.durability = Mathf.Round(Mathf.Max(1f, durability));
            w.maxDurability = w.durability;
            w.armorPenetration = Mathf.Round(Mathf.Max(0f, armorPen) * 10f) / 10f;
            w.weight = Mathf.Round(Mathf.Max(0.1f, weight) * 100f) / 100f;
            w.balance = Mathf.Round(balance * 100f) / 100f;
            w.value = Mathf.Max(1, value);

            if (w.traits == null) w.traits = new List<string>();
            foreach (var t in mat.Traits)
                if (!w.traits.Contains(t)) w.traits.Add(t);
        }
    }
}
