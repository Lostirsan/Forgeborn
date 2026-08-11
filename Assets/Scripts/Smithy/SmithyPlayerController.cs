using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Simple side-scrolling movement for the blacksmith: horizontal only, clamped
    /// inside the room, with a sprite flip and a gentle idle bob. Movement is frozen
    /// while any UI panel is open. Uses the new Input System via direct device reads
    /// so it never conflicts with existing action maps.
    /// </summary>
    public class SmithyPlayerController : MonoBehaviour
    {
        [SerializeField] private SmithyController controller;
        [SerializeField] private Transform visual;
        [Tooltip("Legacy free walking. Off in the dual-view scene, where stations are selected directly.")]
        [SerializeField] private bool freeMovementEnabled = true;
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float minX = -12f;
        [SerializeField] private float maxX = 12f;
        [SerializeField] private float bobAmplitude = 0.06f;
        [SerializeField] private float bobSpeed = 6f;

        private float _bobTime;
        private float _visualBaseY;
        private int _facing = 1;

        public float PositionX => transform.position.x;

        private void Awake()
        {
            if (visual != null) _visualBaseY = visual.localPosition.y;
        }

        public void SetBounds(float min, float max)
        {
            minX = min;
            maxX = max;
        }

        public void SetPositionX(float x)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(x, minX, maxX);
            transform.position = p;
        }

        private void Update()
        {
            float input = 0f;
            bool blocked = !freeMovementEnabled || (controller != null && controller.IsUIOpen);
            if (!blocked)
                input = ReadHorizontal();

            if (Mathf.Abs(input) > 0.01f)
            {
                var p = transform.position;
                p.x = Mathf.Clamp(p.x + input * moveSpeed * Time.deltaTime, minX, maxX);
                transform.position = p;

                _facing = input > 0 ? 1 : -1;
                if (visual != null)
                {
                    var s = visual.localScale;
                    s.x = Mathf.Abs(s.x) * _facing;
                    visual.localScale = s;
                }
                _bobTime += Time.deltaTime * bobSpeed;
            }
            else
            {
                _bobTime += Time.deltaTime * bobSpeed * 0.4f;
            }

            if (visual != null)
            {
                var lp = visual.localPosition;
                lp.y = _visualBaseY + Mathf.Sin(_bobTime) * bobAmplitude;
                visual.localPosition = lp;
            }
        }

        private static float ReadHorizontal()
        {
            float x = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            }
            var gp = Gamepad.current;
            if (gp != null && Mathf.Abs(x) < 0.01f)
            {
                float lx = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(lx) > 0.2f) x = Mathf.Sign(lx);
                if (gp.dpad.left.isPressed) x = -1f;
                if (gp.dpad.right.isPressed) x = 1f;
            }
            return Mathf.Clamp(x, -1f, 1f);
        }
    }
}
