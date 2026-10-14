using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bilye
{
    /// <summary>Shared procedural sprites: soft discs, rings, sparks, icons and 9-slice UI frames.</summary>
    public static class Gfx
    {
        static Sprite _dot, _glow, _ring, _spark, _pixel, _vignette;
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> frames = new Dictionary<string, Sprite>();

        public static Sprite Pixel => _pixel ??= Make(4, 4, 4, (x, y) => Color.white);
        public static Sprite Dot => _dot ??= Make(96, 96, 96, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); return new Color(1, 1, 1, TexKit.Sat((0.5f - r) * 96f * 0.5f + 0.5f)); });
        public static Sprite Glow => _glow ??= Make(128, 128, 128, (x, y) => { float r = Mathf.Sqrt(x * x + y * y) * 2f; float a = TexKit.Sat(1f - r); return new Color(1, 1, 1, a * a * (3f - 2f * a)); });
        public static Sprite Ring => _ring ??= Make(160, 160, 160, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y) * 2f;
            float a = TexKit.Sat(1f - Mathf.Abs(r - 0.92f) / 0.06f);
            return new Color(1, 1, 1, a * a);
        });
        public static Sprite Spark => _spark ??= Make(96, 96, 96, (x, y) =>
        {
            float ax = Mathf.Abs(x) * 2f, ay = Mathf.Abs(y) * 2f;
            float s = Mathf.Pow(Mathf.Max(0, 1f - Mathf.Pow(Mathf.Pow(ax, 0.5f) + Mathf.Pow(ay, 0.5f), 2f)), 1f);
            float core = Mathf.Max(0, 1f - Mathf.Sqrt(ax * ax + ay * ay) * 2.2f);
            return new Color(1, 1, 1, TexKit.Sat(s + core));
        });
        /// <summary>Dark corners for the whole screen (stretched over the camera view).</summary>
        public static Sprite Vignette => _vignette ??= Make(128, 128, 128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y) * 1.6f;
            float a = TexKit.Smooth(0.55f, 1.15f, r);
            return new Color(0.01f, 0.015f, 0.04f, a * 0.78f);
        });

        static Sprite Make(int w, int h, float ppu, Func<float, float, Color> f)
        {
            var t = TexKit.NewTex(w, h);
            var px = new Color32[w * h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float x = (i + 0.5f) / w - 0.5f, y = (j + 0.5f) / h - 0.5f;
                    px[j * w + i] = f(x, y);
                }
            t.SetPixels32(px);
            return TexKit.ToSprite(t, ppu);
        }

        // ---------------------------------------------------------------------------------------------
        // signed distance helpers (unit space, +-1)
        // ---------------------------------------------------------------------------------------------

        static float Seg(float px, float py, float ax, float ay, float bx, float by, float rad)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float h = TexKit.Sat((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - rad;
        }

        static float Poly(float px, float py, float[] pts)
        {
            int n = pts.Length / 2; float d = float.MaxValue; bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float ax = pts[i * 2], ay = pts[i * 2 + 1], bx = pts[j * 2], by = pts[j * 2 + 1];
                d = Mathf.Min(d, Seg(px, py, ax, ay, bx, by, 0f));
                if (((ay > py) != (by > py)) && (px < (bx - ax) * (py - ay) / (by - ay) + ax)) inside = !inside;
            }
            return inside ? -d : d;
        }

        static float[] StarPts()
        {
            var p = new float[20];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5; float r = i % 2 == 0 ? 0.95f : 0.4f;
                p[i * 2] = Mathf.Cos(a) * r; p[i * 2 + 1] = Mathf.Sin(a) * r - 0.04f;
            }
            return p;
        }

        public static Sprite Icon(string name)
        {
            if (icons.TryGetValue(name, out var s)) return s;
            const int N = 128;
            Func<float, float, float> sd;
            switch (name)
            {
                case "star": { var pts = StarPts(); sd = (x, y) => Poly(x, y, pts); break; }
                case "pause": sd = (x, y) => Mathf.Min(TexKit.SdBox(x + 0.32f, y, 0.2f, 0.62f, 0.08f), TexKit.SdBox(x - 0.32f, y, 0.2f, 0.62f, 0.08f)); break;
                case "play": { var tri = new[] { -0.45f, 0.7f, -0.45f, -0.7f, 0.75f, 0f }; sd = (x, y) => Poly(x, y, tri) - 0.04f; break; }
                case "back": sd = (x, y) => Mathf.Min(Seg(x, y, 0.3f, 0.65f, -0.35f, 0f, 0.16f), Seg(x, y, -0.35f, 0f, 0.3f, -0.65f, 0.16f)); break;
                case "check": sd = (x, y) => Mathf.Min(Seg(x, y, -0.6f, 0f, -0.2f, -0.45f, 0.15f), Seg(x, y, -0.2f, -0.45f, 0.65f, 0.5f, 0.15f)); break;
                case "cross": sd = (x, y) => Mathf.Min(Seg(x, y, -0.55f, -0.55f, 0.55f, 0.55f, 0.15f), Seg(x, y, -0.55f, 0.55f, 0.55f, -0.55f, 0.15f)); break;
                case "home":
                {
                    var roof = new[] { 0f, 0.85f, 0.9f, 0.1f, 0.68f, 0.1f, 0.68f, -0.75f, -0.68f, -0.75f, -0.68f, 0.1f, -0.9f, 0.1f };
                    sd = (x, y) => Mathf.Max(Poly(x, y, roof) - 0.02f, -TexKit.SdBox(x, y + 0.45f, 0.17f, 0.3f));
                    break;
                }
                case "restart":
                    sd = (x, y) =>
                    {
                        float r = Mathf.Sqrt(x * x + y * y); float ang = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                        float ringD = Mathf.Abs(r - 0.58f) - 0.14f;
                        float gap = Mathf.Abs(Mathf.DeltaAngle(ang, 35f)) < 38f ? 1f : 0f;
                        float arc = gap > 0 ? 1f : ringD;
                        var head = new[] { 0.34f, 0.9f, 0.95f, 0.62f, 0.45f, 0.32f };
                        return Mathf.Min(arc, Poly(x, y, head));
                    };
                    break;
                case "gear":
                    sd = (x, y) =>
                    {
                        float r = Mathf.Sqrt(x * x + y * y); float a = Mathf.Atan2(y, x);
                        float R = 0.62f + (Mathf.Sin(a * 8f) > 0.1f ? 0.2f : 0f);
                        return Mathf.Max(r - R, -(r - 0.3f));
                    };
                    break;
                case "lock":
                    sd = (x, y) =>
                    {
                        float body = TexKit.SdBox(x, y + 0.2f, 0.62f, 0.5f, 0.12f);
                        float sh = Mathf.Abs(Mathf.Sqrt(x * x + (y - 0.2f) * (y - 0.2f)) - 0.38f) - 0.1f;
                        if (y < 0.2f) sh = Mathf.Abs(Mathf.Abs(x) - 0.38f) - 0.1f;
                        sh = Mathf.Max(sh, -0.25f - y * 0f + (y < -0.2f ? 1f : 0f));
                        return Mathf.Min(body, sh);
                    };
                    break;
                case "crown":
                {
                    var c = new[] { -0.8f, -0.5f, -0.8f, 0.45f, -0.4f, 0f, 0f, 0.7f, 0.4f, 0f, 0.8f, 0.45f, 0.8f, -0.5f };
                    sd = (x, y) => Poly(x, y, c);
                    break;
                }
                case "infinity":
                    sd = (x, y) =>
                    {
                        float a = Mathf.Abs(Mathf.Sqrt((Mathf.Abs(x) - 0.36f) * (Mathf.Abs(x) - 0.36f) + y * y) - 0.3f) - 0.1f;
                        return a;
                    };
                    break;
                default: sd = (x, y) => Mathf.Sqrt(x * x + y * y) - 0.7f; break;
            }
            var t = TexKit.NewTex(N, N); var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = ((i + 0.5f) / N * 2f - 1f), y = ((j + 0.5f) / N * 2f - 1f);
                    px[j * N + i] = new Color(1, 1, 1, TexKit.Cov(sd(x, y), 2.2f / (N * 0.5f)));
                }
            t.SetPixels32(px);
            s = TexKit.ToSprite(t, N);
            icons[name] = s;
            return s;
        }

        // ---------------------------------------------------------------------------------------------
        // 9-slice frames
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Rounded 9-slice with a vertical gradient fill, optional hairline border and a bright top bevel.
        /// kind: "gold", "goldPressed", "glass", "panel", "tile", "danger", "chip", "white"
        /// </summary>
        public static Sprite Frame(string kind)
        {
            if (frames.TryGetValue(kind, out var s)) return s;
            const int N = 96; const float R = 30f;
            Color top, bot, border; float bw = 0; float bevel = 0; float glossTop = 0;
            switch (kind)
            {
                case "gold": top = Pal.Hex("#fbeebc"); bot = Pal.Hex("#cf9f42"); border = Pal.Hex("#7d5516", 0.9f); bw = 1.6f; glossTop = 0.32f; bevel = 1f; break;
                case "goldPressed": top = Pal.Hex("#f1dfa0"); bot = Pal.Hex("#bf9136"); border = Pal.Hex("#6a4510", 0.9f); bw = 1.6f; glossTop = 0.15f; bevel = 0.6f; break;
                case "glass": top = Pal.Hex("#ffffff", 0.10f); bot = Pal.Hex("#ffffff", 0.03f); border = Pal.Hex("#e7c46d", 0.32f); bw = 1.4f; break;
                case "panel": top = Pal.Hex("#19223a"); bot = Pal.Hex("#0e1421"); border = Pal.Hex("#e7c46d", 0.45f); bw = 1.8f; bevel = 0.35f; break;
                case "tile": top = Pal.Hex("#ffffff", 0.09f); bot = Pal.Hex("#ffffff", 0.02f); border = Pal.Hex("#ffffff", 0.12f); bw = 1.2f; break;
                case "danger": top = Pal.Hex("#e5576b", 0.26f); bot = Pal.Hex("#e5576b", 0.08f); border = Pal.Hex("#e5576b", 0.5f); bw = 1.4f; break;
                case "chip": top = Pal.Hex("#0e1420", 0.82f); bot = Pal.Hex("#06080f", 0.82f); border = Pal.Hex("#e7c46d", 0.4f); bw = 1.4f; break;
                case "dark": top = Pal.Hex("#0b101c"); bot = Pal.Hex("#06080f"); border = Pal.Hex("#e7c46d", 0.25f); bw = 1.2f; break;
                default: top = bot = Color.white; border = Color.clear; break;
            }
            var t = TexKit.NewTex(N, N); var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = i + 0.5f - N * 0.5f, y = j + 0.5f - N * 0.5f;
                    float sd = TexKit.SdBox(x, y, N * 0.5f, N * 0.5f, R);
                    float a = TexKit.Sat(0.5f - sd);
                    float k = 1f - (y + N * 0.5f) / N; // 0 at top... (texture y up -> j large = top)
                    k = 1f - (float)j / (N - 1);
                    Color c = Color.Lerp(top, bot, k);
                    if (glossTop > 0f) c = Color.Lerp(c, Color.white, glossTop * TexKit.Smooth(0.5f, 0f, k) * 0.7f);
                    if (bw > 0f)
                    {
                        float bd = TexKit.Sat(1f - Mathf.Abs(sd + bw * 0.5f) / (bw * 0.5f + 0.5f));
                        c = Color.Lerp(c, new Color(border.r, border.g, border.b, 1f), bd * border.a);
                        if (bevel > 0f) { float inner = TexKit.Sat(1f - Mathf.Abs(sd + bw + 1.5f) / 1.2f) * TexKit.Smooth(0.45f, 0f, k); c = Color.Lerp(c, Color.white, inner * 0.55f * bevel); }
                    }
                    float alpha = c.a * a;
                    c.a = alpha;
                    px[j * N + i] = c;
                }
            t.SetPixels32(px);
            t.Apply(true, false);
            s = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2, R + 2, R + 2, R + 2));
            frames[kind] = s;
            return s;
        }
    }
}
