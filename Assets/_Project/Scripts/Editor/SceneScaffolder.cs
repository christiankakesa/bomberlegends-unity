using System.Collections.Generic;
using System.IO;
using BomberLegends.Bootstrap;
using BomberLegends.Gameplay.Match;
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

            CreateCamera(new Color(0.02f, 0.06f, 0.09f));

            var root = new GameObject("MatchInstaller");
            var installer = root.AddComponent<MatchInstaller>();

            var canvas = CreateScreenCanvas("MatchCanvas");
            var quit = CreateButton("QuitButton", canvas.transform, "QUIT", new Vector2(0f, -160f));

            var serialized = new SerializedObject(installer);
            serialized.FindProperty("_quitButton").objectReferenceValue = quit;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveScene(scene, SceneService.NameOf(SceneId.Match));
        }

        private static void CreateCamera(Color background)
        {
            var cameraObject = new GameObject("MainCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
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
