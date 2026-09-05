using UnityEngine;

namespace BomberLegends.Services.Diagnostics
{
    /// <summary>
    /// Which commit a build was made from, read back at run time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The build tools write the short commit into the player version as build metadata —
    /// <c>1.0+ab12cd3</c>, with a trailing <c>*</c> when the working tree was dirty — and restore
    /// the version once the build is done, so the project file never carries it. This reads it
    /// back. No generated source, no asset in a Resources folder: the version field already ships
    /// with every player and is exactly the place semantic versioning reserves for this.
    /// </para>
    /// <para>
    /// It exists because round 1 lost a defect to <i>"tester likely on an older build"</i>, and the
    /// round-3 deploy recorded no commit at all, which is why one of its findings can no longer be
    /// read. A tester's sheet and the developer's own log both carry this line.
    /// </para>
    /// </remarks>
    public static class BuildStamp
    {
        /// <summary>What separates the version from the commit it was built at.</summary>
        public const char Separator = '+';

        /// <summary>What is shown when a build was made without the stamp — the Editor, mostly.</summary>
        public const string Unstamped = "unstamped";

        /// <summary>The commit this player was built from, or <see cref="Unstamped"/>.</summary>
        public static string Commit => CommitIn(Application.version);

        /// <summary>The commit and the flavour together, for a status line.</summary>
        public static string Label => Describe(Application.version, Application.isEditor, Debug.isDebugBuild);

        /// <summary>The commit carried in a version string, or <see cref="Unstamped"/>.</summary>
        public static string CommitIn(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return Unstamped;
            }

            var at = version.IndexOf(Separator);

            return at < 0 || at == version.Length - 1 ? Unstamped : version.Substring(at + 1);
        }

        /// <summary>The version with any commit stripped off, which is what gets stamped onto.</summary>
        public static string Basis(string version)
        {
            var at = version.IndexOf(Separator);

            return at < 0 ? version : version.Substring(0, at);
        }

        /// <summary>Builds the status line from its parts.</summary>
        public static string Describe(string version, bool isEditor, bool isDebug) =>
            $"{CommitIn(version)} · {(isEditor ? "EDITOR" : isDebug ? "DEV" : "REL")}";
    }
}
