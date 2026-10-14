using System;
using UnityEngine;

namespace Bilye
{
    /// <summary>Procedural texture helpers: value noise, SDF shapes and a small "relief" renderer for metal/stone.</summary>
    public static class TexKit
    {
        public static float Sat(float x) => x < 0 ? 0 : x > 1 ? 1 : x;
        public static float Smooth(float a, float b, float x) { float t = Sat((x - a) / (b - a)); return t * t * (3 - 2 * t); }
        public static float Mix(float a, float b, float t) => a + (b - a) * t;

        public static float Hash(int x, int y, int seed = 0)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        public static float Noise(float x, float y, int seed = 0)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mix(Mix(a, b, fx), Mix(c, d, fx), fy);
        }

        public static float Fbm(float x, float y, int seed = 0)
        {
            float v = 0, a = 0.5f;
            for (int i = 0; i < 4; i++) { v += a * Noise(x, y, seed + i); x *= 2.03f; y *= 2.03f; a *= 0.5f; }
            return v;
        }

        /// <summary>Distance to the nearest cell point (voronoi). Returns F1 and the gap F2-F1 (cell borders).</summary>
        public static void Voronoi(float x, float y, int seed, out float f1, out float edge)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float d1 = 9, d2 = 9;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    float px = xi + i + Hash(xi + i, yi + j, seed), py = yi + j + Hash(xi + i, yi + j, seed + 77);
                    float dx = px - x, dy = py - y, dd = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dd < d1) { d2 = d1; d1 = dd; } else if (dd < d2) d2 = dd;
                }
            f1 = d1; edge = d2 - d1;
        }

        public static Color Lerp3(Color lo, Color mid, Color hi, float t)
            => t < 0.5f ? Color.Lerp(lo, mid, t * 2f) : Color.Lerp(mid, hi, (t - 0.5f) * 2f);

        public static Texture2D NewTex(int w, int h, bool mips = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, mips, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 0;
            return t;
        }

        public static Sprite ToSprite(Texture2D t, float ppu, Vector2? pivot = null, Vector4? border = null)
        {
            t.Apply(true, false);
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot ?? new Vector2(0.5f, 0.5f), ppu, 0,
                border.HasValue ? SpriteMeshType.FullRect : SpriteMeshType.FullRect, border ?? Vector4.zero);
        }

        // ---------------------------------------------------------------------------------------------
        // Relief renderer
        // ---------------------------------------------------------------------------------------------

        public sealed class Ramp
        {
            public Color lo, mid, hi;
            public float spec;   // 0..1 specular strength
            public Ramp(Color lo, Color mid, Color hi, float spec = 0.45f) { this.lo = lo; this.mid = mid; this.hi = hi; this.spec = spec; }
            public Ramp(string lo, string mid, string hi, float spec = 0.45f) : this(Pal.Hex(lo), Pal.Hex(mid), Pal.Hex(hi), spec) { }
        }

        public struct Px
        {
            public float a;      // coverage 0..1
            public float h;      // height (bigger = closer to the viewer)
            public Color c;      // diffuse colour (when ramp == null)
            public Ramp ramp;    // metal / stone ramp
            public float glow;   // emissive add 0..1 (uses glowColor)
            public Color glowColor;
        }

        public delegate void Sampler(float x, float y, ref Px p);

        static readonly Vector3 L = new Vector3(-0.5f, 0.62f, 0.6f).normalized;
        static readonly Vector3 H = (L + Vector3.forward).normalized;

        /// <summary>
        /// Renders a lit relief. x,y are in texture units centred on the middle (range +-w/2 / +-h/2 divided by `scale`),
        /// so painters can work in "ball diameters". y points up.
        /// </summary>
        public static Texture2D Relief(int w, int h, float scale, Sampler sampler, float bump = 3.2f)
        {
            var tex = NewTex(w, h);
            var hs = new float[w * h];
            var px = new Px[w * h];
            float inv = 1f / scale;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float x = (i + 0.5f - w * 0.5f) * inv, y = (j + 0.5f - h * 0.5f) * inv;
                    var p = new Px { a = 0, h = 0, c = Color.white, ramp = null, glow = 0, glowColor = Color.white };
                    sampler(x, y, ref p);
                    px[j * w + i] = p; hs[j * w + i] = p.h;
                }
            var outc = new Color32[w * h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    int k = j * w + i;
                    var p = px[k];
                    float hl = hs[j * w + Math.Max(0, i - 1)], hr = hs[j * w + Math.Min(w - 1, i + 1)];
                    float hd = hs[Math.Max(0, j - 1) * w + i], hu = hs[Math.Min(h - 1, j + 1) * w + i];
                    var n = new Vector3(-(hr - hl) * 0.5f * bump * scale, -(hu - hd) * 0.5f * bump * scale, 1f).normalized;
                    float ndl = Vector3.Dot(n, L);
                    Color col;
                    if (p.ramp != null)
                    {
                        float t = Sat(0.5f + (ndl - 0.6f) * 1.7f);
                        col = Lerp3(p.ramp.lo, p.ramp.mid, p.ramp.hi, t);
                        float sp = Mathf.Pow(Mathf.Max(0, Vector3.Dot(n, H)), 46f) * p.ramp.spec;
                        col = Color.Lerp(col, Color.white, Sat(sp));
                    }
                    else
                    {
                        float t = Sat(0.5f + (ndl - 0.6f) * 1.4f);
                        float m = Mix(0.5f, 1.25f, t);
                        col = new Color(p.c.r * m, p.c.g * m, p.c.b * m, 1);
                    }
                    if (p.glow > 0) col = Color.Lerp(col, p.glowColor, Sat(p.glow));
                    col.a = Sat(p.a);
                    outc[k] = col;
                }
            // transparent pixels borrow the colour of the nearest opaque neighbour so bilinear edges don't darken
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    int k = j * w + i;
                    if (outc[k].a == 255) continue;
                    if (outc[k].a > 0) continue;
                    for (int r = 1; r <= 3; r++)
                    {
                        bool found = false;
                        for (int dj = -r; dj <= r && !found; dj++)
                            for (int di = -r; di <= r && !found; di++)
                            {
                                int ii = i + di, jj = j + dj;
                                if (ii < 0 || jj < 0 || ii >= w || jj >= h) continue;
                                var o = outc[jj * w + ii];
                                if (o.a > 0) { outc[k] = new Color32(o.r, o.g, o.b, 0); found = true; }
                            }
                        if (found) break;
                    }
                }
            tex.SetPixels32(outc);
            return tex;
        }

        // ---------------------------------------------------------------------------------------------
        // shape helpers (all in "unit" coordinates, results are signed distances: < 0 inside)
        // ---------------------------------------------------------------------------------------------

        public static float SdCircle(float x, float y, float r) => Mathf.Sqrt(x * x + y * y) - r;
        public static float SdBox(float x, float y, float hx, float hy, float rad = 0)
        {
            float qx = Mathf.Abs(x) - hx + rad, qy = Mathf.Abs(y) - hy + rad;
            return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - rad;
        }
        /// <summary>coverage from a signed distance, with a transition `aa` wide (in the same units)</summary>
        public static float Cov(float sd, float aa) => Sat(0.5f - sd / aa);
    }
}
