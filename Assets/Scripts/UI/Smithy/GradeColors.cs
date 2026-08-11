using ForgeGame.Research;
using UnityEngine;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Maps research grades to colours in one place. Gameplay code deals only in
    /// <see cref="ResultGrade"/>; the colour choice lives here in the UI layer.
    /// </summary>
    public static class GradeColors
    {
        public static readonly Color Unknown = new Color(0.32f, 0.31f, 0.30f, 1f);

        public static Color For(ResultGrade grade)
        {
            switch (grade)
            {
                case ResultGrade.Perfect: return new Color(0.35f, 0.78f, 0.32f);       // green
                case ResultGrade.Good: return new Color(0.66f, 0.80f, 0.28f);          // yellow-green
                case ResultGrade.Acceptable: return new Color(0.87f, 0.74f, 0.24f);    // yellow
                case ResultGrade.Bad: return new Color(0.78f, 0.28f, 0.20f);           // red
                default: return new Color(0.42f, 0.10f, 0.08f);                        // dark red (critical)
            }
        }
    }
}
