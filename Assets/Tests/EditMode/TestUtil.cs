using System;
using System.Collections.Generic;
using Bilye.Core;

namespace Bilye.Tests
{
    public struct View
    {
        public string name; public double w, h; public Insets ins;
        public View(string n, double w, double h, Insets i) { name = n; this.w = w; this.h = h; ins = i; }
    }

    public static class TestUtil
    {
        static View P(string n, double w, double h, double top, double bottom) => new View("P " + n, w, h, new Insets(top, bottom, 0, 0));
        static View L(string n, double w, double h, double side, double bottom) => new View("L " + n, h, w, new Insets(0, bottom, side, side));

        /// <summary>Real device sizes (points) with their safe-area insets.</summary>
        public static readonly View[] Views =
        {
            P("iPhone SE 375x667", 375, 667, 20, 0), P("iPhone 13 mini 375x812", 375, 812, 50, 34), P("iPhone 16 393x852", 393, 852, 59, 34),
            P("iPhone 16 Plus 430x932", 430, 932, 59, 34), P("iPhone 16 Pro Max 440x956", 440, 956, 62, 34), P("Android 360x640", 360, 640, 24, 0),
            P("Android 412x915", 412, 915, 32, 24), P("Android narrow 320x568", 320, 568, 20, 0), P("iPad mini 744x1133", 744, 1133, 24, 20),
            P("iPad 820x1180", 820, 1180, 24, 20), L("iPhone SE", 375, 667, 0, 0), L("iPhone 16", 393, 852, 59, 21),
            L("iPhone 16 Pro Max", 440, 956, 62, 21), L("Android 360x640", 360, 640, 0, 0), L("iPad 820x1180", 820, 1180, 20, 20),
        };

        public static List<string> Audit(LevelConfig level, View v)
        {
            var layout = Layout.Compute(v.w, v.h, v.ins, level.ballScale);
            var g = new Game(new GameOptions { level = level, layout = layout, seed = 1 });
            var path = g.path; double d = g.d; var f = layout.field;
            var issues = new List<string>();
            int startK = (int)Math.Ceiling((d * 2.2 + d * 6) / path.step);
            int outside = 0; double worst = 0; double m = d * 0.78;
            for (int k = startK; k < path.count; k++)
            {
                double ex = Math.Max(Math.Max(f.x + m - path.xs[k], path.xs[k] - (f.x + f.w - m)), Math.Max(f.y + m - path.ys[k], path.ys[k] - (f.y + f.h - m)));
                if (ex > 3) { outside++; worst = Math.Max(worst, ex); }
            }
            if (outside > 0) issues.Add($"leaves the field ({outside} pts, {worst / d:0.00}d)");
            double minSep = double.PositiveInfinity;
            int gap = (int)Math.Ceiling(5 * d / path.step);
            int stride = Math.Max(1, (int)Math.Round(d / 8 / path.step));
            int inF = 0;
            while (inF < path.count - 1 && !(path.xs[inF] > f.x && path.xs[inF] < f.x + f.w && path.ys[inF] > f.y && path.ys[inF] < f.y + f.h)) inF++;
            for (int a = inF; a < path.count; a += stride)
                for (int b = a + gap; b < path.count; b += stride)
                {
                    double dd = Math.Sqrt(Math.Pow(path.xs[a] - path.xs[b], 2) + Math.Pow(path.ys[a] - path.ys[b], 2));
                    if (dd < minSep) minSep = dd;
                }
            if (minSep < 1.62 * d) issues.Add($"lanes touch ({minSep / d:0.00}d)");
            double minC = double.PositiveInfinity;
            for (int k = 0; k < path.count; k++) minC = Math.Min(minC, Math.Sqrt(Math.Pow(path.xs[k] - layout.center.x, 2) + Math.Pow(path.ys[k] - layout.center.y, 2)));
            if (minC < 2.7 * d) issues.Add($"too close to the shooter ({minC / d:0.00}d)");
            double visible = path.length - startK * path.step;
            if (visible / d < 20) issues.Add($"track too short ({visible / d:0}d)");
            var c = layout.clip;
            if (path.xs[0] > c.x && path.xs[0] < c.x + c.w && path.ys[0] > c.y && path.ys[0] < c.y + c.h) issues.Add("entry starts inside the screen");
            return issues;
        }

        /// <summary>A simple bot: aims at the ball it can actually reach whose colour matches the loaded ball.</summary>
        public static void BotShot(Game g)
        {
            double bestAngle = double.NaN, bestScore = double.NegativeInfinity;
            for (int k = 0; k < 360; k++)
            {
                double a = k / 360.0 * Math.PI * 2;
                int idx = g.RayHit(a, out _);
                if (idx < 0) continue;
                var b = g.balls[idx];
                double score = 0;
                if (b.color == g.loaded) score += 10;
                score += b.s / g.targetS * 5;
                if (score > bestScore) { bestScore = score; bestAngle = a; }
            }
            if (!double.IsNaN(bestAngle))
            {
                g.AimAt(g.layout.center.x + Math.Cos(bestAngle) * 100, g.layout.center.y + Math.Sin(bestAngle) * 100);
                g.Fire();
            }
        }

        public static void CheckInvariants(Game g, string label)
        {
            double d = g.d;
            for (int i = 0; i < g.balls.Count; i++)
            {
                var b = g.balls[i];
                if (double.IsNaN(b.s) || double.IsNaN(b.x) || double.IsNaN(b.y)) throw new Exception(label + ": NaN ball");
                if (i > 0 && b.s - g.balls[i - 1].s < d * 0.995) throw new Exception($"{label}: balls overlap (gap {(b.s - g.balls[i - 1].s) / d:0.000}d at {i})");
            }
        }

        public static Game PlayBot(LevelConfig lvl, View dev, long seed, double maxSeconds, List<GameEvent> events = null, bool infinite = false)
        {
            var layout = Layout.Compute(dev.w, dev.h, dev.ins, lvl.ballScale);
            var g = new Game(new GameOptions { level = lvl, layout = layout, seed = seed, infinite = infinite, onEvent = e => events?.Add(e) });
            const double dt = 1.0 / 60;
            double t = 0, since = 0;
            while (g.phase != GamePhase.Won && g.phase != GamePhase.Lost && t < maxSeconds)
            {
                since += dt;
                if (since > 0.28) { BotShot(g); since = 0; }
                g.Update(dt);
                CheckInvariants(g, $"L{lvl.id} t={t:0.0}");
                t += dt;
            }
            return g;
        }
    }
}
