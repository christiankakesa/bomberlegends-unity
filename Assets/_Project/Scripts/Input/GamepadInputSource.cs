using BomberLegends.Core;
using BomberLegends.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BomberLegends.Input
{
    /// <summary>
    /// Gamepad control, shared by desktop play and device testing with a controller attached.
    /// </summary>
    /// <remarks>
    /// The d-pad is read before the stick because it is already discrete, so it needs none of the
    /// snapping the analogue stick does and gives the most precise control available.
    /// </remarks>
    public sealed class GamepadInputSource : IInputSource
    {
        private const float StickDeadzone = 0.3f;

        /// <inheritdoc />
        public PlayerIntent Sample(int tick)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return PlayerIntent.None;
            }

            var buttons = IntentButtons.None;
            if (gamepad.buttonSouth.isPressed)
            {
                buttons = buttons.With(IntentButtons.Bomb);
            }

            if (gamepad.buttonWest.isPressed)
            {
                buttons = buttons.With(IntentButtons.Special);
            }

            var dpad = gamepad.dpad.ReadValue();
            if (dpad.sqrMagnitude > 0f)
            {
                return new PlayerIntent(
                    (sbyte)Mathf.RoundToInt(dpad.x * PlayerIntent.AxisRange),
                    (sbyte)Mathf.RoundToInt(dpad.y * PlayerIntent.AxisRange),
                    buttons);
            }

            var stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude < StickDeadzone)
            {
                return new PlayerIntent(0, 0, buttons);
            }

            return new PlayerIntent(
                (sbyte)Mathf.RoundToInt(Mathf.Clamp(stick.x, -1f, 1f) * PlayerIntent.AxisRange),
                (sbyte)Mathf.RoundToInt(Mathf.Clamp(stick.y, -1f, 1f) * PlayerIntent.AxisRange),
                buttons);
        }
    }
}
