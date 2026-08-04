using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using BomberLegends.Services.Scenes;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Makes pressing Play always start from the bootstrap scene, then continue into whichever
    /// scene the developer actually had open.
    /// </summary>
    /// <remarks>
    /// Without this, pressing Play in Match.unity produces a scene with no services, no save and no
    /// loading screen, so nothing works and the developer has to route through the hub every time.
    /// A small tool, but it protects the iteration loop for the life of the project.
    /// </remarks>
    [InitializeOnLoad]
    public static class PlayFromBootstrap
    {
        private const string MenuPath = "Bomber Legends/Play From Bootstrap";
        private const string EnabledKey = "BomberLegends.PlayFromBootstrap.Enabled";
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        private static bool _isRunningTests;

        static PlayFromBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += Apply;

            // Interactive test runs are not batch mode, so they need their own guard.
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestRunGuard());
        }

        /// <summary>Suspends the forced start scene for the duration of a test run.</summary>
        private sealed class TestRunGuard : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                _isRunningTests = true;
                EditorSceneManager.playModeStartScene = null;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                _isRunningTests = false;
                Apply();
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        [MenuItem(MenuPath, priority = 0)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            // Forcing a start scene hijacks *every* entry into play mode, including the test
            // runner's — which then never loads its own scene and hangs forever. Automated runs are
            // always batch mode, so they are excluded outright.
            if (!Enabled || Application.isBatchMode || _isRunningTests)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrap == null)
            {
                // The scene may not exist yet on a fresh checkout; nothing to do until it does.
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrap;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode || !Enabled || _isRunningTests)
            {
                return;
            }

            // Recorded before the scene is swapped for bootstrap, and read back by GameBootstrap so
            // the developer lands where they were working.
            var openScenePath = EditorSceneManager.GetActiveScene().path;
            var sceneName = string.IsNullOrEmpty(openScenePath)
                ? string.Empty
                : Path.GetFileNameWithoutExtension(openScenePath);

            SessionState.SetString(SceneService.EditorStartSceneKey, sceneName);
        }
    }
}
