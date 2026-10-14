using System;
using System.Collections.Generic;

namespace Bilye.Core
{
    /// <summary>Colours are indices into the marble palette (see Palette in the game assembly).</summary>
    public sealed class LevelConfig
    {
        public int id;
        public string name;
        /// <summary>0-based world (10 levels per world)</summary>
        public int world;
        public int indexInWorld;
        public bool boss;
        public int difficulty;
        public int colors;
        public int ballCount;
        /// <summary>chain speed in ball-diameters per second</summary>
        public double speed;
        public double ballScale;
        public double holeOffset;
        public double runBias;
        public double rushFraction;
        public double catchUp;
        public double magnet;
        public Func<PathCtx, List<V2>> path;

        public LevelConfig Clone() => (LevelConfig)MemberwiseClone();
    }

    public sealed class WorldTheme
    {
        public string name, top, bottom, accent, rim, groove, grooveDeep;
        public WorldTheme(string name, string top, string bottom, string accent, string rim, string groove, string grooveDeep)
        { this.name = name; this.top = top; this.bottom = bottom; this.accent = accent; this.rim = rim; this.groove = groove; this.grooveDeep = grooveDeep; }
    }

    /// <summary>
    /// Ten worlds, ten levels each. A world owns its colours here, its track family below (WorldRecipe) and its
    /// marble / shooter / backdrop artwork in the game assembly (WorldStyles).
    /// </summary>
    public static class Levels
    {
        public const int LevelCount = 100;
        public const int EndlessMapCount = 3;

        public static readonly WorldTheme[] Worlds =
        {
            new WorldTheme("Şafak", "#22345a", "#0b1220", "#7fa8ff", "#b9cdf5", "#0c1426", "#060b16"),
            new WorldTheme("Zümrüt", "#12362f", "#06140f", "#4fe0a8", "#a8edd0", "#08201a", "#030d0a"),
            new WorldTheme("Mercan", "#3a2230", "#150a12", "#ff8f9c", "#f6c1c8", "#22101c", "#0e060a"),
            new WorldTheme("Kehribar", "#3b2c14", "#150f05", "#f4b740", "#f1d9a0", "#221807", "#0e0903"),
            new WorldTheme("Ametist", "#2c2147", "#0f0a1c", "#b48bff", "#d8c7fa", "#150e29", "#080512"),
            new WorldTheme("Kızıl Kum", "#40241a", "#170b07", "#ff9a57", "#f4cdb0", "#25110a", "#0f0603"),
            new WorldTheme("Buzul", "#183a46", "#07151a", "#6fe3f2", "#bff0f7", "#0a232b", "#041014"),
            new WorldTheme("Gece Yarısı", "#161b33", "#05070f", "#8e9dff", "#c3caf7", "#0a0e1f", "#03050c"),
            new WorldTheme("Volkan", "#46201d", "#160807", "#ff6a52", "#f6b6a8", "#260d0b", "#0e0403"),
            new WorldTheme("Tılsım", "#2e2713", "#0d0a04", "#ffd36b", "#fbe8b0", "#1b1507", "#0a0702"),
        };

        /// <summary>Endless mode plays on a carved stone tablet instead of one of the ten worlds.</summary>
        public static readonly WorldTheme TabletTheme = new WorldTheme("Tablet", "#3f5a5c", "#1d2d30", "#e8c15a", "#9fb8b0", "#0f2417", "#06100a");

        private delegate List<V2> Recipe(PathCtx c);
        private delegate T Pick<T>(T[] list);

        private static Recipe SnakeRecipe(SnakeOpts o) => c => Tracks.Snake(c, o);
        private static Recipe RingRecipe(RingOpts o) => c => Tracks.Ring(c, o);

        private static RingOpts R(double n = 2, double laps = 0.84, double wideX = 0.6, double wideY = 0.7, double inward = 0.06, double wobble = 0, double wobbleFreq = 4)
        {
            var o = RingOpts.Default;
            o.n = n; o.laps = laps; o.wideX = wideX; o.wideY = wideY; o.inward = inward; o.wobble = wobble; o.wobbleFreq = wobbleFreq;
            return o;
        }

