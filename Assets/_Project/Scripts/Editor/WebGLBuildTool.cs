using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Configures and produces the WebGL build.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The target that matters for a playtest. A link someone clicks reaches an order of magnitude
    /// more testers than an installer or a sideloaded APK, and the people put off by a download are
    /// exactly the ones whose "did they come back for a second run?" answer is worth having.
    /// </para>
    /// <para>
    /// Everything here is chosen for that: it has to load quickly, run on whatever browser the
    /// tester already has open, and be servable from any static host without configuration.
    /// </para>
    /// </remarks>
    public static class WebGLBuildTool
    {
        private const string OutputDirectory = "Builds/WebGL";

        /// <summary>Applies the web player settings this project requires.</summary>
        [MenuItem("Bomber Legends/WebGL/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            var web = UnityEditor.Build.NamedBuildTarget.WebGL;

            // Brotli with no JavaScript fallback, because the server this ships to is configured
            // for it: nginx sends Content-Encoding for .br files and, crucially, serves .wasm.br as
            // application/wasm.
            //
            // The fallback would defeat both. It renames everything to .unityweb, which matches none
            // of those rules, so the browser receives an opaque blob it must decompress in script
            // and cannot stream-compile. Turning it off hands decompression to the browser and lets
            // WebAssembly compile while it downloads.
            //
            // The cost is that the build no longer runs from a host that does not set the header.
            // Serve it locally with a static server that does, or flip this back for that case.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;

            // Cached in the browser, so a tester reloading or coming back tomorrow does not pay the
            // download again — which is otherwise the largest single cause of a lost second run.
            PlayerSettings.WebGL.dataCaching = true;

            PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.High);
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;

            // Kept running when the canvas loses focus. Going fullscreen swaps the canvas and
            // moves focus with it, and a player that stops rendering at that moment comes back as a
            // black screen rather than a paused one — which is what a browser build does here.
            PlayerSettings.runInBackground = true;

            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;

            AssetDatabase.SaveAssets();
            Debug.Log("[Build] WebGL player settings applied.");
        }

        /// <summary>Builds a development player, with full exception detail.</summary>
        public static void BuildDevelopment() => Build(development: true);

        /// <summary>Builds the player to hand to testers.</summary>
        [MenuItem("Bomber Legends/WebGL/Build Release")]
        public static void BuildRelease() => Build(development: false);

        [MenuItem("Bomber Legends/WebGL/Build Development")]
        private static void BuildDevelopmentMenu() => BuildDevelopment();

        private static void Build(bool development)
        {
            ApplyPlayerSettings();

            // Full stack traces cost both size and speed, and are worth it while the person hitting
            // a fault is the one who can read it. A tester cannot, so the release build trades them
            // away for a smaller download.
            PlayerSettings.WebGL.exceptionSupport = development
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "No scenes are enabled in the build settings. The build would produce an empty app.");
            }

            var directory = Path.Combine(OutputDirectory, development ? "Development" : "Release");
            Directory.CreateDirectory(directory);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = directory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log(
                $"[Build] {summary.result} · {summary.totalSize / (1024 * 1024)} MB · " +
                $"{summary.totalTime.TotalSeconds:F0}s · {directory}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {summary.result}");
            }
        }
    }
}
