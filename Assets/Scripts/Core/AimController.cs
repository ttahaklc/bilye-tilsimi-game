using System;

namespace Bilye.Core
{
    /// <summary>
    /// How the player aims and fires. Two schemes, chosen in the settings:
    ///  - Dial ("Kadran")  put a finger anywhere and turn it around the shooter like a dial. Release fires.
    ///  - Aim  ("Direkt Nişan")  the shooter points at the finger, release fires.
    /// In both a tap on the shooter itself swaps the loaded and the next ball.
    /// </summary>
    public enum ControlMode { Dial, Aim }

    public enum TouchOutcome { None, Fire, Swap }

    public sealed class AimController
    {
        public ControlMode mode;
        /// <summary>true while a finger is down and steering (drives the aim guide)</summary>
        public bool active;
        private bool onShooter, moved;
        private double sx, sy, finger0, aim0;
        private readonly Game g;
        private const double TapSlop = 12;

        public AimController(Game game, ControlMode mode = ControlMode.Aim) { g = game; this.mode = mode; }

        private static double Wrap(double a)
        {
            while (a > Math.PI) a -= Math.PI * 2;
            while (a < -Math.PI) a += Math.PI * 2;
            return a;
        }

        private double AngleOf(double x, double y) { var c = g.layout.center; return Math.Atan2(y - c.y, x - c.x); }

        private bool OnTurret(double x, double y)
        {
            var c = g.layout.center;
            return Math.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y)) < g.d * 1.4;
        }

        public TouchOutcome Down(double x, double y)
        {
            active = true; moved = false; sx = x; sy = y;
            onShooter = OnTurret(x, y);
            if (onShooter) return TouchOutcome.None;
            if (mode == ControlMode.Dial) { finger0 = AngleOf(x, y); aim0 = g.aimAngle; return TouchOutcome.None; }
            g.AimAt(x, y);
            return TouchOutcome.None;
        }

        public void Move(double x, double y)
        {
            if (!active) return;
            if (Math.Sqrt((x - sx) * (x - sx) + (y - sy) * (y - sy)) > TapSlop) moved = true;
            if (onShooter && !moved) return;
            if (mode == ControlMode.Dial)
            {
                var c = g.layout.center;
                if (Math.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y)) < g.d * 0.9) return;
                g.SetAim(aim0 + Wrap(AngleOf(x, y) - finger0));
            }
            else g.AimAt(x, y);
        }

        public TouchOutcome Up(double x, double y)
        {
            if (!active) return TouchOutcome.None;
            active = false;
            if (onShooter && !moved) return g.Swap() ? TouchOutcome.Swap : TouchOutcome.None;
            Move(x, y);
            return g.Fire() ? TouchOutcome.Fire : TouchOutcome.None;
        }

        public void Cancel() { active = false; }
    }
}
