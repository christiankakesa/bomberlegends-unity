using UnityEngine;

namespace BomberLegends.Services
{
    /// <summary>
    /// Leaves the game, whatever it is currently running inside.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Application.Quit()"/> alone is not enough. It does nothing at all in the Editor,
    /// so a QUIT button wired straight to it appears broken to whoever is testing — which is exactly
    /// how this went unnoticed. Play mode has to be stopped explicitly instead.
    /// </para>
    /// <para>
    /// A static rather than a seventh entry on <see cref="GameContext"/>: there is no second
    /// implementation to swap in and nothing meaningful to assert about it, so an interface would be
    /// ceremony. If quitting ever has to do real work — flushing a run in progress, confirming with
    /// the player — it becomes a service then.
    /// </para>
    /// </remarks>
    public static class ApplicationExit
    {
        /// <summary>Ends the session.</summary>
        public static void Request()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
