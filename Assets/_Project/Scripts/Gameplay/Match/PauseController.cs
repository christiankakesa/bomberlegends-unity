using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// Opens and closes the pause menu, and holds the match still while it is open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pause is read straight from the devices rather than travelling through
    /// <c>PlayerIntent</c>. That struct is the simulation's entire input surface and the format
    /// replays are recorded in; pausing changes nothing inside the simulation, so putting it there
    /// would widen the record with something the rules never read.
    /// </para>
    /// <para>
    /// Escape doubles as the Android back button, so the same binding gives a touch build its way
    /// out without any extra control on screen.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PauseController : MonoBehaviour
    {
        private MatchRunner? _runner;
        private PauseMenuView? _menu;
        private Func<bool>? _blocked;
        private Action? _quit;

        /// <summary>Whether the match is currently paused.</summary>
        public bool IsPaused => _menu != null && _menu.IsShowing;

        /// <summary>Wires the controller to the match it governs.</summary>
        /// <param name="blocked">
        /// Reports when pausing must be refused — while the between-arena screen is up, where the
        /// match is already stopped and a second overlay would simply cover the first.
        /// </param>
        public void Begin(MatchRunner runner, PauseMenuView menu, Action quit, Func<bool>? blocked = null)
        {
            _runner = runner;
            _menu = menu;
            _quit = quit;
            _blocked = blocked;

            _menu.Resumed += Resume;
            _menu.Quit += OnQuit;
        }

        private void OnDestroy()
        {
            if (_menu != null)
            {
                _menu.Resumed -= Resume;
                _menu.Quit -= OnQuit;
            }
        }

        /// <summary>
        /// Opens the menu, or closes it if it is already open.
        /// </summary>
        /// <remarks>
        /// Public so the on-screen button and the Start button run the same code. Two paths into
        /// pausing is two things to keep in step, and they always drift.
        /// </remarks>
        public void TogglePause()
        {
            if (_menu == null || _runner == null)
            {
                return;
            }

            if (IsPaused)
            {
                Resume();
                return;
            }

            if (_blocked != null && _blocked())
            {
                return;
            }

            _runner.IsPaused = true;
            _menu.Show();
        }

        private void Update()
        {
            if (WasPausePressed())
            {
                TogglePause();
            }
        }

        private static bool WasPausePressed()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
            {
                return true;
            }

            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        private void Resume()
        {
            if (_runner != null)
            {
                _runner.IsPaused = false;
            }

            _menu?.Hide();
        }

        private void OnQuit()
        {
            // Unpaused first so the runner is never left frozen if the transition is interrupted.
            Resume();
            _quit?.Invoke();
        }
    }
}
