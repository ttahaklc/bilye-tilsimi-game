using System;
using Bilye.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bilye
{
    public sealed partial class App
    {
        int curLevelId = 1;
        bool curEndless; int curMap;
        bool hintVisible;
        float hintTimer;
        bool resultSaved;

        // HUD
        TextMeshProUGUI hudLevel, hudScore;
        RectTransform hudFill; RectTransform hintRt;
        string lastScoreText = ""; float lastProgress = -1;

        // results
        sealed class Result { public int score, best, maxCombo, stars, unlockedNext; public bool newBest; public double maxDanger; public int speedStage; public double speedMul; }
        Result result;

        // ---------------------------------------------------------------------------------------------
        // starting / leaving a game
        // ---------------------------------------------------------------------------------------------

        void StartLevel(int id)
        {
            curLevelId = Mathf.Clamp(id, 1, Levels.LevelCount); curEndless = false;
            BeginGame(Levels.Get(curLevelId), false, false);
        }

        void StartEndless(int map)
        {
            curMap = Mathf.Clamp(map, 0, 2); curEndless = true; curLevelId = 0;
            BeginGame(Levels.EndlessMapOf(curMap), true, true);
        }

        void BeginGame(LevelConfig cfg, bool endless, bool infinite)
        {
            current = Screen_.Game;
            ClearOverlays();
            result = null; resultSaved = false;
            view.Begin(cfg, endless, infinite, false);
            view.inputEnabled = true;
            view.OnEvent = OnGameEvent;
            view.OnEnded = OnGameEnded;
            hintVisible = true; hintTimer = 0;
            lastScoreText = ""; lastProgress = -1;
            BuildGameUi();
            Audio.I.ResumeMusic();
            if (!Save.Data.tutorialSeen) { pausedByTutorial = true; view.paused = true; tutPage = 0; Push(() => BuildTutorial(true)); }
        }

        void Restart()
        {
            if (curEndless) StartEndless(curMap); else StartLevel(curLevelId);
        }

        void ExitTo(Screen_ target)
        {
            SaveEndlessBest();
            ClearOverlays();
            if (target == Screen_.Menu) ShowMenu(); else ShowLevels();
        }

        void SaveEndlessBest()
        {
            if (!curEndless || view == null || view.game == null || view.demo) return;
            int s = view.game.score;
            if (s > Save.Data.endlessBest[curMap]) { Save.Data.endlessBest[curMap] = s; Save.Commit(); }
        }

        // ---------------------------------------------------------------------------------------------
        // command console (automated checks)
        // ---------------------------------------------------------------------------------------------

        public bool bot;
        float botTimer;

        public string Cmd(string line)
        {
            var p = line.Split(' ');
            int N(int i, int d = 0) { int v; return p.Length > i && int.TryParse(p[i], out v) ? v : d; }
            switch (p[0])
            {
                case "menu": ExitTo(Screen_.Menu); return "menu";
                case "levels": ShowLevels(); return "levels";
                case "settings": ShowSettings(); return "settings";
                case "level": StartLevel(N(1, 1)); return "level " + curLevelId;
                case "endless": StartEndless(N(1)); return "endless " + curMap;
                case "picker": ShowMenu(); Push(BuildEndlessPicker); return "picker";
                case "pause": Pause(); return "pause";
                case "resume": Resume(); return "resume";
                case "unlock": Save.Data.unlocked = Levels.LevelCount; Save.Commit(); return "unlocked";
                case "lang": Loc.Set(N(1)); return "lang " + Loc.Lang;
                case "world": lsWorld = N(1); if (current == Screen_.Levels) BuildLevels(); return "world " + lsWorld;
                case "win": if (view.game != null) view.game.DevWin(); return "devwin";
                case "god": if (view.game != null) view.game.god = N(1) == 1; return "god";
                case "bot": bot = N(1, 1) == 1; return "bot " + bot;
                case "tutorial": tutPage = N(1); Push(() => BuildTutorial(true)); return "tutorial";
                case "tutseen": Save.Data.tutorialSeen = N(1, 1) == 1; Save.Commit(); return "tutseen";
                case "speed": Time.timeScale = N(1, 1); return "timescale " + Time.timeScale;
                case "reset": Save.Reset(); return "reset";
                case "size": SetTestSize(N(1, 780), N(2, 1688), N(3), N(4), N(5), N(6)); return $"size {Display.Width}x{Display.Height}";
                case "real": ClearTestSize(); return "real";
                case "orient": Save.Data.orientation = N(1); Save.Commit(); return "orient";
                case "ctl": Save.Data.control = N(1); Save.Commit(); return "ctl";
                case "dev": Save.Data.devOn = N(1, 1) == 1; Save.Commit(); Rebuild(); return "dev";
                case "fire": if (view.game != null) { view.game.SetAim(-System.Math.PI / 2 + N(1) * Mathf.Deg2Rad); view.game.Fire(); } return "fire";
                case "aimguide": if (view.aim != null) { view.aim.active = N(1, 1) == 1; if (view.aim.active && view.game != null) view.game.SetAim(-System.Math.PI / 2 + N(2) * Mathf.Deg2Rad); } return "aimguide";
            }
            return "unknown: " + line;
        }

        void BotUpdate()
        {
            if (!bot || current != Screen_.Game || view.game == null || view.paused) return;
            var g = view.game;
            if (g.phase != GamePhase.Playing) return;
            botTimer -= Time.unscaledDeltaTime;
            if (botTimer > 0) return;
            botTimer = 0.28f;
            // aim at the nearest ball of the loaded colour whose ray really hits a ball of that colour
            var c = g.layout.center;
            double bestA = 0; double bestD = double.MaxValue; bool found = false;
            for (int pass = 0; pass < 2 && !found; pass++)
            {
                int col = pass == 0 ? g.loaded : g.next;
                for (int i = 0; i < g.balls.Count; i++)
                {
                    var b = g.balls[i];
                    if (b.color != col || !g.layout.clip.Contains(b.x, b.y)) continue;
                    double a = System.Math.Atan2(b.y - c.y, b.x - c.x);
                    int hit = g.RayHit(a, out double dist);
                    if (hit >= 0 && g.balls[hit].color == col && dist < bestD) { bestD = dist; bestA = a; found = true; if (pass == 1) g.Swap(); }
                }
            }
            if (!found) { bestA = System.Math.Atan2(g.balls.Count > 0 ? g.balls[g.balls.Count / 2].y - c.y : -1, g.balls.Count > 0 ? g.balls[g.balls.Count / 2].x - c.x : 0); }
            g.SetAim(bestA);
            g.Fire();
        }

        // ---------------------------------------------------------------------------------------------
        // game events
        // ---------------------------------------------------------------------------------------------

        void OnGameEvent(GameEvent e)
        {
            var a = Audio.I;
            switch (e.type)
            {
                case GameEventType.Shoot: a.Play("shoot"); hintVisible = false; break;
                case GameEventType.Swap: a.Play("swap"); Audio.Vibrate(Haptic.Select); break;
                case GameEventType.Hit: a.Play("hit"); Audio.Vibrate(Haptic.Light); break;
                case GameEventType.Match: a.Play("match"); Audio.Vibrate(e.count >= 5 ? Haptic.Heavy : Haptic.Medium); break;
                case GameEventType.Combo: a.Combo(e.combo); break;
                case GameEventType.Danger: a.Play("danger"); break;
                case GameEventType.Stage: a.Combo(3); Audio.Vibrate(Haptic.Medium); break;
                case GameEventType.Win: a.Play("win"); Audio.Vibrate(Haptic.Success); break;
                case GameEventType.Lose: a.Play("lose"); Audio.Vibrate(Haptic.Error); break;
            }
        }

        void OnGameEnded(bool won)
        {
            var g = view.game;
            if (resultSaved) return;
            resultSaved = true;
            var r = new Result { score = g.score, maxCombo = g.maxCombo, stars = g.Stars, maxDanger = g.maxDanger, speedMul = g.speedMul };
            if (curEndless)
            {
                int prev = Save.Data.endlessBest[curMap];
                r.best = Mathf.Max(prev, g.score); r.newBest = g.score > prev;
                SaveEndlessBest();
            }
            else if (won)
            {
                int prev = Save.Data.best[curLevelId];
                int unlockedBefore = Save.Data.unlocked;
                Save.Complete(curLevelId, g.score, r.stars);
                r.best = Mathf.Max(prev, g.score); r.newBest = g.score > prev;
                r.unlockedNext = Save.Data.unlocked > unlockedBefore ? Save.Data.unlocked : 0;
            }
            else r.best = Save.Data.best[curLevelId];
            result = r;
            Push(won && !curEndless ? (Action)BuildWinPanel : BuildLosePanel);
            if (won && !curEndless) MaybeAskReview();
        }

        // Apple throttles the system dialog itself; we only ask at two happy moments (after 6 and 35 cleared levels).
        void MaybeAskReview()
        {
            int done = 0; for (int i = 1; i <= Levels.LevelCount; i++) if (Save.IsCompleted(i)) done++;
            int bit = done == 6 ? 1 : done == 35 ? 2 : 0;
            if (bit == 0 || (Save.Data.ratingAsked & bit) != 0) return;
            Save.Data.ratingAsked |= bit; Save.Commit();
            Invoke(nameof(RequestReview), 2.2f);
        }

        void RequestReview()
        {
#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.RequestStoreReview();
#endif
        }

        // ---------------------------------------------------------------------------------------------
        // HUD
        // ---------------------------------------------------------------------------------------------

        void BuildGameUi()
        {
            Clear(screenRoot);
            SetDim(0f);
            var g = view.game; if (g == null) return;
            bool land = Ui.Landscape;
            float hudH = land ? 40 : 44;
            float top = Mathf.Round((float)(g.layout.hudBand - hudH) / 2f);

            var row = Ui.HBox(screenRoot, 8, 0, TextAnchor.MiddleCenter, true, "hud");
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(12, -(top + hudH)); rt.offsetMax = new Vector2(-12, -top);

            // level / speed pill
            {
                var pill = Ui.Img(row.transform, Gfx.Frame("chip"), Color.white, 1.7f, "lvl"); Ui.Le(pill.gameObject, w: 70, h: hudH);
                var v = Ui.VBox(pill.transform, 0, 4, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)v.transform);
                var c = Caption(v.transform, curEndless ? Loc.Raw("speed") : Loc.Raw("levelLabel"), TextAlignmentOptions.Center, Pal.TextFaint, 9.5f); Ui.Le(c.gameObject, h: 12);
                hudLevel = Ui.Text(v.transform, curEndless ? "X1.0" : curLevelId.ToString(), land ? 16 : 18, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 0, true, false);
                Ui.Le(hudLevel.gameObject, h: 22);
            }
            // progress bar
            {
                var holder = Ui.Rt("barholder", row.transform); Ui.Le(holder.gameObject, fw: 1, h: hudH);
                var track = Ui.Img(holder, Gfx.Frame("white"), new Color(1, 1, 1, 0.12f), 6f, "track");
                Ui.Anchor(track.rectTransform, 0, 0.5f, 1, 0.5f, 0, -3, 0, -3);
                track.rectTransform.sizeDelta = new Vector2(0, 6);
                var fill = Ui.Img(track.transform, Gfx.Frame("gold"), Color.white, 6f, "fill");
                hudFill = fill.rectTransform;
                hudFill.anchorMin = new Vector2(0, 0); hudFill.anchorMax = new Vector2(0.02f, 1); hudFill.offsetMin = hudFill.offsetMax = Vector2.zero;
            }
            // score pill
            {
                float sw = 98;
                var pill = Ui.Img(row.transform, Gfx.Frame("chip"), Color.white, 1.7f, "score"); Ui.Le(pill.gameObject, w: sw, h: hudH);
                var v = Ui.VBox(pill.transform, 0, 4, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)v.transform);
                var c = Caption(v.transform, Loc.Raw("score"), TextAlignmentOptions.Center, Pal.TextFaint, 9.5f); Ui.Le(c.gameObject, h: 12);
                hudScore = Ui.Text(v.transform, "0", land ? 15 : 16, Pal.Text, FontKind.Display, TextAlignmentOptions.Center, 0.5f, true, false);
                Ui.Le(hudScore.gameObject, h: 22);
            }
            Ui.IconButton(row.transform, "pause", hudH, Pause, 0.3f);

            // hint
            {
                var hint = Ui.Img(screenRoot, Gfx.Frame("chip"), Color.white, 1.7f, "hint");
                hintRt = hint.rectTransform;
                Ui.Pin(hintRt, 0.5f, 0, Mathf.Min(420, Ui.SafeW - 40), 58, 0, 16);
                var v = Ui.VBox(hint.transform, 1, 8, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)v.transform);
                var t1 = Ui.Text(v.transform, Loc.T(Save.Data.control == 0 ? "hintDial" : "hintAim"), 13, Pal.Text, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true); Ui.Le(t1.gameObject, h: 18);
                var t2 = Ui.Text(v.transform, Loc.T("hintSwap"), 11.5f, Pal.TextDim, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true); Ui.Le(t2.gameObject, h: 16);
                var cg = hint.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false;
            }
        }

        void UpdateGameUi()
        {
            if (current != Screen_.Game || view == null || view.game == null || hudScore == null) return;
            var g = view.game;
            string s = Fmt(g.score);
            if (s != lastScoreText) { lastScoreText = s; hudScore.text = s; }
            if (curEndless) { string sp = "X" + g.speedMul.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); if (hudLevel.text != sp) hudLevel.text = sp; }
            float p = Mathf.Max(0.02f, (float)g.Progress);
            if (Mathf.Abs(p - lastProgress) > 0.002f) { lastProgress = p; hudFill.anchorMax = new Vector2(p, 1); }
            if (hintRt != null)
            {
                hintTimer += Time.unscaledDeltaTime;
                var cg = hintRt.GetComponent<CanvasGroup>();
                float target = hintVisible && hintTimer > 0.6f && hintTimer < 9f && !view.paused ? 1f : 0f;
                cg.alpha = Mathf.MoveTowards(cg.alpha, target, Time.unscaledDeltaTime * 3f);
            }
        }

        // ---------------------------------------------------------------------------------------------
        // pause / panels
        // ---------------------------------------------------------------------------------------------

        string Subtitle()
        {
            if (curEndless) return $"{Loc.Raw("endless")}  ·  {Loc.MapName(curMap)}";
            var lv = Levels.Get(curLevelId);
            return $"{Loc.Raw("levelLabel")} {curLevelId}  ·  {Loc.WorldName(lv.world)}";
        }

        void Pause()
        {
            if (view.game == null || view.game.phase != GamePhase.Playing || view.paused) return;
            SaveEndlessBest();
            view.paused = true;
            Push(BuildPausePanel);
        }

        void Resume() { overlays.Clear(); Clear(overlayRoot); view.paused = false; Audio.I.ResumeMusic(); }

        void BuildPausePanel()
        {
            var ov = Ui.Overlay(overlayRoot, 0.72f);
            float w = Mathf.Min(380, Ui.SafeW - 32);
            var panel = Ui.Panel(ov);
            bool dev = Save.Data.devOn && !curEndless;
            Ui.Pin(panel, 0.5f, 0.5f, w, (dev ? 410 : 322) + (Save.Data.devOn ? 56 : 0));
            var v = Ui.VBox(panel, 10, 20, TextAnchor.UpperCenter); Ui.Fill((RectTransform)v.transform);
            var t = Ui.Text(v.transform, Ui.Upper(Loc.Raw("pause")), 26, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 4); Ui.GoldGradient(t); Ui.Le(t.gameObject, h: 34);
            Ui.Ornament(v.transform, 70);
            var sub = Caption(v.transform, Subtitle(), TextAlignmentOptions.Center, Pal.TextDim, 11.5f); Ui.Le(sub.gameObject, h: 18);
            Ui.Spacer(v.transform, 2);
            Ui.Button(v.transform, Loc.Raw("resume"), BtnKind.Gold, 60, Resume, -1, 20);
            Ui.Button(v.transform, Loc.Raw("restart"), BtnKind.Glass, 46, () => Push(() => BuildConfirm(Loc.Raw("confirmRestartTitle"), Loc.Raw("confirmRestart"), () => { Restart(); })));
            var row = Ui.HBox(v.transform, 10, 0, TextAnchor.MiddleCenter); Ui.Le(row.gameObject, h: 46);
            Ui.Button(row.transform, Loc.Raw("settings"), BtnKind.Glass, 46, () => Push(BuildPauseSettings));
            Ui.Button(row.transform, Loc.Raw("mainMenu"), BtnKind.Glass, 46, () => Push(() => BuildConfirm(Loc.Raw("confirmLeaveTitle"), Loc.Raw("confirmLeaveLevel"), () => ExitTo(Screen_.Menu))));
            if (Save.Data.devOn)
            {
                if (!curEndless) Ui.Button(v.transform, "DEV · Bölümü bitir", BtnKind.Glass, 40, () => { view.game.DevWin(); Resume(); }, -1, 13, false);
                Ui.Button(v.transform, $"DEV · Ölümsüz: {(Save.Data.devGod ? "açık" : "kapalı")}", BtnKind.Glass, 40, () =>
                {
                    Save.Data.devGod = !Save.Data.devGod; Save.Commit(); view.game.god = Save.Data.devGod; RebuildOverlayLayers();
                }, -1, 13, false);
            }
        }

        void BuildPauseSettings()
        {
            var ov = Ui.Overlay(overlayRoot, 0.8f);
            float w = Mathf.Min(380, Ui.SafeW - 32);
            var panel = Ui.Panel(ov);
            Ui.Pin(panel, 0.5f, 0.5f, w, Mathf.Min(Ui.SafeH - 24, 520));
            var v = Ui.VBox(panel, 10, 18, TextAnchor.UpperCenter); Ui.Fill((RectTransform)v.transform);
            var t = Ui.Text(v.transform, Ui.Upper(Loc.Raw("settings")), 22, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 3); Ui.GoldGradient(t); Ui.Le(t.gameObject, h: 30);
            Ui.Ornament(v.transform, 60);
            var sr = Ui.Scroll(v.transform, out var content);
            Ui.Le(sr.gameObject, fh: 1, fw: 1);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false; vlg.spacing = 14;
            SettingsBody(content, false);
            Ui.Button(v.transform, Loc.Raw("back"), BtnKind.Gold, 48, () => Pop());
        }

        // ---------------------------------------------------------------------------------------------
        // win / lose
        // ---------------------------------------------------------------------------------------------

        void ScoreBlock(Transform parent, string title, int score, (string, string)[] meta)
        {
            var box = Ui.Img(parent, Gfx.Frame("tile"), Color.white, 1.8f, "score");
            var v = Ui.VBox(box.transform, 2, 12, TextAnchor.UpperCenter); Ui.Fill((RectTransform)v.transform);
            Ui.Le(box.gameObject, h: 118);
            var c = Caption(v.transform, title, TextAlignmentOptions.Center, Pal.TextFaint, 10.5f); Ui.Le(c.gameObject, h: 14);
            var big = Ui.Text(v.transform, Fmt(score), 38, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 1, true, false); Ui.GoldGradient(big); Ui.Le(big.gameObject, h: 46);
            var row = Ui.HBox(v.transform, 10, 0, TextAnchor.MiddleCenter); Ui.Le(row.gameObject, h: 34);
            for (int i = 0; i < meta.Length; i++)
            {
                if (i > 0) VSep(row.transform, 24);
                var m = Ui.VBox(row.transform, 0, 0, TextAnchor.MiddleCenter); Ui.Le(m.gameObject, fw: 1);
                var l = Caption(m.transform, meta[i].Item1, TextAlignmentOptions.Center, Pal.TextFaint, 9.5f); Ui.Le(l.gameObject, h: 12);
                var val = Ui.Text(m.transform, meta[i].Item2, 15, Pal.Text, FontKind.Display, TextAlignmentOptions.Center, 0, true, false); Ui.Le(val.gameObject, h: 20);
            }
        }

        void BuildWinPanel()
        {
            var r = result; if (r == null) return;
            var ov = Ui.Overlay(overlayRoot, 0.74f);
            bool two = Ui.Landscape || Ui.SafeH < 560;
            float w = two ? Mathf.Min(640, Ui.SafeW - 32) : Mathf.Min(380, Ui.SafeW - 32);
            var panel = Ui.Panel(ov);
            bool last = curLevelId >= Levels.LevelCount;
            float h = two ? Mathf.Min(Ui.SafeH - 20, 340) : Mathf.Min(Ui.SafeH - 20, 700);
            Ui.Pin(panel, 0.5f, 0.5f, w, h);
            var outer = Ui.VBox(panel, 10, 18, TextAnchor.UpperCenter); Ui.Fill((RectTransform)outer.transform);

            var scroll = Ui.Scroll(outer.transform, out var content); Ui.Le(scroll.gameObject, fh: 1, fw: 1);
            RectTransform left, right;
            if (two)
            {
                var hb = Ui.HBox(content, 20, 0, TextAnchor.UpperCenter, false);
                hb.childForceExpandHeight = false;
                var cs = hb.gameObject.AddComponent<ContentSizeFitter>(); cs.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var lv = Ui.VBox(hb.transform, 8, 0, TextAnchor.UpperCenter); Ui.Le(lv.gameObject, fw: 1.15f);
                var rv = Ui.VBox(hb.transform, 8, 0, TextAnchor.UpperCenter); Ui.Le(rv.gameObject, fw: 1);
                left = (RectTransform)lv.transform; right = (RectTransform)rv.transform;
                var cl = content.gameObject.AddComponent<VerticalLayoutGroup>(); cl.childControlWidth = true; cl.childControlHeight = true; cl.childForceExpandWidth = true; cl.childForceExpandHeight = false;
                hb.transform.SetParent(content, false);
            }
            else
            {
                var vb = Ui.VBox(content, 14, 0, TextAnchor.UpperCenter);
                left = right = (RectTransform)vb.transform;
                var cl = content.gameObject.AddComponent<VerticalLayoutGroup>(); cl.childControlWidth = true; cl.childControlHeight = true; cl.childForceExpandWidth = true; cl.childForceExpandHeight = false;
            }
            var t = Ui.Text(left, Ui.Upper(Loc.Raw("levelComplete")), 24, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 2); Ui.GoldGradient(t); Ui.Le(t.gameObject, h: 32);
            var sub = Caption(left, Subtitle(), TextAlignmentOptions.Center, Pal.TextDim, 11); Ui.Le(sub.gameObject, h: 16);
            var st = Ui.Stars(left, r.stars, 28, 10); Ui.Le(st.gameObject, h: 30);
            ScoreBlock(left, Loc.Raw("score"), r.score, new[] { (Loc.Raw("best"), Fmt(r.best)), (Loc.Raw("combo"), "X" + Mathf.Max(1, r.maxCombo)) });
            if (r.newBest) { var nb = Ui.Text(left, Loc.T("newBest"), 13, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 2); Ui.Le(nb.gameObject, h: 18); }
            if (r.unlockedNext > 0) { var un = Ui.Text(left, $"{Loc.Shape(Loc.Raw("newLevelUnlocked"))}: {r.unlockedNext}", 12.5f, Pal.Good, FontKind.Bold, TextAlignmentOptions.Center, 0.3f, true, false); Ui.Le(un.gameObject, h: 18); }
            if (last) { var ad = Ui.Text(left, Loc.T("allDone"), 12.5f, Pal.Gold, FontKind.Bold, TextAlignmentOptions.Center, 0.6f, true, true, true); Ui.Le(ad.gameObject, h: 34); }
            BuildStarReport(right, r.maxDanger, r.stars, two ? (w - 36 - 20) * 0.46f : w - 36);

            var actions = Ui.VBox(outer.transform, 10, 0, TextAnchor.UpperCenter);
            if (!last) Ui.Button(actions.transform, Loc.Raw("nextLevel"), BtnKind.Gold, two ? 50 : 58, () => StartLevel(curLevelId + 1), -1, 20);
            else Ui.Button(actions.transform, Loc.Raw("levels"), BtnKind.Gold, two ? 50 : 58, () => ExitTo(Screen_.Levels), -1, 20);
            var row = Ui.HBox(actions.transform, 10, 0, TextAnchor.MiddleCenter); Ui.Le(row.gameObject, h: 44);
            Ui.Button(row.transform, Loc.Raw("replay"), BtnKind.Glass, 44, Restart);
            Ui.Button(row.transform, Loc.Raw("mainMenu"), BtnKind.Glass, 44, () => ExitTo(Screen_.Menu));
        }

        void BuildLosePanel()
        {
            var r = result; if (r == null) return;
            var g = view.game;
            var ov = Ui.Overlay(overlayRoot, 0.74f);
            float w = Mathf.Min(380, Ui.SafeW - 32);
            var panel = Ui.Panel(ov, "panel");
            Ui.Pin(panel, 0.5f, 0.5f, w, Mathf.Min(Ui.SafeH - 20, 440));
            var v = Ui.VBox(panel, 12, 20, TextAnchor.UpperCenter); Ui.Fill((RectTransform)v.transform);
            var t = Ui.Text(v.transform, Ui.Upper(Loc.Raw("gameOver")), 26, Pal.DangerSoft, FontKind.Display, TextAlignmentOptions.Center, 3); Ui.Le(t.gameObject, h: 34);
            var sub = Caption(v.transform, Subtitle(), TextAlignmentOptions.Center, Pal.TextDim, 11); Ui.Le(sub.gameObject, h: 16);
            if (curEndless)
                ScoreBlock(v.transform, Loc.Raw("score"), r.score, new[] { (Loc.Raw("best"), Fmt(r.best)), (Loc.Raw("speed"), "X" + r.speedMul.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)) });
            else
                ScoreBlock(v.transform, Loc.Raw("score"), r.score, new[] { (Loc.Raw("best"), Fmt(r.best)), (Loc.Raw("progress"), "%" + Mathf.RoundToInt((float)g.Progress * 100)) });
            if (curEndless && r.newBest) { var nb = Ui.Text(v.transform, Loc.T("newBest"), 13, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 2); Ui.Le(nb.gameObject, h: 18); }
            var sp = Ui.Spacer(v.transform, 0); Ui.Le(sp.gameObject, fh: 1);
            Ui.Button(v.transform, Loc.Raw("retry"), BtnKind.Gold, 58, Restart, -1, 20);
            var row = Ui.HBox(v.transform, 10, 0, TextAnchor.MiddleCenter); Ui.Le(row.gameObject, h: 44);
            if (!curEndless) Ui.Button(row.transform, Loc.Raw("chooseLevel"), BtnKind.Glass, 44, () => ExitTo(Screen_.Levels));
            Ui.Button(row.transform, Loc.Raw("mainMenu"), BtnKind.Glass, 44, () => ExitTo(Screen_.Menu));
        }

        // ---------------------------------------------------------------------------------------------
        // star report ("why these stars?")
        // ---------------------------------------------------------------------------------------------

        void BuildStarReport(Transform parent, double maxDanger, int stars, float width)
        {
            int p3 = Mathf.RoundToInt((float)Tuning.Star3 * 100), p2 = Mathf.RoundToInt((float)Tuning.Star2 * 100);
            int reached = Mathf.Clamp(Mathf.RoundToInt((float)maxDanger * 100), 0, 100);
            var v = Ui.VBox(parent, 8, 0, TextAnchor.UpperCenter); Ui.Le(v.gameObject, w: width);
            var cap = Caption(v.transform, Loc.Raw("starsWhy"), Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, Pal.TextFaint, 10.5f); Ui.Le(cap.gameObject, h: 14);

            var barH = Ui.Rt("bar", v.transform); Ui.Le(barH.gameObject, h: 30);
            var bar = Ui.HBox(barH, 0, 0, TextAnchor.MiddleCenter, false); var brt = (RectTransform)bar.transform;
            Ui.Anchor(brt, 0, 0.5f, 1, 0.5f, 0, -5, 0, -5); brt.sizeDelta = new Vector2(0, 10);
            bar.spacing = 0; bar.childForceExpandHeight = true;
            void Seg(float flex, Color c) { var s = Ui.Img(bar.transform, Gfx.Pixel, c); Ui.Le(s.gameObject, fw: flex); }
            Seg(p3, new Color(0.345f, 0.84f, 0.63f, 0.85f)); Seg(p2 - p3, new Color(0.957f, 0.718f, 0.25f, 0.85f)); Seg(100 - p2, new Color(0.9f, 0.34f, 0.42f, 0.9f));
            var marker = Ui.Img(barH, Gfx.Frame("gold"), Color.white, 6f, "marker"); Ui.Pin(marker.rectTransform, 0, 0.5f, 12, 24);
            var anim = marker.gameObject.AddComponent<Anim>();
            float target = Mathf.Min(1f, (float)maxDanger) * width;
            anim.tick = t => { float k = Mathf.Clamp01((t - 0.5f) / 0.9f); k = 1 - Mathf.Pow(1 - k, 3); marker.rectTransform.anchoredPosition = new Vector2(k * target, 0); };

            var lab = Ui.HBox(v.transform, 0, 0, TextAnchor.MiddleCenter); Ui.Le(lab.gameObject, h: 14);
            var s1 = Caption(lab.transform, Loc.Raw("starSafe"), TextAlignmentOptions.Left, Pal.TextFaint, 9.5f); Ui.Le(s1.gameObject, fw: 1);
            var s2 = Caption(lab.transform, Loc.Raw("starHole"), TextAlignmentOptions.Right, Pal.TextFaint, 9.5f); Ui.Le(s2.gameObject, fw: 1);

            string[] rows = { Loc.Raw("starRowDone"), Loc.Raw("starRowLine", "p", p2), Loc.Raw("starRowLine", "p", p3) };
            for (int i = 0; i < 3; i++)
            {
                bool earned = stars >= i + 1;
                var hb = Ui.HBox(v.transform, 8, 0, TextAnchor.MiddleLeft, false); Ui.Le(hb.gameObject, h: 22);
                hb.childForceExpandWidth = false;
                var gems = Ui.HBox(hb.transform, 1, 0, TextAnchor.MiddleLeft, false); Ui.Le(gems.gameObject, w: 45, h: 16); gems.childForceExpandWidth = false;
                for (int g = 0; g <= i; g++) { var im = Ui.Img(gems.transform, Gfx.Icon("star"), earned ? Pal.Hex("#f5cf62") : new Color(1, 1, 1, 0.16f)); Ui.Le(im.gameObject, w: 14, h: 14); }
                var tx = Ui.Text(hb.transform, Loc.Shape(rows[i]), 12, earned ? Pal.Text : Pal.TextFaint, earned ? FontKind.Semi : FontKind.Semi, Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, 0, true, false);
                tx.isRightToLeftText = Loc.Rtl;
                Ui.Le(tx.gameObject, fw: 1, h: 20);
            }
            var rc = Ui.Text(v.transform, Loc.Shape(Loc.Raw("starReach", "p", reached)), 11.5f, Pal.TextDim, FontKind.Semi, Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, 0, true, false, true);
            rc.isRightToLeftText = Loc.Rtl; Ui.Le(rc.gameObject, h: 32);
            string adv = stars >= 3 ? Loc.Raw("starPerfect") : Loc.Raw("starAdvice", "n", stars + 1, "p", stars == 2 ? p3 : p2);
            var ad = Ui.Text(v.transform, Loc.Shape(adv), 12, stars >= 3 ? Pal.Good : Pal.Gold, FontKind.Semi, Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, 0, true, false, true);
            ad.isRightToLeftText = Loc.Rtl; Ui.Le(ad.gameObject, h: 34);
        }
    }
}
