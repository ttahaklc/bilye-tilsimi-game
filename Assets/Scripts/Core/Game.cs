using System;
using System.Collections.Generic;

namespace Bilye.Core
{
    public sealed class Ball
    {
        public int id;
        /// <summary>distance along the path in points (centre of the ball). The rear of the chain has the smallest s.</summary>
        public double s;
        public int color;
        public double x, y, tx = 1, ty;
        /// <summary>visual-only offsets that decay to 0 (smooth insertion / push animations)</summary>
        public double offS, offX, offY;
        /// <summary>0..1 scale-in animation for freshly inserted balls</summary>
        public double appear = 1;
    }

    public sealed class Projectile
    {
        public double x, y, vx, vy;
        public int color;
        public double age;
    }

    public enum GamePhase { Playing, Winning, Won, Losing, Lost }

    public enum GameEventType { Shoot, Swap, Hit, Match, Combo, Danger, Stage, Win, Lose }

    public struct GameEvent
    {
        public GameEventType type;
        public int count, combo, stage;
        public double speed, points;
        public GameEvent(GameEventType t) { type = t; count = 0; combo = 0; stage = 0; speed = 0; points = 0; }
    }

    public sealed class GameOptions
    {
        public LevelConfig level;
        public Layout layout;
        public long? seed;
        /// <summary>Menu backdrop: balls keep pouring, the chain never reaches the hole.</summary>
        public bool endless;
        /// <summary>Endless ("Sınırsız") mode: balls never run out, every stage makes the chain faster and matches worth more.</summary>
        public bool infinite;
        public Action<GameEvent> onEvent;
    }

    /// <summary>Tunable gameplay constants. Everything is expressed in ball diameters so it scales with the screen.</summary>
    public static class Tuning
    {
        public const double RushMultiplier = 5.5;
        public const double LosingMultiplier = 18;
        public const double ProjectileSpeed = 40; // d / s
        public const double HitDistance = 0.9; // x d
        public const double FireCooldown = 0.19;
        public const double ComboWindow = 2.2;
        public const double PathStep = 2;
        public const double LeadIn = 2.2;
        public const double StageBase = 3000;
        public const double StageGrowth = 900;
        public const double StageSpeedGain = 0.06;
        public const double StageSpeedMax = 2.2;
        public const double StagePointsGain = 0.2;
        public const double StagePointsMax = 6;
        public const int StageColorEvery = 3;
        /// <summary>star rating: the chain must stay below this share of the way to the hole (0..1) for 3 / 2 stars</summary>
        public const double Star3 = 0.55;
        public const double Star2 = 0.82;
        public const double MaxComboMultiplier = 8;
    }

    /// <summary>The whole simulation. Pure C#: no Unity types, so it can be unit-tested and bot-played in edit mode.</summary>
    public sealed class Game
    {
        public LevelConfig level;
        public Layout layout;
        public PathData path;
        public double d = 30;
        public double speedPx = 20;
        /// <summary>distance along the path at which the chain falls into the hole</summary>
        public double targetS;

        public readonly List<Ball> balls = new List<Ball>();
        /// <summary>colours still waiting to be poured into the chain (the next ball is at the end)</summary>
        public List<int> queue = new List<int>();
        public int totalBalls;
        private int rushCount;

        public readonly List<Projectile> projectiles = new List<Projectile>();

        public int loaded, next;
        public double cooldown;
        /// <summary>0..1 reload animation</summary>
        public double reload = 1;
        public double aimAngle = -Math.PI / 2;
        public double turretAngle = -Math.PI / 2;
        public double recoil;

        public int score, combo, maxCombo, shots, matches;
        public double comboTimer;

        public GamePhase phase = GamePhase.Playing;
        public double time;
        private double endTimer;
        private bool gapOpen, dangerActive;
        /// <summary>0..1 how close the leader is to the hole</summary>
        public double danger;
        /// <summary>the closest the chain ever got to the hole (0..1): drives the star rating</summary>
        public double maxDanger;
        /// <summary>developer: the chain never reaches the hole</summary>
        public bool god;

        private readonly Rng rng;
        private int ballId = 1;
        private readonly Action<GameEvent> onEvent;
        private readonly bool endless;
        public readonly bool infinite;
        public int stage;
        public double speedMul = 1;

        // visual hooks (the view layer turns them into particles / popups)
        public Action<double, double, int, int> onBurst;
        public Action<double, double, int, double> onRing;
        public Action<string, double, double, uint, double> onPopup;

