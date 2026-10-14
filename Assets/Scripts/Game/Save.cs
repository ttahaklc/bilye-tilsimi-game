using Bilye.Core;
using System;
using UnityEngine;

namespace Bilye
{
    [Serializable]
    public class SaveData
    {
        public int unlocked = 1;
        public int[] stars = new int[101];
        public int[] best = new int[101];
        public int[] endlessBest = new int[3];
        public bool tutorialSeen;
        public bool sfx = true, music = true, haptics = true;
        public int orientation;   // 0 auto, 1 portrait, 2 landscape
        public int control;       // 0 dial, 1 aim
        public int language = -1; // -1 = follow device
        public bool devOn, devFps, devGod;
        public int ratingAsked;   // bit 0: first prompt shown, bit 1: second
    }

    public static class Save
    {
        const string Key = "bilye.save.v2";
        public static SaveData Data { get; private set; } = new SaveData();
        public static event Action Changed;

        public static void Load()
        {
            try
            {
                var raw = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(raw)) Data = JsonUtility.FromJson<SaveData>(raw) ?? new SaveData();
            }
            catch { Data = new SaveData(); }
            if (Data.stars == null || Data.stars.Length < 101) Array.Resize(ref Data.stars, 101);
            if (Data.best == null || Data.best.Length < 101) Array.Resize(ref Data.best, 101);
            if (Data.endlessBest == null || Data.endlessBest.Length < 3) Array.Resize(ref Data.endlessBest, 3);
            Data.unlocked = Mathf.Clamp(Data.unlocked, 1, Levels.LevelCount);
            if (!Debug.isDebugBuild) { Data.devOn = false; Data.devFps = false; Data.devGod = false; } // no hidden debug menu in store builds
        }

        public static void Commit()
        {
            try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); } catch { }
            Changed?.Invoke();
        }

        public static void Reset()
        {
            var keep = Data;
            Data = new SaveData
            {
                tutorialSeen = keep.tutorialSeen, sfx = keep.sfx, music = keep.music, haptics = keep.haptics,
                orientation = keep.orientation, control = keep.control, language = keep.language,
                devOn = keep.devOn, devFps = keep.devFps, devGod = keep.devGod, ratingAsked = keep.ratingAsked,
            };
            Commit();
        }

        public static void Complete(int level, int score, int stars)
        {
            if (level < 1 || level > Levels.LevelCount) return;
            Data.best[level] = Math.Max(Data.best[level], score);
            Data.stars[level] = Math.Max(Data.stars[level], stars);
            if (level + 1 > Data.unlocked) Data.unlocked = Math.Min(Levels.LevelCount, level + 1);
            Commit();
        }

        public static bool IsCompleted(int level) => level >= 1 && level <= 100 && Data.stars[level] > 0;
        public static int TotalStars() { int n = 0; for (int i = 1; i <= 100; i++) n += Data.stars[i]; return n; }
    }
}
