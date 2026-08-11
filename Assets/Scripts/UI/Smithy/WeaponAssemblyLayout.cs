using UnityEngine;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Shared geometry for stacking a sword's hilt so parts physically touch, whatever
    /// their sprite size or rotation. Contact is measured through child socket transforms
    /// (which rotate WITH the part), never via hand-picked Y constants — that is what used
    /// to leave gaps. A part's <c>bottomSocket</c> is aligned to the part-below's
    /// <c>topSocket</c>; only Y moves, so the player's X offset and rotation are untouched.
    /// Both the Assembly panel and <see cref="WeaponVisualView"/> use this so the live
    /// build and the saved preview are laid out identically.
    /// </summary>
    public static class WeaponAssemblyLayout
    {
        /// <summary>Vertical shift (in <paramref name="root"/> space) needed for the part's
        /// bottom socket to meet the target top socket, sinking <paramref name="overlap"/>
        /// px into it so no anti-alias seam shows.</summary>
        public static float ContactDeltaY(RectTransform root, RectTransform bottomSocket, RectTransform targetTop, float overlap)
            => (LocalY(root, targetTop) - overlap) - LocalY(root, bottomSocket);

        /// <summary>Instantly seats the part onto the target (Y only; X and rotation kept).</summary>
        public static void Contact(RectTransform root, RectTransform part, RectTransform bottomSocket, RectTransform targetTop, float overlap)
        {
            var ap = part.anchoredPosition;
            ap.y += ContactDeltaY(root, bottomSocket, targetTop, overlap);
            part.anchoredPosition = ap;
        }

        private static float LocalY(RectTransform root, RectTransform s)
            => root.InverseTransformPoint(s.position).y;
    }
}