        private const double EpsFrac = 0.02;
        private const double MaxSubstep = 1.0 / 90;
        private static readonly uint[] PaletteText = { 0xff6b6b, 0xffd24a, 0x5be584, 0x6aa5ff, 0xc08bff, 0x4fe8dc };

        public Game(GameOptions opts)
        {
            level = opts.level;
            layout = opts.layout;
            onEvent = opts.onEvent;
            endless = opts.endless;
            infinite = opts.infinite;
            rng = opts.seed.HasValue ? new Rng(opts.seed.Value) : Rng.Random();
            BuildPath(opts.layout);
            queue = GenerateQueue();
            totalBalls = queue.Count;
            rushCount = Math.Max(6, (int)Math.Floor(totalBalls * level.rushFraction + 0.5));
            loaded = PickColor();
            next = PickColor();
            RefreshPositions();
        }

        // ---------------------------------------------------------------------------------------------
        // setup
        // ---------------------------------------------------------------------------------------------

        private void BuildPath(Layout lay)
        {
            layout = lay;
            d = lay.d;
            speedPx = level.speed * d;
            lay.UnitSpans(out double sx, out double sy);
            var control = level.path(new PathCtx(d / sx, d / sy));
            var px = new List<V2>(control.Count);
            foreach (var u in control) px.Add(lay.UnitToPx(u.x, u.y));
            var dense = PathData.Smooth(px, 4);
            var raw = PathData.Resample(dense, Tuning.PathStep);
            path = raw.TrimLeadIn(lay.clip, d * Tuning.LeadIn);
            targetS = Math.Max(d * 4, path.length - level.holeOffset * d);
        }

        private List<int> GenerateQueue()
        {
            int colors = level.colors;
            var seq = new List<int>();
            for (int i = 0; i < level.ballCount; i++)
            {
                int c;
                bool hasPrev = i >= 1;
                int prev = hasPrev ? seq[i - 1] : -1;
                int prev2 = i >= 2 ? seq[i - 2] : -2;
                if (hasPrev && rng.Next() < level.runBias) c = prev;
                else c = (int)Math.Floor(rng.Next() * colors);
                // never start with a ready-made triple
                if (hasPrev && prev == prev2 && c == prev) c = (c + 1 + (int)Math.Floor(rng.Next() * (colors - 1))) % colors;
                seq.Add(c);
            }
            seq.Reverse(); // the queue is consumed from the end
            return seq;
        }

        /// <summary>Re-layout after a rotation / window change. Keeps every ball at the same relative place.</summary>
        public void Resize(Layout lay)
        {
            double oldD = d;
            BuildPath(lay);
            double k = d / oldD;
            foreach (var b in balls) { b.s *= k; b.offS = 0; b.offX = 0; b.offY = 0; }
            // never let a rotation instantly lose the game
            if (balls.Count > 0 && phase == GamePhase.Playing)
            {
                var leader = balls[balls.Count - 1];
                if (leader.s >= targetS)
                {
                    double shift = leader.s - (targetS - d * 0.5);
                    foreach (var b in balls) b.s -= shift;
                }
            }
            projectiles.Clear();
            RefreshPositions();
        }

        // ---------------------------------------------------------------------------------------------
        // player input
        // ---------------------------------------------------------------------------------------------

