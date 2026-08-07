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

            if (gamepad.buttonEast.isPressed)
            {
                buttons = buttons.With(IntentButtons.Skill1);
            }

            if (gamepad.buttonWest.isPressed)
            {
                buttons = buttons.With(IntentButtons.Skill2);
            }

            if (gamepad.buttonNorth.isPressed)
            {
                buttons = buttons.With(IntentButtons.Skill3);
            }

            // The right stick aims, which is what makes a pad a genuine alternative to a mouse
            // rather than a downgrade: both can point somewhere the player is not running.
            sbyte aimX = 0;
            sbyte aimY = 0;
            var aim = gamepad.rightStick.ReadValue();

            if (aim.magnitude >= StickDeadzone)
            {
                PointerAim.TryPack(aim.x, aim.y, out aimX, out aimY);
            }

            var dpad = gamepad.dpad.ReadValue();
            if (dpad.sqrMagnitude > 0f)
            {
                return new PlayerIntent(
                    (sbyte)Mathf.RoundToInt(dpad.x * PlayerIntent.AxisRange),
                    (sbyte)Mathf.RoundToInt(dpad.y * PlayerIntent.AxisRange),
                    buttons,
                    aimX,
                    aimY);
            }

            var stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude < StickDeadzone)
            {
                return new PlayerIntent(0, 0, buttons, aimX, aimY);
            }

            return new PlayerIntent(
                (sbyte)Mathf.RoundToInt(Mathf.Clamp(stick.x, -1f, 1f) * PlayerIntent.AxisRange),
                (sbyte)Mathf.RoundToInt(Mathf.Clamp(stick.y, -1f, 1f) * PlayerIntent.AxisRange),
                buttons,
                aimX,
                aimY);
        }
    }
}