        private static readonly int[] Dirs = { 1, -1 };
        private static readonly double[] Starts = { -125, -60, 35, 120, 200, -170 };

        private static T PickFrom<T>(Rng rng, T[] list) => list[(int)Math.Floor(rng.Next() * list.Length)];

        /// <summary>Generic fallback recipe. Deterministic: the same level always gets the same track.</summary>
        private static Recipe GenericRecipe(int level, int attempt = 0)
        {
            var rng = new Rng((long)level * 7919 + 13 + (long)attempt * 104729);
            bool boss = level % 10 == 0;
            int tier = level <= 12 ? 0 : level <= 35 ? 1 : level <= 65 ? 2 : 3;

            bool ringsAllowed = attempt < 14;
            if (ringsAllowed && (level <= 3 || (tier == 0 && !boss && rng.Next() < 0.45)))
            {
                double n = PickFrom(rng, new[] { 2, 2, 2.6, 3.2, 4 });
                double start = PickFrom(rng, Starts);
                int dir = PickFrom(rng, Dirs);
                double laps = 0.84 + rng.Next() * 0.06;
                double wideX = 0.55 + rng.Next() * 0.45;
                double wideY = 0.65 + rng.Next() * 0.35;
                double inward = 0.03 + rng.Next() * 0.07;
                double wobble = tier == 0 ? 0 : 0.03;
                double wf = PickFrom(rng, new double[] { 3, 4, 5 });
                var o = R(n, laps, wideX, wideY, inward, wobble, wf); o.start = start; o.dir = dir;
                return RingRecipe(o);
            }
            if (ringsAllowed && tier == 1 && !boss && rng.Next() < 0.25)
            {
                double n = PickFrom(rng, new[] { 2.4, 3.2, 4, 5 });
                double start = PickFrom(rng, new double[] { -125, 35, 120, 200 });
                int dir = PickFrom(rng, Dirs);
                double wideX = 0.7 + rng.Next() * 0.3;
                double wideY = 0.7 + rng.Next() * 0.3;
                double wf = PickFrom(rng, new double[] { 3, 5, 7 });
                var o = R(n, 0.84, wideX, wideY, 0.08, 0.04, wf); o.start = start; o.dir = dir;
                return RingRecipe(o);
            }

            int[][] lanes = { new[] { 1, 1 }, new[] { 2, 1 }, new[] { 1, 2 }, new[] { 2, 2 }, new[] { 3, 2 }, new[] { 2, 3 }, new[] { 3, 3 }, new[] { 4, 3 }, new[] { 3, 4 }, new[] { 4, 4 } };
            int minIdx = new[] { 0, 2, 4, 6 }[tier];
            int maxIdx = new[] { 3, 6, 8, 9 }[tier];
            int[] tb = boss ? lanes[maxIdx] : lanes[minIdx + (int)Math.Floor(rng.Next() * (maxIdx - minIdx + 1))];
            double wave = tier == 0 ? 0 : tier == 1 ? PickFrom(rng, new[] { 0, 0, 0.3 }) : tier == 2 ? PickFrom(rng, new[] { 0, 0.35, 0.55 }) : PickFrom(rng, new[] { 0.35, 0.55, 0.8 });
            int spine = PickFrom(rng, Dirs);
            double waves = PickFrom(rng, new double[] { 2, 2, 3 });
            bool mx = rng.Next() < 0.5;
            bool my = rng.Next() < 0.5;
            double holeAt = 0.4 + rng.Next() * 0.45;
            return SnakeRecipe(SnakeOpts.Make(tb[0], tb[1], spine, wave, waves, mx, my, holeAt));
        }

