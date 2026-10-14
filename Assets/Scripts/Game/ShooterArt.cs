using System;
using UnityEngine;

namespace Bilye
{
    public sealed class ShooterSet
    {
        public Sprite barrel, hub, base_;
        public const float BarrelPpu = 220f; // 1 world unit == 1 ball diameter after scaling
    }

    /// <summary>Barrel, hub and portal art, rendered with the relief lighting so they read as carved metal / stone.</summary>
    public static class ShooterArt
    {
        static readonly System.Collections.Generic.Dictionary<int, ShooterSet> cache = new System.Collections.Generic.Dictionary<int, ShooterSet>();
        static readonly System.Collections.Generic.Dictionary<int, Sprite[]> portals = new System.Collections.Generic.Dictionary<int, Sprite[]>();

        const float X0 = -0.62f, X1 = 1.62f, YH = 0.68f; // barrel texture extent in d units

        public static ShooterSet Get(WorldStyle st)
        {
            if (cache.TryGetValue(st.key, out var s)) return s;
            s = new ShooterSet();
            s.barrel = MakeBarrel(st);
            s.hub = MakeHub(st);
            cache[st.key] = s;
            return s;
        }

        // ---------------------------------------------------------------------------------------------
        // barrel
        // ---------------------------------------------------------------------------------------------

        static float Tri(float t) { t = t - Mathf.Floor(t); return Mathf.Abs(t * 2f - 1f); }

