using System;
using System.Collections.Generic;

namespace Bilye
{
    /// <summary>Seven-language dictionary (generated LocData) with {var} substitution and Arabic shaping.</summary>
    public static class Loc
    {
        public static readonly string[] Names = { "Türkçe", "English", "中文", "हिन्दी", "Español", "العربية", "Français" };
        public const string BrandTop = "BİLYE";
        public const string BrandBottom = "TILSIMI";

        static readonly Dictionary<string, int> index = BuildIndex();
        static Dictionary<string, int> BuildIndex()
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i < LocData.Keys.Length; i++) d[LocData.Keys[i]] = i;
            return d;
        }

        public static int Lang { get; private set; }
        public static bool Rtl => Lang == 5;
        public static event Action Changed;

        public static void Set(int lang)
        {
            lang = Math.Max(0, Math.Min(LocData.Langs.Length - 1, lang));
            if (lang == Lang && _inited) return;
            Lang = lang; _inited = true;
            Changed?.Invoke();
        }
        static bool _inited;

        public static int Detect()
        {
            string code;
            switch (UnityEngine.Application.systemLanguage)
            {
                case UnityEngine.SystemLanguage.Turkish: code = "tr"; break;
                case UnityEngine.SystemLanguage.Chinese:
                case UnityEngine.SystemLanguage.ChineseSimplified:
                case UnityEngine.SystemLanguage.ChineseTraditional: code = "zh"; break;
                case UnityEngine.SystemLanguage.Hindi: code = "hi"; break;
                case UnityEngine.SystemLanguage.Spanish: code = "es"; break;
                case UnityEngine.SystemLanguage.Arabic: code = "ar"; break;
                case UnityEngine.SystemLanguage.French: code = "fr"; break;
                default: code = "en"; break;
            }
            return Array.IndexOf(LocData.Langs, code);
        }

        /// <summary>Raw (logical order, unshaped) text.</summary>
        public static string Raw(string key, params object[] kv)
        {
            string s;
            if (index.TryGetValue(key, out int k)) s = LocData.Table[Lang][k];
            else if (LocExtra.Table.TryGetValue(key, out var ex)) s = ex[Lang];
            else return key;
            for (int i = 0; i + 1 < kv.Length; i += 2) s = s.Replace("{" + kv[i] + "}", Convert.ToString(kv[i + 1]));
            return s;
        }

        /// <summary>Display text: Arabic gets contextual forms (TMP does the right-to-left ordering).</summary>
        public static string T(string key, params object[] kv) => Shape(Raw(key, kv));
        public static string Shape(string s) => Lang == 5 ? ArabicShaper.Shape(s) : s;

        public static string WorldName(int w) => T("w" + Math.Max(0, Math.Min(9, w)));
        public static string MapName(int i) => T(new[] { "mapSpiral", "mapSerpent", "mapRose" }[Math.Max(0, Math.Min(2, Math.Abs(i)))]);
        public static string MapInfo(int i) => T(new[] { "mapSpiralInfo", "mapSerpentInfo", "mapRoseInfo" }[Math.Max(0, Math.Min(2, Math.Abs(i)))]);
    }
}
