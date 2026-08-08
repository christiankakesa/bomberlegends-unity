using BomberLegends.Core;
using BomberLegends.Data.Balance;
using BomberLegends.Simulation;
using UnityEngine;

namespace BomberLegends.Input
{
    /// <summary>
    /// Turns the on-screen stick and buttons into simulation intent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Movement is analogue and continuous, matching every other control surface. It previously
    /// snapped the stick to one of four grid directions with hysteresis and a change buffer, which
    /// was correct for the v1.0 lane-based game and became wrong the moment the player gained free
    /// 360° travel: a phone would have been playing a different game from a keyboard.
    /// </para>
    /// <para>
    /// Skills arrive from <see cref="SkillTouchButton"/>, which latches a cast on release. Each
    /// cast is reported for exactly one tick, because the simulation triggers a skill on the press
    /// edge and a button held down across several ticks must not fire more than once.
    /// </para>
    /// </remarks>
    public sealed class TouchInputSource : IInputSource
    {
        private readonly VirtualJoystick _joystick;
        private readonly InputFeelConfig _feel;
        private readonly ActionButton[] _actionButtons;
        private readonly SkillTouchButton[] _skillButtons;
        private readonly IGridProjection _projection;

        /// <summary>Creates the source.</summary>
        public TouchInputSource(
            VirtualJoystick joystick,
            InputFeelConfig feel,
            IGridProjection projection,
            ActionButton[]? actionButtons = null,
            SkillTouchButton[]? skillButtons = null)
        {
            _joystick = joystick != null ? joystick : throw new System.ArgumentNullException(nameof(joystick));
            _feel = feel != null ? feel : throw new System.ArgumentNullException(nameof(feel));
            _projection = projection ?? throw new System.ArgumentNullException(nameof(projection));
            _actionButtons = actionButtons ?? System.Array.Empty<ActionButton>();
            _skillButtons = skillButtons ?? System.Array.Empty<SkillTouchButton>();
        }

        /// <inheritdoc />
        public ControlScheme Scheme => ControlScheme.Touch;

        /// <inheritdoc />
        public PlayerIntent Sample(int tick)
        {
            var buttons = HeldButtons();
            ReadSkillCasts(ref buttons, out var aimX, out var aimY);

            ReadStick(out var moveX, out var moveY);

            return new PlayerIntent(moveX, moveY, buttons, aimX, aimY);
        }

        /// <summary>Reads the thumbstick as an analogue direction in grid space.</summary>
        private void ReadStick(out sbyte moveX, out sbyte moveY)
        {
            moveX = 0;
            moveY = 0;

            var stick = _joystick.Value;

            if (stick.magnitude < _feel.Deadzone)
            {
                return;
            }

            // Through the projection even though it is currently an identity, so a camera that is
            // ever rotated cannot silently divorce the controls from the picture.
            var grid = Vector2.ClampMagnitude(_projection.ScreenToGrid(stick), 1f);

            moveX = Quantise(grid.x);
            moveY = Quantise(grid.y);
        }

        /// <summary>Collects any casts released since the last tick.</summary>
        private void ReadSkillCasts(ref IntentButtons buttons, out sbyte aimX, out sbyte aimY)
        {
            aimX = 0;
            aimY = 0;

            for (var i = 0; i < _skillButtons.Length; i++)
            {
                var button = _skillButtons[i];

                if (button == null || !button.ConsumeCast(out var aim))
                {
                    continue;
                }

                buttons = buttons.With(button.Action);

                // A tap casts with no aim, leaving the simulation to use the direction of travel.
                if (aim.sqrMagnitude <= Mathf.Epsilon)
                {
                    continue;
                }

                var grid = _projection.ScreenToGrid(aim);
                PointerAim.TryPack(grid.x, grid.y, out aimX, out aimY);
            }
        }

        /// <summary>Which actions the plain on-screen buttons are requesting.</summary>
        private IntentButtons HeldButtons()
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

        private static sbyte Quantise(float axis) => (sbyte)Mathf.Clamp(
            Mathf.RoundToInt(axis * PlayerIntent.AxisRange),
            -PlayerIntent.AxisRange,
            PlayerIntent.AxisRange);
    }
}
