using UnityEngine;

namespace Bilye
{
    public struct BallPal
    {
        public Color light, baseC, dark, glow, text;
        public BallPal(string l, string b, string d, string g, string t)
        {
            light = Pal.Hex(l); baseC = Pal.Hex(b); dark = Pal.Hex(d); glow = Pal.Hex(g); text = Pal.Hex(t);
        }
    }

    /// <summary>"Obsidian and gold" palette (from the RN theme).</summary>
    public static class Pal
    {
        public static Color Hex(string h, float a = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(h, out var c)) c = Color.magenta;
            c.a = a;
            return c;
        }

        public static Color A(this Color c, float a) { c.a = a; return c; }

        public static readonly BallPal[] Balls =
        {
            new BallPal("#ffb0a6", "#e8333f", "#6e0a22", "#ff5b62", "#ff6b6b"),
            new BallPal("#fff3a6", "#ffc31c", "#a85f00", "#ffd75a", "#ffd24a"),
            new BallPal("#b4f9bb", "#24b55a", "#07532b", "#4be384", "#5be584"),
            new BallPal("#aad6ff", "#2b7bf0", "#0f3490", "#5b9fff", "#6aa5ff"),
            new BallPal("#e6c2ff", "#9442e8", "#40137f", "#b878ff", "#c08bff"),
            new BallPal("#bafff6", "#14c6b9", "#06625d", "#43eadd", "#4fe8dc"),
        };

        public static readonly Color Ink = Hex("#06080f");
        public static readonly Color Surface = Hex("#0e1420");
        public static readonly Color SurfaceHi = Hex("#161e30");
        public static readonly Color GoldHi = Hex("#fbeebc");
        public static readonly Color Gold = Hex("#e7c46d");
        public static readonly Color GoldMid = Hex("#d4a64a");
        public static readonly Color GoldDeep = Hex("#a87a24");
        public static readonly Color GoldInk = Hex("#3a2808");
        public static readonly Color GoldLine = Hex("#e7c46d", 0.32f);
        public static readonly Color Text = Hex("#f4efe3");
        public static readonly Color TextDim = Hex("#a5adbd");
        public static readonly Color TextFaint = Hex("#6c7587");
        public static readonly Color Danger = Hex("#e5576b");
        public static readonly Color DangerSoft = Hex("#ffc2cb");
        public static readonly Color Good = Hex("#58d6a0");
        public static readonly Color Glass = Hex("#0a0e18", 0.78f);
    }
}
