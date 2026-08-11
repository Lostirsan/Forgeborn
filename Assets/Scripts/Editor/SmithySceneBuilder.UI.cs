using System;
using System.Collections.Generic;
using System.IO;
using ForgeGame.Smithy;
using ForgeGame.UI.Common;
using ForgeGame.UI.Smithy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ForgeGame.EditorTools
{
    public static partial class SmithySceneBuilder
    {
        private static ScreenFader _fader;
        private static InteractionPromptController _prompt;
        private static NotificationController _notifications;
        private static HudController _hud;

        // =====================================================================
        //  Canvas + UI root
        // =====================================================================

        private static void BuildCanvasAndUi(SmithyPlayerController player, InteractionDetector detector, Transform cameraTf)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRt = (RectTransform)canvasGo.transform;

            var esGo = new GameObject("EventSystem", typeof(EventSystem));
            esGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            _uiRoot = NewRect("UIRoot", canvasRt);
            Stretch(_uiRoot);

            BuildHud();
            BuildPrompt();
            BuildNotification();
            BuildVignette();
            BuildShopForgeArrows();

            // Panels.
            BuildInventoryPanel();
            BuildFoundryPanel();
            BuildAnvilPanel();
            BuildAssemblyPanel();
            BuildJournalPanel();
            BuildItemResultPanel();
            BuildPausePanel();
            BuildSettingsPanel();
            BuildDebugPanel();

            // Blocks input during a view transition (above panels, below fader).
            BuildTransitionBlocker();

            // Fader on top of everything.
            var faderRt = NewRect("ScreenFader", canvasRt);
            Stretch(faderRt);
            AddImage(faderRt, Color.black, true);
            var cg = faderRt.gameObject.AddComponent<CanvasGroup>();
            _fader = faderRt.gameObject.AddComponent<ScreenFader>();
            WireComponent(_fader, so => SetRef(so, "canvasGroup", cg));
        }

        // =====================================================================
        //  Dual-view UI: vignette, arrows/HUDs, transition blocker
        // =====================================================================

        private static void BuildVignette()
        {
            var rt = NewRect("Vignette", _uiRoot);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            if (_vignetteSprite != null) { img.sprite = _vignetteSprite; img.color = Color.white; }
            else img.color = new Color(0f, 0f, 0f, 1f);
            _vignette = rt.gameObject.AddComponent<CanvasGroup>();
            _vignette.alpha = 0f; _vignette.interactable = false; _vignette.blocksRaycasts = false;
        }

        private static void BuildShopForgeArrows()
        {
            var shopRt = NewRect("ShopHUD", _uiRoot);
            Stretch(shopRt);
            _shopHud = shopRt.gameObject.AddComponent<CanvasGroup>();
            _shopArrow = AddArrow(shopRt, ">", true);

            var forgeRt = NewRect("ForgeHUD", _uiRoot);
            Stretch(forgeRt);
            _forgeHud = forgeRt.gameObject.AddComponent<CanvasGroup>();
            _forgeArrow = AddArrow(forgeRt, "<", false);
        }

        private static Button AddArrow(RectTransform parent, string glyph, bool right)
        {
            var rt = NewRect(right ? "ShopToForgeArrow" : "ForgeToShopArrow", parent);
            Anchor(rt,
                new Vector2(right ? 1 : 0, 0.5f), new Vector2(right ? 1 : 0, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(right ? -90 : 90, 0), new Vector2(110, 150));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _white; img.type = Image.Type.Simple; img.color = new Color(0.14f, 0.10f, 0.08f, 0.82f); img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = new Color(0.16f, 0.12f, 0.09f, 0.9f); cb.highlightedColor = BtnHi;
            cb.pressedColor = BtnHi * 0.8f; cb.selectedColor = BtnHi; cb.fadeDuration = 0.1f;
            btn.colors = cb;

            var lblRt = NewRect("Label", rt);
            Stretch(lblRt);
            var t = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.text = glyph; t.fontSize = 66; t.color = Accent; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            return btn;
        }

        private static void BuildTransitionBlocker()
        {
            var rt = NewRect("ViewTransitionBlocker", _uiRoot);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // invisible, only blocks raycasts when enabled
            img.raycastTarget = true;
            _transitionBlocker = rt.gameObject.AddComponent<CanvasGroup>();
            _transitionBlocker.alpha = 1f; _transitionBlocker.interactable = false; _transitionBlocker.blocksRaycasts = false;
        }

        // =====================================================================
        //  Dual-view wiring
        // =====================================================================

        private static void WireViewController()
        {
            WireComponent(_viewController, so =>
            {
                SetRef(so, "cameraRig", _cameraRig);
                SetRef(so, "mainCamera", _mainCamera);
                SetRef(so, "shopViewAnchor", _shopAnchor);
                SetRef(so, "forgeViewAnchor", _forgeAnchor);
                SetRef(so, "shopViewRoot", _shopRoot);
                SetRef(so, "forgeViewRoot", _forgeRoot);
                SetRef(so, "shopHud", _shopHud);
                SetRef(so, "forgeHud", _forgeHud);
                SetRef(so, "shopToForgeArrow", _shopArrow);
                SetRef(so, "forgeToShopArrow", _forgeArrow);
                SetRef(so, "transitionBlocker", _transitionBlocker);
                SetRef(so, "vignette", _vignette);
                SetRef(so, "transitionBeam", _seamBeam);
                SetRef(so, "smithyController", _controller);

                var prop = so.FindProperty("parallaxLayers");
                prop.arraySize = _parallax.Count;
                for (int i = 0; i < _parallax.Count; i++)
                {
                    var el = prop.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("transform").objectReferenceValue = _parallax[i].layer;
                    el.FindPropertyRelative("multiplier").floatValue = _parallax[i].multiplier;
                }
            });
        }

        private static void WireStationSelector()
        {
            WireComponent(_stationSelector, so =>
            {
                SetRef(so, "controller", _controller);
                SetRef(so, "viewController", _viewController);
                SetRef(so, "prompt", _prompt);
                SetRef(so, "worldCamera", _mainCamera);
                var prop = so.FindProperty("stations");
                prop.arraySize = _stations.Count;
                for (int i = 0; i < _stations.Count; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = _stations[i];
            });
        }

        private static void WireCustomerPreview()
        {
            WireComponent(_customerPreview, so =>
            {
                SetRef(so, "customer", _customerView);
                SetRef(so, "entryPointLeft", _customerEntry);
                SetRef(so, "talkPoint", _customerTalk);
                SetRef(so, "exitPointLeft", _customerExit);
                SetBool(so, "autoStart", true);
            });
        }

        private static void BuildHud()
        {
            var hud = NewRect("HUD", _uiRoot);
            Stretch(hud);
            _hud = hud.gameObject.AddComponent<HudController>();

            var money = AddLabel(hud, "Золото: 0", -1, 1, 400, 40, 26, Accent, TextAlignmentOptions.Left);
            Anchor(money.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(400, 40));

            var objective = AddLabel(hud, "", 0, 1, 900, 40, 24, TextLight, TextAlignmentOptions.Center);
            Anchor(objective.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1100, 40));

            var invBtn = AddButton(hud, "Инвентарь (I)", 0, 0, 240, 54);
            Anchor(invBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-300, -34), new Vector2(240, 54));
            var jrnBtn = AddButton(hud, "Журнал (J)", 0, 0, 240, 54);
            Anchor(jrnBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -34), new Vector2(240, 54));

            WireComponent(_hud, so =>
            {
                SetRef(so, "controller", _controller);
                SetRef(so, "inventory", _inventory);
                SetRef(so, "moneyText", money);
                SetRef(so, "objectiveText", objective);
                SetRef(so, "inventoryButton", invBtn);
                SetRef(so, "journalButton", jrnBtn);
            });
        }

        private static void BuildPrompt()
        {
            var root = NewRect("InteractionPrompt", _uiRoot);
            Anchor(root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(700, 60));
            AddImage(root, new Color(0.02f, 0.018f, 0.016f, 0.7f), false);
            var label = AddLabel(root, "", 0, 0, 680, 50, 26, TextLight, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 10, 0, 10, 0);
            _prompt = root.gameObject.AddComponent<InteractionPromptController>();
            WireComponent(_prompt, so =>
            {
                SetRef(so, "root", root.gameObject);
                SetRef(so, "label", label);
            });
        }

        private static void BuildNotification()
        {
            var root = NewRect("NotificationPanel", _uiRoot);
            Anchor(root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(900, 60));
            AddImage(root, new Color(0.02f, 0.018f, 0.016f, 0.8f), false);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            var text = AddLabel(root, "", 0, 0, 880, 50, 26, Accent, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 10, 0, 10, 0);
            _notifications = root.gameObject.AddComponent<NotificationController>();
            WireComponent(_notifications, so =>
            {
                SetRef(so, "group", cg);
                SetRef(so, "text", text);
            });
        }

        // =====================================================================
        //  Generic panel scaffold
        // =====================================================================

        private static T AddPanel<T>(PanelId id, string title, Vector2 size, out RectTransform window,
            float dimAlpha = -1f, Sprite windowSprite = null)
            where T : SmithyPanel
        {
            var panel = NewRect("Panel_" + id, _uiRoot);
            Stretch(panel);
            var dim = PanelDim;
            if (dimAlpha >= 0f) dim.a = dimAlpha;
            AddImage(panel, dim, true); // dim + block clicks

            window = NewRect("Window", panel);
            Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            if (windowSprite != null)
            {
                var wimg = AddImage(window, Color.white, true);
                wimg.sprite = windowSprite;
            }
            else AddImage(window, Window, true);

            var titleRt = AddLabel(window, title, 0, 0, size.x - 60, 54, 40, Accent, TextAlignmentOptions.Center);
            Anchor(titleRt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(size.x - 60, 54));

            var comp = panel.gameObject.AddComponent<T>();
            _panels.Add(comp);
            return comp;
        }

        private static void FinishPanel(SmithyPanel comp, PanelId id, GameObject firstSelected)
        {
            WireComponent(comp, so =>
            {
                SetRef(so, "controller", _controller);
                SetEnum(so, "panelId", (int)id);
                SetRef(so, "firstSelected", firstSelected);
            });
        }

        // =====================================================================
        //  Individual panels
        // =====================================================================

        private static void BuildInventoryPanel()
        {
            var p = AddPanel<InventoryPanelController>(PanelId.Inventory, "Инвентарь", new Vector2(1100, 820), out var w);
            var content = AddScroll(w, 0, 30, 1000, 620);
            var back = AddButton(w, "Назад", 0, -360, 260, 60);
            WireComponent(p, so =>
            {
                SetRef(so, "listContent", content);
                SetRef(so, "font", _font);
                SetRef(so, "backButton", back);
            });
            FinishPanel(p, PanelId.Inventory, back.gameObject);
        }

        private static Image AddUiSprite(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = NewChild(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : _white;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        private static void BuildFoundryPanel()
        {
            // Near-full-screen foundry with a wooden workbench skin.
            var p = AddPanel<FoundryPanelController>(PanelId.Foundry, "Плавильня", new Vector2(1840, 1010), out var w, 0.9f, _uiWorkbench);

            // ---- Left column: ore selection (extensible list) ----
            AddLabel(w, "Руды", -650, 380, 320, 50, 34, Accent, TextAlignmentOptions.Center);
            var materialsG = NewChild(w, "MaterialsGroup", new Vector2(-650, 20), new Vector2(360, 760));
            var bronzeCard = AddButton(materialsG, "", 0, 220, 300, 320);
            var brt = bronzeCard.GetComponent<RectTransform>();
            AddSpriteChild(brt, "Icon", new Vector2(0, 46), new Vector2(170, 170), _uiCrucible);
            AddLabel(brt, "Бронза", 0, -88, 260, 40, 30, TextLight, TextAlignmentOptions.Center);
            AddLabel(brt, "Тугоплавкая, надёжная", 0, -128, 260, 30, 20, TextDim, TextAlignmentOptions.Center);

            // ---- One unified foundry view: central hanging crucible + mould below it ----
            var foundryG = NewChild(w, "FoundryGroup", new Vector2(140, 10), new Vector2(1500, 940));

            // The crucible the player GRABS and TILTS (top-centre). Pivot near the spout so
            // the lip moves believably. Melts the ore (gauge + molten fill), then pours.
            // Upright pot while melting; it swaps to a forward-tilted OPEN-BOWL pose while
            // pouring (metal down the centre into the mould below) — no sideways rotation.
            var pourCrucible = AddUiSprite(foundryG, "Crucible", new Vector2(0, 170), new Vector2(430, 350), _uiCrucible, Color.white, true);
            pourCrucible.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var crucibleMolten = AddUiSprite(pourCrucible.rectTransform, "Molten", new Vector2(0, 6), new Vector2(280, 190), _uiCrucibleMolten, new Color(1f, 0.7f, 0.3f, 1f));
            crucibleMolten.raycastTarget = false;
            var pourOrigin = NewChild(pourCrucible.rectTransform, "PourOrigin", new Vector2(0, -140), new Vector2(12, 12));
            var tilt = pourCrucible.gameObject.AddComponent<CrucibleTiltControl>();
            WireComponent(tilt, so => { SetRef(so, "pivot", pourCrucible.rectTransform); SetRef(so, "pourOrigin", pourOrigin); });

            // Melt gauge (readout) + needle, floated ABOVE the pot so it doesn't cover it.
            var gauge = AddUiSprite(foundryG, "Gauge", new Vector2(0, 360), new Vector2(230, 135), _uiMeltGauge, Color.white);
            gauge.raycastTarget = false;
            var needleRt = NewChild(gauge.rectTransform, "Needle", new Vector2(0, -52), new Vector2(30, 128));
            needleRt.pivot = new Vector2(0.5f, 0.05f);
            var needleImg = needleRt.gameObject.AddComponent<Image>();
            needleImg.sprite = _uiGaugeNeedle != null ? _uiGaugeNeedle : _white; needleImg.raycastTarget = false; needleImg.preserveAspect = true;

            // Molten stream from the spout down into the mould (scaled/toggled while pouring).
            var stream = AddUiSprite(foundryG, "Stream", new Vector2(0, -40), new Vector2(46, 300), _uiStream, Color.white);
            var spill = AddUiSprite(foundryG, "SpillFlash", new Vector2(60, -170), new Vector2(190, 140), _uiSparks, new Color(1f, 0.55f, 0.2f, 0f));
            spill.gameObject.SetActive(false);

            // ---- Mould below (appears when pouring) ----
            var mouldO = NewChild(foundryG, "Mould", new Vector2(0, -320), new Vector2(360, 560));
            AddUiSprite(mouldO, "MoldClosed", new Vector2(0, 0), new Vector2(300, 540), _uiMoldClosed, Color.white);
            AddUiSprite(mouldO, "Inlet", new Vector2(0, 250), new Vector2(90, 30), _white, new Color(1f, 0.8f, 0.4f, 0.18f));
            var maskRt = NewChild(mouldO, "MoldMask", new Vector2(0, 0), new Vector2(220, 500));
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.sprite = _uiMoldMask != null ? _uiMoldMask : _white; maskImg.color = Color.white;
            var mask = maskRt.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            var fillRt = NewRect("MoltenFill", maskRt);
            fillRt.anchorMin = new Vector2(0f, 0f); fillRt.anchorMax = new Vector2(1f, 0f); fillRt.pivot = new Vector2(0.5f, 0f);
            fillRt.anchoredPosition = Vector2.zero; fillRt.sizeDelta = Vector2.zero;
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.sprite = _uiMoldFill != null ? _uiMoldFill : _white; fillImg.color = new Color(1f, 0.6f, 0.2f, 1f); fillImg.raycastTarget = false;
            var castBlade = AddUiSprite(mouldO, "CastBlade", new Vector2(40, 0), new Vector2(560, 190), _uiCastBlade, Color.white);

            // ---- Fire under the crucible (behind it) — burns while melting ----
            var fire = AddUiSprite(foundryG, "Fire", new Vector2(0, 55), new Vector2(300, 330), _uiFire, Color.white);
            fire.raycastTarget = false;
            fire.rectTransform.SetAsFirstSibling(); // render behind the pot

            // ---- Temperature board next to the crucible (left) ----
            var board = NewChild(foundryG, "TempBoard", new Vector2(-360, 250), new Vector2(300, 200));
            var plate = board.gameObject.AddComponent<Image>();
            plate.sprite = _white; plate.color = new Color(0.12f, 0.10f, 0.09f, 0.92f);
            AddLabel(board, "Температура", 0, 70, 280, 34, 24, TextDim, TextAlignmentOptions.Center);
            var temp = AddLabel(board, "", 0, 16, 280, 62, 46, Accent, TextAlignmentOptions.Center);
            var tempBarBg = AddUiSprite(board, "TempBarBg", new Vector2(0, -62), new Vector2(250, 26), _white, new Color(0f, 0f, 0f, 0.5f));
            tempBarBg.raycastTarget = false;
            var tempBar = AddUiSprite(tempBarBg.rectTransform, "Fill", Vector2.zero, new Vector2(250, 26), _white, new Color(1f, 0.5f, 0.15f, 1f));
            tempBar.type = Image.Type.Filled; tempBar.fillMethod = Image.FillMethod.Horizontal; tempBar.fillOrigin = 0; tempBar.fillAmount = 0.3f; tempBar.raycastTarget = false;

            // ---- Firewood pile: DRAG the top log into the fire to raise the heat ----
            var logPile = NewChild(foundryG, "LogPile", new Vector2(-360, -150), new Vector2(260, 210));
            var l1 = AddUiSprite(logPile, "Log1", new Vector2(-12, -36), new Vector2(210, 68), _uiLog, Color.white); l1.raycastTarget = false; l1.rectTransform.localRotation = Quaternion.Euler(0, 0, 6f);
            var l2 = AddUiSprite(logPile, "Log2", new Vector2(16, 12), new Vector2(210, 68), _uiLog, new Color(0.9f, 0.9f, 0.9f)); l2.raycastTarget = false; l2.rectTransform.localRotation = Quaternion.Euler(0, 0, -8f);
            var dragLog = AddUiSprite(logPile, "DragLog", new Vector2(-6, 58), new Vector2(196, 64), _uiLog, Color.white);
            dragLog.raycastTarget = true; dragLog.rectTransform.localRotation = Quaternion.Euler(0, 0, 3f);
            var logDrag = dragLog.gameObject.AddComponent<FoundryLogDrag>();
            WireComponent(logDrag, so => { SetRef(so, "fireTarget", fire.rectTransform); SetRef(so, "foundry", p); });
            AddLabel(logPile, "Тащите бревно в огонь", 0, -116, 340, 34, 22, Accent, TextAlignmentOptions.Center);

            // Labels + action buttons (right side / bottom).
            var state = AddLabel(foundryG, "", 470, 250, 560, 60, 40, TextLight, TextAlignmentOptions.Center);
            var pourStatus = AddLabel(foundryG, "", 470, 100, 620, 50, 28, Accent, TextAlignmentOptions.Center);
            var pourFinish = AddButton(foundryG, "Закрыть форму", 470, -120, 380, 74);
            var extract = AddButton(foundryG, "Забрать заготовку", 470, -120, 380, 74);

            var status = AddLabel(w, "", 140, -430, 1300, 44, 24, TextDim, TextAlignmentOptions.Center);
            var start = AddButton(w, "Начать плавку (3 бронзы)", 0, -430, 520, 74); // hidden; kept for wiring
            var back = AddButton(w, "Назад", 780, -430, 200, 64);

            WireComponent(p, so =>
            {
                SetRef(so, "materialsGroup", materialsG.gameObject);
                SetRef(so, "bronzeCard", bronzeCard);
                SetRef(so, "foundryGroup", foundryG.gameObject);
                SetRef(so, "mouldObject", mouldO.gameObject);
                SetRef(so, "tempLabel", temp);
                SetRef(so, "tempBar", tempBar);
                SetRef(so, "tempBoardObject", board.gameObject);
                SetRef(so, "fireObject", fire.gameObject);
                SetRef(so, "fireImage", fire);
                SetRef(so, "logPileObject", logPile.gameObject);
                SetRef(so, "stateLabel", state);
                SetRef(so, "statusLabel", status);
                SetRef(so, "crucibleMoltenImage", crucibleMolten);
                SetRef(so, "crucibleMoltenPour", crucibleMolten);
                SetRef(so, "crucibleImage", pourCrucible);
                SetRef(so, "crucibleMeltSprite", _uiCrucible);
                SetRef(so, "cruciblePourSprite", _uiPourCrucibleFwd);
                SetRef(so, "gaugeObject", gauge.gameObject);
                SetRef(so, "gaugeNeedle", needleRt);
                SetRef(so, "startButton", start);
                SetRef(so, "crucibleTilt", tilt);
                SetRef(so, "spillFlash", spill);
                SetRef(so, "pourFinishButton", pourFinish);
                SetRef(so, "mouldFill", fillRt);
                SetRef(so, "mouldFillImage", fillImg);
                SetRef(so, "streamObject", stream.gameObject);
                SetRef(so, "pourCrucible", pourCrucible.rectTransform);
                SetRef(so, "pourStatus", pourStatus);
                SetFloat(so, "mouldFillMaxHeight", 480f);
                SetRef(so, "castBladeObject", castBlade.gameObject);
                SetRef(so, "extractButton", extract);
                SetRef(so, "backButton", back);
            });
            FinishPanel(p, PanelId.Foundry, bronzeCard.gameObject);
        }

        private static void BuildAnvilPanel()
        {
            var p = AddPanel<AnvilPanelController>(PanelId.Anvil, "Наковальня — ковка кромки", new Vector2(1440, 900), out var w);

            // Anvil backdrop + the live blade mesh on top.
            AddUiSprite(w, "AnvilArt", new Vector2(0, -170), new Vector2(560, 300), _fAnvil, Color.white);
            var meshRt = NewChild(w, "BladeMesh", new Vector2(0, 60), new Vector2(1120, 320));
            meshRt.gameObject.AddComponent<CanvasRenderer>(); // Graphic needs this; add explicitly so the built scene never lacks it
            var mesh = meshRt.gameObject.AddComponent<CastBladeMeshView>();
            mesh.color = new Color(0.78f, 0.55f, 0.30f, 1f);
            WireComponent(mesh, so => SetRef(so, "bladeTexture", _uiCastTexture));

            // Faint section grid over the blade so the local zones read (marks, not a debug editor).
            var grid = AddUiSprite(meshRt, "SectionGrid", Vector2.zero, new Vector2(1080, 300), _uiBladeGrid, new Color(1f, 0.96f, 0.86f, 0.15f));
            grid.rectTransform.anchorMin = Vector2.zero; grid.rectTransform.anchorMax = Vector2.one;
            grid.rectTransform.offsetMin = new Vector2(40, 30); grid.rectTransform.offsetMax = new Vector2(-40, -30);
            grid.preserveAspect = false;

            var hammer = AddUiSprite(w, "Hammer", new Vector2(0, 280), new Vector2(150, 210), _uiHammer, Color.white);
            var sparks = AddUiSprite(w, "Sparks", new Vector2(0, 80), new Vector2(150, 150), _uiSparks, Color.white);
            sparks.gameObject.SetActive(false);

            var status = AddLabel(w, "", 0, 262, 1200, 40, 24, TextLight, TextAlignmentOptions.Center);

            // Four readable quality bars (exact values still evaluated in code).
            var edgeBar = AddStatBar(w, -510, -300, 280, 26, "Кромка", Accent);
            var straightBar = AddStatBar(w, -170, -300, 280, 26, "Ровность", new Color(0.55f, 0.78f, 0.45f));
            var balanceBar = AddStatBar(w, 170, -300, 280, 26, "Баланс", new Color(0.5f, 0.72f, 0.85f));
            var overworkBar = AddStatBar(w, 510, -300, 280, 26, "Перековка", new Color(0.82f, 0.34f, 0.28f));
            var quality = AddLabel(w, "", 0, -352, 1300, 34, 22, TextDim, TextAlignmentOptions.Center);

            var finish = AddButton(w, "Завершить ковку", -160, -410, 340, 66);
            var back = AddButton(w, "Назад", 200, -410, 220, 66);

            WireComponent(p, so =>
            {
                SetRef(so, "meshView", mesh);
                SetRef(so, "hammer", hammer.rectTransform);
                SetRef(so, "sparks", sparks.rectTransform);
                SetRef(so, "statusLabel", status);
                SetRef(so, "qualityLabel", quality);
                SetRef(so, "edgeBar", edgeBar);
                SetRef(so, "straightBar", straightBar);
                SetRef(so, "balanceBar", balanceBar);
                SetRef(so, "overworkBar", overworkBar);
                SetRef(so, "finishButton", finish);
                SetRef(so, "backButton", back);
            });
            FinishPanel(p, PanelId.Anvil, finish.gameObject);
        }

        /// <summary>A labelled horizontal fill bar; returns the fill Image (set fillAmount 0..1).</summary>
        private static Image AddStatBar(RectTransform parent, float x, float y, float w, float h, string label, Color fillColor)
        {
            AddLabel(parent, label, x, y + 32, w, 28, 20, TextDim, TextAlignmentOptions.Center);
            var barRt = NewChild(parent, "Bar_" + label, new Vector2(x, y), new Vector2(w, h));
            AddImage(barRt, new Color(0.05f, 0.04f, 0.03f, 0.92f), false);
            var fillRt = NewRect("Fill", barRt);
            Stretch(fillRt, 2, 2, 2, 2);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = _white; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left; fill.color = fillColor; fill.fillAmount = 0f;
            fill.raycastTarget = false;
            return fill;
        }

        private static void BuildAssemblyPanel()
        {
            // Full-screen overlay controls ONLY — the sword + parts live in the world-space
            // AssemblyStage (physics), rendered by AssemblyCamera behind this panel. The
            // panel has NO background graphic so the workbench shows through and empty-area
            // clicks reach the physics stage (not swallowed by a raycast blocker).
            var panel = NewRect("Panel_" + PanelId.Assembly, _uiRoot);
            Stretch(panel);
            var p = panel.gameObject.AddComponent<AssemblyPanelController>();
            _panels.Add(p);
            var w = panel;

            var title = AddLabel(w, "Сборка меча", 0, 0, 700, 60, 40, Accent, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-260, -50), new Vector2(700, 60));

            // Right-side parts tray (~28% width, full height). Cards are dragged out to spawn parts.
            var tray = NewRect("PartsTray", w);
            tray.anchorMin = new Vector2(1, 0); tray.anchorMax = new Vector2(1, 1); tray.pivot = new Vector2(1, 0.5f);
            tray.sizeDelta = new Vector2(540, -30); tray.anchoredPosition = new Vector2(-10, 0);
            AddImage(tray, new Color(0.14f, 0.10f, 0.06f, 0.55f), true); // blocks world-pick behind the tray
            AddLabel(tray, "Детали", 0, 350, 480, 44, 30, Accent, TextAlignmentOptions.Center);

            var guardLabel = AddLabel(tray, "Гарда", -160, 250, 300, 34, 24, TextLight, TextAlignmentOptions.Left);
            var guardItems = AddCatalogRow(tray, p, 1, _uiGuards, 190);
            var handleLabel = AddLabel(tray, "Рукоять", -160, 60, 300, 34, 24, TextLight, TextAlignmentOptions.Left);
            var handleItems = AddCatalogRow(tray, p, 2, _uiHandles, 0);
            var pommelLabel = AddLabel(tray, "Навершие", -160, -130, 300, 34, 24, TextLight, TextAlignmentOptions.Left);
            var pommelItems = AddCatalogRow(tray, p, 3, _uiPommels, -190);

            var status = AddLabel(w, "", 0, 0, 1300, 44, 26, Accent, TextAlignmentOptions.Center);
            Anchor(status.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-260, 128), new Vector2(1300, 44));
            var assemble = AddButton(w, "Завершить меч", 0, 0, 340, 74);
            Anchor(assemble.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-450, 56), new Vector2(340, 74));
            var back = AddButton(w, "Назад", 0, 0, 180, 74);
            Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-120, 56), new Vector2(180, 74));

            var hudHide = new[]
            {
                _hud != null ? _hud.gameObject : null,
                _shopHud != null ? _shopHud.gameObject : null,
                _forgeHud != null ? _forgeHud.gameObject : null
            };

            WireComponent(p, so =>
            {
                SetRef(so, "guardLabel", guardLabel);
                SetRef(so, "handleLabel", handleLabel);
                SetRef(so, "pommelLabel", pommelLabel);
                SetRef(so, "statusLabel", status);
                SetRef(so, "assembleButton", assemble);
                SetRef(so, "backButton", back);
                SetRef(so, "physicsWorld", _asmWorld);
                SetRef(so, "guardPhys", _asmGuard);
                SetRef(so, "handlePhys", _asmHandle);
                SetRef(so, "pommelPhys", _asmPommel);
                SetComponentArray(so, "guardItems", guardItems);
                SetComponentArray(so, "handleItems", handleItems);
                SetComponentArray(so, "pommelItems", pommelItems);
                SetSpriteArray(so, "guardSprites", _uiGuards);
                SetSpriteArray(so, "handleSprites", _uiHandles);
                SetSpriteArray(so, "pommelSprites", _uiPommels);
                SetGameObjectArray(so, "hideDuringAssembly", hudHide);
            });
            // Releasing a dragged part back over the tray cancels it.
            WireComponent(_asmWorld, so => SetRef(so, "catalogRect", tray));
            FinishPanel(p, PanelId.Assembly, assemble.gameObject);
        }

        /// <summary>A row of draggable catalog cards, one per sprite variant of a slot.</summary>
        private static AssemblyPartCatalogItem[] AddCatalogRow(RectTransform tray, AssemblyPanelController ctrl, int slot, Sprite[] sprites, float y)
        {
            var items = new AssemblyPartCatalogItem[sprites.Length];
            float startX = 40f;
            for (int i = 0; i < sprites.Length; i++)
            {
                var card = NewChild(tray, "Card_" + slot + "_" + i, new Vector2(startX + i * 165f, y), new Vector2(150, 130));
                var bg = card.gameObject.AddComponent<Image>();
                bg.color = new Color(0.10f, 0.09f, 0.08f, 0.7f); bg.raycastTarget = true;
                var icon = AddSpriteChild(card, "Icon", Vector2.zero, new Vector2(130, 110), sprites[i]);
                var mark = AddImageChild(card, "InstalledMark", new Vector2(52, 48), new Vector2(26, 26), new Color(0.45f, 0.8f, 0.4f));
                mark.gameObject.SetActive(false);
                var item = card.gameObject.AddComponent<AssemblyPartCatalogItem>();
                WireComponent(item, so =>
                {
                    SetRef(so, "controller", ctrl);
                    SetEnum(so, "slot", slot);
                    SetInt(so, "variantIndex", i);
                    SetRef(so, "icon", icon);
                    SetRef(so, "background", bg);
                    SetRef(so, "installedMark", mark.gameObject);
                });
                items[i] = item;
            }
            return items;
        }

        private static void SetComponentArray(SerializedObject so, string prop, AssemblyPartCatalogItem[] arr)
        {
            var pr = so.FindProperty(prop);
            if (pr == null) { Debug.LogWarning($"[SmithySceneBuilder] Missing array '{prop}'"); return; }
            pr.arraySize = arr.Length;
            for (int i = 0; i < arr.Length; i++) pr.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
        }

        private static void SetInt(SerializedObject so, string prop, int v)
        {
            var pr = so.FindProperty(prop);
            if (pr != null) pr.intValue = v;
        }

        private static void SetGameObjectArray(SerializedObject so, string prop, GameObject[] arr)
        {
            var pr = so.FindProperty(prop);
            if (pr == null) { Debug.LogWarning($"[SmithySceneBuilder] Missing array '{prop}'"); return; }
            pr.arraySize = arr.Length;
            for (int i = 0; i < arr.Length; i++) pr.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
        }

        // =====================================================================
        //  World-space physics assembly stage (rendered by its own camera)
        // =====================================================================

        private static void BuildAssemblyPhysicsStage()
        {
            const float sx = 300f;        // far from shop/forge so the main camera never sees it
            const float shoulderY = 1.0f; // where the guard rests (blade base)
            const float tangTopY = 3.6f;

            var rootGo = new GameObject("AssemblyStage");
            rootGo.transform.position = new Vector3(sx, 0f, 0f);
            var root = rootGo.transform;

            // Dedicated camera — off until Assembly opens, then draws full-screen over the smithy.
            var camGo = new GameObject("AssemblyCamera", typeof(Camera));
            camGo.transform.SetParent(root, false);
            camGo.transform.localPosition = new Vector3(0f, 0.3f, -10f);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.075f, 0.05f);
            cam.depth = 10f; cam.enabled = false;

            var bg = AddWorldSprite(root, "Workbench", new Vector3(0f, 0.3f, 5f), _uiWorkbench, new Color(0.5f, 0.42f, 0.32f), -50);
            bg.transform.localScale = new Vector3(2.6f, 2.6f, 1f);

            // Blade (tip DOWN via 180°); its shoulder collider stops the guard.
            var blade = AddWorldSprite(root, "Blade", new Vector3(0f, shoulderY - 2.9f, 2f), _uiBladeFinal, new Color(0.82f, 0.6f, 0.34f), 0);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            AddStaticBox(root, "Shoulder", new Vector3(0f, shoulderY - 0.15f, 0f), new Vector2(3.4f, 0.3f));

            // Tang: static collider + visual rod rising from the shoulder. A vertical CAPSULE
            // (rounded top) so a roughly-centred part slides onto it instead of catching the tip.
            var tangGo = new GameObject("Tang");
            tangGo.transform.SetParent(root, false);
            tangGo.transform.localPosition = new Vector3(0f, (shoulderY + tangTopY) * 0.5f, 1f);
            var tangRb = tangGo.AddComponent<Rigidbody2D>(); tangRb.bodyType = RigidbodyType2D.Static;
            var tangCol = tangGo.AddComponent<CapsuleCollider2D>();
            tangCol.direction = CapsuleDirection2D.Vertical;
            tangCol.size = new Vector2(0.12f, tangTopY - shoulderY);
            var tangVis = AddWorldSprite(tangGo.transform, "Visual", new Vector3(0f, 0f, -0.1f), _uiTang, Color.white, 1);
            tangVis.transform.localScale = new Vector3(0.34f, (tangTopY - shoulderY) / 4.4f, 1f);

            // Table boundaries (invisible) so nothing flies off-stage.
            AddStaticBox(root, "BoundLeft", new Vector3(-8.5f, -0.5f, 0f), new Vector2(0.6f, 14f));
            AddStaticBox(root, "BoundRight", new Vector3(8.5f, -0.5f, 0f), new Vector2(0.6f, 14f));
            AddStaticBox(root, "BoundBottom", new Vector3(0f, -5.6f, 0f), new Vector2(18f, 0.6f));

            var spawn = new GameObject("SpawnPoint").transform;
            spawn.SetParent(root, false);
            spawn.localPosition = new Vector3(0f, 4.0f, 0f);

            // Physics parts — split colliders leave a WIDE central channel (forgiving of a
            // near-centred drop, catching a clearly-off one). Tang collider is only ~0.12 wide.
            // Guard: WIDE central opening — the tang passes freely through it and the guard
            // simply rests flat on the shoulder (so it doesn't tip on the thin tang tip).
            _asmGuard = AddPhysicsPart(root, "GuardPhys", _uiGuards[0], 1, new[]
            {
                (new Vector2(-0.95f, 0f), new Vector2(0.8f, 0.30f)),   // inner edge at ±0.55 → 1.1 gap
                (new Vector2(0.95f, 0f), new Vector2(0.8f, 0.30f))
            });
            _asmHandle = AddPhysicsPart(root, "HandlePhys", _uiHandles[0], 2, new[]
            {
                (new Vector2(-0.31f, 0f), new Vector2(0.30f, 1.9f)),   // inner edge at ±0.16 → 0.32 gap
                (new Vector2(0.31f, 0f), new Vector2(0.30f, 1.9f))
            });
            _asmPommel = AddPhysicsPart(root, "PommelPhys", _uiPommels[0], 3, new[]
            {
                (new Vector2(-0.36f, 0f), new Vector2(0.36f, 0.55f)),  // inner edge at ±0.18 → 0.36 gap
                (new Vector2(0.36f, 0f), new Vector2(0.36f, 0.55f))
            });

            _asmWorld = rootGo.AddComponent<AssemblyPhysicsWorld>();
            WireComponent(_asmWorld, so =>
            {
                SetRef(so, "stageCamera", cam);
                SetRef(so, "spawnPoint", spawn);
                SetFloat(so, "tangAxisX", sx);
                SetFloat(so, "shoulderY", shoulderY);
                SetFloat(so, "bladeWorldHeight", 5.8f);
            });
        }

        private static SpriteRenderer AddWorldSprite(Transform parent, string name, Vector3 localPos, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : _white;
            sr.color = color; sr.sortingOrder = order;
            if (_spriteMat != null) sr.sharedMaterial = _spriteMat;
            return sr;
        }

        private static void AddStaticBox(Transform parent, string name, Vector3 localPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
            var col = go.AddComponent<BoxCollider2D>(); col.size = size;
        }

        private static AssemblyPhysicsPart AddPhysicsPart(Transform parent, string name, Sprite sprite, int slot,
            (Vector2 offset, Vector2 size)[] cols)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 6f, 3f); // parked above until spawned
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : _white; sr.color = Color.white; sr.sortingOrder = 5;
            if (_spriteMat != null) sr.sharedMaterial = _spriteMat;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic; rb.gravityScale = 1f; rb.mass = 1f;
            rb.linearDamping = 0.6f; rb.angularDamping = 3.0f; // tame chaotic spinning/flipping
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.simulated = false; // dormant until the part is spawned in play
            foreach (var (offset, size) in cols)
            {
                var colGo = new GameObject("Col");
                colGo.transform.SetParent(go.transform, false);
                var bc = colGo.AddComponent<BoxCollider2D>(); bc.offset = offset; bc.size = size;
            }
            var part = go.AddComponent<AssemblyPhysicsPart>();
            WireComponent(part, so => { SetEnum(so, "slot", slot); SetRef(so, "body", rb); SetRef(so, "sprite", sr); });
            go.SetActive(false);
            return part;
        }

        /// <summary>An invisible contact marker (RectTransform only) — a socket point.</summary>
        private static RectTransform AddSocket(RectTransform parent, string name, Vector2 pos)
            => NewChild(parent, name, pos, new Vector2(24, 24));

        /// <summary>A static weapon-part Image (non-draggable) with bottom/top contact sockets, for WeaponVisualView.</summary>
        private static (Image img, RectTransform bottom, RectTransform top) AddViewPart(
            RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, float contactHalf)
        {
            var img = AddSpriteChild(parent, name, pos, size, sprite);
            var rt = img.rectTransform;
            var bottom = AddSocket(rt, "BottomSocket", new Vector2(0, -contactHalf));
            var top = AddSocket(rt, "TopSocket", new Vector2(0, contactHalf));
            return (img, bottom, top);
        }

        /// <summary>
        /// A read-only forged-blade mesh (reuses <see cref="CastBladeMeshView"/>). The rect
        /// is built horizontal (tang→tip along X) then rotated to the requested orientation
        /// — 90° gives a tip-up vertical sword — without touching the blade data.
        /// </summary>
        private static CastBladeMeshView AddBladeMesh(RectTransform parent, string name, Vector2 pos, Vector2 sizeBeforeRot, float rotationZ)
        {
            var rt = NewChild(parent, name, pos, sizeBeforeRot);
            rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            rt.gameObject.AddComponent<CanvasRenderer>(); // Graphic needs one; add explicitly
            var mesh = rt.gameObject.AddComponent<CastBladeMeshView>();
            mesh.color = new Color(0.82f, 0.6f, 0.34f, 1f);
            mesh.raycastTarget = false;
            WireComponent(mesh, so => SetRef(so, "bladeTexture", _uiCastTexture));
            return mesh;
        }

        /// <summary>Adds an Image showing a real sprite (transparent art), non-raycast.</summary>
        private static Image AddSpriteChild(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite)
        {
            var rt = NewChild(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : _white;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        private static void SetSpriteArray(SerializedObject so, string prop, Sprite[] sprites)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"[SmithySceneBuilder] Missing array '{prop}'"); return; }
            p.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static void BuildJournalPanel()
        {
            var p = AddPanel<ResearchJournalPanelController>(PanelId.Journal, "Журнал кузнеца", new Vector2(1400, 860), out var w);
            var list = AddScroll(NewChild(w, "ListArea", new Vector2(-460, 0), new Vector2(380, 620)), 0, 0, 380, 620);
            var title = AddLabel(w, "", 250, 320, 640, 40, 30, Accent, TextAlignmentOptions.Left);
            var knowledge = AddLabel(w, "", 250, 275, 640, 34, 22, TextDim, TextAlignmentOptions.Left);
            AddLabel(w, "Плавка", 250, 230, 300, 30, 22, TextDim, TextAlignmentOptions.Left);
            var meltBar = AddResearchBar(w, 250, 195, 620, 40);
            AddLabel(w, "Ковочный нагрев", 250, 150, 400, 30, 22, TextDim, TextAlignmentOptions.Left);
            var forgeBar = AddResearchBar(w, 250, 115, 620, 40);
            var props = AddLabel(w, "", 250, -60, 640, 200, 22, TextLight, TextAlignmentOptions.Left);
            var quench = AddLabel(w, "", 250, -260, 640, 120, 22, TextLight, TextAlignmentOptions.Left);
            var back = AddButton(w, "Назад", 0, -390, 240, 54);
            WireComponent(p, so =>
            {
                SetRef(so, "materialListContent", list);
                SetRef(so, "font", _font);
                SetRef(so, "titleText", title);
                SetRef(so, "knowledgeText", knowledge);
                SetRef(so, "propsText", props);
                SetRef(so, "quenchText", quench);
                SetRef(so, "meltingBar", meltBar);
                SetRef(so, "forgeBar", forgeBar);
                SetRef(so, "backButton", back);
            });
            FinishPanel(p, PanelId.Journal, back.gameObject);
        }

        private static void BuildItemResultPanel()
        {
            var p = AddPanel<ItemResultPanelController>(PanelId.ItemResult, "Готовое оружие", new Vector2(1200, 840), out var w);

            // Real weapon visual — the exact crafted sword rebuilt via WeaponVisualView,
            // tip DOWN, hilt stacked by the same socket contact as Assembly (no gaps).
            var visualRt = NewChild(w, "WeaponVisual", new Vector2(-360, 20), new Vector2(320, 640));
            var vBlade = AddBladeMesh(visualRt, "BladeMesh", new Vector2(0, -30), new Vector2(420, 100), 270f);
            var vTang = AddUiSprite(visualRt, "Tang", new Vector2(0, 182), new Vector2(20, 118), _uiTang, Color.white);
            vTang.preserveAspect = false;
            var bladeTop = AddSocket(visualRt, "BladeTopSocket", new Vector2(0, -30f + (210f - 40f)));
            var g = AddViewPart(visualRt, "Guard", new Vector2(0, 150), new Vector2(120, 32), _uiGuards[0], 10f);
            var h = AddViewPart(visualRt, "Handle", new Vector2(0, 180), new Vector2(32, 88), _uiHandles[0], 38f);
            var pm = AddViewPart(visualRt, "Pommel", new Vector2(0, 210), new Vector2(40, 40), _uiPommels[0], 13f);
            var view = visualRt.gameObject.AddComponent<WeaponVisualView>();
            WireComponent(view, so =>
            {
                SetRef(so, "weaponRoot", visualRt);
                SetRef(so, "bladeMesh", vBlade);
                SetRef(so, "tangVisual", vTang);
                SetRef(so, "bladeTopSocket", bladeTop);
                SetRef(so, "guardImage", g.img);
                SetRef(so, "guardBottom", g.bottom);
                SetRef(so, "guardTop", g.top);
                SetRef(so, "handleImage", h.img);
                SetRef(so, "handleBottom", h.bottom);
                SetRef(so, "handleTop", h.top);
                SetRef(so, "pommelImage", pm.img);
                SetRef(so, "pommelBottom", pm.bottom);
                SetRef(so, "bladeTexture", _uiCastTexture);
                SetSpriteArray(so, "guardSprites", _uiGuards);
                SetSpriteArray(so, "handleSprites", _uiHandles);
                SetSpriteArray(so, "pommelSprites", _uiPommels);
                SetFloat(so, "offsetScale", 420f); // this view's blade display height (denormalises offsets)
            });

            var nameField = AddInputField(w, 200, 320, 640, 56);
            var stats = AddLabel(w, "", 200, 40, 640, 400, 24, TextLight, TextAlignmentOptions.Left);
            var defects = AddLabel(w, "", 200, -240, 640, 160, 22, TextDim, TextAlignmentOptions.Left);

            var keep = AddButton(w, "Оставить", -300, -360, 260, 60);
            var equip = AddButton(w, "Экипировать", 0, -360, 260, 60);
            var store = AddButton(w, "В хранилище", 300, -360, 260, 60);
            var back = AddButton(w, "Закрыть", 520, -360, 180, 60);

            WireComponent(p, so =>
            {
                SetRef(so, "nameField", nameField);
                SetRef(so, "statsText", stats);
                SetRef(so, "defectsText", defects);
                SetRef(so, "weaponVisual", view);
                SetRef(so, "keepButton", keep);
                SetRef(so, "equipButton", equip);
                SetRef(so, "storeButton", store);
                SetRef(so, "backButton", back);
            });
            FinishPanel(p, PanelId.ItemResult, keep.gameObject);
        }

        private static void BuildPausePanel()
        {
            var p = AddPanel<PausePanelController>(PanelId.Pause, "Пауза", new Vector2(700, 640), out var w);
            var resume = AddButton(w, "Продолжить", 0, 150, 460, 66);
            var save = AddButton(w, "Сохранить", 0, 70, 460, 66);
            var settings = AddButton(w, "Настройки", 0, -10, 460, 66);
            var menu = AddButton(w, "В главное меню", 0, -90, 460, 66);
            WireComponent(p, so =>
            {
                SetRef(so, "resumeButton", resume);
                SetRef(so, "saveButton", save);
                SetRef(so, "settingsButton", settings);
                SetRef(so, "mainMenuButton", menu);
            });
            FinishPanel(p, PanelId.Pause, resume.gameObject);
        }

        private static void BuildSettingsPanel()
        {
            var p = AddPanel<SettingsMiniPanelController>(PanelId.Settings, "Настройки", new Vector2(900, 700), out var w);
            AddLabel(w, "Общая громкость", -250, 200, 340, 30, 22, TextDim, TextAlignmentOptions.Left);
            var master = AddSlider(w, 200, 200, 380, 28, 0f, 1f, 1f);
            AddLabel(w, "Музыка", -250, 130, 340, 30, 22, TextDim, TextAlignmentOptions.Left);
            var music = AddSlider(w, 200, 130, 380, 28, 0f, 1f, 0.8f);
            AddLabel(w, "Эффекты", -250, 60, 340, 30, 22, TextDim, TextAlignmentOptions.Left);
            var sfx = AddSlider(w, 200, 60, 380, 28, 0f, 1f, 0.9f);
            AddLabel(w, "Интенсивность эффектов", -250, -10, 340, 30, 22, TextDim, TextAlignmentOptions.Left);
            var effects = AddSlider(w, 200, -10, 380, 28, 0f, 1f, 1f);
            AddLabel(w, "Дрожание экрана", -250, -80, 340, 30, 22, TextDim, TextAlignmentOptions.Left);
            var shake = AddToggle(w, 200, -80);
            var back = AddButton(w, "Назад", 0, -260, 260, 60);
            WireComponent(p, so =>
            {
                SetRef(so, "settings", _settings);
                SetRef(so, "masterSlider", master);
                SetRef(so, "musicSlider", music);
                SetRef(so, "sfxSlider", sfx);
                SetRef(so, "effectsSlider", effects);
                SetRef(so, "shakeToggle", shake);
                SetRef(so, "backButton", back);
            });
            FinishPanel(p, PanelId.Settings, back.gameObject);
        }

        private static void BuildDebugPanel()
        {
            var p = AddPanel<DebugPanelController>(PanelId.Debug, "Отладка (Dev)", new Vector2(1000, 760), out var w);
            var giveBronze = AddButton(w, "+5 бронзы", -250, 220, 300, 56);
            var createCast = AddButton(w, "Создать заготовку", 100, 220, 300, 56);
            var completeForge = AddButton(w, "Завершить ковку", -250, 150, 300, 56);
            var openAnvil = AddButton(w, "Открыть наковальню", 100, 150, 300, 56);
            var resetS = AddButton(w, "Сбросить сессию", -250, 80, 300, 56);
            var output = AddLabel(w, "", 0, -120, 900, 260, 22, TextLight, TextAlignmentOptions.Left);
            var back = AddButton(w, "Назад", 0, -320, 240, 54);
            WireComponent(p, so =>
            {
                SetRef(so, "output", output);
                SetRef(so, "giveBronzeButton", giveBronze);
                SetRef(so, "createCastButton", createCast);
                SetRef(so, "completeForgeButton", completeForge);
                SetRef(so, "openAnvilButton", openAnvil);
                SetRef(so, "resetSessionButton", resetS);
                SetRef(so, "backButton", back);
            });
            FinishPanel(p, PanelId.Debug, back.gameObject);
        }

        // =====================================================================
        //  Controller wiring
        // =====================================================================

        private static void WireController(SmithyPlayerController player, InteractionDetector detector, Transform cameraTf)
        {
            WireComponent(_controller, so =>
            {
                SetRef(so, "database", _db);
                SetRef(so, "inventory", _inventory);
                SetRef(so, "research", _research);
                SetRef(so, "sessionController", _session);
                SetRef(so, "settings", _settings);
                SetRef(so, "fader", _fader);
                SetRef(so, "audioAdapter", _audio);
                SetRef(so, "notifications", _notifications);
                // Prompt is owned by StationSelectionController in the dual-view scene;
                // leaving the controller's prompt null keeps its UpdatePrompt a no-op.
                SetRef(so, "hud", _hud);
                SetRef(so, "player", player);
                SetRef(so, "detector", detector);
                SetRef(so, "cameraTransform", cameraTf);
                SetString(so, "mainMenuSceneName", "MainMenu");
                SetString(so, "dungeonSceneName", "Dungeon");
                var listProp = so.FindProperty("panels");
                listProp.arraySize = _panels.Count;
                for (int i = 0; i < _panels.Count; i++)
                    listProp.GetArrayElementAtIndex(i).objectReferenceValue = _panels[i];
            });

            WireComponent(player, so => SetRef(so, "controller", _controller));
        }

        // =====================================================================
        //  UI element helpers
        // =====================================================================

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform NewChild(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var rt = NewRect(name, parent);
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            return rt;
        }

        private static Image AddImage(RectTransform rt, Color color, bool raycast)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color; img.sprite = _white; img.type = Image.Type.Simple; img.raycastTarget = raycast;
            return img;
        }

        private static Image AddImageChild(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var rt = NewChild(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color; img.sprite = _white; img.raycastTarget = false;
            return img;
        }

        private static TMP_Text AddLabel(RectTransform parent, string text, float x, float y, float w, float h,
            float size, Color color, TextAlignmentOptions align)
        {
            var rt = NewChild(parent, "Label", new Vector2(x, y), new Vector2(w, h));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
            return t;
        }

        private static Button AddButton(RectTransform parent, string label, float x, float y, float w, float h)
        {
            var rt = NewChild(parent, "Button_" + label, new Vector2(x, y), new Vector2(w, h));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = BtnNormal; img.sprite = _white; img.type = Image.Type.Simple; img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = BtnNormal; cb.highlightedColor = BtnHi; cb.pressedColor = BtnHi * 0.8f;
            cb.selectedColor = BtnHi; cb.disabledColor = new Color(0.1f, 0.09f, 0.08f, 0.6f); cb.fadeDuration = 0.1f;
            btn.colors = cb;

            var lblRt = NewRect("Label", rt);
            Stretch(lblRt, 8, 0, 8, 0);
            var t = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.text = label; t.fontSize = 24; t.color = TextLight; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            return btn;
        }

        private static Slider AddSlider(RectTransform parent, float x, float y, float w, float h, float min, float max, float val)
        {
            var go = DefaultControls.CreateSlider(_uiRes);
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
            var sl = go.GetComponent<Slider>();
            sl.minValue = min; sl.maxValue = max; sl.value = val;
            Tint(go, "Background", Control);
            Tint(go, "Fill Area/Fill", Accent);
            Tint(go, "Handle Slide Area/Handle", new Color(0.95f, 0.82f, 0.6f));
            return sl;
        }

        private static Toggle AddToggle(RectTransform parent, float x, float y)
        {
            var go = DefaultControls.CreateToggle(_uiRes);
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(40, 40));
            var lbl = go.transform.Find("Label");
            if (lbl != null) lbl.gameObject.SetActive(false);
            Tint(go, "Background", Control);
            Tint(go, "Background/Checkmark", Accent);
            return go.GetComponent<Toggle>();
        }

        private static TMP_InputField AddInputField(RectTransform parent, float x, float y, float w, float h)
        {
            var go = TMP_DefaultControls.CreateInputField(_tmpRes);
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
            var field = go.GetComponent<TMP_InputField>();
            var img = go.GetComponent<Image>(); if (img != null) img.color = Control;
            if (field.textComponent != null) { field.textComponent.font = _font; field.textComponent.color = TextLight; }
            var ph = go.transform.Find("Text Area/Placeholder")?.GetComponent<TMP_Text>();
            if (ph != null) { ph.font = _font; ph.text = "Название…"; ph.color = TextDim; }
            return field;
        }

        private static ResearchBar AddResearchBar(RectTransform parent, float x, float y, float w, float h)
        {
            var root = NewChild(parent, "ResearchBar", new Vector2(x, y), new Vector2(w, h));
            var bg = AddImage(root, GradeColors.Unknown, false);
            var seg = NewRect("Segments", root); Stretch(seg);
            var markerRt = NewRect("Marker", root);
            markerRt.anchorMin = new Vector2(0.5f, 0f); markerRt.anchorMax = new Vector2(0.5f, 1f);
            markerRt.pivot = new Vector2(0.5f, 0.5f); markerRt.sizeDelta = new Vector2(4f, 0f);
            var markerImg = markerRt.gameObject.AddComponent<Image>();
            markerImg.color = Color.white; markerImg.raycastTarget = false;
            markerRt.gameObject.SetActive(false);

            var bar = root.gameObject.AddComponent<ResearchBar>();
            WireComponent(bar, so =>
            {
                SetRef(so, "segmentContainer", seg);
                SetRef(so, "background", bg);
                SetRef(so, "marker", markerRt);
            });
            return bar;
        }

        private static Transform AddScroll(RectTransform parent, float x, float y, float w, float h)
        {
            var go = DefaultControls.CreateScrollView(_uiRes);
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
            var scroll = go.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            var img = go.GetComponent<Image>(); if (img != null) img.color = new Color(0.05f, 0.045f, 0.04f, 0.6f);

            var content = scroll.content;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        private static void Tint(GameObject root, string path, Color color)
        {
            var t = root.transform.Find(path);
            if (t != null)
            {
                var img = t.GetComponent<Image>();
                if (img != null) img.color = color;
            }
        }

        private static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        private static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        // =====================================================================
        //  Serialization + build settings helpers
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
            if (p == null) { Debug.LogWarning($"[SmithySceneBuilder] Missing field '{prop}' on {so.targetObject.GetType().Name}"); return; }
            p.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject so, string prop, float v) { var p = so.FindProperty(prop); if (p != null) p.floatValue = v; }
        private static void SetString(SerializedObject so, string prop, string v) { var p = so.FindProperty(prop); if (p != null) p.stringValue = v; }
        private static void SetBool(SerializedObject so, string prop, bool v) { var p = so.FindProperty(prop); if (p != null) p.boolValue = v; }
        private static void SetEnum(SerializedObject so, string prop, int v) { var p = so.FindProperty(prop); if (p != null) p.enumValueIndex = v; }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AddSceneToBuildSettingsAfterMainMenu()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            int insertAt = scenes.Count;
            for (int i = 0; i < scenes.Count; i++)
                if (scenes[i].path.EndsWith("MainMenu.unity")) { insertAt = i + 1; break; }
            scenes.Insert(insertAt, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void PointMainMenuAtSmithy()
        {
            // Make MainMenu's New Game load the Smithy scene (best-effort, non-fatal).
            const string menuScenePath = "Assets/Scenes/MainMenu.unity";
            if (!File.Exists(menuScenePath)) return;
            try
            {
                var current = EditorSceneManager.GetActiveScene();
                var menuScene = EditorSceneManager.OpenScene(menuScenePath, OpenSceneMode.Additive);
                foreach (var root in menuScene.GetRootGameObjects())
                {
                    var menu = root.GetComponentInChildren<ForgeGame.UI.MainMenu.MainMenuController>(true);
                    if (menu != null)
                    {
                        var so = new SerializedObject(menu);
                        var prop = so.FindProperty("forgeSceneName");
                        if (prop != null && prop.stringValue != "Smithy")
                        {
                            prop.stringValue = "Smithy";
                            so.ApplyModifiedPropertiesWithoutUndo();
                            EditorSceneManager.MarkSceneDirty(menuScene);
                            EditorSceneManager.SaveScene(menuScene);
                        }
                        break;
                    }
                }
                EditorSceneManager.CloseScene(menuScene, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SmithySceneBuilder] Could not update MainMenu start scene: " + e.Message);
            }
        }
    }
}
