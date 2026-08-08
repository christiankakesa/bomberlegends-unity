using BomberLegends.Gameplay.Ui;
using BomberLegends.Input;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers when the on-screen controls are drawn.
    /// </summary>
    /// <remarks>
    /// This decision has been wrong twice — once keyed on the platform name, once on whether a
    /// touchscreen device existed. Both read as reasonable, and both drew a thumbstick over a
    /// mouse-and-keyboard game. It is stated here so the next change has to argue with something.
    /// </remarks>
    public sealed class TouchControlVisibilityTests
    {
        private static bool Show(bool used, ControlScheme scheme, bool pointer) =>
            TouchControlVisibility.ShouldShow(used, scheme, pointer);

        [Test]
        public void ADesktopBrowserGetsNoOnScreenControls()
        {
            // The reported bug. Desktop browsers advertise touch support with nothing attached, so
            // asking whether a touchscreen exists answered yes and drew the controls anyway.
            Assert.That(Show(used: false, ControlScheme.KeyboardMouse, pointer: true), Is.False);
        }

        [Test]
        public void APhoneGetsControlsBeforeAnythingHasBeenTouched()
        {
            // The chicken and egg: a phone cannot report touch as its active device until the
            // controls it needs to touch are already on screen.
            Assert.That(Show(used: false, ControlScheme.KeyboardMouse, pointer: false), Is.True);
        }

        [Test]
        public void TouchingTheScreenBringsThemBack()
        {
            // A laptop with a touchscreen: mouse and keys attached, but a finger was used last.
            Assert.That(Show(used: true, ControlScheme.Touch, pointer: true), Is.True);
        }

        [Test]
        public void UsingAMouseTakesThemAway()
        {
            Assert.That(Show(used: true, ControlScheme.KeyboardMouse, pointer: true), Is.False);
        }

        [Test]
        public void UsingAPadTakesThemAway()
        {
            // A phone with a controller paired is playing as a console, not as a phone.
            Assert.That(Show(used: true, ControlScheme.Gamepad, pointer: false), Is.False);
        }

        [Test]
        public void WhatWasUsedOutranksWhatIsAttached()
        {
            // The whole correction: presence of hardware only decides the opening state, and stops
            // mattering the moment the player tells you what they are actually holding.
            Assert.That(Show(used: true, ControlScheme.Touch, pointer: true), Is.True);
            Assert.That(Show(used: true, ControlScheme.KeyboardMouse, pointer: false), Is.False);
        }
    }
}
