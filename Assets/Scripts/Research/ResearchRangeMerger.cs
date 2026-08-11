using System.Collections.Generic;

namespace ForgeGame.Research
{
    /// <summary>A contiguous discovered span of a scale with a single grade.</summary>
    public struct ResearchSegment
    {
        public float min;
        public float max;
        public ResultGrade grade;

        public ResearchSegment(float min, float max, ResultGrade grade)
        {
            this.min = min; this.max = max; this.grade = grade;
        }
    }

    /// <summary>
    /// Turns a list of overlapping experiment samples into a small, normalized set
    /// of coloured segments for display. Later experiments refine earlier ones on
    /// any overlap, and adjacent equal grades are merged so we never produce
    /// hundreds of slivers.
    /// </summary>
    public static class ResearchRangeMerger
    {
        public static List<ResearchSegment> BuildSegments(List<ResearchRangeSample> samples)
        {
            var result = new List<ResearchSegment>();
            if (samples == null || samples.Count == 0) return result;

            // Collect all unique boundaries.
            var bounds = new SortedSet<float>();
            foreach (var s in samples)
            {
                bounds.Add(s.testedRangeMin);
                bounds.Add(s.testedRangeMax);
            }
            var b = new List<float>(bounds);

            // For each elementary interval, pick the grade of the newest covering sample.
            for (int i = 0; i < b.Count - 1; i++)
            {
                float lo = b[i];
                float hi = b[i + 1];
                float mid = (lo + hi) * 0.5f;

                bool covered = false;
                int newest = int.MinValue;
                ResultGrade grade = ResultGrade.Bad;
                foreach (var s in samples)
                {
                    if (mid >= s.testedRangeMin && mid <= s.testedRangeMax && s.experimentNumber >= newest)
                    {
                        covered = true;
                        newest = s.experimentNumber;
                        grade = s.resultGrade;
                    }
                }
                if (!covered) continue;

                if (result.Count > 0)
                {
                    var last = result[result.Count - 1];
                    if (last.grade == grade && Mathf_Approximately(last.max, lo))
                    {
                        last.max = hi;
                        result[result.Count - 1] = last;
                        continue;
                    }
                }
                result.Add(new ResearchSegment(lo, hi, grade));
            }
            return result;
        }

        private static bool Mathf_Approximately(float a, float b) =>
            UnityEngine.Mathf.Abs(a - b) < 0.01f;
    }
}
