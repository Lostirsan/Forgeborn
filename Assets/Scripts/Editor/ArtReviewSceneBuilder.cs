using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// Bakes an "art review" copy of the Smithy scene for handing to art designers:
    /// EVERY UI panel is made visible at once and laid out in a grid, the dark dim
    /// backdrops and the black screen fader are switched off. No game needs to run —
    /// a designer just opens the scene and sees (and can re-skin) all the UI and art.
    /// Menu: Tools ▸ Forge Game ▸ Build Art Review Scene.
    /// </summary>
    public static class ArtReviewSceneBuilder
    {
        private const string OutPath = "Assets/Scenes/Smithy_ArtReview.unity";

        [MenuItem("Tools/Forge Game/Build Art Review Scene")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[ArtReview] Сначала выйдите из Play Mode.");
                return;
            }

            // 1) Fresh Smithy scene so every panel exists and is wired.
            SmithySceneBuilder.Build();
            var scene = EditorSceneManager.GetActiveScene();

            // 2) Kill the black fader / loading blocker so the view isn't dark.
            foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = img.gameObject.name.ToLower();
                if (n.Contains("fade") || n.Contains("blocker"))
                {
                    var c = img.color; c.a = 0f; img.color = c;
                    img.gameObject.SetActive(false);
                }
            }

            // 2b) Show the forge ROOM too (it's toggled at runtime, off by default), so the
            //     workshop interior + stations are visible next to the shop-window view.
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "ForgeViewRoot" || root.name == "ShopViewRoot") root.SetActive(true);

            // 3) Gather every panel root ("Panel_*").
            var panels = new List<RectTransform>();
            foreach (var rt in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (rt.name.StartsWith("Panel_")) panels.Add(rt);
            panels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            // 4) Show each panel, hide its dim backdrop, and grid-lay the window.
            const int cols = 3;
            const float cellW = 2200f, cellH = 1320f;
            float x0 = -(cols - 1) * 0.5f * cellW;

            for (int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];
                panel.gameObject.SetActive(true);

                var dim = panel.GetComponent<Image>();
                if (dim != null) dim.enabled = false; // no dark overlay stacking

                int col = i % cols, row = i / cols;
                var cell = new Vector2(x0 + col * cellW, -row * cellH + cellH);

                // Offset ALL of the panel's content into its grid cell — works for panels with a
                // single "Window" child and for multi-child panels (e.g. Assembly) alike, so
                // nothing overlaps.
                foreach (Transform child in panel)
                    if (child is RectTransform crt) crt.anchoredPosition += cell;

                AddCaption(panel.parent, cell + new Vector2(0f, 640f), panel.name.Replace("Panel_", ""));
            }

            // Panels whose contents are built at runtime look empty in the editor. Fill them
            // with representative sample content so designers see the FULL populated UI.
            foreach (var inv in Object.FindObjectsByType<ForgeGame.UI.Smithy.InventoryPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                inv.BuildPreview();
                if (inv.transform is RectTransform prt)
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
            }
            Canvas.ForceUpdateCanvases();

            // 5) Save as a COPY (leaves the real Smithy.unity untouched).
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, OutPath, true);
            AssetDatabase.Refresh();

            Debug.Log($"[ArtReview] Готово: {OutPath} — панелей показано: {panels.Count}. Оригинальная Smithy.unity не тронута.");
        }

        private static void AddCaption(Transform parent, Vector2 pos, string text)
        {
            var go = new GameObject("Caption_" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1200f, 56f);
            rt.anchoredPosition = pos;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = 44; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = new Color(1f, 0.7f, 0.3f);
            t.raycastTarget = false;
        }
    }
}
