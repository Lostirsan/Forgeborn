using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// The crucible the player physically tilts to pour. Grab it (LMB) and move the cursor
    /// around the spout pivot: the horizontal offset sets a target tilt angle, reached with
    /// damped, slightly heavy motion (a little inertia). The <b>angle</b> — not a held button
    /// — is what the foundry reads to decide the flow. Rotates a visual around a lip pivot so
    /// the spout moves believably. UI-space (a RectTransform under the panel).
    /// </summary>
    public class CrucibleTiltControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform pivot;       // rotated visual (lip/spout at its base)
        [SerializeField] private RectTransform pourOrigin;  // spout tip — where the stream starts
        [SerializeField] private float degreesPerPixel = 0.16f;
        [SerializeField] private float maxAngle = 78f;
        [SerializeField] private float smoothTime = 0.12f;  // heaviness

        private RectTransform _self;
        private float _targetAngle;
        private float _currentAngle;
        private float _angleVel;
        private bool _held;
        private float _grabY;

        public float CurrentAngle => _currentAngle;   // 0 = upright, positive = tipped to pour
        public bool Held => _held;
        public RectTransform PourOrigin => pourOrigin;

        private void Awake() { _self = (RectTransform)transform; }

        public void ResetTilt()
        {
            _held = false; _targetAngle = 0f; _currentAngle = 0f; _angleVel = 0f;
            Apply();
        }

        public void OnPointerDown(PointerEventData e)
        {
            _held = true;
            // Pulling DOWN tips the pot forward to pour, so the grab is anchored on Y (inverted).
            _grabY = LocalY(e) + _targetAngle / Mathf.Max(0.0001f, degreesPerPixel);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_held) return;
            _targetAngle = Mathf.Clamp((_grabY - LocalY(e)) * degreesPerPixel, 0f, maxAngle);
        }

        public void OnPointerUp(PointerEventData e) => _held = false; // stays at its angle; keep tilting to stop

        private void Update()
        {
            _currentAngle = Mathf.SmoothDampAngle(_currentAngle, _targetAngle, ref _angleVel, smoothTime);
            Apply();
        }

        private void Apply()
        {
            // The pot tilts TOWARD the player (a forward tip), not sideways — the controller
            // swaps in an open-bowl "pouring" sprite for that. Here we only add a subtle
            // vertical foreshorten as the tilt deepens, for a little life. No Z rotation.
            if (pivot == null) return;
            pivot.localRotation = Quaternion.identity;
            float k = maxAngle > 0.01f ? Mathf.Clamp01(_currentAngle / maxAngle) : 0f;
            pivot.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.9f, k), 1f);
        }

        private float LocalY(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_self, e.position, e.pressEventCamera, out var local);
            return local.y;
        }
    }
}
