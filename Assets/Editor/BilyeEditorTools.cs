#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Bilye.EditorTools
{
    /// <summary>Editor helpers: Game view size presets (for screenshot checks on phone shapes) and project setup.</summary>
    public static class GameViewSizer
    {
        static object GroupObj()
        {
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleType.GetProperty("instance").GetValue(null, null);
            var getGroup = sizesType.GetMethod("GetGroup");
            return getGroup.Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
        }

        public static void Set(int w, int h)
        {
            var group = GroupObj();
            var gt = group.GetType();
            var getCount = gt.GetMethod("GetTotalCount");
            var getSize = gt.GetMethod("GetGameViewSize");
            int count = (int)getCount.Invoke(group, null);
            string name = $"Bilye {w}x{h}";
            int idx = -1;
            for (int i = 0; i < count; i++)
            {
                var s = getSize.Invoke(group, new object[] { i });
                var dn = (string)s.GetType().GetProperty("displayText").GetValue(s, null);
                if (dn != null && dn.StartsWith(name)) { idx = i; break; }
            }
            if (idx < 0)
            {
                var sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
                var enumType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { enumType, typeof(int), typeof(int), typeof(string) });
                var size = ctor.Invoke(new object[] { Enum.ToObject(enumType, 1), w, h, name });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                idx = (int)getCount.Invoke(group, null) - 1;
            }
            var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var win = EditorWindow.GetWindow(gvType);
            gvType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(win, idx, null);
            win.Repaint();
        }

        [MenuItem("Bilye/GameView/Phone Portrait 390x844")] static void P() => Set(390, 844);
        [MenuItem("Bilye/GameView/Phone Landscape 844x390")] static void L() => Set(844, 390);
        [MenuItem("Bilye/GameView/Small Phone 375x667")] static void S() => Set(375, 667);
        [MenuItem("Bilye/GameView/Tablet Portrait 820x1180")] static void T() => Set(820, 1180);
    }
}
#endif
