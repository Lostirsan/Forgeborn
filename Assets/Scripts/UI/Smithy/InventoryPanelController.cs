using ForgeGame.Data;
using ForgeGame.Localization;
using ForgeGame.Smithy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Read-only inventory viewer. Items are shown as cards in two columns: a square
    /// "slot" holding the icon on the left, the name and details on the right.
    /// </summary>
    public class InventoryPanelController : SmithyPanel
    {
        [SerializeField] private Transform listContent;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Button backButton;

        private const float CardWidth = 468f;
        private const float CardHeight = 78f;
        private const int Columns = 2;

        private static readonly Color Header = new Color(0.95f, 0.6f, 0.28f);
        private static readonly Color Body = new Color(0.90f, 0.85f, 0.74f);
        private static readonly Color Dim = new Color(0.62f, 0.58f, 0.5f);
        private static readonly Color CardBg = new Color(0.16f, 0.14f, 0.12f, 0.92f);
        private static readonly Color SlotBorder = new Color(0.34f, 0.30f, 0.25f);
        private static readonly Color SlotBg = new Color(0.07f, 0.06f, 0.05f);

        private static readonly Color OreTint = new Color(0.72f, 0.78f, 0.86f);
        private static readonly Color FuelTint = new Color(0.95f, 0.62f, 0.32f);
        private static readonly Color FluxTint = new Color(0.6f, 0.85f, 0.8f);
        private static readonly Color CompTint = new Color(0.8f, 0.7f, 0.95f);
        private static readonly Color IngotTint = new Color(0.9f, 0.7f, 0.42f);
        private static readonly Color BlankTint = new Color(0.85f, 0.55f, 0.3f);
        private static readonly Color WeaponTint = new Color(0.85f, 0.88f, 0.95f);

        private Transform _row;
        private int _col;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
            Loc.LocaleChanged += OnLocaleChanged;
        }

        private void OnDestroy() => Loc.LocaleChanged -= OnLocaleChanged;

        private void OnLocaleChanged()
        {
            if (isActiveAndEnabled && Controller != null) Rebuild();
        }

        protected override void OnOpened() => Rebuild();

        /// <summary>
        /// Fills the panel with representative sample cards WITHOUT needing a live game
        /// (no Controller/Inventory). Used by the art-review scene so designers see the full,
        /// populated UI in the editor. Safe to call in edit mode.
        /// </summary>
        public void BuildPreview()
        {
            if (listContent == null) return;
            RuntimeUI.ClearChildren(listContent);
            _row = null; _col = 0;

            AddHeader("Руда и материалы");
            AddCard(null, OreTint, "Железная руда", "× 12", Body);
            AddCard(null, FuelTint, "Уголь", "× 8", Body);
            AddCard(null, FluxTint, "Флюс", "× 3", Body);
            AddCard(null, CompTint, "Кожаная обмотка", "× 4", Body);

            AddHeader("Слитки");
            AddCard(null, IngotTint, "Бронзовый слиток", "масса 2.4 · чистота 88% · качество 76%", Body);

            AddHeader("Заготовки");
            AddCard(null, BlankTint, "Заготовка клинка (Бронза)", "плавка 82% · заливка 75%", Body);

            AddHeader("Оружие");
            AddCard(null, WeaponTint, "Бронзовый меч", "урон 23.8 · проч. 114 · 58 зол.", new Color(0.98f, 0.9f, 0.7f));
        }

        private void Rebuild()
        {
            if (listContent == null || Controller == null) return;
            var db = Controller.Database;
            var inv = Controller.Inventory;
            RuntimeUI.ClearChildren(listContent);

            AddHeader(Loc.Tr("inventory.materials"));
            bool any = false;
            any |= AddStacks(ItemType.Ore, OreTint);
            any |= AddStacks(ItemType.Fuel, FuelTint);
            any |= AddStacks(ItemType.Flux, FluxTint);
            any |= AddStacks(ItemType.Component, CompTint);
            if (!any) AddEmpty();

            AddHeader(Loc.Tr("inventory.ingots"));
            if (inv.Ingots.Count == 0) AddEmpty();
            foreach (var ig in inv.Ingots)
            {
                var mat = db.GetMaterial(ig.materialId);
                string flags = ig.isScrap ? "  " + Loc.Tr("inventory.flag_scrap")
                             : ig.porous ? "  " + Loc.Tr("inventory.flag_porous")
                             : ig.overheated ? "  " + Loc.Tr("inventory.flag_overheated") : "";
                AddCard(mat != null ? mat.Icon : null, IngotTint,
                    mat != null ? mat.DisplayName : ig.materialId,
                    Loc.Format("inventory.ingot_detail", $"{ig.mass:0.0}", $"{ig.purity * 100f:0}", $"{ig.smeltingQuality * 100f:0}") + flags, Body);
            }

            AddHeader(Loc.Tr("inventory.blanks"));
            if (inv.CastBlanks.Count == 0) AddEmpty();
            foreach (var cb in inv.CastBlanks)
            {
                var mat = db.GetMaterial(cb.materialId);
                var bpc = db.GetBlueprint(cb.blueprintId);
                string det = Loc.Format("inventory.blank_detail", $"{cb.meltQuality * 100f:0}", $"{cb.pourQuality * 100f:0}");
                if (cb.defects != null && cb.defects.Count > 0) det += Loc.Format("inventory.defects", cb.defects.Count);
                AddCard(bpc != null ? bpc.Preview : null, BlankTint,
                    mat != null ? Loc.Format("inventory.blank_name", mat.DisplayName) : Loc.Tr("inventory.blank_cast"), det, Body);
            }

            AddHeader(Loc.Tr("inventory.weapons"));
            if (inv.Weapons.Count == 0) AddEmpty();
            foreach (var wp in inv.Weapons)
            {
                var bp = db.GetBlueprint(wp.blueprintId);
                AddCard(bp != null ? bp.Preview : null, WeaponTint, wp.customName,
                    Loc.Format("inventory.weapon_detail", $"{wp.damage:0.0}", $"{wp.durability:0}", wp.value), new Color(0.98f, 0.9f, 0.7f));
            }
        }

        private bool AddStacks(ItemType type, Color tint)
        {
            var inv = Controller.Inventory;
            var db = Controller.Database;
            bool any = false;
            foreach (var s in inv.GetStacksByType(type))
            {
                var data = db.GetItem(s.itemId);
                AddCard(data != null ? data.Icon : null, tint,
                    data != null ? data.DisplayName : s.itemId, $"× {s.count}", Body);
                any = true;
            }
            return any;
        }

        // ---- Layout building ----

        private void AddHeader(string text)
        {
            _row = null; _col = 0;                     // headers break the column flow
            var t = RuntimeUI.MakeText(listContent, font, text, 26, Header);
            t.fontStyle = FontStyles.Bold;
            var le = t.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 44f; le.preferredHeight = 44f; }
        }

        private void AddEmpty()
        {
            _row = null; _col = 0;
            RuntimeUI.MakeText(listContent, font, Loc.Tr("common.empty"), 20, Dim);
        }

        private Transform NextCell()
        {
            if (_row == null || _col >= Columns) { _row = NewRow(); _col = 0; }
            _col++;
            return _row;
        }

        private Transform NewRow()
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(listContent, false);
            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16; hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
            hl.childAlignment = TextAnchor.UpperLeft;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = CardHeight; le.preferredHeight = CardHeight;
            return go.transform;
        }

        private void AddCard(Sprite icon, Color accent, string name, string detail, Color nameColor)
        {
            var parent = NextCell();

            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            var cardImg = card.AddComponent<Image>(); cardImg.color = CardBg; cardImg.raycastTarget = false;
            var chl = card.AddComponent<HorizontalLayoutGroup>();
            chl.spacing = 12; chl.padding = new RectOffset(10, 12, 8, 8);
            chl.childControlWidth = true; chl.childControlHeight = true;
            chl.childForceExpandWidth = false; chl.childForceExpandHeight = false;
            chl.childAlignment = TextAnchor.MiddleLeft;
            var cle = card.AddComponent<LayoutElement>();
            cle.preferredWidth = CardWidth; cle.minWidth = CardWidth;
            cle.minHeight = CardHeight; cle.preferredHeight = CardHeight; cle.flexibleWidth = 0;

            // Square slot (border + dark inner) with the icon (or a letter fallback).
            float slotSize = CardHeight - 16f;
            var slot = new GameObject("Slot", typeof(RectTransform));
            slot.transform.SetParent(card.transform, false);
            var slotImg = slot.AddComponent<Image>(); slotImg.color = SlotBorder; slotImg.raycastTarget = false;
            var sle = slot.AddComponent<LayoutElement>();
            sle.minWidth = slotSize; sle.preferredWidth = slotSize; sle.minHeight = slotSize; sle.preferredHeight = slotSize;

            var inner = new GameObject("Inner", typeof(RectTransform));
            inner.transform.SetParent(slot.transform, false);
            var innerRt = (RectTransform)inner.transform;
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3, 3); innerRt.offsetMax = new Vector2(-3, -3);
            var innerImg = inner.AddComponent<Image>(); innerImg.color = SlotBg; innerImg.raycastTarget = false;

            if (icon != null)
            {
                var ico = new GameObject("Icon", typeof(RectTransform));
                ico.transform.SetParent(inner.transform, false);
                var icoRt = (RectTransform)ico.transform;
                icoRt.anchorMin = Vector2.zero; icoRt.anchorMax = Vector2.one;
                icoRt.offsetMin = new Vector2(5, 5); icoRt.offsetMax = new Vector2(-5, -5);
                var icoImg = ico.AddComponent<Image>();
                icoImg.sprite = icon; icoImg.preserveAspect = true; icoImg.raycastTarget = false;
            }
            else
            {
                string glyph = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpper();
                var letter = RuntimeUI.MakeText(inner.transform, font, glyph, 30, accent, TextAlignmentOptions.Center);
                letter.fontStyle = FontStyles.Bold;
                var lrt = (RectTransform)letter.transform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            }

            // Name + detail on the right.
            var txt = new GameObject("TextBlock", typeof(RectTransform));
            txt.transform.SetParent(card.transform, false);
            var tvl = txt.AddComponent<VerticalLayoutGroup>();
            tvl.childControlWidth = true; tvl.childControlHeight = true;
            tvl.childForceExpandWidth = true; tvl.childForceExpandHeight = false;
            tvl.spacing = 1; tvl.childAlignment = TextAnchor.MiddleLeft;
            var tle = txt.AddComponent<LayoutElement>(); tle.flexibleWidth = 1;
            RuntimeUI.MakeText(txt.transform, font, name, 21, nameColor);
            if (!string.IsNullOrEmpty(detail)) RuntimeUI.MakeText(txt.transform, font, detail, 16, Dim);
        }
    }
}
