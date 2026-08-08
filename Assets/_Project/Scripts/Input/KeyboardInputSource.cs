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
        private readonly IAimSource? _aim;

        /// <summary>Creates a keyboard source, optionally aimed by the mouse pointer.</summary>
        /// <remarks>
        /// Without an aim source the skillshot still works — it falls back to the direction of
        /// travel — so keyboard-only play is never blocked on the pointer being wired up.
        /// </remarks>
        public KeyboardInputSource(IAimSource? aim = null) => _aim = aim;

        /// <inheritdoc />
        public ControlScheme Scheme => ControlScheme.KeyboardMouse;

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
                buttons = buttons.With(IntentButtons.Skill1);
            }

            var mouse = Mouse.current;

            if (keyboard.qKey.isPressed || (mouse != null && mouse.leftButton.isPressed))
            {
                buttons = buttons.With(IntentButtons.Skill2);
            }

            if (keyboard.eKey.isPressed || (mouse != null && mouse.rightButton.isPressed))
            {
                buttons = buttons.With(IntentButtons.Skill3);
            }

            if (_aim != null && _aim.TryGetAim(out var aimGridX, out var aimGridY) &&
                PointerAim.TryPack(aimGridX, aimGridY, out var aimX, out var aimY))
            {
                return new PlayerIntent((sbyte)x, (sbyte)y, buttons, aimX, aimY);
            }

            return new PlayerIntent((sbyte)x, (sbyte)y, buttons);
        }
    }
}
