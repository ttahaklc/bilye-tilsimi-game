using UnityEngine;

namespace Bilye
{
    /// <summary>Screen size + safe area, with a test override so layouts can be checked at any size / notch without the editor window.</summary>
    public static class Display
    {
        public static bool test;
        public static int testW, testH;
        public static Rect testSafe;

        public static int Width => test ? testW : Screen.width;
        public static int Height => test ? testH : Screen.height;
        public static Rect Safe => test ? testSafe : Screen.safeArea;
    }
}
