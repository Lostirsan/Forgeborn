using UnityEngine;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Generic interactable for every workstation and door. It simply forwards the
    /// interaction to the <see cref="SmithyController"/> together with its station
    /// type; the controller decides what that opens or triggers.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WorkstationInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private SmithyStation station;
        [SerializeField] private string promptText = "Взаимодействовать";
        [SerializeField] private bool available = true;

        [Header("Selection highlight (optional)")]
        [SerializeField] private Transform highlightTarget;
        [SerializeField] private GameObject glow;
        [SerializeField] private float highlightScale = 1.08f;
        [SerializeField] private float highlightSpeed = 12f;

        private bool _highlighted;
        private Vector3 _baseScale = Vector3.one;

        public SmithyStation Station => station;
        public string PromptText => promptText;
        public bool CanInteract => available;
        public Transform Transform => transform;

        private void Awake()
        {
            if (highlightTarget == null) highlightTarget = transform;
            _baseScale = highlightTarget.localScale;
            if (glow != null) glow.SetActive(false);
        }

        private void Update()
        {
            if (highlightTarget == null) return;
            Vector3 target = _highlighted ? _baseScale * highlightScale : _baseScale;
            highlightTarget.localScale = Vector3.Lerp(highlightTarget.localScale, target, highlightSpeed * Time.deltaTime);
        }

        public void SetAvailable(bool value) => available = value;

        /// <summary>Turns the hover/selection highlight on or off. Safe with no glow assigned.</summary>
        public void SetHighlighted(bool value)
        {
            _highlighted = value;
            if (glow != null) glow.SetActive(value && available);
        }

        public void Interact(SmithyController controller)
        {
            if (!available || controller == null) return;
            controller.ActivateStation(station);
        }

        public void Configure(SmithyStation stationType, string prompt)
        {
            station = stationType;
            promptText = prompt;
        }
    }
}
