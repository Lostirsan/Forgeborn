using System.Collections.Generic;
using ForgeGame.Data;
using ForgeGame.Items;
using ForgeGame.Smithy;
using ForgeGame.Smithy.Assembly;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Physics-based manual sword assembly. The forged blade sits in a full-screen
    /// world-space workbench (see <see cref="AssemblyPhysicsWorld"/>) with a real tang
    /// collider. The player picks up the required component (guard → handle → pommel),
    /// rotates it, and drops it from any height; it FALLS under gravity, collides with the
    /// tang and the parts below and comes to rest wherever physics leaves it. Nothing is
    /// snapped, centred or straightened — a crooked settle stays crooked and lowers the
    /// weapon's assembly quality. When a part rests on the sword it is committed and the
    /// player's real X offset + rotation are recorded into the session / snapshot.
    ///
    /// The controller keeps its original responsibility: build the final
    /// <see cref="WeaponInstance"/>, copy foundry/anvil qualities, run
    /// <see cref="WeaponStatCalculator"/>, name it, add it to inventory, show the result.
    /// The item-result preview stays a deterministic renderer — no physics there.
    /// </summary>
    public class AssemblyPanelController : SmithyPanel
    {
        [SerializeField] private TMP_Text guardLabel;
        [SerializeField] private TMP_Text handleLabel;
        [SerializeField] private TMP_Text pommelLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button assembleButton;
        [SerializeField] private Button backButton;

        [Header("World-space physics stage")]
        [SerializeField] private AssemblyPhysicsWorld physicsWorld;
        [SerializeField] private CastBladeMeshView bladeMesh; // forged blade shown on the stage (world canvas)
        [SerializeField] private AssemblyPhysicsPart guardPhys;
        [SerializeField] private AssemblyPhysicsPart handlePhys;
        [SerializeField] private AssemblyPhysicsPart pommelPhys;
        [SerializeField] private GameObject[] hideDuringAssembly; // HUD/arrows hidden for the full-screen minigame

        [Header("World stacking half-heights (restore committed parts)")]
        [SerializeField] private float guardStackHalf = 0.28f;
        [SerializeField] private float handleStackHalf = 1.0f;
        [SerializeField] private float pommelStackHalf = 0.42f;

        [Header("Quality tuning (WORLD units for X/seat, degrees for angle)")]
        [SerializeField] private float maxGoodXError = 0.08f;
        [SerializeField] private float maxAllowedXError = 1.2f;
        [SerializeField] private float maxGoodAngleError = 2f;
        [SerializeField] private float maxAllowedAngleError = 26f;
        [SerializeField] private float maxGoodSeat = 0.12f;
        [SerializeField] private float maxAllowedSeat = 1.4f;

        [Header("Parts tray (catalog) + variant sprites")]
        [SerializeField] private AssemblyPartCatalogItem[] guardItems;
        [SerializeField] private AssemblyPartCatalogItem[] handleItems;
        [SerializeField] private AssemblyPartCatalogItem[] pommelItems;
        [SerializeField] private Sprite[] guardSprites;
        [SerializeField] private Sprite[] handleSprites;
        [SerializeField] private Sprite[] pommelSprites;

        private readonly List<WeaponComponentData> _guards = new List<WeaponComponentData>();
        private readonly List<WeaponComponentData> _handles = new List<WeaponComponentData>();
        private readonly List<WeaponComponentData> _pommels = new List<WeaponComponentData>();

        private static readonly ComponentSlot[] Slots =
            { ComponentSlot.Guard, ComponentSlot.Handle, ComponentSlot.Pommel };
        private static readonly ComponentSlot None = (ComponentSlot)(-1);

        private bool _subscribed;

        private ForgeSession Session => Controller.Session.Current;
        private bool Ready => Session != null && Session.castBlade != null;
        private bool AllInstalled => Session != null && Session.guardInstalled && Session.handleInstalled && Session.pommelInstalled;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => Controller?.ClosePanel());
            if (assembleButton != null) assembleButton.onClick.AddListener(Assemble);
            if (physicsWorld != null && !_subscribed) { physicsWorld.PartSettled += OnPartSettled; _subscribed = true; }
        }

        private void OnDestroy()
        {
            if (physicsWorld != null && _subscribed) { physicsWorld.PartSettled -= OnPartSettled; _subscribed = false; }
        }

        protected override void OnOpened()
        {
            GatherComponents();
            SetHudHidden(true);
            ShowForgedBlade();
            if (physicsWorld != null) physicsWorld.SetRunning(true);

            // Restore committed parts only. NOTHING is spawned automatically — the player
            // drags parts out of the tray. RequiredSlot() only gates which tray section is live.
            foreach (var slot in Slots) SetupSlot(slot);
            RefreshUi();
        }

        protected override void OnClosed()
        {
            if (physicsWorld != null) physicsWorld.SetRunning(false);
            SetHudHidden(false);
        }

        private void SetHudHidden(bool hidden)
        {
            if (hideDuringAssembly == null) return;
            foreach (var go in hideDuringAssembly)
                if (go != null) go.SetActive(!hidden);
        }

        private void ShowForgedBlade()
        {
            if (bladeMesh == null) return;
            bladeMesh.raycastTarget = false;
            bladeMesh.color = BladeTint();
            bladeMesh.SetBlade(Ready ? Session.castBlade : null);
        }

        private Color BladeTint()
        {
            var mat = Session != null ? Controller.Database.GetMaterial(Session.selectedMaterialId) : null;
            return mat != null ? mat.VisualColor : new Color(0.82f, 0.6f, 0.34f, 1f);
        }

        private void GatherComponents()
        {
            _guards.Clear(); _handles.Clear(); _pommels.Clear();
            foreach (var c in Controller.Database.Components)
            {
                switch (c.Slot)
                {
                    case ComponentSlot.Guard: _guards.Add(c); break;
                    case ComponentSlot.Handle: _handles.Add(c); break;
                    case ComponentSlot.Pommel: _pommels.Add(c); break;
                }
            }
        }

        /// <summary>Show a committed part frozen where it was placed; hide not-yet-reached parts.</summary>
        private void SetupSlot(ComponentSlot slot)
        {
            var part = PhysFor(slot);
            if (part == null) return;

            if (Ready && Installed(slot))
            {
                part.SetSprite(SpriteFor(slot, VariantOf(slot)));
                part.SetTint(Color.white);
                part.ComponentId = IdOf(slot); part.VariantIndex = VariantOf(slot);
                float x = TangX + OffsetWorld(slot);
                float y = StackY(slot);
                part.Spawn(new Vector2(x, y), RotationOf(slot));
                part.Commit(); // frozen static, keeps its committed crookedness
            }
            else
            {
                part.gameObject.SetActive(false);
            }
        }

        // ---- Tray drag → held physics part ----

        /// <summary>Called by a tray card when the player drags it out. Spawns the matching
        /// loose part under the cursor (held). Carries the chosen variant on the part; the
        /// session variant is written only when it commits.</summary>
        public void CatalogDragBegin(ComponentSlot slot, int variant, Vector2 screenPos)
        {
            if (!Ready || Installed(slot) || slot != RequiredSlot() || physicsWorld == null) return;
            var part = PhysFor(slot);
            if (part == null || part.Committed) return;
            part.ComponentId = Pick(DataList(slot), 0);
            part.VariantIndex = variant;
            part.SetSprite(SpriteFor(slot, variant));
            part.SetTint(Color.white);
            physicsWorld.BeginHeld(part, screenPos); // replaces any loose part of this slot
        }

        // ---- Settle → commit ----

        private void OnPartSettled(AssemblyPhysicsPart part)
        {
            if (part == null || Session == null) return;
            var slot = part.Slot;
            if (slot != RequiredSlot()) return;

            float worldOffsetX = part.transform.position.x - TangX;
            float rot = NormalizeAngle(part.transform.eulerAngles.z);
            float idealY = StackY(slot);
            float seatGap = Mathf.Max(0f, part.transform.position.y - idealY); // rests higher than ideal = under-seated
            float quality = PlacementQuality(Mathf.Abs(worldOffsetX), Mathf.Abs(rot), seatGap);

            // Store normalised offset (fraction of blade length) so the snapshot's /Reference
            // recovers it, matching the old drag-based saves and the deterministic preview.
            float offsetStored = (worldOffsetX / physicsWorld.BladeWorldHeight) * WeaponVisualSnapshot.ReferenceBladeHeight;

            SetOffsetX(slot, offsetStored);
            SetRotationVal(slot, rot);
            SetInstalled(slot, true);
            SetQuality(slot, quality);
            SetVariant(slot, part.VariantIndex);                 // commit the chosen variant now
            SetId(slot, part.ComponentId ?? Pick(DataList(slot), 0));
            Controller.Session.RaiseChanged();

            part.Commit(); // freeze exactly where it landed — NOTHING is spawned next
            Controller.Audio?.PlayItemGet();
            RefreshUi();
        }

        private float PlacementQuality(float xErrorWorld, float angleError, float seatGapWorld)
        {
            float xScore = 1f - Norm(xErrorWorld, maxGoodXError, maxAllowedXError);
            float rScore = 1f - Norm(angleError, maxGoodAngleError, maxAllowedAngleError);
            float seatScore = 1f - Norm(seatGapWorld, maxGoodSeat, maxAllowedSeat);
            return Mathf.Clamp01(xScore * 0.55f + rScore * 0.30f + seatScore * 0.15f);
        }

        private static float Norm(float error, float good, float max)
            => Mathf.Clamp01((error - good) / Mathf.Max(0.001f, max - good));

        private static float NormalizeAngle(float deg)
        {
            deg %= 360f;
            if (deg > 180f) deg -= 360f;
            if (deg < -180f) deg += 360f;
            return deg;
        }

        // ---- Tray states ----

        private void RefreshCatalog()
        {
            var required = RequiredSlot();
            foreach (var slot in Slots)
            {
                var items = ItemsFor(slot);
                if (items == null) continue;
                var state = Installed(slot) ? AssemblyPartCatalogItem.ItemState.Installed
                          : (slot == required && Ready) ? AssemblyPartCatalogItem.ItemState.Available
                          : AssemblyPartCatalogItem.ItemState.Locked;
                foreach (var it in items) if (it != null) it.SetState(state);
            }
        }

        private AssemblyPartCatalogItem[] ItemsFor(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => guardItems,
            ComponentSlot.Handle => handleItems,
            ComponentSlot.Pommel => pommelItems,
            _ => null
        };

        // ---- Finish ----

        private void Assemble()
        {
            if (!Ready) { Controller.Notify("Нет клинка для сборки."); return; }
            if (!AllInstalled) { Controller.Notify("Сначала установите все детали."); return; }
            if (assembleButton != null) assembleButton.interactable = false;

            float assemblyQuality = (Session.guardAssemblyQuality +
                                     Session.handleAssemblyQuality +
                                     Session.pommelAssemblyQuality) / 3f;

            var w = WeaponInstance.CreateEmpty(Session.blueprintId, Session.selectedMaterialId);
            w.meltQuality = Session.meltQuality;
            w.pourQuality = Session.pourQuality;
            w.edgeForgeQuality = Session.edgeForgeQuality;
            w.edgeThinness = Session.edgeThinness;
            w.straightness = Session.straightness;
            w.symmetry = Session.symmetry;
            w.workHardening = Session.workHardening;
            w.overworkDamage = Session.overworkDamage;
            w.assemblyQuality = assemblyQuality;
            w.guardId = Session.guardId;
            w.handleId = Session.handleId;
            w.pommelId = Session.pommelId;
            w.guardOffsetX = Session.guardOffsetX; w.guardRotation = Session.guardRotation;
            w.handleOffsetX = Session.handleOffsetX; w.handleRotation = Session.handleRotation;
            w.pommelOffsetX = Session.pommelOffsetX; w.pommelRotation = Session.pommelRotation;
            w.defectIds = new List<string>(Session.accumulatedDefects);
            w.visual = WeaponVisualSnapshot.FromSession(Session);

            WeaponStatCalculator.Calculate(w, Controller.Database);
            w.customName = WeaponNameGenerator.Generate(w, Controller.Database);

            Controller.Inventory.AddWeapon(w);
            Controller.Session.SetStage(ForgeStage.Completed);
            Controller.Session.ClearSession();
            Controller.Audio?.PlayWeaponDone();
            Controller.UpdateObjective();
            Controller.ShowWeaponResult(w);
        }

        // ---- UI ----

        private void RefreshUi()
        {
            bool ready = Ready;
            if (bladeMesh != null) bladeMesh.enabled = ready;

            if (guardLabel != null) guardLabel.text = "Гарда" + Mark(ComponentSlot.Guard);
            if (handleLabel != null) handleLabel.text = "Рукоять" + Mark(ComponentSlot.Handle);
            if (pommelLabel != null) pommelLabel.text = "Навершие" + Mark(ComponentSlot.Pommel);

            RefreshCatalog();
            if (assembleButton != null) assembleButton.interactable = ready && AllInstalled;

            if (statusLabel != null)
            {
                if (!ready) statusLabel.text = "Нет клинка — откуйте заготовку.";
                else if (!Session.guardInstalled) statusLabel.text = "Наденьте гарду на хвостовик (ЛКМ — взять/бросить, колесо — поворот).";
                else if (!Session.handleInstalled) statusLabel.text = "Наденьте рукоять на хвостовик.";
                else if (!Session.pommelInstalled) statusLabel.text = "Наденьте навершие.";
                else statusLabel.text = "Меч собран. Завершите работу.";
            }
        }

        private string Mark(ComponentSlot slot) => Installed(slot) ? "  (установлено)" : "";

        // ---- Geometry helpers ----

        private float TangX => physicsWorld != null ? physicsWorld.TangAxisX : 0f;
        private float OffsetWorld(ComponentSlot slot)
            => (OffsetX(slot) / WeaponVisualSnapshot.ReferenceBladeHeight) * (physicsWorld != null ? physicsWorld.BladeWorldHeight : 1f);

        /// <summary>Ideal stacked Y for a slot (shoulder → guard → handle → pommel).</summary>
        private float StackY(ComponentSlot slot)
        {
            float shoulder = physicsWorld != null ? physicsWorld.ShoulderY : 0f;
            switch (slot)
            {
                case ComponentSlot.Guard: return shoulder + guardStackHalf;
                case ComponentSlot.Handle: return GuardTopY() + handleStackHalf;
                case ComponentSlot.Pommel: return HandleTopY() + pommelStackHalf;
                default: return shoulder;
            }
        }

        private float GuardTopY()
        {
            float y = (guardPhys != null && Installed(ComponentSlot.Guard)) ? guardPhys.transform.position.y
                                                                            : (physicsWorld != null ? physicsWorld.ShoulderY + guardStackHalf : guardStackHalf);
            return y + guardStackHalf;
        }

        private float HandleTopY()
        {
            float y = (handlePhys != null && Installed(ComponentSlot.Handle)) ? handlePhys.transform.position.y
                                                                              : GuardTopY() + handleStackHalf;
            return y + handleStackHalf;
        }

        // ---- Slot state ----

        private ComponentSlot RequiredSlot()
        {
            if (Session == null || !Session.guardInstalled) return ComponentSlot.Guard;
            if (!Session.handleInstalled) return ComponentSlot.Handle;
            if (!Session.pommelInstalled) return ComponentSlot.Pommel;
            return None;
        }

        private bool Installed(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => Session != null && Session.guardInstalled,
            ComponentSlot.Handle => Session != null && Session.handleInstalled,
            ComponentSlot.Pommel => Session != null && Session.pommelInstalled,
            _ => false
        };

        private void SetInstalled(ComponentSlot slot, bool v)
        {
            switch (slot)
            {
                case ComponentSlot.Guard: Session.guardInstalled = v; break;
                case ComponentSlot.Handle: Session.handleInstalled = v; break;
                case ComponentSlot.Pommel: Session.pommelInstalled = v; break;
            }
        }

        private float OffsetX(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => Session.guardOffsetX,
            ComponentSlot.Handle => Session.handleOffsetX,
            ComponentSlot.Pommel => Session.pommelOffsetX,
            _ => 0f
        };

        private void SetOffsetX(ComponentSlot slot, float v)
        {
            switch (slot)
            {
                case ComponentSlot.Guard: Session.guardOffsetX = v; break;
                case ComponentSlot.Handle: Session.handleOffsetX = v; break;
                case ComponentSlot.Pommel: Session.pommelOffsetX = v; break;
            }
        }

        private float RotationOf(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => Session.guardRotation,
            ComponentSlot.Handle => Session.handleRotation,
            ComponentSlot.Pommel => Session.pommelRotation,
            _ => 0f
        };

        private void SetRotationVal(ComponentSlot slot, float v)
        {
            switch (slot)
            {
                case ComponentSlot.Guard: Session.guardRotation = v; break;
                case ComponentSlot.Handle: Session.handleRotation = v; break;
                case ComponentSlot.Pommel: Session.pommelRotation = v; break;
            }
        }

        private void SetQuality(ComponentSlot slot, float v)
        {
            switch (slot)
            {
                case ComponentSlot.Guard: Session.guardAssemblyQuality = v; break;
                case ComponentSlot.Handle: Session.handleAssemblyQuality = v; break;
                case ComponentSlot.Pommel: Session.pommelAssemblyQuality = v; break;
            }
        }

        private void SetId(ComponentSlot slot, string id)
        {
            switch (slot)
            {
                case ComponentSlot.Guard: Session.guardId = id; break;
                case ComponentSlot.Handle: Session.handleId = id; break;
                case ComponentSlot.Pommel: Session.pommelId = id; break;
            }
        }

        private string IdOf(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => Session?.guardId,
            ComponentSlot.Handle => Session?.handleId,
            ComponentSlot.Pommel => Session?.pommelId,
            _ => null
        };

        private int VariantOf(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => Session?.guardVariant ?? 0,
            ComponentSlot.Handle => Session?.handleVariant ?? 0,
            ComponentSlot.Pommel => Session?.pommelVariant ?? 0,
            _ => 0
        };

        private void SetVariant(ComponentSlot slot, int v)
        {
            if (Session == null) return;
            switch (slot)
            {
                case ComponentSlot.Guard: Session.guardVariant = v; break;
                case ComponentSlot.Handle: Session.handleVariant = v; break;
                case ComponentSlot.Pommel: Session.pommelVariant = v; break;
            }
        }

        private Sprite[] SpritesFor(ComponentSlot slot) => (slot switch
        {
            ComponentSlot.Guard => guardSprites,
            ComponentSlot.Handle => handleSprites,
            ComponentSlot.Pommel => pommelSprites,
            _ => null
        }) ?? System.Array.Empty<Sprite>();

        private Sprite SpriteFor(ComponentSlot slot, int variant)
        {
            var arr = SpritesFor(slot);
            return arr.Length == 0 ? null : arr[variant % arr.Length];
        }

        private List<WeaponComponentData> DataList(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => _guards,
            ComponentSlot.Handle => _handles,
            ComponentSlot.Pommel => _pommels,
            _ => _guards
        };

        private AssemblyPhysicsPart PhysFor(ComponentSlot slot) => slot switch
        {
            ComponentSlot.Guard => guardPhys,
            ComponentSlot.Handle => handlePhys,
            ComponentSlot.Pommel => pommelPhys,
            _ => null
        };

        private static string Pick(List<WeaponComponentData> list, int i) =>
            list.Count == 0 ? null : list[Mathf.Clamp(i % list.Count, 0, list.Count - 1)].Id;

        private static string NameOf(List<WeaponComponentData> list, int i) =>
            list.Count == 0 ? "—" : list[Mathf.Clamp(i % list.Count, 0, list.Count - 1)].DisplayName;
    }
}
