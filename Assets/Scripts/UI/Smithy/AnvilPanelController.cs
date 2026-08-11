using System.Collections;
using ForgeGame.Data;
using ForgeGame.Smithy;
using ForgeGame.Smithy.Casting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Post-casting edge forging. The cast blade is shown as a live mesh; the player
    /// clicks the top or bottom edge (LMB) to thin, straighten and work-harden that
    /// section. Moderate, controlled deformation — the blank already has the sword
    /// shape, so this is refinement, not free-form shaping.
    /// </summary>
    public class AnvilPanelController : SmithyPanel
    {
        private const float HitCooldown = 0.2f;
        private const float ThinPerHit = 0.06f;
        private const float NeighborThin = 0.022f;
        private const float WorkPerHit = 0.12f;
        private const float HardenPerHit = 0.09f;
        private const float BendPerHit = 0.018f;
        private const float MinEdge = 0.15f;

        [SerializeField] private CastBladeMeshView meshView;
        [SerializeField] private RectTransform hammer;
        [SerializeField] private RectTransform sparks;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text qualityLabel;
        [SerializeField] private Button finishButton;
        [SerializeField] private Button backButton;
        [SerializeField] private float hitMargin = 40f;

        [Header("Quality bars (Edge / Straight / Balance / Overwork)")]
        [SerializeField] private Image edgeBar;
        [SerializeField] private Image straightBar;
        [SerializeField] private Image balanceBar;
        [SerializeField] private Image overworkBar;

        private float _lastHit = -1f;
        private Vector2 _hammerRest;
        private Coroutine _hammerRoutine;

        private ForgeSession Session => Controller.Session.Current;
        private CastBladeState Blade => Session != null ? Session.castBlade : null;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (finishButton != null) finishButton.onClick.AddListener(Finish);
            if (meshView != null) meshView.EdgeHit += OnEdgeHit;
            if (hammer != null) _hammerRest = hammer.anchoredPosition;
        }

        private void OnDestroy()
        {
            if (meshView != null) meshView.EdgeHit -= OnEdgeHit;
        }

        protected override void OnOpened()
        {
            // No craft in progress? Pull the newest cast blank out of the inventory and
            // start forging it. (Cast blanks are produced/stored by the foundry.)
            if (!Controller.Session.HasActiveSession)
            {
                var blank = Controller.Inventory.TakeNewestCastBlank();
                if (blank != null)
                {
                    var s = Controller.Session.StartNewSession();
                    s.selectedMaterialId = blank.materialId;
                    s.blueprintId = blank.blueprintId;
                    s.meltQuality = blank.meltQuality;
                    s.pourQuality = blank.pourQuality;
                    s.castBlade = blank.blade;
                    if (blank.defects != null) foreach (var d in blank.defects) s.AddDefect(d);
                    Controller.Session.SetStage(ForgeStage.EdgeForging);
                }
            }

            if (Blade != null && (Session.currentStage == ForgeStage.CastBlankReady || Session.currentStage == ForgeStage.EdgeForging))
            {
                if (Session.currentStage == ForgeStage.CastBlankReady)
                    Controller.Session.SetStage(ForgeStage.EdgeForging);
                meshView.SetBlade(Blade);
                if (statusLabel != null) statusLabel.text = "ЛКМ по краю клинка — удар молотом. Обрабатывайте обе стороны равномерно.";
                if (finishButton != null) finishButton.interactable = true;
                if (sparks != null) sparks.gameObject.SetActive(false);
            }
            else
            {
                meshView.SetBlade(null);
                if (statusLabel != null) statusLabel.text = "Нет литой заготовки — сначала отлейте клинок в плавильне.";
                if (finishButton != null) finishButton.interactable = false;
            }
            RefreshQuality();
        }

        private void OnEdgeHit(int section, bool top)
        {
            if (Blade == null || Session.currentStage != ForgeStage.EdgeForging) return;
            if (Time.unscaledTime - _lastHit < HitCooldown) return;
            _lastHit = Time.unscaledTime;

            ApplyHit(section, top);
            meshView.Refresh();
            RefreshQuality();

            Controller.Audio?.PlayHammer();
            Controller.Shake(0.12f);
            PlayHammer(section);
            PlaySparks(section, top);
        }

        private void ApplyHit(int section, bool top)
        {
            var s = Blade.sections[section];
            HitEdge(s, top, ThinPerHit, WorkPerHit, HardenPerHit);
            s.centerOffset += top ? -BendPerHit : BendPerHit; // uneven work bends the spine

            // Overwork / over-thinning damage.
            float work = top ? s.topWork : s.bottomWork;
            float edge = top ? s.topEdge : s.bottomEdge;
            if (work > 1.3f) s.damage = Mathf.Clamp01(s.damage + 0.09f);
            if (edge <= MinEdge + 0.02f) s.damage = Mathf.Clamp01(s.damage + 0.05f);

            // Neighbours take a little.
            if (section > 0) HitEdge(Blade.sections[section - 1], top, NeighborThin, WorkPerHit * 0.4f, HardenPerHit * 0.4f);
            if (section < Blade.SectionCount - 1) HitEdge(Blade.sections[section + 1], top, NeighborThin, WorkPerHit * 0.4f, HardenPerHit * 0.4f);
        }

        private static void HitEdge(CastBladeSection s, bool top, float thin, float work, float harden)
        {
            if (top)
            {
                s.topEdge = Mathf.Max(MinEdge, s.topEdge - thin);
                s.topWork += work;
                s.topHardening = Mathf.Min(1.5f, s.topHardening + harden);
            }
            else
            {
                s.bottomEdge = Mathf.Max(MinEdge, s.bottomEdge - thin);
                s.bottomWork += work;
                s.bottomHardening = Mathf.Min(1.5f, s.bottomHardening + harden);
            }
        }

        private void RefreshQuality()
        {
            if (Blade == null)
            {
                if (qualityLabel != null) qualityLabel.text = "";
                SetBar(edgeBar, 0f); SetBar(straightBar, 0f); SetBar(balanceBar, 0f); SetBar(overworkBar, 0f);
                return;
            }

            // Full six-value evaluation is kept; the UI shows the four that read best.
            var r = EdgeForgeEvaluator.Evaluate(Blade);
            SetBar(edgeBar, r.edgeForgeQuality);   // overall edge quality
            SetBar(straightBar, r.straightness);
            SetBar(balanceBar, r.symmetry);
            SetBar(overworkBar, r.overworkDamage); // this bar is "bad" — more is worse

            if (qualityLabel != null)
                qualityLabel.text = $"Готовность кромки: {r.edgeForgeQuality * 100:0}%";
        }

        private static void SetBar(Image fill, float value01)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(value01);
        }

        private void Finish()
        {
            if (Blade == null) return;
            var r = EdgeForgeEvaluator.Evaluate(Blade);
            Session.edgeForgeQuality = r.edgeForgeQuality;
            Session.edgeUniformity = r.edgeUniformity;
            Session.edgeThinness = r.edgeThinness;
            Session.straightness = r.straightness;
            Session.symmetry = r.symmetry;
            Session.workHardening = r.workHardening;
            Session.overworkDamage = r.overworkDamage;
            foreach (var d in r.defects) Session.AddDefect(d);

            Controller.Session.SetStage(ForgeStage.Assembly);
            Controller.Notify($"Ковка кромки завершена ({r.edgeForgeQuality * 100:0}%). К сборке.");
            Controller.OpenPanel(PanelId.Assembly);
        }

        // ---- Feedback ----

        private float HitLocalX(int section)
        {
            if (meshView == null || Blade == null || Blade.SectionCount < 2) return _hammerRest.x;
            var rect = meshView.rectTransform.rect;
            float nx = section / (float)(Blade.SectionCount - 1);
            float localX = Mathf.Lerp(rect.xMin + hitMargin, rect.xMax - hitMargin, nx);
            return meshView.rectTransform.anchoredPosition.x + localX;
        }

        private void PlayHammer(int section)
        {
            if (hammer == null) return;
            if (_hammerRoutine != null) StopCoroutine(_hammerRoutine);
            _hammerRoutine = StartCoroutine(HammerStrike(HitLocalX(section)));
        }

        private IEnumerator HammerStrike(float x)
        {
            var start = new Vector2(x, _hammerRest.y);
            var hit = new Vector2(x, _hammerRest.y - 120f);
            float t = 0f;
            hammer.gameObject.SetActive(true);
            while (t < 1f) { t += Time.unscaledDeltaTime / 0.06f; hammer.anchoredPosition = Vector2.Lerp(start, hit, t); hammer.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(20f, -10f, t)); yield return null; }
            t = 0f;
            while (t < 1f) { t += Time.unscaledDeltaTime / 0.12f; hammer.anchoredPosition = Vector2.Lerp(hit, start, t); hammer.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-10f, 20f, t)); yield return null; }
            hammer.anchoredPosition = start;
            _hammerRoutine = null;
        }

        private void PlaySparks(int section, bool top)
        {
            if (sparks == null) return;
            float y = _hammerRest.y - 130f + (top ? 30f : -30f);
            sparks.anchoredPosition = new Vector2(HitLocalX(section), y);
            StopCoroutine(nameof(SparkFlash));
            StartCoroutine(SparkFlash());
        }

        private IEnumerator SparkFlash()
        {
            sparks.gameObject.SetActive(true);
            float t = 0f;
            float intensity = Controller.EffectsIntensity;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.18f;
                sparks.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.4f * intensity, t);
                var img = sparks.GetComponent<Image>();
                if (img != null) { var c = img.color; c.a = (1f - t) * intensity; img.color = c; }
                yield return null;
            }
            sparks.gameObject.SetActive(false);
        }
    }
}
