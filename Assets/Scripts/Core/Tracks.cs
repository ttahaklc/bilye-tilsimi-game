using System;
using System.Collections.Generic;
using System.Linq;

namespace Bilye.Core
{
    /// <summary>
    /// Real field size in ball diameters: dx / dy are one ball diameter as a fraction of the unit-x / unit-y span.
    /// Tracks are authored in "d-space" (units of one ball diameter, origin at the shooter) and converted to the
    /// 0..1 unit space the engine maps onto the screen.
    /// </summary>
    public struct PathCtx
    {
        public double dx, dy;
        public PathCtx(double dx, double dy) { this.dx = dx; this.dy = dy; }
    }

    public struct SnakeOpts
    {
        public int top, bottom;
        public int spine; // +1 / -1
        public double wave, waves;
        public bool mirrorX, mirrorY;
        public double holeAt;
        public static SnakeOpts Make(int top, int bottom, int spine = 1, double wave = 0, double waves = 2, bool mirrorX = false, bool mirrorY = false, double holeAt = 0.62)
            => new SnakeOpts { top = top, bottom = bottom, spine = spine, wave = wave, waves = waves, mirrorX = mirrorX, mirrorY = mirrorY, holeAt = holeAt };
    }

    public struct RingOpts
    {
        public double n, start, laps, wideX, wideY, inward, wobble, wobbleFreq;
        public int dir;
        /// <summary>Defaults of the TypeScript original.</summary>
        public static RingOpts Default => new RingOpts { n = 2, start = 0, dir = 1, laps = 0.84, wideX = 0.6, wideY = 0.7, inward = 0.06, wobble = 0, wobbleFreq = 4 };
    }

    public struct SpiralOpts
    {
        public double n, start, turns;
        public int dir;
        public static SpiralOpts Default => new SpiralOpts { n = 2, start = -125, dir = 1, turns = 2.2 };
    }

    public static class Tracks
    {
        public const double Platform = 1.85, Half = 0.75, Clear = 3.1, Edge = 1.05, Pitch = 1.82;
        private const double SpiralPitch = 1.96;

        private static void Line(List<V2> o, double x0, double y0, double x1, double y1, double step = 0.5)
        {
            double len = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            int n = Math.Max(1, (int)Math.Ceiling(len / step));
            for (int i = 1; i <= n; i++)
            {
                double t = (double)i / n;
                o.Add(new V2(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t));
            }
        }

