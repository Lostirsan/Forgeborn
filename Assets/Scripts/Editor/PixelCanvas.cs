using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// A tiny software canvas for painting placeholder sprite art in the editor:
    /// alpha-over blending plus gradients, discs, rings, triangles, lines and a
    /// vignette, then saving as a properly-imported Sprite. Shared by the art
    /// generators so drawing code lives in one place.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int w, h;
        public readonly Color[] p;

        public PixelCanvas(int w, int h)
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
                    Blend(x, y, new Color(c.r, c.g, c.b, c.a * Mathf.Pow(1f - d, pow)));
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

        /// <summary>Filled triangle with a horizontal base of half-width, apex above.</summary>
        public void Tri(int apexX, int apexY, int halfBase, int height, Color c)
        {
            for (int i = 0; i < height; i++)
            {
                float t = (float)i / height;
                int hb = (int)(halfBase * t);
                Row(apexX - hb, apexX + hb, apexY - i, c);
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

        public void Vignette(float strength)
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float max = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / max;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, d)) * strength;
                    if (a > 0f) Blend(x, y, new Color(0.02f, 0.02f, 0.03f, a));
                }
        }

        /// <summary>Grain streaks (wood/metal). Deterministic per-region.</summary>
        public void Grain(int x0, int y0, int x1, int y1, bool vertical, float strength, int seed)
        {
            var rng = new System.Random(seed);
            if (vertical)
                for (int x = x0; x < x1; x += 2 + rng.Next(3))
                    Rect(x, y0, x + 1, y1, new Color(0f, 0f, 0f, (float)rng.NextDouble() * strength));
            else
                for (int y = y0; y < y1; y += 2 + rng.Next(3))
                    Rect(x0, y, x1, y + 1, new Color(0f, 0f, 0f, (float)rng.NextDouble() * strength));
        }

        public void Save(string path, float ppu)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(p);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
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
