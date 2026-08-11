using System.Collections.Generic;
using ForgeGame.UI.Smithy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Lets the player pick a workstation in the Forge view by mouse, keyboard or
    /// gamepad, with hover highlight and prompt, then routes activation through the
    /// existing <see cref="WorkstationInteractable"/> → <see cref="SmithyController"/>
    /// path. It only reads input while the Forge view is settled and no panel is open,
    /// so it never fights the panels or the view transition.
    /// </summary>
    public class StationSelectionController : MonoBehaviour
    {
        [SerializeField] private SmithyController controller;
        [SerializeField] private SmithyViewController viewController;
        [SerializeField] private InteractionPromptController prompt;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private List<WorkstationInteractable> stations = new List<WorkstationInteractable>();

        private int _selectedIndex;
        private WorkstationInteractable _current;

        private void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private bool IsActive =>
            controller != null && !controller.IsUIOpen &&
            viewController != null && viewController.CurrentView == SmithyViewMode.Forge &&
            !viewController.IsTransitioning;

        private void Update()
        {
            if (!IsActive)
            {
                SetCurrent(null);
                prompt?.Hide();
                return;
            }

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            // Keyboard / gamepad navigation between stations.
            bool left = (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) ||
                        (gp != null && gp.dpad.left.wasPressedThisFrame);
            bool right = (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) ||
                         (gp != null && gp.dpad.right.wasPressedThisFrame);
            if (left) Step(-1);
            if (right) Step(1);

            // Mouse hover overrides the keyboard selection.
            var hovered = HoveredStation();
            if (hovered != null)
            {
                _selectedIndex = Mathf.Max(0, stations.IndexOf(hovered));
                SetCurrent(hovered);
            }
            else
            {
                SetCurrent(stations.Count > 0 ? stations[Mathf.Clamp(_selectedIndex, 0, stations.Count - 1)] : null);
            }

            if (_current != null)
                prompt?.Show(_current.PromptText);
            else
                prompt?.Hide();

            // Activation.
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && hovered != null;
            bool submit = (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                          (gp != null && gp.buttonSouth.wasPressedThisFrame);
            if ((clicked || submit) && _current != null && _current.CanInteract)
                _current.Interact(controller);
        }

        private void Step(int dir)
        {
            if (stations.Count == 0) return;
            _selectedIndex = (_selectedIndex + dir + stations.Count) % stations.Count;
        }

        private WorkstationInteractable HoveredStation()
        {
            if (worldCamera == null || Mouse.current == null) return null;
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            return hit != null ? hit.GetComponentInParent<WorkstationInteractable>() : null;
        }

        private void SetCurrent(WorkstationInteractable next)
        {
            if (_current == next) return;
            if (_current != null) _current.SetHighlighted(false);
            _current = next;
            if (_current != null) _current.SetHighlighted(true);
        }
    }
}
