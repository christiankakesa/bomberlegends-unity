using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Configures and produces the Windows desktop build.
    /// </summary>
    /// <remarks>
    /// Settings are applied in code rather than left to whatever the project was last saved with, so
    /// a build from a clean checkout matches a build from a developer machine.
    /// </remarks>
    public static class WindowsBuildTool
    {
        private const string OutputDirectory = "Builds/Windows";
        private const string ExecutableName = "BomberLegends.exe";

        /// <summary>Applies the desktop player settings this project requires.</summary>
        [MenuItem("Bomber Legends/Windows/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            var windows = UnityEditor.Build.NamedBuildTarget.Standalone;

            // Mono rather than IL2CPP. The desktop build exists to be iterated on and handed round
            // quickly; IL2CPP roughly triples build time for a target that has no size or
            // certification pressure. Android keeps IL2CPP, where it is required.
            PlayerSettings.SetScriptingBackend(windows, ScriptingImplementation.Mono2x);

            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            // Kept on so a tester who hits something can send a log rather than describe it.
            PlayerSettings.usePlayerLog = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[Build] Windows player settings applied.");
        }

        /// <summary>Builds a development player. Invoked from the command line.</summary>
        public static void BuildDevelopment() => Build(development: true);

        /// <summary>
        /// Builds a release player.
        /// </summary>
        /// <remarks>
        /// The one to hand to a playtester. A development player carries the on-screen console,
        /// which turns any harmless warning into something a tester reports as a crash — and hides
        /// nothing that matters, because their machine is not the one being debugged.
        /// </remarks>
        [MenuItem("Bomber Legends/Windows/Build Release")]
        public static void BuildRelease() => Build(development: false);

        [MenuItem("Bomber Legends/Windows/Build Development")]
        private static void BuildDevelopmentMenu() => BuildDevelopment();

        private static void Build(bool development)
        {
            ApplyPlayerSettings();

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "No scenes are enabled in the build settings. The build would produce an empty app.");
            }

            // Each configuration gets its own folder. Unity writes a player as a directory of
            // supporting files, so building one over the other leaves the previous build's data
            // behind and produces a player that runs but is subtly not what was asked for.
            var directory = Path.Combine(OutputDirectory, development ? "Development" : "Release");
            Directory.CreateDirectory(directory);

            var outputPath = Path.Combine(directory, ExecutableName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log(
                $"[Build] {summary.result} · {summary.totalSize / (1024 * 1024)} MB · " +
                $"{summary.totalTime.TotalSeconds:F0}s · {outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed: {summary.result}");
            }
        }
    }
}
