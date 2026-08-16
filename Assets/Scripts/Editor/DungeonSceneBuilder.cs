using System;
using System.Collections.Generic;
using System.IO;
using ForgeGame.Dungeon;
using TMPro;
using UnityEditor;
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
            AddHero(partyGo.transform, heroA, new Vector3(0f, HalfH(heroA, 1.3f), 0f), 1.3f, 6);   // leader (front)
            AddHero(partyGo.transform, heroB, new Vector3(-1.3f, HalfH(heroB, 1.15f), 0f), 1.15f, 5); // companion (behind)

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
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[DungeonSceneBuilder] Dungeon scene built at " + ScenePath + " and registered in Build Settings.");
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

            AddText(wrt, font, "Инвентарь", 40, new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(480, 54), TextAlignmentOptions.Center);

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

            AddText(wrt, font, "I — закрыть", 24, new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(400, 34), TextAlignmentOptions.Center);

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

        private static void AddHero(Transform party, Sprite sprite, Vector3 localPos, float scale, int order)
        {
            var go = new GameObject("Hero");
            go.transform.SetParent(party, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = order;
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
