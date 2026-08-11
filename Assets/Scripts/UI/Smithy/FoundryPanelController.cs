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
        private const float HeatRate = 130f;   // deg/s auto-heat (gentle so it completes in the good band)
        private const float MaxTemp = 1400f;
        private const float MeltRate = 0.4f;   // gauge fills over a few seconds once in range
        private const float MaxFillRate = 0.72f;
        private const float SafePourRate = 0.28f;
        private const float CoolTime = 2.5f;
        private const float FillTarget = 1f;

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
            var bm = Bronze;
            s.meltTemperature = bm != null ? bm.MeltingMin - 30f : 20f; // already near melting; furnace warms fast
            s.meltQuality = 0f;
            s.meltProgress = 0f;
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

        private void TickMelting()
        {
            // Auto-melt: the furnace heats the ore by itself once selected. The player just
            // watches the gauge and proceeds to pour while the metal is in the good band.
            Session.meltTemperature = Mathf.Clamp(Session.meltTemperature + HeatRate * Time.deltaTime, 20f, MaxTemp);

            var b = Bronze;
            if (b != null)
            {
                float q = RangeEvaluator.EvaluateQuality(Session.meltTemperature,
                    b.MeltingMin, b.MeltingIdealMin, b.MeltingIdealMax, b.MeltingMax);
                if (Session.meltTemperature >= b.MeltingMin)
                {
                    float meltEfficiency = Mathf.Lerp(0.5f, 1f,
                        Mathf.InverseLerp(b.MeltingMin, b.MeltingIdealMin, Session.meltTemperature));
                    Session.meltProgress = Mathf.Clamp01(Session.meltProgress + MeltRate * meltEfficiency * Time.deltaTime);
                }

                if (Session.meltTemperature > b.MeltingIdealMax)
                {
                    float overheat = Mathf.InverseLerp(b.MeltingIdealMax, MaxTemp, Session.meltTemperature);
                    Session.overheatExposure = Mathf.Clamp01(Session.overheatExposure + overheat * 0.22f * Time.deltaTime);
                }

                Session.meltQuality = Mathf.Clamp01(q * (1f - Session.overheatExposure * 0.8f));
            }

            // Once molten, the player can grab and TILT the same crucible; tilting past the
            // pour threshold starts the pour automatically (no separate button/phase).
            bool molten = b != null && Session.meltProgress >= 1f && Session.meltTemperature >= b.MeltingMin;
            if (crucibleTilt != null) crucibleTilt.enabled = molten;
            if (molten && crucibleTilt != null && crucibleTilt.CurrentAngle > pourStartAngle) { BeginPouring(); return; }
            RefreshMelting();
        }

        private float GaugeHeat01()
        {
            // The arc FILLS as the ore melts; a small extra push into the red once overheated.
            if (Session == null) return 0f;
            var b = Bronze;
            float over = (b != null && Session.meltTemperature > b.MeltingMax) ? 0.12f : 0f;
            return Mathf.Clamp01(Session.meltProgress * 0.88f + over);
        }

        private void RefreshMelting()
        {
            var b = Bronze;
            float t = Session != null ? Session.meltTemperature : 20f;
            if (tempLabel != null) tempLabel.text = $"Температура: {t:0}°";

            string state = "Холодно";
            bool molten = false;
            if (b != null)
            {
                // Ready to pour once fully melted and hot enough — overheat is a quality
                // penalty, NOT a lock-out, so the player can still salvage the metal.
                molten = Session != null && Session.meltProgress >= 1f && t >= b.MeltingMin;
                if (t > b.MeltingMax) state = molten ? "Перегрев — лейте!" : "Перегрев!";
                else if (molten) state = "Расплав готов";
                else if (t >= b.MeltingIdealMin) state = "Руда плавится";
                else if (t >= b.MeltingMin) state = "Горячо";
                else if (t >= 400f) state = "Нагрев";
            }
            if (stateLabel != null) stateLabel.text = state;
            // Needle sweeps left(cold)→right(hot); ~+82° at cold, ~-82° at overheat.
            if (gaugeNeedle != null) gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(82f, -82f, GaugeHeat01()));
            if (statusLabel != null && Session != null)
                statusLabel.text = molten
                    ? "Расплав готов — наклоните котёл, чтобы залить форму."
                    : $"Плавление: {Session.meltProgress * 100f:0}%…";

            if (crucibleMoltenImage != null && b != null)
            {
                float heat = Mathf.Clamp01(Mathf.InverseLerp(400f, b.MeltingMax, t));
                Color moltenColor = Color.Lerp(new Color(0.3f, 0.14f, 0.08f, 0.35f), new Color(1f, 0.75f, 0.3f, 1f), heat);
                moltenColor.a = Mathf.Lerp(0.25f, 1f, Session != null ? Session.meltProgress : 0f);
                crucibleMoltenImage.color = moltenColor;
            }
        }

        private void BeginPouring()
        {
            var b = Bronze;
            if (b == null || Session.meltProgress < 1f || Session.meltTemperature < b.MeltingMin) return;
            float temperatureQuality = RangeEvaluator.EvaluateQuality(Session.meltTemperature,
                b.MeltingMin, b.MeltingIdealMin, b.MeltingIdealMax, b.MeltingMax);
            Session.meltQuality = Mathf.Clamp01(temperatureQuality * (1f - Session.overheatExposure * 0.8f));
            if (Session.overheatExposure >= 0.45f) Session.AddDefect(DefectIds.OverheatedMetal);
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
            // Bronze gets less fluid as it cools; near solidification it barely flows.
            float fluidity = Bronze != null
                ? Mathf.Clamp01(Mathf.InverseLerp(Bronze.MeltingMin - 60f, Bronze.MeltingIdealMin, Session.meltTemperature))
                : 1f;
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
                Session.meltTemperature = Mathf.Max(180f, Session.meltTemperature - (24f + pourRate * 32f) * Time.deltaTime);
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
            // The temperature dial only matters while heating; hide it once we start pouring.
            if (gaugeObject != null) gaugeObject.SetActive(stage == ForgeStage.Melting);

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
