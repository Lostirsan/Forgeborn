using UnityEngine;

namespace ForgeGame.Research
{
    /// <summary>
    /// Single place that grades a value against a four-point range
    /// (min, idealMin, idealMax, max). Both the research grade (enum) and a
    /// continuous 0..1 quality come from here, so every stage judges temperature
    /// the same way and no colour logic leaks into gameplay code.
    /// </summary>
    public static class RangeEvaluator
    {
        public static ResultGrade Evaluate(float value, float min, float idealMin, float idealMax, float max)
        {
            if (value >= idealMin && value <= idealMax) return ResultGrade.Perfect;
            if (value >= min && value < idealMin) return ResultGrade.Acceptable;
            if (value > idealMax && value <= max) return ResultGrade.Good;

            float lowFail = min - Mathf.Max(1f, idealMin - min);
            float highFail = max + Mathf.Max(1f, max - idealMax);

            if (value < min) return value >= lowFail ? ResultGrade.Bad : ResultGrade.CriticalFailure;
            return value <= highFail ? ResultGrade.Bad : ResultGrade.CriticalFailure;
        }

        /// <summary>
        /// Continuous 0..1 quality: 1 inside the ideal band, tapering to 0 at the
        /// outer failure edges.
        /// </summary>
        public static float EvaluateQuality(float value, float min, float idealMin, float idealMax, float max)
        {
            if (value >= idealMin && value <= idealMax) return 1f;

            if (value < idealMin)
            {
                float lowFail = min - Mathf.Max(1f, idealMin - min);
                return Mathf.Clamp01(Mathf.InverseLerp(lowFail, idealMin, value));
            }

            float highFail = max + Mathf.Max(1f, max - idealMax);
            return Mathf.Clamp01(Mathf.InverseLerp(highFail, idealMax, value));
        }

        public static float GradeToScore(ResultGrade grade)
        {
            switch (grade)
            {
                case ResultGrade.Perfect: return 1f;
                case ResultGrade.Good: return 0.8f;
                case ResultGrade.Acceptable: return 0.55f;
                case ResultGrade.Bad: return 0.25f;
                default: return 0.05f;
            }
        }
    }
}
