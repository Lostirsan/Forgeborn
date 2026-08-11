using ForgeGame.Data;
using ForgeGame.Smithy;
using ForgeGame.Smithy.Casting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Development-only cheats for the bronze-casting flow. Opened only in the editor
    /// or a development build, so it is inert in a release player.
    /// </summary>
    public class DebugPanelController : SmithyPanel
    {
        [SerializeField] private TMP_Text output;
        [SerializeField] private Button giveBronzeButton;
        [SerializeField] private Button createCastButton;
        [SerializeField] private Button completeForgeButton;
        [SerializeField] private Button openAnvilButton;
        [SerializeField] private Button resetSessionButton;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (giveBronzeButton != null) giveBronzeButton.onClick.AddListener(() => { Controller.Inventory.AddItem(SmithyIds.Bronze, 5); Log("Добавлено 5 бронзы."); });
            if (createCastButton != null) createCastButton.onClick.AddListener(CreateCast);
            if (completeForgeButton != null) completeForgeButton.onClick.AddListener(CompleteForge);
            if (openAnvilButton != null) openAnvilButton.onClick.AddListener(() => Controller.OpenPanel(PanelId.Anvil));
            if (resetSessionButton != null) resetSessionButton.onClick.AddListener(() => { Controller.Session.ClearSession(); Controller.UpdateObjective(); Log("Сессия сброшена."); });
        }

        private void CreateCast()
        {
            var s = Controller.Session.StartNewSession();
            s.selectedMaterialId = SmithyIds.Bronze;
            s.blueprintId = SmithyIds.BronzeSword;
            s.meltQuality = 0.8f;
            s.pourQuality = 0.8f;
            s.castBlade = CastBladeState.CreateFromPour(0.8f);
            Controller.Session.SetStage(ForgeStage.CastBlankReady);
            Controller.UpdateObjective();
            Log("Создана тестовая литая заготовка. Откройте наковальню.");
        }

        private void CompleteForge()
        {
            var s = Controller.Session.Current;
            if (s == null || s.castBlade == null) { Log("Нет заготовки."); return; }
            var r = EdgeForgeEvaluator.Evaluate(s.castBlade);
            s.edgeForgeQuality = r.edgeForgeQuality; s.edgeUniformity = r.edgeUniformity;
            s.edgeThinness = r.edgeThinness; s.straightness = r.straightness; s.symmetry = r.symmetry;
            s.workHardening = r.workHardening; s.overworkDamage = r.overworkDamage;
            Controller.Session.SetStage(ForgeStage.Assembly);
            Controller.UpdateObjective();
            Log($"Ковка завершена ({r.edgeForgeQuality * 100:0}%). К сборке.");
        }

        private void Log(string t) { if (output != null) output.text = t; }
    }
}
