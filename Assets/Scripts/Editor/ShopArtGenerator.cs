using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// Programmatically paints stylised placeholder art for the Smithy Shop (and a
    /// couple of Forge backdrops) as real PNG sprite assets — gradients, silhouettes,
    /// wood grain, shading and vignettes — so the scene reads as an actual painted
    /// prototype rather than flat coloured blocks. Deterministic (fixed seeds), so
    /// re-running reproduces the same images. The scene builder calls
    /// <see cref="EnsureAll"/> and then references the saved sprites.
    /// </summary>
    public static class ShopArtGenerator
    {
        public const string ShopDir = "Assets/Art/Generated/Smithy/Shop";
        public const string ForgeDir = "Assets/Art/Generated/Smithy/Forge";

        public const string StreetBackgroundPath = ShopDir + "/Shop_StreetBackground.png";
        public const string StreetMidgroundPath = ShopDir + "/Shop_StreetMidground.png";
        public const string WindowForegroundPath = ShopDir + "/Shop_WindowForeground.png";
        public const string TransitionBeamPath = ShopDir + "/Shop_TransitionBeam.png";
        public const string CustomerPath = ShopDir + "/Customer_Placeholder.png";
        public const string ForgeBackgroundPath = ForgeDir + "/Forge_Background.png";
        public const string ForgeForegroundPath = ForgeDir + "/Forge_Foreground.png";

        [MenuItem("Tools/Forge Game/Generate Smithy Shop Art")]
        public static void GenerateMenu()
        {
            GenerateAll(true);
            EditorUtility.DisplayDialog("Forge Game", "Арт-слои лавки сгенерированы в " + ShopDir, "OK");
        }

        /// <summary>Creates any missing art assets (used by the scene builder).</summary>
        public static void EnsureAll() => GenerateAll(false);

        private static void GenerateAll(bool force)
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Generated");
            EnsureFolder("Assets/Art/Generated/Smithy");
            EnsureFolder(ShopDir);
            EnsureFolder(ForgeDir);

            if (force || !File.Exists(StreetBackgroundPath)) StreetBackground();
            if (force || !File.Exists(StreetMidgroundPath)) StreetMidground();
            if (force || !File.Exists(WindowForegroundPath)) WindowForeground();
            if (force || !File.Exists(TransitionBeamPath)) TransitionBeam();
            if (force || !File.Exists(CustomerPath)) Customer();
            if (force || !File.Exists(ForgeBackgroundPath)) ForgeBackground();
            if (force || !File.Exists(ForgeForegroundPath)) ForgeForeground();

            AssetDatabase.Refresh();
        }

        // =====================================================================
        //  Textures
        // =====================================================================

        // World sizes (units) and resolution (px/unit) per asset.
        private const float PpuBg = 22f;

        private static void StreetBackground()
        {
            const float W = 64f, H = 28f;
            var img = new Img(Mathf.RoundToInt(W * PpuBg), Mathf.RoundToInt(H * PpuBg));
            var rng = new System.Random(1001);
            int w = img.w, h = img.h;
            float horizon = h * 0.42f;

            // Sky: deep indigo up top fading to a warm dusk band at the horizon.
            img.VGrad(0, (int)horizon, w, h, new Color(0.34f, 0.30f, 0.34f), new Color(0.14f, 0.15f, 0.26f));
            // Warm horizon glow.
            img.VGrad(0, (int)(horizon - h * 0.10f), w, (int)(horizon + h * 0.10f),
                new Color(0.62f, 0.44f, 0.36f, 0.9f), new Color(0.40f, 0.34f, 0.38f, 0f));
            // Moon glow, upper area.
            img.Radial((int)(w * 0.78f), (int)(h * 0.82f), h * 0.22f, new Color(0.85f, 0.85f, 0.78f, 0.30f), 2f);
            img.Radial((int)(w * 0.78f), (int)(h * 0.82f), h * 0.06f, new Color(0.95f, 0.94f, 0.86f, 0.8f), 1.5f);

            // Far building silhouettes (bluish, lighter — atmospheric).
            int x = -10;
            while (x < w + 10)
            {
                int bw = 40 + rng.Next(70);
                int bh = (int)(h * (0.10f + (float)rng.NextDouble() * 0.14f));
                Building(img, x, (int)horizon, bw, bh, new Color(0.26f, 0.28f, 0.38f), rng, false);
                x += bw + rng.Next(6);
            }
            // Near buildings (darker, taller, lit windows).
            x = -20;
            while (x < w + 10)
            {
                int bw = 60 + rng.Next(90);
                int bh = (int)(h * (0.16f + (float)rng.NextDouble() * 0.20f));
                Building(img, x, (int)(horizon - h * 0.02f), bw, bh, new Color(0.17f, 0.16f, 0.22f), rng, true);
                x += bw + rng.Next(10);
            }

            // Ground / road below the horizon.
            img.VGrad(0, 0, w, (int)horizon, new Color(0.17f, 0.14f, 0.13f), new Color(0.30f, 0.25f, 0.23f));
            // A lit central path.
            img.VGrad((int)(w * 0.32f), 0, (int)(w * 0.68f), (int)(horizon * 0.9f),
                new Color(0.30f, 0.25f, 0.22f, 0.5f), new Color(0.40f, 0.33f, 0.28f, 0f));
            // Cobble streaks.
            for (int i = 0; i < 60; i++)
            {
                int cy = rng.Next((int)horizon);
                int cx = rng.Next(w);
                int cw = 8 + rng.Next(26);
                img.Rect(cx, cy, cx + cw, cy + 2, new Color(0f, 0f, 0f, 0.10f));
            }

            // Soft edge vignette.
            Vignette(img, 0.85f);
            img.Save(StreetBackgroundPath, PpuBg);
        }

        private static void StreetMidground()
        {
            const float W = 52f, H = 16f;
            var img = new Img(Mathf.RoundToInt(W * PpuBg), Mathf.RoundToInt(H * PpuBg));
            var rng = new System.Random(2002);
            int w = img.w, h = img.h;
            Color wood = new Color(0.16f, 0.12f, 0.09f, 0.95f);

            // Left: lamp post + lantern glow (kept within the on-screen band).
            int lx = (int)(w * 0.30f);
            img.Rect(lx - 3, (int)(h * 0.1f), lx + 3, (int)(h * 0.72f), wood);
            img.Rect(lx - 10, (int)(h * 0.68f), lx + 10, (int)(h * 0.78f), wood);
            img.Radial(lx, (int)(h * 0.73f), h * 0.22f, new Color(1f, 0.78f, 0.40f, 0.45f), 2f);
            img.Rect(lx - 5, (int)(h * 0.70f), lx + 5, (int)(h * 0.76f), new Color(1f, 0.85f, 0.5f, 0.8f));

            // Left: barrels.
            Barrel(img, (int)(w * 0.20f), (int)(h * 0.12f), 26, 34, rng);
            Barrel(img, (int)(w * 0.24f), (int)(h * 0.10f), 24, 30, rng);

            // Right: hanging sign on a bracket.
            int sx = (int)(w * 0.72f);
            img.Rect(sx - 40, (int)(h * 0.86f), sx + 4, (int)(h * 0.90f), wood);
            img.Rect(sx - 34, (int)(h * 0.60f), sx - 30, (int)(h * 0.88f), new Color(0.1f, 0.08f, 0.06f, 0.9f));
            img.Rect(sx - 58, (int)(h * 0.55f), sx - 12, (int)(h * 0.72f), new Color(0.30f, 0.22f, 0.14f, 0.95f));
            img.Rect(sx - 56, (int)(h * 0.57f), sx - 14, (int)(h * 0.70f), new Color(0.42f, 0.30f, 0.18f, 0.9f));

            // Right: cart wheel + plant.
            Wheel(img, (int)(w * 0.80f), (int)(h * 0.18f), 30);
            Plant(img, (int)(w * 0.64f), (int)(h * 0.08f), 22, 40);

            // Low fence segments only toward the sides (centre stays clear for the customer).
            Fence(img, 0, (int)(w * 0.22f), (int)(h * 0.06f), (int)(h * 0.20f), rng);
            Fence(img, (int)(w * 0.78f), w, (int)(h * 0.06f), (int)(h * 0.20f), rng);

            img.Save(StreetMidgroundPath, PpuBg);
        }

        private static void WindowForeground()
        {
            const float W = 42f, H = 24f, Ppu = 24f;
            var img = new Img(Mathf.RoundToInt(W * Ppu), Mathf.RoundToInt(H * Ppu));
            int w = img.w, h = img.h;

            // Opening rectangle (transparent). Narrower so the stiles stay on-screen
            // even at 16:9; everything outside is frame.
            int ox0 = (int)(w * 0.24f), ox1 = (int)(w * 0.76f);
            int oy0 = (int)(h * 0.30f), oy1 = (int)(h * 0.80f);

            Color woodLo = new Color(0.22f, 0.15f, 0.10f);
            Color woodHi = new Color(0.40f, 0.28f, 0.17f);

            // Fill whole frame with warm wood, then carve the opening as transparent.
            img.VGrad(0, 0, w, h, woodLo, woodHi);
            // Warm interior light spilling onto the frame from the forge behind the player.
            img.Radial((int)(w * 0.5f), (int)(h * 0.12f), h * 0.6f, new Color(0.9f, 0.55f, 0.25f, 0.22f), 1.5f);
            img.ClearRect(ox0, oy0, ox1, oy1);

            // Vertical grain on the stiles.
            Grain(img, 0, 0, ox0, h, true, 0.06f);
            Grain(img, ox1, 0, w, h, true, 0.06f);
            // Horizontal grain on beam + counter.
            Grain(img, 0, oy1, w, h, false, 0.06f);
            Grain(img, 0, 0, w, oy0, false, 0.05f);

            // Iron corner brackets around the opening.
            IronBracket(img, ox0, oy1, +1, -1);
            IronBracket(img, ox1, oy1, -1, -1);
            IronBracket(img, ox0, oy0, +1, +1);
            IronBracket(img, ox1, oy0, -1, +1);

            // Inner shadow fading into the opening (the frame shades the reveal).
            int s = (int)(h * 0.06f);
            img.HGrad(ox0, oy0, ox0 + s, oy1, new Color(0, 0, 0, 0.6f), new Color(0, 0, 0, 0f)); // left
            img.HGrad(ox1 - s, oy0, ox1, oy1, new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0.6f)); // right
            img.VGrad(ox0, oy1 - s, ox1, oy1, new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0.6f)); // top
            img.VGrad(ox0, oy0, ox1, oy0 + s, new Color(0, 0, 0, 0.55f), new Color(0, 0, 0, 0f)); // bottom

            // Counter top highlight.
            img.Rect(ox0 - 6, oy0 - 3, ox1 + 6, oy0 + 3, new Color(0.42f, 0.30f, 0.18f, 0.9f));

            // Counter props (small blacksmith items with a warm rim).
            int cy = (int)(oy0 * 0.5f);
            Hammer(img, (int)(w * 0.30f), cy);
            Tongs(img, (int)(w * 0.50f), cy);
            Horseshoe(img, (int)(w * 0.66f), cy);

            img.Save(WindowForegroundPath, Ppu);
        }

        private static void TransitionBeam()
        {
            const float W = 3.6f, H = 28f, Ppu = 40f;
            var img = new Img(Mathf.RoundToInt(W * Ppu), Mathf.RoundToInt(H * Ppu));
            int w = img.w, h = img.h;
            img.VGrad(0, 0, w, h, new Color(0.10f, 0.07f, 0.05f), new Color(0.17f, 0.12f, 0.08f));
            Grain(img, 0, 0, w, h, true, 0.10f);
            // Warm left rim, dark right shadow.
            img.Rect(0, 0, (int)(w * 0.16f), h, new Color(0.45f, 0.30f, 0.16f, 0.5f));
            img.Rect((int)(w * 0.80f), 0, w, h, new Color(0f, 0f, 0f, 0.5f));
            // Iron bands with bolts.
            IronBand(img, (int)(h * 0.30f));
            IronBand(img, (int)(h * 0.70f));
            img.Save(TransitionBeamPath, Ppu);
        }

        private static void Customer()
        {
            const float W = 2.8f, H = 4.4f, Ppu = 48f;
            var img = new Img(Mathf.RoundToInt(W * Ppu), Mathf.RoundToInt(H * Ppu));
            int w = img.w, h = img.h;
            Color cloak = new Color(0.19f, 0.17f, 0.24f);
            Color cloakDark = new Color(0.11f, 0.10f, 0.16f);
            Color rim = new Color(0.55f, 0.46f, 0.40f);

            // Cloak: trapezoid widening toward the feet.
            int topY = (int)(h * 0.62f);
            for (int y = 0; y < topY; y++)
            {
                float t = 1f - (float)y / topY;          // 0 at feet, 1 near shoulders
                int half = (int)Mathf.Lerp(w * 0.44f, w * 0.24f, t);
                int cx = w / 2;
                Color body = Color.Lerp(cloak, cloakDark, 0.4f + 0.3f * (y / (float)topY));
                img.Row(cx - half, cx + half, y, body);
            }
            // Shoulders + hood.
            int hy = (int)(h * 0.60f);
            img.Disc(w / 2, hy, (int)(w * 0.30f), cloak);          // shoulders
            img.Disc(w / 2, (int)(h * 0.78f), (int)(w * 0.22f), cloak); // head/hood
            img.Tri(w / 2, (int)(h * 0.98f), (int)(w * 0.30f), (int)(h * 0.72f), cloakDark); // hood point

            // Left rim light (from the street lamp).
            for (int y = 0; y < (int)(h * 0.95f); y++)
                for (int x = 0; x < w; x++)
                    if (img.A(x, y) > 0.3f && img.A(x - 3, y) <= 0.3f)
                        img.Blend(x, y, new Color(rim.r, rim.g, rim.b, 0.7f));

            img.Save(CustomerPath, Ppu);
        }

        private static void ForgeBackground()
        {
            const float W = 50f, H = 26f;
            var img = new Img(Mathf.RoundToInt(W * PpuBg), Mathf.RoundToInt(H * PpuBg));
            var rng = new System.Random(4004);
            int w = img.w, h = img.h;
            float floorY = h * 0.24f;

            // Warm stone wall.
            img.VGrad(0, (int)floorY, w, h, new Color(0.13f, 0.11f, 0.10f), new Color(0.22f, 0.17f, 0.15f));
            // Brick seams.
            for (int by = (int)floorY; by < h; by += 22)
            {
                img.Rect(0, by, w, by + 1, new Color(0f, 0f, 0f, 0.18f));
                int off = ((by / 22) % 2) * 22;
                for (int bx = off; bx < w; bx += 44)
                    img.Rect(bx, by, bx + 1, by + 22, new Color(0f, 0f, 0f, 0.14f));
            }
            // Wood plank floor.
            img.VGrad(0, 0, w, (int)floorY, new Color(0.16f, 0.10f, 0.06f), new Color(0.26f, 0.17f, 0.10f));
            for (int py = 0; py < (int)floorY; py += 16)
                img.Rect(0, py, w, py + 1, new Color(0f, 0f, 0f, 0.22f));

            // Forge hood / chimney on the left.
            img.Rect((int)(w * 0.14f), (int)(h * 0.55f), (int)(w * 0.30f), h, new Color(0.10f, 0.09f, 0.09f));
            img.Tri((int)(w * 0.22f), (int)(h * 0.40f), (int)(w * 0.14f), (int)(h * 0.20f), new Color(0.12f, 0.10f, 0.09f));
            // Warm forge glow baked in.
            img.Radial((int)(w * 0.22f), (int)(h * 0.30f), h * 0.28f, new Color(1f, 0.55f, 0.22f, 0.28f), 2f);

            // Arched back door on the right.
            img.Rect((int)(w * 0.78f), (int)floorY, (int)(w * 0.90f), (int)(h * 0.72f), new Color(0.08f, 0.07f, 0.08f));
            img.Disc((int)(w * 0.84f), (int)(h * 0.72f), (int)(w * 0.06f), new Color(0.08f, 0.07f, 0.08f));

            // Shelves with jars on the right.
            for (int s = 0; s < 2; s++)
            {
                int sy = (int)(h * (0.62f + s * 0.14f));
                img.Rect((int)(w * 0.55f), sy, (int)(w * 0.72f), sy + 3, new Color(0.16f, 0.12f, 0.08f));
                for (int j = 0; j < 4; j++)
                {
                    int jx = (int)(w * 0.56f) + j * 14;
                    img.Rect(jx, sy + 3, jx + 9, sy + 16, new Color(0.22f, 0.20f, 0.16f, 0.9f));
                }
            }
            Vignette(img, 0.8f);
            img.Save(ForgeBackgroundPath, PpuBg);
        }

        private static void ForgeForeground()
        {
            const float W = 50f, H = 8f;
            var img = new Img(Mathf.RoundToInt(W * PpuBg), Mathf.RoundToInt(H * PpuBg));
            int w = img.w, h = img.h;
            // Low front beam.
            img.VGrad(0, 0, w, (int)(h * 0.30f), new Color(0.06f, 0.05f, 0.04f), new Color(0.11f, 0.08f, 0.06f));
            Grain(img, 0, 0, w, (int)(h * 0.30f), false, 0.08f);
            // Hanging chains.
            Chain(img, (int)(w * 0.14f), (int)(h * 0.30f), h);
            Chain(img, (int)(w * 0.86f), (int)(h * 0.30f), h);
            img.Save(ForgeForegroundPath, PpuBg);
        }

        // =====================================================================
        //  Shape helpers
        // =====================================================================

        private static void Building(Img img, int x0, int baseY, int bw, int bh, Color c, System.Random rng, bool windows)
        {
            img.Rect(x0, baseY, x0 + bw, baseY + bh, c);
            // Simple roof.
            if (rng.Next(2) == 0)
                img.Tri(x0 + bw / 2, baseY + bh + bh / 4, bw / 2 + 2, bh / 4, c);
            else
                img.Rect(x0 - 2, baseY + bh, x0 + bw + 2, baseY + bh + 3, c);
            if (!windows) return;
            for (int wy = baseY + 8; wy < baseY + bh - 6; wy += 14)
                for (int wx = x0 + 6; wx < x0 + bw - 6; wx += 14)
                    if (rng.NextDouble() < 0.45)
                        img.Rect(wx, wy, wx + 5, wy + 7, new Color(1f, 0.78f, 0.40f, 0.7f));
        }

        private static void Barrel(Img img, int cx, int by, int bw, int bh, System.Random rng)
        {
            img.Rect(cx - bw / 2, by, cx + bw / 2, by + bh, new Color(0.24f, 0.16f, 0.10f, 0.95f));
            img.Rect(cx - bw / 2, by + (int)(bh * 0.3f), cx + bw / 2, by + (int)(bh * 0.36f), new Color(0.12f, 0.09f, 0.06f, 0.9f));
            img.Rect(cx - bw / 2, by + (int)(bh * 0.7f), cx + bw / 2, by + (int)(bh * 0.76f), new Color(0.12f, 0.09f, 0.06f, 0.9f));
            img.Rect(cx - bw / 2, by, cx - bw / 2 + 2, by + bh, new Color(0.34f, 0.24f, 0.14f, 0.6f));
        }

        private static void Wheel(Img img, int cx, int cy, int r)
        {
            img.Ring(cx, cy, r, r - 4, new Color(0.14f, 0.10f, 0.07f, 0.9f));
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f;
                img.Line(cx, cy, cx + (int)(Mathf.Cos(ang) * r), cy + (int)(Mathf.Sin(ang) * r), 2, new Color(0.14f, 0.10f, 0.07f, 0.8f));
            }
        }

        private static void Plant(Img img, int cx, int by, int pw, int ph)
        {
            img.Rect(cx - pw / 2, by, cx + pw / 2, by + (int)(ph * 0.35f), new Color(0.28f, 0.18f, 0.10f, 0.95f));
            img.Disc(cx, by + (int)(ph * 0.6f), (int)(pw * 0.7f), new Color(0.18f, 0.26f, 0.14f, 0.9f));
            img.Disc(cx - pw / 3, by + (int)(ph * 0.5f), (int)(pw * 0.5f), new Color(0.16f, 0.24f, 0.13f, 0.9f));
        }

        private static void Fence(Img img, int x0, int x1, int y0, int y1, System.Random rng)
        {
            img.Rect(x0, y1 - 3, x1, y1 - 1, new Color(0.14f, 0.11f, 0.08f, 0.9f));
            for (int x = x0 + 4; x < x1 - 4; x += 16)
                img.Rect(x, y0, x + 4, y1, new Color(0.16f, 0.12f, 0.09f, 0.9f));
        }

        private static void Grain(Img img, int x0, int y0, int x1, int y1, bool vertical, float strength)
        {
            var rng = new System.Random((x0 * 73856093) ^ (y0 * 19349663) ^ (vertical ? 1 : 2));
            if (vertical)
                for (int x = x0; x < x1; x += 2 + rng.Next(3))
                    img.Rect(x, y0, x + 1, y1, new Color(0f, 0f, 0f, (float)rng.NextDouble() * strength));
            else
                for (int y = y0; y < y1; y += 2 + rng.Next(3))
                    img.Rect(x0, y, x1, y + 1, new Color(0f, 0f, 0f, (float)rng.NextDouble() * strength));
        }

        private static void IronBracket(Img img, int x, int y, int dirX, int dirY)
        {
            int s = 14;
            img.Rect(Mathf.Min(x, x + dirX * s), Mathf.Min(y, y + dirY * 4), Mathf.Max(x, x + dirX * s), Mathf.Max(y, y + dirY * 4), new Color(0.08f, 0.08f, 0.09f));
            img.Rect(Mathf.Min(x, x + dirX * 4), Mathf.Min(y, y + dirY * s), Mathf.Max(x, x + dirX * 4), Mathf.Max(y, y + dirY * s), new Color(0.08f, 0.08f, 0.09f));
            img.Disc(x + dirX * 4, y + dirY * 4, 2, new Color(0.3f, 0.3f, 0.32f));
        }

        private static void IronBand(Img img, int y)
        {
            img.Rect(0, y - 6, img.w, y + 6, new Color(0.09f, 0.09f, 0.10f));
            img.Disc((int)(img.w * 0.3f), y, 3, new Color(0.32f, 0.32f, 0.34f));
            img.Disc((int)(img.w * 0.7f), y, 3, new Color(0.32f, 0.32f, 0.34f));
        }

        private static void Hammer(Img img, int cx, int cy)
        {
            img.Rect(cx - 2, cy - 12, cx + 2, cy + 8, new Color(0.30f, 0.20f, 0.12f)); // handle
            img.Rect(cx - 10, cy + 6, cx + 10, cy + 14, new Color(0.20f, 0.20f, 0.22f)); // head
            img.Rect(cx - 10, cy + 6, cx + 10, cy + 8, new Color(0.5f, 0.4f, 0.25f, 0.6f)); // rim
        }

        private static void Tongs(Img img, int cx, int cy)
        {
            img.Line(cx - 6, cy - 12, cx, cy + 10, 2, new Color(0.20f, 0.20f, 0.22f));
            img.Line(cx + 6, cy - 12, cx, cy + 10, 2, new Color(0.20f, 0.20f, 0.22f));
            img.Rect(cx - 2, cy + 8, cx + 2, cy + 12, new Color(0.5f, 0.4f, 0.25f, 0.6f));
        }

        private static void Horseshoe(Img img, int cx, int cy)
        {
            img.Ring(cx, cy + 4, 10, 6, new Color(0.24f, 0.22f, 0.20f));
            img.Rect(cx - 10, cy - 6, cx + 10, cy + 4, new Color(0, 0, 0, 0)); // (Ring already leaves gap)
            img.Disc(cx - 6, cy - 2, 1, new Color(0.5f, 0.4f, 0.25f, 0.6f));
        }

        private static void Chain(Img img, int x, int y0, int y1)
        {
            for (int y = y0; y < y1; y += 8)
                img.Ring(x, y, 4, 2, new Color(0.12f, 0.11f, 0.10f, 0.9f));
        }

        private static void Vignette(Img img, float strength)
        {
            int w = img.w, h = img.h;
            float cx = w * 0.5f, cy = h * 0.5f;
            float max = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / max;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1f, d)) * strength;
                    if (a > 0f) img.Blend(x, y, new Color(0.02f, 0.02f, 0.03f, a));
                }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // =====================================================================
        //  Tiny software canvas
        // =====================================================================

        private class Img
        {
            public readonly int w, h;
            public readonly Color[] p;

            public Img(int w, int h)
            {
                this.w = w; this.h = h;
                p = new Color[w * h];
                for (int i = 0; i < p.Length; i++) p[i] = new Color(0, 0, 0, 0);
            }

            public float A(int x, int y) => (uint)x >= (uint)w || (uint)y >= (uint)h ? 0f : p[y * w + x].a;

            public void Blend(int x, int y, Color c)
            {
                if ((uint)x >= (uint)w || (uint)y >= (uint)h || c.a <= 0f) return;
                int i = y * w + x;
                Color d = p[i];
                float a = c.a;
                float outA = a + d.a * (1f - a);
                if (outA <= 0f) { p[i] = new Color(0, 0, 0, 0); return; }
                p[i] = new Color(
                    (c.r * a + d.r * d.a * (1f - a)) / outA,
                    (c.g * a + d.g * d.a * (1f - a)) / outA,
                    (c.b * a + d.b * d.a * (1f - a)) / outA,
                    outA);
            }

            public void ClearRect(int x0, int y0, int x1, int y1)
            {
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++)
                        p[y * w + x] = new Color(0, 0, 0, 0);
            }

            public void Row(int x0, int x1, int y, Color c)
            {
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) Blend(x, y, c);
            }

            public void Rect(int x0, int y0, int x1, int y1, Color c)
            {
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) Blend(x, y, c);
            }

            public void VGrad(int x0, int y0, int x1, int y1, Color lo, Color hi)
            {
                if (y1 <= y0) return;
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                {
                    float t = (float)(y - y0) / (y1 - y0);
                    Color c = Color.Lerp(lo, hi, t);
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) Blend(x, y, c);
                }
            }

            public void HGrad(int x0, int y0, int x1, int y1, Color lo, Color hi)
            {
                if (x1 <= x0) return;
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++)
                {
                    float t = (float)(x - x0) / (x1 - x0);
                    Color c = Color.Lerp(lo, hi, t);
                    for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++) Blend(x, y, c);
                }
            }

            public void Radial(int cx, int cy, float rad, Color c, float pow)
            {
                int r = Mathf.CeilToInt(rad);
                for (int y = cy - r; y <= cy + r; y++)
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / rad;
                        if (d >= 1f) continue;
                        float a = c.a * Mathf.Pow(1f - d, pow);
                        Blend(x, y, new Color(c.r, c.g, c.b, a));
                    }
            }

            public void Disc(int cx, int cy, int r, Color c)
            {
                for (int y = cy - r; y <= cy + r; y++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) Blend(x, y, c);
            }

            public void Ring(int cx, int cy, int rOuter, int rInner, Color c)
            {
                for (int y = cy - rOuter; y <= cy + rOuter; y++)
                    for (int x = cx - rOuter; x <= cx + rOuter; x++)
                    {
                        int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                        if (d2 <= rOuter * rOuter && d2 >= rInner * rInner) Blend(x, y, c);
                    }
            }

            public void Tri(int apexX, int apexY, int halfBase, int height, Color c)
            {
                for (int i = 0; i < height; i++)
                {
                    float t = (float)i / height;
                    int hb = (int)(halfBase * t);
                    int y = apexY - i;
                    Row(apexX - hb, apexX + hb, y, c);
                }
            }

            public void Line(int x0, int y0, int x1, int y1, int thick, Color c)
            {
                int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
                int err = dx - dy;
                while (true)
                {
                    Disc(x0, y0, thick, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x0 += sx; }
                    if (e2 < dx) { err += dx; y0 += sy; }
                }
            }

            public void Save(string path, float ppu)
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels(p);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = ppu;
                    imp.alphaIsTransparency = true;
                    imp.mipmapEnabled = false;
                    imp.wrapMode = TextureWrapMode.Clamp;
                    imp.filterMode = FilterMode.Bilinear;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                }
            }
        }
    }
}
