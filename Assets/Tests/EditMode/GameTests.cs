using System.Collections.Generic;
using System.Text;
using Bilye.Core;
using NUnit.Framework;

namespace Bilye.Tests
{
    public class GeometryTests
    {
        [Test]
        public void EveryTrackFitsEveryScreen()
        {
            var all = new List<LevelConfig>(Levels.All);
            all.AddRange(Levels.EndlessMaps);
            all.Add(Levels.Menu);
            var sb = new StringBuilder();
            int bad = 0;
            foreach (var lv in all)
                foreach (var v in TestUtil.Views)
                {
                    var issues = TestUtil.Audit(lv, v);
                    if (issues.Count > 0) { bad++; sb.AppendLine($"L{lv.id} {lv.name} @ {v.name}: {string.Join("; ", issues)}"); }
                }
            Assert.AreEqual(0, bad, sb.ToString());
        }

        [Test]
        public void HundredLevelsTenWorldsAndTracksDiffer()
        {
            Assert.AreEqual(100, Levels.All.Length);
            var v = TestUtil.Views[2];
            var sigs = new HashSet<string>();
            foreach (var lv in Levels.All)
            {
                var layout = Layout.Compute(v.w, v.h, v.ins, lv.ballScale);
                var g = new Game(new GameOptions { level = lv, layout = layout, seed = 1 });
                sigs.Add($"{g.path.count}|{g.path.xs[g.path.count / 2]:0.0}|{g.path.ys[g.path.count / 3]:0.0}");
            }
            Assert.GreaterOrEqual(sigs.Count, 85, "too many copy-pasted tracks");
        }

        [Test]
        public void DifficultyOnlyGrows()
        {
            for (int i = 2; i <= 100; i++)
            {
                var a = Levels.All[i - 2]; var b = Levels.All[i - 1];
                Assert.GreaterOrEqual(b.ballCount, a.ballCount);
                Assert.GreaterOrEqual(b.speed, a.speed);
                Assert.LessOrEqual(b.ballScale, a.ballScale);
                Assert.GreaterOrEqual(b.colors, a.colors);
            }
        }
    }

    public class GameplayTests
    {
        [Test]
        public void Level1CanBeWonByBot()
        {
            var ev = new List<GameEvent>();
            var g = TestUtil.PlayBot(Levels.All[0], TestUtil.Views[2], 7, 400, ev);
            Assert.AreEqual(GamePhase.Won, g.phase, $"balls left {g.balls.Count + g.queue.Count}");
            Assert.IsTrue(ev.Exists(e => e.type == GameEventType.Shoot));
            Assert.IsTrue(ev.Exists(e => e.type == GameEventType.Hit));
            Assert.IsTrue(ev.Exists(e => e.type == GameEventType.Match));
            Assert.IsTrue(ev.Exists(e => e.type == GameEventType.Win));
            Assert.Greater(g.score, 0);
        }

        [Test]
        public void DoingNothingLoses()
        {
            var layout = Layout.Compute(393, 852, new Insets(59, 34, 0, 0), Levels.All[0].ballScale);
            var ev = new List<GameEvent>();
            var g = new Game(new GameOptions { level = Levels.All[0], layout = layout, seed = 3, onEvent = e => ev.Add(e) });
            double t = 0;
            while (g.phase != GamePhase.Lost && t < 300) { g.Update(1.0 / 60); TestUtil.CheckInvariants(g, "idle"); t += 1.0 / 60; }
            Assert.AreEqual(GamePhase.Lost, g.phase);
            Assert.IsTrue(ev.Exists(e => e.type == GameEventType.Lose));
            Assert.IsFalse(ev.Exists(e => e.type == GameEventType.Win));
        }

        [Test]
        public void AllLevelsRun25SecondsWithoutBreakingInvariants()
        {
            for (int i = 0; i < Levels.All.Length; i++)
            {
                var g = TestUtil.PlayBot(Levels.All[i], TestUtil.Views[(i % 3) + 1], 11 + i, 25);
                Assert.IsFalse(double.IsNaN(g.score));
            }
        }

        [Test]
        public void EarlyLevelsAreWinnable()
        {
            foreach (int idx in new[] { 0, 1, 2 })
            {
                var g = TestUtil.PlayBot(Levels.All[idx], TestUtil.Views[1], 21 + idx, 400);
                Assert.AreEqual(GamePhase.Won, g.phase, $"level {idx + 1}");
            }
        }

        [Test]
        public void EndlessMapsKeepPouringAndSpeedUp()
        {
            for (int m = 0; m < Levels.EndlessMapCount; m++)
            {
                var ev = new List<GameEvent>();
                var g = TestUtil.PlayBot(Levels.EndlessMaps[m], TestUtil.Views[2], 5 + m, 90, ev, true);
                Assert.AreNotEqual(GamePhase.Won, g.phase);
                Assert.IsFalse(ev.Exists(e => e.type == GameEventType.Win));
                Assert.Greater(g.score, 0);
            }
        }

        [Test]
        public void StarsFollowTheDangerLine()
        {
            Assert.Less(Tuning.Star3, Tuning.Star2);
            var g = new Game(new GameOptions { level = Levels.All[0], layout = Layout.Compute(393, 852, new Insets(59, 34, 0, 0), 1), seed = 1 });
            g.maxDanger = 0.3; Assert.AreEqual(3, g.Stars);
            g.maxDanger = 0.7; Assert.AreEqual(2, g.Stars);
            g.maxDanger = 0.95; Assert.AreEqual(1, g.Stars);
        }

        [Test]
        public void GodModeAndDevWin()
        {
            var layout = Layout.Compute(393, 852, new Insets(59, 34, 0, 0), 1);
            var ev = new List<GameEvent>();
            var g = new Game(new GameOptions { level = Levels.All[0], layout = layout, seed = 3, onEvent = e => ev.Add(e) });
            g.god = true;
            for (int i = 0; i < 60 * 120; i++) g.Update(1.0 / 60);
            Assert.IsFalse(ev.Exists(e => e.type == GameEventType.Lose));
            g.DevWin();
            for (int i = 0; i < 60 * 5; i++) g.Update(1.0 / 60);
            Assert.IsTrue(ev.Exists(e => e.type == GameEventType.Win));
        }

        [Test]
        public void DialControlTurnsRelativelyAndTapSwaps()
        {
            var layout = Layout.Compute(393, 852, new Insets(59, 34, 0, 0), 1);
            var g = new Game(new GameOptions { level = Levels.All[0], layout = layout, seed = 1 });
            var ctl = new AimController(g, ControlMode.Dial);
            int l0 = g.loaded, n0 = g.next;
            var c = g.layout.center;
            Assert.AreEqual(TouchOutcome.None, ctl.Down(c.x + 2, c.y + 2));
            Assert.AreEqual(TouchOutcome.Swap, ctl.Up(c.x + 2, c.y + 2));
            Assert.AreEqual(n0, g.loaded); Assert.AreEqual(l0, g.next);
            ctl.Down(c.x, c.y + 200);
            ctl.Move(c.x + 200, c.y);
            Assert.AreEqual(TouchOutcome.Fire, ctl.Up(c.x + 200, c.y));
            Assert.AreEqual(1, g.projectiles.Count);
        }
    }
}
