using ForgeGame.Data;
using ForgeGame.Research;
using ForgeGame.Smithy;
using ForgeGame.Smithy.Casting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// The foundry: the whole cast-blank half of production in one station —
    /// <b>melt</b> bronze to the working range, <b>pour</b> it into the sword mould
    /// (visually filling the sword cavity), let it <b>cool</b>, then take out the
    /// cast blade blank. Drives the shared <see cref="ForgeSession"/> so it survives
    /// closing the panel.
    /// </summary>
    public class FoundryPanelController : SmithyPanel
    {
        private const int BronzeCost = 3;
        private const float MaxFillRate = 0.72f;
        private const float SafePourRate = 0.28f;
        private const float CoolTime = 2.5f;
        private const float FillTarget = 1f;

        [Header("Melting (readiness bar — driven by the fire's heat)")]
        [SerializeField] private float meltDuration = 22f;     // seconds to fill the bar at normal heat (factor 1)
        [SerializeField] private float readyFraction = 0.65f;  // below this = under-melted → weak forging
        [SerializeField] private float ruinFraction = 1.6f;    // this much overcook = fully ruined metal

        [Header("Fire / temperature (feed logs to keep it hot)")]
        [SerializeField] private float startTemp = 1000f;
        [SerializeField] private float maxTemp = 1500f;
        [SerializeField] private float minMeltTemp = 850f;     // below this the ore won't melt (bar frozen)
        [SerializeField] private float hotTemp = 1350f;        // melt speed maxes out at this heat
        [SerializeField] private float logHeat = 240f;         // heat added per log dragged in

        [Header("Crucible pour tuning (degrees)")]
        [SerializeField] private float pourStartAngle = 22f;   // stream begins past this tilt
        [SerializeField] private float fullPourAngle = 58f;    // flow reaches max here
        [SerializeField] private float spillAngle = 70f;       // over-tilt → stream overshoots the inlet

        [Header("Groups")]
        [SerializeField] private GameObject materialsGroup;
        [SerializeField] private Button bronzeCard;
        [SerializeField] private GameObject foundryGroup;   // single unified crucible + mould view
        [SerializeField] private GameObject mouldObject;    // the mould below (fades in when pouring)

        [Header("Melting / gauge")]
        [SerializeField] private TMP_Text tempLabel;
        [SerializeField] private Image tempBar;              // horizontal fill of current heat
        [SerializeField] private GameObject tempBoardObject; // temperature board (melting only)
        [SerializeField] private GameObject fireObject;      // flames under the crucible (melting only)
        [SerializeField] private Image fireImage;            // pulsed by heat
        [SerializeField] private GameObject logPileObject;   // firewood pile (melting only)
        [SerializeField] private TMP_Text stateLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Image crucibleMoltenImage;
        [SerializeField] private GameObject gaugeObject;    // temperature dial — only shown while melting
        [SerializeField] private RectTransform gaugeNeedle; // sweeps LOW→good→overheat as the ore heats
        [SerializeField] private Button startButton;

        [Header("Pouring / cooling")]
        [SerializeField] private CrucibleTiltControl crucibleTilt;
        [SerializeField] private Image crucibleImage;      // swapped between upright / forward-pour pose
        [SerializeField] private Sprite crucibleMeltSprite; // upright pot (melting)
        [SerializeField] private Sprite cruciblePourSprite; // forward-tilted open bowl (pouring)
        [SerializeField] private Image crucibleMoltenPour; // molten level inside the pouring crucible
        [SerializeField] private Image spillFlash;
        [SerializeField] private Button pourFinishButton;
        [SerializeField] private RectTransform mouldFill;
        [SerializeField] private Image mouldFillImage;
        [SerializeField] private GameObject streamObject;
        [SerializeField] private RectTransform pourCrucible;
        [SerializeField] private TMP_Text pourStatus;
        [SerializeField] private float mouldFillMaxHeight = 460f;

        [Header("Cast blank")]
        [SerializeField] private GameObject castBladeObject;
        [SerializeField] private Button extractButton;

        [SerializeField] private Button backButton;

        private bool _open;
        private float _coolTimer;

        private ForgeSession Session => Controller.Session.Current;
        private MaterialData Bronze => Controller.Database.GetMaterial(SmithyIds.Bronze);

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (startButton != null) startButton.onClick.AddListener(StartMelt);
            if (bronzeCard != null) bronzeCard.onClick.AddListener(StartMelt);
            if (pourFinishButton != null) pourFinishButton.onClick.AddListener(FinishPouring);
            if (extractButton != null) extractButton.onClick.AddListener(ExtractBlade);
        }

        protected override void OnOpened()
        {
            _open = true;
            _coolTimer = 0f;
            ApplyPhase();
            RefreshMelting();
        }

        protected override void OnClosed() => _open = false;

        private void Update()
        {
            if (!_open || Session == null) return;
            switch (Session.currentStage)
            {
                case ForgeStage.Melting: TickMelting(); break;
                case ForgeStage.Pouring: TickPouring(); break;
                case ForgeStage.Cooling: TickCooling(); break;
            }
        }

        // ---- Start ----

        private void StartMelt()
        {
            if (Controller.Session.HasActiveSession) { Controller.Notify("Уже есть незавершённый меч."); return; }
            if (Controller.Inventory.GetCount(SmithyIds.Bronze) < BronzeCost)
            {
                if (statusLabel != null) statusLabel.text = $"Нужно {BronzeCost} бронзы.";
                return;
            }
            Controller.Inventory.RemoveItem(SmithyIds.Bronze, BronzeCost);
            var s = Controller.Session.StartNewSession();
            s.selectedMaterialId = SmithyIds.Bronze;
            s.blueprintId = SmithyIds.BronzeSword;
            // Fire starts lit but coolish; the player feeds logs to keep the ore melting.
            s.meltTemperature = startTemp;
            s.meltQuality = 0f;
            s.meltProgress = 0f;
            s.meltTimer = 0f;
            s.overheatExposure = 0f;
            s.fillAmount = 0f;
            s.pourQualityWeightedSum = 0f;
            s.pouredAmountForQuality = 0f;
            s.lastPourRate = 0f;
            s.remainingMetal = 1f;
            s.spilledMetal = 0f;
            Controller.Session.SetStage(ForgeStage.Melting);
            Controller.Audio?.PlayBellows();
            ApplyPhase();
        }

        // ---- Melting ----

        // Readiness p: 0 at start, 1.0 when the bar is full (ideal pour), >1 = overcook.
        // Now stored directly in meltProgress (its rate is set by the fire's heat).
        private float MeltP => Session != null ? Session.meltProgress : 0f;

        // How fast the ore melts at the current heat: frozen below minMeltTemp, then rising —
        // the hotter the fire, the faster it goes (up to hotTemp).
        private float HeatFactor(float temp)
        {
            if (temp < minMeltTemp) return 0f;
            return Mathf.Lerp(0.35f, 1.8f, Mathf.Clamp01(Mathf.InverseLerp(minMeltTemp, hotTemp, temp)));
        }

        // Quality by WHEN you pour: too early (< readyFraction) = under-melted and weak; a good
        // window rising to a peak at a full bar; then it degrades the longer it overcooks.
        private float MeltQualityAt(float p)
        {
            if (p < readyFraction) return Mathf.Lerp(0.08f, 0.45f, Mathf.Clamp01(p / readyFraction));
            if (p <= 1f) return Mathf.Lerp(0.7f, 1f, Mathf.InverseLerp(readyFraction, 1f, p));
            return Mathf.Lerp(1f, 0.2f, Mathf.Clamp01(Mathf.InverseLerp(1f, ruinFraction, p)));
        }

        private void TickMelting()
        {
            // Temperature holds where the player set it (no passive cooling); dragging logs in
            // raises it. The readiness bar advances only while hot enough, faster the hotter it is.
            float factor = HeatFactor(Session.meltTemperature);
            if (factor > 0f && meltDuration > 0.01f)
                Session.meltProgress = Mathf.Min(ruinFraction, Session.meltProgress + factor / meltDuration * Time.deltaTime);
            Session.meltQuality = MeltQualityAt(Session.meltProgress);

            // Pour at ANY time — the crucible is always grabbable while melting; tilting past the
            // pour threshold locks in the quality and starts pouring.
            if (crucibleTilt != null) crucibleTilt.enabled = true;
            if (crucibleTilt != null && crucibleTilt.CurrentAngle > pourStartAngle) { BeginPouring(); return; }
            RefreshMelting();
        }

        // Throw a log on the fire: a burst of heat (feeds the melt). Fire flares briefly.
        // Public so the drag-and-drop log (FoundryLogDrag) can call it on a valid drop.
        public void ThrowLog()
        {
            if (Session == null || Session.currentStage != ForgeStage.Melting) return;
            Session.meltTemperature = Mathf.Min(maxTemp, Session.meltTemperature + logHeat);
            Controller.Audio?.PlayBellows();
            if (fireImage != null) fireImage.rectTransform.localScale = new Vector3(1.25f, 1.3f, 1f); // pops back down in UpdateFire
            RefreshMelting();
        }

        private float GaugeHeat01()
        {
            // Needle sweep mapped so the gauge's green band lines up with the good pour window
            // and the red with overcook. (Art zones: <0.70 amber, <0.90 green, else red.)
            if (Session == null) return 0f;
            float p = MeltP;
            if (p < readyFraction) return Mathf.Lerp(0f, 0.66f, Mathf.Clamp01(p / readyFraction));
            if (p <= 1f) return Mathf.Lerp(0.66f, 0.88f, Mathf.InverseLerp(readyFraction, 1f, p));
            return Mathf.Lerp(0.88f, 1f, Mathf.Clamp01(Mathf.InverseLerp(1f, ruinFraction, p)));
        }

        private void RefreshMelting()
        {
            float p = MeltP;
            float temp = Session != null ? Session.meltTemperature : 20f;
            bool tooCold = temp < minMeltTemp;

            // Temperature board: number + colour-zoned bar (blue cold → green good → red hot).
            Color hot = new Color(1f, 0.45f, 0.15f), warm = new Color(0.55f, 0.85f, 0.4f), cold = new Color(0.45f, 0.65f, 1f);
            Color tcol = tooCold ? cold : temp > hotTemp ? hot : warm;
            if (tempLabel != null) { tempLabel.text = $"{temp:0}°"; tempLabel.color = tcol; }
            if (tempBar != null) { tempBar.fillAmount = Mathf.Clamp01(Mathf.InverseLerp(20f, maxTemp, temp)); tempBar.color = tcol; }

            string state = tooCold ? "Слишком холодно"
                : p < readyFraction ? "Руда плавится"
                : p <= 1f ? "Расплав готов"
                : "Перегрев — металл портится";
            if (stateLabel != null) stateLabel.text = state;

            // Needle sweeps cold→hot; green band = good window, red = overcook.
            if (gaugeNeedle != null) gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(82f, -82f, GaugeHeat01()));
            if (statusLabel != null)
                statusLabel.text = tooCold ? "Огонь остыл — подкиньте бревно, иначе руда не плавится."
                    : p < readyFraction ? "Руда плавится — держите жар. Рано лить = слабая ковка."
                    : p <= 1f ? "Расплав готов — наклоните котёл, чтобы залить форму."
                    : "Металл перегревается и портится — лейте скорее!";

            if (crucibleMoltenImage != null)
            {
                float heat = Mathf.Clamp01(p);
                Color moltenColor = Color.Lerp(new Color(0.3f, 0.14f, 0.08f, 0.35f), new Color(1f, 0.75f, 0.3f, 1f), heat);
                moltenColor.a = Mathf.Lerp(0.25f, 1f, heat);
                crucibleMoltenImage.color = moltenColor;
            }
            UpdateFire(temp);
        }

        // Flames grow with heat; a log-throw briefly pops the scale up and this eases it back.
        private void UpdateFire(float temp)
        {
            if (fireImage == null) return;
            float k = Mathf.Clamp01(Mathf.InverseLerp(300f, maxTemp, temp));
            var target = new Vector3(Mathf.Lerp(0.8f, 1.05f, k), Mathf.Lerp(0.55f, 1.15f, k), 1f);
            fireImage.rectTransform.localScale = Vector3.MoveTowards(fireImage.rectTransform.localScale, target, Time.deltaTime * 1.6f);
            var c = fireImage.color; c.a = Mathf.Lerp(0.5f, 1f, k); fireImage.color = c;
        }

        private void BeginPouring()
        {
            // Pour is allowed at ANY time; the quality is locked in at this moment from how far
            // the melt has progressed. Too early → soft, under-melted metal; too long → burnt.
            float p = MeltP;
            Session.meltQuality = MeltQualityAt(p);
            if (p < readyFraction)
                Session.AddDefect(DefectIds.SoftMetal);                    // недокал — слабая, гнётся
            else if (p > 1f)
            {
                Session.AddDefect(DefectIds.OverheatedMetal);              // перестоял — выгорел
                if (p > (1f + ruinFraction) * 0.5f) Session.AddDefect(DefectIds.BrittleStructure);
            }
            Session.fillAmount = 0f;
            Session.pourQualityWeightedSum = 0f;
            Session.pouredAmountForQuality = 0f;
            Session.lastPourRate = 0f;
            Session.spilledMetal = 0f;
            if (Session.remainingMetal <= 0f) Session.remainingMetal = 1f;
            // Do NOT reset the tilt — the player is already tipping it; the pour flows on.
            Controller.Session.SetStage(ForgeStage.Pouring);
            ApplyPhase();
        }

        // ---- Pouring (manual crucible tilt) ----

        private void TickPouring()
        {
            float angle = crucibleTilt != null ? crucibleTilt.CurrentAngle : 0f;
            float flow01 = angle <= pourStartAngle ? 0f
                : Mathf.Clamp01(Mathf.InverseLerp(pourStartAngle, fullPourAngle, angle));
            // The metal is molten when pouring begins; flow is governed by tilt, not temperature.
            float fluidity = 1f;
            bool empty = Session.remainingMetal <= 0.0001f;
            float pourRate = empty ? 0f : MaxFillRate * flow01 * fluidity;
            Session.lastPourRate = pourRate;

            // Over-tilting overshoots the inlet; too-cold metal dribbles down the outside.
            bool missing = angle > spillAngle || (pourRate > 0.02f && fluidity < 0.3f);
            bool overfull = Session.fillAmount >= FillTarget;

            if (pourCrucible != null) pourCrucible.localRotation = Quaternion.Euler(0f, 0f, -angle);
            if (streamObject != null)
            {
                bool visible = pourRate > 0.01f;
                streamObject.SetActive(visible);
                if (visible && streamObject.transform is RectTransform sr)
                    sr.localScale = new Vector3(Mathf.Lerp(0.35f, 1.5f, flow01), 1f, 1f);
                if (streamObject.GetComponent<Image>() is Image si)
                    si.color = missing ? new Color(1f, 0.45f, 0.15f, 0.9f) : new Color(1f, 0.7f, 0.3f, 1f);
            }

            if (pourRate > 0f)
            {
                float poured = Mathf.Min(pourRate * Time.deltaTime, Session.remainingMetal);
                Session.remainingMetal -= poured;

                if (missing || overfull)
                {
                    Session.spilledMetal += poured;                       // wasted, no fill
                    ShowSpill();
                }
                else
                {
                    Session.fillAmount += poured;
                    float rateQuality = EvaluatePourRateQuality(pourRate);
                    Session.pourQualityWeightedSum += rateQuality * poured;
                    Session.pouredAmountForQuality += poured;
                }
                if (Controller.Audio != null && Random.value < 0.05f) Controller.Audio.PlayWater();
            }
            FadeSpill();
            UpdateFillVisual(new Color(1f, 0.6f, 0.2f, 1f));
            UpdateCrucibleLevel();

            if (pourFinishButton != null) pourFinishButton.interactable = Session.fillAmount > 0.05f || empty;
            if (pourStatus != null)
            {
                string msg = empty ? "Тигель пуст"
                    : angle <= pourStartAngle ? "наклоните тигель, чтобы полилась бронза"
                    : missing ? "мимо формы — бронза проливается!"
                    : overfull ? "форма переполнена!"
                    : pourRate <= SafePourRate ? "ровный поток — хорошо"
                    : "слишком быстро";
                pourStatus.text = $"Форма: {Mathf.Min(1f, Session.fillAmount) * 100f:0}%   Металл: {Session.remainingMetal * 100f:0}%   •   {msg}";
            }
            if (empty && Session.fillAmount <= 0.05f) FinishPouring();
        }

        private void ShowSpill()
        {
            if (spillFlash != null) { var c = spillFlash.color; c.a = 0.85f; spillFlash.color = c; spillFlash.gameObject.SetActive(true); }
        }

        private void FadeSpill()
        {
            if (spillFlash == null || !spillFlash.gameObject.activeSelf) return;
            var c = spillFlash.color; c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 2f); spillFlash.color = c;
            if (c.a <= 0.01f) spillFlash.gameObject.SetActive(false);
        }

        private void UpdateCrucibleLevel()
        {
            if (crucibleMoltenPour == null) return;
            var c = crucibleMoltenPour.color; c.a = Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(Session.remainingMetal)); crucibleMoltenPour.color = c;
        }

        private void FinishPouring()
        {
            if (Session.currentStage != ForgeStage.Pouring) return;
            float fill = Session.fillAmount;
            // Fill accuracy: how close to target; both under- and over-fill hurt.
            float fillScore = fill <= FillTarget ? Mathf.Clamp01(Mathf.InverseLerp(0.4f, FillTarget, fill))
                                                 : Mathf.Clamp01(1f - (fill - FillTarget) * 2.2f);
            float flowScore = Session.pouredAmountForQuality > 0.001f
                ? Session.pourQualityWeightedSum / Session.pouredAmountForQuality
                : 0f;
            float spillPenalty = Mathf.Clamp01(Session.spilledMetal * 1.4f); // metal that missed the inlet
            Session.pourQuality = Mathf.Clamp01((fillScore * 0.4f + flowScore * 0.6f) * (1f - spillPenalty * 0.6f));

            if (fill < 0.4f) { Session.AddDefect(DefectIds.PorousIngot); if (fill < 0.2f) Session.pourQuality *= 0.5f; }
            else if (fill < 0.6f) Session.AddDefect(DefectIds.PorousIngot);
            if (Session.spilledMetal > 0.35f) Session.AddDefect(DefectIds.PorousIngot);

            if (streamObject != null) streamObject.SetActive(false);
            if (crucibleTilt != null) crucibleTilt.ResetTilt();
            if (pourCrucible != null) pourCrucible.localRotation = Quaternion.identity;
            _coolTimer = 0f;
            Controller.Session.SetStage(ForgeStage.Cooling);
            Controller.Audio?.PlaySteam();
            ApplyPhase();
        }

        // ---- Cooling ----

        private void TickCooling()
        {
            _coolTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_coolTimer / CoolTime);
            UpdateFillVisual(Color.Lerp(new Color(1f, 0.6f, 0.2f, 1f), new Color(0.55f, 0.38f, 0.18f, 1f), t));
            if (pourStatus != null) pourStatus.text = "Охлаждение…";
            if (_coolTimer >= CoolTime)
            {
                Session.castBlade = CastBladeState.CreateFromPour(Session.pourQuality);
                Controller.Session.SetStage(ForgeStage.CastBlankReady);
                ApplyPhase();
            }
        }

        private void ExtractBlade()
        {
            if (Session == null || Session.castBlade == null) return;
            // Store the blank in the inventory and FREE the foundry so the player can cast
            // more without forging a whole sword first. The anvil pulls a blank from here.
            Controller.Inventory.AddCastBlank(ForgeGame.Items.CastBlankInstance.FromSession(Session));
            Controller.Session.SetStage(ForgeStage.Completed);
            Controller.Session.ClearSession();
            Controller.Audio?.PlayItemGet();
            Controller.Notify("Заготовка убрана в инвентарь. Плавильня свободна.");
            Controller.UpdateObjective();
            ApplyPhase();
        }

        // ---- Shared ----

        private void UpdateFillVisual(Color color)
        {
            if (mouldFill != null)
                mouldFill.sizeDelta = new Vector2(mouldFill.sizeDelta.x, Mathf.Clamp01(Session.fillAmount) * mouldFillMaxHeight);
            if (mouldFillImage != null) mouldFillImage.color = color;
        }

        private static float EvaluatePourRateQuality(float rate)
        {
            if (rate <= 0.02f) return 0f;
            if (rate <= SafePourRate) return Mathf.Lerp(0.72f, 1f, Mathf.InverseLerp(0.02f, SafePourRate, rate));
            return Mathf.Clamp01(Mathf.InverseLerp(MaxFillRate, SafePourRate, rate));
        }

        private void ApplyPhase()
        {
            var stage = Session != null ? Session.currentStage : ForgeStage.None;
            bool selecting = Session == null || stage == ForgeStage.None;
            bool crafting = stage == ForgeStage.Melting || stage == ForgeStage.Pouring ||
                            stage == ForgeStage.Cooling || stage == ForgeStage.CastBlankReady;
            bool mould = stage == ForgeStage.Pouring || stage == ForgeStage.Cooling || stage == ForgeStage.CastBlankReady;

            // One unified view: crucible (+ gauge) always visible while crafting; the mould
            // below appears once we start pouring.
            if (materialsGroup != null) materialsGroup.SetActive(selecting);
            if (foundryGroup != null) foundryGroup.SetActive(crafting);
            if (mouldObject != null) mouldObject.SetActive(mould);
            // Heating-only props (gauge, temperature board, fire, firewood) show while melting.
            bool melting = stage == ForgeStage.Melting;
            if (gaugeObject != null) gaugeObject.SetActive(melting);
            if (tempBoardObject != null) tempBoardObject.SetActive(melting);
            if (fireObject != null) fireObject.SetActive(melting);
            if (logPileObject != null) logPileObject.SetActive(melting);

            bool canStart = !Controller.Session.HasActiveSession &&
                            Controller.Inventory.GetCount(SmithyIds.Bronze) >= BronzeCost;
            if (bronzeCard != null) bronzeCard.interactable = canStart;
            if (startButton != null) startButton.gameObject.SetActive(false);
            bool otherJob = Controller.Session.HasActiveSession && (int)stage >= (int)ForgeStage.EdgeForging;
            if (otherJob && statusLabel != null) statusLabel.text = "Завершите текущий меч (наковальня/сборка).";
            else if (selecting && statusLabel != null)
                statusLabel.text = canStart ? "Выберите металл для плавки." : $"Нужно {BronzeCost} бронзы.";

            if (streamObject != null) streamObject.SetActive(false);
            // Swap the crucible pose: upright while heating, forward-tilted open bowl while
            // pouring (and cooling, until the blank is taken). The pour pose has the molten
            // baked in, so the separate melt-pool overlay is hidden then.
            bool pourPose = stage == ForgeStage.Pouring || stage == ForgeStage.Cooling;
            if (crucibleImage != null)
                crucibleImage.sprite = pourPose ? cruciblePourSprite : crucibleMeltSprite;
            if (crucibleMoltenPour != null) crucibleMoltenPour.gameObject.SetActive(!pourPose);
            // Tilt is enabled while pouring, and enabled in TickMelting once the ore is molten.
            if (crucibleTilt != null && stage != ForgeStage.Pouring)
            {
                crucibleTilt.enabled = false;
                crucibleTilt.ResetTilt();
                if (pourCrucible != null) { pourCrucible.localRotation = Quaternion.identity; pourCrucible.localScale = Vector3.one; }
            }
            if (castBladeObject != null) castBladeObject.SetActive(stage == ForgeStage.CastBlankReady);
            if (extractButton != null) extractButton.gameObject.SetActive(stage == ForgeStage.CastBlankReady);
            if (pourFinishButton != null) pourFinishButton.gameObject.SetActive(stage == ForgeStage.Pouring);
            if (spillFlash != null) spillFlash.gameObject.SetActive(false);
            // The pour/cooling readout ("Охлаждение…") belongs only to those phases; clear it
            // otherwise so it doesn't linger into melting or the next casting.
            if (pourStatus != null && stage != ForgeStage.Pouring && stage != ForgeStage.Cooling)
                pourStatus.text = "";

            if (mould) { UpdateFillVisual(mouldFillImage != null ? mouldFillImage.color : Color.white); UpdateCrucibleLevel(); }
        }
    }
}
