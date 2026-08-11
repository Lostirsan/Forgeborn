using System;
using System.Collections.Generic;
using ForgeGame.Data;
using UnityEngine;

namespace ForgeGame.Research
{
    /// <summary>
    /// Stores and updates the player's material knowledge. Recording a result only
    /// reveals a local window around the tested value; the merger later turns the
    /// accumulated windows into a display scale. This is the single source of truth
    /// for what the journal and the grey research bars show.
    /// </summary>
    public class ResearchService : MonoBehaviour
    {
        [SerializeField] private float meltingRevealHalfWidth = 30f;
        [SerializeField] private float forgeHeatRevealHalfWidth = 25f;

        private readonly Dictionary<string, MaterialResearchProgress> _progress =
            new Dictionary<string, MaterialResearchProgress>();

        /// <summary>Raised with the affected material id after any knowledge change.</summary>
        public event Action<string> ResearchChanged;

        public MaterialResearchProgress GetProgress(string materialId)
        {
            if (string.IsNullOrEmpty(materialId)) return null;
            return _progress.TryGetValue(materialId, out var p) ? p : null;
        }

        public MaterialResearchProgress GetOrCreate(string materialId)
        {
            if (string.IsNullOrEmpty(materialId)) return null;
            if (!_progress.TryGetValue(materialId, out var p))
            {
                p = new MaterialResearchProgress { materialId = materialId };
                _progress.Add(materialId, p);
            }
            return p;
        }

        public IEnumerable<MaterialResearchProgress> AllProgress => _progress.Values;

        public void RecordTemperatureResult(string materialId, ResearchStageType stage, float testedValue, ResultGrade grade)
        {
            var p = GetOrCreate(materialId);
            if (p == null) return;

            float half = stage == ResearchStageType.Melting ? meltingRevealHalfWidth : forgeHeatRevealHalfWidth;
            p.experimentCount++;
            p.SamplesFor(stage).Add(new ResearchRangeSample
            {
                stageType = stage,
                testedValue = testedValue,
                testedRangeMin = testedValue - half,
                testedRangeMax = testedValue + half,
                resultGrade = grade,
                experimentNumber = p.experimentCount
            });
            ResearchChanged?.Invoke(materialId);
        }

        public void RecordQuenchResult(string materialId, QuenchMedium medium, ResultGrade grade)
        {
            var p = GetOrCreate(materialId);
            if (p == null) return;

            var entry = p.GetQuench((int)medium);
            if (entry == null)
            {
                entry = new QuenchResearchEntry { medium = (int)medium };
                p.quenchEntries.Add(entry);
            }
            entry.bestGrade = entry.tested
                ? (ResultGrade)Mathf.Max((int)entry.bestGrade, (int)grade)
                : grade;
            entry.tested = true;
            entry.experiments++;
            p.experimentCount++;
            ResearchChanged?.Invoke(materialId);
        }

        public void RevealProperty(string materialId, string property)
        {
            var p = GetOrCreate(materialId);
            p?.RevealProperty(property);
            if (p != null) ResearchChanged?.Invoke(materialId);
        }

        public List<ResearchSegment> GetSegments(string materialId, ResearchStageType stage)
        {
            var p = GetProgress(materialId);
            if (p == null) return new List<ResearchSegment>();
            return ResearchRangeMerger.BuildSegments(p.SamplesFor(stage));
        }

        public KnowledgeLevel GetKnowledgeLevel(string materialId)
        {
            var p = GetProgress(materialId);
            if (p == null || p.experimentCount == 0) return KnowledgeLevel.Unknown;
            if (p.mastered) return KnowledgeLevel.Mastered;
            if (p.experimentCount >= 8) return KnowledgeLevel.Studied;
            if (p.experimentCount >= 3) return KnowledgeLevel.Tested;
            return KnowledgeLevel.Observed;
        }

        // ---- Persistence ----

        public List<MaterialResearchProgress> Export()
        {
            return new List<MaterialResearchProgress>(_progress.Values);
        }

        public void LoadFrom(List<MaterialResearchProgress> data)
        {
            _progress.Clear();
            if (data != null)
                foreach (var p in data)
                    if (p != null && !string.IsNullOrEmpty(p.materialId))
                        _progress[p.materialId] = p;
            ResearchChanged?.Invoke(null);
        }

        public void ClearAll()
        {
            _progress.Clear();
            ResearchChanged?.Invoke(null);
        }
    }
}
