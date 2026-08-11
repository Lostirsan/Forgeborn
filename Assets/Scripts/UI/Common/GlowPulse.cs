using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Common
{
    /// <summary>
    /// Gently pulses a graphic's alpha and scale to fake the flicker of a forge
    /// fire. Purely decorative and self-contained; safe to remove or replace when
    /// real art arrives.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField] private Graphic target;
        [SerializeField] private float minAlpha = 0.55f;
        [SerializeField] private float maxAlpha = 0.9f;
        [SerializeField] private float minScale = 0.97f;
        [SerializeField] private float maxScale = 1.05f;
        [SerializeField] private float speed = 0.6f;

        private float _seed;

        private void Reset() => target = GetComponent<Graphic>();

        private void Awake()
        {
            if (target == null) target = GetComponent<Graphic>();
            _seed = Random.value * 10f;
        }

        private void Update()
        {
            if (target == null) return;

            // Perlin noise gives an irregular, non-mechanical flicker.
            float n = Mathf.PerlinNoise(_seed, Time.unscaledTime * speed);

            Color c = target.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, n);
            target.color = c;

            float s = Mathf.Lerp(minScale, maxScale, n);
            target.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }
}
