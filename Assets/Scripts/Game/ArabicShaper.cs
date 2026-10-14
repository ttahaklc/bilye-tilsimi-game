using System.Collections.Generic;
using System.Text;

namespace Bilye
{
    /// <summary>
    /// Minimal Arabic contextual shaper: maps base letters to Presentation Forms-B (isolated/final/initial/medial)
    /// and lam-alef ligatures. Output is in LOGICAL order; the TMP text is switched to right-to-left so TMP does the reordering.
    /// </summary>
    public static class ArabicShaper
    {
        // base, isolated, final, initial, medial   (0 = form does not exist)
        static readonly int[][] T =
        {
            new[]{0x0621,0xFE80,0,0,0},
            new[]{0x0622,0xFE81,0xFE82,0,0},
            new[]{0x0623,0xFE83,0xFE84,0,0},
            new[]{0x0624,0xFE85,0xFE86,0,0},
            new[]{0x0625,0xFE87,0xFE88,0,0},
            new[]{0x0626,0xFE89,0xFE8A,0xFE8B,0xFE8C},
            new[]{0x0627,0xFE8D,0xFE8E,0,0},
            new[]{0x0628,0xFE8F,0xFE90,0xFE91,0xFE92},
            new[]{0x0629,0xFE93,0xFE94,0,0},
            new[]{0x062A,0xFE95,0xFE96,0xFE97,0xFE98},
            new[]{0x062B,0xFE99,0xFE9A,0xFE9B,0xFE9C},
            new[]{0x062C,0xFE9D,0xFE9E,0xFE9F,0xFEA0},
            new[]{0x062D,0xFEA1,0xFEA2,0xFEA3,0xFEA4},
            new[]{0x062E,0xFEA5,0xFEA6,0xFEA7,0xFEA8},
            new[]{0x062F,0xFEA9,0xFEAA,0,0},
            new[]{0x0630,0xFEAB,0xFEAC,0,0},
            new[]{0x0631,0xFEAD,0xFEAE,0,0},
            new[]{0x0632,0xFEAF,0xFEB0,0,0},
            new[]{0x0633,0xFEB1,0xFEB2,0xFEB3,0xFEB4},
            new[]{0x0634,0xFEB5,0xFEB6,0xFEB7,0xFEB8},
            new[]{0x0635,0xFEB9,0xFEBA,0xFEBB,0xFEBC},
            new[]{0x0636,0xFEBD,0xFEBE,0xFEBF,0xFEC0},
            new[]{0x0637,0xFEC1,0xFEC2,0xFEC3,0xFEC4},
            new[]{0x0638,0xFEC5,0xFEC6,0xFEC7,0xFEC8},
            new[]{0x0639,0xFEC9,0xFECA,0xFECB,0xFECC},
            new[]{0x063A,0xFECD,0xFECE,0xFECF,0xFED0},
            new[]{0x0641,0xFED1,0xFED2,0xFED3,0xFED4},
            new[]{0x0642,0xFED5,0xFED6,0xFED7,0xFED8},
            new[]{0x0643,0xFED9,0xFEDA,0xFEDB,0xFEDC},
            new[]{0x0644,0xFEDD,0xFEDE,0xFEDF,0xFEE0},
            new[]{0x0645,0xFEE1,0xFEE2,0xFEE3,0xFEE4},
            new[]{0x0646,0xFEE5,0xFEE6,0xFEE7,0xFEE8},
            new[]{0x0647,0xFEE9,0xFEEA,0xFEEB,0xFEEC},
            new[]{0x0648,0xFEED,0xFEEE,0,0},
            new[]{0x0649,0xFEEF,0xFEF0,0,0},
            new[]{0x064A,0xFEF1,0xFEF2,0xFEF3,0xFEF4},
        };

        static readonly Dictionary<int, int[]> map = Build();
        static Dictionary<int, int[]> Build()
        {
            var d = new Dictionary<int, int[]>();
            foreach (var r in T) d[r[0]] = r;
            return d;
        }

        static bool IsMark(int c) => c >= 0x064B && c <= 0x065F || c == 0x0670;
        static bool IsArabic(int c) => map.ContainsKey(c);
        // can connect to the NEXT letter (has initial form)
        static bool JoinsNext(int c) => map.TryGetValue(c, out var r) && r[3] != 0;
        // can connect to the PREVIOUS letter
        static bool JoinsPrev(int c) => map.TryGetValue(c, out var r) && r[2] != 0;

        public static bool HasArabic(string s)
        {
            foreach (var ch in s) if (ch >= 0x0600 && ch <= 0x06FF) return true;
            return false;
        }

        public static string Shape(string s)
        {
            if (string.IsNullOrEmpty(s) || !HasArabic(s)) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                int c = s[i];
                if (!IsArabic(c)) { sb.Append((char)c); continue; }

                // previous base letter (skip diacritics)
                int p = i - 1;
                while (p >= 0 && IsMark(s[p])) p--;
                int prev = p >= 0 ? s[p] : 0;
                bool joinPrev = prev != 0 && JoinsNext(prev) && JoinsPrev(c);

                // lam + alef ligature
                if (c == 0x0644 && i + 1 < s.Length)
                {
                    int n = s[i + 1];
                    int lig = n == 0x0622 ? (joinPrev ? 0xFEF6 : 0xFEF5)
                            : n == 0x0623 ? (joinPrev ? 0xFEF8 : 0xFEF7)
                            : n == 0x0625 ? (joinPrev ? 0xFEFA : 0xFEF9)
                            : n == 0x0627 ? (joinPrev ? 0xFEFC : 0xFEFB) : 0;
                    if (lig != 0) { sb.Append((char)lig); i++; continue; }
                }

                int q = i + 1;
                while (q < s.Length && IsMark(s[q])) q++;
                int next = q < s.Length ? s[q] : 0;
                bool joinNext = next != 0 && IsArabic(next) && JoinsNext(c) && JoinsPrev(next);

                var r = map[c];
                int form = joinPrev && joinNext ? r[4] : joinPrev ? r[2] : joinNext ? r[3] : r[1];
                if (form == 0) form = joinPrev ? (r[2] != 0 ? r[2] : r[1]) : r[1];
                sb.Append((char)form);
            }
            return sb.ToString();
        }
    }
}
