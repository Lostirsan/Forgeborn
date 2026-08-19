using System;
using System.Collections.Generic;
using System.IO;
using ForgeGame.Dungeon;
using ForgeGame.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// Builds the Dungeon vertical-slice scene: fixed camera down a block-built cave corridor,
    /// the hero drawn from behind, a depth-fade into the dark ahead, and a HUD hint. Wires the
    /// <see cref="DungeonController"/> and registers Dungeon (and Smithy) in Build Settings so
    /// the smithy's dungeon door can load it. Menu: Tools ▸ Forge Game ▸ Build Dungeon Scene.
    /// </summary>
    public static class DungeonSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Dungeon.unity";
        private const string SmithyPath = "Assets/Scenes/Smithy.unity";
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string FontPath = "Assets/Fonts/ForgeUI Dynamic SDF.asset";

        [MenuItem("Tools/Forge Game/Build Dungeon Scene")]
        public static void BuildFromMenu()
        {
            Build();
            EditorUtility.DisplayDialog("Forge Game", "Сцена подземелья создана и добавлена в Build Settings.", "OK");
        }

        public static void Build()
        {
            DungeonArtGenerator.EnsureAll();
            var wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.SideWall);
            var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.SideFloor);
            var ceilingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.SideCeiling);
            var heroA = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.SideHeroA);
            var heroB = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.SideHeroB);
            var vision = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.Vision);
            var torch = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.Torch);
            var glow = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.TorchGlow);
            var ore = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.Ore);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;

            Sprite Load(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);
            var enemyRat = Load(DungeonArtGenerator.EnemyRat);
            var enemyGoblin = Load(DungeonArtGenerator.EnemyGoblin);
            var enemyBrute = Load(DungeonArtGenerator.EnemyBrute);
            var hpBar = Load(DungeonArtGenerator.HpBar);
            var btnPlate = Load(DungeonArtGenerator.BtnPlate);
            var abStrike = Load(DungeonArtGenerator.AbStrike);
            var abBreak = Load(DungeonArtGenerator.AbBreak);
            var abGuard = Load(DungeonArtGenerator.AbGuard);
            var abLunge = Load(DungeonArtGenerator.AbLunge);
            var abHeal = Load(DungeonArtGenerator.AbHeal);

            const float groundY = -3.2f, partyX = -3f;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            camGo.AddComponent<AudioListener>();

            var worldRoot = new GameObject("WorldRoot").transform;

            // Party stands on the ground on the left, facing right, torch on the leader.
            var partyGo = new GameObject("Party", typeof(Transform));
            partyGo.transform.position = new Vector3(partyX, groundY, 0f);
            var leaderT = AddHero(partyGo.transform, heroA, new Vector3(0f, HalfH(heroA, 1.3f), 0f), 1.3f, 6);   // leader (front)
            var companionT = AddHero(partyGo.transform, heroB, new Vector3(-1.3f, HalfH(heroB, 1.15f), 0f), 1.15f, 5); // companion (behind)

            // Torch light rides with the party: darkness mask (clear circle around them), warm glow, flame.
            var visionGo = new GameObject("Vision");
            visionGo.transform.SetParent(partyGo.transform, false);
            visionGo.transform.localPosition = new Vector3(0.6f, 1.0f, 1.5f);
            visionGo.transform.localScale = new Vector3(8f, 8f, 1f);
            var visionSr = visionGo.AddComponent<SpriteRenderer>();
            visionSr.sprite = vision; visionSr.sortingOrder = 15; visionSr.color = Color.white;

            var glowGo = new GameObject("TorchGlow");
            glowGo.transform.SetParent(partyGo.transform, false);
            glowGo.transform.localPosition = new Vector3(0.4f, 1.9f, 1.4f);
            glowGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = glow; glowSr.sortingOrder = 16;

            var torchGo = new GameObject("TorchFlame");
            torchGo.transform.SetParent(partyGo.transform, false);
            torchGo.transform.localPosition = new Vector3(0.4f, 2.05f, 1.3f);
            torchGo.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            var torchSr = torchGo.AddComponent<SpriteRenderer>();
            torchSr.sprite = torch; torchSr.sortingOrder = 17;

            // HUD.
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var canvasRt = (RectTransform)canvasGo.transform;

            var depthLabel = AddText(canvasRt, font, "Глубина: 0 м", 34, new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(400, 50), TextAlignmentOptions.TopLeft);
            var hotbar = BuildInventoryPanel(canvasRt, font, out var invPanel);

            BuildEventSystem();

            var sys = new GameObject("DungeonSystem").AddComponent<DungeonSideController>();
            var so = new SerializedObject(sys);
            SetRef(so, "worldRoot", worldRoot);
            SetRef(so, "party", partyGo.transform);
            SetRef(so, "leaderView", leaderT);
            SetRef(so, "companionView", companionT);
            SetRef(so, "cam", cam);
            SetRef(so, "wallSprite", wallSprite);
            SetRef(so, "floorSprite", floorSprite);
            SetRef(so, "ceilingSprite", ceilingSprite);
            SetRef(so, "torchGlow", glowSr);
            SetRef(so, "torchFlame", torchGo.transform);
            SetRef(so, "visionTransform", visionGo.transform);
            SetRef(so, "depthLabel", depthLabel);
            SetRef(so, "oreSprite", ore);
            SetRef(so, "hotbar", hotbar);
            SetRef(so, "inventoryPanel", invPanel);
            SetStr(so, "smithySceneName", "Smithy");

            // Combat HUD + system (references the party/heroes above and the shared hotbar).
            var combat = BuildCombat(canvasRt, font, cam, partyGo.transform, leaderT, companionT, groundY, sys,
                new[] { enemyRat, enemyGoblin, enemyBrute }, hpBar, btnPlate,
                new[] { abStrike, abBreak, abGuard, abLunge, abHeal }, hotbar, ore);
            SetRef(so, "combat", combat);

            // ESC pause + settings panels.
            var pause = BuildPausePanels(canvasRt, font, btnPlate);
            SetRef(so, "pause", pause);
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[DungeonSceneBuilder] Dungeon scene built at " + ScenePath + " and registered in Build Settings.");
        }

        // ---- ESC pause + settings ----

        private static void Localize(TMP_Text t, string key)
        {
            if (t == null) return;
            t.gameObject.AddComponent<LocalizedText>().SetKey(key);
        }

        // A full-screen click-blocking dim with a centred window; returns the panel root (out window).
        private static GameObject FullDim(RectTransform canvas, string name, out RectTransform window, Vector2 winSize)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            var dim = panel.AddComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.72f); dim.raycastTarget = true;

            var win = new GameObject("Window", typeof(RectTransform));
            win.transform.SetParent(panel.transform, false);
            window = (RectTransform)win.transform;
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = winSize;
            var wbg = win.AddComponent<Image>(); wbg.color = new Color(0.11f, 0.10f, 0.09f, 0.98f);
            return panel;
        }

        private static Button AddButton(RectTransform parent, Sprite plate, TMP_FontAsset font, Vector2 pos, Vector2 size,
            string text, string locKey, out TMP_Text label)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var bg = go.AddComponent<Image>();
            bg.sprite = plate; bg.type = Image.Type.Sliced; bg.color = Color.white;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors; colors.highlightedColor = new Color(1.12f, 1.06f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.6f); btn.colors = colors;

            label = AddText(rt, font, text, 30, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(24, 8), TextAlignmentOptions.Center);
            label.raycastTarget = false;
            if (locKey != null) Localize(label, locKey);
            return btn;
        }

        private static Slider AddSlider(RectTransform parent, Vector2 pos, Vector2 size, float value)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var slider = go.AddComponent<Slider>();

            var bg = new GameObject("Background", typeof(RectTransform)); bg.transform.SetParent(go.transform, false);
            var bgRt = (RectTransform)bg.transform; bgRt.anchorMin = new Vector2(0, 0.35f); bgRt.anchorMax = new Vector2(1, 0.65f); bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
            var faRt = (RectTransform)fillArea.transform; faRt.anchorMin = new Vector2(0, 0.35f); faRt.anchorMax = new Vector2(1, 0.65f); faRt.offsetMin = new Vector2(6, 0); faRt.offsetMax = new Vector2(-6, 0);
            var fill = new GameObject("Fill", typeof(RectTransform)); fill.transform.SetParent(fillArea.transform, false);
            var fillRt = (RectTransform)fill.transform; fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1); fillRt.sizeDelta = new Vector2(10, 0);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(0.85f, 0.62f, 0.28f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)); handleArea.transform.SetParent(go.transform, false);
            var haRt = (RectTransform)handleArea.transform; haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.offsetMin = new Vector2(10, 0); haRt.offsetMax = new Vector2(-10, 0);
            var handle = new GameObject("Handle", typeof(RectTransform)); handle.transform.SetParent(handleArea.transform, false);
            var hRt = (RectTransform)handle.transform; hRt.sizeDelta = new Vector2(28, 0);
            var hImg = handle.AddComponent<Image>(); hImg.color = new Color(0.92f, 0.88f, 0.8f);

            slider.fillRect = fillRt; slider.handleRect = hRt; slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.value = value;
            return slider;
        }

        private static Toggle AddToggle(RectTransform parent, TMP_FontAsset font, Vector2 pos, string locKey, bool isOn)
        {
            var go = new GameObject("Toggle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(420, 52);
            var toggle = go.AddComponent<Toggle>();

            var box = new GameObject("Box", typeof(RectTransform)); box.transform.SetParent(go.transform, false);
            var boxRt = (RectTransform)box.transform; boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0, 0.5f);
            boxRt.anchoredPosition = new Vector2(0, 0); boxRt.sizeDelta = new Vector2(44, 44);
            var boxImg = box.AddComponent<Image>(); boxImg.color = new Color(0.06f, 0.06f, 0.06f, 1f);
            var check = new GameObject("Check", typeof(RectTransform)); check.transform.SetParent(box.transform, false);
            var chRt = (RectTransform)check.transform; chRt.anchorMin = new Vector2(0.15f, 0.15f); chRt.anchorMax = new Vector2(0.85f, 0.85f); chRt.offsetMin = chRt.offsetMax = Vector2.zero;
            var chImg = check.AddComponent<Image>(); chImg.color = new Color(0.5f, 0.82f, 0.4f);

            toggle.targetGraphic = boxImg; toggle.graphic = chImg; toggle.isOn = isOn;

            var lbl = AddText(rt, font, "", 28, new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(340, 44), TextAlignmentOptions.Left);
            Localize(lbl, locKey);
            return toggle;
        }

        private static DungeonPauseController BuildPausePanels(RectTransform canvas, TMP_FontAsset font, Sprite plate)
        {
            // Pause window: Resume / Return to smithy / Settings / Exit to menu.
            var pausePanel = FullDim(canvas, "PausePanel", out var pWin, new Vector2(560, 640));
            Localize(AddText(pWin, font, "Пауза", 54, new Vector2(0.5f, 1f), new Vector2(0, -54), new Vector2(500, 66), TextAlignmentOptions.Center), "pause.title");
            var resume = AddButton(pWin, plate, font, new Vector2(0, 150), new Vector2(440, 84), "Продолжить", "menu.continue", out _);
            var smithy = AddButton(pWin, plate, font, new Vector2(0, 54), new Vector2(440, 84), "Вернуться в кузню", "pause.to_smithy", out _);
            var settingsBtn = AddButton(pWin, plate, font, new Vector2(0, -42), new Vector2(440, 84), "Настройки", "menu.settings", out _);
            var exit = AddButton(pWin, plate, font, new Vector2(0, -138), new Vector2(440, 84), "Выход в меню", "pause.exit_menu", out _);

            // Settings window: language / master volume / fullscreen.
            var settingsPanel = FullDim(canvas, "SettingsPanel", out var sWin, new Vector2(720, 620));
            Localize(AddText(sWin, font, "Настройки", 50, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(600, 62), TextAlignmentOptions.Center), "settings.title");
            Localize(AddText(sWin, font, "Язык", 30, new Vector2(0f, 1f), new Vector2(60, -150), new Vector2(300, 40), TextAlignmentOptions.Left), "settings.language");
            var langs = new[] { ("ru", "Рус"), ("en", "Eng"), ("es", "Esp"), ("hi", "हिन्दी") };
            var langButtons = new Button[langs.Length];
            for (int i = 0; i < langs.Length; i++)
                langButtons[i] = AddButton(sWin, plate, font, new Vector2(-240 + i * 160, 130), new Vector2(150, 76), langs[i].Item2, null, out _);

            Localize(AddText(sWin, font, "Общая громкость", 30, new Vector2(0f, 1f), new Vector2(60, -300), new Vector2(400, 40), TextAlignmentOptions.Left), "settings.master");
            var master = AddSlider(sWin, new Vector2(60, -20), new Vector2(560, 44), 1f);
            var fullscreen = AddToggle(sWin, font, new Vector2(-30, -110), "settings.fullscreen", Screen.fullScreen);
            var back = AddButton(sWin, plate, font, new Vector2(0, -250), new Vector2(280, 84), "Назад", "common.back", out _);

            var go = new GameObject("DungeonPause");
            var ctrl = go.AddComponent<DungeonPauseController>();
            foreach (var (code, _) in langs)
            {
                var b = langButtons[Array.FindIndex(langs, l => l.Item1 == code)];
                UnityEventTools.AddStringPersistentListener(b.onClick, ctrl.SetLanguage, code);
            }

            var cso = new SerializedObject(ctrl);
            SetRef(cso, "pauseRoot", pausePanel);
            SetRef(cso, "settingsRoot", settingsPanel);
            SetRef(cso, "resumeButton", resume);
            SetRef(cso, "smithyButton", smithy);
            SetRef(cso, "settingsButton", settingsBtn);
            SetRef(cso, "exitButton", exit);
            SetRef(cso, "backButton", back);
            SetRef(cso, "masterSlider", master);
            SetRef(cso, "fullscreenToggle", fullscreen);
            SetStr(cso, "menuSceneName", "MainMenu");
            cso.ApplyModifiedPropertiesWithoutUndo();

            pausePanel.SetActive(false); settingsPanel.SetActive(false);
            return ctrl;
        }

        private static DungeonCombat BuildCombat(RectTransform canvas, TMP_FontAsset font, Camera cam,
            Transform party, Transform leader, Transform companion, float groundY, DungeonSideController controller,
            Sprite[] enemies, Sprite hpBar, Sprite btnPlate, Sprite[] abIcons, DungeonHotbar hotbar, Sprite ore)
        {
            // Turn indicator (top-centre) + one-line combat log (upper-centre).
            var turnLabel = AddText(canvas, font, "", 40, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(700, 56), TextAlignmentOptions.Center);
            var logLabel = AddText(canvas, font, "", 30, new Vector2(0.5f, 1f), new Vector2(0, -96), new Vector2(1100, 44), TextAlignmentOptions.Center);
            logLabel.color = new Color(0.85f, 0.78f, 0.6f);

            // Ability bar pinned to the bottom-LEFT corner, entirely left of the party so the
            // buttons never cover our heroes (who stand at centre-left).
            var barGo = new GameObject("AbilityBar", typeof(RectTransform));
            barGo.transform.SetParent(canvas, false);
            var barRt = (RectTransform)barGo.transform;
            barRt.anchorMin = barRt.anchorMax = barRt.pivot = new Vector2(0f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(560, 220);

            var buttons = new Button[3]; var icons = new Image[3]; var labels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float x = 6f + i * 154f; // 3 buttons flush into the bottom-left corner
                AddAbilityButton(barRt, font, btnPlate, x, out buttons[i], out icons[i], out labels[i]);
            }

            // Victory / defeat banner (centre), hidden by default.
            var bannerGo = new GameObject("Banner", typeof(RectTransform));
            bannerGo.transform.SetParent(canvas, false);
            var bRt = (RectTransform)bannerGo.transform;
            bRt.anchorMin = bRt.anchorMax = bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0, 120); bRt.sizeDelta = new Vector2(1200, 160);
            var bBg = bannerGo.AddComponent<Image>(); bBg.color = new Color(0f, 0f, 0f, 0.55f); bBg.raycastTarget = false;
            var bannerLabel = AddText(bRt, font, "", 76, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 150), TextAlignmentOptions.Center);
            bannerLabel.color = new Color(0.98f, 0.86f, 0.5f);
            bannerGo.SetActive(false);

            var combatGo = new GameObject("DungeonCombat");
            var combat = combatGo.AddComponent<DungeonCombat>();
            for (int i = 0; i < 3; i++)
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, combat.ClickAbility, i);

            var cso = new SerializedObject(combat);
            SetRef(cso, "cam", cam);
            SetRef(cso, "party", party);
            SetRef(cso, "leaderView", leader);
            SetRef(cso, "companionView", companion);
            cso.FindProperty("groundY").floatValue = groundY;
            SetArray(cso, "enemySprites", enemies);
            SetRef(cso, "hpBarSprite", hpBar);
            SetArray(cso, "abilityIcons", abIcons);
            SetRef(cso, "hotbar", hotbar);
            SetRef(cso, "oreSprite", ore);
            SetStr(cso, "oreItemId", "iron_ore");
            SetRef(cso, "controller", controller);
            SetRef(cso, "abilityBar", barGo);
            SetArray(cso, "abilityButtons", buttons);
            SetArray(cso, "abilityButtonIcons", icons);
            SetArray(cso, "abilityButtonLabels", labels);
            SetRef(cso, "turnLabel", turnLabel);
            SetRef(cso, "logLabel", logLabel);
            SetRef(cso, "bannerObject", bannerGo);
            SetRef(cso, "bannerLabel", bannerLabel);
            cso.ApplyModifiedPropertiesWithoutUndo();

            barGo.SetActive(false);
            return combat;
        }

        private static void AddAbilityButton(RectTransform parent, TMP_FontAsset font, Sprite plate, float x,
            out Button button, out Image icon, out TMP_Text label)
        {
            var go = new GameObject("AbilityBtn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, 8); rt.sizeDelta = new Vector2(148, 148);
            var bg = go.AddComponent<Image>();
            bg.sprite = plate; bg.type = Image.Type.Sliced; bg.color = Color.white;
            button = go.AddComponent<Button>();
            var colors = button.colors; colors.highlightedColor = new Color(1.1f, 1.05f, 0.9f); colors.pressedColor = new Color(0.8f, 0.75f, 0.6f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.7f); button.colors = colors;

            var icoGo = new GameObject("Icon", typeof(RectTransform));
            icoGo.transform.SetParent(go.transform, false);
            var icoRt = (RectTransform)icoGo.transform;
            icoRt.anchorMin = icoRt.anchorMax = icoRt.pivot = new Vector2(0.5f, 0.5f);
            icoRt.anchoredPosition = new Vector2(0, 24); icoRt.sizeDelta = new Vector2(104, 104);
            icon = icoGo.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;

            label = AddText(rt, font, "", 26, new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(176, 40), TextAlignmentOptions.Center);
            label.raycastTarget = false;
        }

        private static TMP_Text AddText(RectTransform parent, TMP_FontAsset font, string text, float size,
            Vector2 anchor, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = new Color(0.9f, 0.82f, 0.66f);
            t.alignment = align; t.raycastTarget = false;
            return t;
        }

        private static DungeonHotbar BuildInventoryPanel(RectTransform canvas, TMP_FontAsset font, out GameObject panelRoot)
        {
            // Full-screen dim that blocks clicks + a centred window.
            var panel = new GameObject("InventoryPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            var dim = panel.AddComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.6f); dim.raycastTarget = true;

            var window = new GameObject("Window", typeof(RectTransform));
            window.transform.SetParent(panel.transform, false);
            var wrt = (RectTransform)window.transform;
            wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(520, 600);
            var wbg = window.AddComponent<Image>(); wbg.color = new Color(0.10f, 0.09f, 0.08f, 0.98f); wbg.raycastTarget = true;

            Localize(AddText(wrt, font, "Инвентарь", 40, new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(480, 54), TextAlignmentOptions.Center), "inventory.title");

            // 3×3 grid of slots.
            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(window.transform, false);
            var grt = (RectTransform)gridGo.transform;
            grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0, -20);
            grt.sizeDelta = new Vector2(3 * 130 + 2 * 14, 3 * 130 + 2 * 14);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            grid.cellSize = new Vector2(130, 130); grid.spacing = new Vector2(14, 14);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var icons = new Image[9];
            var counts = new TMP_Text[9];
            for (int i = 0; i < 9; i++)
            {
                var cell = new GameObject("Slot" + i, typeof(RectTransform));
                cell.transform.SetParent(gridGo.transform, false);
                var cbg = cell.AddComponent<Image>();
                cbg.color = new Color(0.06f, 0.055f, 0.05f, 1f);

                var icoGo = new GameObject("Icon", typeof(RectTransform));
                icoGo.transform.SetParent(cell.transform, false);
                var icoRt = (RectTransform)icoGo.transform;
                icoRt.anchorMin = icoRt.anchorMax = new Vector2(0.5f, 0.5f);
                icoRt.sizeDelta = new Vector2(92, 92);
                var ico = icoGo.AddComponent<Image>();
                ico.enabled = false; ico.preserveAspect = true; ico.raycastTarget = false;
                icons[i] = ico;

                var cntGo = new GameObject("Count", typeof(RectTransform));
                cntGo.transform.SetParent(cell.transform, false);
                var cntRt = (RectTransform)cntGo.transform;
                cntRt.anchorMin = cntRt.anchorMax = cntRt.pivot = new Vector2(1f, 0f);
                cntRt.sizeDelta = new Vector2(60, 34); cntRt.anchoredPosition = new Vector2(-6, 4);
                var cnt = cntGo.AddComponent<TextMeshProUGUI>();
                if (font != null) cnt.font = font;
                cnt.fontSize = 30; cnt.color = Color.white; cnt.alignment = TextAlignmentOptions.BottomRight;
                cnt.raycastTarget = false; cnt.text = "";
                counts[i] = cnt;
            }

            Localize(AddText(wrt, font, "I — закрыть", 24, new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(400, 34), TextAlignmentOptions.Center), "dungeon.inv_close");

            var hotbar = panel.AddComponent<DungeonHotbar>();
            var so = new SerializedObject(hotbar);
            SetArray(so, "slotIcons", icons);
            SetArray(so, "slotCounts", counts);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false); // hidden until the player opens it
            panelRoot = panel;
            return hotbar;
        }

        private static void SetArray(SerializedObject so, string prop, UnityEngine.Object[] items)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning("[DungeonSceneBuilder] Missing array field " + prop); return; }
            p.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static float HalfH(Sprite s, float scale) => (s != null ? s.rect.height / s.pixelsPerUnit : 1.4f) * scale * 0.5f;

        private static Transform AddHero(Transform party, Sprite sprite, Vector3 localPos, float scale, int order)
        {
            var go = new GameObject("Hero");
            go.transform.SetParent(party, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = order;
            return go.transform;
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static void RegisterScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            void Ensure(string path)
            {
                if (!File.Exists(path)) return;
                if (!scenes.Exists(s => s.path == path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            Ensure(MainMenuPath);
            Ensure(SmithyPath);
            Ensure(ScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetRef(SerializedObject so, string prop, UnityEngine.Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning("[DungeonSceneBuilder] Missing field " + prop);
        }

        private static void SetStr(SerializedObject so, string prop, string value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.stringValue = value;
        }
    }
}
