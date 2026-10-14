using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bilye
{
    public enum FontKind { Display, Bold, Semi }
    public enum BtnKind { Gold, Glass, Danger }

    /// <summary>Small immediate-style uGUI toolkit: everything is built in code with the procedural frames from Gfx.</summary>
    public static class Ui
    {
        public static Canvas Root;
        public static RectTransform Safe;
        public static float Width, Height;     // canvas units (== layout points)
        public static float SafeW, SafeH;
        public static bool Landscape => Width > Height;

        public static TMP_FontAsset FontOf(FontKind k) => k == FontKind.Display ? Fonts.Display : k == FontKind.Semi ? Fonts.Semi ?? Fonts.Body : Fonts.Body;

        public static string Upper(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var ci = Loc.Lang == 0 ? new CultureInfo("tr-TR") : CultureInfo.InvariantCulture;
            return s.ToUpper(ci);
        }

        // ---------------------------------------------------------------------------------------------
        // construction helpers
        // ---------------------------------------------------------------------------------------------

        public static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            return r;
        }

        public static RectTransform Anchor(RectTransform r, float x0, float y0, float x1, float y1, float l = 0, float b = 0, float rr = 0, float t = 0)
        {
            r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1);
            r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t);
            return r;
        }

        /// <summary>Fixed-size rect anchored to a point of its parent (anchor == pivot).</summary>
        public static RectTransform Pin(RectTransform r, float ax, float ay, float w, float h, float x = 0, float y = 0)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(ax, ay);
            r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, y);
            return r;
        }

        public static Image Img(Transform parent, Sprite s, Color c, float ppum = 1f, string name = "img", bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var im = go.GetComponent<Image>();
            im.sprite = s; im.color = c; im.raycastTarget = raycast;
            if (s != null && s.border != Vector4.zero) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = ppum; }
            return im;
        }

        public static LayoutElement Le(GameObject go, float w = -1, float h = -1, float fw = -1, float fh = -1)
        {
            var le = go.GetComponent<LayoutElement>(); if (le == null) le = go.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w; if (h >= 0) le.preferredHeight = h;
            if (w >= 0) le.minWidth = w; if (h >= 0) le.minHeight = h;
            if (fw >= 0) le.flexibleWidth = fw; if (fh >= 0) le.flexibleHeight = fh;
            return le;
        }

        public static VerticalLayoutGroup VBox(Transform parent, float spacing = 8, float pad = 0, TextAnchor align = TextAnchor.UpperCenter, bool expandW = true, string name = "v")
        {
            var r = Rt(name, parent);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            v.childAlignment = align; v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = expandW; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HBox(Transform parent, float spacing = 8, float pad = 0, TextAnchor align = TextAnchor.MiddleCenter, bool expandH = true, string name = "h")
        {
            var r = Rt(name, parent);
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            h.childAlignment = align; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = expandH;
            Le(r.gameObject, fh: 0);
            return h;
        }

        public static RectTransform Spacer(Transform parent, float h)
        {
            var r = Rt("sp", parent); Le(r.gameObject, h: h, w: 0); return r;
        }

        // ---------------------------------------------------------------------------------------------
        // text
        // ---------------------------------------------------------------------------------------------

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, FontKind kind = FontKind.Bold,
            TextAlignmentOptions align = TextAlignmentOptions.Center, float spacing = 0f, bool shrink = true, bool localized = true, bool wrap = false)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = FontOf(kind);
            t.fontSize = size; t.color = color; t.alignment = align; t.characterSpacing = spacing;
            t.enableWordWrapping = wrap; t.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = false;
            if (shrink && !wrap) { t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = Mathf.Max(8, size * 0.5f); }
            SetText(t, text, localized);
            return t;
        }

        public static void SetText(TMP_Text t, string text, bool localized = true)
        {
            bool rtl = Loc.Rtl && localized;
            t.isRightToLeftText = rtl;
            t.text = rtl ? ArabicShaper.Shape(text) : text;
        }

        public static void GoldGradient(TMP_Text t)
        {
            t.enableVertexGradient = true;
            t.colorGradient = new VertexGradient(Pal.GoldHi, Pal.GoldHi, Pal.Hex("#d9ac52"), Pal.Hex("#d9ac52"));
            t.color = Color.white;
        }

        // ---------------------------------------------------------------------------------------------
        // widgets
        // ---------------------------------------------------------------------------------------------

        public sealed class PressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler
        {
            public Action onClick; public bool silent; public Image tint; public Color normal; public float pressScale = 0.965f;
            public Sprite pressedSprite, normalSprite;
            bool down;
            public void OnPointerDown(PointerEventData e) { down = true; Apply(); }
            public void OnPointerUp(PointerEventData e) { down = false; Apply(); }
            public void OnPointerExit(PointerEventData e) { down = false; Apply(); }
            void Apply()
            {
                transform.localScale = Vector3.one * (down ? pressScale : 1f);
                if (tint != null)
                {
                    if (pressedSprite != null) tint.sprite = down ? pressedSprite : normalSprite;
                    else tint.color = down ? normal * 0.82f : normal;
                    if (tint.color.a < normal.a) { }
                }
            }
            public void OnPointerClick(PointerEventData e)
            {
                if (!silent) { Audio.I?.Play("click"); Audio.Vibrate(Haptic.Light); }
                onClick?.Invoke();
            }
            void OnDisable() { down = false; transform.localScale = Vector3.one; }
        }

        public static GameObject Clickable(GameObject go, Action onClick, bool silent = false)
        {
            var im = go.GetComponent<Graphic>(); if (im != null) im.raycastTarget = true;
            var p = go.GetComponent<PressFx>(); if (p == null) p = go.AddComponent<PressFx>();
            p.onClick = onClick; p.silent = silent;
            return go;
        }

        /// <summary>Rounded button. Height is fixed, width comes from the layout (or `width` when given).</summary>
        public static RectTransform Button(Transform parent, string label, BtnKind kind, float height, Action onClick, float width = -1, float fontSize = -1, bool localized = true)
        {
            string frame = kind == BtnKind.Gold ? "gold" : kind == BtnKind.Danger ? "danger" : "glass";
            var im = Img(parent, Gfx.Frame(frame), Color.white, 1.7f, "btn", true);
            var rt = im.rectTransform;
            Le(im.gameObject, w: width, h: height, fw: width >= 0 ? 0 : 1);
            var p = im.gameObject.AddComponent<PressFx>();
            p.onClick = onClick; p.tint = im; p.normal = Color.white;
            if (kind == BtnKind.Gold) { p.normalSprite = Gfx.Frame("gold"); p.pressedSprite = Gfx.Frame("goldPressed"); }
            float fs = fontSize > 0 ? fontSize : Mathf.Clamp(height * 0.38f, 12, 24);
            var t = Text(rt, localized ? Upper(label) : label, fs, kind == BtnKind.Gold ? Pal.GoldInk : kind == BtnKind.Danger ? Pal.DangerSoft : Pal.Text, FontKind.Display, TextAlignmentOptions.Center, fs * 0.09f, true, localized);
            Fill(t.rectTransform); t.rectTransform.offsetMin = new Vector2(12, 2); t.rectTransform.offsetMax = new Vector2(-12, -2);
            return rt;
        }

        public static RectTransform IconButton(Transform parent, string icon, float size, Action onClick, float iconScale = 0.46f)
        {
            var im = Img(parent, Gfx.Frame("chip"), Color.white, 1.4f, "ibtn", true);
            Le(im.gameObject, w: size, h: size);
            var p = im.gameObject.AddComponent<PressFx>();
            p.onClick = onClick; p.tint = im; p.normal = Color.white;
            var ic = Img(im.transform, Gfx.Icon(icon), Pal.Gold, 1f, "ic");
            Pin(ic.rectTransform, 0.5f, 0.5f, size * iconScale * 2f, size * iconScale * 2f);
            return im.rectTransform;
        }

        public static void Ornament(Transform parent, float width = 120)
        {
            var r = Rt("orn", parent); Le(r.gameObject, w: width, h: 10);
            var line = Img(r, Gfx.Pixel, Pal.GoldLine, 1, "line");
            Anchor(line.rectTransform, 0, 0.5f, 1, 0.5f, 0, -0.5f, 0, -0.5f);
            var dia = Img(r, Gfx.Pixel, Pal.Gold, 1, "dia");
            Pin(dia.rectTransform, 0.5f, 0.5f, 6, 6); dia.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var gap = Img(r, Gfx.Pixel, new Color(0, 0, 0, 0), 1, "gap");
        }

        public static RectTransform Stars(Transform parent, int count, float size, float gap = 4, int total = 3)
        {
            var h = HBox(parent, gap, 0, TextAnchor.MiddleCenter, false, "stars");
            h.childForceExpandWidth = false;
            for (int i = 0; i < total; i++)
            {
                var im = Img(h.transform, Gfx.Icon("star"), i < count ? Pal.Hex("#f5cf62") : new Color(1, 1, 1, 0.14f), 1, "s");
                Le(im.gameObject, w: size, h: size);
            }
            return (RectTransform)h.transform;
        }

        public sealed class SwitchAnim : MonoBehaviour
        {
            public RectTransform knob; public Image track; public bool on; float k; public float travel = 20;
            void Update()
            {
                float target = on ? 1 : 0;
                k = Mathf.MoveTowards(k, target, Time.unscaledDeltaTime * 8f);
                if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, k), 0);
                if (track != null) track.color = Color.Lerp(new Color(1, 1, 1, 0.14f), Pal.Hex("#d4a64a"), k);
            }
            public void Snap(bool v) { on = v; k = v ? 1 : 0; }
        }

        public static RectTransform Toggle(Transform parent, bool value, Action<bool> onChange)
        {
            var tr = Img(parent, Gfx.Frame("white"), Color.white, 2.1f, "toggle", true);
            Le(tr.gameObject, w: 52, h: 30);
            var knob = Img(tr.transform, Gfx.Dot, Pal.GoldHi, 1, "knob");
            Pin(knob.rectTransform, 0.5f, 0.5f, 24, 24);
            var sa = tr.gameObject.AddComponent<SwitchAnim>();
            sa.knob = knob.rectTransform; sa.track = tr; sa.travel = 11; sa.Snap(value);
            var p = tr.gameObject.AddComponent<PressFx>();
            p.onClick = () => { sa.on = !sa.on; Audio.Vibrate(Haptic.Select); onChange(sa.on); };
            p.silent = false;
            return tr.rectTransform;
        }

        public static RectTransform Segmented(Transform parent, string[] labels, int selected, Action<int> onChange, float height = 40)
        {
            var h = HBox(parent, 6, 0, TextAnchor.MiddleCenter, true, "seg");
            Le(h.gameObject, h: height);
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i; bool on = i == selected;
                var im = Img(h.transform, Gfx.Frame(on ? "gold" : "glass"), Color.white, 1.5f, "opt", true);
                Le(im.gameObject, fw: 1, h: height);
                var p = im.gameObject.AddComponent<PressFx>(); p.onClick = () => onChange(idx);
                var t = Text(im.transform, labels[i], 13.5f, on ? Pal.GoldInk : Pal.Text, FontKind.Bold);
                Fill(t.rectTransform); t.rectTransform.offsetMin = new Vector2(6, 2); t.rectTransform.offsetMax = new Vector2(-6, -2);
            }
            return (RectTransform)h.transform;
        }

        public static RectTransform Panel(Transform parent, string frame = "panel", float ppum = 2.2f)
        {
            var im = Img(parent, Gfx.Frame(frame), Color.white, ppum, "panel", true);
            return im.rectTransform;
        }

        public static RectTransform Overlay(Transform parent, float dim = 0.68f)
        {
            var im = Img(parent, Gfx.Pixel, new Color(0.01f, 0.02f, 0.05f, dim), 1, "overlay", true);
            Fill(im.rectTransform);
            // bleed past the safe area (symmetric, so centred children stay centred)
            im.rectTransform.offsetMin = new Vector2(-400, -400); im.rectTransform.offsetMax = new Vector2(400, 400);
            return im.rectTransform;
        }

        public static ScrollRect Scroll(Transform parent, out RectTransform content)
        {
            var root = Rt("scroll", parent);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            var mask = Rt("viewport", root); Fill(mask);
            var im = mask.gameObject.AddComponent<Image>(); im.color = new Color(0, 0, 0, 0.004f);
            mask.gameObject.AddComponent<RectMask2D>();
            content = Rt("content", mask);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(0, 0); content.offsetMax = new Vector2(0, 0);
            var csf = content.gameObject.AddComponent<ContentSizeFitter>(); csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = mask; sr.content = content; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30; sr.inertia = true;
            return sr;
        }
    }
}

namespace Bilye
{
    /// <summary>Tiny per-frame animation hook for illustrations.</summary>
    public sealed class Anim : UnityEngine.MonoBehaviour
    {
        public System.Action<float> tick; float t;
        void Update() { t += UnityEngine.Time.unscaledDeltaTime; tick?.Invoke(t); }
    }
}
