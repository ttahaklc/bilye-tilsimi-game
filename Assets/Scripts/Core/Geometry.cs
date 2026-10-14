using System;

namespace Bilye.Core
{
    public struct V2
    {
        public double x, y;
        public V2(double x, double y) { this.x = x; this.y = y; }
        public override string ToString() => $"({x:0.##}, {y:0.##})";
    }

    public struct Rect2
    {
        public double x, y, w, h;
        public Rect2(double x, double y, double w, double h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public bool Contains(double px, double py) => px >= x && px <= x + w && py >= y && py <= y + h;
    }

    public struct Insets
    {
        public double top, bottom, left, right;
        public Insets(double top, double bottom, double left, double right) { this.top = top; this.bottom = bottom; this.left = left; this.right = right; }
    }

    /// <summary>
    /// Everything the game needs to know about the screen. No fixed pixel values anywhere else: the play field,
    /// the ball size, the shooter position and the path all derive from this. Units are "points" (a phone's short
    /// side is ~390 points); the view layer maps points to world units.
    /// </summary>
    public sealed class Layout
    {
        public double width, height;
        public Insets insets;
        public bool landscape;
        public double hudBand;
        public Rect2 field;
        public Rect2 clip;
        public V2 center;
        /// <summary>ball diameter in points</summary>
        public double d;

        public const double MinBall = 26;
        public const double MinBallSmall = 22;
        public const double MaxBall = 68;
        public const double BallFraction = 0.106;

        public static Layout Compute(double width, double height, Insets insets, double ballScale)
        {
            bool landscape = width > height;
            double hudBand = landscape ? 46 : 68;
            const double pad = 10;
            double left = insets.left + pad;
            double right = width - insets.right - pad;
            double top = insets.top + hudBand;
            double bottom = height - insets.bottom - pad;
            var field = new Rect2(left, top, Math.Max(100, right - left), Math.Max(100, bottom - top));
            double shortSide = Math.Min(field.w, field.h);
            double floor = ballScale < 0.75 ? MinBallSmall : MinBall;
            double d = Math.Max(floor, Math.Min(MaxBall, shortSide * BallFraction * ballScale));
            double infl = d * 0.55;
            var clip = new Rect2(field.x - infl, field.y - infl, field.w + infl * 2, field.h + infl * 2);
            return new Layout
            {
                width = width, height = height, insets = insets, landscape = landscape, hudBand = hudBand,
                field = field, clip = clip, center = new V2(field.x + field.w / 2, field.y + field.h / 2), d = d,
            };
        }

        /// <summary>
        /// Paths are designed in "unit space": x runs along the SHORT side of the field, y along the LONG side. In
        /// landscape the design is rotated by 90 degrees, so a level keeps the same lane spacing in both orientations.
        /// </summary>
        public V2 UnitToPx(double ux, double uy)
        {
            if (landscape) return new V2(field.x + uy * field.w, field.y + ux * field.h);
            return new V2(field.x + ux * field.w, field.y + uy * field.h);
        }

        /// <summary>Length of the unit x / unit y axes in points (unit x always runs along the short side).</summary>
        public void UnitSpans(out double sx, out double sy)
        {
            if (landscape) { sx = field.h; sy = field.w; }
            else { sx = field.w; sy = field.h; }
        }
    }
}
