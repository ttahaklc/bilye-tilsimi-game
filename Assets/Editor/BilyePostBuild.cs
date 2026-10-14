#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Bilye.EditorTools
{
    /// <summary>Store-readiness patches applied to every generated Xcode project (Info.plist + build settings).</summary>
    public static class BilyePostBuild
    {
        [PostProcessBuild(900)]
        public static void Patch(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;

            // no custom cryptography: skips the export-compliance questionnaire on every upload
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            root.SetString("CFBundleDisplayName", "Bilye Tılsımı");
            // a game that plays full screen: no iPad split-view / slide-over surprises
            root.SetBoolean("UIRequiresFullScreen", true);
            root.SetBoolean("UIStatusBarHidden", true);
            root.SetBoolean("UIViewControllerBasedStatusBarAppearance", false);

            var loc = root.CreateArray("CFBundleLocalizations");
            foreach (var l in new[] { "en", "tr", "zh-Hans", "hi", "es", "ar", "fr" }) loc.AddString(l);

            plist.WriteToFile(plistPath);

            var projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            var main = proj.GetUnityMainTargetGuid();
            var fw = proj.GetUnityFrameworkTargetGuid();
            proj.SetBuildProperty(main, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(fw, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(main, "ASSETCATALOG_COMPILER_APPICON_NAME", "AppIcon");
            proj.WriteToFile(projPath);
        }
    }
}
#endif
