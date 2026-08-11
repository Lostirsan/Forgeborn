using ForgeGame.Data;
using ForgeGame.Items;

namespace ForgeGame.Smithy.Assembly
{
    /// <summary>
    /// Builds a readable placeholder name for a forged weapon from its material and
    /// overall quality, e.g. "Хорошо сбалансированный железный меч" or
    /// "Треснувший стальной клинок".
    /// </summary>
    public static class WeaponNameGenerator
    {
        public static string Generate(WeaponInstance w, SmithyDatabase db)
        {
            var mat = db != null ? db.GetMaterial(w.materialId) : null;
            string adjective = MaterialAdjective(w.materialId, mat);
            string noun = "меч";

            float quality = (w.edgeForgeQuality + w.pourQuality + w.meltQuality + w.assemblyQuality) * 0.25f;

            bool cracked = w.defectIds != null &&
                           (w.defectIds.Contains(DefectIds.HairlineCrack) ||
                            w.defectIds.Contains(DefectIds.BentBlade));
            bool wellBalanced = w.balance >= 0.7f;

            if (cracked)
                return $"Треснувший {adjective.ToLowerInvariant()} клинок";
            if (quality >= 0.85f)
                return $"Безупречный {adjective.ToLowerInvariant()} {noun}";
            if (wellBalanced && quality >= 0.6f)
                return $"Хорошо сбалансированный {adjective.ToLowerInvariant()} {noun}";
            if (quality < 0.35f)
                return $"Грубый {adjective.ToLowerInvariant()} {noun}";

            return $"{adjective} {noun}";
        }

        private static string MaterialAdjective(string materialId, MaterialData mat)
        {
            switch (materialId)
            {
                case "bronze": return "Бронзовый";
                case "iron": return "Железный";
                case "steel": return "Стальной";
                case "silver": return "Серебряный";
                default: return mat != null ? mat.DisplayName : "Кованый";
            }
        }
    }
}
