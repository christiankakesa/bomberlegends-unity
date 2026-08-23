using BomberLegends.Input;
using UnityEngine;

namespace BomberLegends.Gameplay.Ui
{
    /// <summary>
    /// Shows the on-screen controls only while touch is the device actually being used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asking whether a touchscreen <i>exists</i> does not work. Desktop browsers advertise touch
    /// support whether or not any hardware is attached, so the Input System creates a
    /// <c>Touchscreen</c> device and a mouse-and-keyboard player gets a thumbstick drawn over their
    /// game. That is exactly what the WebGL build did.
    /// </para>
    /// <para>
    /// Whether touch is being <i>used</i> is a different question, and one already answered:
    /// <see cref="ControlSchemeTracker"/> follows the last device deliberately used. So the controls
    /// follow it — they appear the moment the screen is touched and leave the moment a mouse moves
    /// or a pad is pressed, which is also the correct behaviour for a hybrid laptop.
    /// </para>
    /// <para>
    /// The opening state is the one case the tracker cannot answer, because nothing has been used
    /// yet. A device with no mouse and no keyboard is a phone, and a phone must show its controls
    /// before it can be touched.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TouchControlVisibility : MonoBehaviour
    {
        private ControlSchemeTracker? _devices;
        private GameObject[] _controls = System.Array.Empty<GameObject>();
        private bool _forced;
        private bool _forcedState;
        private bool _shown = true;

        /// <summary>
        /// Reports when something is covering the match, so the controls stand down.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Supplied by the match, which knows about the between-arena screen and the pause menu.
        /// This class knows only about devices, and deliberately: what counts as "covered" is a
        /// question about the match, not about which hand is on the phone.
        /// </para>
        /// <para>
        /// Hiding them is not tidiness. The skill cluster is anchored to the bomb button in the
        /// bottom-right, which is where the right-hand choice card is drawn, and the cluster is
        /// built after the overlay — so it draws over the card and, being a raycast target, takes
        /// the taps meant for it. The card could not be picked on the one input the screen was
        /// rebuilt for. They are also dead while the match is stopped, and a live-looking control
        /// that does nothing is its own bug.
        /// </para>
        /// </remarks>
        public System.Func<bool>? Covered { get; set; }

        /// <summary>Follows the given tracker, toggling the supplied controls.</summary>
        public void Begin(ControlSchemeTracker devices, params GameObject?[] controls)
        {
            _devices = devices;

            var kept = new System.Collections.Generic.List<GameObject>(controls.Length);
            for (var i = 0; i < controls.Length; i++)
            {
                if (controls[i] != null)
                {
                    kept.Add(controls[i]!);
                }
            }

            _controls = kept.ToArray();

            Apply(ShouldShow(), force: true);
        }

        /// <summary>
        /// Pins visibility, overriding what the devices say.
        /// </summary>
        /// <remarks>
        /// It does not override <see cref="Covered"/>. Forcing answers "which devices is this build
        /// for", which is a different question from whether the match is currently behind a screen.
        /// </remarks>
        public void Force(bool visible)
        {
            _forced = true;
            _forcedState = visible;

            Apply(ShouldShow(), force: true);
        }

        private void Update()
        {
            if (!_forced)
            {
                Apply(ShouldShow(), force: false);
            }
        }

        private bool ShouldShow()
        {
            if (Covered != null && Covered())
            {
                return false;
            }

            return _forced
                ? _forcedState
                : _devices != null &&
                  ShouldShow(_devices.HasBeenUsed, _devices.Current, HasPointerHardware);
        }

        /// <summary>
        /// The rule itself, separated from the devices so it can be stated and tested.
        /// </summary>
        /// <remarks>
        /// Worth pulling out because this decision has been wrong twice: once keyed on the platform
        /// name, once on whether a touchscreen device existed. Both read as reasonable and both put
        /// a thumbstick over somebody's mouse-and-keyboard game.
        /// </remarks>
        /// <param name="hasBeenUsed">Whether any device has been deliberately used yet.</param>
        /// <param name="current">The device family currently in charge.</param>
        /// <param name="hasPointerHardware">Whether a mouse or keyboard is attached.</param>
        public static bool ShouldShow(bool hasBeenUsed, ControlScheme current, bool hasPointerHardware)
        {
            // Once something has been used, the answer is simply whether it was a finger.
            if (hasBeenUsed)
            {
                return current == ControlScheme.Touch;
            }

            // Before that there is nothing to go on but the hardware. No pointer and no keys means
            // a phone, and a phone must show its controls before anyone can touch them.
            return !hasPointerHardware;
        }

        private static bool HasPointerHardware =>
            UnityEngine.InputSystem.Mouse.current != null ||
            UnityEngine.InputSystem.Keyboard.current != null;

        private void Apply(bool visible, bool force)
        {
            if (!force && visible == _shown)
            {
                return;
            }

            _shown = visible;

            for (var i = 0; i < _controls.Length; i++)
            {
                if (_controls[i] != null)
                {
                    _controls[i].SetActive(visible);
                }
            }
        }
    }
}