        public void AimAt(double x, double y)
        {
            var c = layout.center;
            if (Math.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y)) < 4) return;
            aimAngle = Math.Atan2(y - c.y, x - c.x);
        }

        /// <summary>Aim straight at an angle (radians) - used by the rotary control.</summary>
        public void SetAim(double angle) { aimAngle = Math.Atan2(Math.Sin(angle), Math.Cos(angle)); }

        /// <summary>Returns true when a ball was fired.</summary>
        public bool Fire()
        {
            if (phase != GamePhase.Playing || cooldown > 0) return false;
            double a = aimAngle;
            double dx = Math.Cos(a), dy = Math.Sin(a);
            var c = layout.center;
            double muzzle = d * 1.35;
            projectiles.Add(new Projectile
            {
                x = c.x + dx * muzzle, y = c.y + dy * muzzle,
                vx = dx * d * Tuning.ProjectileSpeed, vy = dy * d * Tuning.ProjectileSpeed,
                color = loaded, age = 0,
            });
            loaded = next;
            next = PickColor();
            cooldown = Tuning.FireCooldown;
            reload = 0;
            recoil = 1;
            turretAngle = a;
            shots++;
            Emit(new GameEvent(GameEventType.Shoot));
            return true;
        }

        public bool Swap()
        {
            if (phase != GamePhase.Playing) return false;
            int t = loaded;
            loaded = next;
            next = t;
            reload = 0.55;
            Emit(new GameEvent(GameEventType.Swap));
            return true;
        }

        // ---------------------------------------------------------------------------------------------
        // simulation
        // ---------------------------------------------------------------------------------------------

        /// <summary>Advance the world by dt seconds. Frame-rate independent: long frames are sub-stepped.</summary>
        public void Update(double dt)
        {
            if (!(dt > 0)) return;
            dt = Math.Min(dt, 0.1);
            int steps = Math.Max(1, (int)Math.Ceiling(dt / MaxSubstep));
            double h = dt / steps;
            for (int i = 0; i < steps; i++) Step(h);
        }

        private void Step(double h)
        {
            time += h;
            cooldown = Math.Max(0, cooldown - h);
            reload = Math.Min(1, reload + h * 6);
            recoil = Math.Max(0, recoil - h * 9);

            double diff = aimAngle - turretAngle;
            while (diff > Math.PI) diff -= Math.PI * 2;
            while (diff < -Math.PI) diff += Math.PI * 2;
            turretAngle += diff * Math.Min(1, h * 28);

            if (phase == GamePhase.Playing || phase == GamePhase.Losing) UpdateChain(h);
            if (phase == GamePhase.Playing)
            {
                UpdateProjectiles(h);
                comboTimer -= h;
                if (gapOpen) comboTimer = Math.Max(comboTimer, 0.6);
                if (comboTimer <= 0) combo = 0;
            }
            UpdateVisuals(h);
            UpdatePhase(h);
        }

        /// <summary>Developer: finish the level right now (clears the chain and the queue, the normal win flow follows).</summary>
        public void DevWin()
        {
            if (infinite) return;
            queue.Clear();
            balls.Clear();
        }

        private void UpdatePhase(double h)
        {
            if (phase == GamePhase.Playing)
            {
                if (!infinite && queue.Count == 0 && balls.Count == 0)
                {
                    phase = GamePhase.Winning;
                    endTimer = 0.9;
                    score += 500;
                    Popup("+500", layout.center.x, layout.center.y - d * 2.5, 0xffe27a, 1.5);
                }
            }
            else if (phase == GamePhase.Winning)
            {
                endTimer -= h;
                if (endTimer <= 0)
                {
                    phase = GamePhase.Won;
                    Emit(new GameEvent(GameEventType.Win));
                }
            }
            else if (phase == GamePhase.Losing)
            {
                endTimer -= h;
                if (endTimer <= 0 || balls.Count == 0)
                {
                    phase = GamePhase.Lost;
                    Emit(new GameEvent(GameEventType.Lose));
                }
            }
        }

        private void UpdateChain(double h)
        {
            double gapTol = d * 1e-5;
            if (phase == GamePhase.Playing) Spawn();
            int n = balls.Count;
            if (n == 0) { gapOpen = false; return; }

            bool losing = phase == GamePhase.Losing;
            bool rushing = !losing && queue.Count > totalBalls - rushCount;
            double v = speedPx * speedMul * (losing ? Tuning.LosingMultiplier : rushing ? Tuning.RushMultiplier : 1);

            int gapIdx = -1;
            for (int i = 0; i < n - 1; i++)
            {
                if (balls[i + 1].s - balls[i].s > d + gapTol) { gapIdx = i; break; }
            }

            if (gapIdx < 0 || losing)
            {
                gapOpen = false;
                double move = v * h;
                for (int i = 0; i < n; i++) balls[i].s += move;
            }
            else
            {
                gapOpen = true;
                double gap = balls[gapIdx + 1].s - balls[gapIdx].s - d;
                double move = Math.Max(v, speedPx * level.catchUp) * h;
                double pull = 0;
                if (gap < d * 3.2 && WouldMatch(gapIdx)) pull = level.magnet * d * h;
                double closing = move + pull;
                bool closed = false;
                if (closing >= gap)
                {
                    double k = gap / closing;
                    move *= k;
                    pull *= k;
                    closed = true;
                }
                for (int i = 0; i <= gapIdx; i++) balls[i].s += move;
                if (pull > 0) for (int i = gapIdx + 1; i < n; i++) balls[i].s -= pull;
                if (closed)
                {
                    balls[gapIdx + 1].s = balls[gapIdx].s + d;
                    for (int i = gapIdx + 2; i < n; i++)
                    {
                        if (balls[i].s - balls[i - 1].s < d + gapTol) balls[i].s = Math.Max(balls[i].s, balls[i - 1].s + d);
                        else break;
                    }
                    RefreshPositions();
                    OnJoin(gapIdx);
                }
            }

            if (phase == GamePhase.Playing)
            {
                if (balls.Count > 0)
                {
                    var leader = balls[balls.Count - 1];
                    danger = Math.Max(0, Math.Min(1, leader.s / targetS));
                    if (danger > maxDanger) maxDanger = danger;
                    if (leader.s >= targetS && !god)
                    {
                        if (endless) PopBall(balls.Count - 1);
                        else { phase = GamePhase.Losing; endTimer = 1.5; }
                    }
                    if (!dangerActive && danger > 0.85)
                    {
                        dangerActive = true;
                        Emit(new GameEvent(GameEventType.Danger));
                    }
                    else if (dangerActive && danger < 0.7) dangerActive = false;
                }
            }
            else if (losing)
            {
                while (balls.Count > 0 && balls[balls.Count - 1].s > path.length + d * 0.4) PopBall(balls.Count - 1, true);
            }

            RefreshPositions();
        }

        private void Spawn()
        {
            int guard = 0;
            while (queue.Count > 0 && guard++ < 8)
            {
                Ball rear = balls.Count > 0 ? balls[0] : null;
                if (rear != null && rear.s < 0) break;
                double s = rear != null ? Math.Min(rear.s - d, 0) : 0;
                int color = queue[queue.Count - 1];
                queue.RemoveAt(queue.Count - 1);
                balls.Insert(0, MakeBall(color, s));
            }
            if ((endless || infinite) && queue.Count == 0)
            {
                int colors = infinite ? CurrentColors : level.colors;
                int prev = balls.Count > 0 ? balls[0].color : -1;
                for (int i = 0; i < 10; i++)
                {
                    double bias = infinite ? Math.Max(0.12, level.runBias - 0.02 * stage) : level.runBias;
                    int c = prev >= 0 && rng.Next() < bias ? prev : (int)Math.Floor(rng.Next() * colors);
                    queue.Add(c);
                    prev = c;
                }
                totalBalls += 10;
            }
        }

        private Ball MakeBall(int color, double s) => new Ball { id = ballId++, s = s, color = color };

        /// <summary>Refresh cached world positions of all balls.</summary>
        public void RefreshPositions()
        {
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                path.PointAt(b.s, out b.x, out b.y);
                path.TangentAt(b.s, out b.tx, out b.ty);
            }
        }

        private bool Linked(int i) => balls[i + 1].s - balls[i].s < d * (1 + EpsFrac);

        private void RunAround(int i, out int lo, out int hi)
        {
            int c = balls[i].color;
            lo = i; hi = i;
            while (lo > 0 && balls[lo - 1].color == c && Linked(lo - 1)) lo--;
            while (hi < balls.Count - 1 && balls[hi + 1].color == c && Linked(hi)) hi++;
        }

        /// <summary>Would joining balls[gapIdx] and balls[gapIdx+1] create a run of 3+?</summary>
        private bool WouldMatch(int gapIdx)
        {
            var a = balls[gapIdx];
            var b = balls[gapIdx + 1];
            if (a.color != b.color) return false;
            int left = 1;
            int i = gapIdx;
            while (i > 0 && balls[i - 1].color == a.color && Linked(i - 1)) { left++; i--; }
            int right = 1;
            i = gapIdx + 1;
            while (i < balls.Count - 1 && balls[i + 1].color == a.color && Linked(i)) { right++; i++; }
            return left + right >= 3;
        }

        /// <summary>Two parts of the chain just touched: if the colours line up it is a chain reaction.</summary>
        private void OnJoin(int gapIdx)
        {
            if (gapIdx < 0 || gapIdx + 1 >= balls.Count) return;
            var a = balls[gapIdx];
            var b = balls[gapIdx + 1];
            if (a.color != b.color) return;
            RunAround(gapIdx, out int lo, out int hi);
            if (hi - lo + 1 >= 3) RemoveRun(lo, hi);
        }

        private void UpdateProjectiles(double h)
        {
            var clip = layout.clip;
            for (int pi = projectiles.Count - 1; pi >= 0; pi--)
            {
                var p = projectiles[pi];
                p.x += p.vx * h;
                p.y += p.vy * h;
                p.age += h;
                if (p.x < clip.x - d * 2 || p.x > clip.x + clip.w + d * 2 || p.y < clip.y - d * 2 || p.y > clip.y + clip.h + d * 2)
                {
                    projectiles.RemoveAt(pi);
                    continue;
                }
                int hit = FindHit(p);
                if (hit >= 0)
                {
                    projectiles.RemoveAt(pi);
                    InsertBall(hit, p);
                }
            }
        }

        private int FindHit(Projectile p)
        {
            double lim = d * Tuning.HitDistance;
            double bestD = lim * lim;
            var clip = layout.clip;
            int best = -1;
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                if (!clip.Contains(b.x, b.y)) continue;
                double dx = b.x - p.x, dy = b.y - p.y;
                double dd = dx * dx + dy * dy;
                if (dd < bestD) { bestD = dd; best = i; }
            }
            return best;
        }

        private void InsertBall(int k, Projectile p)
        {
            var hitBall = balls[k];
            bool ahead = (p.x - hitBall.x) * hitBall.tx + (p.y - hitBall.y) * hitBall.ty > 0;
            int idx = ahead ? k + 1 : k;
            double s = ahead ? hitBall.s + d : hitBall.s;

            var nb = MakeBall(p.color, s);
            path.PointAt(s, out nb.x, out nb.y);
            nb.offX = p.x - nb.x;
            nb.offY = p.y - nb.y;
            nb.appear = 0;
            balls.Insert(idx, nb);

            for (int j = idx + 1; j < balls.Count; j++)
            {
                double need = balls[j - 1].s + d;
                if (balls[j].s < need - 1e-6)
                {
                    balls[j].offS -= need - balls[j].s;
                    balls[j].s = need;
                }
                else break;
            }
            RefreshPositions();
            Emit(new GameEvent(GameEventType.Hit));
            onRing?.Invoke(nb.x, nb.y, p.color, d * 0.9);

            RunAround(idx, out int lo, out int hi);
            if (hi - lo + 1 >= 3) RemoveRun(lo, hi);
            else if (!gapOpen) comboTimer = Math.Min(comboTimer, 0.35);
        }

        private void RemoveRun(int lo, int hi)
        {
            int count = hi - lo + 1;
            combo = comboTimer > 0 && combo > 0 ? combo + 1 : 1;
            comboTimer = Tuning.ComboWindow;
            maxCombo = Math.Max(maxCombo, combo);
            matches++;

            double baseScore = 100 + (count - 3) * 60 + (count >= 5 ? 100 : 0);
            int points = (int)Math.Floor(baseScore * Math.Min(combo, Tuning.MaxComboMultiplier) * PointsMul + 0.5);
            score += points;
            if (infinite) UpdateStage();

            double cx = 0, cy = 0;
            for (int i = lo; i <= hi; i++)
            {
                var b = balls[i];
                cx += b.x; cy += b.y;
                onBurst?.Invoke(b.x + b.offX, b.y + b.offY, b.color, 10);
                onRing?.Invoke(b.x, b.y, b.color, d * 1.1);
            }
            cx /= count; cy /= count;
            uint color = PaletteText[balls[lo].color % PaletteText.Length];
            Popup("+" + points, cx, cy - d * 0.6, color, 1 + Math.Min(0.6, (count - 3) * 0.15));
            if (combo >= 2) Popup("x" + combo, cx, cy - d * 1.7, 0xffffff, 1.2 + Math.Min(1, combo * 0.12));
            balls.RemoveRange(lo, count);
            RefreshPositions();
            var ev = new GameEvent(GameEventType.Match) { count = count, combo = combo };
            Emit(ev);
            if (combo >= 2) Emit(new GameEvent(GameEventType.Combo) { combo = combo });
        }

        private void PopBall(int i, bool swallow = false)
        {
            if (i < 0 || i >= balls.Count) return;
            var b = balls[i];
            onBurst?.Invoke(b.x, b.y, b.color, swallow ? 4 : 8);
            balls.RemoveAt(i);
        }

        /// <summary>Pick a colour the player can actually use: one that is currently on the field.</summary>
        public int PickColor()
        {
            var present = new List<int>();
            var clip = layout.clip;
            foreach (var b in balls)
            {
                if (!clip.Contains(b.x, b.y)) continue;
                if (!present.Contains(b.color)) present.Add(b.color);
            }
            if (present.Count == 0)
            {
                foreach (int c in queue) if (!present.Contains(c)) present.Add(c);
            }
            if (present.Count == 0) return (int)Math.Floor(rng.Next() * CurrentColors);
            return present[(int)Math.Floor(rng.Next() * present.Count)];
        }

        // ---------------------------------------------------------------------------------------------
        // visuals
        // ---------------------------------------------------------------------------------------------

        private void UpdateVisuals(double h)
        {
            double decay = Math.Exp(-h * 15);
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                if (b.offS != 0 || b.offX != 0 || b.offY != 0)
                {
                    b.offS *= decay; b.offX *= decay; b.offY *= decay;
                    if (Math.Abs(b.offS) < 0.05) b.offS = 0;
                    if (Math.Abs(b.offX) < 0.05) b.offX = 0;
                    if (Math.Abs(b.offY) < 0.05) b.offY = 0;
                }
                if (b.appear < 1) b.appear = Math.Min(1, b.appear + h * 9);
            }
        }

        private void Popup(string text, double x, double y, uint color, double scale) => onPopup?.Invoke(text, x, y, color, scale);

        private void Emit(GameEvent e) => onEvent?.Invoke(e);

        // ---------------------------------------------------------------------------------------------
        // read-only helpers for the UI
        // ---------------------------------------------------------------------------------------------

        /// <summary>1..3 stars for a finished level: the further the chain stayed from the hole, the better</summary>
        public int Stars => maxDanger < Tuning.Star3 ? 3 : maxDanger < Tuning.Star2 ? 2 : 1;

        /// <summary>number of colours in play (grows with the stage in endless mode)</summary>
        public int CurrentColors => !infinite ? level.colors : Math.Min(6, level.colors + stage / Tuning.StageColorEvery);

        /// <summary>score at which endless stage n begins</summary>
        public static double StageStart(int n) => Tuning.StageBase * n + Tuning.StageGrowth * n * (n - 1) / 2;

        public double PointsMul => !infinite ? 1 : Math.Min(Tuning.StagePointsMax, 1 + Tuning.StagePointsGain * stage);

        private void UpdateStage()
        {
            int st = stage;
            while (score >= StageStart(st + 1)) st++;
            if (st == stage) return;
            stage = st;
            speedMul = Math.Min(Tuning.StageSpeedMax, 1 + Tuning.StageSpeedGain * st);
            Popup("x" + speedMul.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), layout.center.x, layout.center.y - d * 3.2, 0xf6d98a, 1.5);
            Emit(new GameEvent(GameEventType.Stage) { stage = st, speed = speedMul, points = PointsMul });
        }

        /// <summary>fraction of the level's balls already cleared (endless: progress toward the next speed-up), 0..1</summary>
        public double Progress
        {
            get
            {
                if (infinite)
                {
                    double a = StageStart(stage), b = StageStart(stage + 1);
                    return Math.Max(0, Math.Min(1, (score - a) / (b - a)));
                }
                if (endless || totalBalls == 0) return 0;
                return Math.Max(0, Math.Min(1, 1 - (double)(queue.Count + balls.Count) / totalBalls));
            }
        }

        /// <summary>Position of the portal at the end of the track.</summary>
        public V2 HolePosition() { path.PointAt(path.length, out double x, out double y); return new V2(x, y); }

        /// <summary>First ball hit by a ray from the muzzle: index into balls (or -1) and the travel distance.</summary>
        public int RayHit(double angle, out double distance)
        {
            var c = layout.center;
            var clip = layout.clip;
            double dx = Math.Cos(angle), dy = Math.Sin(angle);
            double lim = d * Tuning.HitDistance;
            double best = double.PositiveInfinity;
            int index = -1;
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                if (!clip.Contains(b.x, b.y)) continue;
                double rx = b.x - c.x, ry = b.y - c.y;
                double along = rx * dx + ry * dy;
                if (along <= 0) continue;
                double perp = Math.Abs(rx * dy - ry * dx);
                if (perp > lim) continue;
                double t = along - Math.Sqrt(Math.Max(0, lim * lim - perp * perp));
                if (t < best) { best = t; index = i; }
            }
            distance = best;
            return index;
        }
    }
}
