using BomberLegends.Core;
using BomberLegends.Simulation;
using UnityEngine.InputSystem;

namespace BomberLegends.Input
{
    /// <summary>
    /// Keyboard control, for iterating in the Editor without a touch device.
    /// </summary>
    /// <remarks>
    /// Produces the same <see cref="PlayerIntent"/> as every other source, so behaviour matches the
    /// device exactly. Keys map straight to grid directions with no rotation: on a keyboard the
    /// player is thinking in grid terms, not screen terms.
    /// </remarks>
    public sealed class KeyboardInputSource : IInputSource
    {
        /// <inheritdoc />
        public PlayerIntent Sample(int tick)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return PlayerIntent.None;
            }

            var x = 0;
            var y = 0;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                x -= PlayerIntent.AxisRange;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                x += PlayerIntent.AxisRange;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                y -= PlayerIntent.AxisRange;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                y += PlayerIntent.AxisRange;
            }

            var buttons = IntentButtons.None;
            if (keyboard.spaceKey.isPressed)
            {
                buttons = buttons.With(IntentButtons.Bomb);
            }

            if (keyboard.leftShiftKey.isPressed)
            {
                buttons = buttons.With(IntentButtons.Special);
            }

            return new PlayerIntent((sbyte)x, (sbyte)y, buttons);
        }
    }
}
