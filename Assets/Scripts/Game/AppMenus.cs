using System;
using System.Collections.Generic;
using Bilye.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bilye
{
    public sealed partial class App
    {
        // ---------------------------------------------------------------------------------------------
        // shared bits
        // ---------------------------------------------------------------------------------------------

        static string Fmt(long n) => n.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));

        static TextMeshProUGUI Caption(Transform p, string text, TextAlignmentOptions al = TextAlignmentOptions.Center, Color? c = null, float size = 10.5f)
        {
            var t = Ui.Text(p, Ui.Upper(text), size, c ?? Pal.TextFaint, FontKind.Bold, al, size * 0.14f);
            return t;
        }

        static RectTransform Stat(Transform p, string label, string value, Color? color = null, float flex = 1)
        {
            var v = Ui.VBox(p, 2, 0, TextAnchor.MiddleCenter);
            Ui.Le(v.gameObject, fw: flex);
            var cap = Caption(v.transform, label); Ui.Le(cap.gameObject, h: 14);
            var val = Ui.Text(v.transform, value, 17, color ?? Pal.Text, FontKind.Display, TextAlignmentOptions.Center, 0, true, false);
            Ui.Le(val.gameObject, h: 22);
            return (RectTransform)v.transform;
        }

        static void VSep(Transform p, float h = 28)
        {
            var s = Ui.Img(p, Gfx.Pixel, new Color(1, 1, 1, 0.1f), 1, "sep"); Ui.Le(s.gameObject, w: 1, h: h, fw: 0);
        }

        RectTransform Header(Transform parent, string title, Action onBack, string right = null)
        {
            var h = Ui.HBox(parent, 12, 0, TextAnchor.MiddleCenter);
            Ui.Le(h.gameObject, h: 48);
            Ui.IconButton(h.transform, "back", 44, onBack, 0.38f);
            var t = Ui.Text(h.transform, Ui.Upper(title), 24, Pal.Gold, FontKind.Display, Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, 2.5f);
            Ui.GoldGradient(t); Ui.Le(t.gameObject, fw: 1, h: 40);
            if (right != null)
            {
                var c = Caption(h.transform, right, TextAlignmentOptions.Right); Ui.Le(c.gameObject, w: 120, h: 30);
            }
            return (RectTransform)h.transform;
        }

        // ---------------------------------------------------------------------------------------------
        // main menu
        // ---------------------------------------------------------------------------------------------

        void BuildMenu()
        {
            Clear(screenRoot);
            SetDim(0.42f);
            bool land = Ui.Landscape;
            float sw = Ui.SafeW, sh = Ui.SafeH;
            var root = Ui.Rt("menu", screenRoot); Ui.Fill(root);

            int next = Mathf.Min(Save.Data.unlocked, Levels.LevelCount);
            bool allDone = Save.TotalStars() > 0 && Save.IsCompleted(Levels.LevelCount);
            float colW = land ? Mathf.Min(330, sw * 0.4f) : Mathf.Min(360, sw - 48);
            bool small = sh < 640 && !land;

            // ---- logo
            RectTransform logo;
            {
                var v = Ui.VBox(null == root ? screenRoot : root, small ? 6 : 10, 0, TextAnchor.MiddleCenter, true, "logo");
                logo = (RectTransform)v.transform;
                var dots = Ui.HBox(logo, 7, 0, TextAnchor.MiddleCenter, false, "dots"); Ui.Le(dots.gameObject, h: 11);
                dots.childForceExpandWidth = false;
                for (int i = 0; i < 6; i++)
                {
                    var d = Ui.Img(dots.transform, Gfx.Dot, Pal.Balls[i].baseC); Ui.Le(d.gameObject, w: 10, h: 10);
                }
                var top = Ui.Text(logo, Loc.BrandTop, land ? 54 : 64, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 7, true, false);
                Ui.GoldGradient(top); Ui.Le(top.gameObject, h: land ? 62 : 74);
                var bot = Ui.Text(logo, Loc.BrandBottom, land ? 24 : 28, Pal.Text, FontKind.Display, TextAlignmentOptions.Center, 14, true, false);
                Ui.Le(bot.gameObject, h: land ? 30 : 34);
                Ui.Ornament(logo, 150);
                var tag = Ui.Text(logo, Loc.T("tagline"), 13, Pal.TextDim, FontKind.Semi, TextAlignmentOptions.Center, 0.4f, true, true, true);
                Ui.Le(tag.gameObject, h: 36);
            }

            // ---- buttons
            RectTransform buttons;
            {
                var v = Ui.VBox(root, 12, 0, TextAnchor.LowerCenter, true, "buttons");
                buttons = (RectTransform)v.transform;
                Ui.Button(buttons, Loc.Raw("play"), BtnKind.Gold, 64, () => StartLevel(allDone ? 1 : next), -1, 22);
                Ui.Button(buttons, Loc.Raw("endless"), BtnKind.Glass, 52, () => Push(BuildEndlessPicker));
                var row = Ui.HBox(buttons, 12, 0, TextAnchor.MiddleCenter); Ui.Le(row.gameObject, h: 52);
                Ui.Button(row.transform, Loc.Raw("levels"), BtnKind.Glass, 52, ShowLevels);
                Ui.Button(row.transform, Loc.Raw("settings"), BtnKind.Glass, 52, ShowSettings);
                var strip = Ui.Img(buttons, Gfx.Frame("chip"), Color.white, 2f, "stats");
                Ui.Le(strip.gameObject, h: 56);
                var hb = Ui.HBox(strip.transform, 8, 10, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)hb.transform);
                int eb = Mathf.Max(Save.Data.endlessBest[0], Mathf.Max(Save.Data.endlessBest[1], Save.Data.endlessBest[2]));
                Stat(hb.transform, Loc.Raw("levelLabel"), (allDone ? Levels.LevelCount : next).ToString(), Pal.Gold);
                VSep(hb.transform);
                Stat(hb.transform, Loc.Raw("stars"), $"{Save.TotalStars()}/{Levels.LevelCount * 3}", null, 1.3f);
                VSep(hb.transform);
                Stat(hb.transform, Loc.Raw("endlessName"), eb > 0 ? Fmt(eb) : "-", null, 1.3f);
            }

            if (land)
            {
                var hb = Ui.HBox(root, 16, 14, TextAnchor.MiddleCenter, false, "row");
                Ui.Fill((RectTransform)hb.transform);
                logo.SetParent(hb.transform, false); buttons.SetParent(hb.transform, false);
                Ui.Le(logo.gameObject, w: Mathf.Min(380, sw * 0.42f), fw: 1);
                Ui.Le(buttons.gameObject, w: colW, fw: 1);
                ((VerticalLayoutGroup)buttons.GetComponent<VerticalLayoutGroup>()).childAlignment = TextAnchor.MiddleCenter;
            }
            else
            {
                var vb = Ui.VBox(root, 0, 0, TextAnchor.UpperCenter, true, "col");
                Ui.Fill((RectTransform)vb.transform);
                vb.padding = new RectOffset(24, 24, small ? 18 : 36, 24);
                logo.SetParent(vb.transform, false); buttons.SetParent(vb.transform, false);
                Ui.Le(logo.gameObject, h: small ? 190 : 230);
                var sp = Ui.Spacer(vb.transform, 0); Ui.Le(sp.gameObject, fh: 1);
                Ui.Le(buttons.gameObject, w: colW, h: 64 + 52 + 52 + 56 + 12 * 3);
                vb.childAlignment = TextAnchor.UpperCenter;
            }
        }

        // ---------------------------------------------------------------------------------------------
        // endless picker
        // ---------------------------------------------------------------------------------------------

        void BuildEndlessPicker()
        {
            var ov = Ui.Overlay(overlayRoot, 0.72f);
            Ui.Clickable(ov.gameObject, () => Pop(), true);
            bool land = Ui.Landscape;
            float w = Mathf.Min(land ? 640 : 380, Ui.SafeW - 32);
            var panel = Ui.Panel(ov);
            Ui.Pin(panel, 0.5f, 0.5f, w, land ? 250 : 470);
            var v = Ui.VBox(panel, 12, 18, TextAnchor.UpperCenter); Ui.Fill((RectTransform)v.transform);
            var title = Ui.Text(v.transform, Ui.Upper(Loc.Raw("endless")), 24, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 3);
            Ui.GoldGradient(title); Ui.Le(title.gameObject, h: 34);
            var sub = Ui.Text(v.transform, Loc.T("mapPick"), 13, Pal.TextDim, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true); Ui.Le(sub.gameObject, h: 22);
            RectTransform cards;
            if (land) { var h = Ui.HBox(v.transform, 12, 0, TextAnchor.MiddleCenter); cards = (RectTransform)h.transform; Ui.Le(cards.gameObject, fh: 1); }
            else { var vv = Ui.VBox(v.transform, 10, 0, TextAnchor.UpperCenter); cards = (RectTransform)vv.transform; Ui.Le(cards.gameObject, fh: 1); }
            for (int i = 0; i < 3; i++)
            {
                int map = i;
                var card = Ui.Img(cards, Gfx.Frame("tile"), Color.white, 1.8f, "card", true);
                Ui.Le(card.gameObject, h: land ? -1 : 100, fw: 1, fh: land ? 1 : -1);
                var p = card.gameObject.AddComponent<Ui.PressFx>(); p.onClick = () => { ClearOverlays(); StartEndless(map); };
                var cv = Ui.VBox(card.transform, 4, 12, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)cv.transform);
                var nm = Ui.Text(cv.transform, Ui.Upper(Loc.Raw(new[] { "mapSpiral", "mapSerpent", "mapRose" }[i])), 19, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 2);
                Ui.Le(nm.gameObject, h: 26);
                var info = Ui.Text(cv.transform, Loc.T(new[] { "mapSpiralInfo", "mapSerpentInfo", "mapRoseInfo" }[i]), 12, Pal.TextDim, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true);
                Ui.Le(info.gameObject, h: 34);
                int best = Save.Data.endlessBest[i];
                var b = Caption(cv.transform, best > 0 ? $"{Loc.Raw("best")}  {Fmt(best)}" : "", TextAlignmentOptions.Center, Pal.GoldMid, 11); Ui.Le(b.gameObject, h: 16);
            }
            Ui.Button(v.transform, Loc.Raw("back"), BtnKind.Glass, 44, () => Pop());
        }

        // ---------------------------------------------------------------------------------------------
        // level select
        // ---------------------------------------------------------------------------------------------

        int lsWorld = -1;

        void BuildLevels()
        {
            Clear(screenRoot);
            SetDim(0.82f);
            if (lsWorld < 0) lsWorld = Mathf.Clamp((Mathf.Min(Save.Data.unlocked, Levels.LevelCount) - 1) / 10, 0, 9);
            int world = lsWorld;
            var theme = Levels.Worlds[world];
            var accent = Pal.Hex(theme.accent);
            float sw = Ui.SafeW, sh = Ui.SafeH;

            var root = Ui.VBox(screenRoot, 8, 0, TextAnchor.UpperCenter, true, "levels");
            Ui.Fill((RectTransform)root.transform);
            root.padding = new RectOffset(16, 16, 10, 12);
            Header(root.transform, Loc.Raw("levels"), ShowMenu, Loc.Raw("levelProgress", "done", CountDone(), "total", Levels.LevelCount));

            const int cols = 5; const float gap = 9;
            float innerW = Mathf.Min(720, sw - 32);
            float tileW = Mathf.Floor((innerW - gap * (cols - 1)) / cols);
            float availH = sh - 64 - 58 - 26 - 28 - 70 - 20;
            float tileH = Mathf.Max(46, Mathf.Min(Mathf.Floor((availH - gap) / 2), Mathf.Round(tileW * 1.3f)));
            float gridW = tileW * cols + gap * (cols - 1);

            // world bar
            {
                var bar = Ui.HBox(root.transform, 10, 0, TextAnchor.MiddleCenter); Ui.Le(bar.gameObject, w: gridW, h: 58);
                var l = Ui.IconButton(bar.transform, "back", 40, () => GoWorld(world - 1), 0.34f);                if (world == 0) SetAlpha(l, 0.3f);
                var mid = Ui.VBox(bar.transform, 1, 0, TextAnchor.MiddleCenter); Ui.Le(mid.gameObject, fw: 1);
                int doneW = 0; for (int i = 1; i <= 10; i++) if (Save.IsCompleted(world * 10 + i)) doneW++;
                var cap = Caption(mid.transform, $"{Loc.Raw("world")} {world + 1}  ·  {doneW}/10", TextAlignmentOptions.Center, accent, 11); Ui.Le(cap.gameObject, h: 15);
                var nm = Ui.Text(mid.transform, Ui.Upper(Loc.Raw("w" + world)), 21, Pal.Text, FontKind.Display, TextAlignmentOptions.Center, 3); Ui.Le(nm.gameObject, h: 28);
                var r = Ui.IconButton(bar.transform, "back", 40, () => GoWorld(world + 1), 0.34f);
                r.localRotation = Quaternion.Euler(0, 0, 180);
                r.GetChild(0).localRotation = Quaternion.Euler(0, 0, 0);
                if (world == 9) SetAlpha(r, 0.3f);
            }
            // pips
            {
                var pips = Ui.HBox(root.transform, 0, 0, TextAnchor.MiddleCenter); Ui.Le(pips.gameObject, w: gridW - 20, h: 24);
                pips.childForceExpandWidth = true;
                for (int i = 0; i < 10; i++)
                {
                    int wi = i;
                    var slot = Ui.Img(pips.transform, Gfx.Pixel, new Color(0, 0, 0, 0), 1, "pipslot", true);
                    Ui.Le(slot.gameObject, fw: 1, h: 24);
                    var p = slot.gameObject.AddComponent<Ui.PressFx>(); p.onClick = () => GoWorld(wi);
                    bool on = i == world; bool unl = i * 10 + 1 <= Save.Data.unlocked;
                    var dot = Ui.Img(slot.transform, Gfx.Frame("white"), on ? Pal.Hex(Levels.Worlds[i].accent) : new Color(1, 1, 1, unl ? 0.35f : 0.12f), 6f, "pip");
                    Ui.Pin(dot.rectTransform, 0.5f, 0.5f, on ? 18 : 6, 6);
                }
            }
            // grid
            {
                var gridRt = Ui.Rt("grid", root.transform);
                var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(tileW, tileH); grid.spacing = new Vector2(gap, gap);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = cols;
                grid.childAlignment = TextAnchor.UpperCenter;
                Ui.Le(gridRt.gameObject, w: gridW, h: tileH * 2 + gap);
                for (int i = 1; i <= 10; i++) BuildTile(gridRt, world * 10 + i, tileW, tileH, accent);
            }
            Ui.Spacer(root.transform, 6);
            // footer
            {
                var strip = Ui.Img(root.transform, Gfx.Frame("chip"), Color.white, 2f, "stats"); Ui.Le(strip.gameObject, w: gridW, h: 56);
                var hb = Ui.HBox(strip.transform, 8, 10, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)hb.transform);
                int ws = 0, wb = 0; for (int i = 1; i <= 10; i++) { ws += Save.Data.stars[world * 10 + i]; wb += Save.Data.best[world * 10 + i]; }
                Stat(hb.transform, Loc.Raw("stars"), $"{ws}/30", accent);
                VSep(hb.transform);
                Stat(hb.transform, Loc.Raw("best"), wb > 0 ? Fmt(wb) : "-", null, 1.4f);
            }
        }

        static void SetAlpha(RectTransform r, float a)
        {
            var cg = r.gameObject.GetComponent<CanvasGroup>(); if (cg == null) cg = r.gameObject.AddComponent<CanvasGroup>(); cg.alpha = a;
        }

        int CountDone() { int n = 0; for (int i = 1; i <= Levels.LevelCount; i++) if (Save.IsCompleted(i)) n++; return n; }

        void GoWorld(int w)
        {
            if (w < 0 || w > 9) return;
            Audio.I.Play("click"); Audio.Vibrate(Haptic.Select);
            lsWorld = w; BuildLevels();
        }

        void BuildTile(Transform parent, int id, float tw, float th, Color accent)
        {
            var lvl = Levels.Get(id);
            bool unlocked = id <= Save.Data.unlocked, done = Save.IsCompleted(id), current = unlocked && !done && id == Save.Data.unlocked;
            var bg = Ui.Img(parent, Gfx.Frame("tile"), Color.white, 1.9f, "tile", true);
            if (done) bg.color = Pal.Hex("#ffe9a6", 1f);
            else if (!unlocked) bg.color = new Color(1, 1, 1, 0.45f);
            var p = bg.gameObject.AddComponent<Ui.PressFx>();
            p.silent = !unlocked; p.tint = bg; p.normal = bg.color;
            p.onClick = () => { if (unlocked) StartLevel(id); else Audio.Vibrate(Haptic.Error); };
            if (current || (lvl.boss && unlocked))
            {
                var ring = Ui.Img(bg.transform, Gfx.Frame("white"), current ? Pal.Gold : Pal.GoldMid, 1.9f, "ring");
                ring.color = new Color(ring.color.r, ring.color.g, ring.color.b, 0.0f);
                var glow = Ui.Img(bg.transform, Gfx.Glow, accent.A(current ? 0.22f : 0.12f), 1, "glow"); Ui.Fill(glow.rectTransform);
                glow.rectTransform.offsetMin = new Vector2(-6, -6); glow.rectTransform.offsetMax = new Vector2(6, 6);
            }
            var v = Ui.VBox(bg.transform, 4, 2, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)v.transform);
            if (unlocked)
            {
                float ns = Mathf.Clamp(Mathf.Floor(tw * 0.4f), 15, 26);
                var num = Ui.Text(v.transform, id.ToString(), ns, done || current ? Pal.Gold : Pal.Text, FontKind.Display, TextAlignmentOptions.Center, 0, true, false);
                Ui.Le(num.gameObject, h: ns + 6);
                var st = Ui.Stars(v.transform, Save.Data.stars[id], Mathf.Clamp(Mathf.Floor(tw * 0.2f), 9, 13), 2);
                Ui.Le(st.gameObject, h: 14);
            }
            else
            {
                float ls = Mathf.Max(14, Mathf.Floor(tw * 0.3f));
                var lk = Ui.Img(v.transform, Gfx.Icon("lock"), new Color(1, 1, 1, 0.28f)); Ui.Le(lk.gameObject, w: ls, h: ls);
                var num = Ui.Text(v.transform, id.ToString(), 12, Pal.TextFaint, FontKind.Bold, TextAlignmentOptions.Center, 0, true, false); Ui.Le(num.gameObject, h: 16);
            }
            if (lvl.boss && unlocked)
            {
                var d = Ui.Img(bg.transform, Gfx.Pixel, Pal.GoldMid, 1, "boss");
                Ui.Pin(d.rectTransform, 1, 1, 6, 6, -9, -9); d.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
        }

        // ---------------------------------------------------------------------------------------------
        // settings
        // ---------------------------------------------------------------------------------------------

        int versionTaps;

        void BuildSettings()
        {
            Clear(screenRoot);
            SetDim(0.84f);
            var root = Ui.VBox(screenRoot, 8, 0, TextAnchor.UpperCenter, true, "settings");
            Ui.Fill((RectTransform)root.transform);
            root.padding = new RectOffset(16, 16, 10, 12);
            Header(root.transform, Loc.Raw("settings"), ShowMenu);
            var sr = Ui.Scroll(root.transform, out var content);
            Ui.Le(sr.gameObject, fh: 1, fw: 1);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            int side = Mathf.Max(0, Mathf.FloorToInt((Ui.SafeW - 32 - 560) / 2f));
            vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 18; vlg.padding = new RectOffset(side, side, 4, 24); vlg.childAlignment = TextAnchor.UpperCenter;
            SettingsBody(content, true);
        }

        void Group(Transform parent, string title, out RectTransform body)
        {
            var v = Ui.VBox(parent, 8, 0, TextAnchor.UpperCenter);
            var cap = Caption(v.transform, title, Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, Pal.TextFaint, 11); Ui.Le(cap.gameObject, h: 16);
            body = (RectTransform)v.transform;
        }

        void Row(Transform parent, string label, out RectTransform right)
        {
            var h = Ui.HBox(parent, 10, 0, TextAnchor.MiddleCenter); Ui.Le(h.gameObject, h: 34);
            var t = Ui.Text(h.transform, label, 15, Pal.Text, FontKind.Semi, Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
            Ui.Le(t.gameObject, fw: 1, h: 30);
            var slot = Ui.Rt("slot", h.transform); Ui.Le(slot.gameObject, w: 56, h: 32);
            right = slot;
        }

        /// <summary>Shared by the settings screen and the in-game pause menu.</summary>
        void SettingsBody(Transform parent, bool full)
        {
            var d = Save.Data;
            RectTransform b;
            Group(parent, Ui.Upper(Loc.Raw("control")), out b);
            Ui.Segmented(b, new[] { Loc.Raw("controlDial"), Loc.Raw("controlAim") }, d.control, i => { d.control = i; Save.Commit(); ApplyControl(); Rebuild(); }, 42);
            {
                var info = Ui.Text(b, Loc.T(d.control == 0 ? "controlDialInfo" : "controlAimInfo"), 12.5f, Pal.TextDim, FontKind.Semi, Loc.Rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, 0, true, true, true);
                Ui.Le(info.gameObject, h: 40);
            }
            Group(parent, Ui.Upper(Loc.Raw("orientation")), out b);
            Ui.Segmented(b, new[] { Loc.Raw("orientAuto"), Loc.Raw("orientPortrait"), Loc.Raw("orientLandscape") }, d.orientation, i => { d.orientation = i; Save.Commit(); ApplyOrientation(); Rebuild(); }, 42);

            var tg = Ui.VBox(parent, 12, 0, TextAnchor.UpperCenter);
            RectTransform r;
            Row(tg.transform, Loc.Raw("sfx"), out r); Ui.Toggle(r, d.sfx, v => { d.sfx = v; Save.Commit(); });
            FixToggle(r);
            Row(tg.transform, Loc.Raw("music"), out r); Ui.Toggle(r, d.music, v => { d.music = v; Save.Commit(); }); FixToggle(r);
            Row(tg.transform, Loc.Raw("haptics"), out r); Ui.Toggle(r, d.haptics, v => { d.haptics = v; Save.Commit(); }); FixToggle(r);

            if (full)
            {
                Group(parent, Ui.Upper(Loc.Raw("language")), out b);
                var gridRt = Ui.Rt("langs", b);
                float w = Mathf.Min(560, Ui.SafeW - 32);
                var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(Mathf.Floor((w - 8 * 3) / 4), 40); grid.spacing = new Vector2(8, 8);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
                Ui.Le(gridRt.gameObject, h: 40 * 2 + 8);
                for (int i = 0; i < 7; i++)
                {
                    int li = i; bool on = i == Loc.Lang;
                    var im = Ui.Img(gridRt, Gfx.Frame(on ? "gold" : "glass"), Color.white, 1.5f, "lang", true);
                    var p = im.gameObject.AddComponent<Ui.PressFx>();
                    p.onClick = () => { d.language = li; Save.Commit(); Loc.Set(li); };
                    var t = Ui.Text(im.transform, i == 5 ? ArabicShaper.Shape(Loc.Names[i]) : Loc.Names[i], 14, on ? Pal.GoldInk : Pal.Text, FontKind.Bold, TextAlignmentOptions.Center, 0, true, false);
                    if (i == 5) t.isRightToLeftText = true; Ui.Fill(t.rectTransform); t.rectTransform.offsetMin = new Vector2(4, 2); t.rectTransform.offsetMax = new Vector2(-4, -2);
                }
                Group(parent, "", out b);
                Ui.Button(b, Loc.Raw("howTo"), BtnKind.Glass, 46, () => { tutPage = 0; Push(() => BuildTutorial(false)); });
                if (full) Ui.Button(b, Loc.Raw("about"), BtnKind.Glass, 46, () => Push(BuildAbout));
            }

            if (full && d.devOn) BuildDevGroup(parent);

            if (full)
            {
                Ui.Button(parent, Loc.Raw("resetProgress"), BtnKind.Danger, 46, () => Push(() => BuildConfirm(Loc.Raw("confirmResetTitle"), Loc.Raw("confirmReset"), () =>
                {
                    Save.Reset(); ClearOverlays(); lsWorld = -1; ShowMenu();
                })));
                var ver = Ui.Text(parent, $"{Loc.Raw("version")} {Application.version}", 12, Pal.TextFaint, FontKind.Bold, TextAlignmentOptions.Center, 0.6f, true, false);
                Ui.Le(ver.gameObject, h: 30);
                ver.raycastTarget = true; // TMP is itself a Graphic; a second Image cannot be added
                var p = ver.gameObject.AddComponent<Ui.PressFx>(); p.silent = true;
                p.onClick = () =>
                {
                    versionTaps++;
                    if (Debug.isDebugBuild && versionTaps >= 7 && !Save.Data.devOn) { Save.Data.devOn = true; Save.Commit(); Audio.Vibrate(Haptic.Success); Rebuild(); }
                };
            }
        }

        static void FixToggle(RectTransform slot)
        {
            if (slot.childCount == 0) return;
            var t = (RectTransform)slot.GetChild(0);
            t.anchorMin = t.anchorMax = new Vector2(1, 0.5f); t.pivot = new Vector2(1, 0.5f); t.anchoredPosition = Vector2.zero;
            var le = t.GetComponent<LayoutElement>(); if (le != null) UnityEngine.Object.Destroy(le);
            t.sizeDelta = new Vector2(52, 30);
        }

        void BuildDevGroup(Transform parent)
        {
            RectTransform b;
            Group(parent, "DEV", out b);
            var d = Save.Data;
            RectTransform r;
            Row(b, "FPS", out r); Ui.Toggle(r, d.devFps, v => { d.devFps = v; Save.Commit(); if (!v && fpsText != null) fpsText.text = ""; }); FixToggle(r);
            Row(b, "Ölümsüz (god)", out r); Ui.Toggle(r, d.devGod, v => { d.devGod = v; Save.Commit(); if (view.game != null) view.game.god = v; }); FixToggle(r);
            Ui.Button(b, "Tüm bölümleri aç", BtnKind.Glass, 42, () => { d.unlocked = Levels.LevelCount; Save.Commit(); Rebuild(); }, -1, 14, false);
            Ui.Button(b, "Dev modunu kapat", BtnKind.Glass, 42, () => { d.devOn = false; versionTaps = 0; Save.Commit(); Rebuild(); }, -1, 14, false);
        }

        // ---------------------------------------------------------------------------------------------
        // dialogs
        // ---------------------------------------------------------------------------------------------

        void BuildAbout()
        {
            var ov = Ui.Overlay(overlayRoot, 0.86f);
            float w = Mathf.Min(380, Ui.SafeW - 32);
            var panel = Ui.Panel(ov);
            Ui.Pin(panel, 0.5f, 0.5f, w, Mathf.Min(Ui.SafeH - 24, 470));
            var v = Ui.VBox(panel, 12, 20, TextAnchor.UpperCenter); Ui.Fill((RectTransform)v.transform);
            var t = Ui.Text(v.transform, Ui.Upper(Loc.Raw("about")), 20, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 2, true, true, true); Ui.GoldGradient(t); Ui.Le(t.gameObject, h: 54);
            Ui.Ornament(v.transform, 60);
            var m = Ui.Text(v.transform, Loc.T("privacyBody"), 13.5f, Pal.Text, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true); Ui.Le(m.gameObject, h: 120);
            var c = Ui.Text(v.transform, Loc.T("creditsBody"), 12, Pal.TextDim, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true); Ui.Le(c.gameObject, h: 62);
            var ver = Ui.Text(v.transform, $"{Loc.Raw("version")} {Application.version}", 11.5f, Pal.TextFaint, FontKind.Bold, TextAlignmentOptions.Center, 0.6f, true, false); Ui.Le(ver.gameObject, h: 18);
            var sp = Ui.Spacer(v.transform, 0); Ui.Le(sp.gameObject, fh: 1);
            Ui.Button(v.transform, Loc.Raw("back"), BtnKind.Gold, 48, () => Pop());
        }

        void BuildConfirm(string title, string message, Action yes)
        {
            var ov = Ui.Overlay(overlayRoot, 0.78f);
            var panel = Ui.Panel(ov);
            Ui.Pin(panel, 0.5f, 0.5f, Mathf.Min(340, Ui.SafeW - 40), 230);
            var v = Ui.VBox(panel, 10, 20, TextAnchor.MiddleCenter); Ui.Fill((RectTransform)v.transform);
            var t = Ui.Text(v.transform, Ui.Upper(title), 21, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 2); Ui.GoldGradient(t); Ui.Le(t.gameObject, h: 30);
            Ui.Ornament(v.transform, 60);
            var m = Ui.Text(v.transform, message, 14, Pal.TextDim, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true); Ui.Le(m.gameObject, h: 60);
            var row = Ui.HBox(v.transform, 10, 0, TextAnchor.MiddleCenter); Ui.Le(row.gameObject, h: 48);
            Ui.Button(row.transform, Loc.Raw("cancel"), BtnKind.Glass, 48, () => Pop());
            Ui.Button(row.transform, Loc.Raw("yes"), BtnKind.Gold, 48, () => { overlays.RemoveAt(overlays.Count - 1); RebuildOverlayLayers(); yes(); });
        }

        // ---------------------------------------------------------------------------------------------
        // tutorial (4 cards, each with a looping miniature)
        // ---------------------------------------------------------------------------------------------

        int tutPage;

        /// <summary>Takes the saved control style live, so changing it from the pause menu works at once.</summary>
        void ApplyControl()
        {
            if (view != null && view.aim != null) view.aim.mode = Save.Data.control == 1 ? ControlMode.Aim : ControlMode.Dial;
        }

        void BuildTutorial(bool first)
        {
            var ov = Ui.Overlay(overlayRoot, 0.86f);
            float w = Mathf.Min(360, Ui.SafeW - 32);
            var panel = Ui.Panel(ov);
            Ui.Pin(panel, 0.5f, 0.5f, w, Ui.Landscape ? Mathf.Min(300, Ui.SafeH - 16) : 460);
            var v = Ui.VBox(panel, 10, 18, TextAnchor.UpperCenter); Ui.Fill((RectTransform)v.transform);
            bool land = Ui.Landscape;
            int page = tutPage;
            string[] titles = { "tut1Title", "tut2Title", "tut3Title", "tut4Title" };
            string body = page == 0 ? (Save.Data.control == 0 ? "tut1Dial" : "tut1Aim") : page == 1 ? "tut2Body" : page == 2 ? "tut3Body" : "tut4Body";

            var stage = Ui.Img(v.transform, Gfx.Frame("dark"), Color.white, 1.8f, "stage"); Ui.Le(stage.gameObject, h: land ? 80 : 150);
            BuildTutorialArt(stage.rectTransform, page, land ? 80 : 150);
            var t = Ui.Text(v.transform, Loc.T(titles[page]), 21, Pal.Gold, FontKind.Display, TextAlignmentOptions.Center, 1, true, true, true); Ui.GoldGradient(t); Ui.Le(t.gameObject, h: 54);
            var m = Ui.Text(v.transform, Loc.T(body), 13.5f, Pal.TextDim, FontKind.Semi, TextAlignmentOptions.Center, 0, true, true, true); Ui.Le(m.gameObject, h: land ? 54 : 84);
            var sp = Ui.Spacer(v.transform, 0); Ui.Le(sp.gameObject, fh: 1);
            var dots = Ui.HBox(v.transform, 6, 0, TextAnchor.MiddleCenter, false); Ui.Le(dots.gameObject, h: 8); dots.childForceExpandWidth = false;
            for (int i = 0; i < 4; i++) { var dd = Ui.Img(dots.transform, Gfx.Frame("white"), i == page ? Pal.Gold : new Color(1, 1, 1, 0.2f), 6f); Ui.Le(dd.gameObject, w: i == page ? 18 : 6, h: 6); }
            var row = Ui.HBox(v.transform, 10, 0, TextAnchor.MiddleCenter); Ui.Le(row.gameObject, h: 46);
            if (page < 3)
            {
                Ui.Button(row.transform, Loc.Raw("tutSkip"), BtnKind.Glass, 46, () => CloseTutorial());
                Ui.Button(row.transform, Loc.Raw("tutNext"), BtnKind.Gold, 46, () => { tutPage++; RebuildOverlayLayers(); });
            }
            else Ui.Button(row.transform, Loc.Raw("tutStart"), BtnKind.Gold, 46, () => CloseTutorial());
        }

        void CloseTutorial()
        {
            Save.Data.tutorialSeen = true; Save.Commit();
            Pop();
            if (current == Screen_.Game && view.game != null) view.paused = pausedByTutorial ? false : view.paused;
            pausedByTutorial = false;
        }

        bool pausedByTutorial;

        void BuildTutorialArt(RectTransform stage, int page, float h)
        {
            var set = MarbleArt.Get(WorldStyles.All[0]);
            var shooterSet = ShooterArt.Get(WorldStyles.All[0]);
            float bs = h * 0.26f;
            Image Ball(int c, float x, float y, float size)
            {
                var b = Ui.Img(stage, set.body[c], Color.white); Ui.Pin(b.rectTransform, 0.5f, 0.5f, size / MarbleSet.Fill, size / MarbleSet.Fill, x, y);
                var g = Ui.Img(b.transform, set.gloss, Color.white); Ui.Fill(g.rectTransform);
                return b;
            }
            var anim = stage.gameObject.AddComponent<Anim>();
            switch (page)
            {
                case 0:
                {
                    var barrel = Ui.Img(stage, shooterSet.barrel, Color.white); Ui.Pin(barrel.rectTransform, 0.5f, 0.35f, shooterSet.barrel.rect.width / 220f * bs * 1.2f, shooterSet.barrel.rect.height / 220f * bs * 1.2f);
                    barrel.rectTransform.pivot = new Vector2(0.3043f, 0.5f);
                    var hub = Ui.Img(stage, shooterSet.hub, Color.white); Ui.Pin(hub.rectTransform, 0.5f, 0.35f, bs * 1.2f * 2.5f * 0.78f, bs * 1.2f * 2.5f * 0.78f);
                    var dot = Ui.Img(stage, Gfx.Dot, Pal.Gold.A(0.9f)); Ui.Pin(dot.rectTransform, 0.5f, 0.35f, 16, 16);
                    anim.tick = t =>
                    {
                        float a = 90f + Mathf.Sin(t * 1.6f) * 55f;
                        barrel.rectTransform.localRotation = Quaternion.Euler(0, 0, a); hub.rectTransform.localRotation = Quaternion.Euler(0, 0, a);
                        float fa = (a - 90f) * Mathf.Deg2Rad;
                        dot.rectTransform.anchoredPosition = new Vector2(-Mathf.Sin(fa) * -bs * 2.2f * -1f + 0f, -h * 0.15f - Mathf.Cos(fa) * bs * 0.8f);
                    };
                    break;
                }
                case 1:
                {
                    var balls = new List<Image>();
                    for (int i = 0; i < 4; i++) balls.Add(Ball(i == 1 ? 0 : (i == 3 ? 3 : 0), (i - 1.5f) * bs * 1.05f, 0, bs));
                    balls[1].sprite = set.body[0]; balls[0].sprite = set.body[0]; balls[2].sprite = set.body[0]; balls[3].sprite = set.body[3];
                    var shot = Ball(0, 0, -h * 0.34f, bs);
                    anim.tick = t =>
                    {
                        float k = Mathf.Repeat(t, 2.4f) / 2.4f;
                        shot.rectTransform.anchoredPosition = new Vector2(0 * 1f - bs * 1.05f * 0.5f + bs * 1.05f * 0f, Mathf.Lerp(-h * 0.34f, 0, Mathf.Clamp01(k / 0.4f)));
                        shot.rectTransform.anchoredPosition = new Vector2(0.5f * bs * 1.05f * 0f + (0.5f * bs * 1.05f) * 0f + (-0.5f * bs * 1.05f) * 1f * 0f + bs * 1.05f * 0.5f * 0f, shot.rectTransform.anchoredPosition.y);
                        float pop = Mathf.Clamp01((k - 0.45f) / 0.2f);
                        for (int i = 0; i < 3; i++) { balls[i].rectTransform.localScale = Vector3.one * (k < 0.45f ? 1f : Mathf.Lerp(1.15f, 0f, pop)); }
                        shot.rectTransform.localScale = Vector3.one * (k < 0.45f ? 1f : Mathf.Lerp(1.15f, 0f, pop));
                        float slide = Mathf.SmoothStep(0, 1, Mathf.Clamp01((k - 0.65f) / 0.25f));
                        balls[3].rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1.5f, -0.2f, slide) * bs * 1.05f, 0);
                        shot.rectTransform.anchoredPosition = new Vector2(bs * 1.05f * -0.5f * 0f + (-0.5f) * bs * 1.05f * 0f + 0f - bs * 0.525f * 0f + (0f), shot.rectTransform.anchoredPosition.y);
                        if (k >= 0.9f) { foreach (var b in balls) b.rectTransform.localScale = Vector3.one; }
                    };
                    break;
                }
                case 2:
                {
                    var a = Ball(0, -bs * 1.5f, 0, bs); var b = Ball(3, bs * 1.5f, 0, bs);
                    anim.tick = t =>
                    {
                        float k = Mathf.SmoothStep(0, 1, Mathf.PingPong(t * 0.7f, 1f));
                        a.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-1.5f, 1.5f, k) * bs, Mathf.Sin(k * Mathf.PI) * bs * 0.6f);
                        b.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1.5f, -1.5f, k) * bs, -Mathf.Sin(k * Mathf.PI) * bs * 0.6f);
                    };
                    break;
                }
                default:
                {
                    var portal = ShooterArt.Portal(WorldStyles.All[0]);
                    var core = Ui.Img(stage, portal[0], Color.white); Ui.Pin(core.rectTransform, 0.5f, 0.5f, h * 0.95f, h * 0.95f, bs * 3.2f, 0);
                    var arms = Ui.Img(stage, portal[2], WorldStyles.All[0].portal); Ui.Pin(arms.rectTransform, 0.5f, 0.5f, h * 0.95f, h * 0.95f, bs * 3.2f, 0);
                    var chain = new List<Image>();
                    for (int i = 0; i < 5; i++) chain.Add(Ball(i % 3, 0, 0, bs));
                    anim.tick = t =>
                    {
                        float k = Mathf.Repeat(t * 0.18f, 1f);
                        arms.rectTransform.localRotation = Quaternion.Euler(0, 0, -t * 90f);
                        for (int i = 0; i < chain.Count; i++) chain[i].rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-3.8f, 2.4f, k) * bs - i * bs * 1.05f, 0);
                    };
                    break;
                }
            }
        }
    }
}
