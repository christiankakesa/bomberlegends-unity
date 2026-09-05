using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Writes the short commit into every player build's version, and takes it back out again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs for any build — Android, WebGL, Windows, a menu item — because it hangs off the build
    /// pipeline rather than off one tool. The version becomes <c>1.0+ab12cd3</c>, with a <c>*</c>
    /// when the tree had uncommitted changes, and is restored once the build has finished so the
    /// project settings file never shows a diff for having been built.
    /// </para>
    /// <para>
    /// If the build fails part-way the restore may not run and the stamp is left in the project
    /// settings. That is a visible diff and harmless; the next successful build corrects it.
    /// </para>
    /// </remarks>
    public sealed class BuildStampProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const char Separator = '+';
        private const string NoGit = "nogit";

        private static string? _restore;

        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            _restore = PlayerSettings.bundleVersion;

            var at = _restore.IndexOf(Separator);
            var basis = at < 0 ? _restore : _restore.Substring(0, at);
            var stamp = Stamp();

            PlayerSettings.bundleVersion = $"{basis}{Separator}{stamp}";
            Debug.Log($"[Build] Stamped {PlayerSettings.bundleVersion}");
        }

        /// <inheritdoc />
        public void OnPostprocessBuild(BuildReport report)
        {
            if (_restore == null)
            {
                return;
            }

            PlayerSettings.bundleVersion = _restore;
            _restore = null;
        }

        /// <summary>The short commit, starred when the working tree is dirty.</summary>
        private static string Stamp()
        {
            var commit = Git("rev-parse --short HEAD");
            if (commit == null)
            {
                return NoGit;
            }

            var status = Git("status --porcelain");
            var dirty = status == null || status.Length > 0;

            return dirty ? commit + "*" : commit;
        }

        /// <summary>Runs one git command at the project root and returns its trimmed output, or null.</summary>
        private static string? Git(string arguments)
        {
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(info);
                if (process == null)
                {
                    return null;
                }

                var output = process.StandardOutput.ReadToEnd();

                if (!process.WaitForExit(5000) || process.ExitCode != 0)
                {
                    return null;
                }

                return output.Trim();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Build] git {arguments} failed: {exception.Message}");
                return null;
            }
        }
    }
}
