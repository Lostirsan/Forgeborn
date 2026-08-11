using System;
using System.Collections.Generic;
using ForgeGame.Smithy.Casting;

namespace ForgeGame.Smithy
{
    /// <summary>The stage of the single bronze-casting production path.</summary>
    public enum ForgeStage
    {
        None = 0,
        Melting = 1,
        Pouring = 2,
        Cooling = 3,
        CastBlankReady = 4,
        EdgeForging = 5,
        Assembly = 6,
        Completed = 7,
        Failed = 8
    }

    /// <summary>
    /// State of one bronze-sword crafting job: melt → pour into the mould → cool →
    /// cast blank → edge-forge on the anvil → assemble. Plain serializable data (ids,
    /// not asset/scene references) so it survives panel close, walking between
    /// stations and a save/load round-trip.
    /// </summary>
    [Serializable]
    public class ForgeSession
    {
        public string sessionId;
        public ForgeStage currentStage = ForgeStage.None;

        public string selectedMaterialId; // always "bronze" in this slice
        public string blueprintId;

        // Foundry — melting.
        public float meltTemperature = 20f;
        public float meltQuality;
        public float meltProgress;
        public float meltTimer;        // seconds since melt started (timed-readiness model)
        public float overheatExposure;

        // Foundry — pouring.
        public float pourQuality;
        public float fillAmount;
        public float pourQualityWeightedSum;
        public float pouredAmountForQuality;
        public float lastPourRate;
        public float remainingMetal = 1f; // crucible level, drains as it pours/spills
        public float spilledMetal;         // total bronze that missed the mould (quality penalty)

        // The cast blade produced by the foundry and worked on the anvil.
        public CastBladeState castBlade;

        // Anvil — edge forging results.
        public float edgeForgeQuality;
        public float edgeUniformity;
        public float edgeThinness;
        public float straightness;
        public float symmetry;
        public float workHardening;
        public float overworkDamage;

        // Assembly components (selected/installed by the manual assembly mini-game).
        public string guardId;
        public string handleId;
        public string pommelId;

        // Manual assembly progress — persisted so closing/reopening the table never
        // resets placement or lets the player re-roll a better score.
        public bool guardInstalled;
        public bool handleInstalled;
        public bool pommelInstalled;
        public float guardAssemblyQuality;
        public float handleAssemblyQuality;
        public float pommelAssemblyQuality;
        // The player's ACTUAL committed placement (Jacksmith-style): horizontal offset
        // from the ideal centre line and rotation in degrees. Never auto-corrected, so a
        // crooked assembly survives panel close/reopen and save/load exactly as placed.
        public float guardOffsetX;
        public float guardRotation;
        public float handleOffsetX;
        public float handleRotation;
        public float pommelOffsetX;
        public float pommelRotation;
        // Chosen visual variant per slot (cosmetic; restores the right sprite on reopen).
        public int guardVariant;
        public int handleVariant;
        public int pommelVariant;

        public List<string> accumulatedDefects = new List<string>();
        public long startTimeUnix;

        public static ForgeSession CreateNew()
        {
            return new ForgeSession
            {
                sessionId = Guid.NewGuid().ToString("N"),
                currentStage = ForgeStage.None,
                accumulatedDefects = new List<string>(),
                startTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        public void AddDefect(string defectId)
        {
            if (string.IsNullOrEmpty(defectId)) return;
            accumulatedDefects ??= new List<string>();
            if (!accumulatedDefects.Contains(defectId)) accumulatedDefects.Add(defectId);
        }
    }
}
