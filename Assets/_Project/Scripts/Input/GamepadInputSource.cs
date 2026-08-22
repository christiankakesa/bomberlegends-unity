using BomberLegends.Core;
using BomberLegends.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BomberLegends.Input
{
    /// <summary>
    /// Gamepad control: left stick moves, right stick aims, triggers act.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mapping: left stick and d-pad move, right stick aims, <b>A</b> bombs, <b>LB</b> dashes,
    /// <b>RT</b> shoots, <b>LT</b> takes the third slot.
    /// </para>
    /// <para>
    /// <b>Skills are on the shoulders and triggers, not the face buttons.</b> They were on face
    /// buttons through round two of playtesting, and every gamepad tester declined a second run.
    /// The cause turned out to be physical rather than a matter of taste: aiming needs the right
    /// thumb on the right stick, and a face button needs that same thumb somewhere else. A player
    /// could aim or shoot, never both — reported by one of them as "I can't aim the attack
    /// properly, are the buttons too close together?"
    /// </para>
    /// <para>
    /// Face buttons remain as aliases. They cost nothing, they help anyone who reaches for one
    /// first, and the on-screen hints name the triggers.
    /// </para>
    /// </remarks>
    public sealed class GamepadInputSource : IInputSource
    {
        /// <summary>
        /// Stick deflection below which nothing is requested.
        /// </summary>
        /// <remarks>
        /// Lower than it was. The old 0.3 discarded a third of the stick's travel, and the input
        /// jumped straight from nothing to a third of full deflection the moment it crossed — which
        /// is no way to aim.
        /// </remarks>
        private const float StickDeadzone = 0.2f;

        /// <summary>Trigger pull that counts as a press.</summary>
        private const float TriggerThreshold = 0.5f;

        /// <summary>
        /// How long an aim survives the stick returning to centre.
        /// </summary>
        /// <remarks>
        /// A player lines up a shot, releases, then presses fire — and without this the aim has
        /// already collapsed back to the direction of travel, so the shot leaves sideways. The most
        /// likely cause of "it fired at the wrong target". Long enough to cover thumb travel, short
        /// enough that a stale aim never surprises anyone.
        /// </remarks>
        private const float AimHoldSeconds = 0.35f;

        private sbyte _heldAimX;
        private sbyte _heldAimY;
        private float _aimExpiry;

        /// <inheritdoc />
        public ControlScheme Scheme => ControlScheme.Gamepad;

        /// <inheritdoc />
        public PlayerIntent Sample(int tick)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return PlayerIntent.None;
            }

            var buttons = ReadButtons(gamepad);
            ReadAim(gamepad, out var aimX, out var aimY);
            ReadMove(gamepad, out var moveX, out var moveY);

            return new PlayerIntent(moveX, moveY, buttons, aimX, aimY);
        }

        private static IntentButtons ReadButtons(Gamepad gamepad)
        {
            var buttons = IntentButtons.None;

            // Bomb stays on the south face button. It needs no aim, so the thumb can leave the
            // stick for it without costing anything.
            if (gamepad.buttonSouth.isPressed)
            {
                buttons = buttons.With(IntentButtons.Bomb);
            }

            // Dash on the left bumper rather than a trigger. It is the most timing-critical input
            // in the game — escaping your own blast — and a trigger costs it both travel and a
            // threshold, so a half press does nothing at all. Round two recorded exactly that
            // failure: "I panicked and fat-fingered the dodge." A bumper is digital and instant.
            if (gamepad.leftShoulder.isPressed || gamepad.buttonEast.isPressed)
            {
                buttons = buttons.With(IntentButtons.Skill1);
            }

            // Shot on the right trigger, under the index finger that never has to leave it while
            // the right thumb aims.
            if (gamepad.rightTrigger.ReadValue() > TriggerThreshold || gamepad.buttonWest.isPressed)
            {
                buttons = buttons.With(IntentButtons.Skill2);
            }

            // The third slot takes the remaining left-hand input. Nothing sits beside the shot
            // trigger, so there is nothing to fumble into while firing.
            if (gamepad.leftTrigger.ReadValue() > TriggerThreshold || gamepad.buttonNorth.isPressed)
            {
                buttons = buttons.With(IntentButtons.Skill3);
            }

            return buttons;
        }

        /// <summary>Reads the aim stick, holding the last deliberate direction briefly after release.</summary>
        private void ReadAim(Gamepad gamepad, out sbyte aimX, out sbyte aimY)
        {
            var aim = Rescale(gamepad.rightStick.ReadValue());

            if (aim.sqrMagnitude > 0f && PointerAim.TryPack(aim.x, aim.y, out var packedX, out var packedY))
            {
                _heldAimX = packedX;
                _heldAimY = packedY;
                _aimExpiry = Time.unscaledTime + AimHoldSeconds;
            }
            else if (Time.unscaledTime >= _aimExpiry)
            {
                // Expired. Reporting no aim lets the simulation fall back to the direction of
                // travel, which is the right answer for someone not aiming at all.
                _heldAimX = 0;
                _heldAimY = 0;
            }

            aimX = _heldAimX;
            aimY = _heldAimY;
        }

        private static void ReadMove(Gamepad gamepad, out sbyte moveX, out sbyte moveY)
        {
            // The d-pad is read first because it is already discrete and needs none of the
            // smoothing an analogue stick does.
            var dpad = gamepad.dpad.ReadValue();

            if (dpad.sqrMagnitude > 0f)
            {
                moveX = Quantise(dpad.x);
                moveY = Quantise(dpad.y);
                return;
            }

            var stick = Rescale(gamepad.leftStick.ReadValue());

            moveX = Quantise(stick.x);
            moveY = Quantise(stick.y);
        }

        /// <summary>
        /// Removes the deadzone without losing the travel inside it.
        /// </summary>
        /// <remarks>
        /// A plain threshold makes the stick jump from nothing to a fifth of full deflection the
        /// instant it is crossed, so there is no such thing as a small movement or a fine aim.
        /// Rescaling what remains of the range onto the whole range restores both.
        /// </remarks>
        private static Vector2 Rescale(Vector2 raw)
        {
            var magnitude = raw.magnitude;

            if (magnitude < StickDeadzone)
            {
                return Vector2.zero;
            }

            var scaled = (magnitude - StickDeadzone) / (1f - StickDeadzone);
            return raw / magnitude * Mathf.Clamp01(scaled);
        }

        private static sbyte Quantise(float axis) => (sbyte)Mathf.Clamp(
            Mathf.RoundToInt(axis * PlayerIntent.AxisRange),
            -PlayerIntent.AxisRange,
            PlayerIntent.AxisRange);
    }
}
