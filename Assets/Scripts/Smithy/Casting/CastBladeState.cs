using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgeGame.Smithy.Casting
{
    /// <summary>
    /// One longitudinal slice of the cast blade. Each section tracks its two edges
    /// (top/bottom) independently so hammer blows change local state, plus a spine
    /// offset used for straightness and local overwork damage.
    /// </summary>
    [Serializable]
    public class CastBladeSection
    {
        public float normalizedX;   // 0 = tang, 1 = tip

        public float topEdge;       // edge thickness 1 = fat as-cast, ~0.3 = well forged
        public float bottomEdge;

        public float topWork;       // total work applied to this edge
        public float bottomWork;

        public float topHardening;  // 0..~1.5 work-hardening
        public float bottomHardening;

        public float centerOffset;  // spine deviation from a straight line (bend)
        public float damage;        // 0..1 local overwork damage
    }

    /// <summary>
    /// The whole cast blade as a chain of sections. It is produced by the foundry
    /// (as a thick, slightly uneven casting) and refined on the anvil. Also carries a
    /// baseline profile used by the mesh view to draw a recognizable sword.
    /// </summary>
    [Serializable]
    public class CastBladeState
    {
        public const int DefaultSectionCount = 14;

        public List<CastBladeSection> sections = new List<CastBladeSection>();
        public float pourQuality;
        public float baseStraightness;

        public int SectionCount => sections.Count;

        /// <summary>
        /// Builds a fresh casting. A poor pour starts thicker, more uneven and more
        /// bent, giving the player more to fix on the anvil.
        /// </summary>
        public static CastBladeState CreateFromPour(float pourQuality, int sectionCount = DefaultSectionCount)
        {
            var state = new CastBladeState { pourQuality = Mathf.Clamp01(pourQuality) };
            var rng = new System.Random(unchecked(Environment.TickCount * 2654435761u).GetHashCode());

            float unevenness = Mathf.Lerp(0.14f, 0.02f, state.pourQuality); // worse pour = more noise
            float startThick = Mathf.Lerp(1.0f, 0.85f, state.pourQuality);

            for (int i = 0; i < sectionCount; i++)
            {
                float nx = sectionCount <= 1 ? 0.5f : i / (float)(sectionCount - 1);
                float noise = (float)(rng.NextDouble() - 0.5) * 2f * unevenness;
                float bend = (float)(rng.NextDouble() - 0.5) * 2f * unevenness * 0.8f;
                state.sections.Add(new CastBladeSection
                {
                    normalizedX = nx,
                    topEdge = Mathf.Clamp(startThick + noise, 0.7f, 1.1f),
                    bottomEdge = Mathf.Clamp(startThick - noise * 0.6f, 0.7f, 1.1f),
                    centerOffset = bend,
                    topWork = 0f, bottomWork = 0f,
                    topHardening = 0f, bottomHardening = 0f,
                    damage = 0f
                });
            }
            state.baseStraightness = 1f - unevenness;
            return state;
        }

        /// <summary>
        /// An independent deep copy: a finished weapon keeps its OWN blade geometry so it
        /// never shares a mutable reference with the live session (or with other weapons).
        /// </summary>
        public CastBladeState DeepCopy()
        {
            var copy = new CastBladeState
            {
                pourQuality = pourQuality,
                baseStraightness = baseStraightness,
                sections = new List<CastBladeSection>(sections != null ? sections.Count : 0)
            };
            if (sections != null)
            {
                foreach (var s in sections)
                {
                    copy.sections.Add(new CastBladeSection
                    {
                        normalizedX = s.normalizedX,
                        topEdge = s.topEdge,
                        bottomEdge = s.bottomEdge,
                        topWork = s.topWork,
                        bottomWork = s.bottomWork,
                        topHardening = s.topHardening,
                        bottomHardening = s.bottomHardening,
                        centerOffset = s.centerOffset,
                        damage = s.damage
                    });
                }
            }
            return copy;
        }

        /// <summary>
        /// A clean, deterministic straight blade — used only as a compatibility fallback
        /// for old saved weapons that predate the visual snapshot. Never random, so a
        /// loaded weapon's look is stable.
        /// </summary>
        public static CastBladeState CreateStraight(int sectionCount = DefaultSectionCount)
        {
            var state = new CastBladeState { pourQuality = 0.8f, baseStraightness = 1f };
            for (int i = 0; i < sectionCount; i++)
            {
                float nx = sectionCount <= 1 ? 0.5f : i / (float)(sectionCount - 1);
                state.sections.Add(new CastBladeSection
                {
                    normalizedX = nx, topEdge = 0.6f, bottomEdge = 0.6f, centerOffset = 0f
                });
            }
            return state;
        }

        /// <summary>
        /// Fixed base half-height (silhouette) at a position along the blade, giving a
        /// tapered sword: widest a little past the tang, narrowing to a point at the
        /// tip. Independent of forging so the weapon always reads as a sword.
        /// </summary>
        public static float BaseHalfHeight(float normalizedX)
        {
            // 0..~0.12 tang, swell to ~1 in the mid, taper to a sharp tip at 1.
            if (normalizedX < 0.12f) return Mathf.Lerp(0.35f, 0.85f, normalizedX / 0.12f);
            if (normalizedX > 0.82f) return Mathf.Lerp(1f, 0.06f, (normalizedX - 0.82f) / 0.18f);
            return Mathf.Lerp(0.85f, 1f, Mathf.InverseLerp(0.12f, 0.82f, normalizedX));
        }
    }
}
