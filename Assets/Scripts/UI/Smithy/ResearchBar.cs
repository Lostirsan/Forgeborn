using System.Collections.Generic;
using ForgeGame.Research;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Draws a research scale: a grey "unknown" background with coloured discovered
    /// segments laid on top, plus an optional marker for the current temperature.
    /// Positions are normalized into [min,max] so it is resolution independent.
    /// Segment images are pooled to avoid per-update allocations.
    /// </summary>
    public class ResearchBar : MonoBehaviour
    {
        [SerializeField] private RectTransform segmentContainer;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform marker;

        private float _min;
        private float _max = 1f;
        private readonly List<Image> _pool = new List<Image>();

        public void SetRange(float min, float max)
        {
            _min = min;
            _max = Mathf.Max(min + 1f, max);
        }

        public void Render(List<ResearchSegment> segments)
        {
            if (segmentContainer == null) return;

            int used = 0;
            if (segments != null)
            {
                for (int i = 0; i < segments.Count; i++)
                {
                    var seg = segments[i];
                    float a = Normalize(seg.min);
                    float b = Normalize(seg.max);
                    if (b <= a) continue;

                    var img = GetPooled(used++);
                    var rt = img.rectTransform;
                    rt.anchorMin = new Vector2(a, 0f);
                    rt.anchorMax = new Vector2(b, 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    img.color = GradeColors.For(seg.grade);
                    img.gameObject.SetActive(true);
                }
            }

            for (int i = used; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);
        }

        public void SetMarker(float value, bool visible)
        {
            if (marker == null) return;
            marker.gameObject.SetActive(visible);
            if (!visible) return;
            float x = Normalize(value);
            marker.anchorMin = new Vector2(x, 0f);
            marker.anchorMax = new Vector2(x, 1f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = Vector2.zero;
        }

        private float Normalize(float v) => Mathf.Clamp01(Mathf.InverseLerp(_min, _max, v));

        private Image GetPooled(int index)
        {
            while (_pool.Count <= index)
            {
                var go = new GameObject("Segment", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(segmentContainer, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                _pool.Add(img);
            }
            return _pool[index];
        }
    }
}
