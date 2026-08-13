using UnityEditor;
using UnityEngine;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// Procedural placeholder art for the dungeon vertical slice: the hero seen from
    /// BEHIND (we look over his shoulders as he walks away into the cave), a tiling stone
    /// floor block, a chunky "voluminous" cave-rock wall block, and a top depth-fade.
    /// Menu: Tools ▸ Forge Game ▸ Generate Dungeon Art.
    /// </summary>
    public static class DungeonArtGenerator
    {
        private const string Dir = "Assets/Art/Generated/Dungeon";
        public const string PlayerBack = Dir + "/Dungeon_PlayerBack.png";
        public const string Floor = Dir + "/Dungeon_Floor.png";
        public const string Rock = Dir + "/Dungeon_Rock.png";
        public const string DepthFade = Dir + "/Dungeon_DepthFade.png";
        public const string Floor2 = Dir + "/Dungeon_Floor2.png";       // tileable flagstone floor (fills the middle)
        public const string WallStone = Dir + "/Dungeon_WallStone.png"; // tall thin rocky wall (edges), lit on the inner side
        public const string Corridor = Dir + "/Dungeon_Corridor.png";   // painted cave strip (scrolls)
        public const string Vision = Dir + "/Dungeon_Vision.png";        // darkness mask, transparent centre (torch)
        public const string Torch = Dir + "/Dungeon_Torch.png";         // flame in the hand
        public const string TorchGlow = Dir + "/Dungeon_TorchGlow.png"; // warm light around the torch

        [MenuItem("Tools/Forge Game/Generate Dungeon Art")]
        public static void GenerateMenu() { GenerateAll(true); EditorUtility.DisplayDialog("Forge Game", "Арт подземелья сгенерирован в " + Dir, "OK"); }

        public static void EnsureAll() => GenerateAll(false);

        private static void GenerateAll(bool force)
        {
            EnsureFolder("Assets/Art"); EnsureFolder("Assets/Art/Generated"); EnsureFolder(Dir);
            Make(force, PlayerBack, MakePlayerBack, 128f);
            Make(force, Floor, MakeFloor, 128f);
            Make(force, Rock, MakeRock, 128f);
            Make(force, DepthFade, MakeDepthFade, 100f);
            Make(force, Corridor, MakeCorridor, 64f);
            Make(force, Vision, MakeVision, 100f);
            Make(force, Torch, MakeTorch, 128f);
            Make(force, TorchGlow, MakeTorchGlow, 100f);
            Make(force, Floor2, MakeFloor2, 128f);
            Make(force, WallStone, MakeWall, 128f);
            SetFullRect(Floor2); // floor is tiled at runtime → needs a full-rect sprite mesh
        }

        // ---- Hero, back view ----
        private static void MakePlayerBack()
        {
            var c = new PixelCanvas(96, 144); int w = c.w, h = c.h;
            var cloak = new Color(0.40f, 0.16f, 0.14f);
            var cloakHi = new Color(0.55f, 0.26f, 0.22f);
            var hair = new Color(0.28f, 0.19f, 0.11f);
            var skin = new Color(0.80f, 0.62f, 0.46f);
            var leather = new Color(0.34f, 0.24f, 0.14f);
            var metal = new Color(0.62f, 0.65f, 0.72f);
            var dark = new Color(0.24f, 0.10f, 0.09f);

            // Cloak / back (rounded trapezoid).
            c.VGrad((int)(w * 0.22f), (int)(h * 0.05f), (int)(w * 0.78f), (int)(h * 0.60f), cloak, cloakHi);
            c.Disc((int)(w * 0.24f), (int)(h * 0.56f), (int)(w * 0.15f), cloak);  // left shoulder
            c.Disc((int)(w * 0.76f), (int)(h * 0.56f), (int)(w * 0.15f), cloak);  // right shoulder
            c.Disc((int)(w * 0.50f), (int)(h * 0.10f), (int)(w * 0.28f), cloak);  // hem
            c.Disc((int)(w * 0.16f), (int)(h * 0.36f), (int)(w * 0.10f), dark);   // left arm
            c.Disc((int)(w * 0.84f), (int)(h * 0.36f), (int)(w * 0.10f), dark);   // right arm

            // Backpack + straps.
            c.Rect((int)(w * 0.37f), (int)(h * 0.18f), (int)(w * 0.63f), (int)(h * 0.52f), leather);
            c.Rect((int)(w * 0.30f), (int)(h * 0.30f), (int)(w * 0.34f), (int)(h * 0.60f), dark); // left strap
            c.Rect((int)(w * 0.66f), (int)(h * 0.30f), (int)(w * 0.70f), (int)(h * 0.60f), dark); // right strap

            // Sword slung across the back.
            c.Line((int)(w * 0.34f), (int)(h * 0.28f), (int)(w * 0.68f), (int)(h * 0.66f), 3, metal);
            c.Line((int)(w * 0.28f), (int)(h * 0.20f), (int)(w * 0.36f), (int)(h * 0.30f), 3, leather); // hilt

            // Neck + head (top of hair, lit).
            c.Rect((int)(w * 0.44f), (int)(h * 0.58f), (int)(w * 0.56f), (int)(h * 0.66f), skin);
            c.Disc((int)(w * 0.50f), (int)(h * 0.76f), (int)(w * 0.17f), hair);
            c.Radial((int)(w * 0.50f), (int)(h * 0.80f), w * 0.12f, new Color(0.5f, 0.36f, 0.22f, 0.7f), 1.8f);

            c.Save(PlayerBack, 128f);
        }

        // ---- Stone floor block (tiles) ----
        private static void MakeFloor()
        {
            var c = new PixelCanvas(128, 128); int w = c.w, h = c.h;
            c.VGrad(0, 0, w, h, new Color(0.20f, 0.18f, 0.17f), new Color(0.26f, 0.23f, 0.21f));
            c.Grain(0, 0, w, h, false, 0.10f, 41);
            // Seams (cobble edges) — kept near the border so tiles read as blocks.
            c.Rect(0, h / 2 - 1, w, h / 2 + 1, new Color(0.12f, 0.11f, 0.10f, 0.6f));
            c.Rect(w / 2 - 1, 0, w / 2 + 1, h, new Color(0.12f, 0.11f, 0.10f, 0.6f));
            // A couple of pebbles / highlights.
            c.Disc((int)(w * 0.30f), (int)(h * 0.68f), 8, new Color(0.32f, 0.29f, 0.26f));
            c.Disc((int)(w * 0.70f), (int)(h * 0.30f), 6, new Color(0.30f, 0.27f, 0.24f));
            c.Save(Floor, 128f);
        }

        // ---- Chunky cave rock wall (lit from above → looks 3D) ----
        private static void MakeRock()
        {
            var c = new PixelCanvas(128, 128); int w = c.w, h = c.h;
            c.VGrad(0, 0, w, h, new Color(0.10f, 0.09f, 0.09f), new Color(0.17f, 0.15f, 0.14f)); // dark base, lighter top
            var mid = new Color(0.26f, 0.23f, 0.21f);
            // Rounded rock lumps in a rough grid; each: shadow (down-right) → body → top-left highlight.
            int[,] pts = { { 30, 96 }, { 88, 104 }, { 60, 70 }, { 22, 44 }, { 100, 52 }, { 66, 22 }, { 30, 12 } };
            for (int i = 0; i < pts.GetLength(0); i++)
            {
                int x = pts[i, 0], y = pts[i, 1], r = 20 + (i % 3) * 4;
                c.Disc(x + 4, y - 5, r, new Color(0.06f, 0.05f, 0.05f));           // cast shadow
                c.Disc(x, y, r, mid);                                              // body
                c.Radial(x - 5, y + 6, r * 0.9f, new Color(0.42f, 0.38f, 0.34f, 0.8f), 1.7f); // top-left light
            }
            c.Grain(0, 0, w, h, false, 0.08f, 7);
            c.Save(Rock, 128f);
        }

        // ---- Depth fade: dark at the top (corridor vanishing into the dark ahead) ----
        private static void MakeDepthFade()
        {
            var c = new PixelCanvas(16, 128); int w = c.w, h = c.h;
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);              // 0 bottom → 1 top
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, t)) * 0.95f;
                c.Row(0, w, y, new Color(0.03f, 0.025f, 0.03f, a));
            }
            c.Save(DepthFade, 100f);
        }

        // ---- Painted cave corridor strip (scrolls; tiles vertically) ----
        private static void MakeCorridor()
        {
            var c = new PixelCanvas(1200, 768); int w = c.w, h = c.h;
            var rockBase = new Color(0.30f, 0.24f, 0.34f);
            var rockHi = new Color(0.50f, 0.42f, 0.58f);
            var rockShadow = new Color(0.15f, 0.11f, 0.18f);
            var floorBase = new Color(0.19f, 0.15f, 0.21f);
            int fx0 = (int)(w * 0.30f), fx1 = (int)(w * 0.70f);

            // Floor path down the middle.
            c.Rect(fx0, 0, fx1, h, floorBase);
            c.Grain(fx0, 0, fx1, h, false, 0.10f, 11);
            for (int i = 0; i < 14; i++)
            {
                int rx = fx0 + (i * 137 % (fx1 - fx0));
                int ry = (i * 211) % h;
                c.Disc(rx, ry, 5 + i % 4, new Color(0.26f, 0.21f, 0.28f));
            }

            FillWall(c, 0, fx0, rockBase, rockHi, rockShadow, +1);
            FillWall(c, fx1, w, rockBase, rockHi, rockShadow, -1);

            // Crystal clusters at the wall/floor seams (subtle blue flavour).
            Crystals(c, fx0 - 20, (int)(h * 0.25f));
            Crystals(c, fx1 + 20, (int)(h * 0.70f));
            Crystals(c, fx0 - 30, (int)(h * 0.80f));

            c.Save(Corridor, 64f);
        }

        private static void FillWall(PixelCanvas c, int x0, int x1, Color baseC, Color hi, Color shadow, int lightDir)
        {
            int h = c.h;
            c.Rect(x0, 0, x1, h, baseC);
            // faceted rock lumps in a rough grid
            int cols = 3, step = 150;
            for (int gy = -1; gy * step < h + step; gy++)
                for (int gx = 0; gx < cols; gx++)
                {
                    int cx = x0 + (int)((gx + 0.5f) / cols * (x1 - x0)) + ((gy % 2) * 26 - 13);
                    int cy = gy * step + ((gx % 2) * 40);
                    int r = 46 + ((gx + gy) % 3) * 10;
                    c.Disc(cx + 6 * lightDir, cy - 8, r, shadow);                       // cast shadow
                    c.Disc(cx, cy, r, baseC);                                           // body
                    c.Radial(cx - 10 * lightDir, cy + 10, r * 0.85f, new Color(hi.r, hi.g, hi.b, 0.6f), 1.7f); // lit edge
                }
            // dark crevices
            for (int i = 0; i <= 4; i++)
            {
                int x = x0 + (i * (x1 - x0)) / 4;
                c.Line(x, 0, x + 18 * lightDir, h, 2, new Color(shadow.r, shadow.g, shadow.b, 0.6f));
            }
            c.Grain(x0, 0, x1, h, false, 0.07f, x0 + 3);
        }

        private static void Crystals(PixelCanvas c, int x, int y)
        {
            var lo = new Color(0.28f, 0.45f, 0.9f);
            var hi = new Color(0.65f, 0.85f, 1f);
            c.Tri(x, y + 46, 12, 46, lo);
            c.Tri(x - 22, y + 30, 9, 34, lo);
            c.Tri(x + 20, y + 36, 10, 40, lo);
            c.Tri(x, y + 40, 5, 34, hi);
        }

        // ---- Darkness mask with a soft transparent centre (the torch's reach) ----
        private static void MakeVision()
        {
            var c = new PixelCanvas(512, 512); int w = c.w, h = c.h;
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f;
            var dark = new Color(0.012f, 0.008f, 0.014f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / R;
                    // Tight torch circle: a small clear centre, then a fast falloff to total black.
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.24f, d));
                    c.p[y * w + x] = new Color(dark.r, dark.g, dark.b, a);
                }
            c.Save(Vision, 100f);
        }

        // ---- Warm torch glow (soft, lightens the lit circle) ----
        private static void MakeTorchGlow()
        {
            var c = new PixelCanvas(256, 256); int w = c.w, h = c.h;
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Clamp01(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / R);
                    float a = (1f - d); a = a * a * 0.55f;
                    c.p[y * w + x] = new Color(1f, 0.62f, 0.28f, a);
                }
            c.Save(TorchGlow, 100f);
        }

        // ---- Torch flame in the hand ----
        private static void MakeTorch()
        {
            var c = new PixelCanvas(48, 72); int w = c.w, h = c.h;
            c.Rect((int)(w * 0.44f), 0, (int)(w * 0.56f), (int)(h * 0.42f), new Color(0.32f, 0.20f, 0.10f)); // handle
            c.Tri(w / 2, (int)(h * 0.98f), (int)(w * 0.30f), (int)(h * 0.62f), new Color(0.98f, 0.40f, 0.10f)); // outer flame
            c.Disc(w / 2, (int)(h * 0.50f), (int)(w * 0.26f), new Color(0.98f, 0.40f, 0.10f));
            c.Tri(w / 2, (int)(h * 0.90f), (int)(w * 0.18f), (int)(h * 0.45f), new Color(1f, 0.78f, 0.25f)); // inner
            c.Disc(w / 2, (int)(h * 0.52f), (int)(w * 0.12f), new Color(1f, 0.95f, 0.7f)); // core
            c.Save(Torch, 128f);
        }

        // ---- Tileable flagstone floor (fills the ~90% middle) ----
        private static void MakeFloor2()
        {
            var c = new PixelCanvas(256, 256); int w = c.w, h = c.h;
            var grout = new Color(0.09f, 0.08f, 0.09f);
            c.VGrad(0, 0, w, h, new Color(0.21f, 0.19f, 0.21f), new Color(0.28f, 0.25f, 0.27f));
            c.Grain(0, 0, w, h, false, 0.10f, 23);
            // Four flagstones per tile, grout at the edges + centre lines → tiles seamlessly.
            c.Rect(0, 0, w, 3, grout); c.Rect(0, h / 2 - 1, w, h / 2 + 2, grout);
            c.Rect(0, 0, 3, h, grout); c.Rect(w / 2 - 1, 0, w / 2 + 2, h, grout);
            int[,] q = { { w / 4, h / 4 }, { 3 * w / 4, h / 4 }, { w / 4, 3 * h / 4 }, { 3 * w / 4, 3 * h / 4 } };
            for (int i = 0; i < 4; i++)
            {
                c.Radial(q[i, 0], q[i, 1], w * 0.13f, new Color(0.34f, 0.30f, 0.32f, 0.5f), 1.8f); // stone sheen
                c.Disc(q[i, 0] + (i * 17 % 20) - 10, q[i, 1] + (i * 23 % 20) - 10, 4, new Color(0.18f, 0.16f, 0.18f)); // pebble
            }
            c.Save(Floor2, 128f);
        }

        // ---- Tall thin rocky wall; lit on the RIGHT (inner) edge, tiles vertically ----
        private static void MakeWall()
        {
            var c = new PixelCanvas(128, 768); int w = c.w, h = c.h;
            var dark = new Color(0.13f, 0.11f, 0.15f);
            var mid = new Color(0.26f, 0.22f, 0.28f);
            var hi = new Color(0.42f, 0.37f, 0.46f);
            c.HGrad(0, 0, w, h, dark, mid); // darker outer (left) → lighter inner (right)
            // Inner lit edge.
            c.Rect((int)(w * 0.80f), 0, w, h, new Color(hi.r, hi.g, hi.b, 0.35f));
            // Repeating rock units (every 128px) so it tiles vertically.
            for (int by = 0; by < h; by += 128)
            {
                c.Rect(0, by, w, by + 3, new Color(0.06f, 0.05f, 0.06f, 0.7f)); // horizontal crack (seam)
                for (int k = 0; k < 3; k++)
                {
                    int cx = 20 + k * 40 + ((by / 128) % 2) * 12;
                    int cy = by + 30 + k * 34;
                    int r = 16 + (k % 2) * 6;
                    c.Disc(cx + 4, cy - 4, r, new Color(0.06f, 0.05f, 0.06f));
                    c.Disc(cx, cy, r, mid);
                    c.Radial(cx + 6, cy + 5, r * 0.9f, new Color(hi.r, hi.g, hi.b, 0.5f), 1.7f);
                }
            }
            c.Grain(0, 0, w, h, true, 0.08f, 5);
            c.Save(WallStone, 128f);
        }

        private static void SetFullRect(string path)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            if (s.spriteMeshType == SpriteMeshType.FullRect) return;
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }

        // ---- infra ----
        private static void Make(bool force, string path, System.Action draw, float ppu)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<Sprite>(path) != null) return;
            draw();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
