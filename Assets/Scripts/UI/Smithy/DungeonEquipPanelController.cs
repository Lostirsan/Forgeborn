using System.Collections.Generic;
using ForgeGame.Dungeon;
using ForgeGame.Items;
using ForgeGame.Localization;
using ForgeGame.Smithy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Step 2 of the expedition flow — equipment. Left: portraits of the party (pick who to
    /// equip). Middle: that character's weapon slot. Right: the crafted-weapon inventory — click
    /// a weapon to slot it on the selected character, click the slot to clear it. "Спуститься"
    /// writes each hero's weapon (name + damage bonus) into <see cref="ExpeditionLoadout"/> and
    /// enters the dungeon.
    /// </summary>
    public class DungeonEquipPanelController : SmithyPanel
    {
        [Header("Party (left)")]
        [SerializeField] private Button leaderSelectButton;
        [SerializeField] private Button companionSelectButton;
        [SerializeField] private Image leaderPortrait;
        [SerializeField] private Image companionPortrait;
        [SerializeField] private GameObject leaderEntry;    // hidden if the leader stayed behind
        [SerializeField] private GameObject companionEntry; // hidden if the companion stayed behind

        [Header("Weapon slot (middle)")]
        [SerializeField] private Button weaponSlotButton; // click to clear
        [SerializeField] private Image weaponSlotIcon;
        [SerializeField] private TMP_Text weaponSlotLabel;

        [Header("Inventory (right)")]
        [SerializeField] private Button[] invButtons;
        [SerializeField] private Image[] invIcons;
        [SerializeField] private TMP_Text[] invLabels;

        [Header("Weapon icon lookup (parallel arrays by blueprint id)")]
        [SerializeField] private string[] weaponIconIds;
        [SerializeField] private Sprite[] weaponIconSprites;

        [SerializeField] private Button descendButton;
        [SerializeField] private Button backButton;

        private readonly List<WeaponInstance> _weapons = new List<WeaponInstance>();
        private WeaponInstance _leaderWeapon, _companionWeapon;
        private bool _takeLeader = true, _takeCompanion = true;
        private int _selected; // 0 = leader, 1 = companion

        private static readonly Color Selected = new Color(1.2f, 1.15f, 0.95f);
        private static readonly Color Unselected = new Color(0.7f, 0.7f, 0.72f);

        private void Awake()
        {
            if (leaderSelectButton != null) leaderSelectButton.onClick.AddListener(() => SelectChar(0));
            if (companionSelectButton != null) companionSelectButton.onClick.AddListener(() => SelectChar(1));
            if (weaponSlotButton != null) weaponSlotButton.onClick.AddListener(ClearWeapon);
            if (descendButton != null) descendButton.onClick.AddListener(Descend);
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.OpenPanel(PanelId.DungeonPrep));

            if (invButtons != null)
                for (int i = 0; i < invButtons.Length; i++)
                {
                    int idx = i;
                    if (invButtons[i] != null) invButtons[i].onClick.AddListener(() => AssignWeapon(idx));
                }
        }

        protected override void OnOpened()
        {
            bool cfg = ExpeditionLoadout.configured;
            _takeLeader = !cfg || ExpeditionLoadout.takeLeader;
            _takeCompanion = !cfg || ExpeditionLoadout.takeCompanion;
            _weapons.Clear();
            if (Controller != null && Controller.Inventory != null)
                _weapons.AddRange(Controller.Inventory.Weapons);

            if (leaderEntry != null) leaderEntry.SetActive(_takeLeader);
            if (companionEntry != null) companionEntry.SetActive(_takeCompanion);
            // Select the first hero that is actually coming.
            _selected = _takeLeader ? 0 : 1;

            RefreshInventory();
            Refresh();
        }

        private void SelectChar(int i)
        {
            if (i == 0 && !_takeLeader) return;
            if (i == 1 && !_takeCompanion) return;
            _selected = i;
            Refresh();
        }

        private void AssignWeapon(int invIndex)
        {
            if (invIndex < 0 || invIndex >= _weapons.Count) return;
            if (_selected == 1) _companionWeapon = _weapons[invIndex];
            else _leaderWeapon = _weapons[invIndex];
            Refresh();
        }

        private void ClearWeapon()
        {
            if (_selected == 1) _companionWeapon = null; else _leaderWeapon = null;
            Refresh();
        }

        private WeaponInstance CurrentWeapon => _selected == 1 ? _companionWeapon : _leaderWeapon;

        private void Refresh()
        {
            if (leaderPortrait != null) leaderPortrait.color = _selected == 0 ? Selected : Unselected;
            if (companionPortrait != null) companionPortrait.color = _selected == 1 ? Selected : Unselected;

            var w = CurrentWeapon;
            if (weaponSlotIcon != null)
            {
                var icon = IconFor(w);
                weaponSlotIcon.sprite = icon;
                weaponSlotIcon.enabled = icon != null;
            }
            if (weaponSlotLabel != null)
                weaponSlotLabel.text = w != null ? WeaponName(w) : Loc.Tr("prep.fists");
        }

        private void RefreshInventory()
        {
            if (invButtons == null) return;
            for (int i = 0; i < invButtons.Length; i++)
            {
                bool has = i < _weapons.Count;
                if (invButtons[i] != null) invButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var w = _weapons[i];
                if (invIcons != null && i < invIcons.Length && invIcons[i] != null)
                {
                    var icon = IconFor(w);
                    invIcons[i].sprite = icon;
                    invIcons[i].enabled = icon != null;
                }
                if (invLabels != null && i < invLabels.Length && invLabels[i] != null)
                    invLabels[i].text = WeaponName(w);
            }
        }

        private Sprite IconFor(WeaponInstance w)
        {
            if (w == null || weaponIconIds == null || weaponIconSprites == null) return null;
            int i = System.Array.IndexOf(weaponIconIds, w.blueprintId);
            return (i >= 0 && i < weaponIconSprites.Length) ? weaponIconSprites[i] : null;
        }

        private static string WeaponName(WeaponInstance w)
        {
            if (w == null) return "?";
            if (!string.IsNullOrEmpty(w.customName)) return w.customName;
            string type = string.IsNullOrEmpty(w.blueprintId) ? "sword" : w.blueprintId.Replace("bronze_", "");
            return Loc.Tr("weapon." + type);
        }

        private static int WeaponBonus(WeaponInstance w) =>
            w == null ? 0 : Mathf.Clamp(Mathf.RoundToInt(w.damage * 0.2f), 1, 12);

        private void Descend()
        {
            ExpeditionLoadout.configured = true;
            ExpeditionLoadout.takeLeader = _takeLeader;
            ExpeditionLoadout.takeCompanion = _takeCompanion;
            if (_takeLeader)
            {
                ExpeditionLoadout.leaderWeaponName = _leaderWeapon != null ? WeaponName(_leaderWeapon) : null;
                ExpeditionLoadout.leaderWeaponBonus = WeaponBonus(_leaderWeapon);
            }
            else { ExpeditionLoadout.leaderWeaponName = null; ExpeditionLoadout.leaderWeaponBonus = 0; }
            if (_takeCompanion)
            {
                ExpeditionLoadout.companionWeaponName = _companionWeapon != null ? WeaponName(_companionWeapon) : null;
                ExpeditionLoadout.companionWeaponBonus = WeaponBonus(_companionWeapon);
            }
            else
            {
                ExpeditionLoadout.companionWeaponName = null;
                ExpeditionLoadout.companionWeaponBonus = 0;
            }
            Controller?.GoToDungeon();
        }
    }
}
