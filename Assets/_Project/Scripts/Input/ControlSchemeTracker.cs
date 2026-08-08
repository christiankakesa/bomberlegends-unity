using UnityEngine.InputSystem;

namespace BomberLegends.Input
{
    /// <summary>
    /// Remembers which device the player last actually used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Needed because several devices are always connected and always readable, so "does this one
    /// have something to say?" is the wrong question. The decisive detail is that <b>a mouse always
    /// reports a position but only sometimes reports movement</b>: position is state, movement is
    /// intent. Asking the wrong one meant a pointer resting anywhere on screen out-voted a gamepad
    /// stick that was actively being aimed.
    /// </para>
    /// <para>
    /// Every device is judged the same way — by deliberate activity, past a threshold that
    /// stick drift and a nudged desk cannot cross. Whichever moved last owns the input, and the
    /// switch is immediate because a player who picks up a pad expects it to work on the first
    /// press, not the second.
    /// </para>
    /// <para>
    /// The same signal is what drives on-screen button prompts and cursor visibility in a finished
    /// game, which is why it is tracked centrally rather than being decided inside any one source.
    /// </para>
    /// </remarks>
    public sealed class ControlSchemeTracker
    {
        /// <summary>Stick deflection that counts as deliberate, past any resting drift.</summary>
        private const float StickDeadzone = 0.25f;

        /// <summary>Trigger pull that counts as deliberate.</summary>
        private const float TriggerThreshold = 0.3f;

        /// <summary>Pointer travel in a frame that counts as deliberate, in pixels.</summary>
        private const float PointerThreshold = 1.5f;

        /// <summary>The device family currently in charge.</summary>
        public ControlScheme Current { get; private set; } = ControlScheme.KeyboardMouse;

        /// <summary>Whether any device has been used yet.</summary>
        public bool HasBeenUsed { get; private set; }

        /// <summary>
        /// Pins the active family, overriding what the devices say until they are used again.
        /// </summary>
        /// <remarks>
        /// Exists for two reasons: tests cannot press a physical stick, and a settings screen that
        /// lets a player lock the game to one scheme needs exactly this. Without it the arbitration
        /// could only ever be exercised by hand on real hardware, which is how a bug like aim
        /// following the pointer survives to a play session.
        /// </remarks>
        public void ForceScheme(ControlScheme scheme) => Adopt(scheme);

        /// <summary>Re-reads every device and updates <see cref="Current"/>.</summary>
        public void Poll()
        {
            // Ordered by how unambiguous the signal is. A frame containing genuine input from two
            // families at once is a developer with both hands full, not a player.
            if (IsGamepadActive())
            {
                Adopt(ControlScheme.Gamepad);
                return;
            }

            if (IsTouchActive())
            {
                Adopt(ControlScheme.Touch);
                return;
            }

            if (IsKeyboardMouseActive())
            {
                Adopt(ControlScheme.KeyboardMouse);
            }
        }

        private void Adopt(ControlScheme scheme)
        {
            Current = scheme;
            HasBeenUsed = true;
        }

        private static bool IsGamepadActive()
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return false;
            }

            if (gamepad.leftStick.ReadValue().sqrMagnitude > StickDeadzone * StickDeadzone ||
                gamepad.rightStick.ReadValue().sqrMagnitude > StickDeadzone * StickDeadzone ||
                gamepad.dpad.ReadValue().sqrMagnitude > 0f)
            {
                return true;
            }

            if (gamepad.leftTrigger.ReadValue() > TriggerThreshold ||
                gamepad.rightTrigger.ReadValue() > TriggerThreshold)
            {
                return true;
            }

            return gamepad.buttonSouth.isPressed || gamepad.buttonEast.isPressed ||
                   gamepad.buttonWest.isPressed || gamepad.buttonNorth.isPressed ||
                   gamepad.startButton.isPressed || gamepad.selectButton.isPressed ||
                   gamepad.leftShoulder.isPressed || gamepad.rightShoulder.isPressed;
        }

        private static bool IsKeyboardMouseActive()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.isPressed)
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            // Movement, not position. A pointer parked on the far side of the screen is not the
            // player asking for anything, but it reports a position every single frame.
            if (mouse.delta.ReadValue().sqrMagnitude > PointerThreshold * PointerThreshold)
            {
                return true;
            }

            return mouse.leftButton.isPressed || mouse.rightButton.isPressed ||
                   mouse.middleButton.isPressed;
        }

        private static bool IsTouchActive()
        {
            var screen = Touchscreen.current;
            return screen != null && screen.primaryTouch.press.isPressed;
        }
    }
}
