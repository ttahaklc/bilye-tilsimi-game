using System;
using System.Collections.Generic;
using Bilye.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Bilye
{
    /// <summary>
    /// Owns one running Game and draws it with sprites: backdrop, groove mesh, portal, marbles (shadow/body/gloss),
    /// shooter, projectiles, aim guide, particles and score popups. Also routes touch input to the AimController.
    /// World units: 1 layout point = Unit (0.01) world units, origin at the screen centre, y up.
    /// </summary>
    public sealed class GameView : MonoBehaviour
    {
        public const float Unit = 0.01f;

        public Game game;
        public AimController aim;
        public bool demo;
        public bool paused;
        public bool inputEnabled = true;
        public Action<GameEvent> OnEvent;
        public Action<bool> OnEnded;      // true = won
        public WorldStyle style;
        public WorldTheme theme;
        public LevelConfig level;
        public bool endlessMode, infiniteMode;
        public float scale = 1f;          // pixels per layout point
        public float layoutW, layoutH;

        Camera cam;
        Backdrop backdrop;
        MeshFilter trackMf; MeshRenderer trackMr;
        Material vertexMat;
        MarbleSet marbles;
        ShooterSet shooter;
        Sprite[] portalSprites;
        Transform root;
        bool endedSent;
        Vector2Int lastScreen; Rect lastSafe;
        long seedUsed;

        // pools
        sealed class BallObj { public SpriteRenderer shadow, body, gloss; }
        readonly List<BallObj> ballObjs = new List<BallObj>();
        readonly List<BallObj> projObjs = new List<BallObj>();
        readonly List<SpriteRenderer> trailObjs = new List<SpriteRenderer>();
        BallObj loadedObj, nextObj;
        SpriteRenderer barrelSr, hubSr, coreSr, glowSr, armsSr, rimSr, raysSr, dangerSr;
        readonly List<SpriteRenderer> guide = new List<SpriteRenderer>();
        SpriteRenderer targetRing, guideBead;

        struct Particle { public SpriteRenderer sr; public Vector2 pos, vel; public float life, max, size; public int kind; public Color color; public bool alive; }
        readonly List<Particle> particles = new List<Particle>();
        sealed class Popup { public TextMeshPro tmp; public float life, max, scale; public Vector2 pos; public Color color; public bool alive; }
        readonly List<Popup> popups = new List<Popup>();

        public static Material SpriteMat;

        static float EaseOutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
        public Vector3 W(double x, double y) => new Vector3((float)(x - layoutW * 0.5) * Unit, (float)(layoutH * 0.5 - y) * Unit, 0f);

        // ---------------------------------------------------------------------------------------------

        public void Init(Camera camera)
        {
            cam = camera;
            root = new GameObject("GameRoot").transform;
            root.SetParent(transform, false);
            var bd = new GameObject("Backdrop");
            bd.transform.SetParent(root, false);
            backdrop = bd.AddComponent<Backdrop>();
            var tr = new GameObject("Track");
            tr.transform.SetParent(root, false);
            trackMf = tr.AddComponent<MeshFilter>();
            trackMr = tr.AddComponent<MeshRenderer>();
            trackMr.sortingOrder = -50;
            vertexMat = SpriteMat != null ? SpriteMat : new Material(Shader.Find("Sprites/Default"));
            trackMr.sharedMaterial = vertexMat;
            Backdrop.SpriteMat = SpriteMat;
        }

        SpriteRenderer Sr(string name, Sprite s, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
            if (SpriteMat != null) sr.sharedMaterial = SpriteMat;
            return sr;
        }

        BallObj MakeBallObj(int order, bool shadow = true)
        {
            var b = new BallObj();
            if (shadow) b.shadow = Sr("sh", marbles.shadow, order);
            b.body = Sr("body", marbles.body[0], order + 1);
            b.gloss = Sr("gloss", marbles.gloss, order + 2);
            return b;
        }

        // ---------------------------------------------------------------------------------------------
        // lifecycle
        // ---------------------------------------------------------------------------------------------

        public void Begin(LevelConfig lvl, bool endless, bool infinite, bool isDemo, long? seed = null)
        {
            Teardown();
            level = lvl; endlessMode = endless; infiniteMode = infinite; demo = isDemo; paused = false; endedSent = false;
            style = WorldStyles.Of(lvl.world, endless);
            theme = endless ? Levels.TabletTheme : (isDemo ? Levels.Worlds[0] : Levels.Worlds[Mathf.Clamp(lvl.world, 0, 9)]);
            marbles = MarbleArt.Get(style);
            shooter = ShooterArt.Get(style);
            portalSprites = ShooterArt.Portal(style);
            ComputeScreen();
            var lay = MakeLayout(lvl.ballScale);
            seedUsed = seed ?? (long)(UnityEngine.Random.value * 2e9f);
            game = new Game(new GameOptions
            {
                level = lvl, layout = lay, endless = endless, infinite = infinite, seed = seedUsed,
                onEvent = e => { OnEvent?.Invoke(e); },
            });
            game.god = demo || (Save.Data.devOn && Save.Data.devGod);
            game.onBurst = Burst; game.onRing = RingFx; game.onPopup = PopupFx;
            aim = new AimController(game, Save.Data.control == 1 ? ControlMode.Aim : ControlMode.Dial);
            BuildScene();
        }

        void Teardown()
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var c = root.GetChild(i);
                if (c.gameObject == backdrop.gameObject || c.gameObject == trackMf.gameObject) continue;
                Destroy(c.gameObject);
            }
            ballObjs.Clear(); projObjs.Clear(); trailObjs.Clear(); guide.Clear(); particles.Clear(); popups.Clear();
            loadedObj = nextObj = null;
        }

        public void Stop()
        {
            Teardown();
            game = null;
            if (trackMf != null) trackMf.sharedMesh = null;
        }

        void ComputeScreen()
        {
            lastScreen = new Vector2Int(Display.Width, Display.Height);
            lastSafe = Display.Safe;
            scale = Mathf.Max(0.5f, Mathf.Min(Display.Width, Display.Height) / 390f);
            layoutW = Display.Width / scale; layoutH = Display.Height / scale;
            if (cam != null) { cam.orthographicSize = layoutH * 0.5f * Unit; cam.transform.position = new Vector3(0, 0, -10); }
        }

        Layout MakeLayout(double ballScale)
        {
            var sa = Display.Safe;
            var ins = new Insets(
                (Display.Height - sa.yMax) / scale,   // top
                sa.yMin / scale,                      // bottom
                sa.xMin / scale,                      // left
                (Display.Width - sa.xMax) / scale);    // right
            return Layout.Compute(layoutW, layoutH, ins, ballScale);
        }

        void BuildScene()
        {
            backdrop.Build(style.motif, theme, layoutW * Unit, layoutH * Unit);
            RebuildTrack();

            // portal
            int order = -40;
            coreSr = Sr("core", portalSprites[0], order);
            glowSr = Sr("glow", portalSprites[1], order + 1);
            if (style.sun)
            {
                rimSr = Sr("rim", portalSprites[3], order + 2);
                raysSr = Sr("rays", portalSprites[4], order + 1);
            }
            else armsSr = Sr("arms", portalSprites[2], order + 2);

            // shooter
            barrelSr = Sr("barrel", shooter.barrel, 30);
            hubSr = Sr("hub", shooter.hub, 34);
            loadedObj = MakeBallObj(31, false);
            nextObj = MakeBallObj(31, false);
            barrelSr.enabled = hubSr.enabled = loadedObj.body.enabled = loadedObj.gloss.enabled = nextObj.body.enabled = nextObj.gloss.enabled = !demo;
            if (demo) { barrelSr.enabled = false; hubSr.enabled = false; }

            // aim guide
            for (int i = 0; i < 14; i++) guide.Add(Sr("g", Gfx.Dot, 29));
            targetRing = Sr("target", Gfx.Ring, 29);
            guideBead = Sr("bead", Gfx.Dot, 29);
            foreach (var g in guide) g.enabled = false;
            targetRing.enabled = guideBead.enabled = false;

            dangerSr = Sr("danger", Gfx.Vignette, 905);
            dangerSr.color = new Color(1, 0.1f, 0.15f, 0);
            dangerSr.transform.localScale = new Vector3(layoutW * Unit * 1.1f, layoutH * Unit * 1.1f, 1);
        }

        void RebuildTrack()
        {
            var mesh = TrackRenderer.Build(game, theme, (x, y) => W(x, y), Unit, endlessMode);
            if (trackMf.sharedMesh != null) Destroy(trackMf.sharedMesh);
            trackMf.sharedMesh = mesh;
            backdrop.Build(style.motif, theme, layoutW * Unit, layoutH * Unit);
        }

        // ---------------------------------------------------------------------------------------------
        // update
        // ---------------------------------------------------------------------------------------------

        void Update()
        {
            if (game == null) return;
            if (lastScreen.x != Display.Width || lastScreen.y != Display.Height || lastSafe != Display.Safe) OnScreenChanged();

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (!paused)
            {
                HandleInput();
                game.Update(dt);
                if (demo && game.phase != GamePhase.Playing && game.phase != GamePhase.Winning && game.phase != GamePhase.Losing) Begin(level, false, false, true, seedUsed + 1);
                if (demo && (game.phase == GamePhase.Won || game.phase == GamePhase.Lost)) Begin(level, false, false, true, seedUsed + 1);
                if (!endedSent && game != null && (game.phase == GamePhase.Won || game.phase == GamePhase.Lost) && !demo)
                {
                    endedSent = true;
                    OnEnded?.Invoke(game.phase == GamePhase.Won);
                }
            }
            if (game == null) return;
            UpdateParticles(paused ? 0f : dt);
            Render(paused ? 0f : dt);
        }

        void OnScreenChanged()
        {
            ComputeScreen();
            if (game == null) return;
            var lay = MakeLayout(level.ballScale);
            game.Resize(lay);
            RebuildTrack();
            dangerSr.transform.localScale = new Vector3(layoutW * Unit * 1.1f, layoutH * Unit * 1.1f, 1);
            // the hole moved: nothing else is cached
        }

        // ---------------------------------------------------------------------------------------------
        // input
        // ---------------------------------------------------------------------------------------------

        bool touching;
        public Vector2 lastTouchPts;

        void HandleInput()
        {
            if (!inputEnabled || demo || game.phase != GamePhase.Playing) { if (touching) { aim.Cancel(); touching = false; } return; }
            var ptr = Pointer.current;
            if (ptr == null) return;
            var sp = ptr.position.ReadValue();
            double px = sp.x / scale, py = (Display.Height - sp.y) / scale;
            lastTouchPts = new Vector2((float)px, (float)py);
            if (ptr.press.wasPressedThisFrame)
            {
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                bool inHud = py < game.layout.insets.top + game.layout.hudBand - 8;
                if (!overUi && !inHud) { touching = true; Fx(aim.Down(px, py)); }
            }
            else if (touching && ptr.press.isPressed) aim.Move(px, py);
            if (touching && ptr.press.wasReleasedThisFrame) { touching = false; Fx(aim.Up(px, py)); }
        }

        void Fx(TouchOutcome o) { }

        // ---------------------------------------------------------------------------------------------
        // render
        // ---------------------------------------------------------------------------------------------

        BallObj Obj(List<BallObj> pool, int i, int order)
        {
            while (pool.Count <= i) pool.Add(MakeBallObj(order));
            return pool[i];
        }

        void SetBall(BallObj o, int color, Vector3 pos, float d, float rotDeg, float scaleK, bool shadow, int alphaPct = 100)
        {
            float s = d * Unit / MarbleSet.Fill * scaleK;
            if (o.shadow != null)
            {
                o.shadow.enabled = shadow;
                if (shadow)
                {
                    o.shadow.transform.localPosition = pos + new Vector3(0.07f, -0.10f, 0) * d * Unit * scaleK;
                    o.shadow.transform.localScale = Vector3.one * s * 1.05f;
                }
            }
            o.body.enabled = o.gloss.enabled = true;
            o.body.sprite = marbles.body[color % 6];
            o.body.transform.localPosition = pos; o.body.transform.localScale = Vector3.one * s; o.body.transform.localRotation = Quaternion.Euler(0, 0, rotDeg);
            o.gloss.transform.localPosition = pos; o.gloss.transform.localScale = Vector3.one * s;
        }

        void Hide(BallObj o) { if (o.shadow != null) o.shadow.enabled = false; o.body.enabled = false; o.gloss.enabled = false; }

        void Render(float dt)
        {
            var g = game;
            float d = (float)g.d;
            var clip = g.layout.clip;
            float pad = d;

            // chain
            int n = 0;
            for (int i = 0; i < g.balls.Count; i++)
            {
                var b = g.balls[i];
                double x = b.x + b.offX + b.tx * b.offS, y = b.y + b.offY + b.ty * b.offS;
                if (x < clip.x - pad || x > clip.x + clip.w + pad || y < clip.y - pad || y > clip.y + clip.h + pad) continue;
                var o = Obj(ballObjs, n++, 10);
                float sc = b.appear < 1 ? 0.55f + 0.45f * EaseOutBack((float)b.appear) : 1f;
                float rot = -(float)((b.s - b.offS) / (d * 0.5)) * Mathf.Rad2Deg;
                SetBall(o, b.color, W(x, y), d, rot, sc, true);
            }
            for (int i = n; i < ballObjs.Count; i++) Hide(ballObjs[i]);

            RenderPortal(g, d);
            if (!demo) RenderShooter(g, d);
            RenderProjectiles(g, d);
            RenderGuide(g, d);

            // danger vignette
            float danger = (float)g.danger;
            var dc = dangerSr.color; dc.a = Mathf.Clamp01((danger - 0.45f) * 1.5f) * (0.35f + 0.25f * Mathf.Sin((float)g.time * (6f + danger * 6f))); dangerSr.color = dc;
        }

        void RenderPortal(Game g, float d)
        {
            var hp = g.HolePosition();
            var pos = W(hp.x, hp.y);
            float danger = (float)g.danger, t = (float)g.time;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * (3f + danger * 7f));
            float k = (1.45f + 0.1f * pulse + danger * 0.2f) / 1.45f;
            float size = d * Unit * 4.35f * k;
            bool hot = danger > 0.7f;
            Color tint = hot ? new Color(1f, 0.24f, 0.31f) : style.portal;
            coreSr.transform.localPosition = pos; coreSr.transform.localScale = Vector3.one * size;
            glowSr.transform.localPosition = pos; glowSr.transform.localScale = Vector3.one * size;
            glowSr.color = new Color(tint.r, tint.g, tint.b, 0.55f + 0.25f * pulse);
            if (style.sun)
            {
                rimSr.transform.localPosition = pos; rimSr.transform.localScale = Vector3.one * size * 0.96f;
                raysSr.transform.localPosition = pos; raysSr.transform.localScale = Vector3.one * size * 0.96f;
                raysSr.transform.localRotation = Quaternion.Euler(0, 0, -t * (14f + danger * 40f));
                raysSr.color = new Color(1f, 0.86f, 0.45f, 1f);
            }
            else
            {
                armsSr.transform.localPosition = pos; armsSr.transform.localScale = Vector3.one * size;
                armsSr.transform.localRotation = Quaternion.Euler(0, 0, -t * (80f + danger * 140f));
                armsSr.color = new Color(tint.r, tint.g, tint.b, 0.55f + 0.3f * pulse);
            }
        }

        void RenderShooter(Game g, float d)
        {
            var c = g.layout.center;
            var pos = W(c.x, c.y);
            float ang = -(float)g.turretAngle;                 // y flip
            var rot = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg);
            float u = d * Unit;
            var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0);
            barrelSr.transform.localPosition = pos - dir * (float)g.recoil * u * 0.2f;
            barrelSr.transform.localRotation = rot;
            barrelSr.transform.localScale = Vector3.one * u;
            hubSr.transform.localPosition = pos; hubSr.transform.localRotation = rot; hubSr.transform.localScale = Vector3.one * u;

            bool live = g.phase == GamePhase.Playing || g.phase == GamePhase.Winning;
            if (live)
            {
                float lr = EaseOutBack(Mathf.Min(1f, (float)g.reload));
                SetBall(loadedObj, g.loaded, pos + dir * u * 0.92f, d, 0, 0.85f * (0.4f + 0.6f * lr), false);
                SetBall(nextObj, g.next, pos - dir * u * 0.98f, d, 0, 0.5f, false);
            }
            else { Hide(loadedObj); Hide(nextObj); }
        }

        void RenderProjectiles(Game g, float d)
        {
            int n = 0, tn = 0;
            foreach (var p in g.projectiles)
            {
                var o = Obj(projObjs, n++, 20);
                var pos = W(p.x, p.y);
                SetBall(o, p.color, pos, d, -(float)p.age * 720f, 1f, true);
                float len = Mathf.Sqrt((float)(p.vx * p.vx + p.vy * p.vy)); if (len < 0.001f) len = 1;
                float ux = (float)p.vx / len, uy = -(float)p.vy / len;
                for (int i = 1; i <= 3; i++)
                {
                    while (trailObjs.Count <= tn) trailObjs.Add(Sr("trail", Gfx.Glow, 19));
                    var t = trailObjs[tn++];
                    t.enabled = true;
                    t.transform.localPosition = pos - new Vector3(ux, uy, 0) * d * Unit * 0.38f * i;
                    t.transform.localScale = Vector3.one * d * Unit * (0.95f - i * 0.16f) * 1.6f;
                    var gc = Pal.Balls[p.color % 6].glow; gc.a = new[] { 0.55f, 0.35f, 0.2f }[i - 1]; t.color = gc;
                }
            }
            for (int i = n; i < projObjs.Count; i++) Hide(projObjs[i]);
            for (int i = tn; i < trailObjs.Count; i++) trailObjs[i].enabled = false;
        }

        void RenderGuide(Game g, float d)
        {
            bool on = aim != null && aim.active && g.phase == GamePhase.Playing && !demo;
            foreach (var s in guide) s.enabled = false;
            targetRing.enabled = guideBead.enabled = false;
            if (!on) return;
            bool dial = aim.mode == ControlMode.Dial;
            var c = g.layout.center;
            double a = g.aimAngle, dx = Math.Cos(a), dy = Math.Sin(a);
            int hit = g.RayHit(a, out double dist);
            var pal = Pal.Balls[g.loaded % 6];
            if (dial)
            {
                double start = d * 1.75, end = Math.Max(start + d, Math.Min(dist - d * 0.55, d * 14));
                int N = guide.Count;
                for (int i = 0; i < N; i++)
                {
                    double t0 = start + (end - start) * (i + 0.4) / N;
                    var sr = guide[i];
                    sr.enabled = true;
                    sr.transform.localPosition = W(c.x + dx * t0, c.y + dy * t0);
                    float k = 1f - i / (float)N;
                    sr.transform.localScale = Vector3.one * d * Unit * (0.07f + 0.10f * k);
                    sr.color = pal.glow.A(0.9f * Mathf.Pow(k, 1.2f));
                }
                guideBead.enabled = true;
                guideBead.transform.localPosition = W(c.x + dx * start, c.y + dy * start);
                guideBead.transform.localScale = Vector3.one * d * Unit * 0.2f;
                guideBead.color = pal.glow.A(0.95f);
            }
            if (hit >= 0)
            {
                var b = g.balls[hit];
                targetRing.enabled = true;
                targetRing.transform.localPosition = W(b.x, b.y);
                targetRing.transform.localScale = Vector3.one * d * Unit * 1.3f;
                targetRing.color = pal.glow.A(0.55f + 0.25f * Mathf.Sin((float)g.time * 10f));
            }
        }

        // ---------------------------------------------------------------------------------------------
        // effects
        // ---------------------------------------------------------------------------------------------

        void AddParticle(int kind, Vector2 pos, Vector2 vel, float life, float size, Color col)
        {
            if (particles.Count > 360) return;
            int slot = -1;
            for (int i = 0; i < particles.Count; i++) if (!particles[i].alive) { slot = i; break; }
            Sprite sp = kind == 0 ? Gfx.Dot : kind == 1 ? Gfx.Spark : Gfx.Ring;
            var p = slot >= 0 ? particles[slot] : new Particle();
            if (p.sr == null) p.sr = Sr("fx", sp, 40);
            p.sr.sprite = sp; p.sr.enabled = true; p.sr.sortingOrder = kind == 2 ? 41 : 42;
            p.pos = pos; p.vel = vel; p.life = p.max = life; p.size = size; p.kind = kind; p.color = col; p.alive = true;
            if (slot >= 0) particles[slot] = p; else particles.Add(p);
        }

        void Burst(double x, double y, int color, int n)
        {
            if (demo) return;
            var pal = Pal.Balls[color % 6];
            float d = (float)game.d;
            for (int i = 0; i < n; i++)
            {
                float a = UnityEngine.Random.value * 6.2832f, sp = (60f + UnityEngine.Random.value * 220f) * Unit * (d / 30f);
                var vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp;
                AddParticle(i % 4 == 0 ? 1 : 0, (Vector2)W(x, y), vel, 0.45f + UnityEngine.Random.value * 0.4f, d * Unit * (i % 4 == 0 ? 0.55f : 0.22f), i % 4 == 0 ? Color.white : pal.light);
            }
        }

        void RingFx(double x, double y, int color, double size)
        {
            if (demo) return;
            AddParticle(2, (Vector2)W(x, y), Vector2.zero, 0.5f, (float)size * Unit, Pal.Balls[color % 6].glow);
        }

        void PopupFx(string text, double x, double y, uint col, double sc)
        {
            if (demo) return;
            Popup p = null;
            foreach (var q in popups) if (!q.alive) { p = q; break; }
            if (p == null)
            {
                if (popups.Count > 12) return;
                var go = new GameObject("popup");
                go.transform.SetParent(root, false);
                var t = go.AddComponent<TextMeshPro>();
                t.font = Fonts.Display; t.alignment = TextAlignmentOptions.Center; t.enableWordWrapping = false;
                t.sortingOrder = 60; t.outlineWidth = 0.28f; t.outlineColor = new Color32(8, 14, 24, 255);
                t.rectTransform.sizeDelta = new Vector2(8, 2);
                p = new Popup { tmp = t }; popups.Add(p);
            }
            p.alive = true; p.life = p.max = 1.0f; p.scale = (float)sc; p.pos = (Vector2)W(x, y);
            p.color = new Color(((col >> 16) & 255) / 255f, ((col >> 8) & 255) / 255f, (col & 255) / 255f, 1);
            p.tmp.text = text; p.tmp.color = p.color; p.tmp.gameObject.SetActive(true);
        }

        void UpdateParticles(float dt)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                if (!p.alive) continue;
                p.life -= dt;
                if (p.life <= 0) { p.alive = false; p.sr.enabled = false; particles[i] = p; continue; }
                float k = p.life / p.max;
                p.pos += p.vel * dt; p.vel *= 1f - 2.2f * dt;
                p.sr.transform.localPosition = p.pos;
                if (p.kind == 2)
                {
                    float t = 1f - k;
                    p.sr.transform.localScale = Vector3.one * p.size * (0.35f + t * 0.75f) * 1.15f;
                    p.sr.color = p.color.A(Mathf.Min(1f, k * 0.9f));
                }
                else
                {
                    p.sr.transform.localScale = Vector3.one * p.size * (0.4f + k * 0.9f);
                    p.sr.color = p.color.A(Mathf.Min(1f, k * 1.4f));
                    if (p.kind == 1) p.sr.transform.localRotation = Quaternion.Euler(0, 0, k * 120f);
                }
                particles[i] = p;
            }
            float d = (float)game.d;
            foreach (var q in popups)
            {
                if (!q.alive) continue;
                q.life -= dt;
                if (q.life <= 0) { q.alive = false; q.tmp.gameObject.SetActive(false); continue; }
                float k = q.life / q.max, age = 1f - k;
                float pop = age < 0.18f ? EaseOutBack(age / 0.18f) : 1f;
                float alpha = k < 0.3f ? k / 0.3f : 1f;
                q.pos.y += 38f * Unit * dt;
                q.tmp.transform.localPosition = new Vector3(q.pos.x, q.pos.y, 0);
                q.tmp.fontSize = d * Unit * 0.78f * q.scale * (0.5f + 0.5f * pop) * 10f;
                var c = q.color; c.a = alpha; q.tmp.color = c;
            }
        }
    }
}