        private static void Arc(List<V2> o, double cx, double cy, double r, double a0, double a1, double step = 0.35)
        {
            int n = Math.Max(4, (int)Math.Ceiling(Math.Abs(a1 - a0) * r / step));
            for (int i = 1; i <= n; i++)
            {
                double a = a0 + (a1 - a0) * i / n;
                o.Add(new V2(cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
        }

        /// <summary>Largest number of lanes that fit between the border and the shooter.</summary>
        public static int MaxLanes(PathCtx c)
        {
            double HY = 0.5 / c.dy;
            double avail = HY - Edge - Clear;
            return Math.Max(1, (int)Math.Floor(avail / Pitch) + 1);
        }

        /// <summary>Serpentine ("S") track: lanes above the shooter, a crossing along one border, lanes below.</summary>
        public static List<V2> Snake(PathCtx c, SnakeOpts o)
        {
            double HX = 0.5 / c.dx;
            double HY = 0.5 / c.dy;
            int cap = MaxLanes(c);
            int T = Math.Max(1, Math.Min(o.top, cap));
            int B = Math.Max(0, Math.Min(o.bottom, cap));
            double x1 = HX - Edge;
            double ampReq = Math.Max(0, o.wave);
            double waves = o.waves;

            double[] LanesFor(int n, double a)
            {
                double outer = HY - Edge - a;
                double inner = Clear + a;
                if (n <= 1) return new[] { (outer + inner) / 2 };
                double pitch = (outer - inner) / (n - 1);
                var r = new double[n];
                for (int i = 0; i < n; i++) r[i] = outer - pitch * i;
                return r;
            }

            double amp = ampReq;
            double PitchOf(double a)
            {
                var ps = new List<double>();
                if (T > 1) { var l = LanesFor(T, a); ps.Add(l[0] - l[1]); }
                if (B > 1) { var l = LanesFor(B, a); ps.Add(l[0] - l[1]); }
                return ps.Count > 0 ? ps.Min() : 99;
            }
            if (amp > 0)
            {
                double slope = 2 * Math.PI * waves / Math.Max(2, 2 * (x1 - 2));
                while (amp > 0.01)
                {
                    double p = PitchOf(amp);
                    if (p >= 99) break;
                    if (p / Math.Sqrt(1 + (amp * slope) * (amp * slope)) >= 1.74) break;
                    amp -= 0.05;
                }
                if (amp <= 0.01) amp = 0;
            }
            var topY = LanesFor(T, amp).Select(y => -y).ToArray();
            var botArr = LanesFor(B, amp); Array.Reverse(botArr);
            var botY = B == 0 ? new double[0] : botArr;
            double Wob(double x, double xl, double xr)
            {
                if (amp <= 0) return 0;
                double t = (x - xl) / (xr - xl);
                if (t <= 0 || t >= 1) return 0;
                return amp * Math.Sin(2 * Math.PI * waves * t) * Math.Sin(Math.PI * t);
            }

            double rq = Math.Min(2.0, Clear * 0.64);
            int spine = o.spine;
            var pts = new List<V2>();

            void LaneRun(double y, double xFrom, double xTo)
            {
                double lo = Math.Min(xFrom, xTo), hi = Math.Max(xFrom, xTo);
                int n = Math.Max(1, (int)Math.Ceiling((hi - lo) / 0.5));
                for (int i = 1; i <= n; i++)
                {
                    double x = xFrom + (xTo - xFrom) * i / n;
                    pts.Add(new V2(x, y + Wob(x, -(x1 - 2), x1 - 2)));
                }
            }

            int topLast = T - 1;
            int d0 = o.bottom > 0 ? spine * (topLast % 2 == 0 ? 1 : -1) : spine;

            double entryX = -d0 * (HX + 4);
            pts.Add(new V2(entryX, topY[0]));
            int dir = d0;
            for (int k = 0; k < T; k++)
            {
                double y = topY[k];
                double rU = k < T - 1 ? Math.Abs(topY[k + 1] - topY[k]) / 2 : 0;
                double xEnd = dir * (x1 - (k < T - 1 ? rU : rq));
                bool last = k == T - 1;
                if (last && B == 0)
                {
                    double hole = o.holeAt;
                    double xStart = pts[pts.Count - 1].x;
                    LaneRun(y, xStart, xStart + (xEnd - xStart) * hole);
                    break;
                }
                LaneRun(y, pts[pts.Count - 1].x, xEnd);
                if (!last)
                {
                    double yMid = (topY[k] + topY[k + 1]) / 2;
                    double a0 = -Math.PI / 2;
                    if (dir > 0) Arc(pts, xEnd, yMid, rU, a0, a0 + Math.PI);
                    else Arc(pts, xEnd, yMid, rU, a0, a0 - Math.PI);
                    dir = -dir;
                }
            }

            if (B > 0)
            {
                double yt = topY[topLast];
                double yb = botY[0];
                double xs = spine * (x1 - rq);
                if (spine > 0) Arc(pts, xs, yt + rq, rq, -Math.PI / 2, 0);
                else Arc(pts, xs, yt + rq, rq, -Math.PI / 2, -Math.PI);
                Line(pts, spine * x1, yt + rq, spine * x1, yb - rq);
                if (spine > 0) Arc(pts, xs, yb - rq, rq, 0, Math.PI / 2);
                else Arc(pts, xs, yb - rq, rq, Math.PI, Math.PI / 2);
                dir = -spine;
                for (int k = 0; k < B; k++)
                {
                    double y = botY[k];
                    bool lastB = k == B - 1;
                    double rU = !lastB ? Math.Abs(botY[k + 1] - botY[k]) / 2 : 0;
                    double xEnd = dir * (x1 - (!lastB ? rU : 0));
                    if (lastB)
                    {
                        double hole = o.holeAt;
                        double xStart = pts[pts.Count - 1].x;
                        LaneRun(y, xStart, xStart + (xEnd - xStart) * hole);
                        break;
                    }
                    LaneRun(y, pts[pts.Count - 1].x, xEnd);
                    double yMid = (botY[k] + botY[k + 1]) / 2;
                    if (dir > 0) Arc(pts, xEnd, yMid, rU, -Math.PI / 2, Math.PI / 2);
                    else Arc(pts, xEnd, yMid, rU, -Math.PI / 2, -Math.PI * 1.5);
                    dir = -dir;
                }
            }

            double mx = o.mirrorX ? -1 : 1;
            double my = o.mirrorY ? -1 : 1;
            return pts.Select(p => new V2(0.5 + mx * p.x * c.dx, 0.5 + my * p.y * c.dy)).ToList();
        }

        private static List<V2> WithEntry(List<V2> pts, double HX, double HY, PathCtx c)
        {
            V2 p0 = pts[0], p1 = pts[1];
            double len = Math.Sqrt((p1.x - p0.x) * (p1.x - p0.x) + (p1.y - p0.y) * (p1.y - p0.y));
            if (len == 0) len = 1;
            double tx = (p0.x - p1.x) / len;
            double ty = (p0.y - p1.y) / len;
            int kMax = 1;
            while (kMax < 80 && Math.Abs(p0.x + tx * 0.8 * kMax) < HX + 3 && Math.Abs(p0.y + ty * 0.8 * kMax) < HY + 3) kMax++;
            var all = new List<V2>();
            for (int k = kMax; k >= 1; k--) all.Add(new V2(p0.x + tx * 0.8 * k, p0.y + ty * 0.8 * k));
            all.AddRange(pts);
            return all.Select(p => new V2(0.5 + p.x * c.dx, 0.5 + p.y * c.dy)).ToList();
        }

        private static double SignPow(double v, double e) => Math.Sign(v) * Math.Pow(Math.Abs(v), e);

        /// <summary>A single lap around the shooter, entered along the tangent.</summary>
        public static List<V2> Ring(PathCtx c, RingOpts o)
        {
            double HX = 0.5 / c.dx, HY = 0.5 / c.dy;
            double n = o.n;
            int dir = o.dir;
            double start = o.start * Math.PI / 180;
            double laps = Math.Min(0.9, o.laps);
            double inward = o.inward;
            double wob = o.wobble;
            double aMin = Clear + 0.35, aMax = HX - Edge - wob * HX;
            double bMin = Clear + 0.35, bMax = HY - Edge - wob * HY;
            double a = aMin + (aMax - aMin) * o.wideX;
            double b = bMin + (bMax - bMin) * o.wideY;
            double e = 2 / n;
            const int steps = 120;
            var pts = new List<V2>();
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double th = start + dir * Math.PI * 2 * laps * t;
                double k = 1 - inward * t;
                double w = wob != 0 ? 1 + wob * Math.Sin(o.wobbleFreq * th) : 1;
                double cs = Math.Cos(th), sn = Math.Sin(th);
                pts.Add(new V2(SignPow(cs, e) * a * k * w, SignPow(sn, e) * b * k * w));
            }
            return WithEntry(pts, HX, HY, c);
        }

        /// <summary>Classic inward spiral: the chain enters at the outer rim, winds around the shooter and ends next to it.</summary>
        public static List<V2> Spiral(PathCtx c, SpiralOpts o)
        {
            double HX = 0.5 / c.dx, HY = 0.5 / c.dy;
            double n = o.n;
            int dir = o.dir;
            double start = o.start * Math.PI / 180;
            double aOut = HX - Edge - 0.15, aIn = Clear + 0.65;
            double bOut = HY - Edge - 0.15, bIn = Clear + 0.65;
            double fit = (aOut - aIn) / SpiralPitch;
            double turns = Math.Max(0.6, Math.Min(o.turns, fit));
            double e = 2 / n;
            int steps = (int)Math.Ceiling(160 * turns);
            var pts = new List<V2>();
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double th = start + dir * Math.PI * 2 * turns * t;
                double a = aOut + (aIn - aOut) * t;
                double b = bOut + (bIn - bOut) * t;
                pts.Add(new V2(SignPow(Math.Cos(th), e) * a, SignPow(Math.Sin(th), e) * b));
            }
            return WithEntry(pts, HX, HY, c);
        }

        /// <summary>
        /// Checks a generated track (unit space) against the rules every screen needs: the visible part stays inside the
        /// field, lanes keep their distance, the shooter has room, and the track is long enough to hold a chain.
        /// </summary>
        public static bool Validate(List<V2> control, PathCtx c, double minLength = 20)
        {
            var points = PathData.Smooth(control, 4);
            double HX = 0.5 / c.dx, HY = 0.5 / c.dy;
            var px = new List<double>();
            var py = new List<double>();
            for (int i = 0; i < points.Count; i++)
            {
                double x = (points[i].x - 0.5) / c.dx;
                double y = (points[i].y - 0.5) / c.dy;
                if (i == 0) { px.Add(x); py.Add(y); continue; }
                double lx = px[px.Count - 1], ly = py[py.Count - 1];
                double len = Math.Sqrt((x - lx) * (x - lx) + (y - ly) * (y - ly));
                int n = Math.Max(1, (int)Math.Ceiling(len / 0.25));
                for (int k = 1; k <= n; k++) { px.Add(lx + (x - lx) * k / n); py.Add(ly + (y - ly) * k / n); }
            }
            int cnt = px.Count;
            var cum = new double[cnt];
            for (int i = 1; i < cnt; i++) cum[i] = cum[i - 1] + Math.Sqrt((px[i] - px[i - 1]) * (px[i] - px[i - 1]) + (py[i] - py[i - 1]) * (py[i] - py[i - 1]));
            int first = 0;
            while (first < cnt - 1 && !(Math.Abs(px[first]) < HX && Math.Abs(py[first]) < HY)) first++;
            if (first == 0) return false;
            const double m = 0.88;
            double startArc = cum[first] + 4.2;
            int from = first;
            while (from < cnt - 1 && cum[from] < startArc) from++;
            double visible = cum[cnt - 1] - cum[from];
            if (visible < minLength) return false;
            for (int i = from; i < cnt; i++)
                if (Math.Abs(px[i]) > HX - m || Math.Abs(py[i]) > HY - m) return false;
            for (int i = first; i < cnt; i++)
                if (Math.Sqrt(px[i] * px[i] + py[i] * py[i]) < 2.9) return false;
            const int stride = 2;
            for (int a = first; a < cnt; a += stride)
                for (int b = a + 1; b < cnt; b += stride)
                {
                    if (cum[b] - cum[a] < 5) continue;
                    double dx = px[a] - px[b];
                    if (dx > 1.7 || dx < -1.7) continue;
                    double dy = py[a] - py[b];
                    if (dx * dx + dy * dy < 1.68 * 1.68) return false;
                }
            return true;
        }
    }
}
