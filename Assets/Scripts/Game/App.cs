using System;
using System.Collections.Generic;
using Bilye.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Bilye
{
    public enum Screen_ { Menu, Levels, Settings, Game }

    /// <summary>Application root: camera + post effects, canvas, event system, screen flow. Everything is created from code.</summary>
    public sealed partial class App : MonoBehaviour
    {
        public static App I;
        Camera cam;
        Canvas canvas; CanvasScaler scaler;
        RectTransform screenRoot, overlayRoot;
        Image bgDim;
        GameView view;
        Screen_ current = Screen_.Menu;
        Vector2Int lastScreen; Rect lastSafe;
        float uiScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("BilyeApp");
            DontDestroyOnLoad(go);
            I = go.AddComponent<App>();
        }

        void Awake()
        {
            I = this;
            try { Init(); }
            catch (Exception e) { Dbg.LastError = e.ToString(); Debug.LogException(e); }
        }

        void Init()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Save.Load();
            Loc.Set(Save.Data.language >= 0 ? Save.Data.language : Loc.Detect());
            Fonts.Init();
            SetupCamera();
            SetupEventSystem();
            Audio.Create(transform);
            SetupCanvas();
            ApplyOrientation();
            view = new GameObject("GameView").AddComponent<GameView>();
            view.transform.SetParent(transform, false);
            view.Init(cam);
            Loc.Changed += OnLanguageChanged;
            ShowMenu();
            if (!Save.Data.tutorialSeen) { /* shown on first game start */ }
            Audio.I.StartMusic();
        }

        void SetupCamera()
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(c.gameObject);
            
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 4f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Pal.Ink;
            go.transform.position = new Vector3(0, 0, -10);
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            go.AddComponent<AudioListener>();
            var mat = Resources.Load<Material>("Mats/SpriteUnlit");
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            GameView.SpriteMat = mat;
            Backdrop.SpriteMat = mat;

            var volGo = new GameObject("PostFx");
            volGo.transform.SetParent(transform, false);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true);
            bloom.intensity.Override(0.28f);
            bloom.threshold.Override(0.92f);
            bloom.scatter.Override(0.55f);
            bloom.highQualityFiltering.Override(false);
            vol.sharedProfile = prof;
        }

        void SetupEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) foreach (var e in FindObjectsByType<EventSystem>(FindObjectsSortMode.None)) Destroy(e.gameObject);
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            var mod = go.AddComponent<InputSystemUIInputModule>();
            mod.AssignDefaultActions();
        }

        void SetupCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Ui.Root = canvas;
            bgDim = Ui.Img(go.transform, Gfx.Pixel, new Color(0.012f, 0.02f, 0.04f, 0.6f), 1, "dim");
            Ui.Fill(bgDim.rectTransform);
            var safe = Ui.Rt("Safe", go.transform);
            Ui.Safe = safe;
            screenRoot = Ui.Rt("Screen", safe); Ui.Fill(screenRoot);
            overlayRoot = Ui.Rt("Overlay", safe); Ui.Fill(overlayRoot);
            RefreshScale();
        }

        void RefreshScale()
        {
            lastScreen = new Vector2Int(Display.Width, Display.Height);
            lastSafe = Display.Safe;
            uiScale = Mathf.Max(0.5f, Mathf.Min(lastScreen.x, lastScreen.y) / 390f);
            scaler.scaleFactor = uiScale;
            Ui.Width = lastScreen.x / uiScale; Ui.Height = lastScreen.y / uiScale;
            var sa = lastSafe;
            var s = Ui.Safe;
            s.anchorMin = new Vector2(sa.xMin / lastScreen.x, sa.yMin / lastScreen.y);
            s.anchorMax = new Vector2(sa.xMax / lastScreen.x, sa.yMax / lastScreen.y);
            s.offsetMin = s.offsetMax = Vector2.zero;
            Ui.SafeW = sa.width / uiScale; Ui.SafeH = sa.height / uiScale;
        }

        void SetDim(float a) { if (bgDim != null) bgDim.color = new Color(0.012f, 0.02f, 0.04f, a); }

        // overlay stack -------------------------------------------------------------------------------
        readonly List<Action> overlays = new List<Action>();
        void Push(Action builder) { overlays.Add(builder); builder(); }
        void Pop() { if (overlays.Count > 0) overlays.RemoveAt(overlays.Count - 1); RebuildOverlayLayers(); }
        void ClearOverlays() { overlays.Clear(); Clear(overlayRoot); }
        void RebuildOverlayLayers() { Clear(overlayRoot); foreach (var b in overlays.ToArray()) b(); }

        public static void ApplyOrientation()
        {
            int o = Save.Data.orientation;
            if (UnityEngine.Application.isEditor) return;
            UnityEngine.Screen.autorotateToPortrait = o != 2;
            UnityEngine.Screen.autorotateToPortraitUpsideDown = false;
            UnityEngine.Screen.autorotateToLandscapeLeft = o != 1;
            UnityEngine.Screen.autorotateToLandscapeRight = o != 1;
            UnityEngine.Screen.orientation = o == 1 ? ScreenOrientation.Portrait : o == 2 ? ScreenOrientation.LandscapeLeft : ScreenOrientation.AutoRotation;
        }

        void OnLanguageChanged() { Rebuild(); }

        // test mode: render to a texture of any size with a simulated safe area ---------------------------
        public RenderTexture testRt;

        public void SetTestSize(int w, int h, int top, int bottom, int left, int right)
        {
            Display.test = true; Display.testW = w; Display.testH = h;
            Display.testSafe = new Rect(left, bottom, w - left - right, h - top - bottom);
            if (testRt != null) { cam.targetTexture = null; testRt.Release(); }
            testRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = testRt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f;
            RefreshScale(); Rebuild();
        }

        public void ClearTestSize()
        {
            Display.test = false;
            if (testRt != null) { cam.targetTexture = null; testRt.Release(); testRt = null; }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RefreshScale(); Rebuild();
        }

        // phone call, Control Center, app switcher: never let the chain run while the player is away
        void OnApplicationPause(bool paused) { if (paused) AutoPause(); }
        void OnApplicationFocus(bool focus) { if (!focus) AutoPause(); }
        void AutoPause() { if (current == Screen_.Game && view != null) Pause(); }

        void Update()
        {
            if (lastScreen.x != Display.Width || lastScreen.y != Display.Height || lastSafe != Display.Safe)
            {
                RefreshScale();
                Rebuild();
            }
            UpdateGameUi();
            BotUpdate();
            if (Save.Data.devOn && Save.Data.devFps) UpdateFps();
        }

        // ---------------------------------------------------------------------------------------------
        // screen management
        // ---------------------------------------------------------------------------------------------

        static void Clear(RectTransform r) { for (int i = r.childCount - 1; i >= 0; i--) { var c = r.GetChild(i).gameObject; c.SetActive(false); Destroy(c); } }

        public void Rebuild()
        {
            if (screenRoot == null) return;
            switch (current)
            {
                case Screen_.Menu: BuildMenu(); break;
                case Screen_.Levels: BuildLevels(); break;
                case Screen_.Settings: BuildSettings(); break;
                case Screen_.Game: BuildGameUi(); break;
            }
            RebuildOverlayLayers();
        }

        void ShowMenu()
        {
            current = Screen_.Menu;
            ClearOverlays();
            SaveEndlessBest();
            StartDemo();
            BuildMenu();
        }

        void StartDemo()
        {
            view.Begin(Levels.Menu, false, false, true, 42);
            view.inputEnabled = false;
        }

        void ShowLevels() { current = Screen_.Levels; ClearOverlays(); BuildLevels(); }
        void ShowSettings() { current = Screen_.Settings; ClearOverlays(); BuildSettings(); }

        // fps counter (dev)
        TextMeshProUGUI fpsText; float fpsAcc, fpsTime; int fpsFrames;
        void UpdateFps()
        {
            fpsAcc += Time.unscaledDeltaTime; fpsFrames++;
            if (fpsAcc < 0.5f) return;
            float fps = fpsFrames / fpsAcc; fpsAcc = 0; fpsFrames = 0;
            if (fpsText == null)
            {
                fpsText = Ui.Text(Ui.Safe, "", 11, Pal.Good, FontKind.Bold, TextAlignmentOptions.TopLeft, 0, false, false);
                Ui.Pin(fpsText.rectTransform, 0, 0, 120, 16, 6, 6);
                fpsText.rectTransform.anchorMin = fpsText.rectTransform.anchorMax = new Vector2(0, 0); fpsText.rectTransform.pivot = new Vector2(0, 0);
            }
            fpsText.text = $"{fps:0} fps";
        }
    }
}
