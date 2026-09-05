using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Configures and produces the Android build.
    /// </summary>
    /// <remarks>
    /// Settings are applied in code rather than left to whatever the project happened to be saved
    /// with, so a build from a clean checkout is identical to a build from a developer machine.
    /// </remarks>
    public static class AndroidBuildTool
    {
        private const string OutputDirectory = "Builds/Android";
        private const string ApplicationIdentifier = "com.christiankakesa.bomberlegends";

        /// <summary>Applies the Android player settings this project requires.</summary>
        [MenuItem("Bomber Legends/Android/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            var android = UnityEditor.Build.NamedBuildTarget.Android;

            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(android, ApplicationIdentifier);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.forceInternetPermission = false;

            // Landscape only, both ways up, so the device can be rotated without the HUD flipping
            // into a layout it was never designed for.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            AssetDatabase.SaveAssets();
            Debug.Log("[Build] Android player settings applied.");
        }

        /// <summary>Builds a development APK. Invoked from the command line by the build script.</summary>
        public static void BuildDevelopment() => Build(development: true);

        /// <summary>Builds a release APK.</summary>
        [MenuItem("Bomber Legends/Android/Build Release APK")]
        public static void BuildRelease() => Build(development: false);

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

            Directory.CreateDirectory(OutputDirectory);
            var outputPath = Path.Combine(
                OutputDirectory, development ? "BomberLegends-dev.apk" : "BomberLegends.apk");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                // The development APK reaches for the Editor's profiler as it starts. On a device
                // with nothing listening the attempt fails silently and costs nothing; with the
                // Editor open over adb it is the T-036 baseline capture without a menu to find.
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging |
                      BuildOptions.ConnectWithProfiler
                    : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log(
                $"[Build] {summary.result} · {summary.totalSize / (1024 * 1024)} MB · " +
                $"{summary.totalTime.TotalSeconds:F0}s · {outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed: {summary.result}");
            }
        }
    }
}
