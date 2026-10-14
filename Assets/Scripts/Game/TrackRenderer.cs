using System.Collections.Generic;
using Bilye.Core;
using UnityEngine;

namespace Bilye
{
    /// <summary>
    /// The carved groove the marbles roll in. One mesh strip with a lit cross-section (outer shadow, bevelled rim,
    /// gold hairline, wall, floor) whose lighting follows the path direction so every bend catches the light differently.
    /// </summary>
    public static class TrackRenderer
    {
        // offset (in units of the groove half width), role
        const int Rows = 17;
        static readonly float[] Off = { -1.42f, -1.18f, -1.08f, -1.03f, -0.97f, -0.80f, -0.60f, -0.25f, 0f, 0.25f, 0.60f, 0.80f, 0.97f, 1.03f, 1.08f, 1.18f, 1.42f };

        public static Mesh Build(Game g, WorldTheme theme, System.Func<double, double, Vector3> toWorld, float unit, bool tablet)
        {
            var p = g.path;
            int n = p.count;
            int stride = Mathf.Max(1, Mathf.RoundToInt(5f / (float)p.step));
            var idx = new List<int>();
            for (int i = 0; i < n; i += stride) idx.Add(i);
            if (idx[idx.Count - 1] != n - 1) idx.Add(n - 1);
            int m = idx.Count;

            float half = (float)g.d * 0.66f * unit;
            Color rim = Pal.Hex(theme.rim), groove = Pal.Hex(theme.groove), deep = Pal.Hex(theme.grooveDeep), acc = Pal.Hex(theme.accent);
            var L = new Vector2(-0.55f, 0.83f).normalized; // light from upper left, y up in world

            var verts = new Vector3[m * Rows];
            var cols = new Color[m * Rows];
            var tris = new int[(m - 1) * (Rows - 1) * 6];

            for (int s = 0; s < m; s++)
            {
                int i = idx[s];
                int i0 = Mathf.Max(0, i - 2), i1 = Mathf.Min(n - 1, i + 2);
                var a = toWorld(p.xs[i0], p.ys[i0]); var b = toWorld(p.xs[i1], p.ys[i1]);
                var c = toWorld(p.xs[i], p.ys[i]);
                var t = new Vector2(b.x - a.x, b.y - a.y);
                if (t.sqrMagnitude < 1e-12f) t = Vector2.right;
                t.Normalize();
                var nrm = new Vector2(-t.y, t.x);          // left normal
                float lit = Vector2.Dot(nrm, L);            // >0: the left side faces the light

                for (int r = 0; r < Rows; r++)
                {
                    float o = Off[r];
                    verts[s * Rows + r] = new Vector3(c.x + nrm.x * o * half, c.y + nrm.y * o * half, 0f);
                    float side = Mathf.Sign(o);                        // -1 right, +1 left
                    float facing = side * lit;                          // outward normal . light
                    float ao = Mathf.Abs(o);
                    Color col;
                    if (ao > 1.3f) col = new Color(0.01f, 0.015f, 0.03f, 0f);
                    else if (ao > 1.12f) col = new Color(0.01f, 0.015f, 0.03f, 0.5f);
                    else if (ao > 1.05f) col = Color.Lerp(rim * 0.7f, Color.Lerp(rim, Color.white, 0.55f), Mathf.Clamp01(0.5f + facing)); // rim crest
                    else if (ao > 1.0f) col = Color.Lerp(deep, acc, 0.85f) * (0.6f + 0.4f * Mathf.Clamp01(0.5f + facing)); // gold hairline
                    else if (ao > 0.9f) col = Color.Lerp(rim * 0.55f, rim * 1.05f, Mathf.Clamp01(0.5f + facing * 0.8f));
                    else if (ao > 0.7f) col = Color.Lerp(deep * 0.55f, groove * 1.15f, Mathf.Clamp01(0.5f - facing * 0.9f)); // inner wall slope: lit when it faces away from outward normal
                    else if (ao > 0.5f) col = Color.Lerp(deep, groove, 0.45f + 0.45f * Mathf.Clamp01(0.5f - facing * 0.9f)) * (0.85f - 0.3f * Mathf.Clamp01(facing)); // cast shadow of the lit wall
                    else col = Color.Lerp(groove, deep, 0.35f + 0.6f * (1f - ao * 2f) * 0.5f);
                    col.a = ao > 1.12f ? col.a : 1f;
                    if (ao <= 1.12f) col.a = 1f;
                    cols[s * Rows + r] = col;
                }
            }

            int ti = 0;
            for (int s = 0; s < m - 1; s++)
                for (int r = 0; r < Rows - 1; r++)
                {
                    int a0 = s * Rows + r, a1 = a0 + 1, b0 = a0 + Rows, b1 = b0 + 1;
                    tris[ti++] = a0; tris[ti++] = b0; tris[ti++] = a1;
                    tris[ti++] = a1; tris[ti++] = b0; tris[ti++] = b1;
                }
            var mesh = new Mesh { name = "track" };
            mesh.indexFormat = verts.Length > 60000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