        /// <summary>
        /// The track "personality" of each world, so a world feels designed rather than shuffled:
        ///  0 Şafak wide sun arcs, 1 Zümrüt winding vines, 2 Mercan coral blossoms, 3 Kehribar honeycomb,
        ///  4 Ametist crystal facets, 5 Kızıl Kum rolling dunes, 6 Buzul straight ice sheets, 7 Gece Yarısı constellations,
        ///  8 Volkan lava rivers, 9 Tılsım everything at once. k is the position inside the world (0..1).
        /// </summary>
        private static Recipe WorldRecipe(int world, double k, bool boss, Rng rng)
        {
            Recipe Snk(int top, int bottom, double wave, double waves = 2)
            {
                int spine = PickFrom(rng, Dirs);
                bool mx = rng.Next() < 0.5;
                bool my = rng.Next() < 0.5;
                double holeAt = 0.4 + rng.Next() * 0.45;
                return SnakeRecipe(SnakeOpts.Make(top, bottom, spine, wave, waves, mx, my, holeAt));
            }
            Recipe Lap(RingOpts o)
            {
                o.dir = PickFrom(rng, Dirs);
                o.start = PickFrom(rng, Starts);
                return RingRecipe(o);
            }
            int Round(double v) => (int)Math.Floor(v + 0.5);
            switch (world)
            {
                case 0:
                    {
                        double laps = 0.84 + rng.Next() * 0.06, wx = 0.55 + rng.Next() * 0.45, wy = 0.65 + rng.Next() * 0.35;
                        double inward = 0.03 + rng.Next() * 0.05 + (boss ? 0.06 : 0);
                        return Lap(R(2, laps, wx, wy, inward));
                    }
                case 1:
                    if (boss) return Snk(3, 2, 0.5, 3);
                    {
                        double w = 0.25 + 0.3 * k;
                        double waves = PickFrom(rng, new double[] { 2, 3 });
                        return Snk(2, k < 0.5 ? 1 : 2, w, waves);
                    }
                case 2:
                    {
                        double laps = 0.86 + rng.Next() * 0.04, wx = 0.8 + rng.Next() * 0.2, wy = 0.8 + rng.Next() * 0.2;
                        double wob = Math.Min(0.09, 0.05 + 0.04 * k);
                        double wf = PickFrom(rng, new double[] { 5, 6, 7 });
                        return Lap(R(2, laps, wx, wy, 0.05, wob, wf));
                    }
                case 3:
                    if (!boss && k < 0.5)
                    {
                        double wx = 0.75 + rng.Next() * 0.25, wy = 0.75 + rng.Next() * 0.25;
                        return Lap(R(4, 0.88, wx, wy, 0.08));
                    }
                    return Snk(boss ? 3 : 2 + Round(k), boss ? 3 : 2, 0);
                case 4:
                    if (!boss && k < 0.5)
                    {
                        double n = PickFrom(rng, new[] { 2.6, 3, 3.2 });
                        double wx = 0.8 + rng.Next() * 0.2, wy = 0.8 + rng.Next() * 0.2;
                        return Lap(R(n, 0.9, wx, wy, 0.12, 0.05, 3));
                    }
                    return Snk(3, boss ? 3 : 2, 0.35, 3);
                case 5:
                    {
                        int t = boss ? 3 : 2 + Round(k);
                        int b = boss ? 3 : 2 + Round(k);
                        double wv = 0.55 + 0.25 * rng.Next();
                        return Snk(t, b, wv, 2);
                    }
                case 6:
                    if (!boss && rng.Next() < 0.3) return Lap(R(4, 0.88, 0.85, 0.85, 0.1));
                    return Snk(3, boss ? 3 : 2 + Round(k), 0);
                case 7:
                    if (!boss && k < 0.4) return Lap(R(2.4, 0.9, 0.85, 0.9, 0.1, 0.06, 4));
                    return Snk(3, boss ? 3 : 2 + Round(k), 0.45, 3);
                case 8:
                    {
                        int t = boss ? 4 : 3;
                        int b = boss ? 4 : 3 + Round(k);
                        double wv = 0.55 + 0.25 * rng.Next();
                        return Snk(t, b, wv, 3);
                    }
                default:
                    if (!boss && k < 0.3) return Lap(R(2.6, 0.9, 0.9, 0.9, 0.1, 0.07, 7));
                    {
                        int b = boss ? 4 : 3 + Round(k);
                        double wv = 0.35 + 0.45 * rng.Next();
                        double waves = PickFrom(rng, new double[] { 2, 3 });
                        return Snk(4, b, wv, waves);
                    }
            }
        }

