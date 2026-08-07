using BomberLegends.Core;
using BomberLegends.Data.Balance;
using BomberLegends.Simulation;
using UnityEngine;

namespace BomberLegends.Input
{
    /// <summary>
    /// Turns the on-screen thumbstick into simulation intent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is where the isometric control problem is solved. The four grid directions land on
    /// screen diagonals, so a stick pushed "up" is ambiguous, and naive handling of that ambiguity
    /// is the single most common reason isometric games feel bad to move around in. Three things
    /// happen here, in order:
    /// </para>
    /// <list type="number">
    /// <item><b>Basis rotation</b> converts the stick from screen space into grid space using the
    /// exact inverse of the render projection, so pushing towards a point moves towards it.</item>
    /// <item><b>Snapping with hysteresis</b> picks a cardinal direction and holds it until the stick
    /// clearly asks for another, so a thumb near a diagonal cannot stutter the character.</item>
    /// <item><b>Change buffering</b> keeps a fresh direction alive briefly if the stick returns to
    /// centre, so a quick flick still registers.</item>
    /// </list>
    /// <para>
    /// The buffer deliberately only covers direction <i>changes</i>. Buffering releases as well
    /// would carry the player past where they let go, which is unacceptable in a game where stopping
    /// on an exact tile decides whether a bomb kills you.
    /// </para>
    /// </remarks>
    public sealed class TouchInputSource : IInputSource
    {
        private readonly VirtualJoystick _joystick;
        private readonly InputFeelConfig _feel;
        private readonly ActionButton[] _actionButtons;
        private readonly IGridProjection _projection;

        private Direction _direction = Direction.None;
        private Direction _bufferedDirection = Direction.None;
        private float _bufferExpiryTime;

        /// <summary>Creates the source.</summary>
        public TouchInputSource(
            VirtualJoystick joystick,
            InputFeelConfig feel,
            IGridProjection projection,
            params ActionButton[] actionButtons)
        {
            _actionButtons = actionButtons ?? System.Array.Empty<ActionButton>();
            _joystick = joystick != null ? joystick : throw new System.ArgumentNullException(nameof(joystick));
            _feel = feel != null ? feel : throw new System.ArgumentNullException(nameof(feel));
            _projection = projection ?? throw new System.ArgumentNullException(nameof(projection));
        }

        /// <summary>Which actions the on-screen buttons are currently requesting.</summary>
        private IntentButtons Buttons
        {
            get
            {
                var buttons = IntentButtons.None;

                for (var i = 0; i < _actionButtons.Length; i++)
                {
                    var button = _actionButtons[i];
                    if (button != null && button.IsHeld)
                    {
                        buttons = buttons.With(button.Action);
                    }
                }

                return buttons;
            }
        }

        /// <inheritdoc />
        public PlayerIntent Sample(int tick)
        {
            var stick = _joystick.Value;

            if (stick.magnitude < _feel.Deadzone)
            {
                return ReleaseOrBuffer();
            }

            var grid = _projection.ScreenToGrid(stick);
            var snapped = DirectionSnapper.Snap(grid, _direction, _feel.SwitchRatio);

            if (snapped != _direction)
            {
                _direction = snapped;
                _bufferedDirection = snapped;
                _bufferExpiryTime = Time.unscaledTime + _feel.ChangeBufferSeconds;
            }

            return PlayerIntent.FromDirection(_direction, Buttons);
        }

        private PlayerIntent ReleaseOrBuffer()
        {
            // A direction requested moments ago survives the stick recentring, so a flick registers.
            if (_bufferedDirection != Direction.None && Time.unscaledTime < _bufferExpiryTime)
            {
                return PlayerIntent.FromDirection(_bufferedDirection, Buttons);
            }

            _direction = Direction.None;
            _bufferedDirection = Direction.None;
            return new PlayerIntent(0, 0, Buttons);
        }
    }
}
