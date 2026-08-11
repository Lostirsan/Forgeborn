using ForgeGame.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// A card in the right-side parts tray. Dragging it out (LMB) does NOT move the card —
    /// it asks the <see cref="AssemblyPanelController"/> to spawn the matching physics part
    /// under the cursor. Only draggable while AVAILABLE; LOCKED/INSTALLED are inert.
    /// </summary>
    public class AssemblyPartCatalogItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum ItemState { Locked, Available, Installed }

        [SerializeField] private AssemblyPanelController controller;
        [SerializeField] private ComponentSlot slot;
        [SerializeField] private int variantIndex;
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private GameObject installedMark;

        public ComponentSlot Slot => slot;
        public int VariantIndex => variantIndex;
        private ItemState _state = ItemState.Locked;

        public void SetState(ItemState s)
        {
            _state = s;
            if (icon != null) icon.color = s == ItemState.Locked ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            if (background != null)
                background.color = s == ItemState.Available ? new Color(0.30f, 0.22f, 0.12f, 0.9f)
                                 : s == ItemState.Installed ? new Color(0.16f, 0.24f, 0.14f, 0.9f)
                                 : new Color(0.10f, 0.09f, 0.08f, 0.7f);
            if (installedMark != null) installedMark.SetActive(s == ItemState.Installed);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_state == ItemState.Available && controller != null)
                controller.CatalogDragBegin(slot, variantIndex, e.position);
        }

        public void OnDrag(PointerEventData e) { }        // physics world follows the cursor
        public void OnEndDrag(PointerEventData e) { }     // physics world handles release/cancel
    }
}
