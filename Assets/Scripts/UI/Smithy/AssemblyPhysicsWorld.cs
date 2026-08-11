using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// The world-space assembly stage: a dedicated orthographic camera looks down at a
    /// workbench where the tang, blade shoulder and table boundaries are static colliders.
    /// This drives the ACTIVE loose part — pick it up (kinematic follow + wheel/Q-E
    /// rotation), release it (dynamic, falls under real gravity, collides with the tang
    /// and committed parts) — and reports when it has come to rest on the sword. There is
    /// NO snapping, no return-on-high-release, no forced Y: where the body settles is the
    /// result. UI controls live on a separate Screen-Space overlay.
    /// </summary>
    public class AssemblyPhysicsWorld : MonoBehaviour
    {
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float tangAxisX;
        [SerializeField] private float shoulderY;
        [SerializeField] private float bladeWorldHeight = 5f; // normalisation basis for X offset
        [SerializeField] private float installHalfWidth = 1.5f;
        [SerializeField] private float restSpeed = 0.2f;
        [SerializeField] private float restAngularSpeed = 14f;
        [SerializeField] private float settleTime = 0.4f;
        [SerializeField] private float degreesPerScrollNotch = 6f;
        [SerializeField] private float keyRotateSpeed = 60f;
        [SerializeField] private float grabRadius = 0.7f;
        [SerializeField] private RectTransform catalogRect; // releasing here cancels the drag

        private AssemblyPhysicsPart _active;
        private bool _held;
        private Vector2 _grabOffset;
        private float _restTimer;
        private bool _running;

        public float TangAxisX => tangAxisX;
        public float ShoulderY => shoulderY;
        public float BladeWorldHeight => Mathf.Max(0.001f, bladeWorldHeight);
        public Vector2 SpawnPoint => spawnPoint != null ? (Vector2)spawnPoint.position : new Vector2(tangAxisX, shoulderY + 5f);

        /// <summary>Raised when the active part has rested on the sword long enough to install.</summary>
        public event Action<AssemblyPhysicsPart> PartSettled;

        public void SetRunning(bool on)
        {
            _running = on;
            if (stageCamera != null) stageCamera.enabled = on;
            if (!on) { _active = null; _held = false; }
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            if (stageCamera == null) return new Vector2(tangAxisX, shoulderY);
            Vector3 w = stageCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            return new Vector2(w.x, w.y);
        }

        /// <summary>Start holding a part dragged out of the catalog, at the cursor.</summary>
        public void BeginHeld(AssemblyPhysicsPart part, Vector2 screenPos)
        {
            if (part == null) return;
            part.Spawn(ScreenToWorld(screenPos), 0f);
            part.Hold(); // kinematic follow, no gravity yet
            _active = part; _held = true; _grabOffset = Vector2.zero; _restTimer = 0f;
        }

        private void CancelHeld()
        {
            if (_active != null) _active.gameObject.SetActive(false);
            _active = null; _held = false;
        }

        private void Update()
        {
            if (!_running || _active == null || _active.Committed || stageCamera == null) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Vector3 w3 = stageCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            Vector2 world = new Vector2(w3.x, w3.y);

            if (_held)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    _active.SetHeldRotation(_active.HeldRotation + Mathf.Sign(scroll) * degreesPerScrollNotch);
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.qKey.isPressed) _active.SetHeldRotation(_active.HeldRotation + keyRotateSpeed * Time.deltaTime);
                    if (kb.eKey.isPressed) _active.SetHeldRotation(_active.HeldRotation - keyRotateSpeed * Time.deltaTime);
                }
                _active.FollowCursor(world + _grabOffset);

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    if (catalogRect != null && RectTransformUtility.RectangleContainsScreenPoint(catalogRect, screen, null))
                    {
                        CancelHeld(); // released back in the tray → cancel, nothing falls
                    }
                    else
                    {
                        _held = false;
                        _active.Release(); // gravity takes over from exactly here
                        _restTimer = 0f;
                    }
                }
            }
            else
            {
                bool overUi = UnityEngine.EventSystems.EventSystem.current != null &&
                              UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                bool overPart = _active.ContainsPoint(world) ||
                                Vector2.Distance(world, (Vector2)_active.transform.position) < grabRadius;
                if (mouse.leftButton.wasPressedThisFrame && overPart && !overUi)
                {
                    // Re-pickup is always allowed until the part is committed.
                    _held = true;
                    _grabOffset = (Vector2)_active.transform.position - world;
                    _active.Hold();
                }
                else
                {
                    // Settle only if the part is resting ON the sword (near the tang, above
                    // the table). Resting out on the table does NOT install it.
                    Vector2 pos = _active.transform.position;
                    bool onSword = Mathf.Abs(pos.x - tangAxisX) < installHalfWidth && pos.y > shoulderY - 0.5f;
                    if (onSword && _active.IsResting(restSpeed, restAngularSpeed))
                    {
                        _restTimer += Time.deltaTime;
                        if (_restTimer >= settleTime)
                        {
                            var settled = _active;
                            _active = null;
                            PartSettled?.Invoke(settled);
                        }
                    }
                    else _restTimer = 0f;
                }
            }
        }
    }
}
