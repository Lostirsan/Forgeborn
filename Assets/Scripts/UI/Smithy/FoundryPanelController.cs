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
        private const float HeatRate = 340f;   // deg/s while the bellows are held
        private const float CoolRate = 95f;    // deg/s while the fire is unattended
        private const float MaxTemp = 1400f;
        private const float MeltRate = 0.24f;  // full crucible takes several seconds in-range
        private const float MaxFillRate = 0.72f;
        private const float SafePourRate = 0.28f;
        private const float MinimumPourTilt = 0.16f;
        private const float FullPourTilt = 0.82f;
        private const float CoolTime = 2.5f;

        [Header("Groups")]
        [SerializeField] private GameObject meltingGroup;
        [SerializeField] private GameObject mouldGroup;

        [Header("Melting")]
        [SerializeField] private TMP_Text tempLabel;
        [SerializeField] private TMP_Text stateLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Image crucibleMoltenImage;
        [SerializeField] private PointerHoldRelay heatRelay;
        [SerializeField] private Button startButton;
        [SerializeField] private Button toPourButton;

        [Header("Pouring / cooling")]
        [SerializeField] private PointerHoldRelay pourRelay;
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
        private float _displayedPourTilt;

        private ForgeSession Session => Controller.Session.Current;
        private MaterialData Bronze => Controller.Database.GetMaterial(SmithyIds.Bronze);

        private void Awake()
        {
            if (pourCrucible == null && mouldGroup != null)
                pourCrucible = mouldGroup.transform.Find("PourCrucible") as RectTransform;
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (startButton != null) startButton.onClick.AddListener(StartMelt);
            if (toPourButton != null) toPourButton.onClick.AddListener(BeginPouring);
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
            s.meltTemperature = 20f;
            s.meltQuality = 0f;
            s.meltProgress = 0f;
            s.overheatExposure = 0f;
            s.fillAmount = 0f;
            s.pourQualityWeightedSum = 0f;
            s.pouredAmountForQuality = 0f;
            s.lastPourRate = 0f;
            Controller.Session.SetStage(ForgeStage.Melting);
            Controller.Audio?.PlayBellows();
            ApplyPhase();
        }

        // ---- Melting ----

        private void TickMelting()
        {
            bool heating = heatRelay != null && heatRelay.IsHeld;
            float target = heating ? Session.meltTemperature + HeatRate * Time.deltaTime
                                   : Session.meltTemperature - CoolRate * Time.deltaTime;
            Session.meltTemperature = Mathf.Clamp(target, 20f, MaxTemp);

            var b = Bronze;
            if (b != null)
            {
                float q = RangeEvaluator.EvaluateQuality(Session.meltTemperature,
                    b.MeltingMin, b.MeltingIdealMin, b.MeltingIdealMax, b.MeltingMax);
                if (Session.meltTemperature >= b.MeltingMin)
                {
                    float meltEfficiency = Mathf.Lerp(0.35f, 1f,
                        Mathf.InverseLerp(b.MeltingMin, b.MeltingIdealMin, Session.meltTemperature));
                    Session.meltProgress = Mathf.Clamp01(Session.meltProgress + MeltRate * meltEfficiency * Time.deltaTime);
                }

                if (Session.meltTemperature > b.MeltingIdealMax)
                {
                    float overheat = Mathf.InverseLerp(b.MeltingIdealMax, MaxTemp, Session.meltTemperature);
                    Session.overheatExposure = Mathf.Clamp01(Session.overheatExposure + overheat * 0.16f * Time.deltaTime);
                }

                Session.meltQuality = Mathf.Clamp01(q * (1f - Session.overheatExposure * 0.8f));
            }
            RefreshMelting();
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
                if (t > b.MeltingMax) state = "Перегрев!";
                else if (Session != null && Session.meltProgress >= 1f && t >= b.MeltingMin) { state = "Расплав готов"; molten = true; }
                else if (t >= b.MeltingIdealMin) state = "Руда плавится";
                else if (t >= b.MeltingMin) state = "Горячо";
                else if (t >= 400f) state = "Нагрев";
            }
            if (stateLabel != null) stateLabel.text = state;
            if (toPourButton != null) toPourButton.interactable = molten;
            if (statusLabel != null && Session != null)
                statusLabel.text = molten
                    ? "Бронза расплавлена — переносите тигель к форме."
                    : $"Плавление: {Session.meltProgress * 100f:0}% — удерживайте температуру в рабочей зоне.";

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
            _displayedPourTilt = 0f;
            Controller.Session.SetStage(ForgeStage.Pouring);
            ApplyPhase();
        }

        // ---- Pouring ----

        private void TickPouring()
        {
            bool pouring = pourRelay != null && pourRelay.IsHeld;
            float heldTilt = pouring
                ? Mathf.Max(pourRelay.HorizontalDrag01, Mathf.Clamp01(pourRelay.HeldDuration * 0.12f))
                : 0f;
            _displayedPourTilt = Mathf.MoveTowards(_displayedPourTilt, heldTilt, Time.deltaTime * 2.8f);
            float flow01 = Mathf.InverseLerp(MinimumPourTilt, FullPourTilt, _displayedPourTilt);
            float temperatureViscosity = Bronze != null
                ? Mathf.Clamp01(Mathf.InverseLerp(Bronze.MeltingMin - 120f, Bronze.MeltingIdealMin, Session.meltTemperature))
                : 1f;
            float pourRate = MaxFillRate * flow01 * temperatureViscosity;
            Session.lastPourRate = pourRate;

            if (pourCrucible != null)
                pourCrucible.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(0f, 72f, _displayedPourTilt));
            if (streamObject != null)
            {
                bool visibleStream = pourRate > 0.01f;
                streamObject.SetActive(visibleStream);
                if (visibleStream)
                {
                    var streamRect = streamObject.transform as RectTransform;
                    if (streamRect != null)
                        streamRect.localScale = new Vector3(Mathf.Lerp(0.35f, 1.45f, flow01), 1f, 1f);
                }
            }

            if (pourRate > 0f && Session.fillAmount < 1.2f)
            {
                float poured = Mathf.Min(pourRate * Time.deltaTime, 1.2f - Session.fillAmount);
                Session.fillAmount += poured;
                float rateQuality = EvaluatePourRateQuality(pourRate);
                Session.pourQualityWeightedSum += rateQuality * poured;
                Session.pouredAmountForQuality += poured;
                Session.meltTemperature = Mathf.Max(200f, Session.meltTemperature - (24f + pourRate * 32f) * Time.deltaTime);
                if (Controller.Audio != null && Random.value < 0.05f) Controller.Audio.PlayWater();
            }
            UpdateFillVisual(new Color(1f, 0.6f, 0.2f, 1f));

            if (pourFinishButton != null) pourFinishButton.interactable = Session.fillAmount > 0.05f;
            if (pourStatus != null)
            {
                string flow = pourRate <= 0.01f ? "потяните вправо, чтобы наклонить тигель"
                    : pourRate <= SafePourRate ? "ровный тонкий поток"
                    : "слишком быстро — качество падает!";
                pourStatus.text = $"Форма: {Mathf.Min(1f, Session.fillAmount) * 100f:0}%  •  {flow}";
            }
            if (Session.fillAmount >= 1.2f) FinishPouring(); // auto-stop on heavy overpour
        }

        private void FinishPouring()
        {
            if (Session.currentStage != ForgeStage.Pouring) return;
            float fill = Session.fillAmount;
            float fillScore = fill <= 1f ? Mathf.Clamp01(Mathf.InverseLerp(0.4f, 1f, fill))
                                         : Mathf.Clamp01(1f - (fill - 1f) * 2.2f);
            float flowScore = Session.pouredAmountForQuality > 0.001f
                ? Session.pourQualityWeightedSum / Session.pouredAmountForQuality
                : 0f;
            Session.pourQuality = Mathf.Clamp01(fillScore * 0.35f + flowScore * 0.65f);
            if (fill < 0.6f) Session.AddDefect(DefectIds.PorousIngot);
            if (flowScore < 0.45f) Session.AddDefect(DefectIds.PorousIngot);

            if (streamObject != null) streamObject.SetActive(false);
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
            Controller.Notify("Литая заготовка готова — отнесите её к наковальне.");
            Controller.UpdateObjective();
            Controller.ClosePanel();
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
            bool melting = Session == null || stage == ForgeStage.Melting || stage == ForgeStage.None;
            bool mould = stage == ForgeStage.Pouring || stage == ForgeStage.Cooling || stage == ForgeStage.CastBlankReady;

            if (meltingGroup != null) meltingGroup.SetActive(melting);
            if (mouldGroup != null) mouldGroup.SetActive(mould);
            if (statusLabel != null) statusLabel.gameObject.SetActive(melting);

            bool otherJob = Controller.Session.HasActiveSession && (int)stage >= (int)ForgeStage.EdgeForging;
            if (startButton != null)
            {
                startButton.gameObject.SetActive(stage == ForgeStage.None);
                startButton.interactable = !Controller.Session.HasActiveSession &&
                                           Controller.Inventory.GetCount(SmithyIds.Bronze) >= BronzeCost;
            }
            if (otherJob && statusLabel != null) statusLabel.text = "Завершите текущий меч (наковальня/сборка).";

            if (streamObject != null) streamObject.SetActive(false);
            if (pourCrucible != null && stage != ForgeStage.Pouring) pourCrucible.localRotation = Quaternion.identity;
            if (castBladeObject != null) castBladeObject.SetActive(stage == ForgeStage.CastBlankReady);
            if (extractButton != null) extractButton.gameObject.SetActive(stage == ForgeStage.CastBlankReady);
            if (pourRelay != null) pourRelay.gameObject.SetActive(stage == ForgeStage.Pouring);
            if (pourFinishButton != null) pourFinishButton.gameObject.SetActive(stage == ForgeStage.Pouring);

            if (mould) UpdateFillVisual(mouldFillImage != null ? mouldFillImage.color : Color.white);
        }
    }
}
