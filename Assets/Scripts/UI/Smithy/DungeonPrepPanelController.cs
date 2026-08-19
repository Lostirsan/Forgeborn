using ForgeGame.Dungeon;
using ForgeGame.Smithy;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Step 1 of the expedition flow — the roster. Every character starts on the LEFT (the
    /// bench) and none are pre-selected; the player clicks a portrait to send that hero into the
    /// party on the RIGHT (and clicks a party portrait to send them back). "Далее" is enabled
    /// once at least one hero is chosen, and opens the equipment step.
    /// </summary>
    public class DungeonPrepPanelController : SmithyPanel
    {
        [Header("Left — available (bench)")]
        [SerializeField] private GameObject leaderAvailable;
        [SerializeField] private Button leaderAvailableButton;
        [SerializeField] private GameObject companionAvailable;
        [SerializeField] private Button companionAvailableButton;
        [SerializeField] private GameObject availableEmpty;   // "— none —" when the bench is empty

        [Header("Right — party")]
        [SerializeField] private GameObject leaderParty;
        [SerializeField] private Button leaderPartyButton;    // click to send back to the bench
        [SerializeField] private GameObject companionParty;
        [SerializeField] private Button companionPartyButton;
        [SerializeField] private GameObject partyEmpty;       // "— none —" when nobody is chosen

        [SerializeField] private Button nextButton;
        [SerializeField] private Button backButton;

        private bool _takeLeader;
        private bool _takeCompanion;

        private void Awake()
        {
            if (leaderAvailableButton != null) leaderAvailableButton.onClick.AddListener(() => SetLeader(true));
            if (leaderPartyButton != null) leaderPartyButton.onClick.AddListener(() => SetLeader(false));
            if (companionAvailableButton != null) companionAvailableButton.onClick.AddListener(() => SetCompanion(true));
            if (companionPartyButton != null) companionPartyButton.onClick.AddListener(() => SetCompanion(false));
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
        }

        protected override void OnOpened()
        {
            // Nobody is pre-selected: a fresh visit starts with an empty party.
            _takeLeader = false;
            _takeCompanion = false;
            Refresh();
        }

        private void SetLeader(bool inParty) { _takeLeader = inParty; Refresh(); }
        private void SetCompanion(bool inParty) { _takeCompanion = inParty; Refresh(); }

        private void Refresh()
        {
            if (leaderAvailable != null) leaderAvailable.SetActive(!_takeLeader);
            if (leaderParty != null) leaderParty.SetActive(_takeLeader);
            if (companionAvailable != null) companionAvailable.SetActive(!_takeCompanion);
            if (companionParty != null) companionParty.SetActive(_takeCompanion);

            if (availableEmpty != null) availableEmpty.SetActive(_takeLeader && _takeCompanion);
            if (partyEmpty != null) partyEmpty.SetActive(!_takeLeader && !_takeCompanion);
            if (nextButton != null) nextButton.interactable = _takeLeader || _takeCompanion;
        }

        private void Next()
        {
            if (!_takeLeader && !_takeCompanion) return;
            ExpeditionLoadout.configured = true;
            ExpeditionLoadout.takeLeader = _takeLeader;
            ExpeditionLoadout.takeCompanion = _takeCompanion;
            Controller?.OpenPanel(PanelId.DungeonEquip);
        }
    }
}
