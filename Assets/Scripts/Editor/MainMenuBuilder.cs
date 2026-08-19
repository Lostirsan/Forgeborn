using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ForgeGame.Audio;
using ForgeGame.Localization;
using ForgeGame.Settings;
using ForgeGame.UI.Common;
using ForgeGame.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SettingsService = ForgeGame.Settings.SettingsService;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// One-shot generator that builds the entire FORGEBORN main-menu scene:
    /// camera, atmospheric background, UI, panels, wiring, and Build Settings
    /// registration. Run it from the menu <b>Tools ▸ Forge Game ▸ Build Main Menu</b>.
    /// It is safe to re-run: it recreates the scene from scratch and regenerates
    /// any missing support assets (font, audio mixer, placeholder art).
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontPath = "Assets/Fonts/ForgeUI Dynamic SDF.asset";
        private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        private const string MixerPath = "Assets/Audio/MainAudioMixer.mixer";
        private const string ArtDir = "Assets/Art/Generated";
        private const string StartSceneName = "Forge";

        // ---- Placeholder palette (warm forge on near-black stone) ----
        private static readonly Color CameraBg = new Color(0.035f, 0.030f, 0.027f, 1f);
        private static readonly Color PanelDark = new Color(0.07f, 0.06f, 0.05f, 0.74f);
        private static readonly Color WindowDark = new Color(0.09f, 0.075f, 0.062f, 0.96f);
        private static readonly Color BorderCol = new Color(0.42f, 0.26f, 0.15f, 0.7f);
        private static readonly Color TextLight = new Color(0.87f, 0.81f, 0.69f, 1f);
        private static readonly Color TextDim = new Color(0.62f, 0.57f, 0.49f, 1f);
        private static readonly Color Accent = new Color(0.95f, 0.55f, 0.22f, 1f);
        private static readonly Color TitleCol = new Color(0.95f, 0.62f, 0.29f, 1f);
        private static readonly Color Anvil = new Color(0.05f, 0.045f, 0.05f, 1f);
        private static readonly Color ControlBg = new Color(0.14f, 0.12f, 0.10f, 1f);

        // Cached during a build.
        private static TMP_FontAsset _font;
        private static AudioMixer _mixer;
        private static DefaultControls.Resources _uiRes;
        private static TMP_DefaultControls.Resources _tmpRes;
        private static Sprite _frame, _glow, _vignette, _vgrad, _dot;
        private static AudioManager _audio;

        [MenuItem("Tools/Forge Game/Build Main Menu")]
        public static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Rebuild Main Menu",
                    $"A scene already exists at {ScenePath}.\nRebuilding will overwrite it. Continue?",
                    "Overwrite", "Cancel"))
            {
                return;
            }

            Build();
            EditorUtility.DisplayDialog("Forge Game",
                "Main menu built and added to Build Settings (index 0).", "OK");
        }

        /// <summary>Builds the scene without any dialogs (used by automation).</summary>
        public static void Build()
        {
            EnsureSupportAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment();

            // --- Canvas + scaler ---
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRt = (RectTransform)canvasGo.transform;

            BuildEventSystem();

            // --- Systems (created early, wired at the end) ---
            var systems = new GameObject("Systems").transform;
            var settingsService = new GameObject("SettingsService").AddComponent<SettingsService>();
            settingsService.transform.SetParent(systems, false);
            _audio = BuildAudioManager(systems);
            var menu = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            menu.transform.SetParent(systems, false);

            // --- Background atmosphere (UI layers) ---
            BuildBackground(canvasRt);

            // --- Safe area (scaled by the UI-scale setting) ---
            var safeArea = NewRect("SafeArea", canvasRt);
            Stretch(safeArea, 100, 60, 100, 60);
            var mainContentGroup = safeArea.gameObject.AddComponent<CanvasGroup>();

            BuildBranding(safeArea);
            var buttons = BuildMainButtons(safeArea);
            var footer = BuildFooter(safeArea);

            // --- Overlays (siblings of SafeArea so they cover the whole screen) ---
            var settings = BuildSettingsPanel(canvasRt, settingsService, out var settingsCtrl, out var settingsBack);
            var credits = BuildCreditsPanel(canvasRt, out var creditsBack);
            var load = BuildLoadPanel(canvasRt);
            var modal = BuildConfirmationModal(canvasRt);
            var loadingBlocker = BuildLoadingBlocker(canvasRt);
            var fader = BuildScreenFader(canvasRt);

            // --- Wire everything ---
            WireComponent(settingsService, so =>
            {
                SetRef(so, "audioMixer", _mixer);
                SetRef(so, "uiScaleRoot", safeArea);
            });

            WireComponent(menu, so =>
            {
                SetRef(so, "fader", fader);
                SetRef(so, "audioManager", _audio);
                SetStr(so, "forgeSceneName", StartSceneName);
                SetRef(so, "continueButton", buttons.Continue);
                SetRef(so, "loadButton", buttons.Load);
                SetRef(so, "newGameButton", buttons.NewGame);
                SetRef(so, "settingsButton", buttons.Settings);
                SetRef(so, "creditsButton", buttons.Credits);
                SetRef(so, "quitButton", buttons.Quit);
                SetRef(so, "settingsPanelRoot", settings.gameObject);
                SetRef(so, "settingsPanel", settingsCtrl);
                SetRef(so, "settingsBackButton", settingsBack);
                SetRef(so, "creditsPanelRoot", credits.gameObject);
                SetRef(so, "creditsBackButton", creditsBack);
                SetRef(so, "loadPanelRoot", load.panel.gameObject);
                SetRef(so, "loadBackButton", load.back);
                SetObjArray(so, "slotNameLabels", load.names);
                SetObjArray(so, "slotDateLabels", load.dates);
                SetObjArray(so, "slotLoadButtons", load.loads);
                SetObjArray(so, "slotDeleteButtons", load.deletes);
                SetRef(so, "confirmationModal", modal);
                SetRef(so, "loadingBlocker", loadingBlocker.gameObject);
                SetRef(so, "versionText", footer.Version);
                SetRef(so, "mainContentGroup", mainContentGroup);
            });

            // Panels start hidden; runtime code also enforces this on Start.
            settings.gameObject.SetActive(false);
            credits.gameObject.SetActive(false);
            load.panel.gameObject.SetActive(false);
            loadingBlocker.gameObject.SetActive(false);

            // --- Save + register ---
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettingsFirst(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MainMenuBuilder] Main menu generated at {ScenePath} and set as Build Settings scene 0. " +
                      $"Start scene name is '{StartSceneName}'.");
        }

        // =====================================================================
        //  Support assets
        // =====================================================================

        private static void EnsureSupportAssets()
        {
            EnsureTmpEssentials();
            _font = EnsureFont();
            _mixer = EnsureMixer();
            EnsureArt();
            BuildControlResources();
        }

        private static void EnsureTmpEssentials()
        {
            if (!Directory.Exists(Path.Combine(Application.dataPath, "TextMesh Pro")))
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                AssetDatabase.Refresh();
            }
        }

        private static TMP_FontAsset EnsureFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/Fonts");
            var src = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (src == null)
            {
                Debug.LogWarning($"[MainMenuBuilder] Source font '{SourceFontPath}' not found; " +
                                 "falling back to TMP default (Cyrillic may not render).");
                return TMP_Settings.defaultFontAsset;
            }

            var fa = TMP_FontAsset.CreateFontAsset(src, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            fa.name = "ForgeUI Dynamic SDF";
            AssetDatabase.CreateAsset(fa, FontPath);
            if (fa.material != null) { fa.material.name = "ForgeUI Dynamic SDF Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
            if (fa.atlasTextures != null)
                foreach (var tex in fa.atlasTextures)
                    if (tex != null) { tex.name = "ForgeUI Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();

            // Make it the project default so any TMP text renders Cyrillic.
            var so = new SerializedObject(TMP_Settings.instance);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null) { prop.objectReferenceValue = fa; so.ApplyModifiedPropertiesWithoutUndo(); }
            return fa;
        }

        private static AudioMixer EnsureMixer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/Audio");
            try
            {
                var edAsm = typeof(Editor).Assembly;
                var tCtrl = edAsm.GetType("UnityEditor.Audio.AudioMixerController");
                var tGroup = edAsm.GetType("UnityEditor.Audio.AudioMixerGroupController");
                var tPath = edAsm.GetType("UnityEditor.Audio.AudioGroupParameterPath");
                const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                var controller = tCtrl.GetMethod("CreateMixerControllerAtPath").Invoke(null, new object[] { MixerPath });
                var master = tCtrl.GetProperty("masterGroup", BF).GetValue(controller);

                Func<string, object, object> mkChild = (name, parent) =>
                {
                    var g = tCtrl.GetMethod("CreateNewGroup", BF).Invoke(controller, new object[] { name, false });
                    tCtrl.GetMethod("AddChildToParent", BF).Invoke(controller, new[] { g, parent });
                    return g;
                };
                var music = mkChild("Music", master);
                var sfx = mkChild("SFX", master);
                try { tCtrl.GetMethod("SanitizeGroupViews", BF).Invoke(controller, null); } catch { /* view-only */ }

                Action<object, string> expose = (grp, pname) =>
                {
                    tGroup.GetMethod("PreallocateGUIDs", BF).Invoke(grp, null);
                    var guid = tGroup.GetMethod("GetGUIDForVolume", BF).Invoke(grp, null);
                    var ap = Activator.CreateInstance(tPath, new[] { grp, guid });
                    tCtrl.GetMethod("AddExposedParameter", BF).Invoke(controller, new[] { ap });
                    var expProp = tCtrl.GetProperty("exposedParameters", BF);
                    var arr = (Array)expProp.GetValue(controller);
                    var last = arr.GetValue(arr.Length - 1);
                    last.GetType().GetField("name").SetValue(last, pname);
                    arr.SetValue(last, arr.Length - 1);
                    expProp.SetValue(controller, arr);
                };
                expose(master, "MasterVolume");
                expose(music, "MusicVolume");
                expose(sfx, "SFXVolume");

                EditorUtility.SetDirty((UnityEngine.Object)controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(MixerPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainMenuBuilder] Could not create AudioMixer automatically: {e.Message}. " +
                               "Audio will fall back to un-mixed playback.");
            }
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }

        private static AudioMixerGroup FindGroup(string name)
        {
            if (_mixer == null) return null;
            var groups = _mixer.FindMatchingGroups(name);
            return (groups != null && groups.Length > 0) ? groups[0] : null;
        }

        // =====================================================================
        //  Placeholder art (procedural textures)
        // =====================================================================

        private static void EnsureArt()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder(ArtDir);

            _dot = MakeSprite("soft_dot", 64, 64, 0, (u, v) =>
            {
                float d = Dist(u, v);
                float a = Mathf.Clamp01(1f - Smooth(0f, 1f, d));
                return new Color(1f, 1f, 1f, a * a);
            });

            _glow = MakeSprite("radial_glow", 256, 256, 0, (u, v) =>
            {
                float d = Dist(u, v);
                float a = Mathf.Clamp01(1f - Smooth(0f, 1f, d));
                return new Color(1f, 1f, 1f, a * a);
            });

            _vignette = MakeSprite("vignette", 512, 512, 0, (u, v) =>
            {
                float d = Dist(u, v);
                float a = Smooth(0.52f, 0.98f, d) * 0.92f;
                return new Color(0.02f, 0.017f, 0.015f, a);
            });

            _vgrad = MakeSprite("vgrad", 8, 256, 0, (u, v) =>
            {
                // Opaque-ish warm dark at the bottom fading to transparent upward.
                float a = Mathf.Clamp01(1f - v) ;
                return new Color(0.10f, 0.06f, 0.04f, a * 0.8f);
            });

            _frame = MakeSprite("frame", 24, 24, 6, (u, v) =>
            {
                float bx = 3f / 24f;
                bool edge = u < bx || u > 1f - bx || v < bx || v > 1f - bx;
                return edge ? Color.white : new Color(1f, 1f, 1f, 0f);
            });
        }

        private static float Dist(float u, float v)
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            return Mathf.Sqrt(dx * dx + dy * dy) / 1.41421f;
        }

        private static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        private static Sprite MakeSprite(string name, int w, int h, int border, Func<float, float, Color> fn)
        {
            string path = $"{ArtDir}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = w == 1 ? 0.5f : x / (float)(w - 1);
                        float v = h == 1 ? 0.5f : y / (float)(h - 1);
                        px[y * w + x] = fn(u, v);
                    }
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                if (border > 0) imp.spriteBorder = new Vector4(border, border, border, border);
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // =====================================================================
        //  Environment: camera + particles
        // =====================================================================

        private static void BuildEnvironment()
        {
            var env = new GameObject("Environment").transform;

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(env, false);
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CameraBg;
            camGo.AddComponent<AudioListener>();

            var mat = new Material(Shader.Find("Sprites/Default"));
            if (_dot != null) mat.mainTexture = _dot.texture;

            BuildParticles("Smoke", env, mat, new Vector3(1.6f, -3.2f, 0f),
                rate: 6f, life: 7f, speed: 0.5f, size: 2.6f, gravity: -0.02f,
                start: new Color(0.32f, 0.30f, 0.30f, 0.16f), spreadX: 2.2f);

            BuildParticles("Sparks", env, mat, new Vector3(2.1f, -2.7f, 0f),
                rate: 14f, life: 1.6f, speed: 2.4f, size: 0.12f, gravity: 0.35f,
                start: new Color(1f, 0.55f, 0.18f, 0.9f), spreadX: 0.5f);

            BuildParticles("Embers", env, mat, new Vector3(2.0f, -2.8f, 0f),
                rate: 10f, life: 3.5f, speed: 0.9f, size: 0.18f, gravity: -0.05f,
                start: new Color(1f, 0.42f, 0.12f, 0.55f), spreadX: 1.4f);
        }

        private static void BuildParticles(string name, Transform parent, Material mat, Vector3 pos,
            float rate, float life, float speed, float size, float gravity, Color start, float spreadX)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = start;
            main.gravityModifier = gravity;
            main.maxParticles = 400;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(spreadX, 0.2f, 0.1f);

            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.Local;
            // All three axes must share the same curve mode (here: TwoConstants),
            // otherwise Unity logs "Particle Velocity curves must all be in the same mode".
            vol.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vol.y = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            vol.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.sortingOrder = -5;
        }

        // =====================================================================
        //  Background UI layers
        // =====================================================================

        private static void BuildBackground(RectTransform canvas)
        {
            var bg = NewRect("Background", canvas);
            Stretch(bg);

            var floor = NewRect("FloorGradient", bg);
            Stretch(floor);
            AddImage(floor, new Color(1f, 1f, 1f, 1f), _vgrad, false).type = Image.Type.Simple;

            var glow = NewRect("ForgeGlow", bg);
            Anchor(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(360, -120), new Vector2(900, 900));
            var glowImg = AddImage(glow, new Color(Accent.r, Accent.g, Accent.b, 0.75f), _glow, false);
            glow.gameObject.AddComponent<GlowPulse>();

            BuildAnvil(bg);

            var vign = NewRect("Vignette", bg);
            Stretch(vign);
            AddImage(vign, Color.white, _vignette, false);
        }

        private static void BuildAnvil(RectTransform bg)
        {
            var anvil = NewRect("AnvilSilhouette", bg);
            Anchor(anvil, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(340, -150), new Vector2(560, 380));

            // Helper: add a dark rectangle anchored to the anvil's bottom-centre.
            void Block(string n, float x, float y, float w, float h) =>
                AddImage(Place(NewRect(n, anvil), x, y, w, h), Anvil, null, false);

            // Wooden stand / pedestal under the anvil.
            Block("Stand", 0, 0, 150, 96);
            Block("StandTop", 0, 96, 190, 26);

            // Anvil body: wide feet → narrow waist → wide top face.
            Block("Feet", 0, 120, 250, 40);
            Block("Waist", 0, 158, 120, 66);
            Block("Table", 0, 222, 300, 30);   // step below the face
            Block("Face", 0, 250, 330, 44);    // flat top working face

            // Horn: stepped taper extending to the right at face height.
            Block("Horn1", 205, 256, 90, 34);
            Block("Horn2", 285, 260, 54, 24);
            Block("Horn3", 330, 263, 26, 14);

            // Heel: small square nub on the left of the face.
            Block("Heel", -190, 254, 40, 30);
        }

        /// <summary>Places a rect anchored to its parent's bottom-centre.</summary>
        private static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(x, y), new Vector2(w, h));
            return rt;
        }

        // =====================================================================
        //  Branding, buttons, footer
        // =====================================================================

        private static void BuildBranding(RectTransform safeArea)
        {
            var branding = NewRect("Branding", safeArea);
            Anchor(branding, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0, -10), new Vector2(900, 200));
            var vlg = branding.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 2;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var titleRt = NewRect("Title", branding);
            var title = AddText(titleRt, "FORGEBORN", 104, TitleCol, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 6;
            AddLayoutHeight(titleRt, 120);

            var subRt = NewRect("Subtitle", branding);
            Localize(AddText(subRt, "Craft. Descend. Discover.", 30, TextDim, TextAlignmentOptions.Left), "menu.tagline");
            AddLayoutHeight(subRt, 40);
        }

        private struct MainButtonRefs
        {
            public Button Continue, Load, NewGame, Settings, Credits, Quit;
        }

        private static MainButtonRefs BuildMainButtons(RectTransform safeArea)
        {
            var container = NewRect("MainButtons", safeArea);
            Anchor(container, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0, -40), new Vector2(420, 470));
            var vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.spacing = 16;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var cont = BuildMenuButton(container, "Продолжить", 420, 66, out var lCont); Localize(lCont, "menu.continue");
            var load = BuildMenuButton(container, "Загрузить", 420, 66, out var lLoad); Localize(lLoad, "menu.load");
            var ng = BuildMenuButton(container, "Новая игра", 420, 66, out var lNg); Localize(lNg, "menu.new_game");
            var set = BuildMenuButton(container, "Настройки", 420, 66, out var lSet); Localize(lSet, "menu.settings");
            var cr = BuildMenuButton(container, "Авторы", 420, 66, out var lCr); Localize(lCr, "menu.credits");
            var quit = BuildMenuButton(container, "Выход", 420, 66, out var lQuit); Localize(lQuit, "menu.quit");
            return new MainButtonRefs { Continue = cont, Load = load, NewGame = ng, Settings = set, Credits = cr, Quit = quit };
        }

        private struct FooterRefs { public TMP_Text Version; }

        private static FooterRefs BuildFooter(RectTransform safeArea)
        {
            var footer = NewRect("Footer", safeArea);
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.anchoredPosition = new Vector2(0, 0);
            footer.sizeDelta = new Vector2(0, 34);

            var versionRt = NewRect("Version", footer);
            Stretch(versionRt);
            var version = AddText(versionRt, "v0.0", 22, TextDim, TextAlignmentOptions.Left);

            var hintRt = NewRect("Hint", footer);
            Stretch(hintRt);
            AddText(hintRt, "Стрелки / WASD — выбор   •   Enter — подтвердить   •   Esc — назад",
                22, TextDim, TextAlignmentOptions.Center);

            var copyRt = NewRect("Copyright", footer);
            Stretch(copyRt);
            AddText(copyRt, "© Vilwayer Studio", 22, TextDim, TextAlignmentOptions.Right);

            return new FooterRefs { Version = version };
        }

        // =====================================================================
        //  Settings panel
        // =====================================================================

        private static RectTransform BuildSettingsPanel(RectTransform canvas, SettingsService service,
            out SettingsPanelController ctrl, out Button backButton)
        {
            var panel = NewRect("SettingsPanel", canvas);
            Stretch(panel);
            AddImage(panel, new Color(0.02f, 0.018f, 0.016f, 0.82f), null, true); // dim + block clicks
            ctrl = panel.gameObject.AddComponent<SettingsPanelController>();

            var window = NewRect("Window", panel);
            Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1180, 820));
            AddImage(window, WindowDark, null, true);
            AddFrame(window);

            var titleRt = NewRect("Title", window);
            Anchor(titleRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -46), new Vector2(1000, 60));
            Localize(AddText(titleRt, "Настройки", 46, TitleCol, TextAlignmentOptions.Center), "settings.title");

            // Tabs
            var tabs = NewRect("Tabs", window);
            Anchor(tabs, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -110), new Vector2(720, 56));
            var tabsHl = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsHl.spacing = 12; tabsHl.childAlignment = TextAnchor.MiddleCenter;
            tabsHl.childControlWidth = true; tabsHl.childControlHeight = true;
            tabsHl.childForceExpandWidth = true; tabsHl.childForceExpandHeight = true;
            var soundTab = BuildMenuButton(tabs, "Звук", 220, 52, out var lTabSound); Localize(lTabSound, "settings.tab_sound");
            var graphicsTab = BuildMenuButton(tabs, "Графика", 220, 52, out var lTabGfx); Localize(lTabGfx, "settings.tab_graphics");
            var interfaceTab = BuildMenuButton(tabs, "Интерфейс", 220, 52, out var lTabUi); Localize(lTabUi, "settings.tab_interface");

            // Sections host
            var host = NewRect("Sections", window);
            Anchor(host, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -180), new Vector2(1040, 520));

            var soundSection = BuildSection(host);
            var master = BuildSliderRow(soundSection, "Общая громкость", "settings.master");
            var music = BuildSliderRow(soundSection, "Музыка", "settings.music");
            var sfx = BuildSliderRow(soundSection, "Эффекты", "settings.effects_slider");
            var mute = BuildToggleRow(soundSection, "Отключить весь звук", "settings.mute");

            var graphicsSection = BuildSection(host);
            var windowMode = BuildDropdownRow(graphicsSection, "Режим экрана", "settings.window_mode");
            var resolution = BuildDropdownRow(graphicsSection, "Разрешение", "settings.resolution");
            var vsync = BuildToggleRow(graphicsSection, "Вертикальная синхронизация", "settings.vsync");
            var fps = BuildDropdownRow(graphicsSection, "Ограничение FPS", "settings.fps");

            var interfaceSection = BuildSection(host);
            var uiScale = BuildSliderRow(interfaceSection, "Масштаб интерфейса", "settings.ui_scale");
            var shake = BuildToggleRow(interfaceSection, "Дрожание экрана", "settings.screen_shake");
            var effects = BuildSliderRow(interfaceSection, "Интенсивность эффектов", "settings.effects_intensity");
            var language = BuildDropdownRow(interfaceSection, "Язык", "settings.language");

            // Footer buttons
            var foot = NewRect("Footer", window);
            Anchor(foot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 44), new Vector2(760, 60));
            var footHl = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footHl.spacing = 20; footHl.childAlignment = TextAnchor.MiddleCenter;
            footHl.childControlWidth = true; footHl.childControlHeight = true;
            footHl.childForceExpandWidth = true; footHl.childForceExpandHeight = true;
            var applyBtn = BuildMenuButton(foot, "Применить", 230, 58, out var lApply); Localize(lApply, "common.apply");
            var resetBtn = BuildMenuButton(foot, "Сбросить", 230, 58, out var lReset); Localize(lReset, "common.reset");
            backButton = BuildMenuButton(foot, "Назад", 230, 58, out var lBack); Localize(lBack, "common.back");

            WireComponent(ctrl, so =>
            {
                SetRef(so, "settingsService", service);
                SetRef(so, "soundSection", soundSection.gameObject);
                SetRef(so, "graphicsSection", graphicsSection.gameObject);
                SetRef(so, "interfaceSection", interfaceSection.gameObject);
                SetRef(so, "soundTabButton", soundTab);
                SetRef(so, "graphicsTabButton", graphicsTab);
                SetRef(so, "interfaceTabButton", interfaceTab);
                SetRef(so, "masterSlider", master);
                SetRef(so, "musicSlider", music);
                SetRef(so, "sfxSlider", sfx);
                SetRef(so, "muteToggle", mute);
                SetRef(so, "windowModeDropdown", windowMode);
                SetRef(so, "resolutionDropdown", resolution);
                SetRef(so, "vsyncToggle", vsync);
                SetRef(so, "fpsDropdown", fps);
                SetRef(so, "uiScaleSlider", uiScale);
                SetRef(so, "screenShakeToggle", shake);
                SetRef(so, "effectsIntensitySlider", effects);
                SetRef(so, "languageDropdown", language);
                SetRef(so, "applyButton", applyBtn);
                SetRef(so, "resetButton", resetBtn);
                SetRef(so, "firstSelected", soundTab.gameObject);
            });

            // uiScale slider range 0.75..1.25
            uiScale.minValue = 0.75f; uiScale.maxValue = 1.25f;

            return panel;
        }

        private static RectTransform BuildSection(RectTransform host)
        {
            var section = NewRect("Section", host);
            Stretch(section);
            var vlg = section.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.padding = new RectOffset(30, 30, 20, 20);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            return section;
        }

        private static RectTransform BuildRow(RectTransform section, string label, string key = null)
        {
            var row = NewRect("Row", section);
            AddLayoutHeight(row, 48);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 24; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;

            var lblRt = NewRect("Label", row);
            var lbl = AddText(lblRt, label, 26, TextLight, TextAlignmentOptions.Left);
            if (key != null) Localize(lbl, key);
            var le = lblRt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 430; le.minWidth = 430; le.flexibleWidth = 0;
            return row;
        }

        private static Slider BuildSliderRow(RectTransform section, string label, string key = null)
        {
            var row = BuildRow(section, label, key);
            var go = DefaultControls.CreateSlider(_uiRes);
            go.transform.SetParent(row, false);
            var sl = go.GetComponent<Slider>();
            sl.minValue = 0f; sl.maxValue = 1f; sl.wholeNumbers = false;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 460; le.flexibleWidth = 1; le.minHeight = 24;
            TintSlider(go);
            return sl;
        }

        private static Toggle BuildToggleRow(RectTransform section, string label, string key = null)
        {
            var row = BuildRow(section, label, key);
            var go = DefaultControls.CreateToggle(_uiRes);
            go.transform.SetParent(row, false);
            var tg = go.GetComponent<Toggle>();
            var lbl = go.transform.Find("Label");
            if (lbl != null) lbl.gameObject.SetActive(false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 40; le.minWidth = 40; le.preferredHeight = 34;
            TintToggle(go);
            return tg;
        }

        private static TMP_Dropdown BuildDropdownRow(RectTransform section, string label, string key = null)
        {
            var row = BuildRow(section, label, key);
            var go = TMP_DefaultControls.CreateDropdown(_tmpRes);
            go.transform.SetParent(row, false);
            var dd = go.GetComponent<TMP_Dropdown>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 360; le.flexibleWidth = 0; le.minHeight = 40;
            TintDropdown(go, dd);
            return dd;
        }

        private static void TintSlider(GameObject go)
        {
            var bg = go.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = ControlBg;
            var fill = go.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fill != null) fill.color = Accent;
            var handle = go.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
            if (handle != null) handle.color = new Color(0.96f, 0.82f, 0.6f, 1f);
        }

        private static void TintToggle(GameObject go)
        {
            var bg = go.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = ControlBg;
            var check = go.transform.Find("Background/Checkmark")?.GetComponent<Image>();
            if (check != null) check.color = Accent;
        }

        private static void TintDropdown(GameObject go, TMP_Dropdown dd)
        {
            var img = go.GetComponent<Image>();
            if (img != null) img.color = ControlBg;
            var caption = go.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (caption != null) { caption.font = _font; caption.color = TextLight; caption.fontSize = 24; }
            var arrow = go.transform.Find("Arrow")?.GetComponent<Image>();
            if (arrow != null) arrow.color = Accent;
            var itemLabel = go.transform.Find("Template/Viewport/Content/Item/Item Label")?.GetComponent<TMP_Text>();
            if (itemLabel != null) { itemLabel.font = _font; itemLabel.color = TextLight; itemLabel.fontSize = 24; }
            var templateImg = go.transform.Find("Template")?.GetComponent<Image>();
            if (templateImg != null) templateImg.color = WindowDark;
        }

        // =====================================================================
        //  Credits, modal, blocker, fader
        // =====================================================================

        private static RectTransform BuildCreditsPanel(RectTransform canvas, out Button backButton)
        {
            var panel = NewRect("CreditsPanel", canvas);
            Stretch(panel);
            AddImage(panel, new Color(0.02f, 0.018f, 0.016f, 0.82f), null, true);

            var window = NewRect("Window", panel);
            Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900, 780));
            AddImage(window, WindowDark, null, true);
            AddFrame(window);

            var titleRt = NewRect("Title", window);
            Anchor(titleRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -46), new Vector2(800, 60));
            Localize(AddText(titleRt, "Авторы", 46, TitleCol, TextAlignmentOptions.Center), "menu.credits");

            // Scroll view for future expansion
            var scrollGo = DefaultControls.CreateScrollView(_uiRes);
            scrollGo.transform.SetParent(window, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            Anchor(scrollRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -100), new Vector2(820, 560));
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            var svImg = scrollGo.GetComponent<Image>();
            if (svImg != null) svImg.color = new Color(0.05f, 0.045f, 0.04f, 0.6f);

            var content = scroll.content;
            var contentVlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 10; contentVlg.padding = new RectOffset(30, 30, 24, 24);
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true; contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true; contentVlg.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddCreditLine(content, "FORGEBORN", 44, TitleCol);
            AddCreditLine(content, "Vilwayer Studio", 30, TextLight);
            AddCreditSpace(content);
            AddCreditLine(content, "Programming", 28, Accent);
            AddCreditLine(content, "TBA", 26, TextLight);
            AddCreditSpace(content);
            AddCreditLine(content, "Game Design", 28, Accent);
            AddCreditLine(content, "TBA", 26, TextLight);
            AddCreditSpace(content);
            AddCreditLine(content, "Art", 28, Accent);
            AddCreditLine(content, "TBA", 26, TextLight);
            AddCreditSpace(content);
            AddCreditLine(content, "Music", 28, Accent);
            AddCreditLine(content, "TBA", 26, TextLight);

            var foot = NewRect("Footer", window);
            Anchor(foot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 44), new Vector2(300, 58));
            backButton = BuildMenuButton(foot, "Назад", 300, 58, out var lCredBack); Localize(lCredBack, "common.back");

            return panel;
        }

        private struct LoadPanelRefs
        {
            public RectTransform panel;
            public Button back;
            public TMP_Text[] names;
            public TMP_Text[] dates;
            public Button[] loads;
            public Button[] deletes;
        }

        private static LoadPanelRefs BuildLoadPanel(RectTransform canvas)
        {
            const int N = 6; // must match LocalSaveGameService slot count
            var refs = new LoadPanelRefs
            {
                names = new TMP_Text[N], dates = new TMP_Text[N],
                loads = new Button[N], deletes = new Button[N],
            };

            var panel = NewRect("LoadPanel", canvas);
            Stretch(panel);
            AddImage(panel, new Color(0.02f, 0.018f, 0.016f, 0.82f), null, true);
            refs.panel = panel;

            var window = NewRect("Window", panel);
            Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1080, 860));
            AddImage(window, WindowDark, null, true);
            AddFrame(window);

            var titleRt = NewRect("Title", window);
            Anchor(titleRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -46), new Vector2(900, 60));
            Localize(AddText(titleRt, "Загрузить игру", 46, TitleCol, TextAlignmentOptions.Center), "menu.load_title");

            float y0 = 250f, step = 92f;
            for (int i = 0; i < N; i++)
            {
                var row = NewRect("Row" + i, window);
                Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, y0 - i * step), new Vector2(960, 80));
                AddImage(row, new Color(0.05f, 0.045f, 0.04f, 0.6f), null, false);

                var nameRt = NewRect("Name", row);
                Anchor(nameRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30, 6), new Vector2(360, 42));
                refs.names[i] = AddText(nameRt, "", 28, TextLight, TextAlignmentOptions.Left);

                var dateRt = NewRect("Date", row);
                Anchor(dateRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30, -22), new Vector2(360, 30));
                refs.dates[i] = AddText(dateRt, "", 20, TextDim, TextAlignmentOptions.Left);

                var loadHost = NewRect("LoadHost", row);
                Anchor(loadHost, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-330, 0), new Vector2(190, 56));
                refs.loads[i] = BuildMenuButton(loadHost, "Загрузить", 190, 56, out var lLoad); Localize(lLoad, "save.load");
                Stretch((RectTransform)refs.loads[i].transform);

                var delHost = NewRect("DelHost", row);
                Anchor(delHost, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120, 0), new Vector2(180, 56));
                refs.deletes[i] = BuildMenuButton(delHost, "Удалить", 180, 56, out var lDel); Localize(lDel, "save.delete");
                Stretch((RectTransform)refs.deletes[i].transform);
            }

            var foot = NewRect("Footer", window);
            Anchor(foot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 44), new Vector2(300, 58));
            refs.back = BuildMenuButton(foot, "Назад", 300, 58, out var lBack); Localize(lBack, "common.back");

            return refs;
        }

        private static void AddCreditLine(RectTransform parent, string text, float size, Color color)
        {
            var rt = NewRect("Line", parent);
            AddText(rt, text, size, color, TextAlignmentOptions.Center);
            AddLayoutHeight(rt, size + 12);
        }

        private static void AddCreditSpace(RectTransform parent)
        {
            var rt = NewRect("Space", parent);
            AddLayoutHeight(rt, 16);
        }

        private static ConfirmationModal BuildConfirmationModal(RectTransform canvas)
        {
            var panel = NewRect("ConfirmationModal", canvas);
            Stretch(panel);
            var modal = panel.gameObject.AddComponent<ConfirmationModal>();

            var frame = NewRect("Frame", panel);
            Stretch(frame);
            AddImage(frame, new Color(0.01f, 0.01f, 0.01f, 0.7f), null, true);

            var window = NewRect("Window", frame);
            Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760, 320));
            AddImage(window, WindowDark, null, true);
            AddFrame(window);

            var msgRt = NewRect("Message", window);
            Anchor(msgRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -40), new Vector2(660, 150));
            var msg = AddText(msgRt, "Сообщение", 30, TextLight, TextAlignmentOptions.Center);
            msg.textWrappingMode = TextWrappingModes.Normal;

            var foot = NewRect("Buttons", window);
            Anchor(foot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 40), new Vector2(560, 62));
            var footHl = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footHl.spacing = 24; footHl.childAlignment = TextAnchor.MiddleCenter;
            footHl.childControlWidth = true; footHl.childControlHeight = true;
            footHl.childForceExpandWidth = true; footHl.childForceExpandHeight = true;
            var confirmBtn = BuildMenuButton(foot, "Начать", 260, 60, out var confirmLabel);
            var cancelBtn = BuildMenuButton(foot, "Отмена", 260, 60, out var cancelLabel);

            WireComponent(modal, so =>
            {
                SetRef(so, "root", frame.gameObject);
                SetRef(so, "messageText", msg);
                SetRef(so, "confirmButton", confirmBtn);
                SetRef(so, "cancelButton", cancelBtn);
                SetRef(so, "confirmLabel", confirmLabel);
                SetRef(so, "cancelLabel", cancelLabel);
            });

            frame.gameObject.SetActive(false);
            return modal;
        }

        private static RectTransform BuildLoadingBlocker(RectTransform canvas)
        {
            var blocker = NewRect("LoadingBlocker", canvas);
            Stretch(blocker);
            AddImage(blocker, new Color(0f, 0f, 0f, 0.35f), null, true);
            var txtRt = NewRect("Label", blocker);
            Anchor(txtRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -260), new Vector2(600, 60));
            Localize(AddText(txtRt, "Загрузка…", 34, TextLight, TextAlignmentOptions.Center), "common.loading");
            return blocker;
        }

        private static ScreenFader BuildScreenFader(RectTransform canvas)
        {
            var faderRt = NewRect("ScreenFader", canvas);
            Stretch(faderRt);
            AddImage(faderRt, Color.black, null, true);
            var cg = faderRt.gameObject.AddComponent<CanvasGroup>();
            var fader = faderRt.gameObject.AddComponent<ScreenFader>();
            WireComponent(fader, so => SetRef(so, "canvasGroup", cg));
            return fader;
        }

        // =====================================================================
        //  Audio
        // =====================================================================

        private static AudioManager BuildAudioManager(Transform systems)
        {
            var go = new GameObject("AudioManager");
            go.transform.SetParent(systems, false);
            var am = go.AddComponent<AudioManager>();

            var musicGo = new GameObject("MusicSource", typeof(AudioSource));
            musicGo.transform.SetParent(go.transform, false);
            var musicSrc = musicGo.GetComponent<AudioSource>();
            musicSrc.playOnAwake = false; musicSrc.loop = true;

            var sfxGo = new GameObject("SFXSource", typeof(AudioSource));
            sfxGo.transform.SetParent(go.transform, false);
            var sfxSrc = sfxGo.GetComponent<AudioSource>();
            sfxSrc.playOnAwake = false;

            WireComponent(am, so =>
            {
                SetRef(so, "musicSource", musicSrc);
                SetRef(so, "sfxSource", sfxSrc);
                SetRef(so, "musicGroup", FindGroup("Music"));
                SetRef(so, "sfxGroup", FindGroup("SFX"));
            });
            return am;
        }

        // =====================================================================
        //  Reusable button
        // =====================================================================

        private static Button BuildMenuButton(RectTransform parent, string label, float width, float height)
            => BuildMenuButton(parent, label, width, height, out _);

        private static Button BuildMenuButton(RectTransform parent, string label, float width, float height, out TMP_Text labelText)
        {
            var root = NewRect("Button_" + label, parent);
            root.sizeDelta = new Vector2(width, height);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = height; le.minHeight = height;

            var bg = AddImage(root, PanelDark, null, true);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;

            var glow = NewRect("Glow", root);
            Stretch(glow, -16, -16, -16, -16);
            var glowImg = AddImage(glow, new Color(Accent.r, Accent.g, Accent.b, 0f), _glow, false);

            var border = NewRect("Border", root);
            Stretch(border);
            var borderImg = AddImage(border, BorderCol, _frame, false);
            borderImg.type = Image.Type.Sliced;

            var labelRt = NewRect("Label", root);
            Stretch(labelRt, 18, 0, 18, 0);
            labelText = AddText(labelRt, label, 30, TextLight, TextAlignmentOptions.Center);

            var vis = root.gameObject.AddComponent<MenuButtonVisual>();
            var lt = labelText;
            WireComponent(vis, so =>
            {
                SetRef(so, "scaleTarget", root);
                SetRef(so, "background", bg);
                SetRef(so, "border", borderImg);
                SetRef(so, "glow", glowImg);
                SetRef(so, "label", lt);
                SetRef(so, "audioManager", _audio);
            });
            return button;
        }

        // =====================================================================
        //  Low-level UI helpers
        // =====================================================================

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image AddImage(RectTransform rt, Color color, Sprite sprite, bool raycast)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        private static TMP_Text AddText(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>Attach a LocalizedText so this label follows the active language by key.</summary>
        private static void Localize(TMP_Text t, string key)
        {
            if (t == null) return;
            t.gameObject.AddComponent<LocalizedText>().SetKey(key);
        }

        private static void AddFrame(RectTransform target)
        {
            var frame = NewRect("Frame", target);
            Stretch(frame);
            var img = AddImage(frame, BorderCol, _frame, false);
            img.type = Image.Type.Sliced;
        }

        private static void AddLayoutHeight(RectTransform rt, float height)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;
        }

        private static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        private static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // =====================================================================
        //  EventSystem / control resources
        // =====================================================================

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static void BuildControlResources()
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
        //  Serialization wiring + build settings
        // =====================================================================

        private static void WireComponent(Component c, Action<SerializedObject> setup)
        {
            var so = new SerializedObject(c);
            setup(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(SerializedObject so, string prop, UnityEngine.Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogWarning($"[MainMenuBuilder] Missing serialized field '{prop}' on {so.targetObject.GetType().Name}.");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static void SetStr(SerializedObject so, string prop, string value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.stringValue = value;
        }

        private static void SetObjArray(SerializedObject so, string prop, UnityEngine.Object[] vals)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"[MainMenuBuilder] Missing array '{prop}' on {so.targetObject.GetType().Name}."); return; }
            p.arraySize = vals.Length;
            for (int i = 0; i < vals.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = vals[i];
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AddSceneToBuildSettingsFirst(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == scenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
