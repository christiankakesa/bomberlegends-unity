using BomberLegends.Core;
using BomberLegends.Input;
using BomberLegends.Simulation;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers which device gets to speak when several are connected at once.
    /// </summary>
    public sealed class ControlSchemeTests
    {
        /// <summary>A source that always reports whatever it was handed.</summary>
        private sealed class StubSource : IInputSource
        {
            private readonly PlayerIntent _intent;

            public StubSource(ControlScheme scheme, PlayerIntent intent)
            {
                Scheme = scheme;
                _intent = intent;
            }

            public ControlScheme Scheme { get; }

            public int Samples { get; private set; }

            public PlayerIntent Sample(int tick)
            {
                Samples++;
                return _intent;
            }
        }

        /// <summary>A pointer always has a position, so this source always offers an aim.</summary>
        private static StubSource Mouse => new StubSource(
            ControlScheme.KeyboardMouse, new PlayerIntent(0, 0, IntentButtons.None, 100, 0));

        private static StubSource Pad(sbyte aimX, sbyte aimY) => new StubSource(
            ControlScheme.Gamepad, new PlayerIntent(50, 0, IntentButtons.None, aimX, aimY));

        [Test]
        public void AimFollowsThePadWhileThePadIsInCharge()
        {
            // The reported bug. A mouse reports a position every frame whether or not anyone has
            // touched it, so merging every source let a resting pointer supply aim forever and the
            // right stick was never heard.
            var tracker = new ControlSchemeTracker();
            tracker.ForceScheme(ControlScheme.Gamepad);

            var composite = new CompositeInputSource(tracker, Mouse, Pad(0, 100));

            var intent = composite.Sample(0);

            Assert.That(intent.AimY, Is.EqualTo(100), "aim must come from the stick");
            Assert.That(intent.AimX, Is.Zero, "and not from the pointer");
        }

        [Test]
        public void AimFollowsThePointerWhileTheMouseIsInCharge()
        {
            var tracker = new ControlSchemeTracker();
            tracker.ForceScheme(ControlScheme.KeyboardMouse);

            var composite = new CompositeInputSource(tracker, Mouse, Pad(0, 100));

            var intent = composite.Sample(0);

            Assert.That(intent.AimX, Is.EqualTo(100));
            Assert.That(intent.AimY, Is.Zero);
        }

        [Test]
        public void AGamepadWithNoAimDoesNotBorrowThePointers()
        {
            // Releasing the right stick must leave the shot following the direction of travel, not
            // silently snap it to wherever the mouse was left.
            var tracker = new ControlSchemeTracker();
            tracker.ForceScheme(ControlScheme.Gamepad);

            var composite = new CompositeInputSource(tracker, Mouse, Pad(0, 0));

            Assert.That(composite.Sample(0).HasAim, Is.False);
        }

        [Test]
        public void OnlyTheDeviceInChargeIsRead()
        {
            // Movement and aim have to come from the same device, or the character runs where the
            // pad says and shoots where the mouse happens to be.
            var tracker = new ControlSchemeTracker();
            tracker.ForceScheme(ControlScheme.Gamepad);

            var mouse = Mouse;
            var pad = Pad(0, 100);
            var composite = new CompositeInputSource(tracker, mouse, pad);

            composite.Sample(0);

            Assert.That(pad.Samples, Is.EqualTo(1));
            Assert.That(mouse.Samples, Is.Zero, "an idle device must not be polled into the result");
        }

        [Test]
        public void WithNoSourceForTheActiveFamily_WhateverIsBeingTouchedStillWorks()
        {
            // Going unresponsive is worse than answering from the wrong family.
            var tracker = new ControlSchemeTracker();
            tracker.ForceScheme(ControlScheme.Touch);

            var composite = new CompositeInputSource(tracker, Mouse, Pad(0, 100));

            Assert.That(composite.Sample(0).MoveX, Is.EqualTo(50));
        }

        [Test]
        public void TheCompositeReportsWhichFamilyIsInCharge()
        {
            // Button prompts and cursor visibility have to agree with what is driving the game.
            var tracker = new ControlSchemeTracker();
            var composite = new CompositeInputSource(tracker, Mouse, Pad(0, 100));

            tracker.ForceScheme(ControlScheme.Gamepad);

            Assert.That(composite.Scheme, Is.EqualTo(ControlScheme.Gamepad));
            Assert.That(tracker.HasBeenUsed, Is.True);
        }

        [Test]
        public void ACompositeNeedsAtLeastOneSource()
        {
            Assert.Throws<System.ArgumentException>(
                () => new CompositeInputSource(new ControlSchemeTracker()));
        }
    }
}
