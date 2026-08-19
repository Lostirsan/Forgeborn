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
        public const string Ore = Dir + "/Dungeon_Ore.png";             // ore chunk lying on the floor

        // ---- Side-view (diorama) dungeon ----
        public const string SideWall = Dir + "/Dungeon_SideWall.png";       // cave back wall (tiles horizontally)
        public const string SideFloor = Dir + "/Dungeon_SideFloor.png";     // ground strip (tiles horizontally)
        public const string SideCeiling = Dir + "/Dungeon_SideCeiling.png"; // dark ceiling + stalactites (tiles horizontally)
        public const string SideHeroA = Dir + "/Dungeon_SideHeroA.png";     // torch-bearing leader, facing right
        public const string SideHeroB = Dir + "/Dungeon_SideHeroB.png";     // hooded companion, facing right

        // ---- Combat: enemies (facing LEFT, toward the party), ability icons, HUD bits ----
        public const string EnemyRat = Dir + "/Dungeon_EnemyRat.png";       // small fast crawler
        public const string EnemyGoblin = Dir + "/Dungeon_EnemyGoblin.png"; // humanoid with a crude blade
        public const string EnemyBrute = Dir + "/Dungeon_EnemyBrute.png";   // big slow hulk
        public const string AbStrike = Dir + "/Dungeon_AbStrike.png";       // sword slash
        public const string AbBreak = Dir + "/Dungeon_AbBreak.png";         // heavy blow (stagger)
        public const string AbGuard = Dir + "/Dungeon_AbGuard.png";         // shield / brace
        public const string AbLunge = Dir + "/Dungeon_AbLunge.png";         // reaching thrust (back rank)
        public const string AbHeal = Dir + "/Dungeon_AbHeal.png";           // bandage / mend
        public const string HpBar = Dir + "/Dungeon_HpBar.png";             // white bar, LEFT pivot (fill scales from left)
        public const string BtnPlate = Dir + "/Dungeon_BtnPlate.png";       // stone button plate
        public const string PortraitKnight = Dir + "/Dungeon_PortraitKnight.png";       // roster bust — leader
        public const string PortraitCompanion = Dir + "/Dungeon_PortraitCompanion.png"; // roster bust — companion

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
            Make(force, Ore, MakeOre, 96f);
            Make(force, SideWall, MakeSideWall, 128f);
            Make(force, SideFloor, MakeSideFloor, 128f);
            Make(force, SideCeiling, MakeSideCeiling, 128f);
            Make(force, SideHeroA, MakeSideHeroA, 128f);
            Make(force, SideHeroB, MakeSideHeroB, 128f);
            Make(force, EnemyRat, MakeEnemyRat, 128f);
            Make(force, EnemyGoblin, MakeEnemyGoblin, 128f);
            Make(force, EnemyBrute, MakeEnemyBrute, 128f);
            Make(force, AbStrike, MakeAbStrike, 100f);
            Make(force, AbBreak, MakeAbBreak, 100f);
            Make(force, AbGuard, MakeAbGuard, 100f);
            Make(force, AbLunge, MakeAbLunge, 100f);
            Make(force, AbHeal, MakeAbHeal, 100f);
            Make(force, HpBar, MakeHpBar, 100f);
            Make(force, BtnPlate, MakeBtnPlate, 100f);
            Make(force, PortraitKnight, MakePortraitKnight, 100f);
            Make(force, PortraitCompanion, MakePortraitCompanion, 100f);
            SetFullRect(Floor2); // floor is tiled at runtime → needs a full-rect sprite mesh
            SetFullRect(SideWall); SetFullRect(SideFloor); SetFullRect(SideCeiling);
            SetPivotLeft(HpBar); // fill scales from the left edge
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

        // ---- Ore chunk lying on the floor (rock with metallic bits) ----
        private static void MakeOre()
        {
            var c = new PixelCanvas(96, 96); int w = c.w, h = c.h;
            var rock = new Color(0.34f, 0.30f, 0.28f);
            var rockD = new Color(0.19f, 0.16f, 0.15f);
            var rockH = new Color(0.52f, 0.47f, 0.43f);
            var metal = new Color(0.80f, 0.52f, 0.30f);   // iron-ore glint
            var metalH = new Color(1f, 0.82f, 0.5f);
            c.Disc(w / 2 + 4, h / 2 - 5, (int)(w * 0.36f), rockD);            // shadow
            c.Disc((int)(w * 0.42f), (int)(h * 0.44f), (int)(w * 0.30f), rock);
            c.Disc((int)(w * 0.60f), (int)(h * 0.56f), (int)(w * 0.26f), rock);
            c.Radial((int)(w * 0.40f), (int)(h * 0.60f), w * 0.30f, new Color(rockH.r, rockH.g, rockH.b, 0.7f), 1.7f);
            int[,] sp = { { 34, 40 }, { 56, 52 }, { 44, 58 }, { 62, 36 }, { 30, 54 }, { 50, 44 } };
            for (int i = 0; i < sp.GetLength(0); i++)
            {
                c.Disc(sp[i, 0], sp[i, 1], 4, metal);
                c.Disc(sp[i, 0] - 1, sp[i, 1] + 1, 2, metalH);
            }
            c.Save(Ore, 96f);
        }

        // ---- Side-view cave (tiles horizontally) ----
        private static readonly Color CaveDark = new Color(0.11f, 0.09f, 0.13f);
        private static readonly Color CaveMid = new Color(0.22f, 0.18f, 0.24f);
        private static readonly Color CaveHi = new Color(0.36f, 0.30f, 0.42f);

        private static void MakeSideWall()
        {
            var c = new PixelCanvas(256, 256); int w = c.w, h = c.h;
            c.VGrad(0, 0, w, h, CaveDark, CaveMid);
            // vertical crevices at the tile seams (x=0 and x=128) so it tiles horizontally
            for (int bx = 0; bx <= 128; bx += 128) c.Line(bx, 0, bx + 10, h, 2, new Color(0.06f, 0.05f, 0.07f, 0.7f));
            int[,] pts = { { 40, 190 }, { 96, 150 }, { 160, 200 }, { 210, 150 }, { 60, 90 }, { 130, 70 }, { 200, 90 }, { 90, 220 } };
            for (int i = 0; i < pts.GetLength(0); i++)
            {
                int x = pts[i, 0], y = pts[i, 1], r = 20 + (i % 3) * 6;
                c.Disc(x + 5, y - 6, r, new Color(0.06f, 0.05f, 0.06f));
                c.Disc(x, y, r, CaveMid);
                c.Radial(x - 6, y + 6, r * 0.85f, new Color(CaveHi.r, CaveHi.g, CaveHi.b, 0.55f), 1.7f);
            }
            c.Grain(0, 0, w, h, false, 0.07f, 5);
            c.Save(SideWall, 128f);
        }

        private static void MakeSideFloor()
        {
            var c = new PixelCanvas(256, 128); int w = c.w, h = c.h;
            var fDark = new Color(0.13f, 0.11f, 0.13f);
            var fMid = new Color(0.22f, 0.19f, 0.21f);
            var fHi = new Color(0.31f, 0.27f, 0.28f);
            c.VGrad(0, 0, w, (int)(h * 0.78f), fDark, fMid);
            c.Rect(0, (int)(h * 0.78f), w, h, fHi);                                  // walkable top band
            c.Rect(0, (int)(h * 0.74f), w, (int)(h * 0.78f), new Color(0, 0, 0, 0.4f)); // shadow line
            c.Grain(0, 0, w, (int)(h * 0.78f), false, 0.10f, 13);
            for (int i = 0; i < 10; i++) c.Disc((i * 151) % w, (i * 37) % (int)(h * 0.7f), 4 + i % 3, new Color(0.18f, 0.16f, 0.17f));
            c.Save(SideFloor, 128f);
        }

        private static void MakeSideCeiling()
        {
            var c = new PixelCanvas(256, 128); int w = c.w, h = c.h;
            c.Rect(0, 0, w, h, CaveDark);
            c.VGrad(0, (int)(h * 0.55f), w, h, CaveDark, CaveMid); // rock toward the top
            Stalactite(c, 46, h - 1, 74, 15, CaveMid);
            Stalactite(c, 120, h - 1, 52, 11, CaveMid);
            Stalactite(c, 200, h - 1, 92, 17, CaveMid);
            c.Grain(0, 0, w, h, false, 0.06f, 9);
            c.Save(SideCeiling, 128f);
        }

        private static void Stalactite(PixelCanvas c, int cx, int baseY, int len, int halfW, Color col)
        {
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                int hb = (int)(halfW * (1f - t));
                c.Row(cx - hb, cx + hb, baseY - i, col);
            }
        }

        // ---- Side-view heroes (facing right) ----
        private static void MakeSideHeroA()
        {
            var c = new PixelCanvas(120, 190); int w = c.w, h = c.h;
            var steel = new Color(0.55f, 0.58f, 0.64f);
            var steelHi = new Color(0.78f, 0.81f, 0.88f);
            var cloak = new Color(0.44f, 0.16f, 0.15f);
            var leather = new Color(0.32f, 0.22f, 0.13f);
            var skin = new Color(0.82f, 0.64f, 0.48f);
            var wood = new Color(0.34f, 0.22f, 0.11f);
            c.Rect((int)(w * 0.24f), (int)(h * 0.10f), (int)(w * 0.50f), (int)(h * 0.60f), cloak);          // cloak behind
            c.Rect((int)(w * 0.42f), 0, (int)(w * 0.52f), (int)(h * 0.30f), leather);                       // back leg
            c.Rect((int)(w * 0.55f), 0, (int)(w * 0.65f), (int)(h * 0.27f), leather);                       // front leg (stride)
            c.Rect((int)(w * 0.40f), (int)(h * 0.27f), (int)(w * 0.66f), (int)(h * 0.58f), steel);          // torso
            c.Rect((int)(w * 0.40f), (int)(h * 0.52f), (int)(w * 0.66f), (int)(h * 0.58f), steelHi);        // shoulder glint
            c.Disc((int)(w * 0.52f), (int)(h * 0.70f), (int)(w * 0.14f), steel);                            // helmet
            c.Disc((int)(w * 0.65f), (int)(h * 0.68f), (int)(w * 0.05f), skin);                             // face (right)
            c.Line((int)(w * 0.60f), (int)(h * 0.52f), (int)(w * 0.80f), (int)(h * 0.74f), 5, steel);       // arm raised
            c.Rect((int)(w * 0.78f), (int)(h * 0.70f), (int)(w * 0.86f), (int)(h * 0.98f), wood);           // torch shaft
            c.Save(SideHeroA, 128f);
        }

        private static void MakeSideHeroB()
        {
            var c = new PixelCanvas(104, 168); int w = c.w, h = c.h;
            var hood = new Color(0.20f, 0.25f, 0.27f);
            var hoodHi = new Color(0.30f, 0.36f, 0.38f);
            var leather = new Color(0.28f, 0.22f, 0.15f);
            var skin = new Color(0.80f, 0.62f, 0.46f);
            c.Rect((int)(w * 0.42f), 0, (int)(w * 0.52f), (int)(h * 0.32f), leather);                       // back leg
            c.Rect((int)(w * 0.54f), 0, (int)(w * 0.63f), (int)(h * 0.30f), leather);                       // front leg
            c.Rect((int)(w * 0.32f), (int)(h * 0.10f), (int)(w * 0.66f), (int)(h * 0.64f), hood);           // cloak
            c.Rect((int)(w * 0.34f), (int)(h * 0.55f), (int)(w * 0.64f), (int)(h * 0.62f), hoodHi);         // shoulder line
            c.Disc((int)(w * 0.50f), (int)(h * 0.72f), (int)(w * 0.16f), hood);                             // hood
            c.Tri((int)(w * 0.50f), (int)(h * 0.94f), (int)(w * 0.14f), (int)(h * 0.28f), hood);            // hood peak
            c.Disc((int)(w * 0.63f), (int)(h * 0.70f), (int)(w * 0.045f), skin);                            // face (right)
            c.Save(SideHeroB, 128f);
        }

        // ================= Combat enemies (facing LEFT) =================

        private static void MakeEnemyRat()
        {
            var c = new PixelCanvas(140, 96); int w = c.w, h = c.h;
            var fur = new Color(0.30f, 0.26f, 0.24f);
            var furHi = new Color(0.42f, 0.37f, 0.34f);
            var dark = new Color(0.16f, 0.13f, 0.12f);
            // Long tail curling to the right (behind).
            c.Line((int)(w * 0.62f), (int)(h * 0.30f), (int)(w * 0.96f), (int)(h * 0.55f), 3, dark);
            // Body (low, hunched) — head points LEFT.
            c.Disc((int)(w * 0.45f), (int)(h * 0.36f), (int)(w * 0.24f), fur);
            c.Disc((int)(w * 0.24f), (int)(h * 0.32f), (int)(w * 0.15f), fur);        // head toward left
            c.Radial((int)(w * 0.42f), (int)(h * 0.46f), w * 0.20f, new Color(furHi.r, furHi.g, furHi.b, 0.7f), 1.7f);
            c.Tri((int)(w * 0.22f), (int)(h * 0.66f), 6, (int)(h * 0.22f), fur);      // ear
            // Snout + red eye.
            c.Tri((int)(w * 0.06f), (int)(h * 0.30f), 5, (int)(w * 0.14f), fur);
            c.Disc((int)(w * 0.18f), (int)(h * 0.36f), 3, new Color(0.9f, 0.2f, 0.15f));
            // Little legs.
            c.Rect((int)(w * 0.34f), 0, (int)(w * 0.40f), (int)(h * 0.18f), dark);
            c.Rect((int)(w * 0.52f), 0, (int)(w * 0.58f), (int)(h * 0.18f), dark);
            c.Save(EnemyRat, 128f);
        }

        private static void MakeEnemyGoblin()
        {
            var c = new PixelCanvas(120, 180); int w = c.w, h = c.h;
            var skin = new Color(0.36f, 0.44f, 0.28f);
            var skinHi = new Color(0.48f, 0.57f, 0.36f);
            var rag = new Color(0.30f, 0.24f, 0.18f);
            var metal = new Color(0.60f, 0.63f, 0.70f);
            var dark = new Color(0.14f, 0.16f, 0.11f);
            // Legs.
            c.Rect((int)(w * 0.40f), 0, (int)(w * 0.50f), (int)(h * 0.30f), rag);
            c.Rect((int)(w * 0.54f), 0, (int)(w * 0.64f), (int)(h * 0.28f), rag);
            // Hunched torso.
            c.Rect((int)(w * 0.36f), (int)(h * 0.28f), (int)(w * 0.68f), (int)(h * 0.58f), skin);
            c.Radial((int)(w * 0.5f), (int)(h * 0.46f), w * 0.22f, new Color(skinHi.r, skinHi.g, skinHi.b, 0.6f), 1.7f);
            c.Rect((int)(w * 0.36f), (int)(h * 0.40f), (int)(w * 0.68f), (int)(h * 0.46f), rag); // belt/rag
            // Head (looking left) + pointed ear + red eye.
            c.Disc((int)(w * 0.46f), (int)(h * 0.66f), (int)(w * 0.15f), skin);
            c.Tri((int)(w * 0.64f), (int)(h * 0.78f), 7, (int)(h * 0.12f), skin);            // ear right
            c.Disc((int)(w * 0.40f), (int)(h * 0.68f), 3, new Color(0.95f, 0.75f, 0.15f));   // eye
            // Arm holding a crude blade pointing LEFT (toward party).
            c.Line((int)(w * 0.40f), (int)(h * 0.52f), (int)(w * 0.16f), (int)(h * 0.44f), 4, skin);
            c.Tri((int)(w * 0.02f), (int)(h * 0.44f), 5, (int)(w * 0.20f), metal);           // blade tip left
            c.Save(EnemyGoblin, 128f);
        }

        private static void MakeEnemyBrute()
        {
            var c = new PixelCanvas(190, 210); int w = c.w, h = c.h;
            var hide = new Color(0.34f, 0.28f, 0.30f);
            var hideHi = new Color(0.46f, 0.39f, 0.41f);
            var dark = new Color(0.16f, 0.13f, 0.14f);
            var iron = new Color(0.30f, 0.30f, 0.34f);
            // Thick legs.
            c.Rect((int)(w * 0.36f), 0, (int)(w * 0.48f), (int)(h * 0.34f), dark);
            c.Rect((int)(w * 0.54f), 0, (int)(w * 0.66f), (int)(h * 0.32f), dark);
            // Massive torso + shoulders.
            c.Rect((int)(w * 0.28f), (int)(h * 0.30f), (int)(w * 0.74f), (int)(h * 0.66f), hide);
            c.Disc((int)(w * 0.30f), (int)(h * 0.64f), (int)(w * 0.14f), hide);
            c.Disc((int)(w * 0.72f), (int)(h * 0.64f), (int)(w * 0.14f), hide);
            c.Radial((int)(w * 0.5f), (int)(h * 0.5f), w * 0.30f, new Color(hideHi.r, hideHi.g, hideHi.b, 0.6f), 1.7f);
            // Small head sunk between shoulders (left-facing), glaring eye.
            c.Disc((int)(w * 0.44f), (int)(h * 0.72f), (int)(w * 0.10f), hide);
            c.Disc((int)(w * 0.38f), (int)(h * 0.73f), 3, new Color(0.95f, 0.35f, 0.15f));
            // Iron-banded club in the left fist.
            c.Line((int)(w * 0.34f), (int)(h * 0.44f), (int)(w * 0.10f), (int)(h * 0.30f), 6, dark);
            c.Rect((int)(w * 0.02f), (int)(h * 0.16f), (int)(w * 0.18f), (int)(h * 0.40f), iron); // club head
            c.Save(EnemyBrute, 128f);
        }

        // ================= Ability icons (transparent) =================

        private static readonly Color IcSteel = new Color(0.78f, 0.82f, 0.90f);
        private static readonly Color IcSteelD = new Color(0.48f, 0.52f, 0.60f);
        private static readonly Color IcGold = new Color(0.95f, 0.78f, 0.35f);

        private static void MakeAbStrike()
        {
            var c = new PixelCanvas(96, 96); int w = c.w, h = c.h;
            // A bright diagonal slash arc + a thin sword.
            c.Line((int)(w * 0.18f), (int)(h * 0.22f), (int)(w * 0.82f), (int)(h * 0.86f), 5, IcSteelD);
            c.Line((int)(w * 0.22f), (int)(h * 0.20f), (int)(w * 0.84f), (int)(h * 0.80f), 2, IcSteel);
            c.Tri((int)(w * 0.86f), (int)(h * 0.96f), 7, (int)(h * 0.34f), new Color(1f, 0.95f, 0.8f, 0.9f)); // spark tip
            c.Save(AbStrike, 100f);
        }

        private static void MakeAbBreak()
        {
            var c = new PixelCanvas(96, 96); int w = c.w, h = c.h;
            // Hammer head + impact burst (stagger).
            c.Rect((int)(w * 0.30f), (int)(h * 0.58f), (int)(w * 0.70f), (int)(h * 0.82f), IcSteelD);
            c.Rect((int)(w * 0.30f), (int)(h * 0.74f), (int)(w * 0.70f), (int)(h * 0.82f), IcSteel);
            c.Rect((int)(w * 0.46f), (int)(h * 0.20f), (int)(w * 0.54f), (int)(h * 0.58f), new Color(0.34f, 0.22f, 0.12f)); // haft
            for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; c.Line(w / 2, (int)(h * 0.2f), w / 2 + (int)(Mathf.Cos(a) * 26), (int)(h * 0.2f) + (int)(Mathf.Sin(a) * 26), 2, new Color(1f, 0.8f, 0.3f, 0.8f)); }
            c.Save(AbBreak, 100f);
        }

        private static void MakeAbGuard()
        {
            var c = new PixelCanvas(96, 96); int w = c.w, h = c.h;
            // Shield: rounded top, pointed bottom.
            c.Rect((int)(w * 0.24f), (int)(h * 0.40f), (int)(w * 0.76f), (int)(h * 0.86f), IcSteelD);
            c.Disc((int)(w * 0.5f), (int)(h * 0.86f), (int)(w * 0.26f), IcSteelD);
            c.Tri((int)(w * 0.5f), (int)(h * 0.06f), (int)(w * 0.26f), (int)(h * 0.40f), IcSteelD); // point down
            c.Rect((int)(w * 0.30f), (int)(h * 0.46f), (int)(w * 0.70f), (int)(h * 0.52f), IcSteel);
            c.Rect((int)(w * 0.47f), (int)(h * 0.20f), (int)(w * 0.53f), (int)(h * 0.80f), new Color(1f, 1f, 1f, 0.35f));
            c.Save(AbGuard, 100f);
        }

        private static void MakeAbLunge()
        {
            var c = new PixelCanvas(96, 96); int w = c.w, h = c.h;
            // Long thrust arrow pointing left (reaches the back rank).
            c.Line((int)(w * 0.92f), (int)(h * 0.5f), (int)(w * 0.24f), (int)(h * 0.5f), 4, IcSteel);
            // Left-pointing arrowhead (apex at x≈0.06w, widening to the right).
            int hx0 = (int)(w * 0.06f), hLen = (int)(w * 0.20f), hHalf = (int)(h * 0.22f);
            for (int i = 0; i < hLen; i++)
            {
                int hb = (int)(hHalf * (i / (float)hLen));
                c.Row(hx0 + i, hx0 + i + 1, h / 2 - hb, IcSteel);
                c.Row(hx0 + i, hx0 + i + 1, h / 2 + hb, IcSteel);
                c.Row(hx0 + i, hx0 + i + 1, h / 2, IcSteel);
            }
            c.Save(AbLunge, 100f);
        }

        private static void MakeAbHeal()
        {
            var c = new PixelCanvas(96, 96); int w = c.w, h = c.h;
            // Green mend cross with a soft glow.
            c.Radial(w / 2, h / 2, w * 0.42f, new Color(0.4f, 0.9f, 0.5f, 0.35f), 1.8f);
            var green = new Color(0.45f, 0.85f, 0.45f);
            c.Rect((int)(w * 0.42f), (int)(h * 0.22f), (int)(w * 0.58f), (int)(h * 0.78f), green);
            c.Rect((int)(w * 0.22f), (int)(h * 0.42f), (int)(w * 0.78f), (int)(h * 0.58f), green);
            c.Save(AbHeal, 100f);
        }

        private static void MakeHpBar()
        {
            var c = new PixelCanvas(100, 16);
            c.Rect(0, 0, c.w, c.h, Color.white); // plain white; tinted per use, left pivot set after import
            c.Save(HpBar, 100f);
        }

        private static void MakeBtnPlate()
        {
            var c = new PixelCanvas(220, 120); int w = c.w, h = c.h;
            c.VGrad(0, 0, w, h, new Color(0.16f, 0.14f, 0.13f), new Color(0.26f, 0.23f, 0.21f));
            c.Grain(0, 0, w, h, false, 0.08f, 19);
            int b = 6;
            var edge = new Color(0.45f, 0.40f, 0.34f);
            c.Rect(0, 0, w, b, edge); c.Rect(0, h - b, w, h, edge); c.Rect(0, 0, b, h, edge); c.Rect(w - b, 0, w, h, edge);
            c.Save(BtnPlate, 100f);
        }

        // ---- Roster portraits (framed bust, 128²) ----
        private static void MakePortraitKnight()
        {
            var c = new PixelCanvas(128, 128); int w = c.w, h = c.h;
            var steel = new Color(0.55f, 0.58f, 0.64f);
            var steelHi = new Color(0.80f, 0.83f, 0.90f);
            var dark = new Color(0.10f, 0.10f, 0.12f);
            c.VGrad(0, 0, w, h, new Color(0.13f, 0.12f, 0.14f), new Color(0.20f, 0.18f, 0.22f)); // bg
            // Shoulders (pauldrons).
            c.Disc((int)(w * 0.24f), (int)(h * 0.24f), (int)(w * 0.20f), steel);
            c.Disc((int)(w * 0.76f), (int)(h * 0.24f), (int)(w * 0.20f), steel);
            c.Rect((int)(w * 0.22f), (int)(h * 0.06f), (int)(w * 0.78f), (int)(h * 0.34f), steel);
            // Helmet.
            c.Disc(w / 2, (int)(h * 0.66f), (int)(w * 0.26f), steel);
            c.Rect((int)(w * 0.26f), (int)(h * 0.44f), (int)(w * 0.74f), (int)(h * 0.68f), steel);
            c.Radial((int)(w * 0.42f), (int)(h * 0.72f), w * 0.22f, new Color(steelHi.r, steelHi.g, steelHi.b, 0.7f), 1.7f);
            // Visor slit + comb.
            c.Rect((int)(w * 0.30f), (int)(h * 0.56f), (int)(w * 0.70f), (int)(h * 0.61f), dark);
            c.Rect((int)(w * 0.48f), (int)(h * 0.66f), (int)(w * 0.52f), (int)(h * 0.90f), steelHi);
            Frame(c, new Color(0.42f, 0.30f, 0.16f));
            c.Save(PortraitKnight, 100f);
        }

        private static void MakePortraitCompanion()
        {
            var c = new PixelCanvas(128, 128); int w = c.w, h = c.h;
            var hood = new Color(0.22f, 0.27f, 0.29f);
            var hoodHi = new Color(0.32f, 0.38f, 0.40f);
            var skin = new Color(0.80f, 0.62f, 0.46f);
            var dark = new Color(0.08f, 0.10f, 0.10f);
            c.VGrad(0, 0, w, h, new Color(0.13f, 0.12f, 0.14f), new Color(0.18f, 0.19f, 0.20f)); // bg
            // Shoulders / cloak.
            c.Rect((int)(w * 0.18f), (int)(h * 0.04f), (int)(w * 0.82f), (int)(h * 0.36f), hood);
            c.Disc((int)(w * 0.22f), (int)(h * 0.28f), (int)(w * 0.18f), hood);
            c.Disc((int)(w * 0.78f), (int)(h * 0.28f), (int)(w * 0.18f), hood);
            // Hood.
            c.Disc(w / 2, (int)(h * 0.66f), (int)(w * 0.28f), hood);
            c.Tri(w / 2, (int)(h * 0.98f), (int)(w * 0.22f), (int)(h * 0.36f), hood);
            c.Rect((int)(w * 0.22f), (int)(h * 0.44f), (int)(w * 0.78f), (int)(h * 0.70f), hood);
            c.Radial((int)(w * 0.42f), (int)(h * 0.72f), w * 0.22f, new Color(hoodHi.r, hoodHi.g, hoodHi.b, 0.6f), 1.7f);
            // Face in shadow of the hood.
            c.Disc(w / 2, (int)(h * 0.58f), (int)(w * 0.15f), dark);
            c.Disc(w / 2, (int)(h * 0.56f), (int)(w * 0.11f), skin);
            Frame(c, new Color(0.30f, 0.34f, 0.20f));
            c.Save(PortraitCompanion, 100f);
        }

        // A simple inset border used by the portraits.
        private static void Frame(PixelCanvas c, Color edge)
        {
            int w = c.w, h = c.h, b = 6;
            c.Rect(0, 0, w, b, edge); c.Rect(0, h - b, w, h, edge);
            c.Rect(0, 0, b, h, edge); c.Rect(w - b, 0, w, h, edge);
        }

        private static void SetPivotLeft(string path)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.LeftCenter;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
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
