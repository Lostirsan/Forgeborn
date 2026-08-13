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
            var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.Floor2);
            var wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.WallStone);
            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.PlayerBack);
            var vision = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.Vision);
            var torch = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.Torch);
            var glow = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonArtGenerator.TorchGlow);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera (fixed, orthographic, looking down the corridor).
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.02f, 0.03f);
            camGo.AddComponent<AudioListener>();

            // Scrolling world (painted corridor segments) + hero.
            var worldRoot = new GameObject("WorldRoot").transform;

            // Hero low on the screen: bottom of his sprite sits at the bottom edge of the view.
            var playerGo = new GameObject("Player");
            float pScale = 1.5f;
            float pHalfH = (playerSprite != null ? playerSprite.rect.height / playerSprite.pixelsPerUnit : 1.1f) * pScale * 0.5f;
            playerGo.transform.position = new Vector3(0, -cam.orthographicSize + pHalfH, 0);
            playerGo.transform.localScale = new Vector3(pScale, pScale, 1f);
            var playerSr = playerGo.AddComponent<SpriteRenderer>();
            playerSr.sprite = playerSprite; playerSr.sortingOrder = 5;

            // Torch darkness: a near-black mask with a transparent circle around the hero, plus a
            // warm glow and the flame — so only the torch-lit area is visible, dark ahead.
            var visionGo = new GameObject("Vision");
            visionGo.transform.SetParent(camGo.transform, false);
            visionGo.transform.localPosition = new Vector3(0f, -3.2f, 1.5f); // light around the low hero, reaching up ahead
            // TOTAL darkness — only the torch's circle reveals anything, everything else is black.
            // Big scale so the mask covers the whole screen; the small clear centre is baked in.
            visionGo.transform.localScale = new Vector3(8f, 8f, 1f);
            var visionSr = visionGo.AddComponent<SpriteRenderer>();
            visionSr.sprite = vision; visionSr.sortingOrder = 15;
            visionSr.color = Color.white; // full-strength mask

            // Small, subtle warm glow right at the flame (no big pulsing blob).
            var glowGo = new GameObject("TorchGlow");
            glowGo.transform.SetParent(camGo.transform, false);
            glowGo.transform.localPosition = new Vector3(0.35f, -3.35f, 1.4f);
            glowGo.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = glow; glowSr.sortingOrder = 16;

            var torchGo = new GameObject("TorchFlame");
            torchGo.transform.SetParent(camGo.transform, false);
            torchGo.transform.localPosition = new Vector3(0.35f, -3.25f, 1.3f);
            torchGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
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

            BuildEventSystem();

            // Controller.
            var sys = new GameObject("DungeonSystem").AddComponent<DungeonController>();
            var so = new SerializedObject(sys);
            SetRef(so, "worldRoot", worldRoot);
            SetRef(so, "player", playerGo.transform);
            SetRef(so, "cam", cam);
            SetRef(so, "floorSprite", floorSprite);
            SetRef(so, "wallSprite", wallSprite);
            SetRef(so, "torchGlow", glowSr);
            SetRef(so, "torchFlame", torchGo.transform);
            SetRef(so, "depthLabel", depthLabel);
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
