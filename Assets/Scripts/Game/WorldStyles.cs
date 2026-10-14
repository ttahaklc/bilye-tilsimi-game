using UnityEngine;

namespace Bilye
{
    public enum BallStyle { Orb, Gem, Pearl, Hex, Crystal, Rune, Frost, Moon, Ember, Talisman, Glyph }
    public enum BarrelStyle { Taper, Leaf, Fan, Hex, Prism, Stone, Spike, Tube, Forge, Idol }
    public enum HubStyle { Dial, Petal, Pearl, Hex, Facet, Sun, Snow, Moon, Gear, Mandala, Idol }
    public enum Motif { Dawn, Leaves, Coral, Honey, Crystals, Dunes, Ice, Night, Lava, Mandala, Tablet }

    public sealed class Metal
    {
        public Color hi, mid, lo, glow;
        public Color[] body;
        public Metal(string hi, string mid, string lo, string b0, string b1, string b2, string glow)
        {
            this.hi = Pal.Hex(hi); this.mid = Pal.Hex(mid); this.lo = Pal.Hex(lo); this.glow = Pal.Hex(glow);
            body = new[] { Pal.Hex(b0), Pal.Hex(b1), Pal.Hex(b2) };
        }
        public TexKit.Ramp MetalRamp(float spec = 0.55f) => new TexKit.Ramp(lo, mid, hi, spec);
        public TexKit.Ramp BodyRamp(float spec = 0.35f) => new TexKit.Ramp(body[2], body[1], body[0], spec);
    }

    public sealed class WorldStyle
    {
        public BallStyle ball; public BarrelStyle barrel; public HubStyle hub; public Motif motif;
        public Metal metal; public bool sun; public Color portal;
        public int key; // unique id for caches
        public WorldStyle(int key, BallStyle b, BarrelStyle br, HubStyle h, Motif m, Metal metal, string portalRgb, bool sun = false)
        {
            this.key = key; ball = b; barrel = br; hub = h; motif = m; this.metal = metal; this.sun = sun;
            var p = portalRgb.Split(',');
            portal = new Color(int.Parse(p[0]) / 255f, int.Parse(p[1]) / 255f, int.Parse(p[2]) / 255f, 1);
        }
    }

    public static class WorldStyles
    {
        static readonly Metal Gold = new Metal("#fbeebc", "#d4a64a", "#a87a24", "#aab8dc", "#4a5a82", "#1a2136", "#fff1b8");

        public static readonly WorldStyle[] All =
        {
            new WorldStyle(0, BallStyle.Orb, BarrelStyle.Taper, HubStyle.Dial, Motif.Dawn, Gold, "110,130,255"),
            new WorldStyle(1, BallStyle.Gem, BarrelStyle.Leaf, HubStyle.Petal, Motif.Leaves, new Metal("#d6f7c8", "#7fbf6a", "#3f7a3a", "#7fd4a5", "#2c7a5c", "#0f3a2c", "#caffd8"), "60,230,160"),
            new WorldStyle(2, BallStyle.Pearl, BarrelStyle.Fan, HubStyle.Pearl, Motif.Coral, new Metal("#ffe3e6", "#f0a4ae", "#b8626f", "#f6c9d0", "#b66a7a", "#4a2030", "#fff0f2"), "255,120,150"),
            new WorldStyle(3, BallStyle.Hex, BarrelStyle.Hex, HubStyle.Hex, Motif.Honey, new Metal("#ffe9a6", "#e0a02a", "#8f5a0c", "#e8c26a", "#9a6a1c", "#3a2408", "#fff0b8"), "255,170,40"),
            new WorldStyle(4, BallStyle.Crystal, BarrelStyle.Prism, HubStyle.Facet, Motif.Crystals, new Metal("#efe0ff", "#a67be8", "#5a3a9a", "#c9a8ff", "#6a44b8", "#241046", "#f2e6ff"), "170,110,255"),
            new WorldStyle(5, BallStyle.Rune, BarrelStyle.Stone, HubStyle.Sun, Motif.Dunes, new Metal("#f7dcc0", "#c98a56", "#7a4426", "#d9a77a", "#8a5638", "#3a2014", "#ffe2bb"), "255,140,70"),
            new WorldStyle(6, BallStyle.Frost, BarrelStyle.Spike, HubStyle.Snow, Motif.Ice, new Metal("#f2fdff", "#8fd8e8", "#3d8aa0", "#c8f2fa", "#5ab0c8", "#163c4c", "#ffffff"), "100,225,250"),
            new WorldStyle(7, BallStyle.Moon, BarrelStyle.Tube, HubStyle.Moon, Motif.Night, new Metal("#e4e8ff", "#8e9ad8", "#454e90", "#aab4ea", "#4c5698", "#161a3a", "#eef0ff"), "140,150,255"),
            new WorldStyle(8, BallStyle.Ember, BarrelStyle.Forge, HubStyle.Gear, Motif.Lava, new Metal("#ffc9a0", "#c4553a", "#6a1f14", "#8a6a62", "#4a3330", "#1a0e0c", "#ff9a5a"), "255,100,70"),
            new WorldStyle(9, BallStyle.Talisman, BarrelStyle.Taper, HubStyle.Mandala, Motif.Mandala, Gold, "255,200,90"),
        };

        public static readonly WorldStyle Tablet = new WorldStyle(10, BallStyle.Glyph, BarrelStyle.Idol, HubStyle.Idol, Motif.Tablet,
            new Metal("#f6e7b4", "#c9a24a", "#7d5f1e", "#b9c4b5", "#6c7c72", "#27332f", "#fff3c0"), "255,200,80", true);

        public static WorldStyle Of(int world, bool endless) => endless ? Tablet : All[Mathf.Clamp(world, 0, 9)];
    }
}
