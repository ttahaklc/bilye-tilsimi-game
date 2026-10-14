using System;
using System.Collections.Generic;
using Bilye.Core;
using UnityEngine;

namespace Bilye
{
    /// <summary>
    /// Everything behind the track: vertical gradient, a faint stone/paper grain, a world specific set of drifting elements
    /// (leaves, bubbles, snow, embers, stars...) and a vignette. Pure decoration; gameplay never reads it.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        sealed class Floater
        {
            public SpriteRenderer sr; public Vector2 pos, vel; public float rot, rotVel, size, phase, baseAlpha, sway, swayAmp;
        }

        struct Recipe
        {
            public int count; public float sizeMin, sizeMax; public Vector2 vel; public float velJitter; public float rotSpeed;
            public float alpha; public float twinkle; public float sway; public Color color; public string sprite; public int sort;
        }

        readonly List<Floater> items = new List<Floater>();
        SpriteRenderer gradient, grain, vignette, hero, hero2;
        float w, h, time;
        Motif motif;
        Color accent;
        static readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        System.Random rng = new System.Random(7);

        public static Material SpriteMat;

        public void Build(Motif m, WorldTheme theme, float worldW, float worldH)
        {
            Clear();
            motif = m; w = worldW; h = worldH;
            accent = Pal.Hex(theme.accent);
            transform.localPosition = Vector3.zero;

            gradient = MakeSr("grad", MakeGradient(Pal.Hex(theme.top), Pal.Hex(theme.bottom)), -100);
            gradient.transform.localScale = new Vector3(worldW * 1.2f, worldH * 1.2f / 32f, 1);

            grain = MakeSr("grain", Grain(), -99);
            grain.color = new Color(1, 1, 1, m == Motif.Tablet ? 0.22f : 0.11f);
            grain.transform.localScale = new Vector3(worldW * 1.2f / 4f, worldH * 1.2f / 4f, 1);

            vignette = MakeSr("vig", Gfx.Vignette, 900);
            vignette.transform.localScale = new Vector3(worldW * 1.1f, worldH * 1.1f, 1);

            BuildHero(m, theme);
            foreach (var r in Recipes(m, theme)) Spawn(r);
        }

        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            items.Clear();
        }

        SpriteRenderer MakeSr(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
            if (SpriteMat != null) sr.sharedMaterial = SpriteMat;
            return sr;
        }

        // ---------------------------------------------------------------------------------------------

        void BuildHero(Motif m, WorldTheme theme)
        {
            hero = hero2 = null;
            switch (m)
            {
                case Motif.Dawn:
                    hero = MakeSr("rays", Art("rays"), -90); hero.color = Pal.Hex("#ffd9a0", 0.16f);
                    hero.transform.position = new Vector3(0, h * 0.62f, 0); hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 1.8f; break;
                case Motif.Night:
                    hero = MakeSr("moon", Art("moon"), -90); hero.color = Pal.Hex("#e8ecff", 0.9f);
                    hero.transform.position = new Vector3(w * 0.28f, h * 0.36f, 0); hero.transform.localScale = Vector3.one * Mathf.Min(w, h) * 0.34f; break;
                case Motif.Mandala:
                    hero = MakeSr("mandala", Art("mandala"), -90); hero.color = Pal.Hex("#e7c46d", 0.17f);
                    hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 1.15f; break;
                case Motif.Tablet:
                    hero = MakeSr("tablet", Art("mandala"), -90); hero.color = Pal.Hex("#e8c15a", 0.12f);
                    hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 1.05f; break;
                case Motif.Dunes:
                    hero = MakeSr("sun", Gfx.Glow, -92); hero.color = Pal.Hex("#ff9a55", 0.35f);
                    hero.transform.position = new Vector3(-w * 0.2f, h * 0.38f, 0); hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 0.9f; break;
                case Motif.Lava:
                    hero = MakeSr("heat", Gfx.Glow, -92); hero.color = Pal.Hex("#ff5a28", 0.30f);
                    hero.transform.position = new Vector3(0, -h * 0.52f, 0); hero.transform.localScale = new Vector3(w * 2.0f, h * 0.8f, 1); break;
                case Motif.Ice:
                    hero = MakeSr("aurora", Gfx.Glow, -92); hero.color = Pal.Hex("#7ff0ff", 0.17f);
                    hero.transform.position = new Vector3(0, h * 0.55f, 0); hero.transform.localScale = new Vector3(w * 2.0f, h * 0.7f, 1); break;
                case Motif.Leaves:
                    hero = MakeSr("canopy", Gfx.Glow, -92); hero.color = Pal.Hex("#8dffb8", 0.14f);
                    hero.transform.position = new Vector3(w * 0.2f, h * 0.45f, 0); hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 0.9f; break;
                case Motif.Coral:
                    hero = MakeSr("lagoon", Gfx.Glow, -92); hero.color = Pal.Hex("#ff9bb8", 0.18f);
                    hero.transform.position = new Vector3(-w * 0.15f, h * 0.4f, 0); hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 0.9f; break;
                case Motif.Honey:
                    hero = MakeSr("amber", Gfx.Glow, -92); hero.color = Pal.Hex("#ffb83a", 0.17f);
                    hero.transform.position = new Vector3(0, h * 0.2f, 0); hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 1.1f; break;
                case Motif.Crystals:
                    hero = MakeSr("aura", Gfx.Glow, -92); hero.color = Pal.Hex("#b78bff", 0.2f);
                    hero.transform.position = new Vector3(0, -h * 0.1f, 0); hero.transform.localScale = Vector3.one * Mathf.Max(w, h) * 1.1f; break;
            }
        }

        Recipe[] Recipes(Motif m, WorldTheme theme)
        {
            float u = Mathf.Min(w, h);
            Recipe R(string sprite, int count, float s0, float s1, Vector2 vel, float jit, float rot, float alpha, float tw, float sway, Color col, int sort = -80)
                => new Recipe { sprite = sprite, count = count, sizeMin = s0 * u, sizeMax = s1 * u, vel = vel * u, velJitter = jit * u, rotSpeed = rot, alpha = alpha, twinkle = tw, sway = sway, color = col, sort = sort };
            switch (m)
            {
                case Motif.Dawn: return new[] { R("dot", 36, 0.006f, 0.016f, new Vector2(0.004f, 0.012f), 0.01f, 0, 0.6f, 1.5f, 0.2f, Pal.Hex("#ffe8b8")), R("spark", 12, 0.03f, 0.06f, Vector2.zero, 0.002f, 0.1f, 0.5f, 2.2f, 0, Pal.Hex("#fff3d0")) };
                case Motif.Leaves: return new[] { R("leaf", 22, 0.04f, 0.085f, new Vector2(0.006f, -0.028f), 0.01f, 40, 0.55f, 0, 0.5f, Pal.Hex("#7fe3a0")), R("dot", 24, 0.005f, 0.012f, new Vector2(0, 0.01f), 0.008f, 0, 0.5f, 1.8f, 0.2f, Pal.Hex("#d6ffe0")) };
                case Motif.Coral: return new[] { R("ring", 30, 0.015f, 0.05f, new Vector2(0, 0.045f), 0.02f, 0, 0.5f, 0, 0.3f, Pal.Hex("#ffd0dc")), R("dot", 20, 0.006f, 0.014f, new Vector2(0, 0.03f), 0.01f, 0, 0.4f, 1.4f, 0.2f, Pal.Hex("#fff0f4")) };
                case Motif.Honey: return new[] { R("hex", 18, 0.05f, 0.12f, new Vector2(0.004f, 0.012f), 0.006f, 8, 0.28f, 0, 0.1f, Pal.Hex("#ffd36a")), R("dot", 28, 0.005f, 0.012f, new Vector2(0.003f, 0.015f), 0.01f, 0, 0.6f, 2f, 0.3f, Pal.Hex("#ffe9a6")) };
                case Motif.Crystals: return new[] { R("shard", 16, 0.04f, 0.1f, new Vector2(0, 0.008f), 0.006f, 12, 0.5f, 1.2f, 0.1f, Pal.Hex("#d6b8ff")), R("spark", 20, 0.025f, 0.055f, Vector2.zero, 0.002f, 0, 0.7f, 2.6f, 0, Pal.Hex("#f2e6ff")) };
                case Motif.Dunes: return new[] { R("streak", 26, 0.06f, 0.14f, new Vector2(0.09f, 0.004f), 0.03f, 0, 0.22f, 0, 0.05f, Pal.Hex("#ffd2a0")), R("dot", 30, 0.004f, 0.01f, new Vector2(0.07f, 0.002f), 0.03f, 0, 0.5f, 0, 0.1f, Pal.Hex("#ffe6c8")) };
                case Motif.Ice: return new[] { R("flake", 30, 0.025f, 0.07f, new Vector2(0.004f, -0.035f), 0.012f, 30, 0.7f, 0, 0.6f, Pal.Hex("#e8fbff")), R("dot", 22, 0.004f, 0.01f, new Vector2(0, -0.02f), 0.01f, 0, 0.7f, 0, 0.4f, Color.white) };
                case Motif.Night: return new[] { R("spark", 40, 0.015f, 0.05f, Vector2.zero, 0.0005f, 0, 0.8f, 2.4f, 0, Pal.Hex("#e8ecff")), R("dot", 50, 0.004f, 0.009f, Vector2.zero, 0.0005f, 0, 0.8f, 3f, 0, Color.white) };
                case Motif.Lava: return new[] { R("dot", 34, 0.008f, 0.024f, new Vector2(0.004f, 0.06f), 0.025f, 0, 0.85f, 1.2f, 0.4f, Pal.Hex("#ff9a45")), R("spark", 10, 0.02f, 0.04f, new Vector2(0, 0.05f), 0.02f, 0, 0.8f, 2f, 0.3f, Pal.Hex("#ffd9a0")) };
                case Motif.Mandala: return new[] { R("spark", 22, 0.02f, 0.05f, Vector2.zero, 0.002f, 0, 0.7f, 2f, 0, Pal.Hex("#ffe08a")), R("dot", 24, 0.005f, 0.012f, new Vector2(0, 0.008f), 0.006f, 0, 0.5f, 1.6f, 0.2f, Pal.Hex("#f4d98e")) };
                default: return new[] { R("dot", 30, 0.004f, 0.012f, new Vector2(0.004f, 0.008f), 0.006f, 0, 0.45f, 1.6f, 0.2f, Pal.Hex("#e8c15a")), R("spark", 10, 0.02f, 0.045f, Vector2.zero, 0.001f, 0, 0.5f, 2f, 0, Pal.Hex("#fff3c0")) };
            }
        }

        void Spawn(Recipe r)
        {
            for (int i = 0; i < r.count; i++)
            {
                var f = new Floater();
                f.sr = MakeSr(r.sprite, Art(r.sprite), r.sort);
                f.size = Mathf.Lerp(r.sizeMin, r.sizeMax, (float)rng.NextDouble());
                f.pos = new Vector2(((float)rng.NextDouble() - 0.5f) * w * 1.15f, ((float)rng.NextDouble() - 0.5f) * h * 1.15f);
                f.vel = r.vel * (0.6f + (float)rng.NextDouble() * 0.8f) + new Vector2(((float)rng.NextDouble() - 0.5f) * r.velJitter, ((float)rng.NextDouble() - 0.5f) * r.velJitter * 0.6f);
                f.rot = (float)rng.NextDouble() * 360f;
                f.rotVel = r.rotSpeed * ((float)rng.NextDouble() * 2f - 1f);
                f.phase = (float)rng.NextDouble() * 6.28f;
                f.baseAlpha = r.alpha * (0.5f + (float)rng.NextDouble() * 0.5f);
                f.sway = r.sway; f.swayAmp = r.twinkle;
                f.sr.color = r.color.A(f.baseAlpha);
                f.sr.transform.localScale = Vector3.one * f.size;
                // store twinkle in swayAmp (shared field for compactness)
                items.Add(f);
            }
        }

        void Update()
        {
            if (items.Count == 0 && hero == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            time += dt;
            float hw = w * 0.58f, hh = h * 0.58f;
            foreach (var f in items)
            {
                f.pos += f.vel * dt;
                if (f.pos.x > hw) f.pos.x = -hw; else if (f.pos.x < -hw) f.pos.x = hw;
                if (f.pos.y > hh) f.pos.y = -hh; else if (f.pos.y < -hh) f.pos.y = hh;
                f.rot += f.rotVel * dt;
                float sx = f.sway > 0 ? Mathf.Sin(time * 0.7f + f.phase) * f.sway * f.size : 0f;
                f.sr.transform.localPosition = new Vector3(f.pos.x + sx, f.pos.y, 0);
                f.sr.transform.localRotation = Quaternion.Euler(0, 0, f.rot);
                if (f.swayAmp > 0f)
                {
                    float tw = 0.55f + 0.45f * Mathf.Sin(time * f.swayAmp + f.phase);
                    var c = f.sr.color; c.a = f.baseAlpha * tw; f.sr.color = c;
                }
            }
            if (hero != null)
            {
                if (motif == Motif.Dawn) hero.transform.localRotation = Quaternion.Euler(0, 0, time * 1.4f);
                else if (motif == Motif.Mandala || motif == Motif.Tablet) hero.transform.localRotation = Quaternion.Euler(0, 0, time * (motif == Motif.Tablet ? -1.2f : 2.2f));
                else if (motif == Motif.Lava) { var c = hero.color; c.a = 0.26f + 0.08f * Mathf.Sin(time * 1.3f); hero.color = c; }
            }
        }

        // ---------------------------------------------------------------------------------------------
        // procedural art
        // ---------------------------------------------------------------------------------------------

        static Sprite gradSprite, grainSprite;

        Sprite MakeGradient(Color top, Color bottom)
        {
            const int H = 128;
            var t = TexKit.NewTex(4, H, false);
            var px = new Color[4 * H];
            for (int j = 0; j < H; j++)
            {
                float k = j / (float)(H - 1); // 0 bottom, 1 top
                var c = Color.Lerp(bottom, top, k);
                for (int i = 0; i < 4; i++) px[j * 4 + i] = c;
            }
            t.SetPixels(px); t.Apply(false, false);
            t.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(t, new Rect(0, 0, 4, H), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect); // 1 unit wide, 32 tall: scaled in Build
        }

        static Sprite Grain()
        {
            if (grainSprite != null) return grainSprite;
            const int N = 256;
            var t = TexKit.NewTex(N, N);
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float n = TexKit.Fbm(i * 0.035f, j * 0.035f, 11) * 0.7f + TexKit.Noise(i * 0.5f, j * 0.5f, 5) * 0.3f;
                    float v = Mathf.Clamp01((n - 0.35f) * 1.6f);
                    px[j * N + i] = new Color(v, v, v, v * 0.5f);
                }
            t.SetPixels32(px); t.wrapMode = TextureWrapMode.Repeat;
            grainSprite = TexKit.ToSprite(t, N / 4f); // 4 units across
            return grainSprite;
        }

        static Sprite Art(string name)
        {
            if (art.TryGetValue(name, out var s)) return s;
            switch (name)
            {
                case "dot": s = Gfx.Dot; break;
                case "spark": s = Gfx.Spark; break;
                case "ring": s = Gfx.Ring; break;
                default: s = Procedural(name); break;
            }
            art[name] = s;
            return s;
        }

        static float Tri(float t) { t = t - Mathf.Floor(t); return Mathf.Abs(t * 2f - 1f); }

        static Sprite Procedural(string name)
        {
            int N = name == "rays" || name == "mandala" ? 384 : 128;
            var t = TexKit.NewTex(N, N);
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = (i + 0.5f) / N * 2f - 1f, y = (j + 0.5f) / N * 2f - 1f;
                    float a = 0;
                    float pix = 2.2f / N;
                    switch (name)
                    {
                        case "leaf":
                        {
                            float lx = x * 1.0f, ly = y;
                            float half = 0.42f * Mathf.Pow(Mathf.Max(0f, 1f - lx * lx), 0.85f);
                            a = TexKit.Cov(Mathf.Abs(ly) - half, pix * 2f);
                            if (Mathf.Abs(lx) < 0.92f) a *= 1f - 0.45f * (1f - TexKit.Smooth(0f, 0.035f, Mathf.Abs(ly)));
                            break;
                        }
                        case "hex":
                        {
                            float k = Mathf.Repeat(Mathf.Atan2(y, x), Mathf.PI / 3f) - Mathf.PI / 6f;
                            float r = Mathf.Sqrt(x * x + y * y) * Mathf.Cos(k) / 0.866f;
                            a = TexKit.Cov(Mathf.Abs(r - 0.85f) - 0.045f, pix * 2f);
                            break;
                        }
                        case "shard":
                        {
                            float d = Mathf.Abs(x) * 1.7f + Mathf.Abs(y) * 0.9f;
                            float inside = TexKit.Cov(d - 0.9f, pix * 3f);
                            float facet = (x > 0 ? 0.65f : 1f) * (y > 0 ? 1f : 0.8f);
                            a = inside * facet;
                            break;
                        }
                        case "flake":
                        {
                            float r = Mathf.Sqrt(x * x + y * y), an = Mathf.Atan2(y, x);
                            float rel = Mathf.Abs(Mathf.Sin(an * 3f)) * r;
                            float arm = TexKit.Cov(rel - 0.04f, pix * 2f) * TexKit.Sat((0.9f - r) * 8f);
                            float br = Mathf.Abs(Mathf.Sin((an + Mathf.PI / 6f) * 3f * 1f)) * r;
                            float branch = (TexKit.Cov(Mathf.Abs(Mathf.Sin(an * 3f) * r) - 0.05f, pix * 2f) > 0 ? 0f : 0f);
                            a = Mathf.Max(arm, TexKit.Cov(Mathf.Abs(r - 0.5f) - 0.03f, pix * 2f) * 0.5f * (Mathf.Abs(Mathf.Sin(an * 3f)) < 0.5f ? 1f : 0.3f));
                            break;
                        }
                        case "streak":
                        {
                            float sx = Mathf.Abs(x), sy = Mathf.Abs(y) * 14f;
                            a = TexKit.Sat(1f - (sx * sx + sy * sy)) ;
                            break;
                        }
                        case "moon":
                        {
                            float r1 = Mathf.Sqrt(x * x + y * y) - 0.82f;
                            float cx = x - 0.34f, cy = y - 0.1f;
                            float r2 = Mathf.Sqrt(cx * cx + cy * cy) - 0.74f;
                            a = TexKit.Cov(Mathf.Max(r1, -r2), pix * 2f);
                            float halo = TexKit.Smooth(1.0f, 0.5f, Mathf.Sqrt(x * x + y * y)) * 0.18f;
                            a = Mathf.Max(a, halo);
                            break;
                        }
                        case "rays":
                        {
                            float r = Mathf.Sqrt(x * x + y * y), an = Mathf.Atan2(y, x);
                            float s = 0.5f + 0.5f * Mathf.Sin(an * 9f);
                            s = Mathf.Pow(s, 3f);
                            a = s * TexKit.Smooth(1f, 0.05f, r);
                            break;
                        }
                        default: // mandala
                        {
                            float r = Mathf.Sqrt(x * x + y * y), an = Mathf.Atan2(y, x);
                            float lines = 0;
                            lines = Mathf.Max(lines, TexKit.Cov(Mathf.Abs(r - 0.96f) - 0.007f, pix));
                            lines = Mathf.Max(lines, TexKit.Cov(Mathf.Abs(r - 0.78f) - 0.006f, pix));
                            lines = Mathf.Max(lines, TexKit.Cov(Mathf.Abs(r - 0.32f) - 0.006f, pix));
                            float p1 = 0.62f * (0.55f + 0.45f * Mathf.Abs(Mathf.Cos(an * 6f)));
                            lines = Mathf.Max(lines, TexKit.Cov(Mathf.Abs(r - p1) - 0.006f, pix));
                            float p2 = 0.9f * (0.7f + 0.3f * Mathf.Abs(Mathf.Cos(an * 12f)));
                            lines = Mathf.Max(lines, TexKit.Cov(Mathf.Abs(r - p2) - 0.005f, pix) * 0.8f);
                            float spokes = TexKit.Cov(Mathf.Abs(Mathf.Sin(an * 12f)) * r - 0.004f, pix) * TexKit.Smooth(0.3f, 0.34f, r) * TexKit.Smooth(0.78f, 0.74f, r) * 0.0f;
                            a = Mathf.Max(lines, spokes);
                            break;
                        }
                    }
                    px[j * N + i] = new Color(1, 1, 1, a);
                }
            t.SetPixels32(px);
            return TexKit.ToSprite(t, N); // 1 unit wide
        }
    }
}
