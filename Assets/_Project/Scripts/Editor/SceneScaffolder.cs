using System.Collections.Generic;
using System.IO;
using BomberLegends.Bootstrap;
using BomberLegends.Data.Balance;
using BomberLegends.Gameplay.Board;
using BomberLegends.Gameplay.Camera;
using BomberLegends.Gameplay.Match;
using BomberLegends.Gameplay.Player;
using BomberLegends.Input;
using BomberLegends.Services.Diagnostics;
using BomberLegends.Services.Save;
using BomberLegends.Services.Scenes;
using BomberLegends.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Creates the three shipped scenes with their required contents and registers them in the build
    /// settings.
    /// </summary>
    /// <remarks>
    /// Scene assets are binary-ish YAML that merges badly and is tedious to review, so their
    /// structure is defined here in code. Running this on a fresh checkout reproduces the exact
    /// scene layout the project expects, and the diff of a structural change is a code diff.
    /// </remarks>
    public static class SceneScaffolder
    {
        private const string SceneDirectory = "Assets/_Project/Scenes";

        /// <summary>Creates or replaces Bootstrap, Hub and Match, then registers them for building.</summary>
        [MenuItem("Bomber Legends/Scenes/Rebuild Scene Scaffolding")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(SceneDirectory);

            var bootstrapPath = CreateBootstrapScene();
            var hubPath = CreateHubScene();
            var matchPath = CreateMatchScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(bootstrapPath, true),
                new EditorBuildSettingsScene(hubPath, true),
                new EditorBuildSettingsScene(matchPath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Scenes] Scaffolding rebuilt and registered in the build settings.");
        }

        private static string CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The only AudioListener and the only EventSystem in the project live here, on the scene
            // that is never unloaded. Duplicates of either are a classic source of silent bugs.
            var root = new GameObject("Bootstrap");
            root.AddComponent<AudioListener>();
            var bootstrap = root.AddComponent<GameBootstrap>();
            var lifecycle = root.AddComponent<SaveLifecycleHandler>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var loadingScreen = CreateLoadingScreen();
            CreateLogOverlay();

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_loadingScreen").objectReferenceValue = loadingScreen;
            serialized.FindProperty("_saveLifecycle").objectReferenceValue = lifecycle;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveScene(scene, SceneService.NameOf(SceneId.Bootstrap));
        }

        private static LoadingScreenView CreateLoadingScreen()
        {
            var canvasObject = new GameObject(
                "LoadingScreen",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(LoadingScreenView));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Above every gameplay and HUD canvas, so nothing can draw over the fade.
            canvas.sortingOrder = 1000;

            ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            var fill = CreateFullScreenImage("Fill", canvasObject.transform, new Color(0.04f, 0.03f, 0.09f));
            fill.raycastTarget = true;

            return canvasObject.GetComponent<LoadingScreenView>();
        }

        /// <summary>Builds the on-screen error readout used to diagnose device-only failures.</summary>
        private static void CreateLogOverlay()
        {
            var canvasObject = new GameObject(
                "LogOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(DeviceLogOverlay));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Above even the loading screen: an error that happens during a transition is exactly
            // the one worth seeing.
            canvas.sortingOrder = 2000;
            ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textObject = new GameObject("Output", typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(24f, 24f);
            rect.offsetMax = new Vector2(-24f, -24f);

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.alignment = TextAnchor.LowerLeft;
            text.color = new Color(1f, 0.45f, 0.4f);
            text.raycastTarget = false;

            var serialized = new SerializedObject(canvasObject.GetComponent<DeviceLogOverlay>());
            serialized.FindProperty("_output").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string CreateHubScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Color(0.05f, 0.04f, 0.11f));

            var root = new GameObject("HubInstaller");
            var installer = root.AddComponent<HubInstaller>();

            var canvas = CreateScreenCanvas("HubCanvas");
            var play = CreateButton("PlayButton", canvas.transform, "PLAY", new Vector2(0f, 0f));

            var serialized = new SerializedObject(installer);
            serialized.FindProperty("_playButton").objectReferenceValue = play;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveScene(scene, SceneService.NameOf(SceneId.Hub));
        }

        private static string CreateMatchScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The rig positions and orients this at runtime; the values here only make the scene
            // view sensible to open.
            var matchCamera = CreateCamera(new Color(0.05f, 0.07f, 0.11f), new Vector3(6f, 10f, -4f));
            matchCamera.orthographic = false;
            matchCamera.fieldOfView = 45f;
            matchCamera.nearClipPlane = 0.3f;
            matchCamera.farClipPlane = 120f;
            CreateSunlight();
            var cameraRig = matchCamera.gameObject.AddComponent<MatchCameraRig>();

            var rigSerialized = new SerializedObject(cameraRig);
            rigSerialized.FindProperty("_camera").objectReferenceValue = matchCamera;
            rigSerialized.ApplyModifiedPropertiesWithoutUndo();

            var root = new GameObject("Match");
            var installer = root.AddComponent<MatchInstaller>();
            var runner = root.AddComponent<MatchRunner>();

            var viewsObject = new GameObject("Views");
            viewsObject.transform.SetParent(root.transform, false);
            var views = viewsObject.AddComponent<MatchViewSynchroniser>();

            var boardObject = new GameObject("Board");
            boardObject.transform.SetParent(root.transform, false);
            var boardRenderer = boardObject.AddComponent<BoardRenderer>();

            // The view builds its own mesh child at run time. Its shader survives the build because
            // ShaderInclusionTool lists it explicitly — nothing in a scene references it, and a
            // stripped shader draws the interface and nothing else, on device only.
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(root.transform, false);
            var playerView = playerObject.AddComponent<PlayerView>();

            var canvas = CreateScreenCanvas("MatchCanvas");
            var quit = CreateButton("QuitButton", canvas.transform, "QUIT", new Vector2(760f, 420f));
            var joystick = CreateJoystick(canvas.transform);
            var bombButton = CreateActionButton(canvas.transform);
            var hud = CreateHud(canvas.transform, root.transform);

            var serialized = new SerializedObject(installer);
            serialized.FindProperty("_quitButton").objectReferenceValue = quit;
            serialized.FindProperty("_runner").objectReferenceValue = runner;
            serialized.FindProperty("_boardRenderer").objectReferenceValue = boardRenderer;
            serialized.FindProperty("_playerView").objectReferenceValue = playerView;
            serialized.FindProperty("_joystick").objectReferenceValue = joystick;
            serialized.FindProperty("_inputFeel").objectReferenceValue = LoadOrCreateInputFeel();
            serialized.FindProperty("_cameraRig").objectReferenceValue = cameraRig;
            serialized.FindProperty("_views").objectReferenceValue = views;
            serialized.FindProperty("_bombButton").objectReferenceValue = bombButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var viewsSerialized = new SerializedObject(views);
            viewsSerialized.FindProperty("_hud").objectReferenceValue = hud;
            viewsSerialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveScene(scene, SceneService.NameOf(SceneId.Match));
        }

        /// <summary>Adds a single directional light, so the greybox geometry reads as solid.</summary>
        private static void CreateSunlight()
        {
            var sun = new GameObject("Sunlight", typeof(Light));
            var light = sun.GetComponent<Light>();

            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;

            sun.transform.rotation = Quaternion.Euler(52f, 138f, 0f);
        }

        /// <summary>Creates the tuning asset if it does not exist yet, so the scene has one to point at.</summary>
        private static InputFeelConfig LoadOrCreateInputFeel()
        {
            const string directory = "Assets/_Project/Data/Balance";
            const string path = directory + "/InputFeel.asset";

            var existing = AssetDatabase.LoadAssetAtPath<InputFeelConfig>(path);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(directory);
            var asset = ScriptableObject.CreateInstance<InputFeelConfig>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Builds the health and enemy-count readout.</summary>
        private static MatchHudView CreateHud(Transform canvas, Transform matchRoot)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var textObject = new GameObject("Readout", typeof(Text));
            textObject.transform.SetParent(canvas, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -30f);
            rect.sizeDelta = new Vector2(1700f, 70f);

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 42;
            text.alignment = TextAnchor.UpperLeft;

            // The readout grew to carry arena, health, enemies, skill charges and the build. Wrapped
            // and clipped, everything past "enemies" was drawn off-screen on device.
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.color = new Color(0.95f, 0.95f, 1f);
            text.raycastTarget = false;
            text.text = "HP --";

            var hudObject = new GameObject("Hud", typeof(MatchHudView));
            hudObject.transform.SetParent(matchRoot, false);

            var serialized = new SerializedObject(hudObject.GetComponent<MatchHudView>());
            serialized.FindProperty("_output").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return hudObject.GetComponent<MatchHudView>();
        }

        /// <summary>Builds the bomb button in the bottom-right thumb zone.</summary>
        private static ActionButton CreateActionButton(Transform parent)
        {
            var buttonObject = new GameObject("BombButton", typeof(Image), typeof(ActionButton));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 240f);

            // Mirrored from the stick, within comfortable reach of the right thumb.
            rect.anchoredPosition = new Vector2(-240f, 240f);

            buttonObject.GetComponent<Image>().color = new Color(0.95f, 0.35f, 0.25f, 0.75f);
            AddLabel(buttonObject.transform, "BOMB");

            return buttonObject.GetComponent<ActionButton>();
        }

        /// <summary>
        /// Builds the on-screen thumbstick in the bottom-left thumb zone.
        /// </summary>
        private static VirtualJoystick CreateJoystick(Transform parent)
        {
            var stickObject = new GameObject("Joystick", typeof(Image), typeof(VirtualJoystick));
            stickObject.transform.SetParent(parent, false);

            var rect = stickObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 300f);
            rect.anchoredPosition = new Vector2(240f, 240f);

            var background = stickObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.12f);

            var handleObject = new GameObject("Handle", typeof(Image));
            handleObject.transform.SetParent(stickObject.transform, false);
            var handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(120f, 120f);
            handleObject.GetComponent<Image>().color = new Color(0.15f, 0.85f, 0.85f, 0.55f);
            handleObject.GetComponent<Image>().raycastTarget = false;

            var joystick = stickObject.GetComponent<VirtualJoystick>();
            var serialized = new SerializedObject(joystick);
            serialized.FindProperty("_handle").objectReferenceValue = handleRect;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return joystick;
        }

        private static UnityEngine.Camera CreateCamera(
            Color background, Vector3? position = null, float orthographicSize = 5f)
        {
            var cameraObject = new GameObject("MainCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            cameraObject.transform.position = position ?? new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static Canvas CreateScreenCanvas(string name)
        {
            var canvasObject = new GameObject(
                name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());
            return canvas;
        }

        private static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // Landscape reference resolution: the game is landscape-only on mobile.
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static Image CreateFullScreenImage(string name, Transform parent, Color colour)
        {
            var imageObject = new GameObject(name, typeof(Image));
            imageObject.transform.SetParent(parent, false);

            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = imageObject.GetComponent<Image>();
            image.color = colour;
            return image;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
        {
            var buttonObject = new GameObject(name, typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 120f);
            rect.anchoredPosition = position;

            buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.85f, 0.85f);

            AddLabel(buttonObject.transform, label);
            return buttonObject.GetComponent<Button>();
        }

        private static void AddLabel(Transform parent, string label)
        {
            // The built-in font is used deliberately: TextMeshPro needs its essential resources
            // imported through a dialog, which does not exist in a batch-mode scaffold run. Real
            // typography arrives with the UI pass.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                return;
            }

            var textObject = new GameObject("Label", typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.04f, 0.03f, 0.09f);
            text.raycastTarget = false;
        }

        private static string SaveScene(Scene scene, string name)
        {
            var path = $"{SceneDirectory}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
