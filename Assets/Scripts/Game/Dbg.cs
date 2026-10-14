using System.Text;
using Bilye.Core;
using UnityEngine;

namespace Bilye
{
    /// <summary>Editor/test hooks (reflection-friendly): state dump and a command console used by automated checks.</summary>
    public static class Dbg
    {
        public static string LastError = "";
        public static string Dump()
        {
            var sb = new StringBuilder();
            if (LastError.Length > 0) sb.AppendLine("LASTERROR: " + LastError);
            var gv = Object.FindFirstObjectByType<GameView>();
            sb.AppendLine("screen=" + Display.Width + "x" + Display.Height + " safe=" + Display.Safe);
            if (gv == null) return sb + "no GameView";
            if (gv.game == null) return sb + "no game";
            var g = gv.game;
            sb.AppendLine($"level={g.level.id} world={g.level.world} balls={g.balls.Count} queue={g.queue.Count} phase={g.phase} score={g.score} d={g.d:0.0} path={g.path.length:0} layout={gv.layoutW:0}x{gv.layoutH:0} paused={gv.paused} demo={gv.demo}");
            if (g.balls.Count > 0) { var b = g.balls[g.balls.Count - 1]; sb.AppendLine($"lead s={b.s:0} xy=({b.x:0},{b.y:0}) target={g.targetS:0} danger={g.danger:0.00} maxDanger={g.maxDanger:0.00}"); }
            var srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int en = 0; foreach (var s in srs) if (s.enabled && s.gameObject.activeInHierarchy) en++;
            sb.AppendLine($"sprites={srs.Length} enabled={en}");
            return sb.ToString();
        }

        /// <summary>Saves the Game view (UI included) to Assets/Screenshots/name.png at the end of the frame.</summary>
        public static string Shot(string name)
        {
            if (App.I == null) return "no app";
            App.I.StartCoroutine(ShotRoutine(name));
            return "queued " + name;
        }

        static System.Collections.IEnumerator ShotRoutine(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D tex;
            if (Display.test && App.I.testRt != null)
            {
                var rt = App.I.testRt; var prev = RenderTexture.active; RenderTexture.active = rt;
                tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
            }
            else tex = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.Directory.CreateDirectory("Assets/Screenshots");
            System.IO.File.WriteAllBytes("Assets/Screenshots/" + name + ".png", tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        public static string Progress = "";

        /// <summary>Runs "cmd; wait 2; shot name; ..." in real time (commands go to App.Cmd).</summary>
        public static string Run(string script)
        {
            if (App.I == null) return "no app";
            Progress = "running";
            App.I.StartCoroutine(RunRoutine(script));
            return "started";
        }

        static System.Collections.IEnumerator RunRoutine(string script)
        {
            foreach (var raw in script.Split(';'))
            {
                var c = raw.Trim();
                if (c.Length == 0) continue;
                if (c.StartsWith("wait ")) { yield return new WaitForSecondsRealtime(float.Parse(c.Substring(5), System.Globalization.CultureInfo.InvariantCulture)); continue; }
                if (c.StartsWith("shot ")) { yield return ShotRoutine(c.Substring(5).Trim()); continue; }
                Progress += " | " + Cmd(c);
            }
            Progress += " | DONE";
        }

        public static string Cmd(string c) { return App.I != null ? App.I.Cmd(c) : "no app"; }
    }
}
