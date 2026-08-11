using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.Smithy
{
    public enum SmithyViewMode
    {
        Shop,
        Forge,
        Transition
    }

    /// <summary>
    /// Drives the two connected views of the single Smithy scene (shop window vs
    /// forge interior). A <b>CameraRig</b> pans horizontally between two anchors while
    /// layered parallax, a mid-transition zoom, a vignette, a foreground beam and an
    /// ambience cross-fade disguise the pan as a stylised turn inside the room. There
    /// is one orthographic camera and one game state — never a second scene.
    /// </summary>
    public class SmithyViewController : MonoBehaviour
    {
        /// <summary>A world layer that slides at its own rate during a transition.</summary>
        [Serializable]
        public class ParallaxLayer
        {
            public Transform transform;
            [Tooltip("1 = moves with the camera. &gt;1 foreground (faster), &lt;1 background (slower).")]
            public float multiplier = 1f;
        }

        [Header("Camera")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform shopViewAnchor;
        [SerializeField] private Transform forgeViewAnchor;

        [Header("Roots")]
        [SerializeField] private GameObject shopViewRoot;
        [SerializeField] private GameObject forgeViewRoot;

        [Header("HUD & arrows")]
        [SerializeField] private CanvasGroup shopHud;
        [SerializeField] private CanvasGroup forgeHud;
        [SerializeField] private Button shopToForgeArrow;
        [SerializeField] private Button forgeToShopArrow;
        [SerializeField] private CanvasGroup transitionBlocker;
        [SerializeField] private CanvasGroup vignette;

        [Header("Transition dressing")]
        [SerializeField] private Transform transitionBeam;
        [SerializeField] private List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();
        [SerializeField] private float parallaxStrength = 3f;

        [Header("Tuning")]
        [SerializeField] private float transitionDuration = 0.8f;
        [SerializeField] private float zoomAmount = 1.2f;
        [SerializeField] private float vignetteMax = 0.6f;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Refs")]
        [SerializeField] private SmithyController smithyController;

        private SmithyViewMode _currentView = SmithyViewMode.Shop;
        private bool _isTransitioning;
        private Coroutine _routine;
        private Vector3[] _layerInitialLocal;
        private float _baseOrthoSize = 8f;

        public SmithyViewMode CurrentView => _currentView;
        public bool IsTransitioning => _isTransitioning;

        private Vector3 ShopRigPos => shopViewAnchor != null ? shopViewAnchor.position : Vector3.zero;
        private Vector3 ForgeRigPos => forgeViewAnchor != null ? forgeViewAnchor.position : new Vector3(44, 0, -10);

        private void Awake()
        {
            CacheLayerInitials();
            if (mainCamera != null) _baseOrthoSize = mainCamera.orthographicSize;

            if (shopToForgeArrow != null) shopToForgeArrow.onClick.AddListener(ShowForgeView);
            if (forgeToShopArrow != null) forgeToShopArrow.onClick.AddListener(ShowShopView);
        }

        private void Start()
        {
            SetViewImmediate(SmithyViewMode.Shop);
        }

        private void OnDestroy()
        {
            if (_routine != null) StopCoroutine(_routine);
            if (shopToForgeArrow != null) shopToForgeArrow.onClick.RemoveListener(ShowForgeView);
            if (forgeToShopArrow != null) forgeToShopArrow.onClick.RemoveListener(ShowShopView);
        }

        private void CacheLayerInitials()
        {
            _layerInitialLocal = new Vector3[parallaxLayers.Count];
            for (int i = 0; i < parallaxLayers.Count; i++)
                if (parallaxLayers[i]?.transform != null)
                    _layerInitialLocal[i] = parallaxLayers[i].transform.localPosition;
        }

        private void Update()
        {
            if (_isTransitioning) return;
            bool uiOpen = smithyController != null && smithyController.IsUIOpen;
            bool shop = _currentView == SmithyViewMode.Shop;
            SetHudState(shopHud, shopToForgeArrow, shop && !uiOpen);
            SetHudState(forgeHud, forgeToShopArrow, !shop && !uiOpen);
        }

        // ---- Public API ----

        public void ShowShopView() => RequestView(SmithyViewMode.Shop);
        public void ShowForgeView() => RequestView(SmithyViewMode.Forge);

        public void ToggleView()
        {
            RequestView(_currentView == SmithyViewMode.Shop ? SmithyViewMode.Forge : SmithyViewMode.Shop);
        }

        private void RequestView(SmithyViewMode target)
        {
            if (_isTransitioning) return;
            if (smithyController != null && smithyController.IsUIOpen) return; // no switch with a panel open
            if (target == _currentView) return;
            _routine = StartCoroutine(TransitionRoutine(_currentView, target));
        }

        /// <summary>Snaps to a view with no animation (used for the initial state).</summary>
        public void SetViewImmediate(SmithyViewMode mode)
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _isTransitioning = false;
            _currentView = mode == SmithyViewMode.Forge ? SmithyViewMode.Forge : SmithyViewMode.Shop;

            if (cameraRig != null) cameraRig.position = _currentView == SmithyViewMode.Forge ? ForgeRigPos : ShopRigPos;
            if (mainCamera != null) mainCamera.orthographicSize = _baseOrthoSize;
            if (vignette != null) vignette.alpha = 0f;
            // StopCoroutine skips the transition's finally, so clear the blocker here too.
            if (transitionBlocker != null) transitionBlocker.blocksRaycasts = false;
            ResetParallax();
            ApplyRestState();
        }

        // ---- Transition ----

        private IEnumerator TransitionRoutine(SmithyViewMode from, SmithyViewMode to)
        {
            _isTransitioning = true;
            var audio = smithyController != null ? smithyController.Audio : null;
            audio?.PlayTransitionCreak();

            // Both compositions visible during the sweep so the seam/beam can mask the swap.
            if (shopViewRoot != null) shopViewRoot.SetActive(true);
            if (forgeViewRoot != null) forgeViewRoot.SetActive(true);
            SetHudState(shopHud, shopToForgeArrow, false);
            SetHudState(forgeHud, forgeToShopArrow, false);
            if (transitionBlocker != null) transitionBlocker.blocksRaycasts = true;

            Vector3 fromRig = from == SmithyViewMode.Forge ? ForgeRigPos : ShopRigPos;
            Vector3 toRig = to == SmithyViewMode.Forge ? ForgeRigPos : ShopRigPos;
            float dir = to == SmithyViewMode.Forge ? 1f : -1f;
            float dur = Mathf.Max(0.05f, transitionDuration);

            try
            {
                float t = 0f;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / dur);
                    float e = easing.Evaluate(p);
                    float bump = Mathf.Sin(p * Mathf.PI); // 0 → 1 → 0

                    if (cameraRig != null) cameraRig.position = Vector3.Lerp(fromRig, toRig, e);
                    if (mainCamera != null) mainCamera.orthographicSize = _baseOrthoSize - zoomAmount * bump;
                    if (vignette != null) vignette.alpha = vignetteMax * bump;
                    ApplyParallax(dir, bump);
                    audio?.SetAmbienceBlend(to == SmithyViewMode.Forge ? e : 1f - e);

                    yield return null;
                }
            }
            finally
            {
                // Always land in a valid state, even if interrupted.
                if (cameraRig != null) cameraRig.position = toRig;
                if (mainCamera != null) mainCamera.orthographicSize = _baseOrthoSize;
                if (vignette != null) vignette.alpha = 0f;
                ResetParallax();
                if (transitionBlocker != null) transitionBlocker.blocksRaycasts = false;
                _currentView = to;
                _isTransitioning = false;
                _routine = null;
                ApplyRestState();
                audio?.SetAmbienceBlend(to == SmithyViewMode.Forge ? 1f : 0f);
            }
        }

        private void ApplyParallax(float dir, float bump)
        {
            if (_layerInitialLocal == null) return;
            for (int i = 0; i < parallaxLayers.Count; i++)
            {
                var layer = parallaxLayers[i];
                if (layer?.transform == null) continue;
                float offset = -dir * parallaxStrength * (layer.multiplier - 1f) * bump;
                layer.transform.localPosition = _layerInitialLocal[i] + new Vector3(offset, 0f, 0f);
            }
        }

        private void ResetParallax()
        {
            if (_layerInitialLocal == null) return;
            for (int i = 0; i < parallaxLayers.Count; i++)
                if (parallaxLayers[i]?.transform != null)
                    parallaxLayers[i].transform.localPosition = _layerInitialLocal[i];
        }

        /// <summary>Sets root visibility and HUD/arrow state for the settled current view.</summary>
        private void ApplyRestState()
        {
            bool shop = _currentView == SmithyViewMode.Shop;
            if (shopViewRoot != null) shopViewRoot.SetActive(shop);
            if (forgeViewRoot != null) forgeViewRoot.SetActive(!shop);

            bool uiOpen = smithyController != null && smithyController.IsUIOpen;
            SetHudState(shopHud, shopToForgeArrow, shop && !uiOpen);
            SetHudState(forgeHud, forgeToShopArrow, !shop && !uiOpen);
        }

        private static void SetHudState(CanvasGroup group, Button arrow, bool visible)
        {
            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }
            if (arrow != null) arrow.interactable = visible;
        }
    }
}
