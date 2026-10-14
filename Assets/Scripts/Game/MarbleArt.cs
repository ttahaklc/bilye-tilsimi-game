using System;
using UnityEngine;

namespace Bilye
{
    /// <summary>
    /// One atlas per world: six rolling marble bodies (albedo + rotation-invariant shading only), a shared
    /// non-rolling gloss overlay (directional light + specular) and a soft drop shadow.
    /// </summary>
    public sealed class MarbleSet
    {
        public Texture2D atlas;
        public Sprite[] body = new Sprite[6];
        public Sprite gloss, shadow;
        public WorldStyle style;
        public const float Fill = 0.92f; // ball diameter as a fraction of the sprite
    }

    public static class MarbleArt
    {
        const int Cell = 224;
        static readonly System.Collections.Generic.Dictionary<int, MarbleSet> cache = new System.Collections.Generic.Dictionary<int, MarbleSet>();

        public static MarbleSet Get(WorldStyle st)
        {
            if (cache.TryGetValue(st.key, out var s)) return s;
            s = Build(st);
            cache[st.key] = s;
            return s;
        }

        static MarbleSet Build(WorldStyle st)
        {
            var set = new MarbleSet { style = st };
            int cols = 4, rows = 2;
            var tex = TexKit.NewTex(Cell * cols, Cell * rows);
            var px = new Color32[Cell * cols * Cell * rows];
            for (int i = 0; i < 6; i++) PaintBody(px, cols, i % cols, i / cols, st.ball, Pal.Balls[i], i);
            PaintGloss(px, cols, 2, 1);
            PaintShadow(px, cols, 3, 1);
            tex.SetPixels32(px);
            tex.Apply(true, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            set.atlas = tex;
            for (int i = 0; i < 6; i++) set.body[i] = Sprite.Create(tex, new Rect((i % cols) * Cell, (i / cols) * Cell, Cell, Cell), new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
            set.gloss = Sprite.Create(tex, new Rect(2 * Cell, 1 * Cell, Cell, Cell), new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
            set.shadow = Sprite.Create(tex, new Rect(3 * Cell, 1 * Cell, Cell, Cell), new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
            return set;
        }

        static void Put(Color32[] px, int cols, int cx, int cy, int i, int j, Color c)
        {
            px[(cy * Cell + j) * (Cell * cols) + cx * Cell + i] = c;
        }

        // ---------------------------------------------------------------------------------------------

        static void PaintBody(Color32[] px, int cols, int cx, int cy, BallStyle style, BallPal pal, int ci)
        {
            float R = Cell * 0.5f * MarbleSet.Fill;
            for (int j = 0; j < Cell; j++)
                for (int i = 0; i < Cell; i++)
                {
                    float dx = i + 0.5f - Cell * 0.5f, dy = j + 0.5f - Cell * 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float cov = TexKit.Sat(R - dist + 0.5f);
                    float rr = Mathf.Min(dist, R - 0.5f) / R;
                    float u = dx / R, v = dy / R;
                    if (dist > R - 0.5f) { float k = (R - 0.5f) / Mathf.Max(dist, 0.001f); u *= k; v *= k; }
                    Color c = Albedo(style, pal, u, v, rr, ci);
                    // rotation-invariant depth: darker towards the limb, a coloured scatter glow on the very edge
                    float limb = TexKit.Smooth(0.5f, 1f, rr);
                    c = Color.Lerp(c, pal.dark, 0.38f * limb * limb);
                    float edge = TexKit.Smooth(0.82f, 0.97f, rr);
                    c = Color.Lerp(c, pal.glow, 0.22f * edge);
                    c.a = cov;
                    Put(px, cols, cx, cy, i, j, c);
                }
        }

        static float Seg(float a, float n) // angular sector helper: distance (0..0.5) to the nearest sector border
        {
            float t = a / (Mathf.PI * 2f) * n; t = t - Mathf.Floor(t);
            return Mathf.Min(t, 1f - t);
        }

        static Color Albedo(BallStyle st, BallPal p, float u, float v, float r, int ci)
        {
            float a = Mathf.Atan2(v, u);
            Color baseC = p.baseC, light = p.light, dark = p.dark;
            switch (st)
            {
                case BallStyle.Orb:
                {
                    Color c = Color.Lerp(baseC, dark, TexKit.Smooth(0.15f, 1f, r) * 0.65f);
                    float caustic = TexKit.Sat((u * 0.35f - v * 0.65f) * 1.25f) * TexKit.Smooth(0.35f, 0.88f, r);
                    c = Color.Lerp(c, light, caustic * 0.55f);
                    float core = TexKit.Smooth(0.5f, 0f, Mathf.Sqrt((u - 0.08f) * (u - 0.08f) + (v + 0.1f) * (v + 0.1f)));
                    c = Color.Lerp(c, light, core * 0.18f);
                    float n = TexKit.Fbm(u * 3 + ci * 9, v * 3, 4); c = Color.Lerp(c, light, (n - 0.45f) * 0.25f);
                    return c;
                }
                case BallStyle.Gem:
                {
                    float sec = Mathf.Floor((a + Mathf.PI) / (Mathf.PI * 2f) * 8f);
                    float fb = 0.62f + 0.55f * TexKit.Hash((int)sec, ci, 3);
                    Color c = Color.Lerp(dark, light, Mathf.Clamp01(fb * 0.8f));
                    c = Color.Lerp(c, baseC, 0.35f);
                    float oct = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)); oct = Mathf.Max(oct, (Mathf.Abs(u) + Mathf.Abs(v)) * 0.7071f);
                    if (oct < 0.46f) c = Color.Lerp(baseC, light, 0.35f + 0.25f * TexKit.Noise(u * 4, v * 4, ci));
                    float ln = 1f - TexKit.Smooth(0f, 0.035f, Mathf.Abs(Seg(a + Mathf.PI, 8f) * 2f * Mathf.PI / 8f) * r);
                    c = Color.Lerp(c, dark, ln * 0.5f * TexKit.Smooth(0.46f, 0.55f, oct));
                    float ol = 1f - TexKit.Smooth(0f, 0.03f, Mathf.Abs(oct - 0.46f));
                    c = Color.Lerp(c, light, ol * 0.55f);
                    return c;
                }
                case BallStyle.Pearl:
                {
                    Color pearl = Color.Lerp(Color.white, light, 0.45f);
                    float n = TexKit.Fbm(u * 2.2f + 3, v * 2.2f, ci);
                    Color c = Color.Lerp(pearl, baseC, 0.25f + 0.5f * n * (0.4f + r));
                    float sheen = Mathf.Sin((u * 0.8f + v * 1.1f) * 7f + n * 3f) * 0.5f + 0.5f;
                    c = Color.Lerp(c, Color.Lerp(light, Color.white, 0.6f), sheen * 0.22f);
                    return c;
                }
                case BallStyle.Hex:
                {
                    float sx = u * 3.4f, sy = v * 3.4f;
                    float rx = 1f, ry = 1.7320508f, hx = 0.5f, hy = 0.8660254f;
                    float ax = Mathf.Repeat(sx, rx) - hx, ay = Mathf.Repeat(sy, ry) - hy;
                    float bx = Mathf.Repeat(sx - hx, rx) - hx, by = Mathf.Repeat(sy - hy, ry) - hy;
                    float gx = ax * ax + ay * ay < bx * bx + by * by ? ax : bx;
                    float gy = ax * ax + ay * ay < bx * bx + by * by ? ay : by;
                    float cc = Mathf.Max(Mathf.Abs(gx) * 0.5f + Mathf.Abs(gy) * 0.866f, Mathf.Abs(gx));
                    float edge = 0.5f - cc;
                    Color cell = Color.Lerp(baseC, light, TexKit.Smooth(0.25f, 0f, edge) * 0.0f + (1f - edge * 2.6f) * 0.0f + 0.35f * TexKit.Smooth(0.02f, 0.3f, edge));
                    cell = Color.Lerp(cell, dark, 0.25f * (1f - TexKit.Smooth(0.0f, 0.22f, edge)));
                    float wall = 1f - TexKit.Smooth(0.02f, 0.085f, edge);
                    return Color.Lerp(cell, Color.Lerp(dark, Color.black, 0.2f), wall * 0.9f);
                }
                case BallStyle.Crystal:
                {
                    float sec = Mathf.Floor((a + Mathf.PI) / (Mathf.PI * 2f) * 12f);
                    float ring = r < 0.5f ? 0f : 1f;
                    float fb = TexKit.Hash((int)sec, (int)ring, 9 + ci);
                    float alt = ((int)sec & 1) == 0 ? 0.25f : 0f;
                    Color c = Color.Lerp(dark, light, Mathf.Clamp01(0.25f + fb * 0.65f + alt * (ring > 0 ? 1 : -0.5f)));
                    c = Color.Lerp(c, baseC, 0.3f);
                    float ln = 1f - TexKit.Smooth(0f, 0.03f, Seg(a + Mathf.PI, 12f) * 2f * Mathf.PI / 12f * r);
                    c = Color.Lerp(c, light, ln * 0.55f);
                    float rl = 1f - TexKit.Smooth(0f, 0.03f, Mathf.Abs(r - 0.5f));
                    c = Color.Lerp(c, Color.white, rl * 0.45f);
                    c = Color.Lerp(c, Color.white, TexKit.Smooth(0.14f, 0f, r) * 0.7f);
                    return c;
                }
                case BallStyle.Rune:
                {
                    Color stone = Color.Lerp(baseC, new Color(0.45f, 0.42f, 0.4f), 0.45f);
                    float g = 0.62f + 0.55f * TexKit.Fbm(u * 6f, v * 6f, ci);
                    Color c = stone * g; c.a = 1;
                    float dia = Mathf.Abs(Mathf.Abs(u) + Mathf.Abs(v) - 0.52f);
                    float cross = Mathf.Min(Mathf.Abs(u) < 0.045f && Mathf.Abs(v) < 0.34f ? 0f : 1f, Mathf.Abs(v) < 0.045f && Mathf.Abs(u) < 0.34f ? 0f : 1f);
                    float cut = Mathf.Max(1f - TexKit.Smooth(0.0f, 0.05f, dia), 1f - cross);
                    c = Color.Lerp(c, dark, cut * 0.85f);
                    float lit = 1f - TexKit.Smooth(0.04f, 0.085f, dia);
                    c = Color.Lerp(c, p.glow, Mathf.Clamp01(lit - cut) * 0.5f);
                    return c;
                }
                case BallStyle.Frost:
                {
                    Color ice = Color.Lerp(Color.Lerp(light, Color.white, 0.55f), baseC, 0.22f + 0.2f * r);
                    TexKit.Voronoi(u * 2.6f + 4, v * 2.6f, ci, out float f1, out float edge);
                    float crack = 1f - TexKit.Smooth(0.0f, 0.07f, edge);
                    Color c = Color.Lerp(ice, baseC, crack * 0.55f);
                    float rel = Seg(a + Mathf.PI, 6f) * 2f * Mathf.PI / 6f * r;
                    float arm = (1f - TexKit.Smooth(0f, 0.035f, rel)) * TexKit.Smooth(0.75f, 0.2f, r);
                    c = Color.Lerp(c, Color.white, arm * 0.8f);
                    c = Color.Lerp(c, Color.white, TexKit.Fbm(u * 8, v * 8, 2) * 0.18f);
                    return c;
                }
                case BallStyle.Moon:
                {
                    Color c = Color.Lerp(Color.Lerp(light, Color.white, 0.35f), baseC, 0.3f);
                    float n = TexKit.Fbm(u * 4f + 5, v * 4f, ci);
                    TexKit.Voronoi(u * 3.5f, v * 3.5f, ci + 20, out float f1, out float edge);
                    float crater = TexKit.Smooth(0.28f, 0.18f, f1) * 0.5f;
                    c = Color.Lerp(c, baseC, n * 0.35f + crater * 0.5f);
                    float sx = u - 0.5f, sy = v - 0.08f;
                    float inShadow = TexKit.Smooth(0.03f, -0.06f, Mathf.Sqrt(sx * sx + sy * sy) - 0.93f);
                    return Color.Lerp(c, Color.Lerp(dark, Color.black, 0.45f), inShadow * 0.86f);
                }
                case BallStyle.Ember:
                {
                    Color rock = Color.Lerp(dark, new Color(0.1f, 0.07f, 0.07f), 0.55f);
                    float g = 0.6f + 0.9f * TexKit.Fbm(u * 5f, v * 5f, ci);
                    Color c = rock * g; c.a = 1;
                    TexKit.Voronoi(u * 2.4f + 2, v * 2.4f, 40 + ci, out float f1, out float edge);
                    float vein = 1f - TexKit.Smooth(0.0f, 0.1f, edge);
                    c = Color.Lerp(c, p.glow, vein * 0.95f);
                    c = Color.Lerp(c, Color.Lerp(p.glow, Color.white, 0.5f), (1f - TexKit.Smooth(0f, 0.025f, edge)) * 0.8f);
                    return c;
                }
                case BallStyle.Talisman:
                {
                    Color c = Color.Lerp(baseC, dark, TexKit.Smooth(0.2f, 1f, r) * 0.6f);
                    Color gold = Pal.Gold;
                    float ring1 = 1f - TexKit.Smooth(0f, 0.035f, Mathf.Abs(r - 0.82f));
                    float ring2 = 1f - TexKit.Smooth(0f, 0.025f, Mathf.Abs(r - 0.62f));
                    float petalR = 0.52f * (0.5f + 0.5f * Mathf.Abs(Mathf.Cos(a * 4f)));
                    float petal = 1f - TexKit.Smooth(0f, 0.03f, Mathf.Abs(r - petalR));
                    float dot = TexKit.Smooth(0.13f, 0.09f, r);
                    float m = Mathf.Max(Mathf.Max(ring1 * 0.9f, ring2 * 0.6f), Mathf.Max(petal * 0.85f, dot));
                    return Color.Lerp(c, Color.Lerp(gold, Pal.GoldHi, 0.3f * (1f - r)), m);
                }
                default: // Glyph (enamel)
                {
                    Color c = Color.Lerp(baseC, dark, TexKit.Smooth(0.1f, 1f, r) * 0.55f);
                    float ring = 1f - TexKit.Smooth(0f, 0.04f, Mathf.Abs(r - 0.8f));
                    float ring2 = 1f - TexKit.Smooth(0f, 0.03f, Mathf.Abs(r - 0.38f));
                    float dot = TexKit.Smooth(0.14f, 0.1f, r);
                    float ray = (1f - TexKit.Smooth(0f, 0.03f, Seg(a + Mathf.PI, 8f) * 2f * Mathf.PI / 8f * r)) * TexKit.Smooth(0.36f, 0.42f, r) * TexKit.Smooth(0.78f, 0.7f, r);
                    float m = Mathf.Max(Mathf.Max(ring * 0.95f, ring2 * 0.7f), Mathf.Max(dot, ray * 0.55f));
                    return Color.Lerp(c, Color.Lerp(Pal.Gold, Pal.GoldHi, 0.35f * (1f - r)), m);
                }
            }
        }

        // ---------------------------------------------------------------------------------------------

        static void PaintGloss(Color32[] px, int cols, int cx, int cy)
        {
            float R = Cell * 0.5f * MarbleSet.Fill;
            var L = new Vector3(-0.42f, 0.55f, 0.72f).normalized;
            var H = (L + Vector3.forward).normalized;
            for (int j = 0; j < Cell; j++)
                for (int i = 0; i < Cell; i++)
                {
                    float dx = i + 0.5f - Cell * 0.5f, dy = j + 0.5f - Cell * 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float cov = TexKit.Sat(R - dist + 0.5f);
                    float u = dx / R, v = dy / R, r = Mathf.Min(1f, dist / R);
                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - r * r));
                    var n = new Vector3(u, v, nz);
                    if (dist > R) n = new Vector3(u, v, 0).normalized;
                    float ndl = Vector3.Dot(n, L);
                    // 1) shade side: black with alpha
                    float shadeA = TexKit.Sat((0.5f - ndl) * 0.85f) * 0.62f;
                    // 2) bounce light on the lower right rim
                    float bounce = TexKit.Smooth(0.78f, 0.98f, r) * TexKit.Sat(u * 0.5f - v * 0.55f - 0.1f);
                    // 3) highlights
                    float sp = Mathf.Pow(Mathf.Max(0, Vector3.Dot(n, H)), 70f);
                    float soft = Mathf.Pow(Mathf.Max(0, Vector3.Dot(n, H)), 9f) * 0.26f;
                    // crescent glare near the upper left limb
                    float ang = Mathf.Atan2(v, u);
                    float da = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, 140f)) / 38f;
                    float glare = TexKit.Smooth(1f, 0.15f, da) * TexKit.Smooth(0.7f, 0.82f, r) * TexKit.Smooth(0.99f, 0.9f, r) * 0.42f;
                    float whiteA = TexKit.Sat(sp * 0.95f + soft + glare + bounce * 0.2f);
                    // composite white over black-shade (straight alpha)
                    float outA = whiteA + shadeA * (1f - whiteA);
                    float outV = outA > 0.0001f ? (whiteA * 1f) / outA : 0f;
                    var c = new Color(outV, outV, Mathf.Min(1f, outV + 0.01f), outA * cov);
                    Put(px, cols, cx, cy, i, j, c);
                }
        }

        static void PaintShadow(Color32[] px, int cols, int cx, int cy)
        {
            float R = Cell * 0.5f;
            for (int j = 0; j < Cell; j++)
                for (int i = 0; i < Cell; i++)
                {
                    float dx = i + 0.5f - Cell * 0.5f, dy = j + 0.5f - Cell * 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / R;
                    float a = TexKit.Smooth(1f, 0.45f, r);
                    a = a * a * 0.55f;
                    Put(px, cols, cx, cy, i, j, new Color(0.01f, 0.01f, 0.03f, a));
                }
        }
    }
}
