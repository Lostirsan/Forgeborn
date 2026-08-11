using System.Text;
using ForgeGame.Data;
using ForgeGame.Items;
using ForgeGame.Smithy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Result screen for a finished weapon: layered visual, editable name, stats,
    /// defects and traits, plus keep/equip/store. The weapon is already in the
    /// inventory, so these buttons only choose what to do with it.
    /// </summary>
    public class ItemResultPanelController : SmithyPanel
    {
        [SerializeField] private TMP_InputField nameField;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text defectsText;
        [SerializeField] private WeaponVisualView weaponVisual;
        [SerializeField] private Button keepButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button backButton;

        private WeaponInstance _weapon;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (keepButton != null) keepButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (storeButton != null) storeButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (equipButton != null) equipButton.onClick.AddListener(Equip);
            if (nameField != null) nameField.onEndEdit.AddListener(OnRename);
        }

        public void Configure(WeaponInstance weapon)
        {
            _weapon = weapon;
            if (weapon == null) return;

            if (nameField != null) nameField.SetTextWithoutNotify(weapon.customName);

            var db = Controller != null ? Controller.Database : null;
            var mat = db != null ? db.GetMaterial(weapon.materialId) : null;

            // Rebuild the EXACT crafted weapon from its snapshot (forged blade mesh + the
            // chosen component variants at the player's committed offsets/rotations).
            if (weaponVisual != null)
                weaponVisual.SetWeapon(weapon, mat != null ? mat.VisualColor : new Color(0.82f, 0.6f, 0.34f, 1f));

            if (statsText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Материал: {(mat != null ? mat.DisplayName : weapon.materialId)}");
                sb.AppendLine($"Плавка: {Pct(weapon.meltQuality)}   Заливка: {Pct(weapon.pourQuality)}");
                sb.AppendLine($"Ковка кромки: {Pct(weapon.edgeForgeQuality)}   Ровность: {Pct(weapon.straightness)}");
                sb.AppendLine();
                sb.AppendLine($"Урон: {weapon.damage:0.0}");
                sb.AppendLine($"Скорость атаки: {weapon.attackSpeed:0.00}");
                sb.AppendLine($"Прочность: {weapon.durability:0}");
                sb.AppendLine($"Пробивание брони: {weapon.armorPenetration:0.0}");
                sb.AppendLine($"Вес: {weapon.weight:0.00}");
                sb.AppendLine($"Баланс: {Pct(weapon.balance)}");
                sb.AppendLine($"Стоимость: {weapon.value}");
                statsText.text = sb.ToString();
            }

            if (defectsText != null)
            {
                var sb = new StringBuilder();
                if (weapon.defectIds.Count > 0)
                {
                    sb.AppendLine("Дефекты:");
                    foreach (var id in weapon.defectIds)
                    {
                        var d = db != null ? db.GetDefect(id) : null;
                        if (d != null && d.HiddenUntilUsed) continue;
                        sb.AppendLine($"• {(d != null ? d.DisplayName : id)}");
                    }
                }
                if (weapon.traits.Count > 0)
                {
                    sb.AppendLine("Особенности:");
                    foreach (var t in weapon.traits) sb.AppendLine($"• {TraitName(t)}");
                }
                if (sb.Length == 0) sb.Append("Без дефектов.");
                defectsText.text = sb.ToString();
            }
        }

        private void Equip()
        {
            if (_weapon != null) Controller.EquipWeapon(_weapon.uniqueId);
            Controller.Notify("Оружие экипировано.");
            Controller.ClosePanel();
        }

        private void OnRename(string value)
        {
            if (_weapon != null && !string.IsNullOrWhiteSpace(value))
                _weapon.customName = value;
        }

        private static string Pct(float v) => $"{Mathf.RoundToInt(v * 100f)}%";

        private static string TraitName(string id)
        {
            switch (id)
            {
                case "anti_supernatural": return "Против нечисти";
                default: return id;
            }
        }
    }
}
