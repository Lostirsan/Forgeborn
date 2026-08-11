using ForgeGame.Data;
using ForgeGame.Smithy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>Read-only inventory viewer grouped by category.</summary>
    public class InventoryPanelController : SmithyPanel
    {
        [SerializeField] private Transform listContent;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Button backButton;

        private static readonly Color Header = new Color(0.95f, 0.6f, 0.28f);
        private static readonly Color Body = new Color(0.86f, 0.81f, 0.69f);

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
        }

        protected override void OnOpened() => Rebuild();

        private void Rebuild()
        {
            if (listContent == null || Controller == null) return;
            var db = Controller.Database;
            var inv = Controller.Inventory;
            RuntimeUI.ClearChildren(listContent);

            AddHeader("Руда и материалы");
            AddStacks(ItemType.Ore);
            AddStacks(ItemType.Fuel);
            AddStacks(ItemType.Flux);
            AddStacks(ItemType.Component);

            AddHeader("Слитки");
            if (inv.Ingots.Count == 0) AddBody("— нет —");
            foreach (var ig in inv.Ingots)
            {
                var mat = db.GetMaterial(ig.materialId);
                string flags = ig.isScrap ? " [лом]" : ig.porous ? " [пористый]" : ig.overheated ? " [перегрет]" : "";
                AddBody($"{(mat != null ? mat.DisplayName : ig.materialId)}: масса {ig.mass:0.0}, чистота {ig.purity * 100f:0}%, качество {ig.smeltingQuality * 100f:0}%{flags}");
            }

            AddHeader("Оружие");
            if (inv.Weapons.Count == 0) AddBody("— нет —");
            foreach (var w in inv.Weapons)
                AddBody($"{w.customName}  (урон {w.damage:0.0}, проч. {w.durability:0}, цена {w.value})");
        }

        private void AddStacks(ItemType type)
        {
            var inv = Controller.Inventory;
            var db = Controller.Database;
            foreach (var s in inv.GetStacksByType(type))
            {
                var data = db.GetItem(s.itemId);
                AddBody($"{(data != null ? data.DisplayName : s.itemId)} × {s.count}");
            }
        }

        private void AddHeader(string t) => RuntimeUI.MakeText(listContent, font, t, 26, Header);
        private void AddBody(string t) => RuntimeUI.MakeText(listContent, font, t, 22, Body);
    }
}
