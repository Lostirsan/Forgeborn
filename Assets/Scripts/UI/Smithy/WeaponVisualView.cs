using ForgeGame.Items;
using ForgeGame.Smithy;
using ForgeGame.Smithy.Casting;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// The single reusable renderer that rebuilds a crafted weapon's exact look from its
    /// serialized <see cref="WeaponVisualSnapshot"/> — the forged blade as a live mesh
    /// (reusing <see cref="CastBladeMeshView"/>, read-only, tip-DOWN) plus the chosen
    /// guard/handle/pommel sprite variants at the player's committed horizontal offset and
    /// rotation. Parts are stacked by the SAME <see cref="WeaponAssemblyLayout"/> socket
    /// contact used in the Assembly panel, so the saved preview is laid out identically to
    /// the live build — no gaps, no stored Y. Used by the item-result screen (reusable by
    /// inventory / dungeon later).
    /// </summary>
    public class WeaponVisualView : MonoBehaviour
    {
        [SerializeField] private RectTransform weaponRoot;   // socket-maths space (this view's container)
        [SerializeField] private CastBladeMeshView bladeMesh;
        [SerializeField] private Image tangVisual;           // the sword's tang/rod (behind the hilt parts)
        [SerializeField] private RectTransform bladeTopSocket;

        [Header("Parts + their contact sockets")]
        [SerializeField] private Image guardImage;
        [SerializeField] private RectTransform guardBottom;
        [SerializeField] private RectTransform guardTop;
        [SerializeField] private Image handleImage;
        [SerializeField] private RectTransform handleBottom;
        [SerializeField] private RectTransform handleTop;
        [SerializeField] private Image pommelImage;
        [SerializeField] private RectTransform pommelBottom;

        [Header("Visual catalogue (same arrays the Assembly panel uses)")]
        [SerializeField] private Texture bladeTexture;
        [SerializeField] private Sprite[] guardSprites;
        [SerializeField] private Sprite[] handleSprites;
        [SerializeField] private Sprite[] pommelSprites;

        [Tooltip("Denormalises placement offsets: this view's blade display height.")]
        [SerializeField] private float offsetScale = 720f;
        [SerializeField] private float contactOverlap = 2f;
        [SerializeField] private Color defaultBladeTint = new Color(0.82f, 0.6f, 0.34f, 1f);

        private float CenterlineX => bladeTopSocket != null ? bladeTopSocket.anchoredPosition.x : 0f;

        /// <summary>Renders a finished weapon (uses its snapshot, or a fallback if legacy).</summary>
        public void SetWeapon(WeaponInstance weapon, Color bladeTint)
        {
            if (weapon != null && weapon.visual != null && weapon.visual.HasBlade)
                RenderSnapshot(weapon.visual, bladeTint);
            else
                RenderFallback(bladeTint);
        }

        /// <summary>Renders directly from a snapshot (e.g. an inventory list preview).</summary>
        public void SetVisual(WeaponVisualSnapshot v, Color bladeTint)
        {
            if (v != null && v.HasBlade) RenderSnapshot(v, bladeTint);
            else RenderFallback(bladeTint);
        }

        /// <summary>Renders from a live crafting session (reuses the same layout code).</summary>
        public void SetSession(ForgeSession session, Color bladeTint)
        {
            if (session == null) { RenderFallback(bladeTint); return; }
            RenderSnapshot(WeaponVisualSnapshot.FromSession(session), bladeTint);
        }

        private void RenderSnapshot(WeaponVisualSnapshot v, Color bladeTint)
        {
            ShowBlade(v.HasBlade ? v.blade : CastBladeState.CreateStraight(), bladeTint);
            // Stack top-of-blade → guard → handle → pommel, in order, each seated on the last.
            PlacePart(guardImage, guardSprites, guardBottom, bladeTopSocket, v.guardVariant, v.guardOffsetNorm, v.guardRotation);
            PlacePart(handleImage, handleSprites, handleBottom, guardTop, v.handleVariant, v.handleOffsetNorm, v.handleRotation);
            PlacePart(pommelImage, pommelSprites, pommelBottom, handleTop, v.pommelVariant, v.pommelOffsetNorm, v.pommelRotation);
        }

        private void RenderFallback(Color bladeTint)
        {
            // Legacy weapon (no snapshot): a clean straight bronze sword, upright, variant 0.
            ShowBlade(CastBladeState.CreateStraight(), bladeTint);
            PlacePart(guardImage, guardSprites, guardBottom, bladeTopSocket, 0, 0f, 0f);
            PlacePart(handleImage, handleSprites, handleBottom, guardTop, 0, 0f, 0f);
            PlacePart(pommelImage, pommelSprites, pommelBottom, handleTop, 0, 0f, 0f);
        }

        private void ShowBlade(CastBladeState blade, Color bladeTint)
        {
            if (tangVisual != null) tangVisual.enabled = true;
            if (bladeMesh == null) return;
            bladeMesh.raycastTarget = false; // display only — never intercepts input
            if (bladeTexture != null) bladeMesh.SetTexture(bladeTexture);
            bladeMesh.color = bladeTint.a > 0f ? bladeTint : defaultBladeTint;
            bladeMesh.SetBlade(blade);
        }

        private void PlacePart(Image img, Sprite[] sprites, RectTransform bottomSocket, RectTransform targetTop,
            int variant, float offsetNorm, float rotation)
        {
            if (img == null) return;
            if (sprites != null && sprites.Length > 0)
                img.sprite = sprites[Mathf.Clamp(variant, 0, sprites.Length - 1) % sprites.Length];
            img.enabled = true;

            var rt = (RectTransform)img.transform;
            rt.anchoredPosition = new Vector2(CenterlineX + offsetNorm * offsetScale, 0f); // player's X, temp Y
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);                          // player's rotation
            if (bottomSocket != null && targetTop != null && weaponRoot != null)
                WeaponAssemblyLayout.Contact(weaponRoot, rt, bottomSocket, targetTop, contactOverlap); // seat Y only
        }
    }
}