        private static Recipe RecipeFor(int level, int attempt = 0)
        {
            if (attempt < 14)
            {
                var rng = new Rng((long)level * 6131 + 7 + (long)attempt * 7001);
                int idx = (level - 1) % 10;
                return WorldRecipe((level - 1) / 10, idx / 9.0, idx == 9, rng);
            }
            return GenericRecipe(level, attempt - 14);
        }

        /// <summary>One smooth curve for every parameter: level 1 is a stroll, level 100 is a test of nerve.</summary>
        public static LevelConfig DifficultyFor(int level)
        {
            double t = (level - 1) / (double)(LevelCount - 1);
            double R3(double v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
            return new LevelConfig
            {
                difficulty = Math.Min(5, 1 + (int)Math.Floor(t * 5)),
                colors = level <= 6 ? 3 : level <= 24 ? 4 : level <= 52 ? 5 : 6,
                ballCount = (int)Math.Floor(16 + 118 * t + 0.5),
                speed = R3(0.5 + 0.52 * t),
                ballScale = R3(1 - 0.13 * t),
                holeOffset = R3(Math.Max(0, 1 - 1.5 * t)),
                runBias = R3(0.5 - 0.26 * t),
                rushFraction = R3(0.45 - 0.23 * t),
                catchUp = R3(2.2 + 0.6 * t),
                magnet = R3(3 - 1.8 * t),
            };
        }

        private static readonly Dictionary<string, List<V2>> TrackCache = new Dictionary<string, List<V2>>();

        /// <summary>
        /// The track of a level on THIS screen. The level's own recipe is tried first; if it does not fit the device
        /// (lanes would touch, the track would leave the field...) the next variation of the same level is tried.
        /// </summary>
        private static List<V2> ResolveTrack(int level, PathCtx c)
        {
            string key = $"{level}|{c.dx:0.0000}|{c.dy:0.0000}";
            if (TrackCache.TryGetValue(key, out var hit)) return hit;
            List<V2> pts = null;
            for (int attempt = 0; attempt < 56; attempt++)
            {
                pts = RecipeFor(level, attempt)(c);
                if (Tracks.Validate(pts, c)) break;
            }
            if (TrackCache.Count > 24) TrackCache.Clear();
            TrackCache[key] = pts;
            return pts;
        }

        /// <summary>Lazily built, the 100 level configs.</summary>
        public static readonly LevelConfig[] All = BuildLevels();

        private static LevelConfig[] BuildLevels()
        {
            var list = new LevelConfig[LevelCount];
            for (int id = 1; id <= LevelCount; id++)
            {
                int world = (id - 1) / 10;
                int idx = (id - 1) % 10 + 1;
                var cfg = DifficultyFor(id);
                cfg.id = id;
                cfg.name = $"{Worlds[world].name} {idx}";
                cfg.world = world;
                cfg.indexInWorld = idx;
                cfg.boss = idx == 10;
                int levelId = id;
                cfg.path = c => ResolveTrack(levelId, c);
                list[id - 1] = cfg;
            }
            return list;
        }

        public static LevelConfig Get(int id) => All[Math.Max(0, Math.Min(All.Length - 1, id - 1))];

        public static WorldTheme WorldOf(int levelId) => Worlds[Math.Max(0, Math.Min(Worlds.Length - 1, (levelId - 1) / 10))];

        /// <summary>Background demo track used behind the main menu.</summary>
        public static readonly LevelConfig Menu = new LevelConfig
        {
            id = 0, name = "Menü", world = 0, indexInWorld = 1, boss = false, difficulty = 1, colors = 5, ballCount = 40, speed = 0.6,
            ballScale = 1, holeOffset = 0.5, runBias = 0.4, rushFraction = 0.5, catchUp = 2, magnet = 2,
            path = c => { var o = R(2, 0.88, 0.9, 0.95, 0.06); o.start = -125; return Tracks.Ring(c, o); },
        };

        private static readonly Dictionary<string, List<V2>> FixedCache = new Dictionary<string, List<V2>>();

        private static List<V2> FixedTrack(string key, PathCtx c, IList<Func<PathCtx, List<V2>>> candidates)
        {
            string k = $"{key}|{c.dx:0.0000}|{c.dy:0.0000}";
            if (FixedCache.TryGetValue(k, out var hit)) return hit;
            List<V2> pts = null;
            foreach (var r in candidates)
            {
                pts = r(c);
                if (Tracks.Validate(pts, c)) break;
            }
            if (FixedCache.Count > 24) FixedCache.Clear();
            FixedCache[k] = pts;
            return pts;
        }

        private static LevelConfig EndlessBase(string name, double ballScale, Func<PathCtx, List<V2>> path) => new LevelConfig
        {
            id = 0, world = 9, indexInWorld = 1, boss = false, difficulty = 2, colors = 4, ballCount = 30, speed = 0.55, holeOffset = 0.8,
            runBias = 0.4, rushFraction = 0.4, catchUp = 2.3, magnet = 3, name = name, ballScale = ballScale, path = path,
        };

        /// <summary>
        /// Endless mode: balls never run out, speed and points rise with the score. Three maps on the stone tablet:
        /// 0 Sarmal (classic spiral, smallest marbles), 1 Yılan (long serpent), 2 Gül (petalled ring).
        /// </summary>
        public static readonly LevelConfig[] EndlessMaps =
        {
            EndlessBase("Sarmal", 0.62, c =>
            {
                var cands = new List<Func<PathCtx, List<V2>>>();
                double[] turns = { 2.4, 2.1, 1.8, 1.6, 1.45, 1.3, 1.15, 1.0 };
                for (int i = 1; i <= 8; i++)
                {
                    double t = turns[i - 1];
                    double n = i % 2 == 1 ? 2.2 : 2;
                    cands.Add(cc => { var o = SpiralOpts.Default; o.turns = t; o.n = n; o.start = -125; o.dir = 1; return Tracks.Spiral(cc, o); });
                }
                cands.Add(cc => { var o = R(2, 0.9, 0.95, 0.95, 0.12); o.start = -125; return Tracks.Ring(cc, o); });
                return FixedTrack("sp", c, cands);
            }),
            EndlessBase("Yılan", 0.82, c => FixedTrack("sn", c, new Func<PathCtx, List<V2>>[]
            {
                cc => Tracks.Snake(cc, SnakeOpts.Make(4, 3, 1, 0.4, 2, false, false, 0.7)),
                cc => Tracks.Snake(cc, SnakeOpts.Make(3, 3, 1, 0.35, 2, false, false, 0.7)),
                cc => Tracks.Snake(cc, SnakeOpts.Make(2, 2, 1, 0, 2, false, false, 0.7)),
            })),
            EndlessBase("Gül", 0.8, c => FixedTrack("ro", c, new Func<PathCtx, List<V2>>[]
            {
                cc => { var o = R(2, 0.9, 1, 1, 0.1, 0.08, 6); o.start = 35; return Tracks.Ring(cc, o); },
                cc => { var o = R(2, 0.88, 0.9, 0.9, 0.1, 0.05, 5); o.start = 35; return Tracks.Ring(cc, o); },
                cc => { var o = R(2, 0.88, 0.8, 0.9, 0.08); o.start = 35; return Tracks.Ring(cc, o); },
            })),
        };

        /// <summary>levelId 0, -1, -2 are the three endless maps (0 stays the classic one).</summary>
        public static LevelConfig EndlessMapOf(int levelId) => EndlessMaps[Math.Max(0, Math.Min(EndlessMaps.Length - 1, Math.Abs(levelId)))];
    }
}
