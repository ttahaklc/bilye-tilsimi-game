#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Projeyi ilk açışta Android (Play Store) için hazırlar: AAB, IL2CPP, ARM64, ikon.
// Menüden de çalıştırılabilir: Bilye > Android hazırla. Hata verirse sessizce atlar, projeyi bozmaz.
[InitializeOnLoad]
static class BilyeAndroidSetup
{
    const string Key = "BilyeAndroidSetup.v1.";
    static BilyeAndroidSetup() { EditorApplication.delayCall += () => { if (!EditorPrefs.GetBool(Key + Application.dataPath, false)) Apply(); }; }

    [MenuItem("Bilye/Android hazırla")]
    static void Menu() { Apply(); }

    static void Apply()
    {
        try
        {
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/AppIcon.png");
            if (tex != null) SetLegacyIcons(tex);
            EditorPrefs.SetBool(Key + Application.dataPath, true);
            Debug.Log("[Bilye] Android ayarları uygulandı (AAB, IL2CPP, ARM64, ikon).");
        }
        catch (Exception e) { Debug.LogWarning("[Bilye] Android kurulumu atlandı: " + e.Message); }
    }

    // API sürümler arası değiştiği için yansıma ile çağrılır.
    static void SetLegacyIcons(Texture2D tex)
    {
        var ps = typeof(PlayerSettings);
        var nbt = UnityEditor.Build.NamedBuildTarget.Android;
        foreach (var m in ps.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (m.Name != "GetIconSizesForTargetGroup" && m.Name != "GetIconSizes") continue;
            var pr = m.GetParameters();
            if (pr.Length != 2 || pr[0].ParameterType != typeof(UnityEditor.Build.NamedBuildTarget)) continue;
            var sizes = (int[])m.Invoke(null, new object[] { nbt, Enum.ToObject(pr[1].ParameterType, 0) });
            var arr = new Texture2D[sizes.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = tex;
            foreach (var s in ps.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (s.Name != "SetIcons") continue;
                var sp = s.GetParameters();
                if (sp.Length != 3 || sp[0].ParameterType != typeof(UnityEditor.Build.NamedBuildTarget)) continue;
                s.Invoke(null, new object[] { nbt, arr, Enum.ToObject(sp[2].ParameterType, 0) });
                return;
            }
        }
    }
}
#endif
