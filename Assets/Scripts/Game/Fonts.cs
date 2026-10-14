using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Bilye
{
    /// <summary>
    /// Cinzel (display) and Manrope (body) as dynamic TMP fonts, with OS fonts as fallbacks for Chinese, Devanagari and Arabic.
    /// </summary>
    public static class Fonts
    {
        public static TMP_FontAsset Display, Body, Semi;
        static readonly List<TMP_FontAsset> fallbacks = new List<TMP_FontAsset>();
        static bool ready;

        static TMP_FontAsset Make(string res)
        {
            var f = Resources.Load<Font>("Fonts/" + res);
            if (f == null) { Debug.LogWarning("Font missing: " + res); return null; }
            var a = TMP_FontAsset.CreateFontAsset(f, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (a != null) { a.name = res; a.isMultiAtlasTexturesEnabled = true; }
            return a;
        }

        static TMP_FontAsset MakeOs(string[] families)
        {
            string[] installed;
            try { installed = Font.GetOSInstalledFontNames(); } catch { return null; }
            foreach (var fam in families)
            {
                foreach (var name in installed)
                {
                    if (!string.Equals(name, fam, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var style in new[] { "Regular", "Medium", "Bold", "" })
                    {
                        try
                        {
                            var a = TMP_FontAsset.CreateFontAsset(name, style, 72);
                            if (a != null) { a.isMultiAtlasTexturesEnabled = true; return a; }
                        }
                        catch { }
                    }
                }
            }
            return null;
        }

        public static void Init()
        {
            if (ready) return;
            ready = true;
            Display = Make("Cinzel_900Black");
            Body = Make("Manrope_700Bold");
            Semi = Make("Manrope_600SemiBold");
            string[][] families =
            {
                // Chinese
                new[] { "PingFang SC", "Heiti SC", "Hiragino Sans GB", "Noto Sans CJK SC", "Noto Sans SC", "Microsoft YaHei", "SimHei", "Droid Sans Fallback", "Arial Unicode MS" },
                // Devanagari
                new[] { "Kohinoor Devanagari", "Devanagari Sangam MN", "ITF Devanagari", "Noto Sans Devanagari", "Mangal", "Nirmala UI", "Lohit Devanagari", "Arial Unicode MS" },
                // Arabic
                new[] { "Geeza Pro", "Al Nile", "Noto Sans Arabic", "Noto Naskh Arabic", "Droid Arabic Naskh", "Traditional Arabic", "Segoe UI", "Arial", "Arial Unicode MS" },
                // last resort
                new[] { "Helvetica Neue", "Arial", "Roboto", "Noto Sans", "Droid Sans" },
            };
            foreach (var fam in families) { var a = MakeOs(fam); if (a != null && !fallbacks.Contains(a)) fallbacks.Add(a); }
            foreach (var f in new[] { Display, Body, Semi })
            {
                if (f == null) continue;
                f.fallbackFontAssetTable = new List<TMP_FontAsset>(fallbacks);
            }
            if (TMP_Settings.instance != null) { /* touch to make sure the settings asset is loaded */ }
        }
    }
}
