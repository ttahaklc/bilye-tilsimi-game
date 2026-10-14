using System;
using System.Collections.Generic;

namespace Bilye.Core
{
    /// <summary>
    /// A path sampled at a uniform arc-length step. Looking up a point at distance s is O(1), which keeps the
    /// per-frame chain placement cheap even with ~100 balls.
    /// </summary>
    public sealed class PathData
    {
        public double step;
        public int count;
        public float[] xs, ys;
        public double length;

        private static V2 CatmullRom(V2 p0, V2 p1, V2 p2, V2 p3, double t)
        {
            double t2 = t * t, t3 = t2 * t;
            return new V2(
                0.5 * (2 * p1.x + (-p0.x + p2.x) * t + (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * t2 + (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * t3),
                0.5 * (2 * p1.y + (-p0.y + p2.y) * t + (2 * p0.y - 5 * p1.y + 4 * p2.y - p3.y) * t2 + (-p0.y + 3 * p1.y - 3 * p2.y + p3.y) * t3));
        }

        /// <summary>Smooth a control polyline with Catmull-Rom splines and return a dense polyline.</summary>
        public static List<V2> Smooth(IList<V2> points, int subdivisions = 12)
        {
            var o = new List<V2>();
            if (points.Count < 3) { o.AddRange(points); return o; }
            for (int i = 0; i < points.Count - 1; i++)
            {
                V2 p0 = points[Math.Max(0, i - 1)];
                V2 p1 = points[i];
                V2 p2 = points[i + 1];
                V2 p3 = points[Math.Min(points.Count - 1, i + 2)];
                for (int k = 0; k < subdivisions; k++) o.Add(CatmullRom(p0, p1, p2, p3, (double)k / subdivisions));
            }
            o.Add(points[points.Count - 1]);
            return o;
        }

        /// <summary>Resample a dense polyline at uniform arc-length spacing.</summary>
        public static PathData Resample(IList<V2> poly, double step)
        {
            var cum = new double[poly.Count];
            for (int i = 1; i < poly.Count; i++)
                cum[i] = cum[i - 1] + Math.Sqrt((poly[i].x - poly[i - 1].x) * (poly[i].x - poly[i - 1].x) + (poly[i].y - poly[i - 1].y) * (poly[i].y - poly[i - 1].y));
            double total = cum[cum.Length - 1];
            int count = Math.Max(2, (int)Math.Floor(total / step) + 1);
            var xs = new float[count];
            var ys = new float[count];
            int seg = 0;
            for (int i = 0; i < count; i++)
            {
                double target = Math.Min(total, i * step);
                while (seg < cum.Length - 2 && cum[seg + 1] < target) seg++;
                double segLen = cum[seg + 1] - cum[seg];
                double f = segLen > 1e-9 ? (target - cum[seg]) / segLen : 0;
                xs[i] = (float)(poly[seg].x + (poly[seg + 1].x - poly[seg].x) * f);
                ys[i] = (float)(poly[seg].y + (poly[seg + 1].y - poly[seg].y) * f);
            }
            return new PathData { step = step, count = count, xs = xs, ys = ys, length = (count - 1) * step };
        }

        /// <summary>Drop the leading part of the path that is far outside the visible area so the chain enters at the edge.</summary>
        public PathData TrimLeadIn(Rect2 bounds, double leadIn)
        {
            bool Inside(int i) => xs[i] > bounds.x && xs[i] < bounds.x + bounds.w && ys[i] > bounds.y && ys[i] < bounds.y + bounds.h;
            int first = 0;
            while (first < count - 2 && !Inside(first)) first++;
            first = Math.Max(0, first - (int)Math.Ceiling(leadIn / step));
            if (first == 0) return this;
            int n = count - first;
            var nx = new float[n];
            var ny = new float[n];
            Array.Copy(xs, first, nx, 0, n);
            Array.Copy(ys, first, ny, 0, n);
            return new PathData { step = step, count = n, xs = nx, ys = ny, length = (n - 1) * step };
        }

        private void TangentAtIndex(int i, out double tx, out double ty)
        {
            int a = Math.Max(0, i - 2);
            int b = Math.Min(count - 1, i + 2);
            double dx = xs[b] - xs[a];
            double dy = ys[b] - ys[a];
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len == 0) len = 1;
            tx = dx / len;
            ty = dy / len;
        }

        public void PointAt(double s, out double x, out double y)
        {
            if (s <= 0)
            {
                // extrapolate backwards along the first tangent so spawning balls queue up off-screen
                TangentAtIndex(0, out double tx0, out double ty0);
                x = xs[0] + tx0 * s;
                y = ys[0] + ty0 * s;
                return;
            }
            if (s >= length)
            {
                int last = count - 1;
                TangentAtIndex(last, out double tx1, out double ty1);
                double over = s - length;
                x = xs[last] + tx1 * over;
                y = ys[last] + ty1 * over;
                return;
            }
            double f = s / step;
            int i = (int)Math.Floor(f);
            double k = f - i;
            x = xs[i] + (xs[i + 1] - xs[i]) * k;
            y = ys[i] + (ys[i + 1] - ys[i]) * k;
        }

        public void TangentAt(double s, out double tx, out double ty)
        {
            int i = Math.Max(0, Math.Min(count - 1, (int)Math.Round(s / step, MidpointRounding.AwayFromZero)));
            TangentAtIndex(i, out tx, out ty);
        }
    }
}
