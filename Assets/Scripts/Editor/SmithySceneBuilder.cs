using System;
using System.Collections.Generic;
using System.IO;
using ForgeGame.Audio;
using ForgeGame.Data;
using ForgeGame.Inventory;
using ForgeGame.Research;
using ForgeGame.Settings;
using ForgeGame.Smithy;
using ForgeGame.UI.Common;
using ForgeGame.UI.Smithy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using SettingsService = ForgeGame.Settings.SettingsService;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// Builds the whole Smithy scene: environment, stations, player, systems, the
    /// full UI (HUD + every station panel) with references wired, then saves it and
    /// registers it in Build Settings after MainMenu. Re-runnable with a confirm.
    /// </summary>
    public static partial class SmithySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Smithy.unity";
        private const string FontPath = "Assets/Fonts/ForgeUI Dynamic SDF.asset";
        private const string MixerPath = "Assets/Audio/MainAudioMixer.mixer";
        private const string WhitePath = "Assets/Art/Generated/white.png";
        private const string DotPath = "Assets/Art/Generated/soft_dot.png";
        private const string VignettePath = "Assets/Art/Generated/vignette.png";

        // Palette
        private static readonly Color Stone = new Color(0.20f, 0.18f, 0.17f);
        private static readonly Color StoneDark = new Color(0.13f, 0.115f, 0.105f);
        private static readonly Color Wood = new Color(0.26f, 0.18f, 0.11f);
        private static readonly Color Warm = new Color(0.95f, 0.55f, 0.22f);
        private static readonly Color PanelDim = new Color(0.02f, 0.018f, 0.016f, 0.85f);
        private static readonly Color Window = new Color(0.11f, 0.09f, 0.075f, 0.98f);
        private static readonly Color BtnNormal = new Color(0.16f, 0.13f, 0.10f, 1f);
        private static readonly Color BtnHi = new Color(0.55f, 0.32f, 0.14f, 1f);
        private static readonly Color TextLight = new Color(0.88f, 0.82f, 0.70f);
        private static readonly Color TextDim = new Color(0.62f, 0.57f, 0.49f);
        private static readonly Color Accent = new Color(0.95f, 0.60f, 0.28f);
        private static readonly Color Control = new Color(0.14f, 0.12f, 0.10f);

        private static TMP_FontAsset _font;
        private static AudioMixer _mixer;
        private static Sprite _white;
        private static Sprite _dot;
        private static Sprite _vignetteSprite;
        private static Material _spriteMat;

        // Painted Shop/Forge art layers.
        private static Sprite _artStreetBg, _artStreetMid, _artWindow, _artBeam, _artCustomer, _artForgeBg, _artForgeFg;

        // Foundry world + UI art.
        private static Sprite _fFurnace, _fAnvil, _fAssembly, _fStorage, _fDoor, _fBg, _fFg;
        private static Sprite _uiCrucible, _uiCrucibleMolten, _uiMoldClosed, _uiMoldMask, _uiMoldFill, _uiStream, _uiCastBlade, _uiHammer, _uiSparks;
        private static Sprite _uiPourCrucible, _uiBladeGrid, _uiWorkbench, _uiBladeFinal, _uiTang;
        private static Sprite[] _uiGuards, _uiHandles, _uiPommels;
        private static Texture _uiCastTexture;
        private static DefaultControls.Resources _uiRes;
        private static TMP_DefaultControls.Resources _tmpRes;
        private static SmithyDatabase _db;

        private static SmithyController _controller;
        private static InventoryService _inventory;
        private static ResearchService _research;
        private static ForgeSessionController _session;
        private static SettingsService _settings;
        private static SmithyAudio _audio;
        private static RectTransform _uiRoot;
        private static Material _particleMat;
        private static readonly List<SmithyPanel> _panels = new List<SmithyPanel>();

        // ---- Dual-view layout ----
        private const float ShopAnchorX = 0f;
        private const float ForgeAnchorX = 44f;
        private const float SeamX = 22f;
        private const float CamZ = -10f;
        private const float OrthoSize = 8f;

        private static Transform _cameraRig;
        private static Transform _mainCameraTf;
        private static Camera _mainCamera;
        private static Transform _shopAnchor;
        private static Transform _forgeAnchor;
        private static GameObject _shopRoot;
        private static GameObject _forgeRoot;
        private static Transform _seamBeam;

        private static SmithyViewController _viewController;
        private static StationSelectionController _stationSelector;
        private static ForgeGame.Smithy.Shop.ShopCustomerPreviewController _customerPreview;
        private static ForgeGame.Smithy.Shop.CustomerView _customerView;
        private static Transform _customerEntry, _customerTalk, _customerExit;

        // World-space physics assembly stage (built in the .UI partial).
        private static ForgeGame.UI.Smithy.AssemblyPhysicsWorld _asmWorld;
        private static ForgeGame.UI.Smithy.AssemblyPhysicsPart _asmGuard, _asmHandle, _asmPommel;

        private static readonly List<WorkstationInteractable> _stations = new List<WorkstationInteractable>();
        private static readonly List<(Transform layer, float multiplier)> _parallax = new List<(Transform, float)>();

        // UI view refs (populated in the .UI partial)
        private static CanvasGroup _shopHud, _forgeHud, _vignette, _transitionBlocker;
        private static Button _shopArrow, _forgeArrow;

        [MenuItem("Tools/Forge Game/Build Smithy Scene")]
        public static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Rebuild Smithy",
                    $"Сцена {ScenePath} уже существует. Перезаписать?", "Перезаписать", "Отмена"))
                return;
            Build();
            EditorUtility.DisplayDialog("Forge Game",
                "Сцена кузницы собрана и добавлена в Build Settings после MainMenu.", "OK");
        }

        public static void Build()
        {
            EnsureAssets();
            _panels.Clear();
            _stations.Clear();
            _parallax.Clear();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCameraRig();
            BuildShopView();
            BuildForgeView(out var player, out var detector);
            BuildSeamBeam();
            BuildSystems(out var systems);
            BuildAssemblyPhysicsStage();
            BuildCanvasAndUi(player, detector, _mainCameraTf);

            WireController(player, detector, _mainCameraTf);
            WireViewController();
            WireStationSelector();
            WireCustomerPreview();

            // Panels saved inactive; SmithyController re-hides them on Start too.
            foreach (var p in _panels) p.gameObject.SetActive(false);
            // Forge starts hidden (Shop is the initial view); SmithyViewController re-applies on Start.
            if (_forgeRoot != null) _forgeRoot.SetActive(false);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettingsAfterMainMenu();
            PointMainMenuAtSmithy();
            AssetDatabase.SaveAssets();

            Debug.Log("[SmithySceneBuilder] Dual-view Smithy scene built at " + ScenePath);
        }

        // =====================================================================
        //  Assets
        // =====================================================================

        private static void EnsureAssets()
        {
            _db = AssetDatabase.LoadAssetAtPath<SmithyDatabase>("Assets/Data/Smithy/SmithyDatabase.asset");
            if (_db == null) _db = TestDataBuilder.BuildAll();

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            _mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            _white = LoadOrMakeWhite();
            _dot = AssetDatabase.LoadAssetAtPath<Sprite>(DotPath) ?? _white;
            _vignetteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);

            // Painted layered art for the Shop (and Forge backdrops).
            ShopArtGenerator.EnsureAll();
            _artStreetBg = AssetDatabase.LoadAssetAtPath<Sprite>(ShopArtGenerator.StreetBackgroundPath);
            _artStreetMid = AssetDatabase.LoadAssetAtPath<Sprite>(ShopArtGenerator.StreetMidgroundPath);
            _artWindow = AssetDatabase.LoadAssetAtPath<Sprite>(ShopArtGenerator.WindowForegroundPath);
            _artBeam = AssetDatabase.LoadAssetAtPath<Sprite>(ShopArtGenerator.TransitionBeamPath);
            _artCustomer = AssetDatabase.LoadAssetAtPath<Sprite>(ShopArtGenerator.CustomerPath);
            _artForgeBg = AssetDatabase.LoadAssetAtPath<Sprite>(ShopArtGenerator.ForgeBackgroundPath);
            _artForgeFg = AssetDatabase.LoadAssetAtPath<Sprite>(ShopArtGenerator.ForgeForegroundPath);

            // Painted foundry art (new bronze-casting station + panels).
            FoundryArtGenerator.EnsureAll();
            _fFurnace = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Furnace);
            _fAnvil = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Anvil);
            _fAssembly = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.AssemblyTable);
            _fStorage = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Storage);
            _fDoor = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Door);
            _fBg = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Background);
            _fFg = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Foreground);
            _uiCrucible = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Crucible);
            _uiCrucibleMolten = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.CrucibleMolten);
            _uiMoldClosed = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.SwordMoldClosed);
            _uiMoldMask = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.MoldFillMask);
            _uiMoldFill = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.MoldFill);
            _uiStream = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.BronzeStream);
            _uiCastBlade = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.CastBladeRaw);
            _uiHammer = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Hammer);
            _uiSparks = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Sparks);
            _uiCastTexture = AssetDatabase.LoadAssetAtPath<Texture>(FoundryArtGenerator.CastBladeTexture);

            _uiPourCrucible = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.PourCrucible);
            _uiBladeGrid = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.BladeGrid);
            _uiWorkbench = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.WorkbenchPanel);
            _uiBladeFinal = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.BladeFinal);
            _uiTang = AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.Tang);
            _uiGuards = new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.GuardBasic),
                AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.GuardOrnate),
            };
            _uiHandles = new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.HandleBasic),
                AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.HandleWrapped),
            };
            _uiPommels = new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.PommelBasic),
                AssetDatabase.LoadAssetAtPath<Sprite>(FoundryArtGenerator.PommelRound),
            };

            // Materials MUST be saved as assets: a runtime `new Material` assigned to a
            // renderer becomes a broken reference once the scene is saved and reloaded,
            // which shows up as the pink error material (particles especially). Persist
            // them so the built scene always resolves a real material.
            _particleMat = LoadOrCreateMaterial("Assets/Art/Generated/ForgeParticle.mat", "Sprites/Default",
                _dot != null ? _dot.texture : null);
            // Unlit material so world sprites are not blacked out by the absence of URP 2D lights.
            _spriteMat = LoadOrCreateMaterial("Assets/Art/Generated/ForgeSpriteUnlit.mat", "Sprites/Default", null);

            BuildResources();
        }

        /// <summary>
        /// Loads (or creates) a persisted <see cref="Material"/> asset. Runtime-only
        /// materials break on scene reload and render magenta, so every renderer must
        /// reference a saved asset instead.
        /// </summary>
        private static Material LoadOrCreateMaterial(string path, string shaderName, Texture mainTex)
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Generated");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader == null || mat.shader.name != shaderName)
            {
                mat.shader = Shader.Find(shaderName);
            }
            if (mainTex != null) mat.mainTexture = mainTex;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Sprite LoadOrMakeWhite()
        {
            if (!File.Exists(WhitePath))
            {
                EnsureFolder("Assets/Art");
                EnsureFolder("Assets/Art/Generated");
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                tex.SetPixels(px); tex.Apply();
                File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(WhitePath);
            }

            var imp = (TextureImporter)AssetImporter.GetAtPath(WhitePath);
            if (imp != null)
            {
                // 4 px sprite at 4 px/unit = 1x1 world unit, so localScale maps to world size directly.
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 4f;
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
        }

        private static void BuildResources()
        {
            Sprite Std(string p) => AssetDatabase.GetBuiltinExtraResource<Sprite>(p);
            _uiRes = new DefaultControls.Resources
            {
                standard = Std("UI/Skin/UISprite.psd"),
                background = Std("UI/Skin/Background.psd"),
                inputField = Std("UI/Skin/InputFieldBackground.psd"),
                knob = Std("UI/Skin/Knob.psd"),
                checkmark = Std("UI/Skin/Checkmark.psd"),
                dropdown = Std("UI/Skin/DropdownArrow.psd"),
                mask = Std("UI/Skin/UIMask.psd")
            };
            _tmpRes = new TMP_DefaultControls.Resources
            {
                standard = Std("UI/Skin/UISprite.psd"),
                background = Std("UI/Skin/Background.psd"),
                inputField = Std("UI/Skin/InputFieldBackground.psd"),
                knob = Std("UI/Skin/Knob.psd"),
                checkmark = Std("UI/Skin/Checkmark.psd"),
                dropdown = Std("UI/Skin/DropdownArrow.psd"),
                mask = Std("UI/Skin/UIMask.psd")
            };
        }

        // =====================================================================
        //  Camera + environment
        // =====================================================================

        // Sorting order bands (single Default sorting layer, gaps per logical layer).
        private const int SortStreetBg = -100;
        private const int SortStreetMid = -70;
        private const int SortCustomerShadow = -55;
        private const int SortCustomer = -45;
        private const int SortForgeBg = -100;
        private const int SortForgeStations = 0;
        private const int SortForeground = 100;
        private const int SortSeam = 200;
        private static int Order(int band, int sub) => band + sub;

        private static void BuildCameraRig()
        {
            var rig = new GameObject("CameraRig");
            rig.transform.position = new Vector3(ShopAnchorX, 0, CamZ);
            _cameraRig = rig.transform;

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = Vector3.zero;
            _mainCamera = camGo.GetComponent<Camera>();
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = OrthoSize;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0.05f, 0.045f, 0.04f);
            camGo.AddComponent<AudioListener>();
            _mainCameraTf = camGo.transform; // shake writes localPosition here (base = zero)

            _shopAnchor = new GameObject("ShopViewAnchor").transform;
            _shopAnchor.position = new Vector3(ShopAnchorX, 0, CamZ);
            _forgeAnchor = new GameObject("ForgeViewAnchor").transform;
            _forgeAnchor.position = new Vector3(ForgeAnchorX, 0, CamZ);
        }

        // ---- Shop view ----

        private static void BuildShopView()
        {
            _shopRoot = new GameObject("ShopViewRoot");
            var r = _shopRoot.transform;

            // StreetBackground (painted skyline) — 0.25. Masked so the outside world
            // is only ever seen through the window opening (never past the frame).
            var bg = MakeLayer("StreetBackground", r, new Vector3(ShopAnchorX, 1, 12));
            SetMasked(AddArtSprite("Street", bg, Vector3.zero, _artStreetBg, Order(SortStreetBg, 0), new Color(0.42f, 0.46f, 0.55f), new Vector2(64, 28)));
            AddParallax(bg, 0.25f);

            // StreetMidground (painted props: lamp, sign, barrels, cart, fence) — 0.55
            var mid = MakeLayer("StreetMidground", r, new Vector3(ShopAnchorX, -0.5f, 10));
            SetMasked(AddArtSprite("Props", mid, Vector3.zero, _artStreetMid, Order(SortStreetMid, 0), new Color(0.20f, 0.18f, 0.16f, 0.9f), new Vector2(52, 16)));
            AddParallax(mid, 0.55f);

            // CustomerLayer (points + test customer) — 0.9
            var cust = MakeLayer("CustomerLayer", r, new Vector3(ShopAnchorX, -3.2f, 8));
            _customerEntry = MakePoint("CustomerEntryPointLeft", cust, new Vector3(-16, 0, 0));
            _customerTalk = MakePoint("CustomerTalkPoint", cust, new Vector3(0, 0, 0));
            _customerExit = MakePoint("CustomerExitPointLeft", cust, new Vector3(-21, 0, 0));
            MakePoint("CustomerExitPointRight", cust, new Vector3(21, 0, 0));
            MakePoint("CustomerQueuePoint1", cust, new Vector3(-4, 0, 0));
            MakePoint("CustomerQueuePoint2", cust, new Vector3(-7.5f, 0, 0));
            BuildCustomer(cust);
            AddParallax(cust, 0.9f);

            // ShopWindowForeground (painted window frame with transparent opening) — 1.25
            var fg = MakeLayer("ShopWindowForeground", r, new Vector3(ShopAnchorX, 0, 2));
            AddArtSprite("WindowFrame", fg, Vector3.zero, _artWindow, Order(SortForeground, 2), new Color(0.18f, 0.13f, 0.09f), new Vector2(42, 24));
            // The mask lives on the same (foreground) layer so it always tracks the
            // opening; the masked street/customer only render inside it.
            BuildWindowMask(fg);
            AddParallax(fg, 1.25f);
        }

        /// <summary>Marks a renderer so it only draws inside the window mask.</summary>
        private static SpriteRenderer SetMasked(SpriteRenderer sr)
        {
            if (sr != null) sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return sr;
        }

        /// <summary>
        /// A rectangular <see cref="SpriteMask"/> matching the transparent opening of
        /// Shop_WindowForeground (see <see cref="ShopArtGenerator"/>: opening 24–76% X,
        /// 30–80% Y of the 42×24 sprite). Kept slightly inside the opening so the frame
        /// overlaps its edges cleanly.
        /// </summary>
        private static void BuildWindowMask(Transform windowLayer)
        {
            var go = new GameObject("WindowMask");
            go.transform.SetParent(windowLayer, false);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0.05f);
            go.transform.localScale = new Vector3(21f, 11f, 1f);
            var mask = go.AddComponent<SpriteMask>();
            mask.sprite = _white;
        }

        private static void BuildCustomer(Transform parent)
        {
            var go = new GameObject("Customer");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(-16, 0, 0); // starts at entry point

            var shadow = AddSprite("Shadow", go.transform, new Vector3(0, 0.1f, 0.2f), new Vector2(2.6f, 0.7f), new Color(0, 0, 0, 0.45f), Order(SortCustomerShadow, 0));
            shadow.sprite = _dot;
            SetMasked(shadow); // the customer is outside, seen only through the window

            var body = new GameObject("Body").transform;
            body.SetParent(go.transform, false);
            body.localPosition = new Vector3(0, 2.2f, 0); // centred so the feet sit on the shadow
            SetMasked(AddArtSprite("Silhouette", body, Vector3.zero, _artCustomer, Order(SortCustomer, 0), new Color(0.12f, 0.11f, 0.15f), new Vector2(2.4f, 4.2f)));

            _customerView = go.AddComponent<ForgeGame.Smithy.Shop.CustomerView>();
            WireComponent(_customerView, so =>
            {
                SetRef(so, "body", body);
                SetRef(so, "shadow", shadow.transform);
            });
        }

        // ---- Forge view ----

        private static void BuildForgeView(out SmithyPlayerController player, out InteractionDetector detector)
        {
            _forgeRoot = new GameObject("ForgeViewRoot");
            var r = _forgeRoot.transform;

            // ForgeBackground (painted foundry interior) — 0.3. Dimmed so the lit
            // stations in front sit brighter than the room behind them.
            var bg = MakeLayer("ForgeBackground", r, new Vector3(ForgeAnchorX, 2, 12));
            var interior = AddArtSprite("Interior", bg, Vector3.zero, _fBg, Order(SortForgeBg, 0), StoneDark, new Vector2(50, 26));
            if (interior != null) interior.color = new Color(0.62f, 0.6f, 0.6f, 1f);
            AddGlowColored(bg, new Vector3(-10.5f, -1.2f, -0.3f), 15f, new Color(Warm.r, Warm.g, Warm.b, 0.5f), Order(SortForgeBg, 4));
            AddParallax(bg, 0.3f);

            // ForgeStations: Foundry (left) · Anvil (centre, focus) · Assembly (right) · Storage · doors — 0.9
            // Ambient glows give each working zone its own light: Foundry = strong orange,
            // Anvil = cool steel highlight, Assembly = warm amber lamp.
            var st = MakeLayer("ForgeStations", r, new Vector3(ForgeAnchorX, 0, 6));
            AddStation(st, SmithyStation.MainMenuDoor, "Меню / выход", new Vector3(-19f, -1.4f, 0), _fDoor, new Vector2(3.4f, 5.1f), new Color(0.28f, 0.22f, 0.16f));
            AddStation(st, SmithyStation.Storage, "Хранилище", new Vector3(-15f, -2.7f, 0), _fStorage, new Vector2(3.8f, 4.2f), new Color(0.5f, 0.42f, 0.32f),
                new Color(0.9f, 0.6f, 0.3f, 0.16f), 1.7f);
            AddStation(st, SmithyStation.Foundry, "Плавильня", new Vector3(-10f, -1.7f, 0), _fFurnace, new Vector2(6f, 7.7f), new Color(0.6f, 0.32f, 0.2f),
                new Color(1f, 0.55f, 0.22f, 0.5f), 2.1f);
            AddStation(st, SmithyStation.Anvil, "Наковальня", new Vector3(-1f, -3.2f, 0), _fAnvil, new Vector2(5.6f, 4.2f), new Color(0.4f, 0.4f, 0.44f),
                new Color(0.65f, 0.75f, 0.95f, 0.26f), 2.0f);
            AddStation(st, SmithyStation.AssemblyTable, "Сборочный стол", new Vector3(8f, -3f, 0), _fAssembly, new Vector2(6.6f, 4.2f), new Color(0.5f, 0.4f, 0.26f),
                new Color(1f, 0.72f, 0.38f, 0.34f), 1.9f);
            AddStation(st, SmithyStation.DungeonDoor, "В подземелье", new Vector3(15.5f, -1.4f, 0), _fDoor, new Vector2(3.4f, 5.1f), new Color(0.24f, 0.2f, 0.3f),
                new Color(0.4f, 0.45f, 0.7f, 0.14f), 1.6f);

            // Warm light pooling on the floor in front of the furnace and the bench.
            AddFloorLight(st, new Vector3(-10f, -4.4f, 0.2f), 9f, new Color(1f, 0.5f, 0.2f, 0.30f));
            AddFloorLight(st, new Vector3(8f, -4.7f, 0.2f), 6.5f, new Color(1f, 0.66f, 0.32f, 0.16f));

            BuildPlayer(st, out player, out detector);
            AddParallax(st, 0.9f);

            // ForgeForeground (painted front beam + chains) — 1.2
            var fg = MakeLayer("ForgeForeground", r, new Vector3(ForgeAnchorX, -5, 2));
            AddArtSprite("Front", fg, Vector3.zero, _fFg, Order(SortForeground, 0), new Color(0.08f, 0.06f, 0.05f), new Vector2(50, 8));
            AddParallax(fg, 1.2f);

            // Foundry heat effects (world coords, near the furnace on the left).
            var fx = new GameObject("ForgeEffects").transform;
            fx.SetParent(r, false);
            AddParticles("Sparks", fx, new Vector3(ForgeAnchorX - 10, -0.5f, 0), 7f, 1.4f, 2.2f, 0.12f, 0.3f, new Color(1f, 0.55f, 0.2f, 0.9f), 0.6f);
            AddParticles("Smoke", fx, new Vector3(ForgeAnchorX - 10, 1.5f, 0), 4f, 6f, 0.5f, 2.4f, -0.02f, new Color(0.3f, 0.28f, 0.28f, 0.14f), 1.6f);
        }

        private static void BuildPlayer(Transform parent, out SmithyPlayerController player, out InteractionDetector detector)
        {
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(parent, false);
            playerGo.transform.localPosition = new Vector3(-3f, -3.6f, -1f); // behind/beside the anvil

            var visual = new GameObject("Visual").transform;
            visual.SetParent(playerGo.transform, false);
            visual.localPosition = new Vector3(0, 1.1f, 0);
            AddSprite("Body", visual, new Vector3(0, 0, 0), new Vector2(1.1f, 2.2f), new Color(0.30f, 0.24f, 0.2f), Order(SortForgeStations, 4));
            AddSprite("Head", visual, new Vector3(0, 1.4f, 0), new Vector2(0.8f, 0.8f), new Color(0.62f, 0.48f, 0.36f), Order(SortForgeStations, 5));
            AddSprite("Apron", visual, new Vector3(0, -0.4f, 0), new Vector2(1.0f, 1.2f), new Color(0.45f, 0.25f, 0.14f), Order(SortForgeStations, 5));

            player = playerGo.AddComponent<SmithyPlayerController>();
            detector = playerGo.AddComponent<InteractionDetector>();
            detector.enabled = false; // proximity path unused; stations are selected directly

            WireComponent(player, so =>
            {
                SetRef(so, "visual", visual);
                SetBool(so, "freeMovementEnabled", false);
                SetFloat(so, "minX", ForgeAnchorX - 6f);
                SetFloat(so, "maxX", ForgeAnchorX + 6f);
            });
        }

        private static void AddStation(Transform parent, SmithyStation station, string prompt, Vector3 localPos,
            Sprite bodySprite, Vector2 size, Color fallback, Color ambientGlow = default, float glowScale = 0f)
        {
            var go = new GameObject("Station_" + station);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            // Always-on ambient light so working stations read against the dark room
            // even without the selection highlight (behind the body).
            if (glowScale > 0f && ambientGlow.a > 0f)
            {
                var amb = AddSprite("AmbientGlow", go.transform, new Vector3(0, size.y * 0.05f, 0.1f),
                    new Vector2(size.x * glowScale, size.y * glowScale * 0.9f), ambientGlow, Order(SortForgeBg, 6));
                amb.sprite = _dot;
            }

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            AddArtSprite("Body", visual, Vector3.zero, bodySprite, Order(SortForgeStations, 0), fallback, size);

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var gs = AddSprite("GlowSprite", glow.transform, new Vector3(0, size.y * 0.1f, 0),
                new Vector2(size.x * 1.7f, size.y * 1.5f), new Color(Warm.r, Warm.g, Warm.b, 0.5f), Order(SortForgeStations, -1));
            gs.sprite = _dot;
            glow.SetActive(false);

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(size.x * 1.05f, size.y * 1.1f);

            var ws = go.AddComponent<WorkstationInteractable>();
            WireComponent(ws, so =>
            {
                SetEnum(so, "station", (int)station);
                SetString(so, "promptText", prompt);
                SetBool(so, "available", true);
                SetRef(so, "highlightTarget", visual);
                SetRef(so, "glow", glow);
            });
            _stations.Add(ws);
        }

        private static void BuildSeamBeam()
        {
            var seam = new GameObject("SeamLayer").transform;
            var beam = new GameObject("TransitionBeam").transform;
            beam.SetParent(seam, false);
            beam.position = new Vector3(SeamX, 0, 1);
            AddArtSprite("Beam", beam, Vector3.zero, _artBeam, Order(SortSeam, 0), new Color(0.08f, 0.06f, 0.05f), new Vector2(3.6f, 28f));
            _seamBeam = beam;
        }

        // ---- Small world helpers ----

        private static Transform MakeLayer(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        private static Transform MakePoint(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        private static void AddParallax(Transform layer, float multiplier) => _parallax.Add((layer, multiplier));

        private static void AddGlowColored(Transform parent, Vector3 localPos, float size, Color color, int order)
        {
            var sr = AddSprite("Glow", parent, localPos, new Vector2(size, size), color, order);
            sr.sprite = _dot;
        }

        /// <summary>A wide, flattened warm pool on the floor (soft dot squashed vertically).</summary>
        private static void AddFloorLight(Transform parent, Vector3 localPos, float width, Color color)
        {
            var sr = AddSprite("FloorLight", parent, localPos, new Vector2(width, width * 0.36f), color, Order(SortForgeBg, 5));
            sr.sprite = _dot;
        }

        private static SpriteRenderer AddSprite(string name, Transform parent, Vector3 localPos, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _white;
            sr.color = color;
            sr.sortingOrder = order;
            if (_spriteMat != null) sr.sharedMaterial = _spriteMat;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return sr;
        }

        /// <summary>
        /// Places a painted art sprite at its native world size (PPU set so
        /// localScale = 1 maps to the intended units). Falls back to a flat colour
        /// block only if the sprite is missing, so the scene never errors.
        /// </summary>
        private static SpriteRenderer AddArtSprite(string name, Transform parent, Vector3 localPos, Sprite sprite,
            int order, Color fallbackColor, Vector2 fallbackSize)
        {
            if (sprite == null)
                return AddSprite(name, parent, localPos, fallbackSize, fallbackColor, order);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (_spriteMat != null) sr.sharedMaterial = _spriteMat;
            return sr;
        }

        private static void AddParticles(string name, Transform parent, Vector3 pos, float rate, float life,
            float speed, float size, float gravity, Color color, float spreadX)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = life; main.startSpeed = speed; main.startSize = size;
            main.startColor = color; main.gravityModifier = gravity; main.maxParticles = 300;
            var em = ps.emission; em.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(spreadX, 0.2f, 0.1f);
            var vol = ps.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.Local;
            vol.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vol.y = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            vol.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = _particleMat; r.sortingOrder = -8;
        }

        // =====================================================================
        //  Systems
        // =====================================================================

        private static void BuildSystems(out Transform systems)
        {
            systems = new GameObject("Systems").transform;

            _controller = new GameObject("SmithyController").AddComponent<SmithyController>();
            _controller.transform.SetParent(systems, false);
            _inventory = new GameObject("InventoryService").AddComponent<InventoryService>();
            _inventory.transform.SetParent(systems, false);
            _research = new GameObject("ResearchService").AddComponent<ResearchService>();
            _research.transform.SetParent(systems, false);
            _session = new GameObject("ForgeSessionController").AddComponent<ForgeSessionController>();
            _session.transform.SetParent(systems, false);
            _settings = new GameObject("SettingsService").AddComponent<SettingsService>();
            _settings.transform.SetParent(systems, false);

            var audioGo = new GameObject("SmithyAudio");
            audioGo.transform.SetParent(systems, false);
            _audio = audioGo.AddComponent<SmithyAudio>();
            var loop = new GameObject("Loop", typeof(AudioSource)); loop.transform.SetParent(audioGo.transform, false);
            var sfx = new GameObject("Sfx", typeof(AudioSource)); sfx.transform.SetParent(audioGo.transform, false);
            var street = new GameObject("StreetAmbience", typeof(AudioSource)); street.transform.SetParent(audioGo.transform, false);
            var forgeAmb = new GameObject("ForgeAmbience", typeof(AudioSource)); forgeAmb.transform.SetParent(audioGo.transform, false);
            var sfxGroup = FindGroup("SFX");
            WireComponent(_audio, so =>
            {
                SetRef(so, "loopSource", loop.GetComponent<AudioSource>());
                SetRef(so, "sfxSource", sfx.GetComponent<AudioSource>());
                SetRef(so, "streetSource", street.GetComponent<AudioSource>());
                SetRef(so, "forgeSource", forgeAmb.GetComponent<AudioSource>());
                SetRef(so, "sfxGroup", sfxGroup);
            });

            // Dual-view systems.
            _viewController = new GameObject("SmithyViewController").AddComponent<SmithyViewController>();
            _viewController.transform.SetParent(systems, false);
            _stationSelector = new GameObject("StationSelection").AddComponent<StationSelectionController>();
            _stationSelector.transform.SetParent(systems, false);
            _customerPreview = new GameObject("ShopCustomerPreview").AddComponent<ForgeGame.Smithy.Shop.ShopCustomerPreviewController>();
            _customerPreview.transform.SetParent(systems, false);

            WireComponent(_inventory, so => SetRef(so, "database", _db));
            WireComponent(_settings, so => SetRef(so, "audioMixer", _mixer));
        }

        private static AudioMixerGroup FindGroup(string name)
        {
            if (_mixer == null) return null;
            var g = _mixer.FindMatchingGroups(name);
            return g != null && g.Length > 0 ? g[0] : null;
        }

        // =====================================================================
        //  Canvas + UI  (continued in second partial file section below)
        // =====================================================================
    }
}
