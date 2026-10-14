using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Bilye
{
    public enum Haptic { Light, Medium, Heavy, Select, Success, Error }

    /// <summary>Sound effects (pooled sources), looping music and haptics, all gated by the saved settings.</summary>
    public sealed class Audio : MonoBehaviour
    {
        public static Audio I;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioSource[]> pools = new Dictionary<string, AudioSource[]>();
        readonly Dictionary<string, int> cursor = new Dictionary<string, int>();
        AudioSource music;
        bool musicWanted, musicPaused;

        static readonly string[] Names = { "shoot", "hit", "match", "combo2", "combo3", "combo4", "win", "lose", "click", "swap", "danger" };
        static readonly Dictionary<string, int> PoolSize = new Dictionary<string, int> { { "shoot", 3 }, { "hit", 3 }, { "match", 3 }, { "click", 2 }, { "swap", 2 } };
        static readonly Dictionary<string, float> Vol = new Dictionary<string, float> { { "shoot", 0.5f }, { "hit", 0.7f }, { "danger", 0.6f }, { "click", 0.6f } };

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _BilyeHaptic(int kind);
#endif

        public static Audio Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<Audio>();
            I.Init();
            return I;
        }

        void Init()
        {
            foreach (var n in Names)
            {
                var clip = Resources.Load<AudioClip>("Sounds/" + n);
                if (clip == null) continue;
                clips[n] = clip;
                int count = PoolSize.TryGetValue(n, out var c) ? c : 1;
                var arr = new AudioSource[count];
                for (int i = 0; i < count; i++)
                {
                    var s = gameObject.AddComponent<AudioSource>();
                    s.clip = clip; s.playOnAwake = false; s.volume = Vol.TryGetValue(n, out var v) ? v : 0.85f;
                    arr[i] = s;
                }
                pools[n] = arr;
            }
            var m = Resources.Load<AudioClip>("Sounds/music");
            if (m != null)
            {
                music = gameObject.AddComponent<AudioSource>();
                music.clip = m; music.loop = true; music.volume = 0.35f; music.playOnAwake = false;
            }
            Save.Changed += ApplyMusic;
        }

        public void Play(string name)
        {
            if (!Save.Data.sfx) return;
            if (!pools.TryGetValue(name, out var list) || list.Length == 0) return;
            cursor.TryGetValue(name, out int i);
            cursor[name] = i + 1;
            var s = list[i % list.Length];
            s.Stop(); s.Play();
        }

        public void Combo(int combo) => Play(combo >= 4 ? "combo4" : combo == 3 ? "combo3" : "combo2");

        public void StartMusic() { musicWanted = true; ApplyMusic(); }
        public void PauseMusic() { musicPaused = true; ApplyMusic(); }
        public void ResumeMusic() { musicPaused = false; ApplyMusic(); }

        void ApplyMusic()
        {
            if (music == null) return;
            bool should = musicWanted && !musicPaused && Save.Data.music;
            if (should && !music.isPlaying) music.Play();
            else if (!should && music.isPlaying) music.Pause();
        }

        void OnApplicationPause(bool paused) { if (paused) { if (music != null) music.Pause(); } else ApplyMusic(); }

        public static void Vibrate(Haptic h)
        {
            if (!Save.Data.haptics) return;
#if UNITY_IOS && !UNITY_EDITOR
            _BilyeHaptic((int)h == 0 ? 0 : (int)h);
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var act = up.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vib = act.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    long ms = h == Haptic.Heavy || h == Haptic.Error ? 40 : h == Haptic.Medium || h == Haptic.Success ? 22 : 10;
                    vib.Call("vibrate", ms);
                }
            }
            catch { }
            if ((int)h == 99) Handheld.Vibrate(); // referenced so Unity adds the VIBRATE permission
#endif
        }
    }
}
