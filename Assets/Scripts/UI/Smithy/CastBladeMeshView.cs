using System;
using ForgeGame.Smithy.Casting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Draws the cast blade as a live UI mesh built from its <see cref="CastBladeState"/>
    /// sections (top + bottom vertex per section). It rebuilds after every hammer blow,
    /// resolves the section and edge under the cursor for hover/hit, and reports edge
    /// hits to the anvil panel. Light, controlled deformation — not soft-body physics.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class CastBladeMeshView : Graphic, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [SerializeField] private Texture bladeTexture;
        [SerializeField] private float bendScale = 0.5f;
        [SerializeField] private float horizontalMargin = 40f;

        private CastBladeState _blade;
        private int _hoverSection = -1;
        private bool _hoverTop;

        public override Texture mainTexture => bladeTexture != null ? bladeTexture : base.mainTexture;

        /// <summary>Raised with (sectionIndex, isTopEdge) when the player clicks the blade.</summary>
        public event Action<int, bool> EdgeHit;

        public void SetTexture(Texture tex) { bladeTexture = tex; SetMaterialDirty(); }

        public void SetBlade(CastBladeState blade)
        {
            _blade = blade;
            _hoverSection = -1;
            SetVerticesDirty();
        }

        public void Refresh() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_blade == null || _blade.SectionCount < 2) return;

            Rect r = GetPixelAdjustedRect();
            float x0 = r.xMin + horizontalMargin;
            float x1 = r.xMax - horizontalMargin;
            float cyBase = r.center.y;
            float halfH = r.height * 0.42f;
            int n = _blade.SectionCount;

            for (int i = 0; i < n; i++)
            {
                var s = _blade.sections[i];
                float nx = s.normalizedX;
                float x = Mathf.Lerp(x0, x1, nx);
                float cy = cyBase + s.centerOffset * halfH * bendScale;
                float baseHalf = CastBladeState.BaseHalfHeight(nx) * halfH;
                float topY = cy + baseHalf * (0.55f + 0.45f * s.topEdge);
                float botY = cy - baseHalf * (0.55f + 0.45f * s.bottomEdge);

                Color32 topCol = TintFor(i, true, s);
                Color32 botCol = TintFor(i, false, s);
                vh.AddVert(new Vector3(x, topY), topCol, new Vector2(nx, 1f));
                vh.AddVert(new Vector3(x, botY), botCol, new Vector2(nx, 0f));
            }

            for (int i = 0; i < n - 1; i++)
            {
                int t0 = i * 2, b0 = i * 2 + 1, t1 = (i + 1) * 2, b1 = (i + 1) * 2 + 1;
                vh.AddTriangle(t0, t1, b1);
                vh.AddTriangle(t0, b1, b0);
            }
        }

        private Color32 TintFor(int i, bool top, CastBladeSection s)
        {
            Color c = color;
            if (s.damage > 0.25f) c = Color.Lerp(c, new Color(0.18f, 0.12f, 0.10f), Mathf.Clamp01(s.damage));
            if (i == _hoverSection && top == _hoverTop) c = Color.Lerp(c, Color.white, 0.35f);
            return c;
        }

        // ---- Pointer ----

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Resolve(eventData, out int section, out bool top))
                EdgeHit?.Invoke(section, top);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (Resolve(eventData, out int section, out bool top))
            {
                if (section != _hoverSection || top != _hoverTop)
                {
                    _hoverSection = section; _hoverTop = top;
                    SetVerticesDirty();
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hoverSection != -1) { _hoverSection = -1; SetVerticesDirty(); }
        }

        private bool Resolve(PointerEventData eventData, out int section, out bool top)
        {
            section = -1; top = false;
            if (_blade == null || _blade.SectionCount < 2) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out var local)) return false;

            Rect r = GetPixelAdjustedRect();
            float x0 = r.xMin + horizontalMargin;
            float x1 = r.xMax - horizontalMargin;
            float nx = Mathf.Clamp01(Mathf.InverseLerp(x0, x1, local.x));
            section = Mathf.Clamp(Mathf.RoundToInt(nx * (_blade.SectionCount - 1)), 0, _blade.SectionCount - 1);
            float cy = r.center.y + _blade.sections[section].centerOffset * (r.height * 0.42f) * bendScale;
            top = local.y >= cy;
            return true;
        }
    }
}
