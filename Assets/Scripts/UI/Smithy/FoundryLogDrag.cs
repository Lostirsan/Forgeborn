using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// A log the player drags (LMB) from the woodpile into the fire. Drop it over the
    /// flames and the foundry gets a burst of heat; the log then snaps back to the pile
    /// so there is always one ready to throw. Dropped anywhere else, it just returns home.
    /// </summary>
    public class FoundryLogDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform fireTarget;         // drop zone (the flames)
        [SerializeField] private FoundryPanelController foundry;   // receives the heat

        private RectTransform _rt;
        private Vector2 _home;
        private bool _dragging;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _home = _rt.anchoredPosition;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _dragging = true;
            _rt.SetAsLastSibling(); // lift above the pile while carried
        }

        public void OnDrag(PointerEventData e)
        {
            if (_rt.parent is RectTransform parent &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out var lp))
                _rt.localPosition = lp;
        }

        public void OnEndDrag(PointerEventData e)
        {
            _dragging = false;
            bool onFire = fireTarget != null &&
                          RectTransformUtility.RectangleContainsScreenPoint(fireTarget, e.position, e.pressEventCamera);
            if (onFire && foundry != null) foundry.ThrowLog();
            _rt.anchoredPosition = _home; // fresh log back on the pile
        }
    }
}
