using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// Paints stylised placeholder art for the bronze foundry: furnace, crucible,
    /// molten bronze, sword mould + fill mask, bronze stream, cast blade, anvil,
    /// assembly table, hammer, sparks and room back/foreground. Deterministic, saved
    /// as real Sprite assets. The scene builder and foundry/anvil panels reference
    /// these. Menu: <b>Tools ▸ Forge Game ▸ Generate Smithy Foundry Art</b>.
    /// </summary>
    public static class FoundryArtGenerator
    {
        public const string Dir = "Assets/Art/Generated/Smithy/Foundry";

        public const string Background = Dir + "/Foundry_Background.png";
        public const string Foreground = Dir + "/Foundry_Foreground.png";
        public const string Furnace = Dir + "/Foundry_Furnace.png";
        public const string FurnaceGlow = Dir + "/Foundry_FurnaceGlow.png";
        public const string Anvil = Dir + "/Foundry_Anvil.png";
        public const string AssemblyTable = Dir + "/Foundry_AssemblyTable.png";
        public const string Storage = Dir + "/Foundry_Storage.png";
        public const string Door = Dir + "/Foundry_Door.png";
        public const string Crucible = Dir + "/Foundry_Crucible.png";
        public const string CrucibleMolten = Dir + "/Foundry_CrucibleMoltenBronze.png";
        public const string SwordMoldClosed = Dir + "/Foundry_SwordMoldClosed.png";
        public const string MoldFillMask = Dir + "/Foundry_MoldFillMask.png";
        public const string MoldFill = Dir + "/Foundry_MoldFill.png";
        public const string BronzeStream = Dir + "/Foundry_BronzeStream.png";
        public const string CastBladeRaw = Dir + "/Foundry_CastBladeRaw.png";
        public const string CastBladeTexture = Dir + "/Foundry_CastBladeTexture.png";
        public const string Hammer = Dir + "/Foundry_Hammer.png";
        public const string Sparks = Dir + "/Foundry_Sparks.png";

        // Assembly component sprites (layered sword preview). Two visual variants per
        // slot so "Сменить" swaps the sprite even with a single data component.
        public const string BladeFinal = Dir + "/Foundry_BronzeBladeFinal.png";
        public const string GuardBasic = Dir + "/Foundry_GuardBasic.png";
        public const string GuardOrnate = Dir + "/Foundry_GuardOrnate.png";
        public const string HandleBasic = Dir + "/Foundry_HandleBasic.png";
        public const string HandleWrapped = Dir + "/Foundry_HandleWrapped.png";
        public const string PommelBasic = Dir + "/Foundry_PommelBasic.png";
        public const string PommelRound = Dir + "/Foundry_PommelRound.png";

        // Anvil section-grid overlay + foundry pour crucible + workbench panel skin.
        public const string BladeGrid = Dir + "/Foundry_BladeGrid.png";
        public const string PourCrucible = Dir + "/Foundry_PourCrucible.png";
        public const string PourCrucibleFwd = Dir + "/Foundry_PourCrucibleFwd.png"; // tilted TOWARD player: open bowl pouring down centre
        public const string WorkbenchPanel = Dir + "/Foundry_WorkbenchPanel.png";
        public const string Tang = Dir + "/Foundry_Tang.png"; // metal rod the hilt is threaded onto
        public const string MeltGauge = Dir + "/Foundry_MeltGauge.png";   // semicircular melt gauge (LOW→good→overheat)
        public const string GaugeNeedle = Dir + "/Foundry_GaugeNeedle.png";

        private static readonly Color BronzeLo = new Color(0.42f, 0.26f, 0.12f);
        private static readonly Color BronzeMid = new Color(0.72f, 0.48f, 0.24f);
        private static readonly Color BronzeHi = new Color(0.92f, 0.72f, 0.42f);
        private static readonly Color Stone = new Color(0.22f, 0.19f, 0.17f);
        private static readonly Color Wood = new Color(0.30f, 0.20f, 0.11f);
        private static readonly Color Iron = new Color(0.18f, 0.18f, 0.20f);
        private static readonly Color Warm = new Color(1f, 0.55f, 0.22f);

        [MenuItem("Tools/Forge Game/Generate Smithy Foundry Art")]
        public static void GenerateMenu()
        {
            GenerateAll(true);
            EditorUtility.DisplayDialog("Forge Game", "Арт плавильни сгенерирован в " + Dir, "OK");
        }

        public static void EnsureAll() => GenerateAll(false);

        private static void GenerateAll(bool force)
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Generated");
            EnsureFolder("Assets/Art/Generated/Smithy");
            EnsureFolder(Dir);

            Make(force, Background, MakeBackground);
            Make(force, Foreground, MakeForeground);
            Make(force, Furnace, MakeFurnace);
            Make(force, FurnaceGlow, MakeFurnaceGlow);
            Make(force, Anvil, MakeAnvil);
            Make(force, AssemblyTable, MakeAssemblyTable);
            Make(force, Storage, MakeStorage);
            Make(force, Door, MakeDoor);
            Make(force, Crucible, MakeCrucible);
            Make(force, CrucibleMolten, MakeCrucibleMolten);
            Make(force, SwordMoldClosed, MakeSwordMoldClosed);
            Make(force, MoldFillMask, MakeMoldFillMask);
            Make(force, MoldFill, MakeMoldFill);
            Make(force, BronzeStream, MakeBronzeStream);
            Make(force, CastBladeRaw, MakeCastBladeRaw);
            Make(force, CastBladeTexture, MakeCastBladeTexture);
            Make(force, Hammer, MakeHammer);
            Make(force, Sparks, MakeSparks);

            Make(force, BladeFinal, MakeBladeFinal);
            Make(force, GuardBasic, MakeGuardBasic);
            Make(force, GuardOrnate, MakeGuardOrnate);
            Make(force, HandleBasic, MakeHandleBasic);
            Make(force, HandleWrapped, MakeHandleWrapped);
            Make(force, PommelBasic, MakePommelBasic);
            Make(force, PommelRound, MakePommelRound);
            Make(force, BladeGrid, MakeBladeGrid);
            Make(force, PourCrucible, MakePourCrucible);
            Make(force, PourCrucibleFwd, MakePourCrucibleForward);
            Make(force, WorkbenchPanel, MakeWorkbenchPanel);
            Make(force, Tang, MakeTang);
            Make(force, MeltGauge, MakeMeltGauge);
            Make(force, GaugeNeedle, MakeGaugeNeedle);

            AssetDatabase.Refresh();
        }

        private static void Make(bool force, string path, Action fn)
        {
            if (force || !File.Exists(path)) fn();
        }

        // ===================================================================
        //  Room
        // ===================================================================

        private static void MakeBackground()
        {
            const float ppu = 22f;
            var c = new PixelCanvas(Mathf.RoundToInt(50 * ppu), Mathf.RoundToInt(26 * ppu));
            int w = c.w, h = c.h;
            float floorY = h * 0.24f;

            c.VGrad(0, (int)floorY, w, h, new Color(0.14f, 0.12f, 0.11f), new Color(0.24f, 0.19f, 0.16f));
            for (int by = (int)floorY; by < h; by += 22)
            {
                c.Rect(0, by, w, by + 1, new Color(0, 0, 0, 0.16f));
                int off = ((by / 22) % 2) * 22;
                for (int bx = off; bx < w; bx += 44) c.Rect(bx, by, bx + 1, by + 22, new Color(0, 0, 0, 0.12f));
            }
            c.VGrad(0, 0, w, (int)floorY, new Color(0.16f, 0.10f, 0.06f), new Color(0.28f, 0.18f, 0.10f));
            for (int py = 0; py < (int)floorY; py += 16) c.Rect(0, py, w, py + 1, new Color(0, 0, 0, 0.22f));

            // Left: furnace hood + warm baked glow.
            c.Rect((int)(w * 0.10f), (int)(h * 0.5f), (int)(w * 0.28f), h, new Color(0.10f, 0.09f, 0.09f));
            c.Tri((int)(w * 0.19f), (int)(h * 0.38f), (int)(w * 0.16f), (int)(h * 0.18f), new Color(0.12f, 0.10f, 0.09f));
            c.Radial((int)(w * 0.19f), (int)(h * 0.28f), h * 0.30f, new Color(Warm.r, Warm.g, Warm.b, 0.30f), 2f);
            // Right: shelves + back door.
            c.Rect((int)(w * 0.80f), (int)floorY, (int)(w * 0.90f), (int)(h * 0.70f), new Color(0.09f, 0.08f, 0.09f));
            c.Disc((int)(w * 0.85f), (int)(h * 0.70f), (int)(w * 0.05f), new Color(0.09f, 0.08f, 0.09f));
            for (int s = 0; s < 2; s++)
            {
                int sy = (int)(h * (0.60f + s * 0.14f));
                c.Rect((int)(w * 0.58f), sy, (int)(w * 0.74f), sy + 3, new Color(0.16f, 0.12f, 0.08f));
                for (int j = 0; j < 4; j++) { int jx = (int)(w * 0.59f) + j * 14; c.Rect(jx, sy + 3, jx + 9, sy + 16, new Color(0.22f, 0.20f, 0.16f, 0.9f)); }
            }
            c.Vignette(0.8f);
            c.Save(Background, ppu);
        }

        private static void MakeForeground()
        {
            const float ppu = 22f;
            var c = new PixelCanvas(Mathf.RoundToInt(50 * ppu), Mathf.RoundToInt(8 * ppu));
            int w = c.w, h = c.h;
            c.VGrad(0, 0, w, (int)(h * 0.32f), new Color(0.06f, 0.05f, 0.04f), new Color(0.11f, 0.08f, 0.06f));
            c.Grain(0, 0, w, (int)(h * 0.32f), false, 0.08f, 91);
            for (int cx = (int)(w * 0.12f); cx <= (int)(w * 0.88f); cx += (int)(w * 0.76f))
                for (int y = (int)(h * 0.32f); y < h; y += 8) c.Ring(cx, y, 4, 2, new Color(0.10f, 0.09f, 0.08f, 0.9f));
            c.Save(Foreground, ppu);
        }

        private static void MakeFurnace()
        {
            const float ppu = 36f;
            var c = new PixelCanvas(Mathf.RoundToInt(7 * ppu), Mathf.RoundToInt(9 * ppu));
            int w = c.w, h = c.h;
            // Stone body.
            c.VGrad((int)(w * 0.1f), 0, (int)(w * 0.9f), h, new Color(0.24f, 0.20f, 0.18f), new Color(0.34f, 0.28f, 0.24f));
            c.Grain((int)(w * 0.1f), 0, (int)(w * 0.9f), h, false, 0.10f, 5);
            // Rounded top.
            c.Disc(w / 2, (int)(h * 0.82f), (int)(w * 0.4f), new Color(0.30f, 0.25f, 0.22f));
            // Arched mouth with fire.
            c.Disc(w / 2, (int)(h * 0.42f), (int)(w * 0.26f), new Color(0.05f, 0.04f, 0.04f));
            c.Rect((int)(w * 0.24f), (int)(h * 0.18f), (int)(w * 0.76f), (int)(h * 0.42f), new Color(0.05f, 0.04f, 0.04f));
            c.Radial(w / 2, (int)(h * 0.32f), h * 0.20f, new Color(1f, 0.6f, 0.2f, 0.9f), 1.6f);
            c.Radial(w / 2, (int)(h * 0.30f), h * 0.09f, new Color(1f, 0.9f, 0.5f, 1f), 1.4f);
            // Iron banding.
            c.Rect((int)(w * 0.1f), (int)(h * 0.5f), (int)(w * 0.9f), (int)(h * 0.53f), Iron);
            c.Save(Furnace, ppu);
        }

        private static void MakeFurnaceGlow()
        {
            const float ppu = 24f;
            var c = new PixelCanvas(Mathf.RoundToInt(10 * ppu), Mathf.RoundToInt(10 * ppu));
            c.Radial(c.w / 2, c.h / 2, c.w * 0.5f, new Color(Warm.r, Warm.g, Warm.b, 0.6f), 2f);
            c.Save(FurnaceGlow, ppu);
        }

        private static void MakeAnvil()
        {
            const float ppu = 44f;
            var c = new PixelCanvas(Mathf.RoundToInt(6 * ppu), Mathf.RoundToInt(4.5f * ppu));
            int w = c.w, h = c.h;
            Color body = new Color(0.20f, 0.20f, 0.23f);
            // Wooden stump.
            c.Rect((int)(w * 0.34f), 0, (int)(w * 0.66f), (int)(h * 0.34f), Wood);
            c.Grain((int)(w * 0.34f), 0, (int)(w * 0.66f), (int)(h * 0.34f), true, 0.12f, 7);
            // Base.
            c.Rect((int)(w * 0.30f), (int)(h * 0.34f), (int)(w * 0.70f), (int)(h * 0.44f), body);
            // Waist.
            c.Rect((int)(w * 0.40f), (int)(h * 0.44f), (int)(w * 0.60f), (int)(h * 0.60f), body);
            // Face + horn.
            c.Rect((int)(w * 0.22f), (int)(h * 0.60f), (int)(w * 0.78f), (int)(h * 0.78f), body);
            c.Tri((int)(w * 0.95f), (int)(h * 0.74f), (int)(h * 0.09f), (int)(w * 0.22f), body); // horn to the right
            // Top highlight.
            c.Rect((int)(w * 0.22f), (int)(h * 0.74f), (int)(w * 0.78f), (int)(h * 0.78f), new Color(0.42f, 0.42f, 0.46f, 0.7f));
            c.Save(Anvil, ppu);
        }

        private static void MakeAssemblyTable()
        {
            const float ppu = 40f;
            var c = new PixelCanvas(Mathf.RoundToInt(7 * ppu), Mathf.RoundToInt(4.5f * ppu));
            int w = c.w, h = c.h;
            // Legs.
            c.Rect((int)(w * 0.14f), 0, (int)(w * 0.22f), (int)(h * 0.55f), new Color(0.20f, 0.13f, 0.08f));
            c.Rect((int)(w * 0.78f), 0, (int)(w * 0.86f), (int)(h * 0.55f), new Color(0.20f, 0.13f, 0.08f));
            // Top.
            c.VGrad((int)(w * 0.06f), (int)(h * 0.55f), (int)(w * 0.94f), (int)(h * 0.74f), new Color(0.28f, 0.18f, 0.10f), new Color(0.40f, 0.27f, 0.15f));
            c.Grain((int)(w * 0.06f), (int)(h * 0.55f), (int)(w * 0.94f), (int)(h * 0.74f), true, 0.10f, 11);
            // Tools & parts on top.
            c.Rect((int)(w * 0.20f), (int)(h * 0.74f), (int)(w * 0.30f), (int)(h * 0.80f), new Color(0.5f, 0.35f, 0.2f)); // guard
            c.Rect((int)(w * 0.45f), (int)(h * 0.74f), (int)(w * 0.49f), (int)(h * 0.90f), new Color(0.3f, 0.2f, 0.12f)); // handle
            c.Disc((int)(w * 0.66f), (int)(h * 0.79f), (int)(h * 0.05f), new Color(0.45f, 0.32f, 0.2f)); // pommel
            c.Save(AssemblyTable, ppu);
        }

        private static void MakeStorage()
        {
            const float ppu = 44f;
            var c = new PixelCanvas(Mathf.RoundToInt(4.5f * ppu), Mathf.RoundToInt(5f * ppu));
            int w = c.w, h = c.h;
            // Crate.
            c.VGrad((int)(w * 0.12f), 0, (int)(w * 0.88f), (int)(h * 0.72f), new Color(0.26f, 0.18f, 0.10f), new Color(0.36f, 0.25f, 0.14f));
            c.Grain((int)(w * 0.12f), 0, (int)(w * 0.88f), (int)(h * 0.72f), false, 0.10f, 13);
            c.Rect((int)(w * 0.12f), (int)(h * 0.35f), (int)(w * 0.88f), (int)(h * 0.38f), new Color(0, 0, 0, 0.25f));
            c.Line((int)(w * 0.12f), 0, (int)(w * 0.88f), (int)(h * 0.72f), 2, new Color(0.18f, 0.12f, 0.07f, 0.6f));
            // Bronze bars poking out.
            c.Rect((int)(w * 0.30f), (int)(h * 0.72f), (int)(w * 0.70f), (int)(h * 0.80f), BronzeMid);
            c.Rect((int)(w * 0.34f), (int)(h * 0.80f), (int)(w * 0.62f), (int)(h * 0.86f), BronzeHi);
            c.Save(Storage, ppu);
        }

        private static void MakeDoor()
        {
            const float ppu = 44f;
            var c = new PixelCanvas(Mathf.RoundToInt(4f * ppu), Mathf.RoundToInt(6f * ppu));
            int w = c.w, h = c.h;
            c.Rect((int)(w * 0.1f), 0, (int)(w * 0.9f), (int)(h * 0.9f), new Color(0.14f, 0.10f, 0.07f));
            c.Disc(w / 2, (int)(h * 0.9f), (int)(w * 0.4f), new Color(0.14f, 0.10f, 0.07f));
            c.Grain((int)(w * 0.1f), 0, (int)(w * 0.9f), (int)(h * 0.9f), true, 0.14f, 17);
            c.Radial(w / 2, (int)(h * 0.45f), h * 0.4f, new Color(0, 0, 0, 0.5f), 2f);
            c.Disc((int)(w * 0.7f), (int)(h * 0.45f), 4, new Color(0.4f, 0.36f, 0.3f)); // handle ring
            c.Save(Door, ppu);
        }

        // ===================================================================
        //  Foundry panel UI
        // ===================================================================

        private static void MakeCrucible()
        {
            var c = new PixelCanvas(256, 256);
            int w = c.w, h = c.h;
            // Pot body.
            c.VGrad((int)(w * 0.22f), (int)(h * 0.14f), (int)(w * 0.78f), (int)(h * 0.72f), new Color(0.18f, 0.14f, 0.12f), new Color(0.30f, 0.24f, 0.20f));
            c.Disc(w / 2, (int)(h * 0.14f), (int)(w * 0.28f), new Color(0.20f, 0.16f, 0.14f));
            // Rim.
            c.Ring(w / 2, (int)(h * 0.70f), (int)(w * 0.30f), (int)(w * 0.24f), new Color(0.34f, 0.28f, 0.24f));
            c.Grain((int)(w * 0.22f), (int)(h * 0.14f), (int)(w * 0.78f), (int)(h * 0.7f), true, 0.10f, 3);
            c.Save(Crucible, 100f);
        }

        private static void MakeCrucibleMolten()
        {
            var c = new PixelCanvas(256, 256);
            int w = c.w, h = c.h;
            // Molten bronze pool inside the crucible mouth.
            c.Disc(w / 2, (int)(h * 0.68f), (int)(w * 0.26f), BronzeMid);
            c.Radial(w / 2, (int)(h * 0.68f), w * 0.24f, new Color(1f, 0.85f, 0.5f, 0.9f), 1.6f);
            c.Radial(w / 2, (int)(h * 0.72f), w * 0.30f, new Color(1f, 0.6f, 0.2f, 0.5f), 2f);
            c.Save(CrucibleMolten, 100f);
        }

        private static void MakeSwordMoldClosed()
        {
            var c = new PixelCanvas(300, 620);
            int w = c.w, h = c.h;
            // Clay/stone mould block.
            c.VGrad((int)(w * 0.1f), (int)(h * 0.05f), (int)(w * 0.9f), (int)(h * 0.98f), new Color(0.22f, 0.19f, 0.17f), new Color(0.30f, 0.26f, 0.23f));
            c.Grain((int)(w * 0.1f), (int)(h * 0.05f), (int)(w * 0.9f), (int)(h * 0.98f), false, 0.06f, 23);
            // Parting line.
            c.Rect(w / 2 - 1, (int)(h * 0.05f), w / 2 + 1, (int)(h * 0.98f), new Color(0, 0, 0, 0.3f));
            // Engraved sword cavity (dark) + pour channel at bottom.
            DrawSwordVertical(c, w / 2, (int)(h * 0.10f), (int)(h * 0.92f), (int)(w * 0.13f), new Color(0.06f, 0.05f, 0.05f), new Color(0.05f, 0.04f, 0.04f));
            c.Rect(w / 2 - 10, (int)(h * 0.06f), w / 2 + 10, (int)(h * 0.14f), new Color(0.06f, 0.05f, 0.05f)); // pour funnel
            c.Save(SwordMoldClosed, 100f);
        }

        private static void MakeMoldFillMask()
        {
            var c = new PixelCanvas(220, 560);
            // Pure white sword silhouette used as a UI Mask.
            DrawSwordVertical(c, c.w / 2, (int)(c.h * 0.05f), (int)(c.h * 0.98f), (int)(c.w * 0.16f), Color.white, Color.white);
            c.Save(MoldFillMask, 100f);
        }

        private static void MakeMoldFill()
        {
            var c = new PixelCanvas(96, 560);
            // Bronze vertical gradient (masked to the sword shape at runtime).
            c.VGrad(0, 0, c.w, c.h, BronzeLo, BronzeHi);
            c.Save(MoldFill, 100f);
        }

        private static void MakeBronzeStream()
        {
            var c = new PixelCanvas(48, 360);
            int w = c.w, h = c.h;
            c.VGrad((int)(w * 0.3f), 0, (int)(w * 0.7f), h, new Color(1f, 0.55f, 0.2f, 1f), new Color(1f, 0.85f, 0.5f, 1f));
            c.Rect((int)(w * 0.42f), 0, (int)(w * 0.58f), h, new Color(1f, 0.95f, 0.7f, 0.8f));
            c.Save(BronzeStream, 100f);
        }

        private static void MakeCastBladeRaw()
        {
            var c = new PixelCanvas(620, 200);
            // Horizontal cast bronze blade (thick, slightly rough).
            DrawSwordHorizontal(c, (int)(c.w * 0.08f), (int)(c.w * 0.95f), c.h / 2, (int)(c.h * 0.32f), BronzeMid, BronzeLo, BronzeHi);
            c.Save(CastBladeRaw, 100f);
        }

        private static void MakeCastBladeTexture()
        {
            var c = new PixelCanvas(256, 128);
            var rng = new System.Random(77);
            c.VGrad(0, 0, c.w, c.h, BronzeLo, BronzeHi);
            for (int i = 0; i < 400; i++)
            {
                int x = rng.Next(c.w), y = rng.Next(c.h);
                float a = (float)rng.NextDouble() * 0.12f;
                c.Blend(x, y, new Color(rng.Next(2) == 0 ? 1f : 0f, rng.Next(2) == 0 ? 0.8f : 0f, 0.4f, a));
            }
            // Central fuller highlight band.
            c.Rect(0, (int)(c.h * 0.46f), c.w, (int)(c.h * 0.54f), new Color(1f, 0.9f, 0.6f, 0.25f));
            c.Save(CastBladeTexture, 100f);
        }

        private static void MakeHammer()
        {
            var c = new PixelCanvas(220, 300);
            int w = c.w, h = c.h;
            // Handle.
            c.VGrad((int)(w * 0.44f), 0, (int)(w * 0.56f), (int)(h * 0.72f), new Color(0.26f, 0.17f, 0.09f), new Color(0.40f, 0.28f, 0.16f));
            c.Grain((int)(w * 0.44f), 0, (int)(w * 0.56f), (int)(h * 0.72f), true, 0.12f, 31);
            // Head.
            c.VGrad((int)(w * 0.18f), (int)(h * 0.70f), (int)(w * 0.82f), (int)(h * 0.94f), new Color(0.16f, 0.16f, 0.18f), new Color(0.30f, 0.30f, 0.34f));
            c.Rect((int)(w * 0.18f), (int)(h * 0.70f), (int)(w * 0.82f), (int)(h * 0.73f), new Color(0.5f, 0.4f, 0.25f, 0.5f));
            c.Save(Hammer, 100f);
        }

        private static void MakeSparks()
        {
            var c = new PixelCanvas(128, 128);
            int cx = c.w / 2, cy = c.h / 2;
            var rng = new System.Random(51);
            c.Radial(cx, cy, 20, new Color(1f, 0.9f, 0.5f, 0.8f), 2f);
            for (int i = 0; i < 14; i++)
            {
                float ang = (float)(rng.NextDouble() * Math.PI * 2);
                int len = 24 + rng.Next(34);
                c.Line(cx, cy, cx + (int)(Mathf.Cos(ang) * len), cy + (int)(Mathf.Sin(ang) * len), 1, new Color(1f, 0.7f, 0.3f, 0.9f));
            }
            c.Save(Sparks, 100f);
        }

        // ===================================================================
        //  Assembly component sprites (transparent, layered into one sword)
        // ===================================================================

        private static void MakeBladeFinal()
        {
            var c = new PixelCanvas(150, 580);
            int w = c.w, h = c.h, cx = w / 2;
            int baseY = (int)(h * 0.16f), tipY = (int)(h * 0.985f), half = 24;
            // Tang below the guard line.
            c.Rect(cx - 8, (int)(h * 0.02f), cx + 8, baseY, new Color(0.30f, 0.20f, 0.11f));
            // Blade, tapering to the tip, bright fuller centre fading to dark edges.
            int span = tipY - baseY;
            for (int y = baseY; y < tipY; y++)
            {
                float t = (float)(y - baseY) / span;
                int hh = (int)(half * (t < 0.85f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.85f) / 0.15f)));
                for (int x = cx - hh; x <= cx + hh; x++)
                {
                    float v = Mathf.Abs(x - cx) / (float)Mathf.Max(1, hh);
                    c.Blend(x, y, Color.Lerp(BronzeHi, BronzeLo, v * v));
                }
            }
            // Fuller highlight line.
            c.Rect(cx - 2, baseY, cx + 3, baseY + (int)(span * 0.8f), new Color(1f, 0.94f, 0.66f, 0.30f));
            c.Save(BladeFinal, 100f);
        }

        private static void MakeGuardBasic()
        {
            var c = new PixelCanvas(250, 74);
            int w = c.w, h = c.h;
            c.VGrad((int)(w * 0.06f), (int)(h * 0.34f), (int)(w * 0.94f), (int)(h * 0.66f), BronzeLo, BronzeMid);
            c.Rect((int)(w * 0.44f), (int)(h * 0.18f), (int)(w * 0.56f), (int)(h * 0.82f), BronzeMid);
            c.Rect((int)(w * 0.06f), (int)(h * 0.46f), (int)(w * 0.94f), (int)(h * 0.52f), new Color(1f, 0.86f, 0.52f, 0.42f));
            c.Save(GuardBasic, 100f);
        }

        private static void MakeGuardOrnate()
        {
            var c = new PixelCanvas(260, 90);
            int w = c.w, h = c.h, cy = h / 2;
            // Slightly drooping quillons + rounded ends.
            c.VGrad((int)(w * 0.05f), (int)(h * 0.40f), (int)(w * 0.95f), (int)(h * 0.62f), BronzeLo, BronzeMid);
            c.Disc((int)(w * 0.07f), cy, (int)(h * 0.22f), BronzeMid);
            c.Disc((int)(w * 0.93f), cy, (int)(h * 0.22f), BronzeMid);
            c.Disc((int)(w * 0.5f), cy, (int)(h * 0.30f), BronzeHi);
            c.Ring((int)(w * 0.5f), cy, (int)(h * 0.30f), (int)(h * 0.22f), BronzeLo);
            c.Save(GuardOrnate, 100f);
        }

        private static void MakeHandleBasic()
        {
            var c = new PixelCanvas(74, 210);
            int w = c.w, h = c.h;
            c.VGrad((int)(w * 0.28f), (int)(h * 0.05f), (int)(w * 0.72f), (int)(h * 0.95f),
                new Color(0.24f, 0.15f, 0.08f), new Color(0.38f, 0.25f, 0.14f));
            for (int i = 1; i < 6; i++)
            {
                int y = (int)(h * (0.08f + i * 0.15f));
                c.Rect((int)(w * 0.28f), y, (int)(w * 0.72f), y + 3, new Color(0.14f, 0.09f, 0.05f, 0.8f));
            }
            c.Save(HandleBasic, 100f);
        }

        private static void MakeHandleWrapped()
        {
            var c = new PixelCanvas(74, 210);
            int w = c.w, h = c.h;
            c.VGrad((int)(w * 0.26f), (int)(h * 0.05f), (int)(w * 0.74f), (int)(h * 0.95f),
                new Color(0.20f, 0.12f, 0.06f), new Color(0.34f, 0.22f, 0.12f));
            // Diagonal leather wrap.
            for (int i = -6; i < 14; i++)
            {
                int y0 = (int)(h * (0.05f + i * 0.09f));
                c.Line((int)(w * 0.26f), y0, (int)(w * 0.74f), y0 + (int)(h * 0.09f), 2, new Color(0.12f, 0.08f, 0.04f, 0.85f));
            }
            c.Save(HandleWrapped, 100f);
        }

        private static void MakePommelBasic()
        {
            var c = new PixelCanvas(104, 104);
            int cx = c.w / 2, cy = c.h / 2;
            c.Disc(cx, cy, (int)(c.w * 0.34f), BronzeMid);
            c.Ring(cx, cy, (int)(c.w * 0.34f), (int)(c.w * 0.30f), BronzeLo);
            c.Radial((int)(cx - c.w * 0.08f), (int)(cy + c.h * 0.08f), c.w * 0.20f, new Color(1f, 0.9f, 0.6f, 0.6f), 2f);
            c.Save(PommelBasic, 100f);
        }

        private static void MakePommelRound()
        {
            var c = new PixelCanvas(104, 116);
            int cx = c.w / 2;
            // Faceted diamond: two triangles apex up / apex down.
            c.Tri(cx, (int)(c.h * 0.92f), (int)(c.w * 0.34f), (int)(c.h * 0.42f), BronzeMid);
            for (int i = 0; i < (int)(c.h * 0.42f); i++)
            {
                float t = (float)i / (c.h * 0.42f);
                int hb = (int)(c.w * 0.34f * t);
                c.Row(cx - hb, cx + hb, (int)(c.h * 0.08f) + i, BronzeLo);
            }
            c.Rect(cx - 2, (int)(c.h * 0.1f), cx + 2, (int)(c.h * 0.9f), new Color(1f, 0.9f, 0.6f, 0.4f));
            c.Save(PommelRound, 100f);
        }

        // ===================================================================
        //  Anvil grid + foundry pour + panel skin
        // ===================================================================

        private static void MakeBladeGrid()
        {
            var c = new PixelCanvas(560, 180);
            int w = c.w, h = c.h, n = 14;
            var line = new Color(1f, 0.96f, 0.86f, 1f); // faint via the Image tint alpha at runtime
            for (int i = 1; i < n; i++)
            {
                int x = (int)(w * (i / (float)n));
                c.Rect(x, (int)(h * 0.10f), x + 1, (int)(h * 0.90f), line);
            }
            c.Rect(0, h / 2, w, h / 2 + 1, new Color(1f, 0.96f, 0.86f, 0.7f)); // centre reference
            c.Save(BladeGrid, 100f);
        }

        private static void MakePourCrucible()
        {
            var c = new PixelCanvas(200, 170);
            int w = c.w, h = c.h;
            // Tilted pot, lip low on the right where the stream leaves.
            c.VGrad((int)(w * 0.16f), (int)(h * 0.36f), (int)(w * 0.78f), (int)(h * 0.86f),
                new Color(0.18f, 0.14f, 0.12f), new Color(0.30f, 0.24f, 0.20f));
            c.Disc((int)(w * 0.47f), (int)(h * 0.86f), (int)(w * 0.31f), new Color(0.26f, 0.21f, 0.18f));
            c.Ring((int)(w * 0.47f), (int)(h * 0.86f), (int)(w * 0.31f), (int)(w * 0.26f), new Color(0.34f, 0.28f, 0.24f));
            // Molten bronze pooling at the spout.
            c.Disc((int)(w * 0.47f), (int)(h * 0.82f), (int)(w * 0.24f), BronzeMid);
            c.Radial((int)(w * 0.72f), (int)(h * 0.6f), w * 0.16f, new Color(1f, 0.75f, 0.35f, 0.95f), 1.6f);
            c.Save(PourCrucible, 100f);
        }

        // Crucible tilted TOWARD the player: we look down into the open bowl, molten inside,
        // and a spout at front-centre pours a column straight down. Used only while pouring, so
        // it swaps in for the upright pot instead of rotating the pot sideways. Aspect ~430:350
        // to match the crucible RectTransform.
        private static void MakePourCrucibleForward()
        {
            var c = new PixelCanvas(300, 244);
            int w = c.w, h = c.h;
            var bodyLo = new Color(0.14f, 0.12f, 0.11f);
            var bodyHi = new Color(0.30f, 0.25f, 0.21f);
            var rimLo = new Color(0.40f, 0.40f, 0.44f);
            var rimHi = new Color(0.66f, 0.66f, 0.70f);
            var cavity = new Color(0.14f, 0.09f, 0.07f);
            var molten = new Color(1f, 0.52f, 0.14f);
            var moltenHot = new Color(1f, 0.82f, 0.42f);
            var handle = new Color(0.09f, 0.09f, 0.10f);

            // Side handles (the pivot the pot swings on).
            c.Rect((int)(w * 0.02f), (int)(h * 0.44f), (int)(w * 0.20f), (int)(h * 0.54f), handle);
            c.Rect((int)(w * 0.80f), (int)(h * 0.44f), (int)(w * 0.98f), (int)(h * 0.54f), handle);
            c.Disc((int)(w * 0.20f), (int)(h * 0.49f), (int)(w * 0.05f), new Color(0.20f, 0.20f, 0.22f));
            c.Disc((int)(w * 0.80f), (int)(h * 0.49f), (int)(w * 0.05f), new Color(0.20f, 0.20f, 0.22f));

            // Bowl body (rounded bucket).
            int bx0 = (int)(w * 0.18f), bx1 = (int)(w * 0.82f), by0 = (int)(h * 0.10f), by1 = (int)(h * 0.72f);
            c.VGrad(bx0, by0, bx1, by1, bodyLo, bodyHi);
            c.Disc(w / 2, (int)(h * 0.16f), (int)(w * 0.31f), bodyLo);   // rounded base
            c.Disc(bx0, (int)(h * 0.42f), (int)(w * 0.09f), bodyHi);     // left cheek
            c.Disc(bx1, (int)(h * 0.42f), (int)(w * 0.09f), bodyHi);     // right cheek
            c.Grain(bx0, by0, bx1, by1, true, 0.07f, 7);

            // Open elliptical mouth (seen from front-above).
            int rcx = w / 2, rcy = (int)(h * 0.72f);
            int ra = (int)(w * 0.32f), rb = (int)(h * 0.13f);
            EllipseFill(c, rcx, rcy, ra, rb, rimLo);
            EllipseFill(c, rcx, rcy + 2, ra - 4, rb - 2, rimHi);
            EllipseFill(c, rcx, rcy, ra - 12, rb - 5, cavity);
            EllipseFill(c, rcx, rcy - 1, ra - 18, rb - 7, molten);
            EllipseFill(c, rcx, rcy - 1, ra - 30, rb - 10, moltenHot);

            // Front spout: a V-notch at the mouth's near lip and a molten column down the centre.
            int sHalf = (int)(w * 0.055f);
            c.Rect(rcx - sHalf - 3, 0, rcx + sHalf + 3, rcy - rb + 4, rimLo);        // lip channel walls
            c.Rect(rcx - sHalf, 0, rcx + sHalf, rcy - rb + 8, molten);               // molten column
            c.Rect(rcx - sHalf + 3, 0, rcx + sHalf - 3, rcy - rb + 6, moltenHot);    // hot core
            c.Radial(rcx, rcy - rb + 6, w * 0.10f, new Color(1f, 0.8f, 0.4f, 0.85f), 1.7f); // spout glow

            c.Save(PourCrucibleFwd, 100f);
        }

        // Solid axis-aligned ellipse (no native primitive in PixelCanvas).
        private static void EllipseFill(PixelCanvas c, int cx, int cy, int a, int b, Color col)
        {
            if (a <= 0 || b <= 0) return;
            for (int dy = -b; dy <= b; dy++)
            {
                float t = 1f - (float)(dy * dy) / (b * b);
                if (t < 0f) continue;
                int half = (int)(a * Mathf.Sqrt(t));
                c.Row(cx - half, cx + half, cy + dy, col);
            }
        }

        private static void MakeTang()
        {
            // A short, stylised metal rod: cylindrical shading (dark edges → bright centre),
            // rounded top, darkened base where it leaves the guard. Reads as a hilt tang.
            var c = new PixelCanvas(48, 440);
            int w = c.w, h = c.h;
            int x0 = (int)(w * 0.34f), x1 = (int)(w * 0.66f);
            var edge = new Color(0.34f, 0.26f, 0.16f);
            var mid = new Color(0.78f, 0.60f, 0.36f);
            c.HGrad(x0, (int)(h * 0.03f), w / 2, (int)(h * 0.97f), edge, mid);           // left → centre
            c.HGrad(w / 2, (int)(h * 0.03f), x1, (int)(h * 0.97f), mid, edge);            // centre → right
            c.Disc(w / 2, (int)(h * 0.965f), (x1 - x0) / 2, new Color(0.7f, 0.53f, 0.30f)); // rounded top
            c.VGrad(x0, (int)(h * 0.03f), x1, (int)(h * 0.22f), new Color(0f, 0f, 0f, 0.30f), new Color(0f, 0f, 0f, 0f)); // shadow at base
            c.Rect((int)(w * 0.46f), (int)(h * 0.06f), (int)(w * 0.53f), (int)(h * 0.95f), new Color(1f, 0.92f, 0.68f, 0.32f)); // specular
            c.Save(Tang, 100f);
        }

        private static void MakeMeltGauge()
        {
            // Semicircular gauge: LOW (left, cold) → amber → good/green (right) → overheat (far right red).
            var c = new PixelCanvas(440, 250);
            int cx = c.w / 2, cy = 30, rO = 200, rI = 138;
            for (int y = cy; y < c.h; y++)
                for (int x = 0; x < c.w; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < rI * rI || d2 > rO * rO) continue;
                    float ang = Mathf.Atan2(dy, dx);          // 0 = right, π = left
                    float heat = 1f - Mathf.Clamp01(ang / Mathf.PI); // 0 left/cold, 1 right/hot
                    Color col = heat < 0.18f ? new Color(0.30f, 0.34f, 0.40f)          // LOW (cold)
                              : heat < 0.70f ? new Color(0.80f, 0.52f, 0.22f)          // melting
                              : heat < 0.90f ? new Color(0.45f, 0.72f, 0.35f)          // ready (good)
                                             : new Color(0.80f, 0.30f, 0.22f);         // overheat
                    c.Blend(x, y, col);
                }
            // Inner + outer rim.
            c.Ring(cx, cy, rO, rO - 4, new Color(0.08f, 0.07f, 0.06f, 0.9f));
            c.Ring(cx, cy, rI + 3, rI, new Color(0.08f, 0.07f, 0.06f, 0.9f));
            for (int y = 0; y < cy; y++) c.Row(0, c.w, y, new Color(0, 0, 0, 0)); // clear above axis
            c.Save(MeltGauge, 100f);
        }

        private static void MakeGaugeNeedle()
        {
            var c = new PixelCanvas(40, 210);
            int w = c.w;
            c.Tri(w / 2, (int)(c.h * 0.98f), (int)(w * 0.30f), (int)(c.h * 0.9f), new Color(0.95f, 0.9f, 0.85f)); // pointer up
            c.Disc(w / 2, (int)(c.h * 0.08f), (int)(w * 0.34f), new Color(0.9f, 0.85f, 0.8f));                     // hub
            c.Save(GaugeNeedle, 100f);
        }

        private static void MakeWorkbenchPanel()
        {
            var c = new PixelCanvas(900, 620);
            int w = c.w, h = c.h;
            // Aged wood planks.
            c.VGrad(0, 0, w, h, new Color(0.20f, 0.14f, 0.09f), new Color(0.28f, 0.20f, 0.13f));
            for (int py = 0; py < h; py += 84)
            {
                c.Rect(0, py, w, py + 2, new Color(0, 0, 0, 0.28f));
                c.Grain(0, py + 2, w, py + 82, false, 0.07f, 40 + py);
            }
            // Iron-bound frame.
            int b = 16;
            c.Rect(0, 0, w, b, Iron); c.Rect(0, h - b, w, h, Iron);
            c.Rect(0, 0, b, h, Iron); c.Rect(w - b, 0, w, h, Iron);
            c.Rect(b, b, w - b, b + 4, new Color(0f, 0f, 0f, 0.35f));
            c.Vignette(0.6f);
            c.Save(WorkbenchPanel, 100f);
        }

        // ===================================================================
        //  Sword shapes
        // ===================================================================

        private static void DrawSwordVertical(PixelCanvas c, int cx, int baseY, int topY, int bladeHalf, Color blade, Color hilt)
        {
            int gripTop = baseY + (int)((topY - baseY) * 0.16f);
            int guardY = gripTop;
            // Pommel.
            c.Disc(cx, baseY, (int)(bladeHalf * 0.7f), hilt);
            // Grip.
            c.Rect(cx - (int)(bladeHalf * 0.35f), baseY, cx + (int)(bladeHalf * 0.35f), gripTop, hilt);
            // Guard.
            c.Rect(cx - (int)(bladeHalf * 1.6f), guardY, cx + (int)(bladeHalf * 1.6f), guardY + (int)(bladeHalf * 0.5f), hilt);
            // Blade with a taper to the tip.
            int bStart = guardY + (int)(bladeHalf * 0.5f);
            int span = topY - bStart;
            for (int y = bStart; y < topY; y++)
            {
                float t = (float)(y - bStart) / span;
                int half = (int)(bladeHalf * (t < 0.82f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.82f) / 0.18f)));
                c.Row(cx - half, cx + half, y, blade);
            }
        }

        private static void DrawSwordHorizontal(PixelCanvas c, int tangX, int tipX, int cy, int bladeHalf, Color blade, Color lo, Color hi)
        {
            int guardX = tangX + (int)((tipX - tangX) * 0.14f);
            // Grip + pommel + guard on the left.
            c.Disc(tangX, cy, (int)(bladeHalf * 0.6f), lo);
            c.Rect(tangX, cy - (int)(bladeHalf * 0.3f), guardX, cy + (int)(bladeHalf * 0.3f), lo);
            c.Rect(guardX, cy - (int)(bladeHalf * 1.5f), guardX + (int)(bladeHalf * 0.4f), cy + (int)(bladeHalf * 1.5f), lo);
            // Blade tapering to the tip on the right.
            int bStart = guardX + (int)(bladeHalf * 0.4f);
            int span = tipX - bStart;
            for (int x = bStart; x < tipX; x++)
            {
                float t = (float)(x - bStart) / span;
                int half = (int)(bladeHalf * (t < 0.82f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.82f) / 0.18f)));
                for (int y = cy - half; y <= cy + half; y++)
                {
                    float v = Mathf.Abs(y - cy) / (float)Mathf.Max(1, half);
                    c.Blend(x, y, Color.Lerp(hi, lo, v)); // central highlight fading to darker edges
                }
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
    }
}