        static float HalfWidth(BarrelStyle b, float x)
        {
            float t = Mathf.InverseLerp(-0.55f, 1.5f, x);
            switch (b)
            {
                case BarrelStyle.Taper: return Mathf.Lerp(0.40f, 0.20f, TexKit.Smooth(0f, 1f, t));
                case BarrelStyle.Leaf: return Mathf.Max(0.03f, 0.46f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 0.92f + 0.04f)), 0.7f));
                case BarrelStyle.Fan: return 0.2f + 0.36f * TexKit.Smooth(0.05f, 1f, t) + 0.025f * Mathf.Sin(x * 26f) * t;
                case BarrelStyle.Hex:
                {
                    float[] w = { 0.40f, 0.35f, 0.30f, 0.25f };
                    int i = Mathf.Clamp(Mathf.FloorToInt(t * 4f), 0, 3);
                    float f = t * 4f - i;
                    float w1 = w[i], w2 = i < 3 ? w[i + 1] : w[3] * 0.8f;
                    return Mathf.Lerp(w1, w2, TexKit.Smooth(0.78f, 1f, f));
                }
                case BarrelStyle.Prism:
                    return t < 0.45f ? Mathf.Lerp(0.16f, 0.46f, TexKit.Smooth(0f, 1f, t / 0.45f)) : Mathf.Lerp(0.46f, 0.02f, Mathf.Pow((t - 0.45f) / 0.55f, 0.9f));
                case BarrelStyle.Stone: return 0.37f - (Mathf.Sin(x * 15f) > 0.55f ? 0.045f : 0f);
                case BarrelStyle.Spike: return 0.24f + 0.13f * Mathf.Pow(Tri(x * 2.6f + 0.2f), 3f) * TexKit.Smooth(0f, 0.25f, t) + 0.1f * (1f - t) * 0.5f;
                case BarrelStyle.Tube: return 0.24f;
                case BarrelStyle.Forge: return Mathf.Lerp(0.52f, 0.34f, t) + 0.05f * Mathf.Sin(t * Mathf.PI * 3f);
                default: return 0.42f - 0.07f * TexKit.Smooth(0.6f, 1f, t); // Idol
            }
        }

        static Sprite MakeBarrel(WorldStyle st)
        {
            const float PPU = ShooterSet.BarrelPpu;
            int w = Mathf.RoundToInt((X1 - X0) * PPU), h = Mathf.RoundToInt(YH * 2 * PPU);
            var bodyRamp = st.metal.BodyRamp(0.4f);
            var goldRamp = st.metal.MetalRamp(0.7f);
            var darkRamp = new TexKit.Ramp(Color.black, st.metal.body[2] * 0.6f, st.metal.body[2], 0.1f);
            float xEnd = st.barrel == BarrelStyle.Leaf || st.barrel == BarrelStyle.Prism ? 1.52f : 1.44f;
            var tex = TexKit.Relief(w, h, PPU, (float x0, float y, ref TexKit.Px p) =>
            {
                float x = x0 + (X0 + X1) * 0.5f; // texture centre -> barrel coordinates
                float hw = HalfWidth(st.barrel, x);
                float sdY = Mathf.Abs(y) - hw;
                float sdX = Mathf.Max(-0.55f - x, x - xEnd);
                float sd = Mathf.Max(sdY, sdX);
                // rounded muzzle for blunt styles
                if (st.barrel != BarrelStyle.Leaf && st.barrel != BarrelStyle.Prism && x > xEnd - 0.12f)
                {
                    float cx = x - (xEnd - 0.12f);
                    sd = Mathf.Max(sdY, Mathf.Sqrt(cx * cx + Mathf.Pow(Mathf.Max(0, Mathf.Abs(y) - hw * 0.0f), 2f) * 0.0f) - 0.12f);
                    sd = Mathf.Max(sdY, sdX);
                }
                float a = TexKit.Cov(sd, 3f / PPU);
                p.a = a;
                if (a <= 0f) return;
                float rel = hw > 0.001f ? Mathf.Clamp01(Mathf.Abs(y) / hw) : 1f;
                float cyl = Mathf.Sqrt(Mathf.Max(0f, 1f - rel * rel));
                p.h = 0.2f * cyl * Mathf.Min(1f, hw * 3f);
                p.ramp = bodyRamp;
                // metal bands
                float[] bandsA = { -0.55f, 0.30f, xEnd - 0.2f };
                float[] bandsB = { -0.40f, 0.40f, xEnd };
                for (int i = 0; i < 3; i++)
                {
                    if (x >= bandsA[i] && x <= bandsB[i])
                    {
                        float k = (x - bandsA[i]) / (bandsB[i] - bandsA[i]);
                        p.ramp = goldRamp; p.h = 0.2f * cyl * Mathf.Min(1f, hw * 3f) + 0.05f * Mathf.Sin(k * Mathf.PI);
                    }
                }
                // style ornaments
                switch (st.barrel)
                {
                    case BarrelStyle.Leaf:
                        if (Mathf.Abs(y) < 0.012f && x > -0.3f && x < 1.3f) { p.ramp = goldRamp; p.h += 0.03f; }
                        break;
                    case BarrelStyle.Hex:
                        if (Mathf.Abs(Mathf.Sin((x + 0.55f) * Mathf.PI * 4f / 1.0f)) < 0.05f) p.ramp = goldRamp;
                        break;
                    case BarrelStyle.Prism:
                        if (Mathf.Abs(Mathf.Abs(y) - (hw - 0.01f)) < 0.014f) { p.ramp = goldRamp; p.h += 0.03f; }
                        break;
                    case BarrelStyle.Stone:
                    {
                        float n = TexKit.Fbm(x * 9f, y * 9f, 3);
                        p.ramp = n > 0.62f ? darkRamp : p.ramp == goldRamp ? goldRamp : bodyRamp;
                        p.h += (n - 0.5f) * 0.06f;
                        break;
                    }
                    case BarrelStyle.Forge:
                        if (x > 0.5f && x < 1.0f && Mathf.Abs(y) < 0.08f) { p.ramp = null; p.c = Color.Lerp(st.metal.glow, st.metal.mid, 0.2f); p.glow = 0.7f; p.glowColor = st.metal.glow; }
                        break;
                    case BarrelStyle.Idol:
                        if (x > 0.65f && x < 0.8f) { p.ramp = goldRamp; }
                        if (Mathf.Abs(y) < 0.03f && x > 0.0f && x < 0.6f) { p.ramp = darkRamp; }
                        break;
                }
                // dark bore at the muzzle
                if (x > xEnd - 0.2f && Mathf.Abs(y) < hw * 0.52f && x < xEnd - 0.015f && st.barrel != BarrelStyle.Leaf && st.barrel != BarrelStyle.Prism)
                { p.ramp = darkRamp; p.h -= 0.07f; }
            }, 2.6f);
            // pivot at barrel x = 0
            float px = (0f - X0) / (X1 - X0);
            return TexKit.ToSprite(tex, PPU, new Vector2(px, 0.5f));
        }

        // ---------------------------------------------------------------------------------------------
        // hub
        // ---------------------------------------------------------------------------------------------

        static float HubRadius(HubStyle s, float a)
        {
            switch (s)
            {
                case HubStyle.Petal: return 0.88f + 0.12f * Mathf.Abs(Mathf.Cos(a * 4f));
                case HubStyle.Hex: { float k = Mathf.Repeat(a, Mathf.PI / 3f) - Mathf.PI / 6f; return 0.866f / Mathf.Cos(k) * 1.04f; }
                case HubStyle.Facet: { float k = Mathf.Repeat(a, Mathf.PI / 4f) - Mathf.PI / 8f; return 0.924f / Mathf.Cos(k) * 1.04f; }
                case HubStyle.Sun: return 0.78f + 0.22f * (1f - Tri(a / (Mathf.PI * 2f) * 12f));
                case HubStyle.Snow: return 0.78f + 0.22f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 3f)), 8f);
                case HubStyle.Gear: return 0.88f + (Mathf.Sin(a * 10f) > 0.15f ? 0.12f : 0f);
                case HubStyle.Mandala: return 0.9f + 0.1f * Mathf.Abs(Mathf.Cos(a * 8f));
                default: return 1f;
            }
        }

        static Sprite MakeHub(WorldStyle st)
        {
            const float PPU = 200f;
            const float HR = 0.78f; // hub radius in d units
            int size = Mathf.RoundToInt(HR * 2.5f * PPU);
            var gold = st.metal.MetalRamp(0.8f);
            var body = st.metal.BodyRamp(0.5f);
            var dark = new TexKit.Ramp(Color.black, st.metal.body[2] * 0.8f, st.metal.body[1], 0.2f);
            var tex = TexKit.Relief(size, size, PPU, (float x, float y, ref TexKit.Px p) =>
            {
                float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
                float R = HR * HubRadius(st.hub, a);
                float rho = r / R;
                p.a = TexKit.Cov(r - R, 3f / PPU);
                if (p.a <= 0) return;
                if (rho > 0.8f)
                {
                    float k = (rho - 0.8f) / 0.2f;
                    p.ramp = gold; p.h = 0.42f * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI * 0.85f + 0.2f) * 0.8f;
                    if (st.hub == HubStyle.Dial && Mathf.Sin(a * 36f) > 0.82f && rho < 0.95f) { p.ramp = dark; p.h -= 0.1f; }
                    if (st.hub == HubStyle.Pearl)
                    {
                        float bead = Mathf.Abs(Mathf.Sin(a * 6f));
                        p.h += 0.09f * bead;
                    }
                }
                else if (rho > 0.24f)
                {
                    p.ramp = body; p.h = 0.18f + 0.1f * (1f - rho);
                    switch (st.hub)
                    {
                        case HubStyle.Facet: p.h += 0.08f * Mathf.Abs(Mathf.Sin(a * 4f)) * (1f - rho); if (Mathf.Abs(Mathf.Sin(a * 4f)) < 0.04f) p.ramp = gold; break;
                        case HubStyle.Hex: if (Mathf.Abs(rho - 0.55f) < 0.04f) p.ramp = gold; break;
                        case HubStyle.Sun: if (Mathf.Abs(rho - 0.5f) < 0.035f) p.ramp = gold; break;
                        case HubStyle.Snow: if (Mathf.Abs(Mathf.Sin(a * 3f)) < 0.06f && rho > 0.3f) p.ramp = gold; break;
                        case HubStyle.Gear: if (Mathf.Abs(rho - 0.6f) < 0.05f) { p.ramp = dark; p.h -= 0.05f; } break;
                        case HubStyle.Mandala: if (Mathf.Abs(rho - 0.46f * (0.7f + 0.3f * Mathf.Abs(Mathf.Cos(a * 4f)))) < 0.04f) p.ramp = gold; break;
                        case HubStyle.Petal: if (Mathf.Abs(rho - 0.55f * (0.75f + 0.25f * Mathf.Abs(Mathf.Cos(a * 4f)))) < 0.04f) p.ramp = gold; break;
                        case HubStyle.Idol:
                        {
                            float sq = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) / HR;
                            if (Mathf.Abs(sq - 0.46f) < 0.045f) { p.ramp = gold; p.h += 0.05f; }
                            break;
                        }
                        case HubStyle.Moon:
                        {
                            float cx = x / HR - 0.16f, cy = y / HR - 0.05f;
                            if (Mathf.Sqrt(cx * cx + cy * cy) < 0.5f) { p.ramp = dark; p.h -= 0.08f; }
                            break;
                        }
                    }
                }
                else
                {
                    float k = rho / 0.24f;
                    p.ramp = gold; p.h = 0.35f + 0.3f * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k));
                    p.glow = 0.55f * (1f - k * k) * (1f - k * 0.3f); p.glowColor = st.metal.glow;
                }
            }, 2.4f);
            return TexKit.ToSprite(tex, PPU);
        }

        // ---------------------------------------------------------------------------------------------
        // portal: [0] dark core, [1] glow ring (tint at runtime), [2] swirl arms (tint), [3] sun rim, [4] sun rays
        // ---------------------------------------------------------------------------------------------

        public static Sprite[] Portal(WorldStyle st)
        {
            if (portals.TryGetValue(st.key, out var s)) return s;
            s = new Sprite[5];
            const int N = 256; const float PPU = 100f; // unit = 2.56 d.. scaled by view
            // core
            var core = TexKit.NewTex(N, N); var cp = new Color32[N * N];
            var glow = TexKit.NewTex(N, N); var gp = new Color32[N * N];
            var arms = TexKit.NewTex(N, N); var ap = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = (i + 0.5f) / N * 2f - 1f, y = (j + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y), ang = Mathf.Atan2(y, x);
                    float ca = TexKit.Smooth(1f, 0.52f, r); ca = ca * ca * 0.96f;
                    cp[j * N + i] = new Color(0.02f, 0.01f, 0.06f, ca);
                    float g = TexKit.Smooth(1f, 0.35f, r) * TexKit.Smooth(0.2f, 0.62f, r);
                    gp[j * N + i] = new Color(1, 1, 1, g * 0.8f);
                    // spiral arms: three log spirals
                    float arm = 0f;
                    for (int k = 0; k < 3; k++)
                    {
                        float aa = ang + k * Mathf.PI * 2f / 3f - Mathf.Log(Mathf.Max(0.05f, r)) * 2.4f;
                        float d = Mathf.Abs(Mathf.DeltaAngle(aa * Mathf.Rad2Deg, 0f)) / 180f;
                        arm = Mathf.Max(arm, TexKit.Smooth(0.16f, 0.0f, d * r * 3.2f) );
                    }
                    ap[j * N + i] = new Color(1, 1, 1, arm * TexKit.Smooth(0.98f, 0.8f, r) * TexKit.Smooth(0.12f, 0.3f, r) * 0.85f);
                }
            core.SetPixels32(cp); glow.SetPixels32(gp); arms.SetPixels32(ap);
            s[0] = TexKit.ToSprite(core, N); s[1] = TexKit.ToSprite(glow, N); s[2] = TexKit.ToSprite(arms, N);

            if (st.sun)
            {
                var gold = st.metal.MetalRamp(0.9f);
                var rim = TexKit.Relief(N, N, N * 0.5f, (float x, float y, ref TexKit.Px p) =>
                {
                    float r = Mathf.Sqrt(x * x + y * y);
                    float ring = Mathf.Abs(r - 0.62f) - 0.1f;
                    p.a = TexKit.Cov(ring, 2.5f / (N * 0.5f));
                    p.ramp = gold;
                    float k = Mathf.Clamp01(1f - Mathf.Abs(r - 0.62f) / 0.1f);
                    p.h = Mathf.Sin(k * Mathf.PI * 0.5f) * 0.5f;
                }, 2.5f);
                s[3] = TexKit.ToSprite(rim, N);
                var rays = TexKit.Relief(N, N, N * 0.5f, (float x, float y, ref TexKit.Px p) =>
                {
                    float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
                    float t = Tri(a / (Mathf.PI * 2f) * 12f + 0.25f);
                    float len = 0.72f + (0.28f * (1f - t) * (((int)Mathf.Floor(a / (Mathf.PI * 2f) * 12f + 12) % 2) == 0 ? 1f : 0.65f));
                    float inRay = Mathf.Min(r - 0.74f, len * 0.98f - r + (1f - t) * 0f);
                    float half = (1f - t) * (len - 0.7f) * 0.9f + 0.002f;
                    float width = (1f - Mathf.Clamp01((r - 0.72f) / Mathf.Max(0.001f, len - 0.72f))) * 0.07f;
                    float tang = Mathf.Abs(Mathf.Sin(Mathf.Repeat(a, Mathf.PI * 2f / 12f) - Mathf.PI / 12f)) * r;
                    float sd = Mathf.Max(r - len * 0.98f, Mathf.Max(0.73f - r, tang - width));
                    p.a = TexKit.Cov(sd, 2.5f / (N * 0.5f)); p.ramp = gold; p.h = Mathf.Clamp01(1f - tang / Mathf.Max(0.001f, width)) * 0.3f;
                }, 2.5f);
                s[4] = TexKit.ToSprite(rays, N);
            }
            portals[st.key] = s;
            return s;
        }
    }
}
