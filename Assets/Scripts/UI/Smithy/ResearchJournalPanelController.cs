using System.Text;
using ForgeGame.Data;
using ForgeGame.Research;
using ForgeGame.Smithy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// The blacksmith's journal: per material, shows the discovered melting and
    /// forging scales, quench findings, revealed properties and knowledge level.
    /// Unknown data stays grey / "???".
    /// </summary>
    public class ResearchJournalPanelController : SmithyPanel
    {
        [SerializeField] private Transform materialListContent;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text knowledgeText;
        [SerializeField] private TMP_Text propsText;
        [SerializeField] private TMP_Text quenchText;
        [SerializeField] private ResearchBar meltingBar;
        [SerializeField] private ResearchBar forgeBar;
        [SerializeField] private Button backButton;

        private string _selected;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
        }

        protected override void OnOpened()
        {
            BuildList();
            var mats = Controller.Database.Materials;
            if (mats.Count > 0)
            {
                if (string.IsNullOrEmpty(_selected)) _selected = mats[0].Id;
                Show(_selected);
            }
        }

        private void BuildList()
        {
            if (materialListContent == null) return;
            RuntimeUI.ClearChildren(materialListContent);
            foreach (var mat in Controller.Database.Materials)
            {
                string id = mat.Id;
                var level = Controller.Research.GetKnowledgeLevel(id);
                string label = level == KnowledgeLevel.Unknown ? "??? (не изучен)" : mat.DisplayName;
                RuntimeUI.MakeButtonRow(materialListContent, font, label, 40,
                    new Color(0.14f, 0.12f, 0.10f, 1f), new Color(0.86f, 0.81f, 0.69f), () => Show(id));
            }
        }

        private void Show(string materialId)
        {
            _selected = materialId;
            var mat = Controller.Database.GetMaterial(materialId);
            if (mat == null) return;
            var progress = Controller.Research.GetProgress(materialId);
            var level = Controller.Research.GetKnowledgeLevel(materialId);

            if (titleText != null)
                titleText.text = level == KnowledgeLevel.Unknown ? "Неизвестный материал" : mat.DisplayName;

            if (knowledgeText != null)
                knowledgeText.text = $"Знание: {LevelName(level)}   Экспериментов: {(progress != null ? progress.experimentCount : 0)}";

            if (meltingBar != null)
            {
                var r = mat.MeltingScaleRange;
                meltingBar.SetRange(r.x, r.y);
                meltingBar.Render(Controller.Research.GetSegments(materialId, ResearchStageType.Melting));
                meltingBar.SetMarker(0, false);
            }
            if (forgeBar != null)
            {
                var r = mat.ForgingScaleRange;
                forgeBar.SetRange(r.x, r.y);
                forgeBar.Render(Controller.Research.GetSegments(materialId, ResearchStageType.ForgeHeat));
                forgeBar.SetMarker(0, false);
            }

            if (propsText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Свойства:");
                sb.AppendLine(Prop(progress, "hardness", "Твёрдость", mat.Hardness));
                sb.AppendLine(Prop(progress, "toughness", "Вязкость", mat.Toughness));
                sb.AppendLine(Prop(progress, "flexibility", "Гибкость", mat.Flexibility));
                sb.AppendLine(Prop(progress, "corrosion", "Коррозионная стойкость", mat.CorrosionResistance));
                propsText.text = sb.ToString();
            }

            if (quenchText != null)
            {
                quenchText.text = "Закалка:\n" +
                    QuenchLine(progress, QuenchMedium.Water, "Вода") + "\n" +
                    QuenchLine(progress, QuenchMedium.Oil, "Масло");
            }
        }

        private static string Prop(MaterialResearchProgress p, string key, string label, float value)
        {
            bool known = p != null && p.HasProperty(key);
            return known ? $"• {label}: {value * 100f:0}%" : $"• {label}: ???";
        }

        private string QuenchLine(MaterialResearchProgress p, QuenchMedium m, string label)
        {
            var e = p != null ? p.GetQuench((int)m) : null;
            if (e == null || !e.tested) return $"• {label}: не проверено";
            return $"• {label}: {GradeName(e.bestGrade)}";
        }

        private static string LevelName(KnowledgeLevel l)
        {
            switch (l)
            {
                case KnowledgeLevel.Observed: return "Наблюдение";
                case KnowledgeLevel.Tested: return "Проверено";
                case KnowledgeLevel.Studied: return "Изучено";
                case KnowledgeLevel.Mastered: return "Мастерское";
                default: return "Неизвестно";
            }
        }

        private static string GradeName(ResultGrade g)
        {
            switch (g)
            {
                case ResultGrade.Perfect: return "рекомендуется";
                case ResultGrade.Good: return "хорошо";
                case ResultGrade.Acceptable: return "допустимо";
                case ResultGrade.Bad: return "плохо";
                default: return "провал";
            }
        }
    }
}
