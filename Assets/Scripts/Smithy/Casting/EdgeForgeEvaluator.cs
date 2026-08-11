using System.Collections.Generic;
using ForgeGame.Data;
using UnityEngine;

namespace ForgeGame.Smithy.Casting
{
    public struct EdgeForgeResult
    {
        public float edgeForgeQuality;
        public float edgeUniformity;
        public float edgeThinness;
        public float straightness;
        public float symmetry;
        public float workHardening;
        public float overworkDamage;
        public List<string> defects;
    }

    /// <summary>
    /// Grades a finished cast blade after edge forging. All sub-scores are 0..1 and
    /// combined with the explicit weights below (no hidden formula). The optimum is a
    /// thin, even, straight, symmetric edge with good work-hardening and little
    /// overwork damage.
    /// </summary>
    public static class EdgeForgeEvaluator
    {
        // Tuning constants.
        private const float OptimalEdge = 0.35f;      // ideal edge thickness
        private const float OptimalHardening = 0.8f;  // ideal mean work-hardening

        // edgeForgeQuality weights.
        private const float WUniformity = 0.25f;
        private const float WThinness = 0.22f;
        private const float WStraightness = 0.20f;
        private const float WSymmetry = 0.13f;
        private const float WHardening = 0.20f;
        private const float WOverwork = 0.60f; // subtracted

        public static EdgeForgeResult Evaluate(CastBladeState blade)
        {
            var r = new EdgeForgeResult { defects = new List<string>() };
            if (blade == null || blade.SectionCount == 0) return r;

            int n = blade.SectionCount;
            float thinAcc = 0f, thinScoreAcc = 0f, uniAcc = 0f, straightAcc = 0f, symAcc = 0f, hardAcc = 0f, dmgAcc = 0f;
            float prevAvg = -1f;

            foreach (var s in blade.sections)
            {
                float avgEdge = (s.topEdge + s.bottomEdge) * 0.5f;
                thinAcc += Mathf.Clamp01(Mathf.InverseLerp(1.0f, 0.15f, avgEdge));
                thinScoreAcc += 1f - Mathf.Clamp01(Mathf.Abs(avgEdge - OptimalEdge) / OptimalEdge);
                straightAcc += Mathf.Clamp01(Mathf.Abs(s.centerOffset) / 0.25f);
                symAcc += Mathf.Clamp01(Mathf.Abs(s.topEdge - s.bottomEdge) / 0.4f);
                hardAcc += (s.topHardening + s.bottomHardening) * 0.5f;
                dmgAcc += s.damage;
                if (prevAvg >= 0f) uniAcc += Mathf.Clamp01(Mathf.Abs(avgEdge - prevAvg) / 0.3f);
                prevAvg = avgEdge;
            }

            r.edgeThinness = thinAcc / n;
            float thinnessScore = thinScoreAcc / n;
            r.edgeUniformity = 1f - (n > 1 ? uniAcc / (n - 1) : 0f);
            r.straightness = 1f - straightAcc / n;
            r.symmetry = 1f - symAcc / n;
            r.workHardening = hardAcc / n;
            r.overworkDamage = Mathf.Clamp01(dmgAcc / n);

            float hardeningScore = 1f - Mathf.Clamp01(Mathf.Abs(r.workHardening - OptimalHardening) / OptimalHardening);

            r.edgeForgeQuality = Mathf.Clamp01(
                r.edgeUniformity * WUniformity +
                thinnessScore * WThinness +
                r.straightness * WStraightness +
                r.symmetry * WSymmetry +
                hardeningScore * WHardening -
                r.overworkDamage * WOverwork);

            // Derived defects.
            if (r.straightness < 0.55f) r.defects.Add(DefectIds.BentBlade);
            if (r.edgeUniformity < 0.5f) r.defects.Add(DefectIds.UnevenEdge);
            if (r.overworkDamage > 0.6f) r.defects.Add(DefectIds.BrittleStructure);
            else if (r.overworkDamage > 0.35f) r.defects.Add(DefectIds.HairlineCrack);
            if (r.edgeThinness < 0.22f) r.defects.Add(DefectIds.SoftMetal);

            return r;
        }
    }
}
